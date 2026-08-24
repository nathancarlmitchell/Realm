using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class DamageNumber : Entity
    {
        private static readonly Random rand = new();

        private const int DefaultLifespanTicks = 40;
        private static readonly Vector2 FloatVelocity = new(0, -0.6f);
        private const float DefaultScale = 1.0f;
        private const float DefaultVerticalOffset = -20f;

        // Same offset/alpha as the title screen's own text backing (see
        // Overlay.DrawTitle()/GameOverState.Draw()) — a black copy drawn
        // behind and offset from the real text.
        private static readonly Vector2 BackingOffset = new(-4, 4);
        private const float BackingAlpha = 0.5f;

        private readonly string text;
        private readonly Color baseColor;
        private readonly bool hasBlackBacking;
        private readonly float scale;
        private readonly int lifespanTicks;
        private int ticksRemaining;
        private float currentAlpha = 1f;

        // hasBlackBacking: only the player's own "I took damage" numbers
        // get the title-style backing (see Player.Hit()) — enemy hit
        // numbers (Enemy.WasShot()) are unaffected, matching the user's
        // request scoped to "the player damage number" specifically.
        //
        // prefix: empty for every damage number (unchanged); "+" for an XP
        // gain (see Enemy.WasShot()'s death branch) so it visibly reads as
        // a gain rather than a hit sharing the same floating-number visual.
        //
        // scale/lifespanTicks/verticalOffset: all default to the original
        // damage-number look, unaffected for every existing call site — the
        // XP gain number (see Enemy.WasShot()) passes larger/longer/higher
        // values instead so it reads as a distinct, more prominent event.
        public DamageNumber(
            Vector2 position,
            int damage,
            Color color,
            bool hasBlackBacking = false,
            string prefix = "",
            float scale = DefaultScale,
            int lifespanTicks = DefaultLifespanTicks,
            float verticalOffset = DefaultVerticalOffset
        )
        {
            // Small spawn jitter so simultaneous hits (a bow's multiple arrows,
            // an AoE ability) don't render as one illegible stack of digits.
            Position = position + new Vector2(rand.NextFloat(-10, 10), verticalOffset);
            text = prefix + damage;
            baseColor = color;
            this.color = color;
            this.hasBlackBacking = hasBlackBacking;
            this.scale = scale;
            this.lifespanTicks = lifespanTicks;
            ticksRemaining = lifespanTicks;
        }

        public override void Update()
        {
            Position += FloatVelocity;
            ticksRemaining--;

            currentAlpha = MathHelper.Clamp(ticksRemaining / (float)lifespanTicks, 0f, 1f);
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
                    scale,
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
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
