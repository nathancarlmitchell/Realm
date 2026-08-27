using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Realm.InventorySystem;

namespace Realm
{
    public static class ItemSpawner
    {
        // Which categories a given enemy's loot can roll from at all — the
        // "real drop pool" backlog item's first real lever: previously
        // every enemy rolled against every category uniformly (just at a
        // difficulty-scaled chance/tier via PointValue), with no way for
        // one enemy type to be excluded from a category entirely. See
        // Enemy.DropPool (defaults to All, so nothing existing changes
        // unless a specific factory opts into a narrower pool).
        //
        // On/off per category. The backlog's other half — "with its own
        // odds" — is the separate per-category weight multiplier below
        // (see WeightFor/WeightedChance, and Enemy.DropWeights), applied on
        // top of whatever this gate lets through.
        [Flags]
        public enum LootCategory
        {
            None = 0,
            Weapon = 1 << 0,
            Armor = 1 << 1,
            Ring = 1 << 2,
            AbilityItem = 1 << 3,
            StatPotion = 1 << 4,
            HealthManaPotion = 1 << 5,
            All = Weapon | Armor | Ring | AbilityItem | StatPotion | HealthManaPotion,
        }

        // Per-category chance multiplier layered on top of LootCategory's
        // simple in/out gate — the backlog's "with its own odds" half,
        // deliberately left out when DropPool/LootCategory first shipped.
        // 1.0 (i.e. no entry for that category) matches today's unweighted
        // rate exactly; >1 rolls more often, <1 less often. Only meaningful
        // for Spawn()'s chance-based rolls below — SpawnGuaranteedLoot's
        // included categories are deterministic once a reachable tier
        // exists (that's what makes the loot "guaranteed"), so a weight
        // wouldn't change anything there and isn't threaded through.
        private static float WeightFor(IReadOnlyDictionary<LootCategory, float> weights, LootCategory category) =>
            weights != null && weights.TryGetValue(category, out float weight) ? weight : 1f;

        // rand.Next(N) == 0 style chances get more frequent as N shrinks, so
        // a weight is applied as a divisor on the base denominator rather
        // than a multiplier on the chance itself. Floored at 1 (guaranteed,
        // rand.Next(1) is always 0) so an extreme weight can't produce a
        // zero/negative Next() argument.
        private static int WeightedChance(int baseChance, float weight) =>
            Math.Max(1, (int)Math.Round(baseChance / weight));

        // Whether a category's chance-based roll succeeds this call. An
        // enemy-supplied dropChances entry (Enemy.DropChances) is a literal
        // absolute probability (0.0-1.0) that bypasses the PointValue-scaled
        // baseChance and the DropWeights multiplier entirely — the same
        // "just give me the exact number" want RollGuaranteedPotions()
        // fills for specific stat potions, generalized to every other
        // chance-based category. Falls back to the existing weighted
        // formula when no override is set for that category, so an enemy
        // that doesn't opt in behaves exactly as before. Only meaningful
        // for Spawn() — SpawnGuaranteedLoot's gear categories are already
        // deterministic (no chance roll to override) and it doesn't use
        // HealthManaPotion at all, so this isn't threaded through there.
        private static bool RollsCategory(
            IReadOnlyDictionary<LootCategory, float> dropChances,
            IReadOnlyDictionary<LootCategory, float> dropWeights,
            LootCategory category,
            int baseChance
        ) =>
            dropChances != null && dropChances.TryGetValue(category, out float chance)
                ? rand.NextDouble() < chance
                : rand.Next(WeightedChance(baseChance, WeightFor(dropWeights, category))) == 0;

        // Every stat potion the StatPotion category can roll from by
        // default — the 8 options both Spawn()'s and SpawnGuaranteedLoot's
        // switch blocks used to hardcode inline. Health/Mana aren't here —
        // those are the separate HealthManaPotion category, not
        // StatPotion.
        private static readonly Potions[] AllStatPotions =
        {
            Potions.Attack,
            Potions.Defense,
            Potions.Dexterity,
            Potions.Life,
            Potions.ManaMax,
            Potions.Speed,
            Potions.Vitality,
            Potions.Wisdom,
        };

