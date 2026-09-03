using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit — realmeye.com/wiki/greater-pit-viper. Same wander/tether/
    // self-AoE shape as GreaterPitSnake, but a 3-pellet spread (Spray)
    // instead of a random single shot, and its bombs land near a random
    // point ~6 tiles away instead of exactly on the player. The wiki gives
    // 3 different damage/speed values across the spread's own pellets
    // (70/50/40, at increasing speed) — simplified to one representative
    // middle value (Spray() fires a uniform pellet count/damage/speed per
    // call), same "close enough" simplification this session's other
    // bosses/enemies already use for non-uniform wiki attack tables.
    class GreaterPitViper : Enemy
    {
        // Not Enemy's own private rand — that field isn't visible to a
        // subclass (private, not protected), same reason Bandit.cs/
        // BeachedBuccaneer.cs each needed their own.
        private static readonly Random rand = new();

        public GreaterPitViper(Vector2 position)
            : base(Art.GreaterPitViper, position)
        {
            health = 500;
            healthMax = 500;
            Defense = 10;
            PointValue = 250;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(MoveTethered());
            AddAttackBehaviour(
                Spray(
                    projectileSpeed: 7f * 32f / 60f,
                    projectileAmount: 3,
                    damage: 50,
                    projectileImage: Art.SnakeBite
                )
            );
            AddAttackBehaviour(
                ThrowGrenades(
                    damage: 65,
                    radius: 2f * 32f,
                    cooldownFrames: 110,
                    targetPosition: () =>
                        Position + Extensions.FromPolar(rand.NextFloat(0, MathHelper.TwoPi), 6f * 32f)
                )
            );
            AddAttackBehaviour(
                ThrowGrenades(
                    damage: 65,
                    radius: 2f * 32f,
                    cooldownFrames: 130,
                    targetPosition: () => Position
                )
            );
        }
    }
}
