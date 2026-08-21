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

        // protected set: a subclass (e.g. Boss/LimonTheSpriteGoddess) sets
        // these directly in its own constructor, same as Wizard/Archer/
        // Knight already do with baseHealth/baseMana/etc.
        public int PointValue { get; protected set; }

        // Flat reduction applied to incoming damage in WasShot() below,
        // floored at 0 (can't turn a hit into healing). Defaults to 0 for
        // every enemy unless a factory/subclass sets it explicitly.
        public int Defense { get; protected set; } = 0;

        private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();
        private List<IEnumerator<int>> attackBehaviours = new List<IEnumerator<int>>();

        // protected: a subclass constructor sets these directly to
        // establish its stats, same as every factory method below already
        // does via object-initializer syntax from within Enemy itself.
        protected int health;
        protected int healthMax;

        // Fraction of max health remaining, 0-1 — a convenience shorthand
        // for phase-transition logic (e.g. LimonTheSpriteGoddess) so
        // callers don't have to repeat the healthMax > 0 guard by hand.
        protected float HealthFraction => healthMax > 0 ? (float)health / healthMax : 0f;

        protected SoundEffect deathSound;
        protected SoundEffect hitSound;
        public List<Guid> HitBy;

        // Which boss arena's portal (if any) this enemy drops on death, on
        // top of its normal loot roll — null for every enemy except the
        // specific ones a factory below wires up (CreateSpriteGod(),
        // CreateBigSnake()). One field instead of a bool per boss, since a
        // second real instance of "enemy X drops boss Y's portal" showed up
        // (mirrors this session's Portal.Destination enum->class cleanup).
        protected Portal.Destination portalDropOnDeath;

        // Invulnerable enemies take zero damage from WasShot() below — a
        // true no-op, not just reduced damage. Used by boss phase-transition
        // windows (e.g. Stheno between phases); false (damageable) for every
        // enemy unless a subclass explicitly sets it.
        protected bool Invulnerable;

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

        // Permanent tint applied to this enemy's sprite, multiplied into
        // the same fade-in alpha every enemy already gets on spawn (see
        // Update() below) — lets a factory reuse an existing texture as a
        // visually distinct variant (a color-swapped "tougher cousin") with
        // its own stats/behaviors, without needing new art. White (the
        // default) means no change from today's plain sprite.
        protected Color tint = Color.White;

        // Brief red blink-flash cue, meant for a boss announcing a phase
        // transition (e.g. entering an "enraged" phase — see
        // LimonTheSpriteGoddess.PhaseWatcher()) — call FlashRed() once at
        // the moment the transition happens. Ticked in Update(), applied in
        // Draw() below as a one-frame color swap rather than mutating
        // `color` itself, since that field also carries the spawn fade-in
        // alpha (see the constructor/Update() below) and permanently
        // overwriting it would clobber that.
        private int blinkTicksRemaining;
        private int blinkPeriodFrames;
        private bool blinkOn;

        // blinkCount full on/off cycles, each half lasting periodFrames —
        // e.g. the defaults (3, 8) blink on/off 3 times over 48 frames
        // (0.8s at 60fps).
        protected void FlashRed(int blinkCount = 3, int periodFrames = 8)
        {
            blinkTicksRemaining = blinkCount * 2 * periodFrames;
            blinkPeriodFrames = periodFrames;
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

            if (blinkTicksRemaining > 0)
            {
                blinkTicksRemaining--;
                blinkOn = (blinkTicksRemaining / blinkPeriodFrames) % 2 == 0;
            }
            else
            {
                blinkOn = false;
            }

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
                color = tint * (1 - timeUntilStart / 60f);
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

        // virtual: Boss overrides this to a no-op, since a boss's health is
        // shown by the dedicated top-of-screen bar (BossRealmState.
        // DrawBossHud()) instead — drawing both would be redundant.
        public virtual void DrawHealthBars(SpriteBatch spriteBatch)
        {
            if (health < healthMax)
            {
                // Position is scaled by drawScale (Entity.Draw() draws the
                // sprite that much bigger around the same center) — without
                // matching it here, a scaled-up enemy's actual on-screen
                // edges extend past where this assumes, so the bar ends up
                // underneath the sprite instead of below it.
                float x = Position.X - (this.image.Width * drawScale / 4);
                float y = Position.Y + (this.image.Height * drawScale / 2);

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

            if (blinkOn)
            {
                // One-frame color swap rather than overwriting `color`
                // itself — `color` also carries the spawn fade-in alpha
                // (see Update() above), so mutating it here would clobber
                // that instead of just layering a flash on top of it.
                Color original = color;
                color = Color.Red;
                base.Draw(spriteBatch);
                color = original;
            }
            else
            {
                base.Draw(spriteBatch);
            }

            DrawDebuffIndicators(spriteBatch);
        }

        public void HandleCollision(Enemy other)
        {
            var d = Position - other.Position;
            Velocity += 10 * d / (d.LengthSquared() + 1);
        }

        public void WasShot(int damage)
        {
            if (Invulnerable)
                return;

            Debug.WriteLine(damage);

            int actualDamage = Math.Max(0, damage - Defense);
            health -= actualDamage;

            EntityManager.Add(new DamageNumber(Position, actualDamage, Color.Yellow));

            if (health <= 0)
            {
                Sound.Play(deathSound, 0.4f);
                IsExpired = true;
                Player.Instance.ExperienceTotal += PointValue;

                // Spawn loot — SpawnLoot() is virtual, so Boss subclasses
                // (guaranteed good loot) override this; every other enemy
                // uses the base implementation's normal random-chance table.
                // Gated on DropsLoot so a specific enemy (e.g. a boss's
                // non-loot-dropping pet/add) can opt out entirely, without
                // affecting anything else.
                if (DropsLoot)
                    SpawnLoot();

                // Some enemies additionally drop a portal into a boss arena,
                // on top of their normal loot above.
                if (portalDropOnDeath != null)
                {
                    Portal.DroppedPortals.Add(new Portal(this.Position, portalDropOnDeath));
                    Sound.Play(Sound.LootAppears, 0.4f);
                }
            }
            else
            {
                Sound.Play(hitSound, 0.5f);
            }
        }

        // Default: the normal random-chance drop table every enemy uses.
        // Boss overrides this for guaranteed good loot instead. PointValue
        // already ranks enemies by toughness (higher = more score for
        // killing it), so it doubles as the difficulty signal ItemSpawner
        // scales drop chance/tier off — no separate difficulty field needed.
        // Whether this even gets called at all is gated by DropsLoot below,
        // not by anything in here.
        protected virtual void SpawnLoot()
        {
            ItemSpawner.Spawn(
                this.Position,
                PointValue,
                DropPool,
                DropWeights,
                DropTierRanges,
                StatPotionPool,
                GuaranteedPotionChances
            );
        }

        // Whether this enemy drops anything on death at all (SpawnLoot()
        // above only runs when true — see WasShot()). True for every enemy
        // by default; a specific enemy (e.g. a boss's pet/add, meant to be
        // a disposable obstacle rather than a source of loot) sets this
        // false in its own constructor.
        protected bool DropsLoot = true;

        // Which loot categories this enemy's drop table (SpawnLoot() above)
        // rolls against at all — the "real drop pool" backlog item's first
        // real lever. Defaults to every category (today's existing
        // behavior, unchanged for any enemy that doesn't opt into a
        // narrower pool); a specific factory below can set this to exclude
        // categories that don't fit that enemy's theme (e.g. CreateSnake()
        // below drops gear only, no potions).
        protected ItemSpawner.LootCategory DropPool = ItemSpawner.LootCategory.All;

        // Per-category chance multiplier layered on top of DropPool above —
        // the backlog's "with its own odds" half. Empty by default (every
        // category implicitly weight 1.0, i.e. today's unweighted rate,
        // unchanged for any enemy that doesn't opt in); a specific factory
        // below can raise or lower individual categories, e.g. CreateBigSnake()
        // leaning toward potions without excluding gear the way DropPool would.
        protected Dictionary<ItemSpawner.LootCategory, float> DropWeights = new();

        // Absolute tier range to roll a given category's dropped items
        // from, bypassing the PointValue/player-tier formula
        // (ResolveDropTier()) entirely for that category — the direct
        // "what tier of gear can this enemy drop" lever. Per-category
        // (e.g. Weapon at tier 7-10, Ring at tier 3-4 on the same enemy),
        // not one shared range — keyed by LootCategory, empty by default
        // (a category with no entry falls back to the existing
        // PointValue-driven behavior, unchanged for any enemy that doesn't
        // opt in for that category). Min/Max are inclusive; Min must be
        // <= Max, since a range where it isn't has no valid roll.
        protected Dictionary<ItemSpawner.LootCategory, (int Min, int Max)> DropTierRanges = new();

        // Which specific stat potions (Attack/Defense/Dexterity/Life/
        // ManaMax/Speed/Vitality/Wisdom) a StatPotion drop can roll from —
        // narrows ItemSpawner.RollStatPotion()'s selection the same way
        // DropPool narrows categories, just one level deeper (inside the
        // category rather than across categories). Null/empty (the
        // default) rolls uniformly from all 8, today's unchanged behavior.
        // Has no effect on an enemy that sets GuaranteedPotionChances below
        // — that mechanism replaces the single-roll selection this narrows.
        protected List<Potions> StatPotionPool = null;

        // Independent per-potion drop chance (0.0-1.0), one entry per
        // specific stat potion type — a different shape from
        // StatPotionPool above: that's one roll picking one type out of an
        // allowed set (mutually exclusive), this is N separate rolls, one
        // per entry, each able to succeed independently — so a kill can
        // drop several of them at once (e.g. a guaranteed Dexterity potion
        // at 1.0 alongside an independent 25% chance at a Defense potion,
        // and both landing on the same kill is possible). Empty by default
        // (the existing single-roll StatPotionPool behavior, unaffected);
        // setting this on an enemy entirely replaces that enemy's normal
        // StatPotion roll rather than adding to it.
        protected Dictionary<Potions, float> GuaranteedPotionChances = new();

        protected void AddBehaviour(IEnumerable<int> behaviour)
        {
            behaviours.Add(behaviour.GetEnumerator());
        }

        protected void AddAttackBehaviour(IEnumerable<int> behaviour)
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

        protected IEnumerable<int> FollowPlayer(float acceleration = 0.5f)
        {
            while (true)
            {
                Velocity += (Player.Instance.Position - Position).ScaleTo(acceleration);
                yield return 0;
            }
        }

        protected IEnumerable<int> MoveSnake(float speed = 0.2f)
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

        // Same weaving randomness as MoveSnake above, but leashed to a
        // radius around wherever the enemy spawned. updateChance is a
        // per-frame probability (0-1) of picking a new direction, rolled
        // independently each frame, unlike MoveSnake's fixed 10-frame
        // cadence — the 0.1f default averages out to roughly the same turn
        // frequency.
        //
        // The boundary check predicts against Velocity (this enemy's real,
        // already-accumulating momentum — see Update() below, which decays
        // it by 0.8x/frame rather than resetting it) plus this frame's new
        // step, not just the new step alone — checking the step alone would
        // let several frames of sustained outward movement build up enough
        // carried momentum to blow well past wanderDistance before a
        // same-frame direction change could catch up. Once that predicted
        // position would cross the boundary, Velocity is zeroed (killing
        // the outward momentum outright) and direction points straight back
        // at origin, so the leash actually holds instead of just slowing
        // the drift.
        protected IEnumerable<int> MoveTethered(
            float wanderDistance = 300f,
            float speed = 0.2f,
            float updateChance = 0.1f
        )
        {
            Vector2 origin = Position;
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                if (rand.NextDouble() < updateChance)
                {
                    direction += rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                    direction = MathHelper.WrapAngle(direction);
                }

                Vector2 candidateVelocity = Velocity + Extensions.FromPolar(direction, speed);
                if (
                    Vector2.DistanceSquared(Position + candidateVelocity, origin)
                    > wanderDistance * wanderDistance
                )
                {
                    direction = (origin - Position).ToAngle();
                    Velocity = Vector2.Zero;
                }

                Velocity += Extensions.FromPolar(direction, speed);
                yield return 0;
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

        protected IEnumerable<int> Spray(
            int projectileSpeed = 3,
            int projectileAmount = 5,
            int damage = 10,
            Texture2D projectileImage = null,
            Entity.CollisionShape collisionShape = Entity.CollisionShape.Circle
        )
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
                    for (var i = 0; i < projectileAmount; i++)
                    {
                        Vector2 vel = Extensions.FromPolar(
                            aimAngle + randomSpread + (i * bulletOffset),
                            projectileSpeed
                        );

                        EntityManager.Add(
                            new EnemyProjectile(Position, vel, projectileImage)
                            {
                                Damage = damage,
                                Shape = collisionShape,
                            }
                        );
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

        // Mid-level — a tougher, faster-closing cousin of Wanderer. Reuses
        // Wanderer's sprite tinted orange-red (no new art), a step up from
        // both Wanderer and Seeker in health/PointValue, and a movement +
        // attack combo ("rush the player, then burst") not used by any
        // existing enemy: FollowPlayer (as fast as Seeker's own chase) paired
        // with Bomb (previously only ever paired with MoveRandomly/MoveSnake).
        public static Enemy CreateBrute(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy, position)
            {
                health = 300,
                healthMax = 300,
                PointValue = 120,
                tint = Color.OrangeRed,
            };

            enemy.AddBehaviour(enemy.FollowPlayer(0.35f));
            enemy.AddAttackBehaviour(enemy.Bomb(4));
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

                // Low-tier gear only, per the backlog's own example — the
                // weakest trash enemy in the game shouldn't ever hand out a
                // stat potion, just (low-tier — see ItemSpawner.
                // IsWeakEnemy) equipment.
                DropPool = ItemSpawner.LootCategory.Weapon
                    | ItemSpawner.LootCategory.Armor
                    | ItemSpawner.LootCategory.Ring
                    | ItemSpawner.LootCategory.AbilityItem,
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddAttackBehaviour(enemy.Shoot(2));

            return enemy;
        }

        // Low-level — a tougher snake-family sibling, using real art
        // (Art.BigSnake) rather than a tint. Same weaving MoveSnake()
        // movement as the base Snake so it reads as the same family, with
        // more health and a faster Shoot to feel like a real step up rather
        // than a reskin. Grouped with Snake in EnemySpawner.BasicEnemyPool
        // at the same Level 1 requirement, so it can appear in the same
        // waves as Snake from the very start.
        public static Enemy CreateBigSnake(Vector2 position)
        {
            var enemy = new Enemy(Art.BigSnake, position)
            {
                health = 500,
                healthMax = 500,
                PointValue = 250,
                Defense = 10,
                deathSound = Sound.SnakesDeath,
                hitSound = Sound.SnakesHit,
                portalDropOnDeath = Portal.Destination.SthenoBossRealm,

                // Leans toward potions per the backlog's own example — gear
                // stays fully in the pool (DropPool defaults to All, unlike
                // Snake's gear-only exclusion), just weighted less likely to
                // roll relative to potions.
                DropWeights = new()
                {
                    [ItemSpawner.LootCategory.StatPotion] = 2.5f,
                    [ItemSpawner.LootCategory.HealthManaPotion] = 2.5f,
                    [ItemSpawner.LootCategory.Weapon] = 0.5f,
                    [ItemSpawner.LootCategory.Armor] = 0.5f,
                    [ItemSpawner.LootCategory.Ring] = 0.5f,
                    [ItemSpawner.LootCategory.AbilityItem] = 0.5f,
                },
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddAttackBehaviour(enemy.Shoot(3));

            return enemy;
        }

        // Low-level — a slightly sturdier alternative to Snake for variety
        // early on. Reuses Snake's sprite tinted light green (no new art),
        // sitting between Snake and Seeker in both health and PointValue. A
        // movement + attack combo not used by any existing enemy:
        // MoveRandomly (previously only Wanderer's) paired with Spray
        // (previously only Seeker's/bosses') — a slow-wandering blob that
        // sprays when the player gets close, instead of Snake's tight
        // weaving dash-and-shoot.
        public static Enemy CreateSlime(Vector2 position)
        {
            var enemy = new Enemy(Art.Snake, position)
            {
                health = 20,
                healthMax = 20,
                PointValue = 4,
                tint = Color.LightGreen,
            };

            enemy.AddBehaviour(enemy.MoveRandomly());
            enemy.AddAttackBehaviour(enemy.Spray(2, 3, damage: 8));

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
                portalDropOnDeath = Portal.Destination.BossRealm,
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddBehaviour(enemy.Bomb(10));

            return enemy;
        }

        #endregion
    }
}
