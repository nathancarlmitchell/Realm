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

            // A bounded instance (BossRealmState/DungeonState) left pending
            // via its OTHER exit (its own dropped Nexus portal, taken
            // instead of killing the boss/finishing the dungeon) routes
            // here, not through RealmState — so any position it was holding
            // onto for a later Realm-ward return (RealmState.
            // PendingRealmReturnPosition) is now stale and must be dropped,
            // or it would wrongly hijack a later, entirely unrelated,
            // ordinary Nexus -> Realm walk.
            RealmState.PendingRealmReturnPosition = null;

            // IsOpen is a static field, so it would otherwise persist stale
            // across a state transition (e.g. leaving with the bank open,
            // returning to a freshly-constructed NexusState) — reset it here so
            // every fresh Nexus entry starts with the bank closed.
            BankSystem.IsOpen = false;

            // Every Nexus portal laid out on one tight grid centered on the
            // player's spawn point, rather than each one picking its own
            // one-off offset (the previous layout spread the later test
            // portals out in a single line 100-200px apart, trailing 700px
            // below the player) — a column of the two "real" portals
            // (Character Select/Bank) plus a 3-wide grid of every test
            // shortcut below it, all within a couple hundred px of the
            // player and each other.
            const float columnSpacing = 175f;
            const float rowSpacing = 150f;
            Vector2 origin = Player.Instance.Position;

            var characterSelectPos = origin + new Vector2(-columnSpacing / 2, -150);
            var bankPortalPos = origin + new Vector2(columnSpacing / 2, -150);

            // TEMP: direct shortcuts into the boss arenas/dungeons for
            // testing, so they don't have to be reached by finding and
            // killing a SpriteGod/BigSnake/Cube, or generating/clearing a
            // dungeon, every time. Remove once every fight/dungeon has been
            // tested. Laid out as a 3-column grid below the two portals
            // above, growing downward a row at a time.
            var bossTestPortalPos = origin + new Vector2(-columnSpacing, 0);
            var sthenoTestPortalPos = origin + new Vector2(0, 0);
            var cubeTestPortalPos = origin + new Vector2(columnSpacing, 0);

            var dungeonTestPortalPos = origin + new Vector2(-columnSpacing, rowSpacing);
            var pirateCaveTestPortalPos = origin + new Vector2(0, rowSpacing);

            // TEMP: same shortcut precedent as the rest of this grid — leads
            // straight into BossRealmState with Dreadstump already spawned,
            // skipping having to generate a Pirate Cave and walk to its
            // farthest room's own boss portal every time.
            var dreadstumpTestPortalPos = origin + new Vector2(columnSpacing, rowSpacing);

            portalList =
            [
                new Portal(),
                new Portal(characterSelectPos, Portal.Destination.CharacterSelect),
                new Portal(bankPortalPos, Portal.Destination.Bank),
                new Portal(bossTestPortalPos, Portal.Destination.BossRealm),
                new Portal(sthenoTestPortalPos, Portal.Destination.SthenoBossRealm),
                new Portal(cubeTestPortalPos, Portal.Destination.CubeGodBossRealm),
                new Portal(dungeonTestPortalPos, Portal.Destination.SnakePitDungeon),
                new Portal(pirateCaveTestPortalPos, Portal.Destination.PirateCaveDungeon),
                new Portal(dreadstumpTestPortalPos, Portal.Destination.DreadstumpBossRealm),
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
