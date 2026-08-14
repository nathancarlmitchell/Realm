using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    class Enemy : Entity
    {
        private static readonly Random rand = new();
        private int timeUntilStart = 60;
        public bool IsActive
        {
            get { return timeUntilStart <= 0; }
        }
        public int PointValue { get; private set; }

        private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();
        private List<IEnumerator<int>> attackBehaviours = new List<IEnumerator<int>>();

        private int health;
        private int healthMax;
        private SoundEffect deathSound;
        private SoundEffect hitSound;
        public List<Guid> HitBy;

        // Paralyzes this enemy, blocking its movement (Update() below) for
        // durationFrames. Backed by Entity's general debuff system (refreshes
        // on re-trigger rather than stacking, ticked/drawn via
        // UpdateDebuffs()/DrawDebuffIndicators() below). Default is 3 seconds
        // at 60fps.
        public void Paralyze(int durationFrames = 180)
        {
            ApplyDebuff(DebuffType.Paralyzed, durationFrames);
        }

        // Stuns this enemy, blocking its attacks (Update() below) for
        // durationFrames — movement is unaffected, unlike Paralyze() above.
        // Default is 3 seconds at 60fps.
        public void Stun(int durationFrames = 180)
        {
            ApplyDebuff(DebuffType.Stunned, durationFrames);
        }

        public Enemy(Texture2D image, Vector2 position)
        {
            this.image = image;
            Position = position;
            Radius = image.Width / 2f;
            color = Color.Transparent;

            deathSound = Sound.DefaultHit;
            hitSound = Sound.DefaultHit;

            HitBy = [];
        }

        public override void Update()
        {
            UpdateDebuffs();

            if (timeUntilStart <= 0)
            {
                ApplyBehaviours();

                // Attack if on screen, or just slightly off it. Stunned
                // blocks this (Paralyzed doesn't — it only blocks movement
                // below).
                if (
                    !HasDebuff(DebuffType.Stunned)
                    && Game1.GetWorldBounds(1.25f).Contains(Position.ToPoint())
                )
                {
                    ApplyAttackBehaviours();
                }
            }
            else
            {
                timeUntilStart--;
                color = Microsoft.Xna.Framework.Color.White * (1 - timeUntilStart / 60f);
            }

            // Paralyzed enemies keep "thinking" (behaviours above still run
            // and accumulate into Velocity), they just don't act on it —
            // Velocity still decays at its normal rate below, so there's no
            // backlog dump of pent-up motion the instant the paralysis ends.
            // Stunned doesn't block movement — only attacks, above.
            if (!HasDebuff(DebuffType.Paralyzed))
            {
                Position += Velocity;
            }

            Velocity *= 0.8f;

            // Despawn enemies that get too far away.
            if (Vector2.DistanceSquared(Position, Player.Instance.Position) > 25000000)
            {
                Debug.WriteLine(
                    "Despawn distance: "
                        + Vector2.DistanceSquared(Position, Player.Instance.Position)
                );
                IsExpired = true;
            }
        }

        public void DrawHealthBars(SpriteBatch spriteBatch)
        {
            if (health < healthMax)
            {
                float x = Position.X - (this.image.Width / 4);
                float y = Position.Y + (this.image.Height / 2);

                int barScale = 1;
                int barHeight = 8;

                Vector2 healthBarPos = new(x, y);

                // Normalize values.
                int normalisedHealth = (health * 25 / healthMax * 25) / 25;

                // Health bars.
                Rectangle greenRect = new(0, 0, normalisedHealth * barScale, barHeight);
                Rectangle redRect = new(0, 0, 25 * barScale, barHeight);

                // Red bar.
                spriteBatch.Draw(
                    Art.HealthBar,
                    healthBarPos,
                    redRect,
                    Color.DarkRed,
                    0f,
                    Vector2.Zero * 0.25f,
                    1f,
                    0,
                    0
                );

                // Green bar.
                spriteBatch.Draw(
                    Art.HealthBar,
                    healthBarPos,
                    greenRect,
                    Color.DarkGreen,
                    0f,
                    Vector2.Zero,
                    1f,
                    0,
                    0
                );
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawHealthBars(spriteBatch);
            base.Draw(spriteBatch);
            DrawDebuffIndicators(spriteBatch);
        }

        public void HandleCollision(Enemy other)
        {
            var d = Position - other.Position;
            Velocity += 10 * d / (d.LengthSquared() + 1);
        }

        public void WasShot(int damage)
        {
            Debug.WriteLine(damage);
            health -= (int)damage;

            EntityManager.Add(new DamageNumber(Position, damage, Color.Yellow));

            if (health <= 0)
            {
                Sound.Play(deathSound, 0.4f);
                IsExpired = true;
                Player.Instance.Experience += PointValue;
                Player.Instance.ExperienceTotal += PointValue;

                // Spawn loot.
                ItemSpawner.Spawn(this.Position);
            }
            else
            {
                Sound.Play(hitSound, 0.5f);
            }
        }

        private void AddBehaviour(IEnumerable<int> behaviour)
        {
            behaviours.Add(behaviour.GetEnumerator());
        }

        private void AddAttackBehaviour(IEnumerable<int> behaviour)
        {
            attackBehaviours.Add(behaviour.GetEnumerator());
        }

        private void ApplyBehaviours()
        {
            for (int i = 0; i < behaviours.Count; i++)
            {
                if (!behaviours[i].MoveNext())
                    behaviours.RemoveAt(i--);
            }
        }

        private void ApplyAttackBehaviours()
        {
            for (int i = 0; i < attackBehaviours.Count; i++)
            {
                if (!attackBehaviours[i].MoveNext())
                    attackBehaviours.RemoveAt(i--);
            }
        }

        private int projectileCooldownRemaining = 0;
        private readonly int projectileCooldown = 250;

        private int healthCooldownRemaining = 0;
        private readonly int healthCooldown = 250;

        IEnumerable<int> RegenHealth(int amount = 1)
        {
            while (true)
            {
                if (healthCooldownRemaining <= 0)
                {
                    healthCooldownRemaining = healthCooldown - (1 * 1);

                    int heal = health;
                    heal += amount;

                    if (heal >= healthMax)
                    {
                        health = healthMax;
                    }
                    else
                    {
                        health += amount;
                    }
                }

                if (healthCooldownRemaining > 0)
                    healthCooldownRemaining--;

                yield return 0;
            }
        }

        #region Movement Behaviors

        IEnumerable<int> FollowPlayer(float acceleration = 0.5f)
        {
            while (true)
            {
                Velocity += (Player.Instance.Position - Position).ScaleTo(acceleration);
                if (Velocity != Vector2.Zero)
                    Orientation = Velocity.ToAngle();
                yield return 0;
            }
        }

        IEnumerable<int> MoveSnake(float speed = 0.2f)
        {
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                direction += rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                direction = MathHelper.WrapAngle(direction);
                for (int i = 0; i < 10; i++)
                {
                    Velocity += Extensions.FromPolar(direction, speed);
                    yield return 0;
                }
            }
        }

        IEnumerable<int> MoveRandomly()
        {
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                direction += rand.NextFloat(-0.1f, 0.1f);
                direction = MathHelper.WrapAngle(direction);
                for (int i = 0; i < 6; i++)
                {
                    Velocity += Extensions.FromPolar(direction, 0.4f);
                    Orientation -= 0.05f;
                    yield return 0;
                }
            }
        }

        #endregion

        #region Attack Behaviors

        IEnumerable<int> Spray(int projectileSpeed = 3, int projectileAmmount = 5)
        {
            while (true)
            {
                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && projectileCooldownRemaining <= 0)
                {
                    projectileCooldownRemaining = projectileCooldown - (1 * 1);
                    float aimAngle = aim.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);

                    float randomSpread = rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);

                    float bulletOffset = 0.05f;
                    for (var i = 0; i < projectileAmmount; i++)
                    {
                        Vector2 vel = Extensions.FromPolar(
                            aimAngle + randomSpread + (i * bulletOffset),
                            projectileSpeed
                        );

                        EntityManager.Add(new EnemyProjectile(Position, vel));
                    }
                }
                if (projectileCooldownRemaining > 0)
                    projectileCooldownRemaining--;

                yield return 0;
            }
        }

        IEnumerable<int> Shoot(int projectileSpeed = 1)
        {
            while (true)
            {
                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && projectileCooldownRemaining <= 0)
                {
                    projectileCooldownRemaining = projectileCooldown - (1 * 1);
                    float aimAngle = aim.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);
                    float randomSpread = rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);
                    Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, projectileSpeed);
                    EntityManager.Add(new EnemyProjectile(Position, vel));
                }
                if (projectileCooldownRemaining > 0)
                    projectileCooldownRemaining--;

                yield return 0;
            }
        }

        IEnumerable<int> Bomb(int projectileSpeed = 3)
        {
            while (true)
            {
                if (projectileCooldownRemaining <= 0)
                {
                    projectileCooldownRemaining = projectileCooldown - (1 * 1);

                    for (int i = 0; i < 35; i++)
                    {
                        Vector2 vel = Extensions.FromPolar(i * 10, projectileSpeed);
                        EntityManager.Add(new EnemyProjectile(Position, vel) { duration = 50 });
                    }
                }

                if (projectileCooldownRemaining > 0)
                    projectileCooldownRemaining--;

                yield return 0;
            }
        }

        #endregion

        #region Enemy Types

        public static Enemy CreateWanderer(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy, position)
            {
                health = 150,
                healthMax = 150,
                PointValue = 15,
            };

            enemy.AddBehaviour(enemy.MoveRandomly());
            enemy.AddAttackBehaviour(enemy.Bomb());
            enemy.AddBehaviour(enemy.RegenHealth());

            return enemy;
        }

        public static Enemy CreateSeeker(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy2, position)
            {
                health = 50,
                healthMax = 50,
                PointValue = 7,
            };

            enemy.AddBehaviour(enemy.FollowPlayer(0.25f));
            enemy.AddAttackBehaviour(enemy.Spray());

            return enemy;
        }

        public static Enemy CreateSnake(Vector2 position)
        {
            var enemy = new Enemy(Art.Snake, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 2,
                deathSound = Sound.SnakesDeath,
                hitSound = Sound.SnakesHit,
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddAttackBehaviour(enemy.Shoot(2));

            return enemy;
        }

        public static Enemy CreateSpriteGod(Vector2 position)
        {
            var enemy = new Enemy(Art.EnemySpriteGod, position)
            {
                health = 1500,
                healthMax = 1500,
                PointValue = 200,
                deathSound = Sound.SpriteGodDeath,
                hitSound = Sound.SpriteGodHit,
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddBehaviour(enemy.Bomb(10));

            return enemy;
        }

        #endregion
    }
}
