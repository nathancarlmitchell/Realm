using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm.Projectiles;

namespace Realm
{
    // The Snake Pit Treasure Room's wall-mounted escort — its own wiki page
    // was deleted ("merged into its parent boss page," per the Snakepit
    // Guard's own attack table), so its only real stats are the dart's own
    // row there: dmg 40, Bleeding for 4s, speed 5t/s, range 15t. No
    // dedicated art exists either — reuses Art.HealthBar tinted, same
    // placeholder-art precedent Cube God's own "cube system" already
    // established. Spawned dormant by TreasureRoomController.cs the moment
    // the room is set up; Activate() is called once the button triggers,
    // and it stops firing the instant the Guard it's escorting dies
    // ("dart throwers stop firing upon the Guard's death").
    class SnakepitDartThrower : Enemy
    {
        // Not Enemy's own private projectileCooldown(Remaining)/rand —
        // those fields aren't visible to a subclass (private, not
        // protected), same reason Bandit.cs/BeachedBuccaneer.cs each needed
        // their own.
        private int cooldownRemaining = 0;
        private const int DartCooldown = 60;
        private const int DartDamage = 40;
        private const float DartSpeed = 5f * 32f / 60f;
        private const float DartRange = 15f * 32f;
        private const int BleedDurationFrames = 240; // 4s

        private bool isActive = false;
        private Enemy guard;

        public SnakepitDartThrower(Vector2 position)
            : base(Art.HealthBar, position)
        {
            health = 1;
            healthMax = 1;
            PointValue = 0;
            drawScale = 10f;
            Radius = 8f;
            tint = Color.DarkSlateGray;

            // Genuinely invincible (not just a high Defense) and drops
            // nothing — it's a fixture of the room, not a real kill.
            Invulnerable = true;
            DropsLoot = false;

            AddAttackBehaviour(FireDarts());
        }

        // Called once by TreasureRoomController.cs when the room's button
        // triggers. guard is checked every tick below so firing stops the
        // instant it dies — passed in here rather than looked up some other
        // way since the controller is the one thing that knows which Guard
        // instance belongs to this room.
        public void Activate(Enemy guard)
        {
            isActive = true;
            this.guard = guard;
        }

        private IEnumerable<int> FireDarts()
        {
            while (true)
            {
                if (isActive && guard != null && !guard.IsExpired)
                {
                    if (cooldownRemaining <= 0)
                    {
                        var aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0 && aim.LengthSquared() <= DartRange * DartRange)
                        {
                            cooldownRemaining = DartCooldown;
                            EntityManager.Add(
                                new EnemyProjectile(Position, aim.ScaleTo(DartSpeed), Art.SnakeBite)
                                {
                                    Damage = DartDamage,
                                    BleedsOnHit = true,
                                    BleedDurationFrames = BleedDurationFrames,
                                }
                            );
                        }
                    }
                    else
                    {
                        cooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }
    }
}
