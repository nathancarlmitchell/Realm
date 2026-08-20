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
        private const float Scale = 1.0f;

        // Same offset/alpha as the title screen's own text backing (see
        // Overlay.DrawTitle()/GameOverState.Draw()) — a black copy drawn
        // behind and offset from the real text.
        private static readonly Vector2 BackingOffset = new(-4, 4);
        private const float BackingAlpha = 0.5f;

        private readonly string text;
        private readonly Color baseColor;
        private readonly bool hasBlackBacking;
        private int ticksRemaining = LifespanTicks;
        private float currentAlpha = 1f;

        // hasBlackBacking: only the player's own "I took damage" numbers
        // get the title-style backing (see Player.Hit()) — enemy hit
        // numbers (Enemy.WasShot()) are unaffected, matching the user's
        // request scoped to "the player damage number" specifically.
        public DamageNumber(Vector2 position, int damage, Color color, bool hasBlackBacking = false)
        {
            // Small spawn jitter so simultaneous hits (a bow's multiple arrows,
            // an AoE ability) don't render as one illegible stack of digits.
            Position = position + new Vector2(rand.NextFloat(-10, 10), -20);
            text = damage.ToString();
            baseColor = color;
            this.color = color;
            this.hasBlackBacking = hasBlackBacking;
        }

        public override void Update()
        {
            Position += FloatVelocity;
            ticksRemaining--;

            currentAlpha = MathHelper.Clamp(ticksRemaining / (float)LifespanTicks, 0f, 1f);
            color = baseColor * currentAlpha;

            if (ticksRemaining <= 0)
                IsExpired = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (hasBlackBacking)
            {
                spriteBatch.DrawString(
                    Art.DamageFont,
                    text,
                    Position + BackingOffset,
                    Color.Black * (BackingAlpha * currentAlpha),
                    0f,
                    Vector2.Zero,
                    Scale,
                    SpriteEffects.None,
                    0f
                );
            }

            spriteBatch.DrawString(
                Art.DamageFont,
                text,
                Position,
                color,
                0f,
                Vector2.Zero,
                Scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
