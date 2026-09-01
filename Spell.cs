using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Spell : AbilityItem
    {
        // Spell Bomb's own shot — see Data/SpellData.cs.
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public Texture2D ProjectileImage;
        public string ProjectileImageName { get; set; }

        // Damage-scales-with-Wisdom stat — see Data/SpellData.cs.
        public float DamagePerWisOver42 { get; set; }

        public Spell(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Required by System.Text.Json to deserialize a saved Spell (only
        // ever reached for the PlayerData.Spell DTO, never treated as a real
        // equipped item — LoadSpell always constructs a fresh one via the
        // constructor above) — without this, JsonSerializer tries to bind the
        // Texture2D-taking constructor's parameter to a JSON property and
        // throws, same reason Weapon/Armor/Ring/Potion each keep one.
        public Spell() { }

        public static Spell LoadSpell(string spellName)
        {
            Spell spellData = Game1.Instance.Spells.FirstOrDefault(x => (x.Name == spellName));

            // spellData is null if spellName doesn't match anything in
            // SpellData.json. A class mismatch (only a Wizard can equip a
            // Spell) is also rejected here, same as Armor.LoadArmor does for
            // ArmorType.
            if (spellData != null && Player.Instance.CanEquipAbilityItem(spellData))
            {
                Texture2D spellTexture = Game1.Instance.Content.Load<Texture2D>(
                    spellData.ImageName
                );
                Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                    spellData.ProjectileImageName
                );

                Spell spell = new(spellTexture)
                {
                    Name = spellData.Name,
                    Description = spellData.Description,
                    Tier = spellData.Tier,
                    MaxHealthBonus = spellData.MaxHealthBonus,
                    MaxManaBonus = spellData.MaxManaBonus,
                    AttackBonus = spellData.AttackBonus,
                    DefenseBonus = spellData.DefenseBonus,
                    SpeedBonus = spellData.SpeedBonus,
                    DexterityBonus = spellData.DexterityBonus,
                    VitalityBonus = spellData.VitalityBonus,
                    WisdomBonus = spellData.WisdomBonus,
                    ManaCost = spellData.ManaCost,
                    MinDamage = spellData.MinDamage,
                    MaxDamage = spellData.MaxDamage,
                    ImageName = spellData.ImageName,
                    ProjectileMagnitude = spellData.ProjectileMagnitude,
                    ProjectileDuration = spellData.ProjectileDuration,
                    ProjectileImage = projectileTexture,
                    ProjectileImageName = spellData.ProjectileImageName,
                    DamagePerWisOver42 = spellData.DamagePerWisOver42,
                    XpBonusPercent = spellData.XpBonusPercent,
                };

                Player.Instance.EquipAbilityItem(spell);
                return spell;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }
    }
}