        // Which specific stat potion a StatPotion drop actually is — an
        // enemy-supplied pool (Enemy.StatPotionPool) narrows the roll to
        // just those types; null or empty (the default) rolls uniformly
        // from all 8, today's existing behavior unchanged. Shared by both
        // Spawn() and SpawnGuaranteedLoot() instead of each keeping its own
        // copy of the same 8-way switch.
        private static Potions RollStatPotion(IReadOnlyList<Potions> statPotionPool)
        {
            IReadOnlyList<Potions> pool =
                statPotionPool != null && statPotionPool.Count > 0 ? statPotionPool : AllStatPotions;
            return pool[rand.Next(pool.Count)];
        }

        // Independent per-potion drop chances (Enemy.GuaranteedPotionChances)
        // — a fundamentally different shape from RollStatPotion() above:
        // that's one roll that picks exactly one type out of a pool
        // (mutually exclusive), this is N independent rolls, one per entry,
        // each of which can succeed or fail on its own — so a single kill
        // could drop 0, 1, or all of them together (e.g. a guaranteed
        // Dexterity potion at 1.0 plus an independent 25% chance at a
        // Defense potion, which can both land on the same kill). When an
        // enemy sets this (non-empty), it entirely replaces the normal
        // single-roll StatPotion behavior for that enemy — StatPotionPool
        // and the category's own DropWeights entry stop applying, since
        // there's no longer a single roll for them to modify.
        private static List<Potions> RollGuaranteedPotions(
            IReadOnlyDictionary<Potions, float> guaranteedPotionChances
        )
        {
            List<Potions> results = [];
            if (guaranteedPotionChances == null)
                return results;

            foreach (var (potion, chance) in guaranteedPotionChances)
                if (rand.NextDouble() < chance)
                    results.Add(potion);

            return results;
        }

        // Which loot bag texture a drop uses — driven by the highest tier
        // of equipment actually dropped, not by which category happened to
        // roll last (the old behavior). Ranks are compared across every
        // dropped equipment item regardless of category, so a bag holding
        // both a low-tier Ring and a high-tier Weapon shows the Weapon's
        // higher rank. Cutoffs given directly against each category's own
        // real Tier field (0-indexed) — Weapon/Armor share one scale since
        // both catalogs run 0-14, AbilityItem has its own since Spell/
        // Quiver/Shield only run 0-7, and Ring has its own since its
        // catalog is far shallower (currently only 0-1) — so higher ranks
        // (Purple 2+ and above) are effectively unreachable for rings today
        // until the Ring catalog gets built out further; that's a content
        // gap, not a bug in this ranking. Tier 0 in every category falls
        // through to no band at all (null) — the "starting" tier isn't
        // even worth a Pink bag. 4 ranks: 0=Pink, 1=Purple, 2=Cyan, 3=Red.
        private static int? BagRankForWeaponOrArmor(int tier) =>
            tier switch
            {
                >= 13 => 3,
                >= 10 => 2,
                >= 7 => 1,
                >= 1 => 0,
                _ => null,
            };

        private static int? BagRankForAbilityItem(int tier) =>
            tier switch
            {
                >= 7 => 3,
                >= 5 => 2,
                >= 3 => 1,
                >= 1 => 0,
                _ => null,
            };

        private static int? BagRankForRing(int tier) =>
            tier switch
            {
                >= 7 => 3,
                >= 5 => 2,
                >= 2 => 1,
                >= 1 => 0,
                _ => null,
            };

        private static Texture2D BagTextureForRank(int rank) =>
            rank switch
            {
                3 => Art.LootBagRed,
                2 => Art.LootBagCyan,
                1 => Art.LootBagPurple,
                _ => Art.LootBagPink, // rank 0
            };

        // Folds a category's own rank (or null, meaning that category
        // either didn't drop or landed in the tier-0 "no band" gap) into
        // the running best-so-far — "highest tier present" across every
        // equipment category in the bag, not just whichever rolled last.
        private static void TrackBestBagRank(ref int? bestRank, int? candidateRank)
        {
            if (candidateRank.HasValue && (!bestRank.HasValue || candidateRank.Value > bestRank.Value))
                bestRank = candidateRank;
        }

