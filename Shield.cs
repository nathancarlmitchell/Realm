using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Shield : AbilityItem
    {
        // Shield Slam's shot fan — see Data/ShieldData.cs.
        public int Shots { get; set; }
        public float ArcGapDegrees { get; set; }

        public Shield(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Required by System.Text.Json to deserialize a saved Shield (only
        // ever reached for the PlayerData.Shield DTO, never treated as a real
        // equipped item — LoadShield always constructs a fresh one via the
        // constructor above) — without this, JsonSerializer tries to bind the
        // Texture2D-taking constructor's parameter to a JSON property and
        // throws, same reason Weapon/Armor/Ring/Potion/Spell/Quiver each keep
        // one.
        public Shield() { }

        public static Shield LoadShield(string shieldName)
        {
            Shield shieldData = Game1.Instance.Shields.FirstOrDefault(x => (x.Name == shieldName));

            // shieldData is null if shieldName doesn't match anything in
            // ShieldData.json. A class mismatch (only a Knight can equip a
            // Shield) is also rejected here, same as Spell/Quiver.
            if (shieldData != null && Player.Instance.CanEquipAbilityItem(shieldData))
            {
                Texture2D shieldTexture = Game1.Instance.Content.Load<Texture2D>(
                    shieldData.ImageName
                );

                Shield shield = new(shieldTexture)
                {
                    Name = shieldData.Name,
                    Description = shieldData.Description,
                    Tier = shieldData.Tier,
                    MaxHealthBonus = shieldData.MaxHealthBonus,
                    MaxManaBonus = shieldData.MaxManaBonus,
                    AttackBonus = shieldData.AttackBonus,
                    DefenseBonus = shieldData.DefenseBonus,
                    SpeedBonus = shieldData.SpeedBonus,
                    DexterityBonus = shieldData.DexterityBonus,
                    VitalityBonus = shieldData.VitalityBonus,
                    WisdomBonus = shieldData.WisdomBonus,
                    ManaCost = shieldData.ManaCost,
                    MinDamage = shieldData.MinDamage,
                    MaxDamage = shieldData.MaxDamage,
                    ImageName = shieldData.ImageName,
                    Shots = shieldData.Shots,
                    ArcGapDegrees = shieldData.ArcGapDegrees,
                };

                Player.Instance.EquipAbilityItem(shield);
                return shield;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }
    }
}
