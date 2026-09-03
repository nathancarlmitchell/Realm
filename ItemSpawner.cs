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
        // odds" — is Enemy.DropChances (see RollsCategory below): the only
        // way a gated-in category actually rolls, now that there's no
        // implicit PointValue-scaled fallback.
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

        // Whether a category's chance-based roll succeeds this call. An
        // enemy-supplied dropChances entry (Enemy.DropChances) is the *only*
        // way a category can drop at all now — no entry means no roll, full
        // stop. Previously a category with no explicit chance still rolled
        // via an implicit PointValue-scaled formula (see DropWeights'
        // now-removed doc comment history) — removed per direct request so
        // an enemy drops nothing unless its loot has actually been
        // considered and given real numbers, rather than silently
        // inheriting whatever the generic formula happened to produce.
        // Only meaningful for Spawn() — SpawnGuaranteedLoot's gear
        // categories are already deterministic (no chance roll at all) and
        // it doesn't use HealthManaPotion, so this isn't threaded through
        // there.
        private static bool RollsCategory(
            IReadOnlyDictionary<LootCategory, float> dropChances,
            LootCategory category
        ) => dropChances != null && dropChances.TryGetValue(category, out float chance) && rand.NextDouble() < chance;

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
        // and the category's own DropChances entry stop applying, since
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
        // MaxTierJump: how many tiers above the player's currently equipped
        // tier a drop can reach — rolled per category via RollTierOffset,
        // not a flat bump, so a tough kill doesn't guarantee the maximum
        // every time. The matching DropChanceDenominator (how often a
        // category rolled at all) was removed alongside RollsCategory's
        // implicit fallback formula — chance is now purely an enemy-
        // supplied DropChances number, no PointValue-scaled default.
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

            int maxTierJump = MaxTierJump(pointValue);

            // Drop weapon.
            if (dropPool.HasFlag(LootCategory.Weapon) && RollsCategory(dropChances, LootCategory.Weapon))
            {
                // Picked at random among every catalog entry at the resolved
                // tier (every WeaponType), not just the first match —
                // Game1.StartGame() merges Wand/Staff/Bow/Sword/Dagger in a
                // fixed order, so FirstOrDefault would always resolve to
                // whichever type loads first regardless of the player's
                // actual class.
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
            if (dropPool.HasFlag(LootCategory.Armor) && RollsCategory(dropChances, LootCategory.Armor))
            {
                // Same reasoning as the weapon drop above — Game1.StartGame()
                // merges Robe/Leather/Heavy in a fixed order, so
                // FirstOrDefault would always resolve to whichever
                // ArmorType loads first regardless of the player's actual
                // class.
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
            if (dropPool.HasFlag(LootCategory.Ring) && RollsCategory(dropChances, LootCategory.Ring))
            {
                // Picked at random among every ring at the resolved tier,
                // same reasoning as the weapon/armor drops above —
                // RingData.json lists several different stat rings per
                // tier (Attack/Defense/Speed/etc.), so FirstOrDefault
                // always resolved to whichever one happens to be listed
                // first for that tier, regardless of which stat rings
                // actually exist there.
                int tier = ResolveDropTier(pointValue, Player.Instance.Ring.Tier, maxTierJump, TierRangeFor(dropTierRanges, LootCategory.Ring));
                List<Ring> nextTierRings = Game1.Instance.Rings.Where(x => x.Tier == tier).ToList();

                if (nextTierRings.Count > 0)
                {
                    TrackBestBagRank(ref bestEquipmentRank, BagRankForRing(tier));
                    items.Add(nextTierRings[rand.Next(nextTierRings.Count)]);
                }
            }

            // Drop ability item.
            if (dropPool.HasFlag(LootCategory.AbilityItem) && RollsCategory(dropChances, LootCategory.AbilityItem))
            {
                // Same "wrong class is possible" spirit as weapon/armor drops
                // above — not filtered to the player's own class. Spell,
                // Quiver, Shield, Tome, and Cloak are separate catalogs (not
                // a single shared list like Weapons/Armors), so concatenate
                // all five next-tier results before picking at random.
                int tier = ResolveDropTier(pointValue, Player.Instance.AbilityItem.Tier, maxTierJump, TierRangeFor(dropTierRanges, LootCategory.AbilityItem));
                List<AbilityItem> nextTierAbilityItems = Game1
                    .Instance.Spells.Where(x => x.Tier == tier)
                    .Cast<AbilityItem>()
                    .Concat(Game1.Instance.Quivers.Where(x => x.Tier == tier))
                    .Concat(Game1.Instance.Shields.Where(x => x.Tier == tier))
                    .Concat(Game1.Instance.Tomes.Where(x => x.Tier == tier))
                    .Concat(Game1.Instance.Cloaks.Where(x => x.Tier == tier))
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
                else if (RollsCategory(dropChances, LootCategory.StatPotion))
                {
                    statPotionDropped = true;
                    items.Add(new Potion(RollStatPotion(statPotionPool)));
                }
            }

            if (dropPool.HasFlag(LootCategory.HealthManaPotion) && RollsCategory(dropChances, LootCategory.HealthManaPotion))
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
                    .Concat(Game1.Instance.Cloaks)
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

        // Every enabled category in dropPool that this method knows how to
        // resolve, in a fixed order — used to pick uniformly among whatever
        // categories a caller enables, not to imply any priority.
        private static readonly LootCategory[] SingleItemCategories =
        {
            LootCategory.Weapon,
            LootCategory.Armor,
            LootCategory.Ring,
            LootCategory.AbilityItem,
            LootCategory.HealthManaPotion,
        };

        // A third drop shape, between Spawn()'s "maybe nothing" chance table
        // and SpawnGuaranteedLoot()'s "one guaranteed item per category,
        // every time" — exactly one item guaranteed, picked uniformly among
        // whichever categories dropPool enables. First use: Dreadstump the
        // Pirate King, whose own review asked for "the same [Pirate Cave]
        // table, but a guaranteed chance of 1 item" rather than the other
        // three bosses' guaranteed one-per-category haul. StatPotion isn't
        // supported here (no statPotionPool/guaranteedPotionChances
        // parameters at all) — every current caller excludes it from
        // dropPool entirely, so it was never worth threading through. Every
        // gear category needs a real entry in dropTierRanges (there's no
        // player-tier-relative fallback here, unlike Spawn()/
        // SpawnGuaranteedLoot) — this method always resolves an *absolute*
        // tier, matching "the same table" being a fixed designer-picked
        // range, not something that should scale with the player's own gear.
        public static void SpawnGuaranteedSingleItem(
            Vector2 pos,
            LootCategory dropPool,
            IReadOnlyDictionary<LootCategory, (int Min, int Max)> dropTierRanges
        )
        {
            List<LootCategory> candidates = SingleItemCategories
                .Where(category => dropPool.HasFlag(category))
                .ToList();

            if (candidates.Count == 0)
                return;

            LootCategory picked = candidates[rand.Next(candidates.Count)];

            Item item = null;
            int? bagRank = null;

            switch (picked)
            {
                case LootCategory.Weapon:
                {
                    var tierRange = TierRangeFor(dropTierRanges, LootCategory.Weapon);
                    var options = tierRange.HasValue
                        ? ItemsAtOverrideTier(Game1.Instance.Weapons, x => x.Tier, tierRange.Value)
                        : [];
                    if (options.Count > 0)
                    {
                        Weapon chosen = options[rand.Next(options.Count)];
                        bagRank = BagRankForWeaponOrArmor(chosen.Tier);
                        item = chosen;
                    }
                    break;
                }
                case LootCategory.Armor:
                {
                    var tierRange = TierRangeFor(dropTierRanges, LootCategory.Armor);
                    var options = tierRange.HasValue
                        ? ItemsAtOverrideTier(Game1.Instance.Armors, x => x.Tier, tierRange.Value)
                        : [];
                    if (options.Count > 0)
                    {
                        Armor chosen = options[rand.Next(options.Count)];
                        bagRank = BagRankForWeaponOrArmor(chosen.Tier);
                        item = chosen;
                    }
                    break;
                }
                case LootCategory.Ring:
                {
                    var tierRange = TierRangeFor(dropTierRanges, LootCategory.Ring);
                    var options = tierRange.HasValue
                        ? ItemsAtOverrideTier(Game1.Instance.Rings, x => x.Tier, tierRange.Value)
                        : [];
                    if (options.Count > 0)
                    {
                        Ring chosen = options[rand.Next(options.Count)];
                        bagRank = BagRankForRing(chosen.Tier);
                        item = chosen;
                    }
                    break;
                }
                case LootCategory.AbilityItem:
                {
                    var tierRange = TierRangeFor(dropTierRanges, LootCategory.AbilityItem);
                    List<AbilityItem> allAbilityItems = Game1
                        .Instance.Spells.Cast<AbilityItem>()
                        .Concat(Game1.Instance.Quivers)
                        .Concat(Game1.Instance.Shields)
                        .Concat(Game1.Instance.Tomes)
                        .Concat(Game1.Instance.Cloaks)
                        .ToList();
                    var options = tierRange.HasValue
                        ? ItemsAtOverrideTier(allAbilityItems, x => x.Tier, tierRange.Value)
                        : [];
                    if (options.Count > 0)
                    {
                        AbilityItem chosen = options[rand.Next(options.Count)];
                        bagRank = BagRankForAbilityItem(chosen.Tier);
                        item = chosen;
                    }
                    break;
                }
                case LootCategory.HealthManaPotion:
                    item = new Potion(rand.Next(2) == 0 ? Potions.Mana : Potions.Health);
                    break;
            }

            // Only happens if a caller enables a gear category without a
            // matching dropTierRanges entry (nothing to resolve against) —
            // a graceful no-op rather than a crash, same grace every other
            // "no catalog entries at this tier" case in this file gets.
            if (item == null)
                return;

            // Same bag-art convention as Spawn()/SpawnGuaranteedLoot() —
            // ranked art for equipment, Brown for the potion case (not
            // Blue, which is reserved for a StatPotion drop specifically).
            Texture2D bagTexture = bagRank.HasValue ? BagTextureForRank(bagRank.Value) : Art.LootBag;

            LootBag bag = new()
            {
                Position = pos,
                Items = [item],
                image = bagTexture,
            };

            EntityManager.Add(bag);
            LootBags.Add(bag);

            Sound.Play(Sound.LootAppears, 0.4f);
        }

        // Drops one specific, named catalog item by exact Item.Name — the
        // shape a UT (untiered) item's own guaranteed-ish drop needs, which
        // none of the tier-based methods above can express (they all pick
        // randomly *within* a tier; a UT item isn't part of any tier at
        // all — see Equipment.IsUntiered's own doc comment). First real
        // use: Enemy.UniqueItemDropChances (Snake Eye Ring, dropped by
        // Stheno/Snakepit Guard). Only searches Game1.Instance.Rings today
        // since that's the only catalog with a UT entry so far — extend the
        // search to Weapons/Armors/AbilityItem-family the same way once a
        // UT item of one of those types exists.
        //
        // Throws on an unresolvable name rather than silently no-op'ing —
        // same "loud failure on a typo'd reference" convention
        // DungeonGenerator.ResolveTileByName() already established, so a
        // mistyped name in a boss's own UniqueItemDropChances is caught
        // immediately instead of silently never dropping.
        public static void SpawnUniqueItem(Vector2 pos, string itemName)
        {
            Ring ring = Game1.Instance.Rings.FirstOrDefault(x => x.Name == itemName);
            if (ring == null)
                throw new InvalidOperationException(
                    $"ItemSpawner.SpawnUniqueItem: no catalog item named '{itemName}' found."
                );

            // A UT item is, by definition, one of the more desirable drops
            // in the game — always the top-ranked bag art, not derived from
            // Tier (which is -1 for a UT item and would otherwise fall
            // through BagRankForRing's own "no band" case).
            LootBag bag = new()
            {
                Position = pos,
                Items = [ring],
                image = BagTextureForRank(3),
            };

            EntityManager.Add(bag);
            LootBags.Add(bag);

            Sound.Play(Sound.LootAppears, 0.4f);
        }
    }
}
