using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm;
using Realm.States;

namespace Realm
{
    static class EntityManager
    {
        static List<Entity> entities = new List<Entity>();
        static bool isUpdating;
        static List<Entity> addedEntities = new List<Entity>();
        public static int Count
        {
            get { return entities.Count; }
        }

        public static void Add(Entity entity)
        {
            if (!isUpdating)
                AddEntity(entity);
            else
                addedEntities.Add(entity);
        }

        static List<Enemy> enemies = new List<Enemy>();

        // Whichever boss is currently alive in this dungeon instance, if
        // any — used by the boss arena's HUD (BossRealmState.DrawBossHud())
        // to show a name+health bar. null outside a boss fight, since
        // BossRealmState is the only place a Boss ever gets added.
        public static Boss ActiveBoss => enemies.OfType<Boss>().FirstOrDefault();

        // General-purpose typed-and-filtered count, e.g. "how many
        // SthenoPets are alive" (predicate: _ => true) or "how many
        // SthenoSwarms are currently chasing" (predicate: s => s.IsChasing)
        // — same OfType<T>() idiom ActiveBoss above already uses internally,
        // just exposed generically since enemies itself isn't public.
        public static int CountWhere<T>(Func<T, bool> predicate)
            where T : Enemy => enemies.OfType<T>().Count(predicate);

        // Positions only, not the Enemy objects themselves — this is all
        // Overlay's minimap needs, so it doesn't need broader access to the
        // private enemies list.
        public static IEnumerable<Vector2> EnemyPositions => enemies.Select(e => e.Position);
        static List<Projectile> bullets = new List<Projectile>();
        static List<EnemyProjectile> enemiesProjectiles = new List<EnemyProjectile>();
        static List<Item> potions = new List<Item>();

        private static void AddEntity(Entity entity)
        {
            entities.Add(entity);
            if (entity is Projectile)
                bullets.Add(entity as Projectile);
            else if (entity is Enemy)
                enemies.Add(entity as Enemy);
            else if (entity is EnemyProjectile)
                enemiesProjectiles.Add(entity as EnemyProjectile);
            else if (entity is Item)
                potions.Add(entity as Item);
        }

        public static void Update()
        {
            isUpdating = true;
            EntityManager.HandleCollisions();
            foreach (var entity in entities)
            {
                // An entity expired between frames (e.g. RemovePlayer()/Reset()) shouldn't
                // get one more tick of behavior — that's what let a just-deselected
                // character's weapon fire once when switching classes.
                if (!entity.IsExpired)
                    entity.Update();
            }
            isUpdating = false;
            foreach (var entity in addedEntities)
                AddEntity(entity);
            addedEntities.Clear();

            // remove any expired entities.
            entities = entities.Where(x => !x.IsExpired).ToList();
            bullets = bullets.Where(x => !x.IsExpired).ToList();
            enemies = enemies.Where(x => !x.IsExpired).ToList();
            enemiesProjectiles = enemiesProjectiles.Where(x => !x.IsExpired).ToList();
            potions = potions.Where(x => !x.IsExpired).ToList();
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            // Draw the player last so it always renders above projectiles,
            // enemies, and other ground clutter, regardless of the order
            // entities happened to be added in (SpriteSortMode.Deferred draws
            // in submission order, so a projectile spawned after the player
            // would otherwise paint right over it).
            foreach (var entity in entities)
            {
                if (entity is not Player)
                    entity.Draw(spriteBatch);
            }

            foreach (var entity in entities)
            {
                if (entity is Player)
                    entity.Draw(spriteBatch);
            }
        }

