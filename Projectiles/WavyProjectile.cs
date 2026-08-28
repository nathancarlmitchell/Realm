using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.Projectiles
{
    // "Wavy shots" — Sand Devil's Dark Gray Spinner. Traces a real sine
    // wave around its straight-line path (offset perpendicular to the
    // original firing direction, growing with distance traveled) rather
    // than a plain straight shot. Position is computed directly each tick
    // from distanceTraveled instead of accumulating Velocity, so the curve
    // stays a clean sine wave regardless of framerate hiccups; Velocity is
    // zeroed before calling base.Update() so its own `Position += Velocity`
    // doesn't double-move this on top of that.
    class WavyProjectile : EnemyProjectile
    {
        private readonly Vector2 spawnPosition;
        private readonly Vector2 baseDirection;
        private readonly float speed;
        private float distanceTraveled = 0f;

        // Not specified anywhere in the spec ("Wavy shots" is just a
        // comment, no numbers) — tunable.
        private const float WaveAmplitude = 20f; // px either side of the straight path
        private const float WaveFrequency = 0.05f; // radians per px traveled

        public WavyProjectile(Vector2 position, Vector2 velocity, Texture2D image = null)
            : base(position, velocity, image)
        {
            spawnPosition = position;
            speed = velocity.Length();
            baseDirection = speed > 0 ? Vector2.Normalize(velocity) : Vector2.UnitX;
        }

        public override void Update()
        {
            distanceTraveled += speed;
            Vector2 perpendicular = new(-baseDirection.Y, baseDirection.X);
            Position =
                spawnPosition
                + baseDirection * distanceTraveled
                + perpendicular * (MathF.Sin(distanceTraveled * WaveFrequency) * WaveAmplitude);
            Orientation = baseDirection.ToAngle();

            Velocity = Vector2.Zero;
            base.Update();
        }
    }
}
