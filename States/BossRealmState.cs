using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.Bosses;

namespace Realm.States
{
    // A self-contained boss-fight arena, entered via the portal SpriteGod
    // drops on death (Enemy.WasShot()). Extends RealmState (rather than
    // being a sibling State) specifically so Input.cs's existing
    // `currentState is RealmState` checks — which gate potions, the ability
    // key, and debug level keys — recognize this as a real dungeon and keep
    // working here with no changes to Input.cs.
    public class BossRealmState : RealmState
    {
        protected override bool SpawnsRegularEnemies => false;
        protected override int InstanceWorldWidth => 2000;
        protected override int InstanceWorldHeight => 2000;

        public BossRealmState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base(game, graphicsDevice, content)
        {
            // base's constructor doesn't reset entities (EnterPortal ->
            // RealmState never has, since the open Realm is meant to feel
            // persistent) — this instance needs to, so nothing from the
            // Realm the player just left follows them into the arena.
            EntityManager.Reset();

            Vector2 center = new(InstanceWorldWidth / 2, InstanceWorldHeight / 2);

            // Arena bottom for the player, arena top for the boss — plenty
            // of room to kite between them.
            Player.Instance.Position = center + new Vector2(0, 600);

            // base already built Game1.Camera using the player's pre-arena
            // position; re-sync now that they've been moved.
            Game1.Camera.Pos = Player.Instance.Position;

            EntityManager.Add(new LimonTheSpriteGoddess(center + new Vector2(0, -600)));

            Portal.DroppedPortals.Add(
                new Portal(
                    Player.Instance.Position + new Vector2(0, 150),
                    Portal.Destination.Nexus
                )
            );
        }

        // Hard walls — the arena is meant to be a bounded room, but nothing
        // in Player.Update() ever clamps Position on its own (the open Realm
        // world is 500,000px, so this was never previously reachable in
        // practice). Runs after the base Update() (which already moved the
        // player this frame) so it's a real wall, not a one-frame-late
        // correction, and re-syncs the camera to match — otherwise the
        // camera would reflect the pre-clamp position for a frame, since
        // Player.Update() already set it before this override runs.
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float radius = Player.Instance.Radius;
            Vector2 pos = Player.Instance.Position;
            pos.X = MathHelper.Clamp(pos.X, radius, InstanceWorldWidth - radius);
            pos.Y = MathHelper.Clamp(pos.Y, radius, InstanceWorldHeight - radius);
            Player.Instance.Position = pos;

            Game1.Camera.Pos = pos;
        }
    }
}
