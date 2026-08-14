using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class DamageNumber : Entity
    {
        private static readonly Random rand = new();

        private const int LifespanTicks = 40;
        private static readonly Vector2 FloatVelocity = new(0, -0.6f);

        private readonly string text;
        private readonly Color baseColor;
        private int ticksRemaining = LifespanTicks;

        public DamageNumber(Vector2 position, int damage, Color color)
        {
            // Small spawn jitter so simultaneous hits (a bow's multiple arrows,
            // an AoE ability) don't render as one illegible stack of digits.
            Position = position + new Vector2(rand.NextFloat(-10, 10), -20);
            text = damage.ToString();
            baseColor = color;
            this.color = color;
        }

        public override void Update()
        {
            Position += FloatVelocity;
            ticksRemaining--;

            float alpha = MathHelper.Clamp(ticksRemaining / (float)LifespanTicks, 0f, 1f);
            color = baseColor * alpha;

            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(Art.HudFont, text, Position, color);
        }
    }
}
