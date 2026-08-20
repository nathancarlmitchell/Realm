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

                // Art.TitleFont is sized for the one-word Main Menu title —
                // a multi-word boss name at scale 1 overflows the gameplay
                // viewport on both sides, so scale it down to fit within a
                // comfortable margin instead of drawing at native size.
                Vector2 rawSize = Art.TitleFont.MeasureString(bossAnnouncementName);
                float scale = Math.Min(1f, (Game1.GameplayViewportWidth - 80) / rawSize.X);
                Vector2 size = rawSize * scale;
                Vector2 pos = new(centerX - size.X / 2, 120);

                spriteBatch.DrawString(
                    Art.TitleFont,
                    bossAnnouncementName,
                    pos + new Vector2(-4, 4) * scale,
                    Color.DarkOrange * (0.75f * alpha),
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
                spriteBatch.DrawString(
                    Art.TitleFont,
                    bossAnnouncementName,
                    pos,
                    Color.DarkMagenta * alpha,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            const int barWidth = 400;
            const int barHeight = 24;
            int barX = centerX - barWidth / 2;
            const int barY = 20;

            Vector2 nameSize = Art.HudFont.MeasureString(boss.Name);
            spriteBatch.DrawString(
                Art.HudFont,
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
