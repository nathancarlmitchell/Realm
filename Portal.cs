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
using Realm.Controls;
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

            // Which animation a portal to this destination draws itself
            // with — a fresh AnimatedTexture.Clone() each call, not the
            // shared Art.* instance directly, so every Portal gets its own
            // independent frame/elapsed clock (see AnimatedTexture.Clone()'s
            // own doc comment for why: a non-looping animation especially
            // needs this, since sharing one clock means a second portal
            // dropped after the first one already finished playing would
            // start out already-finished too, never actually animating).
            // Resolved lazily (only called once a Portal is actually
            // constructed, well after Art.Load() has run) rather than
            // eagerly at these static fields' own init time, since that
            // happens before content is loaded. Defaults to the plain
            // swirl every non-dungeon destination (Realm/Bank/Nexus/etc.)
            // still uses.
            internal virtual AnimatedTexture PortalArt() => Art.Portal.Clone();

            public static readonly Destination Realm = new RealmDestination();
            public static readonly Destination CharacterSelect = new CharacterSelectDestination();
            public static readonly Destination Bank = new BankDestination();

            // Dropped by SpriteGod on death (see Enemy.WasShot()) — leads
            // into a self-contained boss-fight arena (BossRealmState).
            // DungeonName is the room's own identity, distinct from
            // BossName (the boss fought inside it) — first step toward the
            // eventual per-boss unique-dungeon backlog item; today it just
            // labels the portal and picks its animation.
            public static readonly Destination BossRealm = new BossDestination(
                "Limon the Sprite Goddess",
                "Sprite World",
                position => new LimonTheSpriteGoddess(position),
                () => Art.SpriteWorldPortal
            );

            // Dropped by BigSnake on death (see Enemy.CreateBigSnake()) —
            // same shape as BossRealm above, just carrying the second boss.
            public static readonly Destination SthenoBossRealm = new BossDestination(
                "Stheno the Snake Queen",
                "Snake Pit",
                position => new SthenoTheSnakeQueen(position),
                () => Art.SnakePitPortal
            );

            // Dropped by the new "Cube" trigger enemy on death (see
            // Enemy.CreateCube()) — same shape as BossRealm/SthenoBossRealm
            // above, carrying the third boss.
            public static readonly Destination CubeGodBossRealm = new BossDestination(
                "Cube God",
                "The Third Dimension",
                position => new CubeGod(position),
                () => Art.ThirdDimensionPortal
            );

            // The boss arena's own exit portal. No other portal currently
            // routes to Nexus — every other Nexus-bound path goes straight
            // through StateManager.Nexus() rather than a world portal.
            public static readonly Destination Nexus = new NexusDestination();

            // Leads into a procedurally-generated, walled DungeonState —
            // see States/DungeonState.cs. No dedicated art yet (unlike each
            // BossDestination's own portal texture); PortalArt() below is
            // left at the base class's default plain swirl (Art.Portal)
            // until real per-dungeon art exists.
            public static readonly Destination Dungeon = new DungeonDestination();

            private sealed class RealmDestination : Destination
            {
                public override string DisplayName => "Realm";

                public override void Enter() => StateManager.EnterPortal();

                internal override AnimatedTexture PortalArt() => Art.RealmPortal.Clone();
            }

            private sealed class CharacterSelectDestination : Destination
            {
                public override string DisplayName => "Character Select";

                public override void Enter() => StateManager.CharacterSlots();

                internal override AnimatedTexture PortalArt() => Art.CharacterSelectPortal.Clone();
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

                internal override AnimatedTexture PortalArt() => Art.BankPortal.Clone();
            }

            private sealed class NexusDestination : Destination
            {
                public override string DisplayName => "Nexus";

                public override void Enter() => StateManager.Nexus();

                internal override AnimatedTexture PortalArt() => Art.NexusPortal.Clone();
            }

            private sealed class DungeonDestination : Destination
            {
                public override string DisplayName => "Dungeon";

                public override void Enter() => StateManager.EnterDungeon();
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
                internal string DungeonName { get; }
                internal Func<Vector2, Boss> CreateBoss { get; }
                private readonly Func<AnimatedTexture> getPortalArt;

                internal BossDestination(
                    string bossName,
                    string dungeonName,
                    Func<Vector2, Boss> createBoss,
                    Func<AnimatedTexture> getPortalArt
                )
                {
                    BossName = bossName;
                    DungeonName = dungeonName;
                    CreateBoss = createBoss;
                    this.getPortalArt = getPortalArt;
                }

                public override string DisplayName => DungeonName;

                public override void Enter() => StateManager.EnterBossRealm(this);

                internal override AnimatedTexture PortalArt() => getPortalArt().Clone();
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

        // Called from Game1.ChangeState() — every state transition funnels
        // through there, not just RealmState's own constructor (which only
        // resets DroppedPortals above). Without this, leaving mid-prompt via
        // any path that doesn't go through this portal's own confirm flow
        // (Escape to MainMenu, the ReturnToNexus key bind, dying) would
        // leave `pendingConfirmation` pointing at a Portal instance that
        // belonged to the now-discarded state — DrawConfirmationPrompt()
        // would keep rendering a phantom prompt anchored to a stale world
        // position from a completely different screen.
        public static void ClearPendingConfirmation() => pendingConfirmation = null;

        // The current state's own fixed portal set — currently only
        // NexusState has one (Realm/CharacterSelect/Bank/BossRealm test
        // shortcut); null everywhere else. Kept separate from
        // DroppedPortals (which resets on every dungeon entry) since these
        // aren't "dropped" by anything, they're just always there for as
        // long as the Nexus is. Read by Overlay's minimap so it can show
        // portal blips regardless of which state is currently active.
        public static List<Portal> NexusPortals;

        // Whichever portal the player is currently standing in, awaiting a
        // confirm click/keypress before actually teleporting — null when
        // not standing in any. Static rather than per-instance since only
        // one portal can realistically be touched at once (every portal's
        // trigger box is tiny relative to how far apart they're placed),
        // matching BankSystem's own single-shared-panel pattern rather than
        // giving every Portal instance its own redundant Button. Set/cleared
        // in Update() below, read by the static DrawConfirmationPrompt().
        private static Portal pendingConfirmation;

        // Lazily constructed (not a field initializer) — Button's own
        // constructor reads Art.ButtonTexture/Art.RetroFont immediately, and
        // this being `static` on Portal means it could otherwise run before
        // Art.Load() has populated those, capturing null permanently (same
        // hazard Destination.PortalArt() above works around).
        private static Button confirmButton;

        private static Button ConfirmButton
        {
            get
            {
                if (confirmButton == null)
                {
                    confirmButton = new Button { Text = "Enter" };
                    confirmButton.Click += (s, e) =>
                    {
                        pendingConfirmation?.EnterPortal();
                        pendingConfirmation = null;
                    };
                }
                return confirmButton;
            }
        }

        private AnimatedTexture image;

        // The portal's visual CENTER — matches how every other Entity in
        // the engine already treats Position (Player/Enemy/LootBag all draw
        // centered on it, via Origin = Size/2f in Entity.Draw()). Portal
        // isn't an Entity, and its own draw call (image.DrawFrame() below)
        // draws from a top-left corner with no origin offset, so a caller
        // passing e.g. a dying enemy's own Position (its center) used to
        // make the portal render with its top-left corner AT the enemy's
        // center instead — visibly offset down-and-right by roughly half
        // the portal's own rendered size. TopLeft below converts this
        // center back into whatever the actual draw/bounds math needs, so
        // every caller can just pass "where I want this to appear" the same
        // way they already do for every other entity in the game.
        private Vector2 position;
        public Vector2 Position => position;

        private Vector2 TopLeft => position - new Vector2(RenderedWidth / 2f, RenderedHeight / 2f);

        // Fraction of the portal's own rendered footprint the teleport
        // trigger's radius occupies. Was previously a 1/3-sized box
        // centered in the middle third of the frame (and, before that,
        // corner-anchored — see entry 116/117's original investigation:
        // every portal's art is a roughly circular/arch/diamond shape that
        // doesn't fill the corners of its own bounding square, so a
        // corner-anchored box sat next to the visible sprite instead of on
        // it, confirmed by rendering each portal + its outline to an
        // offscreen RenderTarget2D and inspecting the PNG directly). Now a
        // circle instead, per direct request — no separate offset needed
        // at all, since a circle centered on `position` is inherently
        // centered already, unlike a box. Note this is still a single
        // fixed radius, not re-derived per animation frame — Sprite
        // World's opening-animation frames vary a lot in visible content
        // size (frame 0 is a small closed icon, later frames fill most of
        // the cell), so alignment during the tiny early frames is still
        // only approximate; a per-frame hitbox isn't worth the complexity
        // for a debug-only visual plus a walk-up teleport trigger.
        private const float TriggerRadiusFraction = 1f / 3f;

        private float radius =>
            ((RenderedWidth + RenderedHeight) / 2f) * TriggerRadiusFraction * 2f;

        // Public read-only view of the same radius, for the F3 debug
        // hitbox overlay (EntityManager.DrawHitboxes()) — the teleport
        // trigger area, not the sprite's on-screen footprint.
        public float Radius => radius;

        public Destination dest;

        // How close the player needs to stay to a Bank portal for BankSystem to
        // stay open — wider than the tight teleport-trigger `radius` above so a
        // single step away doesn't flicker the panel shut.
        private const float BankInteractionRadius = 120f;

        public string DisplayName => dest.DisplayName;

        // On-screen footprint (source frame size at this portal's own draw
        // scale) — used to center the label beneath it, and to derive
        // `radius` above. Computed per-image rather than a shared constant
        // since dungeon portals (see Destination.PortalArt) use a smaller
        // source frame than the generic 64px swirl.
        private float RenderedWidth => image.FrameWidth * image.Scale;
        private float RenderedHeight => image.FrameHeight * image.Scale;

        public Portal()
        {
            dest = Destination.Realm;
            image = dest.PortalArt();
            position.X = Player.Instance.Position.X + 100;
            position.Y = Player.Instance.Position.Y + 100;
        }

        public Portal(Vector2 position, Destination dest)
        {
            this.dest = dest;
            image = dest.PortalArt();
            this.position = position;
        }

        public void EnterPortal() => dest.Enter();

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            image.UpdateFrame(elapsed);

            // The bank is a panel to stand near, not a place to teleport to —
            // open/close it based on proximity instead of the tight
            // teleport-trigger `radius` the other destinations use below.
            if (dest == Destination.Bank)
            {
                // position is already the portal's visual center (see
                // TopLeft's doc comment above) — no offset needed here.
                float distSq = Vector2.DistanceSquared(Player.Instance.Position, position);

                // Shared so BankSystem's panel can anchor itself above the
                // portal on screen, tracking it as the camera follows the
                // player, rather than sitting at a fixed screen position.
                BankSystem.PortalPosition = position;

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

            // Standing in the trigger no longer teleports instantly — it
            // arms this portal as the pending confirmation (see
            // DrawConfirmationPrompt()), entered only via a click on the
            // HUD button or the ConfirmPortalEntry key bind. Stepping back
            // out cancels it, same as walking away from any other
            // proximity prompt in the game.
            //
            // Circle-vs-circle, combining both radii — the same convention
            // EntityManager.IsColliding() already uses for every other
            // circle pairing in the game (e.g. player vs. an enemy),
            // rather than treating the player as a dimensionless point.
            float triggerRadius = radius + Player.Instance.Radius;
            bool inTrigger =
                Vector2.DistanceSquared(Player.Instance.Position, position)
                < triggerRadius * triggerRadius;
            if (inTrigger)
            {
                // Settings > Gameplay > "Auto-Enter Portals" — skips the
                // confirm prompt entirely and teleports the instant the
                // player steps into the trigger, same "call EnterPortal()
                // and clear pendingConfirmation" shape the manual confirm
                // paths below use, just triggered by proximity instead of a
                // click/keypress.
                if (Player.Instance.AutoEnterPortalsEnabled)
                {
                    pendingConfirmation = null;
                    EnterPortal();
                    return;
                }

                pendingConfirmation = this;

                if (Input.WasBindingPressed(KeyBindings.Get(KeyBindings.Action.ConfirmPortalEntry)))
                {
                    pendingConfirmation = null;
                    EnterPortal();
                }
            }
            else if (pendingConfirmation == this)
            {
                pendingConfirmation = null;
            }
        }

        // Called once per frame from each state's untransformed
        // (screen-space) draw pass, right after Overlay.DrawSidebar() — a
        // fixed sidebar spot below the inventory grid rather than floating
        // above the portal in world space (the original placement), per
        // the user's request. A no-op whenever nothing is currently
        // pending. Sits in Portal.cs rather than Overlay.cs since it still
        // owns the Button/click-wiring itself (see ConfirmButton above);
        // Overlay.cs just decides where in the draw order to invoke it,
        // same as every other section there delegating to its own system
        // (e.g. Player.Instance.Weapon.DrawEquipped()).
        private const int SidebarSectionPadding = 20;

        public static void DrawConfirmationPrompt(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (pendingConfirmation == null)
                return;

            int x = Game1.SidebarX + SidebarSectionPadding;
            // Player.Instance.Inventory.Bounds.Bottom rather than a
            // hardcoded Y, so this stays correctly positioned if the
            // inventory grid's own layout ever changes.
            int y = Player.Instance.Inventory.Bounds.Bottom + 20;

            string dungeonName = pendingConfirmation.DisplayName;
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                dungeonName,
                new Vector2(x, y),
                Color.White
            );
            int labelHeight = (int)Art.RetroFont.MeasureString(dungeonName).Y;

            Button button = ConfirmButton;
            button.Position = new Vector2(x, y + labelHeight + 6);
            button.Update(gameTime);
            button.Draw(gameTime, spriteBatch);

            string hint = $"or press [{KeyBindings.Get(KeyBindings.Action.ConfirmPortalEntry)}]";
            Vector2 hintPos = new(x, button.Position.Y + button.Rectangle.Height + 6);
            Util.DrawOutlinedText(spriteBatch, Art.RetroFont, hint, hintPos, Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Draw the portal — image.DrawFrame() draws from a top-left
            // corner with no origin offset, so this needs TopLeft rather
            // than position (the portal's center) directly.
            image.DrawFrame(spriteBatch, TopLeft);

            // Draw the label, centered under the sprite and just below its
            // bottom edge (position.Y + half the rendered height).
            string label = DisplayName;
            if (!string.IsNullOrEmpty(label))
            {
                Vector2 size = Art.RetroFont.MeasureString(label);
                Vector2 labelPos = new(
                    position.X - (size.X / 2),
                    position.Y + (RenderedHeight / 2) + 4
                );

                Util.DrawOutlinedText(spriteBatch, Art.RetroFont, label, labelPos, Color.White);
            }
        }
    }
}
