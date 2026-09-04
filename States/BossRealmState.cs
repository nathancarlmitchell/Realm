using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    // A self-contained boss-fight arena, entered via any BossDestination
    // portal (e.g. the one SpriteGod drops on death — Enemy.WasShot()) —
    // which boss it spawns is decided by the BossDestination passed into
    // the constructor, not hardcoded here. Extends RealmState (rather than
    // being a sibling State) specifically so Input.cs's existing
    // `currentState is RealmState` checks — which gate potions, the ability
    // key, and debug level keys — recognize this as a real dungeon and keep
    // working here with no changes to Input.cs.
    public class BossRealmState : RealmState
    {
        protected override bool SpawnsRegularEnemies => false;
        protected override int InstanceWorldWidth => 2000;
        protected override int InstanceWorldHeight => 2000;

        // Boss-appearance announcement: the boss's name banner is fully
        // visible for the first announcementHoldFrames, then fades out over
        // announcementFadeFrames — ticked down in Update() below, drawn in
        // DrawBossHud(). Purely visual (no new sound asset) since the fight
        // already has its own music via Sound.PlaySong() in RealmState's
        // constructor.
        private int announcementFramesRemaining;
        private const int announcementHoldFrames = 120;
        private const int announcementFadeFrames = 60;
        private string bossAnnouncementName;

        public BossRealmState(
            Game1 game,
            GraphicsDevice graphicsDevice,
            ContentManager content,
            Portal.Destination.BossDestination bossDestination
        )
            : base(game, graphicsDevice, content)
        {
            // base's constructor doesn't reset entities (EnterPortal ->
            // RealmState never has, since the open Realm is meant to feel
            // persistent) — this instance needs to, so nothing from the
            // Realm the player just left follows them into the arena.
            EntityManager.Reset();

            // Remembered so the player reappears here (not at this arena's
            // own small-coordinate spawn point, reinterpreted in the far
            // larger shared world) whenever they next leave — via a real
            // open-world RealmState, or via this arena's own Nexus-exit
            // portal below — see PendingReturnPosition's own doc comment.
            // ??=, not a plain assignment: if this is a dungeon's own boss
            // room (entered from inside a DungeonState, which already
            // captured the true pre-dungeon position), this must not
            // overwrite that with the dungeon's own bounded coordinate.
            PendingReturnPosition ??= Player.Instance.Position;

            Vector2 center = new(InstanceWorldWidth / 2, InstanceWorldHeight / 2);

            // Arena bottom for the player, arena top for the boss — plenty
            // of room to kite between them.
            Player.Instance.Position = center + new Vector2(0, 600);

            // base already built Game1.Camera using the player's pre-arena
            // position; re-sync now that they've been moved.
            Game1.Camera.Pos = Player.Instance.Position;

            var boss = bossDestination.CreateBoss(center + new Vector2(0, -600));
            EntityManager.Add(boss);

            bossAnnouncementName = boss.Name;
            announcementFramesRemaining = announcementHoldFrames + announcementFadeFrames;

            Portal.DroppedPortals.Add(
                new Portal(
                    Player.Instance.Position + new Vector2(0, 150),
                    Portal.Destination.Nexus
                )
            );
        }

        // Draws the plain flat background every RealmState gets (base call,
        // its own Begin/End pair), then gives the active boss a chance to
        // paint its own arena-floor visual on top (e.g.
        // LimonTheSpriteGoddess's conveyor-zone tiles) in a separate
        // PointClamp-sampled pass — a real tile atlas needs point sampling
        // to avoid bleeding an adjacent packed tile in at its seams (same
        // reasoning DungeonState's own DrawBackground() override already
        // documents), which the base LinearWrap pass doesn't provide.
        protected override void DrawBackground(SpriteBatch spriteBatch)
        {
            base.DrawBackground(spriteBatch);

            Boss boss = EntityManager.ActiveBoss;
            if (boss == null)
                return;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                Game1.Camera.GetTransformation()
            );
            boss.DrawArenaFloor(spriteBatch);
            spriteBatch.End();
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
            Camera cameraBeforeUpdate = Game1.Camera;

            base.Update(gameTime);

            // base.Update() (RealmState.Update()) walks Portal.DroppedPortals
            // and can synchronously call a portal's own EnterPortal() (e.g.
            // the exit portal this arena's own constructor drops, or the one
            // Boss.OnDeath() drops back to the Realm), which constructs the
            // NEXT state right there and gives it its own fresh Game1.Camera
            // (RealmState's constructor always does) — Game1.ChangeState()
            // only queues that state for next frame, so this BossRealmState
            // instance is still "currentState" and keeps running the rest of
            // this very Update() call regardless. Without this guard, the
            // arena-bound clamp below would fire anyway, teleporting
            // Player.Instance.Position into this tiny InstanceWorldWidth/
            // Height range (2000, vs. the real Realm's 500,000) and stomping
            // the NEW state's own Camera.Pos with that same tiny-arena
            // coordinate — which the new Camera's much larger world then
            // reads as "right at the corner," permanently barrier-clamping
            // it there until the player walks far enough away, looking
            // exactly like "camera tracking stopped working" after leaving a
            // boss room. Bailing out here as soon as the Camera identity
            // changes means this stale instance touches neither Player.
            // Instance.Position nor the new state's Camera again.
            if (Game1.Camera != cameraBeforeUpdate)
                return;

            float radius = Player.Instance.Radius;
            Vector2 pos = Player.Instance.Position;
            pos.X = MathHelper.Clamp(pos.X, radius, InstanceWorldWidth - radius);
            pos.Y = MathHelper.Clamp(pos.Y, radius, InstanceWorldHeight - radius);
            Player.Instance.Position = pos;

            Game1.Camera.Pos = pos;

            // Same wall, applied to the boss — every existing boss stays
            // near the arena's center on its own (MoveTethered/FollowPlayer
            // both self-correct toward a bounded point), but nothing stopped
            // an unbounded movement pattern from drifting past the edge
            // undetected (Dreadstump's Kiting phase flees straight away from
            // the player with no tether at all, and reached this within a
            // couple of seconds of the fight starting).
            Boss boss = EntityManager.ActiveBoss;
            if (boss != null)
            {
                float bossRadius = boss.Radius;
                Vector2 bossPos = boss.Position;
                bossPos.X = MathHelper.Clamp(bossPos.X, bossRadius, InstanceWorldWidth - bossRadius);
                bossPos.Y = MathHelper.Clamp(bossPos.Y, bossRadius, InstanceWorldHeight - bossRadius);
                boss.Position = bossPos;
            }

            if (announcementFramesRemaining > 0)
                announcementFramesRemaining--;
        }

        // Boss name+health bar (drawn whenever a boss is alive) and the
        // fade-out appearance announcement banner (drawn only for the first
        // few seconds after the fight starts) — both screen-space, centered
        // over the gameplay viewport (not the sidebar).
        protected override void DrawBossHud(SpriteBatch spriteBatch)
        {
            Boss boss = EntityManager.ActiveBoss;
            if (boss == null)
                return;

            int centerX = Game1.GameplayViewportWidth / 2;

            if (announcementFramesRemaining > 0)
            {
                float alpha =
                    announcementFramesRemaining > announcementFadeFrames
                        ? 1f
                        : announcementFramesRemaining / (float)announcementFadeFrames;

                // Art.RetroFontLarge (shared with the main menu's own
                // "Realm" title — see Overlay.TitleScale) is baked at the
                // intended native title size, so Overlay.TitleScale (1) is
                // the ceiling here, never a stretch-up factor. A multi-word
                // boss name can still overflow the gameplay viewport at
                // that native size, so this scales it down further to fit
                // within a comfortable margin whenever it would — shrinking
                // a large baked font stays crisp, unlike the blur stretching
                // RetroFont's much smaller size up used to cause here.
                Vector2 rawSize = Art.RetroFontLarge.MeasureString(bossAnnouncementName);
                float scale = Math.Min(
                    Overlay.TitleScale,
                    (Game1.GameplayViewportWidth - 80) / rawSize.X
                );
                Vector2 size = rawSize * scale;
                Vector2 pos = new(centerX - size.X / 2, 120);

                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFontLarge,
                    bossAnnouncementName,
                    pos,
                    Color.DarkMagenta * alpha,
                    scale
                );
            }

            const int barWidth = 400;
            const int barHeight = 24;
            int barX = centerX - barWidth / 2;
            const int barY = 20;

            Vector2 nameSize = Art.RetroFont.MeasureString(boss.Name);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                boss.Name,
                new Vector2(centerX - nameSize.X / 2, barY - nameSize.Y - 2),
                Color.White
            );

            float healthFraction = boss.HealthMax > 0 ? (float)boss.Health / boss.HealthMax : 0f;
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(barX, barY, barWidth, barHeight),
                Color.Black * 0.5f
            );
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(barX, barY, (int)(barWidth * healthFraction), barHeight),
                Color.DarkRed
            );
        }
    }
}
