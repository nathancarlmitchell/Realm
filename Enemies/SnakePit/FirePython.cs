using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit — realmeye.com/wiki/fire-python. "Lunges at the nearest
    // player, firing off a 4-shot shotgun if close enough" — FollowPlayer
    // (aggressive) + Spray's range parameter (only fires within range,
    // unlike Spray()'s default unconditional firing). "Wavy shots" visual
    // flourish skipped (plain EnemyProjectile via Spray, not
    // WavyProjectile) — a deliberate simplification, not an oversight.
    class FirePython : Enemy
    {
        public FirePython(Vector2 position)
            : base(Art.FirePython, position)
        {
            health = 200;
            healthMax = 200;
            Defense = 5;
            PointValue = 70;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(FollowPlayer(0.5f));
            AddAttackBehaviour(
                Spray(
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileAmount: 4,
                    damage: 35,
                    projectileImage: Art.SnakeBite,
                    range: 13f * 32f
                )
            );
        }
    }
}