        private static readonly Random rand = new();

        // Move to RealmState?
        public static List<LootBag> LootBags = [];

        public static void Reset()
        {
            // Each bag is also a plain Entity tracked by EntityManager (for
            // its icon draw), separately from this list (for the
            // interactive click-to-pickup logic in DrawLoot()). Clearing
            // just this list leaves that Entity registration untouched, and
            // not every state resets EntityManager on entry (RealmState —
            // dungeons — doesn't; only NexusState/BossRealmState do) — so a
            // bag could keep rendering its icon at its old position forever
            // with nothing left able to interact with it. Expiring it here
            // lets EntityManager's own cleanup drop it on its next Update()
            // regardless of whether the destination state resets it too.
            foreach (LootBag bag in LootBags)
                bag.IsExpired = true;

            LootBags = [];
        }

        private static (LootBag bag, float distSq) FindNearestOpenBag()
        {
            LootBag nearest = null;
            float nearestDistSq = float.MaxValue;

            foreach (LootBag bag in LootBags)
            {
                if (!Player.Instance.Bounds.Intersects(bag.Bounds))
                    continue;

                float distSq = Vector2.DistanceSquared(Player.Instance.Position, bag.Position);
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = bag;
                }
            }

            return (nearest, nearestDistSq);
        }

        // Whichever bag the player is touching and closest to, so only one bag's
        // contents render/accept clicks at a time — otherwise two bags in pickup
        // range at once draw their item portraits on top of each other, since
        // each bag lays out its items at the same fixed screen position.
        public static LootBag NearestOpenBag() => FindNearestOpenBag().bag;

        // Same "closest wins" comparison, exposed as a distance rather than a
        // bag reference — lets other proximity-gated UI (BankSystem, via
        // Portal) decide whether a loot bag beats it for focus, e.g. when a bag
        // is dropped right next to the bank portal. float.MaxValue if no bag is
        // currently in pickup range.
        public static float NearestOpenBagDistanceSquared() => FindNearestOpenBag().distSq;

        // Difficulty buckets, keyed off PointValue — already ranks enemies
        // by toughness (higher = more score for killing it), so it doubles
        // as the loot-difficulty signal instead of a separate field.
        // Boundaries picked to sit between the game's actual PointValues
        // (Snake 2, Seeker 7, Wanderer 15, SpriteGod 200, Limon 2000) so
        // each existing enemy type lands cleanly in one bucket.
        //
        //   < 10   : Snake, Seeker    — common trash
        //   < 100  : Wanderer         — today's original baseline
        //   < 1000 : SpriteGod        — a real threat
        //   1000+  : bosses           — always via SpawnGuaranteedLoot below
        //
        // DropChanceDenominator: lower = more frequent (rand.Next(N) == 0).
        // MaxTierJump: how many tiers above the player's currently equipped
        // tier a drop can reach — rolled per category via RollTierOffset,
        // not a flat bump, so a tough kill doesn't guarantee the maximum
        // every time.
        //
        // Doubled from 20/15/8 per direct playtest feedback — drop rates
        // felt too high across the board. A flat "everything in half" cut,
        // not yet a Difficulty-style global knob (see Difficulty.cs) — the
        // user explicitly asked for the raw numbers halved for now, nothing
        // more abstracted. This also halves every enemy's DropWeights-based
        // rate automatically (WeightedChance divides this same denominator
        // by the weight), so BigSnake's potion-leaning weights etc. don't
        // need separate retuning.
        private static int DropChanceDenominator(int pointValue) =>
            pointValue switch
            {
                < 10 => 40,
                < 100 => 30,
                _ => 16,
            };

        private static int MaxTierJump(int pointValue) =>
            pointValue switch
            {
                < 100 => 1,
                < 1000 => 2,
                _ => 3,
            };

        private static int RollTierOffset(int maxTierJump) => rand.Next(1, maxTierJump + 1);

