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
                Game1.WorldWidth,
                Game1.WorldHeight,
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

            // Define a drawing rectangle based on the number of tiles wide and high, using the texture dimensions.
            targetRectangle = new Rectangle(0, 0, Game1.WorldWidth, Game1.WorldHeight);
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

            // Draw entities (player, enemies, projectiles).
            EntityManager.Draw(spriteBatch);

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
            EnemySpawner.Update();

            // Update high score.
            if (Player.Instance.ExperienceTotal > Player.Instance.HighScore)
            {
                Player.Instance.HighScore = Player.Instance.ExperienceTotal;
            }
        }
    }
}
