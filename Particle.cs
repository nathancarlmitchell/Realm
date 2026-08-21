using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // The engine's first visual-effects primitive — a single small dot that
    // scatters outward, shrinks, and fades over a short lifespan. Same
    // "ephemeral Entity managed by the normal EntityManager pipeline"
    // pattern DamageNumber already established (own lifespan countdown,
    // IsExpired when it runs out, no separate update/draw pass needed).
    // Draws via the base Entity.Draw() unmodified — image/color/drawScale
    // are all it needs, so there's no reason to override Draw() the way
    // DamageNumber does for its text.
    public class Particle : Entity
    {
        private static readonly Random rand = new();

        private Vector2 velocity;
        private readonly Color baseColor;
        private readonly int lifespanTicks;
        private readonly float startScale;
        private int ticksRemaining;

        // Friction applied to velocity each tick so a burst decelerates
        // outward instead of flying off in straight lines forever — makes
        // it read as a "burst" rather than a spray.
        private const float Drag = 0.9f;

        public Particle(Vector2 position, Vector2 velocity, Color color, int lifespanTicks, float startScale)
        {
            Position = position;
            this.velocity = velocity;
            baseColor = color;
            this.lifespanTicks = lifespanTicks;
            ticksRemaining = lifespanTicks;
            this.startScale = startScale;

            image = Art.Circle;
            drawScale = startScale;
            this.color = baseColor;
        }

        public override void Update()
        {
            Position += velocity;
            velocity *= Drag;
            ticksRemaining--;

            // 1 at spawn -> 0 at expiry, driving both fade and shrink
            // together so a particle visually shrinks as it fades rather
            // than popping out at full size.
            float progress = MathHelper.Clamp(ticksRemaining / (float)lifespanTicks, 0f, 1f);
            color = baseColor * progress;
            drawScale = startScale * progress;

            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        // Spawns a radial burst of `count` particles around position, each
        // with its own randomized direction and speed (minSpeed-maxSpeed)
        // so a burst doesn't read as a uniform ring — the one entry point
        // every effect call site (Enemy.WasShot()'s hit/death particles,
        // and any future one) should use rather than constructing Particle
        // directly.
        public static void SpawnBurst(
            Vector2 position,
            Color color,
            int count,
            float minSpeed,
            float maxSpeed,
            int lifespanTicks,
            float startScale = 0.15f
        )
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = rand.NextVector2(minSpeed, maxSpeed);
                EntityManager.Add(new Particle(position, velocity, color, lifespanTicks, startScale));
            }
        }
    }
}