        // Snake/Seeker-tier trash. These always drop from a fixed low
        // absolute tier range instead of scaling off the player's own
        // gear — a Snake shouldn't hand a heavily-geared player a
        // relatively-scaled-up item just because their own tier is high;
        // it should always drop the same weak loot it always does.
        private const int WeakEnemyMinTier = 0;
        private const int WeakEnemyMaxTier = 2;

        private static bool IsWeakEnemy(int pointValue) => pointValue < 10;

        // Looks up a specific category's tier-range override out of an
        // enemy's full per-category map (Enemy.DropTierRanges) — a category
        // with no entry falls through to null, meaning "use the normal
        // PointValue/player-tier formula for this category." Per-category
        // rather than one shared range, since an enemy might want e.g.
        // Weapon at tier 7-10 but Ring at 3-4, not the same window for both.
        private static (int Min, int Max)? TierRangeFor(
            IReadOnlyDictionary<LootCategory, (int Min, int Max)> tierRanges,
            LootCategory category
        ) => tierRanges != null && tierRanges.TryGetValue(category, out var range) ? range : null;

        // The tier a drop should target for the given category. An enemy-
        // supplied tierRange (via TierRangeFor/Enemy.DropTierRanges) takes
        // priority over everything else — a designer-picked absolute range,
        // generalizing the weak-enemy fixed range below to any enemy rather
        // than just ones under the PointValue threshold. Falling through
        // that: a fixed low absolute roll for weak enemies (see above), or
        // the player's current tier plus a random jump (see MaxTierJump)
        // for everything else. Every category's drop routes through this
        // instead of repeating the branch.
        private static int ResolveDropTier(
            int pointValue,
            int playerTier,
            int maxTierJump,
            (int Min, int Max)? tierRange
        ) =>
            tierRange.HasValue
                ? rand.Next(tierRange.Value.Min, tierRange.Value.Max + 1)
                : IsWeakEnemy(pointValue)
                    ? rand.Next(WeakEnemyMinTier, WeakEnemyMaxTier + 1)
                    : playerTier + RollTierOffset(maxTierJump);

        public static void Spawn(
            Vector2 pos,
            int pointValue = 0,
            LootCategory dropPool = LootCategory.All,
            IReadOnlyDictionary<LootCategory, float> dropWeights = null,
            IReadOnlyDictionary<LootCategory, (int Min, int Max)> dropTierRanges = null,
            IReadOnlyList<Potions> statPotionPool = null,
            IReadOnlyDictionary<Potions, float> guaranteedPotionChances = null,
            IReadOnlyDictionary<LootCategory, float> dropChances = null
        )
        {
            List<Item> items = [];

            // Best equipment rank seen across every category below (not
            // just whichever rolled last — see TrackBestBagRank/
            // BagRankFor*), plus whether any stat potion dropped, resolved
            // into the actual bagTexture once every category's been
            // checked.
            int? bestEquipmentRank = null;
            bool statPotionDropped = false;

            int dropChance = DropChanceDenominator(pointValue);
            int maxTierJump = MaxTierJump(pointValue);

            // Drop weapon.
            if (dropPool.HasFlag(LootCategory.Weapon) && RollsCategory(dropChances, dropWeights, LootCategory.Weapon, dropChance))
            {
                // Picked at random among every catalog entry at the resolved
                // tier (both WeaponTypes), not just the first match.
                // WeaponData.json lists every Wand before any Bow, so
                // FirstOrDefault would always resolve to a Wand regardless
                // of the player's actual class.
                int tier = ResolveDropTier(pointValue, Player.Instance.Weapon.Tier, maxTierJump, TierRangeFor(dropTierRanges, LootCategory.Weapon));
                List<Weapon> nextTierWeapons = Game1
                    .Instance.Weapons.Where(x => x.Tier == tier)
                    .ToList();

                if (nextTierWeapons.Count > 0)
                {
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForWeaponOrArmor(tier));
                    items.Add(nextTierWeapons[rand.Next(nextTierWeapons.Count)]);
                }
            }

