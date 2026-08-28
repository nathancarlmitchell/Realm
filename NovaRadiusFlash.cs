using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // A brief fading ring outline at an AoE cast's actual center/radius —
    // visual confirmation of the blast area at the moment it lands, spawned
    // once per cast (e.g. Priest.UseAbility()'s Nova). Separate from a
    // class's own continuous pre-cast aim preview (see Player.
    // DrawAbilityPreview()) — this is a one-shot effect, not a targeting
    // aid. A plain Entity (like DamageNumber/Particle) so it rides the
    // normal EntityManager Update()/Draw() pipeline for free.
    public class NovaRadiusFlash : Entity
    {
        private readonly float radius;
        private readonly Color baseColor;
        private readonly int lifespanTicks;
        private int ticksRemaining;

        public NovaRadiusFlash(Vector2 center, float radius, Color color, int lifespanTicks = 20)
        {
            Position = center;
            this.radius = radius;
            baseColor = color;
            this.lifespanTicks = lifespanTicks;
            ticksRemaining = lifespanTicks;
        }

        public override void Update()
        {
            ticksRemaining--;
            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float alpha = MathHelper.Clamp(ticksRemaining / (float)lifespanTicks, 0f, 1f);
            Util.DrawCircleOutline(spriteBatch, Position, radius, baseColor * alpha);
        }
    }
}
