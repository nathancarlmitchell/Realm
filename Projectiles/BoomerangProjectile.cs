using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.Projectiles
{
    // "Shots boomerang" — Native Greater Magic Sprite/Native Sprite God's
    // Magic form (realmeye.com/wiki/sprite-world). Flies out normally for
    // ReturnAfterFrames, then reverses Velocity to fly straight back the way
    // it came — same "subclass EnemyProjectile, override Update()" shape as
    // WavyProjectile, just a velocity flip instead of a sine-wave offset.
    // Reverses only once (a second, later reversal would just be turning
    // itself back around mid-return, not a real gameplay concern the wiki
    // describes) — tracked via hasReversed rather than a frame-count check
    // each tick, so it still works correctly if duration runs out exactly
    // on the reversal frame.
    class BoomerangProjectile : EnemyProjectile
    {
        private readonly int returnAfterFrames;
        private int elapsed = 0;
        private bool hasReversed = false;

        public BoomerangProjectile(
            Vector2 position,
            Vector2 velocity,
            int returnAfterFrames,
            Texture2D image = null
        )
            : base(position, velocity, image)
        {
            this.returnAfterFrames = returnAfterFrames;
        }

        public override void Update()
        {
            if (!hasReversed && elapsed >= returnAfterFrames)
            {
                hasReversed = true;
                Velocity = -Velocity;
            }
            elapsed++;

            base.Update();
        }
    }
}
