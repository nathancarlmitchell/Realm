using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public class RealmState : State
    {
        Rectangle targetRectangle;

        // Extension points for BossRealmState (a bounded arena instance
        // instead of the open Realm world, and no regular EnemySpawner
        // traffic) — pure constants, safe to read from this base
        // constructor since C# virtual dispatch during construction already
        // resolves to the most-derived override.
        protected virtual bool SpawnsRegularEnemies => true;
        protected virtual int InstanceWorldWidth => Game1.WorldWidth;
        protected virtual int InstanceWorldHeight => Game1.WorldHeight;

        // Boss-arena-specific HUD (name+health bar, appearance announcement)
        // — empty here since the open Realm/regular dungeons never have a
        // Boss; BossRealmState overrides it. Called from the same
        // screen-space spriteBatch.Begin()/End() pair as the rest of the HUD
        // below, so it can use plain screen coordinates like Overlay.cs does.
        protected virtual void DrawBossHud(SpriteBatch spriteBatch) { }

        public static Guid HealthPotionGuid = Guid.NewGuid();
        public static Guid ManaPotionGuid = Guid.NewGuid();
        public static Guid AttackPotionGuid = Guid.NewGuid();
        public static Guid DefensePotionGuid = Guid.NewGuid();
        public static Guid DexterityPotionGuid = Guid.NewGuid();
        public static Guid LifePotionGuid = Guid.NewGuid();
        public static Guid ManaGuid = Guid.NewGuid();
        public static Guid SpeedPotionGuid = Guid.NewGuid();
        public static Guid VitalityPotionGuid = Guid.NewGuid();
        public static Guid WisdomPotionGuid = Guid.NewGuid();

        public RealmState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Debug.WriteLine("New RealmState created.");

            Sound.PlaySong();

            Game1.Camera = new Camera(
                Game1.GameplayViewportWidth,
                Game1.GameplayViewportHeight,
                InstanceWorldWidth,
                InstanceWorldHeight,
                1f
            );

            // Both — every other save site in the game pairs these (StateManager,
            // GameOverState). Saving PlayerData alone here left InventoryData stale
            // on disk if equipment was dragged in/out in the nexus right before
            // entering a dungeon, e.g. an unequipped item's slot showing as empty on
            // disk while the item that should be in the inventory file wasn't there
            // yet — a real desync risk, even if not the exact one reported.
            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();

            ItemSpawner.Reset();
            Portal.Reset();

            // Distance-based enemy spawn density (EnemySpawner.Update())
            // measures from wherever the player entered this Realm instance
            // — only meaningful for a regular dungeon that actually runs
            // EnemySpawner, not the boss arena.
            if (SpawnsRegularEnemies)
                EnemySpawner.SetEntryPosition(Player.Instance.Position);

            // Leaving the Nexus — its fixed portal set no longer applies to
            // the minimap until the player returns (NexusState's
            // constructor re-sets this).
            Portal.NexusPortals = null;

            // Define a drawing rectangle based on the number of tiles wide and high, using the texture dimensions.
            targetRectangle = new Rectangle(0, 0, InstanceWorldWidth, InstanceWorldHeight);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.LinearWrap,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                Game1.Camera.GetTransformation()
            );

            // Draw background.
            spriteBatch.Draw(Art.Tile, new Vector2(32, 32), targetRectangle, Color.White);

            spriteBatch.End();

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                null,
                null,
                null,
                null,
                null,
                Game1.Camera.GetTransformation()
            );

            // Draw portals dropped in the world (e.g. by a defeated
            // SpriteGod, or a boss arena's own exit portal).
            foreach (Portal portal in Portal.DroppedPortals)
            {
                portal.Draw(spriteBatch, gameTime);
            }

            // Draw entities (player, enemies, projectiles).
            EntityManager.Draw(spriteBatch);

            if (Game1._Debug)
            {
                EntityManager.DrawHitboxes(spriteBatch, Portal.DroppedPortals);
            }

            spriteBatch.End();

            spriteBatch.Begin();

            // Draw audio.
            Overlay.DrawAudio(spriteBatch);

            // Draw loot. Iterate a snapshot — DrawLoot() can remove the bag from
            // ItemSpawner.LootBags (when the last item is picked up), which would
            // otherwise throw for mutating the list mid-enumeration.
            foreach (LootBag bag in ItemSpawner.LootBags.ToList())
            {
                bag.DrawLoot(spriteBatch);
            }

            // Draw the HUD sidebar (stats, XP, health, mana, ability,
            // equipment, inventory, in that order).
            Overlay.DrawSidebar(spriteBatch);
            Player.Instance.Inventory.DrawDragGhost(spriteBatch);

            // Draw score.
            Overlay.DrawScore(spriteBatch);

            DrawBossHud(spriteBatch);

            // Draw the portal-entry confirmation prompt, if the player is
            // currently standing in one — no-op otherwise.
            Portal.DrawConfirmationPrompt(gameTime, spriteBatch);

            if (Game1._Debug)
            {
                Overlay.DrawDebug(spriteBatch);
            }

            spriteBatch.End();
        }

        public override void PostUpdate(GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            EntityManager.Update();

            if (SpawnsRegularEnemies)
                EnemySpawner.Update();

            foreach (Portal portal in Portal.DroppedPortals.ToList())
            {
                portal.Update(gameTime);
            }

            // Update high score.
            if (Player.Instance.ExperienceTotal > Player.Instance.HighScore)
            {
                int starsBefore = Player.ComputeStars(
                    Player.Instance.HasReachedLevel20,
                    Player.Instance.HighScore
                );

                Player.Instance.HighScore = Player.Instance.ExperienceTotal;

                int starsAfter = Player.ComputeStars(
                    Player.Instance.HasReachedLevel20,
                    Player.Instance.HighScore
                );

                // Persisted immediately when crossing a star threshold —
                // same reasoning as Player.LevelUp()'s Star 1 save, so a
                // newly-earned star doesn't depend on the player dying or
                // otherwise hitting a save checkpoint first. Gated on
                // starsAfter > starsBefore rather than saving on every
                // HighScore increment — HighScore can climb every frame
                // during active play, and only a threshold crossing is
                // actually worth a disk write.
                if (starsAfter > starsBefore)
                {
                    Util.SavePlayerData();
                    Util.SaveInventoryData();
                    Util.SaveBankData();
                    Util.SaveFameData();
                }
            }
        }
    }
}
