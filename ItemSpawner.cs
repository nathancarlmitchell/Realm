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
        private static int DropChanceDenominator(int pointValue) =>
            pointValue switch
            {
                < 10 => 20,
                < 100 => 15,
                _ => 8,
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

        // The tier a drop should target for the given category: a fixed low
        // absolute roll for weak enemies (see above), or the player's
        // current tier plus a random jump (see MaxTierJump) for everything
        // else — the only two tier-selection shapes anything in this file
        // needs, so every category's drop routes through this instead of
        // repeating the branch.
        private static int ResolveDropTier(int pointValue, int playerTier, int maxTierJump) =>
            IsWeakEnemy(pointValue)
                ? rand.Next(WeakEnemyMinTier, WeakEnemyMaxTier + 1)
                : playerTier + RollTierOffset(maxTierJump);

        // Not currently called from anywhere — regular enemies stopped
        // dropping loot entirely (Enemy.SpawnLoot()'s base is now a no-op;
        // only Boss.SpawnLoot() drops, via SpawnGuaranteedLoot() below).
        // Left in place rather than deleted: it's the flagged foundation
        // for a future "per-enemy drop pool" system (see docs/BACKLOG.md).
        public static void Spawn(Vector2 pos, int pointValue = 0)
        {
            List<Item> items = [];
            Texture2D bagTexture = Art.LootBag;

            int dropChance = DropChanceDenominator(pointValue);
            int maxTierJump = MaxTierJump(pointValue);

            // Drop weapon.
            if (rand.Next(dropChance) == 0)
            {
                // Picked at random among every catalog entry at the resolved
                // tier (both WeaponTypes), not just the first match.
                // WeaponData.json lists every Wand before any Bow, so
                // FirstOrDefault would always resolve to a Wand regardless
                // of the player's actual class.
                int tier = ResolveDropTier(pointValue, Player.Instance.Weapon.Tier, maxTierJump);
                List<Weapon> nextTierWeapons = Game1
                    .Instance.Weapons.Where(x => x.Tier == tier)
                    .ToList();

                if (nextTierWeapons.Count > 0)
                {
                    bagTexture = Art.LootBagPink;
                    items.Add(nextTierWeapons[rand.Next(nextTierWeapons.Count)]);
                }
            }

            // Drop armor.
            if (rand.Next(dropChance) == 0)
            {
                // Same reasoning as the weapon drop above — ArmorData.json
                // lists every Robe before any Leather piece.
                int tier = ResolveDropTier(pointValue, Player.Instance.Armor.Tier, maxTierJump);
                List<Armor> nextTierArmors = Game1
                    .Instance.Armors.Where(x => x.Tier == tier)
                    .ToList();

                if (nextTierArmors.Count > 0)
                {
                    bagTexture = Art.LootBagPurple;
                    items.Add(nextTierArmors[rand.Next(nextTierArmors.Count)]);
                }
            }

            // Drop ring.
            if (rand.Next(dropChance) == 0)
            {
                int tier = ResolveDropTier(pointValue, Player.Instance.Ring.Tier, maxTierJump);
                if (Game1.Instance.Rings.Exists(x => x.Tier == tier))
                {
                    bagTexture = Art.LootBagWhite;
                    Ring nextRing = Game1.Instance.Rings.FirstOrDefault(x => x.Tier == tier);
                    items.Add(nextRing);
                }
            }

            // Drop ability item.
            if (rand.Next(dropChance) == 0)
            {
                // Same "wrong class is possible" spirit as weapon/armor drops
                // above — not filtered to the player's own class. Spell,
                // Quiver, and Shield are separate catalogs (not a single
                // shared list like Weapons/Armors), so concatenate all three
                // next-tier results before picking at random.
                int tier = ResolveDropTier(pointValue, Player.Instance.AbilityItem.Tier, maxTierJump);
                List<AbilityItem> nextTierAbilityItems = Game1
                    .Instance.Spells.Where(x => x.Tier == tier)
                    .Cast<AbilityItem>()
                    .Concat(Game1.Instance.Quivers.Where(x => x.Tier == tier))
                    .Concat(Game1.Instance.Shields.Where(x => x.Tier == tier))
                    .ToList();

                if (nextTierAbilityItems.Count > 0)
                {
                    bagTexture = Art.LootBagGold;
                    items.Add(nextTierAbilityItems[rand.Next(nextTierAbilityItems.Count)]);
                }
            }

            // Drop stat potion.
            if (rand.Next(15) == 0)
            {
                bagTexture = Art.LootBagBlue;
                int next = rand.Next(8);
                Potions potion = Potions.Health;
                switch (next)
                {
                    case 0:
                        potion = Potions.Attack;
                        break;
                    case 1:
                        potion = Potions.Defense;
                        break;
                    case 2:
                        potion = Potions.Dexterity;
                        break;
                    case 3:
                        potion = Potions.Life;
                        break;
                    case 4:
                        potion = Potions.ManaMax;
                        break;
                    case 5:
                        potion = Potions.Speed;
                        break;
                    case 6:
                        potion = Potions.Vitality;
                        break;
                    case 7:
                        potion = Potions.Wisdom;
                        break;
                }
                items.Add(new Potion(potion));
            }

            if (rand.Next(10) == 0)
            {
                if (rand.Next(2) == 0)
                    items.Add(new Potion(Potions.Mana));
                else
                    items.Add(new Potion(Potions.Health));
            }

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

        // Boss drops — same tier-selection logic as Spawn() above for each
        // category, but without the drop-chance rolls: every category that
        // has any reachable tier available always contributes an item (a
        // graceful no-op only if the player is already at the catalog's max
        // tier for that category), plus always one random stat potion.
        // Single bag, in the same "premium" gold color Spawn() uses for
        // ability-item drops.
        public static void SpawnGuaranteedLoot(Vector2 pos, int pointValue = 0)
        {
            List<Item> items = [];
            int maxTierJump = MaxTierJump(pointValue);

            List<Weapon> nextTierWeapons = ItemsAtBestAvailableTier(
                Game1.Instance.Weapons,
                x => x.Tier,
                Player.Instance.Weapon.Tier,
                RollTierOffset(maxTierJump)
            );
            if (nextTierWeapons.Count > 0)
                items.Add(nextTierWeapons[rand.Next(nextTierWeapons.Count)]);

            List<Armor> nextTierArmors = ItemsAtBestAvailableTier(
                Game1.Instance.Armors,
                x => x.Tier,
                Player.Instance.Armor.Tier,
                RollTierOffset(maxTierJump)
            );
            if (nextTierArmors.Count > 0)
                items.Add(nextTierArmors[rand.Next(nextTierArmors.Count)]);

            List<Ring> nextTierRings = ItemsAtBestAvailableTier(
                Game1.Instance.Rings,
                x => x.Tier,
                Player.Instance.Ring.Tier,
                RollTierOffset(maxTierJump)
            );
            if (nextTierRings.Count > 0)
                items.Add(nextTierRings[rand.Next(nextTierRings.Count)]);

            List<AbilityItem> allAbilityItems = Game1
                .Instance.Spells.Cast<AbilityItem>()
                .Concat(Game1.Instance.Quivers)
                .Concat(Game1.Instance.Shields)
                .ToList();
            List<AbilityItem> nextTierAbilityItems = ItemsAtBestAvailableTier(
                allAbilityItems,
                x => x.Tier,
                Player.Instance.AbilityItem.Tier,
                RollTierOffset(maxTierJump)
            );
            if (nextTierAbilityItems.Count > 0)
                items.Add(nextTierAbilityItems[rand.Next(nextTierAbilityItems.Count)]);

            int next = rand.Next(8);
            Potions potion = next switch
            {
                0 => Potions.Attack,
                1 => Potions.Defense,
                2 => Potions.Dexterity,
                3 => Potions.Life,
                4 => Potions.ManaMax,
                5 => Potions.Speed,
                6 => Potions.Vitality,
                _ => Potions.Wisdom,
            };
            items.Add(new Potion(potion));

            LootBag bag = new()
            {
                Position = pos,
                Items = items,
                image = Art.LootBagGold,
            };

            EntityManager.Add(bag);
            LootBags.Add(bag);

            Sound.Play(Sound.LootAppears, 0.4f);
        }
    }
}
