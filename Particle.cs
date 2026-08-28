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

        // Scatters `count` particles at randomized points across a disc
        // around `center` (out to `radius`, plus a bit beyond it via
        // spawnRadiusMultiplier, for the "nearby" part) rather than a single
        // spawn point like SpawnBurst above — reads as debris/sparks thrown
        // across a blast area instead of one burst radiating from its exact
        // center. Each particle still moves further outward from wherever
        // it happened to spawn (not from `center`), using the same
        // velocity/drag/fade-and-shrink physics as a plain burst. Used by
        // Priest's damaging Nova (see CharacterClasses/Priest.cs's
        // UseAbility() and NovaPulse.cs).
        public static void SpawnAreaBurst(
            Vector2 center,
            float radius,
            Color color,
            int count,
            float minSpeed,
            float maxSpeed,
            int lifespanTicks,
            float startScale = 0.15f,
            float spawnRadiusMultiplier = 1.15f
        )
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Extensions.FromPolar(
                    rand.NextFloat(0f, MathHelper.TwoPi),
                    rand.NextFloat(0f, radius * spawnRadiusMultiplier)
                );
                Vector2 position = center + offset;

                // Fly outward from this particle's own spawn point, not from
                // `center` -- a spark that happened to spawn near the center
                // still needs some direction, so fall back to a fully
                // randomized one in that (rare, zero-offset) case.
                float speed = rand.NextFloat(minSpeed, maxSpeed);
                Vector2 velocity =
                    offset == Vector2.Zero ? rand.NextVector2(minSpeed, maxSpeed) : Vector2.Normalize(offset) * speed;

                EntityManager.Add(new Particle(position, velocity, color, lifespanTicks, startScale));
            }
        }
    }
}
