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
        // was, or a rectangle matching RectangleBounds() below for anything
        // that opted into Entity.CollisionShape.Rectangle (e.g. Limon's
        // Spray shots) — so the debug view always matches whichever check
        // IsColliding() actually runs, not just the circle case. Covers
        // player, enemies, and both projectile lists (player-fired bullets
        // and enemy projectiles); loot bags/potions are still left out, per
        // the original request.
        public static void DrawHitboxes(SpriteBatch spriteBatch)
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
        }

        private static void DrawHitbox(SpriteBatch spriteBatch, Entity entity, Color color)
        {
            if (entity.Shape == Entity.CollisionShape.Rectangle)
                DrawHitboxRectangle(spriteBatch, RectangleBounds(entity), color);
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

        private static void DrawHitboxRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
        {
            Vector2 topLeft = new(bounds.Left, bounds.Top);
            Vector2 topRight = new(bounds.Right, bounds.Top);
            Vector2 bottomRight = new(bounds.Right, bounds.Bottom);
            Vector2 bottomLeft = new(bounds.Left, bounds.Bottom);

            DrawHitboxLine(spriteBatch, topLeft, topRight, color);
            DrawHitboxLine(spriteBatch, topRight, bottomRight, color);
            DrawHitboxLine(spriteBatch, bottomRight, bottomLeft, color);
            DrawHitboxLine(spriteBatch, bottomLeft, topLeft, color);
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

            // Rectangle collision wins if either side opts into it (e.g. a
            // wide beam-shaped projectile) — circle-vs-circle (the original,
            // still the default for everything that doesn't set Shape) only
            // applies when both sides are Circle.
            if (a.Shape == Entity.CollisionShape.Rectangle || b.Shape == Entity.CollisionShape.Rectangle)
                return RectangleBounds(a).Intersects(RectangleBounds(b));

            float radius = a.Radius + b.Radius;
            return Vector2.DistanceSquared(a.Position, b.Position) < radius * radius;
        }

        // A box centered on Position, sized to the entity's actual sprite —
        // and, critically, adjusted for Orientation. Entity.Draw() rotates
        // the sprite by Orientation (e.g. EnemyProjectile sets it from
        // Velocity.ToAngle(), so a projectile visually rotates to face its
        // travel direction); a box built from raw Width/Height without
        // accounting for that would stay axis-aligned to the *image*, not
        // to what's actually on screen — for a non-square sprite like
        // limon1.png (40x10), a shot traveling near-vertically renders
        // ~10 wide x 40 tall but a naive box would still be computed as
        // 40 wide x 10 tall, backwards from what's drawn. Uses the standard
        // closed-form AABB-of-a-rotated-rectangle formula (half-extent
        // along each world axis = sum of both local half-extents projected
        // onto that axis) rather than manually rotating and re-bounding all
        // 4 corners, since it's algebraically equivalent and simpler.
        // Orientation 0 reduces to the original unrotated box exactly (cos
        // 1, sin 0), so nothing changes for axis-aligned sprites/whatever
        // doesn't rotate.
        //
        // Not the same as Entity.Bounds (which anchors Position at the
        // rectangle's top-left corner, not its center, and also doesn't
        // account for rotation) — collision needs the box centered the
        // same way the circular Radius check already treats Position, so
        // it's computed directly here instead of reusing Entity.Bounds.
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
                    enemies[i].WasShot(bullets[j].Damage);
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
                if (IsColliding(Player.Instance, enemiesProjectiles[i]))
                {
                    Player.Instance.Hit(enemiesProjectiles[i].Damage);
                    enemiesProjectiles[i].IsExpired = true;
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
