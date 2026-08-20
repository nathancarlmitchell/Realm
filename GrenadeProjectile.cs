using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // A stationary telegraphed-AoE hazard: spawns as a low-opacity grey
    // circle with no live hitbox (Radius 0, so nothing can collide with it
    // yet), then after fuseFrames "arms" — Radius jumps to the real
    // explosion radius and the circle turns red — giving the player a
    // brief window to see exactly where it'll hurt and step out before it
    // does. Drawn as a scaled Art.Circle instead of a sprite image so the
    // visual always matches the actual hitbox size, unlike a fixed-size
    // projectile sprite standing in for a much larger AoE.
    class GrenadeProjectile : EnemyProjectile
    {
        private readonly float armedRadius;
        private readonly int fuseFrames;
        private int elapsed = 0;
        private bool armed = false;

        private static readonly Color TelegraphColor = Color.Gray * 0.35f;
        private static readonly Color ArmedColor = Color.Red * 0.55f;

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
            Color drawColor = armed ? ArmedColor : TelegraphColor;
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
    }
}
