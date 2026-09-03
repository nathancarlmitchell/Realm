using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Owns the Snake Pit Treasure Room encounter for one dungeon instance —
    // same "one small controller class" shape as DungeonPathfindingController/
    // DungeonEnemySpawner. Constructed by DungeonState only when
    // DungeonTypeData.TreasureRoomChance rolls true and a spare room
    // exists; does nothing to the room's own tile carving (see
    // TreasureRoomChance's own doc comment) — just spawns a button and 6
    // dormant Dart Throwers up front, and the Guard once the button
    // triggers.
    public class TreasureRoomController
    {
        private readonly List<SnakepitDartThrower> dartThrowers = new();
        private readonly Rectangle roomBoundsWorld;

        public TreasureRoomController(Rectangle roomBoundsWorld)
        {
            this.roomBoundsWorld = roomBoundsWorld;

            Vector2 center = new(roomBoundsWorld.Center.X, roomBoundsWorld.Center.Y);

            // "A red button... at the top" — placed at the room's own top
            // edge, horizontally centered.
            Vector2 buttonPosition = new(center.X, roomBoundsWorld.Top + 24f);
            EntityManager.Add(new TreasureRoomButton(buttonPosition, OnButtonActivated));

            // 6 Dart Throwers spread evenly around the room's perimeter —
            // the wiki's own "along the walls," not tied to any specific
            // wall-by-wall count.
            const int dartThrowerCount = 6;
            for (int i = 0; i < dartThrowerCount; i++)
            {
                float t = i / (float)dartThrowerCount;
                Vector2 position = PerimeterPoint(roomBoundsWorld, t);
                var dartThrower = new SnakepitDartThrower(position);
                dartThrowers.Add(dartThrower);
                EntityManager.Add(dartThrower);
            }
        }

        // Walks the rectangle's own perimeter at fraction t (0-1, wrapping)
        // — a simple way to spread a fixed count of points evenly around
        // any room's edge regardless of its exact width/height.
        private static Vector2 PerimeterPoint(Rectangle bounds, float t)
        {
            float perimeter = 2f * (bounds.Width + bounds.Height);
            float distance = t * perimeter;

            if (distance < bounds.Width)
                return new Vector2(bounds.Left + distance, bounds.Top);
            distance -= bounds.Width;

            if (distance < bounds.Height)
                return new Vector2(bounds.Right, bounds.Top + distance);
            distance -= bounds.Height;

            if (distance < bounds.Width)
                return new Vector2(bounds.Right - distance, bounds.Bottom);
            distance -= bounds.Width;

            return new Vector2(bounds.Left, bounds.Bottom - distance);
        }

        // The button's own one-shot activation callback — spawns the Guard
        // at the room's center and activates every Dart Thrower, handing
        // each one the Guard reference it needs to know when to stop
        // firing.
        private void OnButtonActivated()
        {
            Vector2 center = new(roomBoundsWorld.Center.X, roomBoundsWorld.Center.Y);
            var guard = new SnakepitGuard(center, roomBoundsWorld);
            EntityManager.Add(guard);

            foreach (var dartThrower in dartThrowers)
                dartThrower.Activate(guard);
        }
    }
}
