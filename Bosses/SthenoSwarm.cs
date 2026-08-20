using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // One of the 3 adds Stheno summons on every phase change. Charges the
    // player in a straight line (captured once at spawn, never re-aimed)
    // while firing rapid shots forward, then — if a chasing slot is free —
    // switches to chasing with aimed shots. Only MaxChasingSwarms can be
    // chasing at once; a charge that finishes while the cap is full just
    // fizzles instead of queuing.
    class SthenoSwarm : Enemy
    {
        private enum SwarmState
        {
            Charging,
            Chasing,
        }

        private SwarmState state = SwarmState.Charging;

        // What EntityManager.CountWhere<SthenoSwarm>() checks to enforce
        // the chasing cap.
        public bool IsChasing => state == SwarmState.Chasing;

        private readonly Vector2 chargeDirection; // unit vector, captured once at spawn

        private const float ChargeSpeed = 6f;
        private const int ChargeMaxFrames = 60;
        private int chargeFramesElapsed = 0;

        private const float ChaseAcceleration = 0.3f;

        private const int MaxChasingSwarms = 3; // matches spec exactly

        private int chargeFireCooldownRemaining = 0;
        private const int ChargeFireCooldown = 8; // "rapid shots in front of them"
        private const int ChargeShotSpeed = 7;
        private const int ChargeDamage = 15;

        private int chaseFireCooldownRemaining = 0;
        private const int ChaseFireCooldown = 70;
        private const int ChaseShotSpeed = 6;
        private const int ChaseDamage = 20;

        // Safety bound in case a swarm somehow never dies and never fizzles
        // out — nothing lingers forever.
        private int lifetimeFramesRemaining = 1200; // ~20s at 60fps

        public SthenoSwarm(Vector2 position)
            : base(Art.SthenoSwarm, position)
        {
            health = 80;
            healthMax = 80;
            PointValue = 20;

            Vector2 toPlayer = Player.Instance.Position - position;
            chargeDirection = toPlayer.LengthSquared() > 0 ? toPlayer.ScaleTo(1f) : Vector2.UnitX;

            AddBehaviour(Move());
            AddBehaviour(LifetimeTimer());
            AddAttackBehaviour(ChargeFire());
            AddAttackBehaviour(ChaseFire());
        }

        private IEnumerable<int> Move()
        {
            while (true)
            {
                if (state == SwarmState.Charging)
                {
                    Velocity = chargeDirection * ChargeSpeed;
                    chargeFramesElapsed++;
                    if (chargeFramesElapsed >= ChargeMaxFrames)
                        TryTransitionToChase();
                }
                else
                {
                    Vector2 toPlayer = Player.Instance.Position - Position;
                    if (toPlayer.LengthSquared() > 0)
                        Velocity += toPlayer.ScaleTo(ChaseAcceleration);
                }

                yield return 0;
            }
        }

        private void TryTransitionToChase()
        {
            if (EntityManager.CountWhere<SthenoSwarm>(s => s.IsChasing) >= MaxChasingSwarms)
            {
                IsExpired = true;
                return;
            }

            state = SwarmState.Chasing;
        }

        private IEnumerable<int> ChargeFire()
        {
            while (true)
            {
                if (state == SwarmState.Charging)
                {
                    if (chargeFireCooldownRemaining <= 0)
                    {
                        chargeFireCooldownRemaining = ChargeFireCooldown;
                        EntityManager.Add(
                            new EnemyProjectile(
                                Position,
                                chargeDirection * ChargeShotSpeed,
                                Art.SwordSlash
                            )
                            {
                                Damage = ChargeDamage,
                            }
                        );
                    }

                    if (chargeFireCooldownRemaining > 0)
                        chargeFireCooldownRemaining--;
                }

                yield return 0;
            }
        }

        private IEnumerable<int> ChaseFire()
        {
            while (true)
            {
                if (state == SwarmState.Chasing)
                {
                    if (chaseFireCooldownRemaining <= 0)
                    {
                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0)
                        {
                            chaseFireCooldownRemaining = ChaseFireCooldown;
                            EntityManager.Add(
                                new EnemyProjectile(Position, aim.ScaleTo(ChaseShotSpeed), Art.SwordSlash)
                                {
                                    Damage = ChaseDamage,
                                }
                            );
                        }
                    }

                    if (chaseFireCooldownRemaining > 0)
                        chaseFireCooldownRemaining--;
                }

                yield return 0;
            }
        }

        private IEnumerable<int> LifetimeTimer()
        {
            while (true)
            {
                lifetimeFramesRemaining--;
                if (lifetimeFramesRemaining <= 0)
                {
                    IsExpired = true;
                    yield break;
                }

                yield return 0;
            }
        }
    }
}
