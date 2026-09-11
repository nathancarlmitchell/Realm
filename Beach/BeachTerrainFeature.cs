using Microsoft.Xna.Framework;
using Realm.Data;

namespace Realm
{
    // One scattered terrain patch in the open Realm's Beach ring (a water
    // pool or an obstacle cluster) — see BeachTerrainGenerator.cs. A plain
    // circle in continuous world space, not a tile-grid region — cheap to
    // place, draw, and collide against without needing a full DungeonMap
    // for the whole ring (the Beach ring has no tile grid at all; see
    // RealmState.cs's own doc comment on why).
    class BeachTerrainFeature
    {
        public Vector2 Center;
        public float Radius; // world px

        // The real tile definition this feature's whole circular footprint
        // is filled with — both its sprite (Tile.OffsetX/OffsetY into the
        // Beach tileset atlas) and its gameplay semantics (Tile.
        // CanPassThrough/SlowsPlayer), reused as-is by RealmState's own
        // draw/collision code rather than a separate feature-type enum.
        public TileDefData Tile;

        public BeachTerrainFeature(Vector2 center, float radius, TileDefData tile)
        {
            Center = center;
            Radius = radius;
            Tile = tile;
        }
    }
}
