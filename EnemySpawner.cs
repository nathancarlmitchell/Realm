using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Realm.States;

namespace Realm
{
    static class EnemySpawner
    {
        static Random rand = new Random();
        static float inverseSpawnChance = 60;

        public static void Update()
        {
            if (!Player.Instance.IsExpired && EntityManager.Count < 1000)
            {
                if (rand.Next((int)inverseSpawnChance) == 0)
                    EntityManager.Add(Enemy.CreateSeeker(GetSpawnPosition()));
                if (rand.Next((int)inverseSpawnChance) == 0)
                    EntityManager.Add(Enemy.CreateWanderer(GetSpawnPosition()));
                if (rand.Next((int)inverseSpawnChance) == 0)
                    EntityManager.Add(Enemy.CreateSnake(GetSpawnPosition()));
                if (rand.Next((int)1500) == 0)
                {
                    Debug.WriteLine("rand.Next((int)2500) == 0");
                    EntityManager.Add(Enemy.CreateSpriteGod(GetSpawnPosition()));
                }
            }

            // slowly increase the spawn rate as time progresses
            if (inverseSpawnChance > 20)
                inverseSpawnChance -= 0.005f;
        }

        private static Vector2 GetSpawnPosition()
        {
            Vector2 pos;

            int minSpawnDistance = 250;
            int maxSpawnDistance = 1000;

            float minX = Player.Instance.Position.X - maxSpawnDistance;
            float minY = Player.Instance.Position.Y - maxSpawnDistance;
            float maxX = Player.Instance.Position.X + maxSpawnDistance;
            float maxY = Player.Instance.Position.Y + maxSpawnDistance;
            do
            {
                pos = new Vector2(rand.Next((int)minX, (int)maxX), rand.Next((int)minY, (int)maxY));
            } while (
                Vector2.DistanceSquared(pos, Player.Instance.Position)
                < minSpawnDistance * minSpawnDistance
            );
            return pos;
        }

        public static void Reset()
        {
            inverseSpawnChance = 60;
        }
    }
}
