using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // A third particle "flavor," alongside Particle.cs's decelerating
    // scatter-and-fade burst and SwirlParticle.cs's orbiting spiral — this
    // one rises straight upward at a steady, undecaying speed (no Drag,
    // unlike Particle) while only fading, never shrinking, so a burst reads
    // as buoyant motes drifting off rather than an explosion. Used by
    // Priest's self-heal (see CharacterClasses/Priest.cs's UseAbility()) —
    // a small side-to-side sway is layered on top of the rise so a burst of
    // these doesn't look like a rigid column of dots. Same "ephemeral
    // Entity managed by the normal EntityManager pipeline" shape as
    // Particle/SwirlParticle — own lifespan countdown, IsExpired when it
    // runs out.
    public class RisingParticle : Entity
    {
        private static readonly Random rand = new();

        private readonly float baseX;
        private readonly float riseSpeed;
        private readonly Color baseColor;
        private readonly int lifespanTicks;
        private readonly float swaySpeed;
        private readonly float swayAmount;
        private readonly float swayPhase;
        private int ticksRemaining;

        public RisingParticle(Vector2 position, Color color, int lifespanTicks, float scale, float riseSpeed)
        {
            Position = position;
            baseX = position.X;
            this.riseSpeed = riseSpeed;
            baseColor = color;
            this.lifespanTicks = lifespanTicks;
            ticksRemaining = lifespanTicks;

            swaySpeed = rand.NextFloat(0.05f, 0.1f);
            swayAmount = rand.NextFloat(1f, 3f);
            swayPhase = rand.NextFloat(0f, MathHelper.TwoPi);

            image = Art.Circle;
            drawScale = scale;
            this.color = baseColor;
        }

        public override void Update()
        {
            ticksRemaining--;
            int elapsedTicks = lifespanTicks - ticksRemaining;

            float x = baseX + swayAmount * (float)Math.Sin(swayPhase + elapsedTicks * swaySpeed);
            Position = new Vector2(x, Position.Y - riseSpeed);

            // Fade only -- drawScale is left at its spawn value throughout,
            // unlike Particle's fade-and-shrink-together treatment.
            float progress = MathHelper.Clamp(ticksRemaining / (float)lifespanTicks, 0f, 1f);
            color = baseColor * progress;

            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        // Spawns `count` motes at randomized points along a short horizontal
        // spread centered on `origin` (e.g. Priest.Position's feet, see
        // UseAbility()) so they don't all rise in a single-file line.
        public static void SpawnRisingBurst(
            Vector2 origin,
            Color color,
            int count,
            int lifespanTicks,
            float scale = 0.12f,
            float riseSpeed = 0.6f,
            float spawnSpread = 6f
        )
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 position = origin + new Vector2(rand.NextFloat(-spawnSpread, spawnSpread), 0);
                float speed = riseSpeed * rand.NextFloat(0.8f, 1.2f);
                EntityManager.Add(new RisingParticle(position, color, lifespanTicks, scale, speed));
            }
        }
    }
}
