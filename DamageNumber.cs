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

        // When true, Position is recomputed every tick from the player's
        // live Position instead of just drifting from a frozen spawn point
        // — so a number spawned on the player (its own "I took damage"
        // number, or an XP gain) stays anchored above the player's head as
        // they walk away, rather than being left behind in empty space. The
        // spawn jitter and the upward float are both preserved as offsets
        // layered on top of the player's current position each tick.
        //
        // Public (not just private) so EntityManager.Draw() can single these
        // out for its own above-the-player draw pass — see the comment
        // there for why a player-anchored number needs one.
        public bool FollowsPlayer { get; }
        private readonly Vector2 spawnOffset;
        private Vector2 floatOffset = Vector2.Zero;

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
        //
        // followsPlayer: false for every existing call site except the two
        // player-anchored numbers (Player.Hit()'s own damage-taken number
        // and Enemy.WasShot()'s XP gain) — an enemy's own hit/death number
        // stays anchored to where that enemy was, unaffected.
        public DamageNumber(
            Vector2 position,
            int damage,
            Color color,
            bool hasBlackBacking = false,
            string prefix = "",
            float scale = DefaultScale,
            int lifespanTicks = DefaultLifespanTicks,
            float verticalOffset = DefaultVerticalOffset,
            bool followsPlayer = false
        )
        {
            // Small spawn jitter so simultaneous hits (a bow's multiple arrows,
            // an AoE ability) don't render as one illegible stack of digits.
            spawnOffset = new Vector2(rand.NextFloat(-10, 10), verticalOffset);
            FollowsPlayer = followsPlayer;
            Position = (followsPlayer ? Player.Instance.Position : position) + spawnOffset;
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
            floatOffset += FloatVelocity;
            Position = FollowsPlayer
                ? Player.Instance.Position + spawnOffset + floatOffset
                : Position + FloatVelocity;
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
