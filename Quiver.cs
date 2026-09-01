using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Quiver : AbilityItem
    {
        // The Quiver ability's own shot — see Data/QuiverData.cs.
        public int Shots { get; set; }
        public float ArcGapDegrees { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public Texture2D ProjectileImage;
        public string ProjectileImageName { get; set; }

        // Damage-scales-with-Wisdom stat — see Data/QuiverData.cs.
        public float DamagePerWisOver34 { get; set; }

        public Quiver(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Required by System.Text.Json to deserialize a saved Quiver (only
        // ever reached for the PlayerData.Quiver DTO, never treated as a real
        // equipped item — LoadQuiver always constructs a fresh one via the
        // constructor above) — without this, JsonSerializer tries to bind the
        // Texture2D-taking constructor's parameter to a JSON property and
        // throws, same reason Weapon/Armor/Ring/Potion each keep one.
        public Quiver() { }

        public static Quiver LoadQuiver(string quiverName)
        {
            Quiver quiverData = Game1.Instance.Quivers.FirstOrDefault(x => (x.Name == quiverName));

            // quiverData is null if quiverName doesn't match anything in
            // QuiverData.json. A class mismatch (only an Archer can equip a
            // Quiver) is also rejected here, same as Armor.LoadArmor does for
            // ArmorType.
            if (quiverData != null && Player.Instance.CanEquipAbilityItem(quiverData))
            {
                Texture2D quiverTexture = Game1.Instance.Content.Load<Texture2D>(
                    quiverData.ImageName
                );
                Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                    quiverData.ProjectileImageName
                );

                Quiver quiver = new(quiverTexture)
                {
                    Name = quiverData.Name,
                    Description = quiverData.Description,
                    Tier = quiverData.Tier,
                    MaxHealthBonus = quiverData.MaxHealthBonus,
                    MaxManaBonus = quiverData.MaxManaBonus,
                    AttackBonus = quiverData.AttackBonus,
                    DefenseBonus = quiverData.DefenseBonus,
                    SpeedBonus = quiverData.SpeedBonus,
                    DexterityBonus = quiverData.DexterityBonus,
                    VitalityBonus = quiverData.VitalityBonus,
                    WisdomBonus = quiverData.WisdomBonus,
                    ManaCost = quiverData.ManaCost,
                    MinDamage = quiverData.MinDamage,
                    MaxDamage = quiverData.MaxDamage,
                    ImageName = quiverData.ImageName,
                    Shots = quiverData.Shots,
                    ArcGapDegrees = quiverData.ArcGapDegrees,
                    ProjectileMagnitude = quiverData.ProjectileMagnitude,
                    ProjectileDuration = quiverData.ProjectileDuration,
                    ProjectileImage = projectileTexture,
                    ProjectileImageName = quiverData.ProjectileImageName,
                    XpBonusPercent = quiverData.XpBonusPercent,
                    DamagePerWisOver34 = quiverData.DamagePerWisOver34,
                };

                Player.Instance.EquipAbilityItem(quiver);
                return quiver;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }
    }
}
