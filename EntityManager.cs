﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm;

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

        private static void AddEntity(Entity entity)
        {
            entities.Add(entity);
            if (entity is Projectile)
                bullets.Add(entity as Projectile);
            else if (entity is Enemy)
                enemies.Add(entity as Enemy);
        }

        public static void Update()
        {
            isUpdating = true;
            EntityManager.HandleCollisions();
            foreach (var entity in entities)
                entity.Update();
            isUpdating = false;
            foreach (var entity in addedEntities)
                AddEntity(entity);
            addedEntities.Clear();
            // remove any expired entities.
            entities = entities.Where(x => !x.IsExpired).ToList();
            bullets = bullets.Where(x => !x.IsExpired).ToList();
            enemies = enemies.Where(x => !x.IsExpired).ToList();
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            foreach (var entity in entities)
                entity.Draw(spriteBatch);
        }

        private static bool IsColliding(Entity a, Entity b)
        {
            float radius = a.Radius + b.Radius;
            return !a.IsExpired
                && !b.IsExpired
                && Vector2.DistanceSquared(a.Position, b.Position) < radius * radius;
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
            // handle collisions between bullets and enemies
            for (int i = 0; i < enemies.Count; i++)
            for (int j = 0; j < bullets.Count; j++)
            {
                if (IsColliding(enemies[i], bullets[j]))
                {
                    enemies[i].WasShot();
                    bullets[j].IsExpired = true;
                }
            }
            // handle collisions between the player and enemies
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsActive && IsColliding(Player.Instance, enemies[i]))
                {
                    //Player.Instance.Kill();
                    enemies.ForEach(x => x.WasShot());
                    break;
                }
            }
        }
    }
}
