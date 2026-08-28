using System;
using Microsoft.Xna.Framework;

namespace Realm.Projectiles
{
    // A Projectile that weaves in a sine wave perpendicular to its base
    // aim direction instead of flying in a straight line — currently only
    // used by Staff (see Weapon.cs's Shoot()). Position is recomputed
    // fresh each tick from total distance traveled along the aim line
    // plus a perpendicular sine offset, rather than accumulating Velocity
    // directly the way the base Projectile does — doing both at once
    // would double-count the forward motion.
    class SineWaveProjectile : Projectile
    {
        private readonly Vector2 origin;
        private readonly Vector2 forward; // unit vector along the base aim direction
        private readonly Vector2 perpendicular; // unit vector 90 degrees from forward
        private readonly float speed; // px/tick along forward
        private readonly float amplitude; // px, perpendicular offset at the wave's peak
        private readonly float frequency; // full cycles completed over the projectile's whole Duration
        private readonly float phaseOffset; // radians — lets a second shot weave opposite the first

        private float distanceTraveled = 0f;
        private int ticksElapsed = 0;

        public SineWaveProjectile(
            Vector2 position,
            float angle,
            float speed,
            float amplitude,
            float frequency,
            float phaseOffset
        )
            : base(position, Extensions.FromPolar(angle, speed))
        {
            origin = position;
            forward = Extensions.FromPolar(angle, 1f);
            perpendicular = Extensions.FromPolar(angle + MathHelper.PiOver2, 1f);
            this.speed = speed;
            this.amplitude = amplitude;
            this.frequency = frequency;
            this.phaseOffset = phaseOffset;

            // Orientation stays fixed along the base aim direction — the
            // wave moves the sprite's position, not its facing, so the
            // sprite doesn't visually wobble as it weaves.
            Orientation = angle;
        }

        public override void Update()
        {
            distanceTraveled += speed;
            ticksElapsed++;

            float progress = Duration > 0 ? ticksElapsed / (float)Duration : 0f;
            float wave = amplitude * (float)Math.Sin(MathHelper.TwoPi * frequency * progress + phaseOffset);

            Position = origin + forward * distanceTraveled + perpendicular * wave;

            if (ticksElapsed >= Duration)
                IsExpired = true;
        }
    }
}
