using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class WeaponData
    {
        public string Description { get; set; }
        public int Teir { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int FireRate { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public Texture2D ProjectileImage { get; set; }
    }
}