        // Debug-only (F3, Game1._Debug). Draws an outline matching each
        // entity's actual collision shape — a circle for the (still
        // default) Circle case, accurate to Radius the same way it always
        // was, or the true rotated rectangle (DrawHitboxRotatedRectangle()
        // below) for anything that opted into Entity.CollisionShape.
        // Rectangle (e.g. Limon's Spray shots, Stheno's blades) — so the
        // debug view always matches whichever check IsColliding() actually
        // runs, not just the circle case. Covers player, enemies, and both
        // projectile lists (player-fired bullets and enemy projectiles).
        // Portals aren't Entity subclasses (no Shape/Radius/Width/Height),
        // so their teleport-trigger rectangle is drawn separately here via
        // an optional list — each caller passes whichever portal list is
        // actually valid for its own state (NexusState's fixed portalList,
        // RealmState's Portal.DroppedPortals) rather than this reaching for
        // a shared static, so a leftover list from a previous state never
        // draws stray outlines.
        public static void DrawHitboxes(SpriteBatch spriteBatch, IEnumerable<Portal> portals = null)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.IsExpired)
                    DrawHitbox(spriteBatch, enemy, Color.Red);
            }

            if (!Player.Instance.IsExpired)
                DrawHitbox(spriteBatch, Player.Instance, Color.Lime);

            foreach (var bullet in bullets)
            {
                if (!bullet.IsExpired)
                    DrawHitbox(spriteBatch, bullet, Color.Yellow);
            }

            foreach (var enemyProjectile in enemiesProjectiles)
            {
                if (!enemyProjectile.IsExpired)
                    DrawHitbox(spriteBatch, enemyProjectile, Color.Orange);
            }

            if (portals != null)
            {
                foreach (var portal in portals)
                    DrawHitboxRectangle(spriteBatch, portal.Bounds, Color.Cyan);
            }

            // LootBag is an Entity (so it does have a Shape/Radius), but its
            // real pickup check (LootBag.Update()) never goes through
            // IsColliding() — it hand-rolls a Bounds.Intersects() check
            // directly, bypassing Shape entirely. Drawing the generic
            // Shape-based DrawHitbox() here would show a circle that has
            // nothing to do with the actual test, so this uses the same
            // rectangle outline as Portal's Bounds above instead. Read
            // directly off ItemSpawner.LootBags (unlike the portals param)
            // since that single static list is already correctly scoped to
            // whichever state is current — Game1.ChangeState() clears it via
            // ItemSpawner.Reset() on every transition, so there's no
            // stale-list risk to guard against the way DroppedPortals has.
            foreach (var bag in ItemSpawner.LootBags)
            {
                if (!bag.IsExpired)
                    DrawHitboxRectangle(spriteBatch, bag.Bounds, Color.Magenta);
            }
        }

        private static void DrawHitbox(SpriteBatch spriteBatch, Entity entity, Color color)
        {
            if (entity.Shape == Entity.CollisionShape.Rectangle)
                DrawHitboxRotatedRectangle(spriteBatch, entity, color);
            else
                DrawHitboxCircle(spriteBatch, entity.Position, entity.Radius, color);
        }

        private static void DrawHitboxCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            const int segments = 24;
            Vector2 prev = center + new Vector2(radius, 0);
            for (int i = 1; i <= segments; i++)
            {
                Vector2 next = center + Extensions.FromPolar(i * (MathHelper.TwoPi / segments), radius);
                DrawHitboxLine(spriteBatch, prev, next, color);
                prev = next;
            }
        }

        // Axis-aligned outline for a plain Rectangle — used for Portal's
        // teleport-trigger bounds (see DrawHitboxes' portals param above),
        // which is never rotated, so this doesn't need
        // DrawHitboxRotatedRectangle's orientation math.
        private static void DrawHitboxRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            Vector2 topLeft = new(rect.Left, rect.Top);
            Vector2 topRight = new(rect.Right, rect.Top);
            Vector2 bottomRight = new(rect.Right, rect.Bottom);
            Vector2 bottomLeft = new(rect.Left, rect.Bottom);

            DrawHitboxLine(spriteBatch, topLeft, topRight, color);
            DrawHitboxLine(spriteBatch, topRight, bottomRight, color);
            DrawHitboxLine(spriteBatch, bottomRight, bottomLeft, color);
            DrawHitboxLine(spriteBatch, bottomLeft, topLeft, color);
        }

        // Draws the entity's actual rotated hitbox (its 4 true corners,
        // rotated by Orientation around Position) rather than an
        // axis-aligned box — matches what IsRectangleCircleColliding()
        // below actually checks against, unlike the old AABB-based outline
        // which was visibly looser than the sprite at diagonal angles.
        private static void DrawHitboxRotatedRectangle(SpriteBatch spriteBatch, Entity entity, Color color)
        {
            float halfWidth = entity.Width / 2f;
            float halfHeight = entity.Height / 2f;

            Vector2 right = Extensions.FromPolar(entity.Orientation, halfWidth);
            Vector2 up = Extensions.FromPolar(entity.Orientation + MathHelper.PiOver2, halfHeight);

            Vector2 corner1 = entity.Position + right + up;
            Vector2 corner2 = entity.Position - right + up;
            Vector2 corner3 = entity.Position - right - up;
            Vector2 corner4 = entity.Position + right - up;

            DrawHitboxLine(spriteBatch, corner1, corner2, color);
            DrawHitboxLine(spriteBatch, corner2, corner3, color);
            DrawHitboxLine(spriteBatch, corner3, corner4, color);
            DrawHitboxLine(spriteBatch, corner4, corner1, color);
        }

        private static void DrawHitboxLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            float angle = (float)Math.Atan2(delta.Y, delta.X);
            spriteBatch.Draw(
                Art.HealthBar,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, 1f),
                SpriteEffects.None,
                0f
            );
        }

        private static bool IsColliding(Entity a, Entity b)
        {
            if (a.IsExpired || b.IsExpired)
                return false;

            bool aRect = a.Shape == Entity.CollisionShape.Rectangle;
            bool bRect = b.Shape == Entity.CollisionShape.Rectangle;

            // Both sides opting into Rectangle is a case nothing in the
            // codebase currently exercises (every real Rectangle entity is
            // a projectile, which only ever collides against the player, a
            // Circle) — kept as a same-as-before AABB approximation rather
            // than building full rotated-rectangle-vs-rotated-rectangle
            // (SAT) for a pairing with zero live callers.
            if (aRect && bRect)
                return RectangleBounds(a).Intersects(RectangleBounds(b));

            // The real case: exactly one side is a rotated rectangle (e.g.
            // a thin blade or beam sprite) and the other is a circle (the
            // player). Uses the true rotated hitbox, not an axis-aligned
            // approximation — see IsRectangleCircleColliding() below.
            if (aRect)
                return IsRectangleCircleColliding(a, b);
            if (bRect)
                return IsRectangleCircleColliding(b, a);

            // A zero-(or negative-)radius circle has no physical footprint,
            // so it can never collide with anything — regardless of the
            // OTHER side's own radius. Without this, "a.Radius + b.Radius"
            // still comes out positive whenever b has any size at all, so a
            // deliberately inert circle (e.g. GrenadeProjectile before it
            // arms) would still register a hit if it happened to spawn
            // exactly on top of something.
            if (a.Radius <= 0 || b.Radius <= 0)
                return false;

            float radius = a.Radius + b.Radius;
            return Vector2.DistanceSquared(a.Position, b.Position) < radius * radius;
        }

        // True oriented-rectangle-vs-circle test — the rectangle's actual
        // rotated silhouette, not an axis-aligned box that balloons larger
        // than the sprite at diagonal angles (see RectangleBounds() below).
        // Transforms the circle's center into the rectangle's own local
        // (unrotated) space by undoing its Orientation, then finds the
        // closest point on the axis-aligned box in that local space and
        // checks the distance to it — the standard closest-point method for
        // circle-vs-OBB collision.
        private static bool IsRectangleCircleColliding(Entity rect, Entity circle)
        {
            if (circle.Radius <= 0)
                return false;

            float halfWidth = rect.Width / 2f;
            float halfHeight = rect.Height / 2f;

            Vector2 delta = circle.Position - rect.Position;
            float cos = (float)Math.Cos(rect.Orientation);
            float sin = (float)Math.Sin(rect.Orientation);

            // Undoes the rectangle's rotation (rotates delta by
            // -Orientation) so the box can be treated as axis-aligned.
            Vector2 local = new(delta.X * cos + delta.Y * sin, -delta.X * sin + delta.Y * cos);

            Vector2 closest = new(
                MathHelper.Clamp(local.X, -halfWidth, halfWidth),
                MathHelper.Clamp(local.Y, -halfHeight, halfHeight)
            );

            return Vector2.DistanceSquared(local, closest) < circle.Radius * circle.Radius;
        }

        // An axis-aligned box that encloses the entity's rotated sprite —
        // NOT the entity's true rotated silhouette (that's
        // IsRectangleCircleColliding() above, used for the actual
        // rectangle-vs-player collision check and the F3 debug outline).
        // This AABB is only still used as a same-as-before approximation
        // for the rare (currently unreached) case of two Rectangle-shaped
        // entities colliding with each other, since building full
        // rotated-rectangle-vs-rotated-rectangle (SAT) has no live caller
        // to justify it yet. At a diagonal Orientation this box is visibly
        // larger than the sprite — half-extent along each world axis is
        // the sum of both local half-extents projected onto that axis
        // (the standard closed-form AABB-of-a-rotated-rectangle formula,
        // algebraically equivalent to rotating and re-bounding all 4
        // corners but simpler). Orientation 0 reduces to the original
        // unrotated box exactly (cos 1, sin 0).
        //
        // Not the same as Entity.Bounds (which is centered on Position the
        // same way this is, but doesn't account for rotation) — this one
        // exists specifically for the rotated-AABB math above.
        private static Rectangle RectangleBounds(Entity entity)
        {
            float halfWidth = entity.Width / 2f;
            float halfHeight = entity.Height / 2f;

            float cos = Math.Abs((float)Math.Cos(entity.Orientation));
            float sin = Math.Abs((float)Math.Sin(entity.Orientation));

            float rotatedHalfWidth = (halfWidth * cos) + (halfHeight * sin);
            float rotatedHalfHeight = (halfWidth * sin) + (halfHeight * cos);

            return new Rectangle(
                (int)(entity.Position.X - rotatedHalfWidth),
                (int)(entity.Position.Y - rotatedHalfHeight),
                (int)(rotatedHalfWidth * 2),
                (int)(rotatedHalfHeight * 2)
            );
        }

        public static void Reset()
        {
            foreach (var entity in entities)
                if (entity is not Player)
                    entity.IsExpired = true;
        }

        public static void RemovePlayer()
        {
            foreach (var entity in entities)
                if (entity is Player)
                    entity.IsExpired = true;
        }

        static void HandleCollisions()
        {
            // handle collisions between enemies
            for (int i = 0; i < enemies.Count; i++)
            for (int j = i + 1; j < enemies.Count; j++)
            {
                if (IsColliding(enemies[i], enemies[j]))
                {
                    enemies[i].HandleCollision(enemies[j]);
                    enemies[j].HandleCollision(enemies[i]);
                }
            }

            // handle collisions between player projectiles and enemies
            for (int i = 0; i < enemies.Count; i++)
            for (int j = 0; j < bullets.Count; j++)
            {
                bool hit =
                    !enemies[i].HitBy.Contains(bullets[j].ID)
                    && IsColliding(enemies[i], bullets[j]);

                if (hit)
                {
                    enemies[i].HitBy.Add(bullets[j].ID);
                    enemies[i].WasShot(bullets[j].Damage, bullets[j].IgnoresDefense);
                    if (bullets[j].ParalyzesOnHit)
                    {
                        enemies[i].Paralyze();
                    }
                    if (bullets[j].StunsOnHit)
                    {
                        enemies[i].Stun();
                    }
                    if (bullets[j].ExpiresOnHit)
                    {
                        bullets[j].IsExpired = true;
                    }
                }
            }

            // handle collisions between enemy projectiles and player
            for (int i = 0; i < enemiesProjectiles.Count; i++)
            {
                if (
                    !enemiesProjectiles[i].HasHitPlayer
                    && IsColliding(Player.Instance, enemiesProjectiles[i])
                )
                {
                    Player.Instance.Hit(enemiesProjectiles[i].Damage);
                    if (enemiesProjectiles[i].SlowsOnHit)
                    {
                        Player.Instance.Slow();
                    }
                    // Marked regardless of ExpiresOnHit below, so a
                    // non-expiring projectile (e.g. GrenadeProjectile) can
                    // only ever damage the player once, not every frame
                    // they keep overlapping it.
                    enemiesProjectiles[i].HasHitPlayer = true;
                    if (enemiesProjectiles[i].ExpiresOnHit)
                    {
                        enemiesProjectiles[i].IsExpired = true;
                    }
                }
            }

            // handle collisions between player and items
            for (int i = 0; i < potions.Count; i++)
            {
                if (IsColliding(Player.Instance, potions[i]))
                {
                    if (Player.Instance.Inventory.AddItem(potions[i], 1))
                        potions[i].IsExpired = true;
                }
            }
        }
    }
}
