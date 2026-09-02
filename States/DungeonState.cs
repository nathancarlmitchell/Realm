using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.Data;

namespace Realm.States
{
    // A procedurally-generated, walled dungeon instance — rooms and
    // corridors with real collision, as opposed to the open Realm's
    // unobstructed plane or BossRealmState's single bounding box. Extends
    // RealmState for the same reason BossRealmState does (Input.cs's
    // `currentState is RealmState` checks keep working here with no changes
    // needed there).
    public class DungeonState : RealmState
    {
        protected override bool SpawnsRegularEnemies => false;
        protected override int InstanceWorldWidth => 3200;
        protected override int InstanceWorldHeight => 3200;

        private readonly DungeonMap dungeonMap;
        private readonly Texture2D tileAtlas;
        private readonly DungeonPathfindingController pathfindingController;
        private readonly DungeonEnemySpawner dungeonEnemySpawner;

        // Accumulates fractional per-frame tile damage (DamagePerSecond / 60,
        // matching this codebase's existing fixed-60fps-tick convention —
        // see e.g. Player.cs's healthCooldown/manaCooldown regen) so a small
        // DamagePerSecond value isn't silently lost to Hit()'s int rounding.
        private float tileDamageAccumulator;

        public DungeonState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base(game, graphicsDevice, content)
        {
            // Clean slate, same reasoning as BossRealmState's constructor —
            // nothing from wherever the player just left should follow them
            // into a freshly-generated dungeon.
            EntityManager.Reset();

            // Which tileset a given dungeon uses is a later, cheap decision
            // (e.g. a constructor parameter once more than one tileset
            // exists) — hardcoded to the one placeholder tileset for now.
            TileSetData tileSet = Util.LoadTileSetData("Crypt");
            dungeonMap = DungeonGenerator.Generate(
                tileSet,
                InstanceWorldWidth / tileSet.TileWidth,
                InstanceWorldHeight / tileSet.TileHeight
            );
            tileAtlas = content.Load<Texture2D>(tileSet.ImageName);

            Vector2 startPos = RoomCenterWorldPosition(dungeonMap.Rooms[0]);
            Player.Instance.Position = startPos;

            // base's constructor already built Game1.Camera using the
            // player's pre-dungeon position; re-sync now that they've moved.
            Game1.Camera.Pos = startPos;

            Portal.DroppedPortals.Add(
                new Portal(startPos + new Vector2(0, 100), Portal.Destination.Nexus)
            );

            pathfindingController = new DungeonPathfindingController(dungeonMap);
            dungeonEnemySpawner = new DungeonEnemySpawner(dungeonMap, pathfindingController);
        }

        private Vector2 RoomCenterWorldPosition(Rectangle room) =>
            new(
                room.Center.X * dungeonMap.TileSet.TileWidth,
                room.Center.Y * dungeonMap.TileSet.TileHeight
            );

        protected override void DrawBackground(SpriteBatch spriteBatch) =>
            dungeonMap.Draw(spriteBatch, Game1.GetWorldBounds(1.1f), tileAtlas);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Player.Instance.Position = dungeonMap.ResolveCircleCollision(
                Player.Instance.Position,
                Player.Instance.Radius
            );
            Game1.Camera.Pos = Player.Instance.Position;

            foreach (Enemy enemy in EntityManager.OfEnemyType<Enemy>())
            {
                enemy.Position = dungeonMap.ResolveCircleCollision(enemy.Position, enemy.Radius);
            }

            ApplyTileEffects(dungeonMap.TileAtWorldPosition(Player.Instance.Position));

            dungeonEnemySpawner.Update();
            pathfindingController.Update();
        }

        // HarmsPlayer/SlowsPlayer/AppliedDebuffs all reuse existing player
        // mechanics (Hit(), the Slow debuff via Player.Slow(), the generic
        // debuff system) — no new player-side systems needed here, just a
        // new place that calls into them for whatever tile the player is
        // currently standing on. CanShootThrough/IsDestructible are
        // schema-only in this plan (see the walled-dungeon plan's "Follow-up
        // work" section) — not read here.
        private void ApplyTileEffects(TileDefData tile)
        {
            if (tile.HarmsPlayer && tile.DamagePerSecond > 0f)
            {
                tileDamageAccumulator += tile.DamagePerSecond / 60f;
                if (tileDamageAccumulator >= 1f)
                {
                    int damage = (int)tileDamageAccumulator;
                    Player.Instance.Hit(damage);
                    tileDamageAccumulator -= damage;
                }
            }
            else
            {
                tileDamageAccumulator = 0f;
            }

            if (tile.SlowsPlayer)
            {
                // The only existing Slow mechanic is a fixed 0.5x speed
                // multiplier (Player.cs) — reused as-is rather than adding a
                // second, tile-specific slow amount; a short duration
                // refreshed every frame the player stands on the tile, same
                // "refresh, don't stack" contract ApplyDebuff() already has.
                Player.Instance.Slow(durationFrames: 10);
            }

            foreach (Entity.DebuffType debuff in tile.AppliedDebuffs)
            {
                Player.Instance.ApplyDebuff(debuff, durationFrames: 10);
            }
        }
    }
}
