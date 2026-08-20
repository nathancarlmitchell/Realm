using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Bosses;
using Realm.States;

namespace Realm
{
    public class Portal
    {
        // Was a fixed enum until a second boss made that shape too rigid —
        // BossRealm needs to carry *which* boss to spawn, not just route to
        // a hardcoded one. Now each destination is its own small subclass
        // instance (same "shared base + concrete variant" shape as
        // Boss/LimonTheSpriteGoddess), so adding a second boss's own
        // destination later is one new static field, not a switch edit.
        public abstract class Destination
        {
            public abstract string DisplayName { get; }
            public abstract void Enter();

            public static readonly Destination Realm = new RealmDestination();
            public static readonly Destination CharacterSelect =
                new CharacterSelectDestination();
            public static readonly Destination Bank = new BankDestination();

            // Dropped by SpriteGod on death (see Enemy.WasShot()) — leads
            // into a self-contained boss-fight arena (BossRealmState).
            public static readonly Destination BossRealm = new BossDestination(
                "Limon the Sprite Goddess",
                position => new LimonTheSpriteGoddess(position)
            );

            // Dropped by BigSnake on death (see Enemy.CreateBigSnake()) —
            // same shape as BossRealm above, just carrying the second boss.
            public static readonly Destination SthenoBossRealm = new BossDestination(
                "Stheno the Snake Queen",
                position => new SthenoTheSnakeQueen(position)
            );

            // The boss arena's own exit portal. No other portal currently
            // routes to Nexus — every other Nexus-bound path goes straight
            // through StateManager.Nexus() rather than a world portal.
            public static readonly Destination Nexus = new NexusDestination();

            private sealed class RealmDestination : Destination
            {
                public override string DisplayName => "Realm";
                public override void Enter() => StateManager.EnterPortal();
            }

            private sealed class CharacterSelectDestination : Destination
            {
                public override string DisplayName => "Character Select";
                public override void Enter() => StateManager.SelectClass();
            }

            private sealed class BankDestination : Destination
            {
                public override string DisplayName => "Bank";

                // Never actually reached — Update() below special-cases the
                // Bank destination via proximity (open/close the panel) and
                // returns before the teleport-trigger EnterPortal() call
                // that would invoke this. Implemented as a real no-op
                // rather than a throw so it can't crash if that ever
                // changes.
                public override void Enter() { }
            }

            private sealed class NexusDestination : Destination
            {
                public override string DisplayName => "Nexus";
                public override void Enter() => StateManager.Nexus();
            }

            // Carries which boss to spawn, so BossRealmState no longer
            // hardcodes one. BossName/CreateBoss are internal rather than
            // public since Boss itself is internal (Boss.cs) — this stays
            // public so it's still nameable as a parameter type from
            // StateManager/BossRealmState (same assembly, so the internal
            // members are still callable from there).
            public sealed class BossDestination : Destination
            {
                internal string BossName { get; }
                internal Func<Vector2, Boss> CreateBoss { get; }

                internal BossDestination(string bossName, Func<Vector2, Boss> createBoss)
                {
                    BossName = bossName;
                    CreateBoss = createBoss;
                }

                public override string DisplayName => "Boss Fight";
                public override void Enter() => StateManager.EnterBossRealm(this);
            }
        }

        // Portals dropped dynamically into the world at runtime (as opposed
        // to a state's own fixed portalList, e.g. NexusState's) — populated
        // by whatever caused the drop (Enemy.WasShot() for SpriteGod's
        // portal, BossRealmState's constructor for its exit portal) and
        // iterated by RealmState's Update()/Draw(). Reset() is called from
        // RealmState's constructor, so each fresh RealmState/BossRealmState
        // entry starts with only the portals it's supposed to have.
        public static List<Portal> DroppedPortals = [];

        public static void Reset() => DroppedPortals = [];

        // The current state's own fixed portal set — currently only
        // NexusState has one (Realm/CharacterSelect/Bank/BossRealm test
        // shortcut); null everywhere else. Kept separate from
        // DroppedPortals (which resets on every dungeon entry) since these
        // aren't "dropped" by anything, they're just always there for as
        // long as the Nexus is. Read by Overlay's minimap so it can show
        // portal blips regardless of which state is currently active.
        public static List<Portal> NexusPortals;

        private AnimatedTexture image;
        private Vector2 position;
        public Vector2 Position => position;
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

        public string DisplayName => dest.DisplayName;

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

        public void EnterPortal() => dest.Enter();

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
