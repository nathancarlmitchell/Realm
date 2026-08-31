using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // Cube God's own escort — doesn't fight directly, matching the wiki's
    // own framing ("Overseer" spawns Cube Defender/Cube Blaster rather than
    // attacking itself). Replenished by CubeGod.MaintainOverseers(), and in
    // turn maintains its own small cluster of minions via MaintainMinions()
    // below — the "cube system" the real fight scatters across the arena.
    class CubeOverseer : Enemy
    {
        private static readonly Random rand = new();

        // Orbits/wanders near the live Cube God rather than a fixed point —
        // MoveTethered's own anchor parameter already tracks another
        // Enemy's live Position every frame, no custom orbit method needed
        // (contrast SthenoPet.Orbit(), written before anchor existed).
        private const float WanderDistance = 150f;
        private const float WanderSpeed = 0.1f;

        private const int TargetDefenderCount = 2;
        private const int TargetBlasterCount = 2;
        private const int MinionRespawnIntervalFrames = 450;

        public CubeOverseer(CubeGod owner, Vector2 position)
            : base(Art.CubeOverseer, position)
        {
            health = 800;
            healthMax = 800;
            Defense = 6;
            PointValue = 0;
            DropsLoot = false;

            AddBehaviour(
                MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed, anchor: owner)
            );
            AddBehaviour(MaintainMinions());

            // Spawns its full Defender/Blaster complement immediately, per
            // direct request ("several of the minions spawn instantly in
            // the fight") — same "instant burst, then MaintainX() only
            // handles replacements" shape as
            // ScorpionQueen.MaintainScorpions(). Combined with CubeGod's own
            // constructor spawning every Overseer instantly too, the whole
            // "cube system" is present from the moment the fight starts.
            for (int i = 0; i < TargetDefenderCount; i++)
                EntityManager.Add(new CubeDefender(this, Position + rand.NextVector2(0f, 40f)));
            for (int i = 0; i < TargetBlasterCount; i++)
                EntityManager.Add(new CubeBlaster(this, Position + rand.NextVector2(0f, 40f)));
        }

        // Tops this Overseer's own Defender/Blaster counts back up,
        // throttled to one spawn per type every MinionRespawnIntervalFrames
        // — same shape as ScorpionQueen.MaintainScorpions(), scoped to
        // Owner == this so multiple simultaneous Overseers never count each
        // other's minions (SandsmanKing's two independent MaintainX()
        // coroutines are the same idea for two escort types on one owner).
        private IEnumerable<int> MaintainMinions()
        {
            int defenderCooldownRemaining = MinionRespawnIntervalFrames;
            int blasterCooldownRemaining = MinionRespawnIntervalFrames;

            while (true)
            {
                int missingDefenders =
                    TargetDefenderCount
                    - EntityManager.CountWhere<CubeDefender>(d => d.Owner == this && !d.IsExpired);

                if (missingDefenders > 0)
                {
                    if (defenderCooldownRemaining <= 0)
                    {
                        EntityManager.Add(new CubeDefender(this, Position + rand.NextVector2(0f, 40f)));
                        defenderCooldownRemaining = MinionRespawnIntervalFrames;
                    }
                    else
                    {
                        defenderCooldownRemaining--;
                    }
                }

                int missingBlasters =
                    TargetBlasterCount
                    - EntityManager.CountWhere<CubeBlaster>(b => b.Owner == this && !b.IsExpired);

                if (missingBlasters > 0)
                {
                    if (blasterCooldownRemaining <= 0)
                    {
                        EntityManager.Add(new CubeBlaster(this, Position + rand.NextVector2(0f, 40f)));
                        blasterCooldownRemaining = MinionRespawnIntervalFrames;
                    }
                    else
                    {
                        blasterCooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }
    }
}
