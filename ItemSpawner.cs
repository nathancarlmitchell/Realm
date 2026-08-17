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

        public static void Spawn(Vector2 pos)
        {
            List<Item> items = [];
            Texture2D bagTexture = Art.LootBag;

            // Drop weapon.
            if (rand.Next(15) == 0)
            {
                // Drop the next highest tier — picked at random among every
                // catalog entry at that tier (both WeaponTypes), not just the
                // first match. WeaponData.json lists every Wand before any
                // Bow, so FirstOrDefault would always resolve to a Wand
                // regardless of the player's actual class.
                List<Weapon> nextTierWeapons = Game1
                    .Instance.Weapons.Where(x => x.Tier == Player.Instance.Weapon.Tier + 1)
                    .ToList();

                if (nextTierWeapons.Count > 0)
                {
                    bagTexture = Art.LootBagPink;
                    items.Add(nextTierWeapons[rand.Next(nextTierWeapons.Count)]);
                }
            }

            // Drop armor.
            if (rand.Next(15) == 0)
            {
                // Same reasoning as the weapon drop above — ArmorData.json
                // lists every Robe before any Leather piece.
                List<Armor> nextTierArmors = Game1
                    .Instance.Armors.Where(x => x.Tier == Player.Instance.Armor.Tier + 1)
                    .ToList();

                if (nextTierArmors.Count > 0)
                {
                    bagTexture = Art.LootBagPurple;
                    items.Add(nextTierArmors[rand.Next(nextTierArmors.Count)]);
                }
            }

            // Drop ring.
            if (rand.Next(15) == 0)
            {
                // Drop the next highest tier.
                if (Game1.Instance.Rings.Exists(x => (x.Tier == Player.Instance.Ring.Tier + 1)))
                {
                    bagTexture = Art.LootBagWhite;
                    Ring nextRing = Game1.Instance.Rings.FirstOrDefault(x =>
                        (x.Tier == Player.Instance.Ring.Tier + 1)
                    );
                    items.Add(nextRing);
                }
            }

            // Drop ability item.
            if (rand.Next(15) == 0)
            {
                // Same "wrong class is possible" spirit as weapon/armor drops
                // above — not filtered to the player's own class. Spell,
                // Quiver, and Shield are separate catalogs (not a single
                // shared list like Weapons/Armors), so concatenate all three
                // next-tier results before picking at random.
                List<AbilityItem> nextTierAbilityItems = Game1
                    .Instance.Spells.Where(x => x.Tier == Player.Instance.AbilityItem.Tier + 1)
                    .Cast<AbilityItem>()
                    .Concat(
                        Game1.Instance.Quivers.Where(x =>
                            x.Tier == Player.Instance.AbilityItem.Tier + 1
                        )
                    )
                    .Concat(
                        Game1.Instance.Shields.Where(x =>
                            x.Tier == Player.Instance.AbilityItem.Tier + 1
                        )
                    )
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

        // Boss drops — same next-tier-above-what's-equipped selection logic
        // as Spawn() above for each category, but without the 1-in-15 rolls:
        // every category that has a next tier available always contributes
        // an item (still a graceful no-op if the player's already at max
        // tier for that category), plus always one random stat potion.
        // Single bag, in the same "premium" gold color Spawn() uses for
        // ability-item drops.
        public static void SpawnGuaranteedLoot(Vector2 pos)
        {
            List<Item> items = [];

            List<Weapon> nextTierWeapons = Game1
                .Instance.Weapons.Where(x => x.Tier == Player.Instance.Weapon.Tier + 1)
                .ToList();
            if (nextTierWeapons.Count > 0)
                items.Add(nextTierWeapons[rand.Next(nextTierWeapons.Count)]);

            List<Armor> nextTierArmors = Game1
                .Instance.Armors.Where(x => x.Tier == Player.Instance.Armor.Tier + 1)
                .ToList();
            if (nextTierArmors.Count > 0)
                items.Add(nextTierArmors[rand.Next(nextTierArmors.Count)]);

            if (Game1.Instance.Rings.Exists(x => x.Tier == Player.Instance.Ring.Tier + 1))
            {
                Ring nextRing = Game1.Instance.Rings.FirstOrDefault(x =>
                    x.Tier == Player.Instance.Ring.Tier + 1
                );
                items.Add(nextRing);
            }

            List<AbilityItem> nextTierAbilityItems = Game1
                .Instance.Spells.Where(x => x.Tier == Player.Instance.AbilityItem.Tier + 1)
                .Cast<AbilityItem>()
                .Concat(
                    Game1.Instance.Quivers.Where(x =>
                        x.Tier == Player.Instance.AbilityItem.Tier + 1
                    )
                )
                .Concat(
                    Game1.Instance.Shields.Where(x =>
                        x.Tier == Player.Instance.AbilityItem.Tier + 1
                    )
                )
                .ToList();
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
