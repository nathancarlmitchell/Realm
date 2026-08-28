using System;
using Microsoft.Xna.Framework;

namespace Realm.Particles
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

        // One entry in a weighted color palette for SpawnRisingBurst's
        // multi-color overloads below — Chance values are relative weights,
        // not required to sum to 1 (see PickWeightedColor()), so e.g.
        // { (White, 3), (Gold, 1) } reads as "White three times as often as
        // Gold" without the caller needing to pre-normalize anything.
        public readonly struct ColorChance
        {
            public readonly Color Color;
            public readonly float Chance;

            public ColorChance(Color color, float chance)
            {
                Color = color;
                Chance = chance;
            }
        }

        private static Color PickWeightedColor(ColorChance[] colors)
        {
            float total = 0f;
            foreach (var c in colors)
                total += c.Chance;

            float roll = rand.NextFloat(0f, total);
            float cumulative = 0f;
            foreach (var c in colors)
            {
                cumulative += c.Chance;
                if (roll <= cumulative)
                    return c.Color;
            }

            // Only reached via float rounding landing the roll a hair past
            // the last cumulative boundary -- fall back to the last color
            // rather than leaving it unhandled.
            return colors[^1].Color;
        }

        // The real spawn loop every public overload below funnels into —
        // `pickColor` is called once per particle (not once for the whole
        // clump), which is what lets the multi-color overloads mix colors
        // within a single burst rather than every particle in it sharing
        // one color. See the individual overloads' own comments for what
        // each convenience wrapper is for.
        private static void SpawnRisingBurstCore(
            Func<Vector2> anchor,
            Func<Color> pickColor,
            int count,
            int lifespanTicks,
            float scale,
            float riseSpeed,
            float spawnWidth,
            float spawnHeightJitter,
            float minRiseSpeed = 1.0f,
            float maxRiseSpeed = 1.8f
        )
        {
            float sliceWidth = spawnWidth / count;
            for (int i = 0; i < count; i++)
            {
                float sliceStart = -spawnWidth / 2f + i * sliceWidth;
                float x = sliceStart + rand.NextFloat(0f, sliceWidth);
                float y = rand.NextFloat(-spawnHeightJitter, spawnHeightJitter);
                float speed = riseSpeed * rand.NextFloat(minRiseSpeed, maxRiseSpeed);
                EntityManager.Add(
                    new RisingParticle(
                        anchor,
                        new Vector2(x, y),
                        pickColor(),
                        lifespanTicks,
                        scale,
                        speed
                    )
                );
            }
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
        ) =>
            SpawnRisingBurstCore(
                anchor,
                () => color,
                count,
                lifespanTicks,
                scale,
                riseSpeed,
                spawnWidth,
                spawnHeightJitter
            );

        // Same as above, but for a one-off effect with nothing to track —
        // `origin` is captured once and never re-evaluated, unlike the
        // `Func<Vector2> anchor` overloads, which keep tracking a moving
        // point (e.g. the player) for as long as each particle lives.
        public static void SpawnRisingBurst(
            Vector2 origin,
            Color color,
            int count,
            int lifespanTicks,
            float scale = 0.06f,
            float riseSpeed = 1.2f,
            float spawnWidth = 12f,
            float spawnHeightJitter = 12f
        ) =>
            SpawnRisingBurstCore(
                () => origin,
                () => color,
                count,
                lifespanTicks,
                scale,
                riseSpeed,
                spawnWidth,
                spawnHeightJitter
            );

        // Same as the Func<Vector2> overload above, but each particle
        // independently rolls its own color from `colors` (see
        // ColorChance/PickWeightedColor above) instead of the whole clump
        // sharing one fixed color.
        public static void SpawnRisingBurst(
            Func<Vector2> anchor,
            ColorChance[] colors,
            int count,
            int lifespanTicks,
            float scale = 0.06f,
            float riseSpeed = 1.2f,
            float spawnWidth = 12f,
            float spawnHeightJitter = 12f
        ) =>
            SpawnRisingBurstCore(
                anchor,
                () => PickWeightedColor(colors),
                count,
                lifespanTicks,
                scale,
                riseSpeed,
                spawnWidth,
                spawnHeightJitter
            );

        // Same as the Vector2 origin overload above, but with a weighted
        // color palette instead of one fixed color -- see the two overloads
        // this combines for what each half is for.
        public static void SpawnRisingBurst(
            Vector2 origin,
            ColorChance[] colors,
            int count,
            int lifespanTicks,
            float scale = 0.06f,
            float riseSpeed = 1.2f,
            float spawnWidth = 12f,
            float spawnHeightJitter = 12f
        ) =>
            SpawnRisingBurstCore(
                () => origin,
                () => PickWeightedColor(colors),
                count,
                lifespanTicks,
                scale,
                riseSpeed,
                spawnWidth,
                spawnHeightJitter
            );
    }
}
