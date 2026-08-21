using Microsoft.Xna.Framework.Graphics;

namespace Realm.Data
{
    public class WeaponData
    {
        public Weapon.WeaponType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tier { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public string ImageName { get; set; }
        public string ProjectileImageName { get; set; }

        // Staff-specific — the perpendicular sine-wave offset applied on
        // top of a shot's straight-line path (see SineWaveProjectile.cs).
        // 0 for every non-Staff weapon type's JSON entries, which is
        // exactly the "no wave, straight line" case those types already
        // want, so no separate flag is needed to opt out.
        public float Amplitude { get; set; }
        public float Frequency { get; set; }
    }
}
