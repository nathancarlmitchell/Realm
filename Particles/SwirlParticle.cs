using System;
using Microsoft.Xna.Framework;

namespace Realm.Particles
{
    // A second particle "flavor," alongside Particle.cs's straight-line
    // scatter-and-fade burst — this one moves in polar coordinates around a
    // tracked center point instead of a fixed velocity, so it reads as a
    // swirl rather than an explosion: starts in a dense cluster near the
    // center (small startRadius) and spirals outward (radius grows every
    // tick) while orbiting (angle advances every tick, all particles in one
    // burst sharing the same spin direction so it reads as one coherent
    // swirl rather than a chaotic scatter). Adds a fast alpha/scale
    // "twinkle" oscillation on top of the overall lifespan fade for the
    // sparkle look. Same "ephemeral Entity managed by the normal
    // EntityManager pipeline" shape as Particle/DamageNumber — own lifespan
    // countdown, IsExpired when it runs out.
    public class SwirlParticle : Entity
    {
        private static readonly Random rand = new();

        // A delegate rather than a fixed spawn-time Vector2 — the swirl
        // tracks wherever the center currently is each frame (e.g. the
        // player, who might keep moving during the ~1s the effect plays),
        // not just where it was at the moment the burst was created.
        private readonly Func<Vector2> center;
        private readonly Color baseColor;
        private readonly int lifespanTicks;
        private readonly float startScale;
        private readonly float angularSpeed; // radians/tick; shared sign per burst
        private readonly float radiusGrowth; // units/tick
        private readonly float twinkleSpeed;
        private readonly float twinklePhase;

        private float angle;
        private float radius;
        private int ticksRemaining;

        public SwirlParticle(
            Func<Vector2> center,
            Color color,
            int lifespanTicks,
            float startScale,
            float startRadius,
            float angularSpeed,
            float radiusGrowth
        )
        {
            this.center = center;
            baseColor = color;
            this.lifespanTicks = lifespanTicks;
            this.startScale = startScale;
            this.angularSpeed = angularSpeed;
            this.radiusGrowth = radiusGrowth;
            ticksRemaining = lifespanTicks;

            angle = rand.NextFloat(0f, MathHelper.TwoPi);
            radius = startRadius;
            twinkleSpeed = rand.NextFloat(0.3f, 0.6f);
            twinklePhase = rand.NextFloat(0f, MathHelper.TwoPi);

            image = Art.Circle;
            drawScale = startScale;
            this.color = baseColor;
            Position = center() + Extensions.FromPolar(angle, radius);
        }

        public override void Update()
        {
            angle += angularSpeed;
            radius += radiusGrowth;
            Position = center() + Extensions.FromPolar(angle, radius);

            ticksRemaining--;
            float progress = MathHelper.Clamp(ticksRemaining / (float)lifespanTicks, 0f, 1f);

            // Fast oscillation layered on top of the overall fade — floored
            // at 40% of the base fade rather than 0 so a particle mid-dip
            // still reads as present, just dimmer, instead of fully
            // vanishing and reappearing every cycle.
            float elapsedTicks = lifespanTicks - ticksRemaining;
            float twinkle = 0.5f + 0.5f * (float)Math.Sin(twinklePhase + elapsedTicks * twinkleSpeed);

            color = baseColor * (progress * (0.4f + 0.6f * twinkle));
            drawScale = startScale * progress * (0.7f + 0.3f * twinkle);

            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        // Spawns `count` particles that swirl outward from `center` (a
        // delegate re-evaluated every frame, not a fixed point) toward
        // maxRadius over lifespanTicks, alternating between colorA/colorB
        // per particle for a two-tone sparkle rather than one flat color.
        public static void SpawnSwirl(
            Func<Vector2> center,
            Color colorA,
            Color colorB,
            int count,
            int lifespanTicks,
            float maxRadius,
            float startScale = 0.15f
        )
        {
            // One shared spin direction per burst — every particle rotating
            // the same way is what makes it read as a coherent swirl rather
            // than particles scattering past each other in both directions.
            float spinDirection = rand.Next(2) == 0 ? 1f : -1f;

            for (int i = 0; i < count; i++)
            {
                Color color = rand.Next(2) == 0 ? colorA : colorB;
                float angularSpeed = spinDirection * rand.NextFloat(0.08f, 0.16f);
                float radiusGrowth = (maxRadius / lifespanTicks) * rand.NextFloat(0.7f, 1.3f);

                // Small, near-zero start radius — the "dense cluster in the
                // center" the swirl expands outward from.
                float startRadius = rand.NextFloat(0f, 4f);

                EntityManager.Add(
                    new SwirlParticle(center, color, lifespanTicks, startScale, startRadius, angularSpeed, radiusGrowth)
                );
            }
        }
    }
}
