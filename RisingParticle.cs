using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // A third particle "flavor," alongside Particle.cs's decelerating
    // scatter-and-fade burst and SwirlParticle.cs's orbiting spiral — this
    // one rises straight upward at a steady, undecaying speed (no Drag,
    // unlike Particle) while only fading, never shrinking, so a burst reads
    // as buoyant motes drifting off rather than an explosion. Used by
    // Priest's self-heal (see CharacterClasses/Priest.cs's Update()
    // override, which spawns a small clump of these every few ticks for as
    // long as the Healing HoT is active) — a small side-to-side sway is
    // layered on top of the rise so a clump of these doesn't look like a
    // rigid column of dots. Same "ephemeral Entity managed by the normal
    // EntityManager pipeline" shape as Particle/SwirlParticle — own
    // lifespan countdown, IsExpired when it runs out.
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

        // Spawns `count` motes across `spawnWidth` (e.g. the player's own
        // Size.X, see Priest.cs's Update() override) centered on `origin`.
        // Stratified rather than fully independent per-particle randomness —
        // `spawnWidth` is divided into `count` equal slices and each
        // particle picks a random point within its own slice — so a small
        // clump (3 particles per call) reliably spreads evenly across the
        // width instead of occasionally clustering to one side the way pure
        // per-particle randomness could. A small random vertical jitter is
        // layered on top (spawnHeightJitter) so a clump doesn't spawn along
        // one perfectly flat line either. Called repeatedly in small clumps
        // for as long as a healing effect is active, rather than once as a
        // single big burst — small `count`/`scale` values are the expected
        // inputs.
        public static void SpawnRisingBurst(
            Vector2 origin,
            Color color,
            int count,
            int lifespanTicks,
            float scale = 0.06f,
            float riseSpeed = 1.2f,
            float spawnWidth = 12f,
            float spawnHeightJitter = 4f
        )
        {
            float sliceWidth = spawnWidth / count;
            for (int i = 0; i < count; i++)
            {
                float sliceStart = -spawnWidth / 2f + i * sliceWidth;
                float x = sliceStart + rand.NextFloat(0f, sliceWidth);
                float y = rand.NextFloat(-spawnHeightJitter, spawnHeightJitter);
                Vector2 position = origin + new Vector2(x, y);
                float speed = riseSpeed * rand.NextFloat(0.8f, 1.2f);
                EntityManager.Add(new RisingParticle(position, color, lifespanTicks, scale, speed));
            }
        }
    }
}
