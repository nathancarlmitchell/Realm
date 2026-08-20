using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // A stationary telegraphed-AoE hazard: spawns as a grey circle with no
    // live hitbox (Radius 0, so nothing can collide with it yet), ramping
    // through 3 opacity stages as the fuse burns down, then "arms" —
    // Radius jumps to the real explosion radius and the circle turns red —
    // giving the player a brief window to see exactly where it'll hurt and
    // step out before it does. Drawn as a scaled Art.Circle instead of a
    // sprite image so the visual always matches the actual hitbox size,
    // unlike a fixed-size projectile sprite standing in for a much larger
    // AoE.
    class GrenadeProjectile : EnemyProjectile
    {
        private readonly float armedRadius;
        private readonly int fuseFrames;
        private int elapsed = 0;
        private bool armed = false;

        // 3 equal telegraph stages (each fuseFrames/3 long), opacity
        // ramping up as the fuse gets closer to arming — a rougher "how
        // much longer" cue than a smooth fade, but cheap and readable at a
        // glance.
        private const float TelegraphOpacityStage1 = 0.15f;
        private const float TelegraphOpacityStage2 = 0.35f;
        private const float TelegraphOpacityStage3 = 0.55f;

        // Once armed, opacity keeps cycling through these 3 stages
        // (repeating, unlike the one-shot telegraph ramp above) for as long
        // as the grenade stays alive — a pulsing "this is live" cue. Purely
        // visual: the hitbox (Radius) is identical across all 3 stages,
        // already set once in Update() when arming happens.
        private const float ArmedOpacityStage1 = 0.95f;
        private const float ArmedOpacityStage2 = 0.85f;
        private const float ArmedOpacityStage3 = 0.35f;
        private const int ArmedCycleStageLength = 10; // frames/stage, ~0.17s at 60fps

        public GrenadeProjectile(
            Vector2 position,
            float radius,
            int damage,
            int fuseFrames = 25,
            int duration = 90
        )
            : base(position, Vector2.Zero, Art.Circle)
        {
            armedRadius = radius;
            this.fuseFrames = fuseFrames;
            Damage = damage;
            this.duration = duration;

            // Inert until armed — see Update() below. The full-size circle
            // still draws immediately (Draw() always uses armedRadius), so
            // the telegraph shows the real danger zone from frame one; only
            // the actual collision radius starts at zero.
            Radius = 0f;

            // Stays alive (hitbox and all) for its whole duration instead
            // of vanishing the instant it first touches the player — a
            // lingering AoE hazard, not a one-shot bullet. EnemyProjectile's
            // new HasHitPlayer tracking (see EntityManager.HandleCollisions())
            // still caps it to damaging the player exactly once.
            ExpiresOnHit = false;
        }

        public override void Update()
        {
            base.Update();

            if (!armed && elapsed >= fuseFrames)
            {
                armed = true;
                Radius = armedRadius;
            }

            elapsed++;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Color drawColor = armed
                ? Color.Red * CurrentArmedOpacity()
                : Color.Gray * CurrentTelegraphOpacity();
            float scale = (armedRadius * 2f) / Art.Circle.Width;

            spriteBatch.Draw(
                Art.Circle,
                Position,
                null,
                drawColor,
                0f,
                new Vector2(Art.Circle.Width / 2f, Art.Circle.Height / 2f),
                scale,
                SpriteEffects.None,
                0f
            );
        }

        // Which of the 3 equal-length telegraph stages (fuseFrames/3 each)
        // elapsed currently falls in. Only called pre-arming, so the last
        // stage covers everything up to fuseFrames (including any leftover
        // frames from the integer division).
        private float CurrentTelegraphOpacity()
        {
            int stageLength = fuseFrames / 3;

            if (elapsed < stageLength)
                return TelegraphOpacityStage1;
            if (elapsed < stageLength * 2)
                return TelegraphOpacityStage2;
            return TelegraphOpacityStage3;
        }

        // Which of the 3 armed-cycle stages elapsed currently falls in,
        // repeating every 3 * ArmedCycleStageLength frames for as long as
        // the grenade stays armed. "elapsed - fuseFrames" is frames since
        // arming (0 on the exact frame it arms), only ever called while
        // armed is true.
        private float CurrentArmedOpacity()
        {
            int cycleStage = ((elapsed - fuseFrames) / ArmedCycleStageLength) % 3;

            return cycleStage switch
            {
                0 => ArmedOpacityStage1,
                1 => ArmedOpacityStage2,
                _ => ArmedOpacityStage3,
            };
        }
    }
}
