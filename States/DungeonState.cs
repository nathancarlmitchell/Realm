using System.Collections.Generic;
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
    //
    // Generic over dungeon type (Data/DungeonTypeData.cs) — which tileset,
    // room-generation rules, eligible enemies, and boss a given instance
    // uses is entirely data-driven via the dungeonTypeName constructor
    // parameter, not hardcoded here. World size (InstanceWorldWidth/Height
    // below) is the one exception, staying a fixed constant shared by every
    // dungeon type — see the walled-dungeon-generalization plan's own
    // scope note for why.
    public class DungeonState : RealmState
    {
        protected override bool SpawnsRegularEnemies => false;
        protected override int InstanceWorldWidth => 3200;
        protected override int InstanceWorldHeight => 3200;

        private readonly DungeonMap dungeonMap;
        private readonly Texture2D tileAtlas;
        private readonly DungeonPathfindingController pathfindingController;
        private readonly DungeonEnemySpawner dungeonEnemySpawner;

        // World position of the boss room's portal — null only if the
        // dungeon generated with just the one (start) room, in which case
        // there's nowhere else to put it. Used by DrawQuestIndicator() below
        // to point an arrow at it.
        private readonly Vector2? bossPortalPosition;

        // Accumulates fractional per-frame tile damage (DamagePerSecond / 60,
        // matching this codebase's existing fixed-60fps-tick convention —
        // see e.g. Player.cs's healthCooldown/manaCooldown regen) so a small
        // DamagePerSecond value isn't silently lost to Hit()'s int rounding.
        private float tileDamageAccumulator;

        public DungeonState(
            Game1 game,
            GraphicsDevice graphicsDevice,
            ContentManager content,
            string dungeonTypeName
        )
            : base(game, graphicsDevice, content)
        {
            // Clean slate, same reasoning as BossRealmState's constructor —
            // nothing from wherever the player just left should follow them
            // into a freshly-generated dungeon.
            EntityManager.Reset();

            // Everything that varies per dungeon type — see Data/
            // DungeonTypeData.cs and the walled-dungeon-generalization plan.
            DungeonTypeData dungeonType = Util.LoadDungeonTypeData(dungeonTypeName);
            TileSetData tileSet = Util.LoadTileSetData(dungeonType.TileSetName);
            dungeonMap = DungeonGenerator.Generate(
                tileSet,
                InstanceWorldWidth / tileSet.TileWidth,
                InstanceWorldHeight / tileSet.TileHeight,
                dungeonType.MinRoomSize,
                dungeonType.MaxRoomSize,
                dungeonType.MinRoomCount,
                dungeonType.MaxRoomCount,
                dungeonType.CorridorWidth
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

            // Boss room: whichever room is farthest (straight-line, room
            // center to room center) from the player's start room — gives
            // the player somewhere to head toward, on the opposite side of
            // the dungeon from where they came in. Skipped entirely for a
            // degenerate one-room dungeon (nowhere else to put it).
            if (dungeonMap.Rooms.Count > 1)
            {
                Rectangle bossRoom = FindFarthestRoom(dungeonMap.Rooms);
                bossPortalPosition = RoomCenterWorldPosition(bossRoom);
                Portal.Destination.BossDestination bossDestination = Portal
                    .Destination
                    .BossesByName[dungeonType.BossName];
                Portal.DroppedPortals.Add(new Portal(bossPortalPosition.Value, bossDestination));
            }

            pathfindingController = new DungeonPathfindingController(dungeonMap);
            dungeonEnemySpawner = new DungeonEnemySpawner(
                dungeonMap,
                pathfindingController,
                EnemySpawner.ResolveFactories(dungeonType.EnemyNames)
            );

            // Every enemy the dungeon will ever have, spawned once, up
            // front — no respawning afterward, so clearing them all actually
            // clears the dungeon.
            dungeonEnemySpawner.SpawnAll();
        }

        private Vector2 RoomCenterWorldPosition(Rectangle room) =>
            new(
                room.Center.X * dungeonMap.TileSet.TileWidth,
                room.Center.Y * dungeonMap.TileSet.TileHeight
            );

        // rooms[0] is always the player's start room (see this constructor
        // above) — returns whichever of the rest has the greatest
        // straight-line distance from it, in tile-space (distance
        // comparisons don't care about the world-space scale factor, so
        // there's no need to convert first). Caller guards rooms.Count > 1.
        private static Rectangle FindFarthestRoom(List<Rectangle> rooms)
        {
            Point start = rooms[0].Center;
            Rectangle farthestRoom = rooms[1];
            float farthestDistSq = Vector2.DistanceSquared(start.ToVector2(), rooms[1].Center.ToVector2());

            for (int i = 2; i < rooms.Count; i++)
            {
                float distSq = Vector2.DistanceSquared(start.ToVector2(), rooms[i].Center.ToVector2());
                if (distSq > farthestDistSq)
                {
                    farthestDistSq = distSq;
                    farthestRoom = rooms[i];
                }
            }

            return farthestRoom;
        }

        protected override void DrawBackground(SpriteBatch spriteBatch) =>
            dungeonMap.Draw(spriteBatch, Game1.GetWorldBounds(1.1f), tileAtlas);

        protected override void DrawQuestIndicator(SpriteBatch spriteBatch)
        {
            if (bossPortalPosition.HasValue)
                Overlay.DrawIndicatorArrowTowards(spriteBatch, bossPortalPosition.Value);
        }

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
