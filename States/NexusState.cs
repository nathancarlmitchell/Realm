using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public class NexusState : State
    {
        Rectangle targetRectangle;

        private List<Portal> portalList;

        public NexusState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Game1.Camera = new Camera(
                Game1.GameplayViewportWidth,
                Game1.GameplayViewportHeight,
                Game1.WorldWidth,
                Game1.WorldHeight,
                1f
            );

            EntityManager.Reset();

            // IsOpen is a static field, so it would otherwise persist stale
            // across a state transition (e.g. leaving with the bank open,
            // returning to a freshly-constructed NexusState) — reset it here so
            // every fresh Nexus entry starts with the bank closed.
            BankSystem.IsOpen = false;

            var portalPos = new Vector2(
                Player.Instance.Position.X - 25,
                Player.Instance.Position.Y - 100
            );

            var bankPortalPos = new Vector2(
                Player.Instance.Position.X + 150,
                Player.Instance.Position.Y - 100
            );

            // TEMP: direct shortcuts into the boss arenas for testing, so
            // they don't have to be reached by finding and killing a
            // SpriteGod/BigSnake/Cube every time. Remove once all three boss
            // fights have been tested.
            var bossTestPortalPos = new Vector2(
                Player.Instance.Position.X - 150,
                Player.Instance.Position.Y - 100
            );

            var sthenoTestPortalPos = new Vector2(
                Player.Instance.Position.X - 150,
                Player.Instance.Position.Y + 100
            );

            var cubeTestPortalPos = new Vector2(
                Player.Instance.Position.X - 150,
                Player.Instance.Position.Y + 300
            );

            // TEMP: same "fixed test portal" precedent as the three boss
            // portals above — lowest-risk way to reach a real DungeonState
            // before any in-world discovery mechanic (e.g. a rare drop)
            // exists for it.
            var dungeonTestPortalPos = new Vector2(
                Player.Instance.Position.X - 150,
                Player.Instance.Position.Y + 500
            );

            portalList =
            [
                new Portal(),
                new Portal(portalPos, Portal.Destination.CharacterSelect),
                new Portal(bankPortalPos, Portal.Destination.Bank),
                new Portal(bossTestPortalPos, Portal.Destination.BossRealm),
                new Portal(sthenoTestPortalPos, Portal.Destination.SthenoBossRealm),
                new Portal(cubeTestPortalPos, Portal.Destination.CubeGodBossRealm),
                new Portal(dungeonTestPortalPos, Portal.Destination.Dungeon),
            ];

            // So Overlay's minimap can show these regardless of which state
            // is active — see Portal.NexusPortals's own doc comment.
            Portal.NexusPortals = portalList;

            // Define a drawing rectangle based on the number of tiles wide and high, using the texture dimensions.
            targetRectangle = new Rectangle(0, 0, Game1.WorldWidth, Game1.WorldHeight);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                null,
                null,
                null,
                null,
                null,
                Game1.Camera.GetTransformation()
            );

            // Draw background.
            spriteBatch.Draw(Art.Tile, new Vector2(32, 32), targetRectangle, Color.WhiteSmoke);

            // Draw portal.
            foreach (Portal portal in portalList)
            {
                portal.Draw(spriteBatch, gameTime);
            }

            // Draw player.
            EntityManager.Draw(spriteBatch);

            if (Game1._Debug || Player.Instance.ShowHitboxesEnabled)
            {
                EntityManager.DrawHitboxes(spriteBatch, portalList);
            }

            spriteBatch.End();

            spriteBatch.Begin();

            // Draw loot. Iterate a snapshot — DrawLoot() can remove the bag from
            // ItemSpawner.LootBags (when the last item is picked up), which would
            // otherwise throw for mutating the list mid-enumeration. Matches
            // RealmState.Draw() — without this, bags dropped in the Nexus render
            // (via EntityManager.Draw above) but can never actually be opened,
            // since DrawLoot() is where the click-to-pickup logic lives.
            foreach (LootBag bag in ItemSpawner.LootBags.ToList())
            {
                bag.DrawLoot(spriteBatch);
            }

            // Draw the HUD sidebar (stats, XP, health, mana, ability,
            // equipment, inventory, in that order).
            Overlay.DrawSidebar(spriteBatch);

            // Draw the portal-entry confirmation prompt (sidebar, below the
            // inventory grid), if the player is currently standing in one —
            // no-op otherwise.
            Portal.DrawConfirmationPrompt(gameTime, spriteBatch);

            // Draw bank (only visible while open).
            BankSystem.Draw(spriteBatch);

            // Drag ghosts draw last, after both panels' own contents, so
            // dragging between the two never gets covered up by whichever
            // panel happens to draw later.
            Player.Instance.Inventory.DrawDragGhost(spriteBatch);
            BankSystem.DrawDragGhost(spriteBatch);

            // Draw Fame, top-left — same corner Score/Hi Score used to occupy.
            Overlay.DrawFame(spriteBatch);

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

            foreach (Portal portal in portalList)
            {
                portal.Update(gameTime);
            }

            BankSystem.Update();
        }
    }
}