            // Drop armor.
            if (dropPool.HasFlag(LootCategory.Armor) && RollsCategory(dropChances, dropWeights, LootCategory.Armor, dropChance))
            {
                // Same reasoning as the weapon drop above — ArmorData.json
                // lists every Robe before any Leather piece.
                int tier = ResolveDropTier(pointValue, Player.Instance.Armor.Tier, maxTierJump, TierRangeFor(dropTierRanges, LootCategory.Armor));
                List<Armor> nextTierArmors = Game1
                    .Instance.Armors.Where(x => x.Tier == tier)
                    .ToList();

                if (nextTierArmors.Count > 0)
                {
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForWeaponOrArmor(tier));
                    items.Add(nextTierArmors[rand.Next(nextTierArmors.Count)]);
                }
            }

            // Drop ring.
            if (dropPool.HasFlag(LootCategory.Ring) && RollsCategory(dropChances, dropWeights, LootCategory.Ring, dropChance))
            {
                int tier = ResolveDropTier(pointValue, Player.Instance.Ring.Tier, maxTierJump, TierRangeFor(dropTierRanges, LootCategory.Ring));
                if (Game1.Instance.Rings.Exists(x => x.Tier == tier))
                {
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForRing(tier));
                    Ring nextRing = Game1.Instance.Rings.FirstOrDefault(x => x.Tier == tier);
                    items.Add(nextRing);
                }
            }

            // Drop ability item.
            if (dropPool.HasFlag(LootCategory.AbilityItem) && RollsCategory(dropChances, dropWeights, LootCategory.AbilityItem, dropChance))
            {
                // Same "wrong class is possible" spirit as weapon/armor drops
                // above — not filtered to the player's own class. Spell,
                // Quiver, Shield, and Tome are separate catalogs (not a
                // single shared list like Weapons/Armors), so concatenate
                // all four next-tier results before picking at random.
                int tier = ResolveDropTier(pointValue, Player.Instance.AbilityItem.Tier, maxTierJump, TierRangeFor(dropTierRanges, LootCategory.AbilityItem));
                List<AbilityItem> nextTierAbilityItems = Game1
                    .Instance.Spells.Where(x => x.Tier == tier)
                    .Cast<AbilityItem>()
                    .Concat(Game1.Instance.Quivers.Where(x => x.Tier == tier))
                    .Concat(Game1.Instance.Shields.Where(x => x.Tier == tier))
                    .Concat(Game1.Instance.Tomes.Where(x => x.Tier == tier))
                    .ToList();

                if (nextTierAbilityItems.Count > 0)
                {
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForAbilityItem(tier));
                    items.Add(nextTierAbilityItems[rand.Next(nextTierAbilityItems.Count)]);
                }
            }

            // Drop stat potion(s). A non-empty guaranteedPotionChances
            // entirely replaces the normal single-roll behavior below with
            // N independent per-potion rolls — see RollGuaranteedPotions().
            if (dropPool.HasFlag(LootCategory.StatPotion))
            {
                if (guaranteedPotionChances != null && guaranteedPotionChances.Count > 0)
                {
                    foreach (Potions potion in RollGuaranteedPotions(guaranteedPotionChances))
                    {
                        statPotionDropped = true;
                        items.Add(new Potion(potion));
                    }
                }
                else if (RollsCategory(dropChances, dropWeights, LootCategory.StatPotion, 30))
                {
                    statPotionDropped = true;
                    items.Add(new Potion(RollStatPotion(statPotionPool)));
                }
            }

            if (dropPool.HasFlag(LootCategory.HealthManaPotion) && RollsCategory(dropChances, dropWeights, LootCategory.HealthManaPotion, 20))
            {
                if (rand.Next(2) == 0)
                    items.Add(new Potion(Potions.Mana));
                else
                    items.Add(new Potion(Potions.Health));
            }

            // Equipment's tier always wins when any dropped — potions have
            // no tier to compare against, so Blue only shows when the bag
            // is potion-only. Brown (Art.LootBag) is the fallback when
            // nothing tiered or Blue-worthy dropped at all (e.g. only a
            // Health/Mana potion, which doesn't set either flag above).
            Texture2D bagTexture = bestEquipmentRank.HasValue
                ? BagTextureForRank(bestEquipmentRank.Value)
                : statPotionDropped
                    ? Art.LootBagBlue
                    : Art.LootBag;

            if (items.Count > 0)
            {
                LootBag bag = new()
                {
                    Position = pos,
                    Items = items,
                    image = bagTexture,
                };

                EntityManager.Add(bag);
                LootBags.Add(bag);

                Sound.Play(Sound.LootAppears, 0.4f);
            }
        }

        // Steps down from the rolled offset to 1 until a tier with actual
        // catalog entries is found, instead of a single-offset roll that
        // could land past the catalog's top tier and come back empty —
        // keeps SpawnGuaranteedLoot's "every category always contributes
        // when any reachable tier exists" promise even though the offset
        // itself is now randomized instead of always exactly +1.
        private static List<T> ItemsAtBestAvailableTier<T>(
            IEnumerable<T> catalog,
            Func<T, int> tierOf,
            int baseTier,
            int rolledOffset
        )
        {
            for (int offset = rolledOffset; offset >= 1; offset--)
            {
                List<T> found = catalog.Where(x => tierOf(x) == baseTier + offset).ToList();
                if (found.Count > 0)
                    return found;
            }
            return [];
        }

        // An enemy-supplied tierRange (Enemy.DropTierRange) bypasses the
        // "step down from the player's tier until something exists" search
        // above entirely — a single exact-tier filter at a designer-rolled
        // absolute tier, mirroring Spawn()'s own tierRange handling in
        // ResolveDropTier(). Same grace as everywhere else in this file if
        // that exact tier has no catalog entries: no item for that
        // category, not a fallback search — the enemy's own range is
        // expected to actually have content.
        private static List<T> ItemsAtOverrideTier<T>(
            IEnumerable<T> catalog,
            Func<T, int> tierOf,
            (int Min, int Max) tierRange
        ) => catalog.Where(x => tierOf(x) == rand.Next(tierRange.Min, tierRange.Max + 1)).ToList();

        // Boss drops — same tier-selection logic as Spawn() above for each
        // category, but without the drop-chance rolls: every category that
        // has any reachable tier available always contributes an item (a
        // graceful no-op only if the player is already at the catalog's max
        // tier for that category), plus always one random stat potion.
        // Single bag, same tier-ranked bag art as Spawn() (see
        // TrackBestBagRank/BagRankFor*) — a boss's typically-high-tier gear
        // routinely lands Cyan/Red bags rather than one fixed color.
        public static void SpawnGuaranteedLoot(
            Vector2 pos,
            int pointValue = 0,
            LootCategory dropPool = LootCategory.All,
            IReadOnlyDictionary<LootCategory, (int Min, int Max)> dropTierRanges = null,
            IReadOnlyList<Potions> statPotionPool = null,
            IReadOnlyDictionary<Potions, float> guaranteedPotionChances = null
        )
        {
            List<Item> items = [];
            int maxTierJump = MaxTierJump(pointValue);

            // Same "highest tier present wins" bag art as Spawn() — see
            // TrackBestBagRank/BagRankFor*. Ranked off the actually-
            // selected item's own Tier, not the originally-targeted one,
            // since ItemsAtBestAvailableTier can step down to a lower tier
            // than requested when the exact target has no catalog entries.
            int? bestEquipmentRank = null;
            bool statPotionDropped = false;

            if (dropPool.HasFlag(LootCategory.Weapon))
            {
                (int Min, int Max)? weaponTierRange = TierRangeFor(dropTierRanges, LootCategory.Weapon);
                List<Weapon> nextTierWeapons = weaponTierRange.HasValue
                    ? ItemsAtOverrideTier(Game1.Instance.Weapons, x => x.Tier, weaponTierRange.Value)
                    : ItemsAtBestAvailableTier(
                        Game1.Instance.Weapons,
                        x => x.Tier,
                        Player.Instance.Weapon.Tier,
                        RollTierOffset(maxTierJump)
                    );
                if (nextTierWeapons.Count > 0)
                {
                    Weapon chosen = nextTierWeapons[rand.Next(nextTierWeapons.Count)];
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForWeaponOrArmor(chosen.Tier));
                    items.Add(chosen);
                }
            }

            if (dropPool.HasFlag(LootCategory.Armor))
            {
                (int Min, int Max)? armorTierRange = TierRangeFor(dropTierRanges, LootCategory.Armor);
                List<Armor> nextTierArmors = armorTierRange.HasValue
                    ? ItemsAtOverrideTier(Game1.Instance.Armors, x => x.Tier, armorTierRange.Value)
                    : ItemsAtBestAvailableTier(
                        Game1.Instance.Armors,
                        x => x.Tier,
                        Player.Instance.Armor.Tier,
                        RollTierOffset(maxTierJump)
                    );
                if (nextTierArmors.Count > 0)
                {
                    Armor chosen = nextTierArmors[rand.Next(nextTierArmors.Count)];
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForWeaponOrArmor(chosen.Tier));
                    items.Add(chosen);
                }
            }

            if (dropPool.HasFlag(LootCategory.Ring))
            {
                (int Min, int Max)? ringTierRange = TierRangeFor(dropTierRanges, LootCategory.Ring);
                List<Ring> nextTierRings = ringTierRange.HasValue
                    ? ItemsAtOverrideTier(Game1.Instance.Rings, x => x.Tier, ringTierRange.Value)
                    : ItemsAtBestAvailableTier(
                        Game1.Instance.Rings,
                        x => x.Tier,
                        Player.Instance.Ring.Tier,
                        RollTierOffset(maxTierJump)
                    );
                if (nextTierRings.Count > 0)
                {
                    Ring chosen = nextTierRings[rand.Next(nextTierRings.Count)];
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForRing(chosen.Tier));
                    items.Add(chosen);
                }
            }

            if (dropPool.HasFlag(LootCategory.AbilityItem))
            {
                List<AbilityItem> allAbilityItems = Game1
                    .Instance.Spells.Cast<AbilityItem>()
                    .Concat(Game1.Instance.Quivers)
                    .Concat(Game1.Instance.Shields)
                    .Concat(Game1.Instance.Tomes)
                    .ToList();
                (int Min, int Max)? abilityItemTierRange = TierRangeFor(dropTierRanges, LootCategory.AbilityItem);
                List<AbilityItem> nextTierAbilityItems = abilityItemTierRange.HasValue
                    ? ItemsAtOverrideTier(allAbilityItems, x => x.Tier, abilityItemTierRange.Value)
                    : ItemsAtBestAvailableTier(
                        allAbilityItems,
                        x => x.Tier,
                        Player.Instance.AbilityItem.Tier,
                        RollTierOffset(maxTierJump)
                    );
                if (nextTierAbilityItems.Count > 0)
                {
                    AbilityItem chosen = nextTierAbilityItems[rand.Next(nextTierAbilityItems.Count)];
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForAbilityItem(chosen.Tier));
                    items.Add(chosen);
                }
            }

            if (dropPool.HasFlag(LootCategory.StatPotion))
            {
                if (guaranteedPotionChances != null && guaranteedPotionChances.Count > 0)
                {
                    foreach (Potions potion in RollGuaranteedPotions(guaranteedPotionChances))
                    {
                        statPotionDropped = true;
                        items.Add(new Potion(potion));
                    }
                }
                else
                {
                    statPotionDropped = true;
                    items.Add(new Potion(RollStatPotion(statPotionPool)));
                }
            }

            // Same resolution as Spawn() — equipment's tier always wins,
            // Blue only for a potion-only bag, Brown as the final fallback
            // (though in practice a boss's guaranteed loot rarely ends up
            // with nothing at all).
            Texture2D bagTexture = bestEquipmentRank.HasValue
                ? BagTextureForRank(bestEquipmentRank.Value)
                : statPotionDropped
                    ? Art.LootBagBlue
                    : Art.LootBag;

            LootBag bag = new()
            {
                Position = pos,
                Items = items,
                image = bagTexture,
            };

            EntityManager.Add(bag);
            LootBags.Add(bag);

            Sound.Play(Sound.LootAppears, 0.4f);
        }
    }
}
