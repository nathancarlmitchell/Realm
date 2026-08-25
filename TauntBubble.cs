using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // A floating, word-wrapped line of enemy dialogue above whoever's
    // speaking — the "he randomly taunts" flavor behavior (see
    // Enemy.TauntWhenPlayerNear()). Modeled on DamageNumber's live-follow/
    // fade shape, but purpose-built rather than reusing DamageNumber
    // directly: DamageNumber's FollowsPlayer only ever tracks
    // Player.Instance, never an arbitrary Enemy, and a multi-word sentence
    // needs Util.WrapText, which DamageNumber's single-line damage/XP
    // numbers never needed. Draws its own background panel rather than
    // calling Util.DrawTooltip — that helper's ClampTooltipX assumes
    // screen-space HUD coordinates, but this renders in world space
    // (wherever the speaker actually is), where clamping against
    // Game1.WindowWidth would be meaningless.
    class TauntBubble : Entity
    {
        private const float MaxLineWidth = 220f;
        private const float VerticalOffset = 70f;
        private const int Padding = 4;

        private readonly Enemy speaker;
        private readonly string wrappedText;
        private readonly int lifespanTicks;
        private int ticksRemaining;

        public TauntBubble(Enemy speaker, string text, int lifespanTicks = 300)
        {
            this.speaker = speaker;
            this.lifespanTicks = lifespanTicks;
            ticksRemaining = lifespanTicks;
            wrappedText = Util.WrapText(Art.HudFont, SanitizeForFont(text), MaxLineWidth);
            Position = speaker.Position - new Vector2(0, VerticalOffset);
        }

        // Taunt text comes from hand-written dialogue, which routinely
        // includes "smart" typography (curly quotes, em/en dashes, an
        // ellipsis character) that Art.HudFont's SpriteFont has no glyph
        // for — MeasureString()/DrawString() both throw ArgumentException
        // on a single unresolvable character, which crashes the whole
        // game the instant this bubble tries to render (found via a real
        // "…" in one of BanditLeader.cs's own taunts). Common cases are
        // mapped to a close ASCII equivalent so the wording still reads
        // naturally; anything left over that the font still doesn't
        // support is stripped outright — a last-resort safety net for
        // whatever character neither this list nor the original author
        // anticipated, rather than risking a crash on it.
        private static string SanitizeForFont(string text)
        {
            text = text.Replace('‘', '\'') // left single quote
                .Replace('’', '\'') // right single quote
                .Replace('“', '"') // left double quote
                .Replace('”', '"') // right double quote
                .Replace("…", "...") // ellipsis
                .Replace('–', '-') // en dash
                .Replace('—', '-'); // em dash

            var builder = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (Art.HudFont.Characters.Contains(c))
                    builder.Append(c);
            }
            return builder.ToString();
        }

        public override void Update()
        {
            // Re-anchored every tick (not just at spawn) so the bubble
            // tracks a speaker that's still moving (e.g. MoveTethered)
            // instead of being left behind in empty space.
            Position = speaker.Position - new Vector2(0, VerticalOffset);
            ticksRemaining--;

            if (ticksRemaining <= 0 || speaker.IsExpired)
                IsExpired = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 textSize = Art.HudFont.MeasureString(wrappedText);
            Vector2 textPos = Position - new Vector2(textSize.X / 2f, textSize.Y);

            Rectangle background = new(
                (int)(textPos.X - Padding),
                (int)(textPos.Y - Padding),
                (int)(textSize.X + Padding * 2),
                (int)(textSize.Y + Padding * 2)
            );

            spriteBatch.Draw(Art.HealthBar, background, Color.WhiteSmoke * 0.85f);
            spriteBatch.DrawString(Art.HudFont, wrappedText, textPos, Color.Black);
        }
    }
}
