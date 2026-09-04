using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.Projectiles
{
    // A projectile whose sprite plays a real multi-frame animation instead
    // of a single static image — every other EnemyProjectile subclass
    // (WavyProjectile, BoomerangProjectile) only changes Position/Velocity
    // over time, never the sprite itself. First real use: Limon the Sprite
    // Goddess's own phase 3 "rainbow blast" (Art.RainbowBlast, a real
    // 4-frame animation) — the one shot in that fight dramatic enough
    // (armor-piercing, a single aimed hit rather than a swarm of plain
    // bolts) to earn genuinely new animated-projectile infrastructure
    // rather than reusing a static sprite.
    //
    // Collision/damage/duration all still come from the base
    // EnemyProjectile exactly as normal (Radius, HitBy tracking,
    // Damage/IgnoresDefense/etc.) — only Draw()/Update() differ, to drive
    // the animation clock and paint the current frame instead of the
    // inherited `image` field (left at whatever the constructor's
    // `image` param defaults to; unused once animation is set up).
    class AnimatedEnemyProjectile : EnemyProjectile
    {
        private readonly AnimatedTexture animation;

        // template: a shared, already-loaded AnimatedTexture (e.g.
        // Art.RainbowBlast) — Clone()'d so this instance gets its own
        // independent frame/elapsed clock, same "don't share one clock
        // across multiple owners" precedent Portal's own PortalArt()
        // already established (a second projectile spawned after the
        // first one already finished playing would otherwise start out
        // already-finished too, never actually animating).
        public AnimatedEnemyProjectile(
            Vector2 position,
            Vector2 velocity,
            AnimatedTexture template,
            CollisionShape? shape = null
        )
            // image left null — base falls back to Art.EnemyProjectile, a
            // real, harmless placeholder never actually drawn (Draw() below
            // is fully overridden and never reads `image`). Radius is
            // recomputed from the real animation frame size immediately
            // after, correcting the base constructor's own guess.
            : base(position, velocity, null, shape)
        {
            animation = template.Clone();
            Radius = animation.FrameWidth / 2f;
        }

        public override void Update()
        {
            // Runs before base.Update() moves Position/expires this
            // projectile — same "advance the animation every real tick"
            // cadence every other per-tick timer in this codebase uses,
            // fixed-timestep (1/60s) rather than a measured GameTime delta
            // since Entity.Update() has no GameTime parameter to measure
            // from (this engine assumes a steady 60 ticks/sec throughout).
            animation.UpdateFrame(1f / 60f);

            base.Update();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            animation.Rotation = Orientation;
            animation.DrawFrame(spriteBatch, Position);
        }
    }
}
