using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    public class Portal
    {
        public enum Destination
        {
            None,
            Realm,
            CharacterSelect,
            Bank,
        }

        private AnimatedTexture image;
        private Vector2 position;
        private Rectangle bounds
        {
            get { return new Rectangle((int)position.X + 64, (int)position.Y + 64, 32, 32); }
        }
        public Destination dest;

        // Art.Portal renders each 64px source frame at 1.5x scale (Art.cs),
        // and draws from Origin Vector2.Zero, so this is the on-screen
        // footprint used to center the label beneath it.
        private const int RenderedSize = 96;

        // How close the player needs to stay to a Bank portal for BankSystem to
        // stay open — wider than the tight teleport-trigger `bounds` above so a
        // single step away doesn't flicker the panel shut.
        private const float BankInteractionRadius = 120f;

        public string DisplayName =>
            dest switch
            {
                Destination.Realm => "Realm",
                Destination.CharacterSelect => "Character Select",
                Destination.Bank => "Bank",
                _ => string.Empty,
            };

        public Portal()
        {
            image = Art.Portal;
            position.X = Player.Instance.Position.X + 100;
            position.Y = Player.Instance.Position.Y + 100;
            dest = Destination.Realm;
        }

        public Portal(Vector2 position, Destination dest)
        {
            image = Art.Portal;
            this.position = position;
            this.dest = dest;
        }

        public void EnterPortal()
        {
            switch (dest)
            {
                case Destination.Realm:
                    StateManager.EnterPortal();
                    break;

                case Destination.CharacterSelect:
                    StateManager.SelectClass();
                    break;
            }
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            image.UpdateFrame(elapsed);

            // The bank is a panel to stand near, not a place to teleport to —
            // open/close it based on proximity instead of the tight
            // teleport-trigger `bounds` the other destinations use below.
            if (dest == Destination.Bank)
            {
                Vector2 center = position + new Vector2(RenderedSize / 2f, RenderedSize / 2f);
                float distSq = Vector2.DistanceSquared(Player.Instance.Position, center);

                // Shared so BankSystem's panel can anchor itself above the
                // portal on screen, tracking it as the camera follows the
                // player, rather than sitting at a fixed screen position.
                BankSystem.PortalPosition = center;

                // A loot bag at least as close as the bank wins focus — matches
                // ItemSpawner.NearestOpenBag()'s "closest wins" rule between
                // multiple bags, extended here so dropping an item right next
                // to the bank portal doesn't fight it for the player's
                // attention (see LootBag.DrawLoot()'s matching check).
                BankSystem.IsOpen =
                    distSq < BankInteractionRadius * BankInteractionRadius
                    && distSq <= ItemSpawner.NearestOpenBagDistanceSquared();
                return;
            }

            if (Player.Instance.Bounds.Intersects(bounds))
            {
                EnterPortal();
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Draw the portal.
            image.DrawFrame(spriteBatch, position);

            // Draw the label.
            string label = DisplayName;
            if (!string.IsNullOrEmpty(label))
            {
                Vector2 size = Art.HudFont.MeasureString(label);
                Vector2 labelPos = new(
                    position.X + (RenderedSize / 2) - (size.X / 2),
                    position.Y + RenderedSize + 4
                );

                spriteBatch.DrawString(
                    Art.HudFont,
                    label,
                    labelPos + Vector2.One,
                    Color.Black * 0.6f
                );
                spriteBatch.DrawString(Art.HudFont, label, labelPos, Color.White);
            }
        }
    }
}
