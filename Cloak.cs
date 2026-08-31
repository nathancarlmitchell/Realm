using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // Rogue's ability item — https://www.realmeye.com/wiki/cloaks. Unlike
    // Spell/Quiver/Shield/Tome, Cloak has no direct damage roll of its own
    // (MinDamage/MaxDamage, inherited from AbilityItem, stay 0 — see
    // AbilityItem.AbilitySummary()'s own "skip the Damage line when both are
    // zero" handling) — its "ability" is Rogue.UseAbility()'s Invisibility,
    // not a projectile burst.
    public class Cloak : AbilityItem
    {
        // Cloak's own ability fields — see Data/CloakData.cs.
        public int InvisibilityDurationFrames { get; set; }
        public float BaseFlatDamage { get; set; }
        public float FlatDamagePerWisOver34 { get; set; }
        public float BasePercentDamage { get; set; }
        public float PercentDamagePerWisOver34 { get; set; }

        public Cloak(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Required by System.Text.Json to deserialize a saved Cloak (only
        // ever reached for the PlayerData.Cloak DTO, never treated as a real
        // equipped item — LoadCloak always constructs a fresh one via the
        // constructor above) — without this, JsonSerializer tries to bind
        // the Texture2D-taking constructor's parameter to a JSON property
        // and throws, same reason Weapon/Armor/Ring/Potion/Spell/Quiver/
        // Shield/Tome each keep one.
        public Cloak() { }

        public static Cloak LoadCloak(string cloakName)
        {
            Cloak cloakData = Game1.Instance.Cloaks.FirstOrDefault(x => (x.Name == cloakName));

            // cloakData is null if cloakName doesn't match anything in
            // CloakData.json. A class mismatch (only a Rogue can equip a
            // Cloak) is also rejected here, same as Spell/Quiver/Shield/Tome.
            if (cloakData != null && Player.Instance.CanEquipAbilityItem(cloakData))
            {
                Texture2D cloakTexture = Game1.Instance.Content.Load<Texture2D>(
                    cloakData.ImageName
                );

                Cloak cloak = new(cloakTexture)
                {
                    Name = cloakData.Name,
                    Description = cloakData.Description,
                    Tier = cloakData.Tier,
                    MaxHealthBonus = cloakData.MaxHealthBonus,
                    MaxManaBonus = cloakData.MaxManaBonus,
                    AttackBonus = cloakData.AttackBonus,
                    DefenseBonus = cloakData.DefenseBonus,
                    SpeedBonus = cloakData.SpeedBonus,
                    DexterityBonus = cloakData.DexterityBonus,
                    VitalityBonus = cloakData.VitalityBonus,
                    WisdomBonus = cloakData.WisdomBonus,
                    ManaCost = cloakData.ManaCost,
                    ImageName = cloakData.ImageName,
                    XpBonusPercent = cloakData.XpBonusPercent,
                    InvisibilityDurationFrames = cloakData.InvisibilityDurationFrames,
                    BaseFlatDamage = cloakData.BaseFlatDamage,
                    FlatDamagePerWisOver34 = cloakData.FlatDamagePerWisOver34,
                    BasePercentDamage = cloakData.BasePercentDamage,
                    PercentDamagePerWisOver34 = cloakData.PercentDamagePerWisOver34,
                };

                Player.Instance.EquipAbilityItem(cloak);
                return cloak;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }

        // Lethal Strike's damage bonus (see Weapon.Shoot()'s own
        // HasDebuff(LethalStrike) check) — flat + percent-of-this-shot's-
        // own-damage, each with a further bonus scaling off Wisdom past 34,
        // per this Cloak's own tier (the wiki's "Comparative Cloaks Table").
        // The real game's percent component scales off the *target's*
        // Defense instead — not modeled here, since the projectile
        // architecture doesn't know which enemy it'll hit at fire-time
        // (Defense is only known at the point of collision).
        public int ComputeLethalStrikeBonus(int baseDamage, int wisdom)
        {
            float wisOver34 = Math.Max(0, wisdom - 34);
            float flat = BaseFlatDamage + FlatDamagePerWisOver34 * wisOver34;
            float percent = (BasePercentDamage + PercentDamagePerWisOver34 * wisOver34) / 100f;
            return (int)(flat + baseDamage * percent);
        }
    }
}
