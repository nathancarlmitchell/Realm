using Microsoft.Xna.Framework.Graphics;

namespace Realm.Data
{
    public class WeaponData
    {
        public Weapon.WeaponType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Teir { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public string ImageName { get; set; }
        public string ProjectileImageName { get; set; }
    }
}
