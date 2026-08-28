using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // A third particle "flavor," alongside Particle.cs's decelerating
    // scatter-and-fade burst and SwirlParticle.cs's orbiting spiral — this
    // one rises at a steady, undecaying speed (no Drag, unlike Particle)
    // relative to a tracked anchor point (e.g. the Priest, who might keep
    // moving while the HoT is active — see CharacterClasses/Priest.cs's
    // Update() override) rather than a fixed spawn-time position, so a
    // clump keeps pace with the player instead of being left behind in
    // world space. Same delegate-based tracking SwirlParticle.cs already
    // established for this exact reason. Only fades, never shrinks, so a
    // burst reads as buoyant motes drifting off rather than an explosion —
    // a small side-to-side sway is layered on top of the rise so a clump of
    // these doesn't look like a rigid column of dots. Same "ephemeral
    // Entity managed by the normal EntityManager pipeline" shape as
    // Particle/SwirlParticle — own lifespan countdown, IsExpired when it
    // runs out.
    public class RisingParticle : Entity
    {
        private static readonly Random rand = new();

        private readonly Func<Vector2> anchor;
        private readonly float spawnOffsetX;
        private readonly float spawnOffsetY;
        private readonly float riseSpeed;
        private readonly Color baseColor;
        private readonly int lifespanTicks;
        private readonly float swaySpeed;
        private readonly float swayAmount;
        private readonly float swayPhase;
        private float risenDistance;
        private int ticksRemaining;

        public RisingParticle(
            Func<Vector2> anchor,
            Vector2 spawnOffset,
            Color color,
            int lifespanTicks,
            float scale,
            float riseSpeed
        )
        {
            this.anchor = anchor;
            spawnOffsetX = spawnOffset.X;
            spawnOffsetY = spawnOffset.Y;
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
            Position = anchor() + spawnOffset;
        }

        public override void Update()
        {
            ticksRemaining--;
            int elapsedTicks = lifespanTicks - ticksRemaining;
            risenDistance += riseSpeed;

            float sway = swayAmount * (float)Math.Sin(swayPhase + elapsedTicks * swaySpeed);
            Position = anchor() + new Vector2(spawnOffsetX + sway, spawnOffsetY - risenDistance);

            // Fade only -- drawScale is left at its spawn value throughout,
            // unlike Particle's fade-and-shrink-together treatment.
            float progress = MathHelper.Clamp(ticksRemaining / (float)lifespanTicks, 0f, 1f);
            color = baseColor * progress;

            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        // Spawns `count` motes across `spawnWidth` (e.g. the player's own
        // Size.X, see Priest.cs's Update() override), each tracking
        // `anchor` (re-evaluated every frame, not a fixed spawn-time point)
        // plus its own small random offset. Stratified rather than fully
        // independent per-particle randomness — `spawnWidth` is divided
        // into `count` equal slices and each particle picks a random point
        // within its own slice — so a small clump (a few particles per
        // call) reliably spreads evenly across the width instead of
        // occasionally clustering to one side the way pure per-particle
        // randomness could. A small random vertical jitter is layered on
        // top (spawnHeightJitter) so a clump doesn't spawn along one
        // perfectly flat line either. Called repeatedly in small clumps for
        // as long as a healing effect is active, rather than once as a
        // single big burst — small `count`/`scale` values are the expected
        // inputs.
        public static void SpawnRisingBurst(
            Func<Vector2> anchor,
            Color color,
            int count,
            int lifespanTicks,
            float scale = 0.06f,
            float riseSpeed = 1.2f,
            float spawnWidth = 12f,
            float spawnHeightJitter = 12f
        )
        {
            float sliceWidth = spawnWidth / count;
            for (int i = 0; i < count; i++)
            {
                float sliceStart = -spawnWidth / 2f + i * sliceWidth;
                float x = sliceStart + rand.NextFloat(0f, sliceWidth);
                float y = rand.NextFloat(-spawnHeightJitter, spawnHeightJitter);
                float speed = riseSpeed * rand.NextFloat(0.8f, 1.2f);
                EntityManager.Add(new RisingParticle(anchor, new Vector2(x, y), color, lifespanTicks, scale, speed));
            }
        }
    }
}
