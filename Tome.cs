using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Tome : AbilityItem
    {
        // Tome's own ability fields — see Data/TomeData.cs.
        public float Range { get; set; }
        public int HealAmount { get; set; }
        public float HealingAmountPerSecond { get; set; }
        public float HealingDurationSeconds { get; set; }

        public Tome(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Required by System.Text.Json to deserialize a saved Tome (only
        // ever reached for the PlayerData.Tome DTO, never treated as a real
        // equipped item — LoadTome always constructs a fresh one via the
        // constructor above) — without this, JsonSerializer tries to bind
        // the Texture2D-taking constructor's parameter to a JSON property
        // and throws, same reason Weapon/Armor/Ring/Potion/Spell/Quiver/
        // Shield each keep one.
        public Tome() { }

        public static Tome LoadTome(string tomeName)
        {
            Tome tomeData = Game1.Instance.Tomes.FirstOrDefault(x => (x.Name == tomeName));

            // tomeData is null if tomeName doesn't match anything in
            // TomeData.json. A class mismatch (only a Priest can equip a
            // Tome) is also rejected here, same as Spell/Quiver/Shield.
            if (tomeData != null && Player.Instance.CanEquipAbilityItem(tomeData))
            {
                Texture2D tomeTexture = Game1.Instance.Content.Load<Texture2D>(tomeData.ImageName);

                Tome tome = new(tomeTexture)
                {
                    Name = tomeData.Name,
                    Description = tomeData.Description,
                    Tier = tomeData.Tier,
                    MaxHealthBonus = tomeData.MaxHealthBonus,
                    MaxManaBonus = tomeData.MaxManaBonus,
                    AttackBonus = tomeData.AttackBonus,
                    DefenseBonus = tomeData.DefenseBonus,
                    SpeedBonus = tomeData.SpeedBonus,
                    DexterityBonus = tomeData.DexterityBonus,
                    VitalityBonus = tomeData.VitalityBonus,
                    WisdomBonus = tomeData.WisdomBonus,
                    ManaCost = tomeData.ManaCost,
                    MinDamage = tomeData.MinDamage,
                    MaxDamage = tomeData.MaxDamage,
                    ImageName = tomeData.ImageName,
                    XpBonusPercent = tomeData.XpBonusPercent,
                    Range = tomeData.Range,
                    HealAmount = tomeData.HealAmount,
                    HealingAmountPerSecond = tomeData.HealingAmountPerSecond,
                    HealingDurationSeconds = tomeData.HealingDurationSeconds,
                };

                Player.Instance.EquipAbilityItem(tome);
                return tome;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }
    }
}
