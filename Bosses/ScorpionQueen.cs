using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // Beach's third mini-boss — same "dedicated Enemy subclass, no portal/
    // arena" shape as BeachedBuccaneer/BanditLeader. Unlike those two,
    // EnemySpawner.SpawnScorpionQueenPack() drops only her — she manages her
    // own escort of Little Scorpions internally (an immediate burst of 10 on
    // spawn, then a slow trickle to replace losses), rather than
    // EnemySpawner spawning a separate escort pack alongside her the way it
    // does for the other two mini-bosses. She has no attack of her own at
    // all — "Does not attack" is explicit in the spec.
    class ScorpionQueen : Enemy
    {
        private static readonly Random rand = new();

        private const int TargetScorpionCount = 10;

        // "spawn another one slowly" — no explicit cadence given; ~5s
        // between replacement spawns reads as a real trickle rather than an
        // instant top-up (contrast SthenoTheSnakeQueen.MaintainPets(), which
        // tops up every single frame — that spec never said "slowly").
        // Tunable.
        private const int ScorpionRespawnIntervalFrames = 300;

        // "Only wanders in place" — a small tether radius around her own
        // spawn point. Tunable.
        private const float WanderDistance = 60f;
        private const float WanderSpeed = 0.05f;

        public ScorpionQueen(Vector2 position)
            : base(Art.ScorpionQueen, position)
        {
            health = 100;
            healthMax = 100;
            Defense = 0;
            PointValue = 40;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddBehaviour(MaintainScorpions());

            for (int i = 0; i < TargetScorpionCount; i++)
                EntityManager.Add(new LittleScorpion(this, Position + rand.NextVector2(0f, 40f)));
        }

        // Tops the live-scorpion count belonging to THIS Queen back up to
        // TargetScorpionCount, throttled to one spawn every
        // ScorpionRespawnIntervalFrames rather than filling every missing
        // slot at once. Scoped to Owner == this so two live Queens (e.g. a
        // fresh pack spawning while an older Queen's scorpions are still
        // wandering around) never count each other's escorts.
        private IEnumerable<int> MaintainScorpions()
        {
            int cooldownRemaining = ScorpionRespawnIntervalFrames;
            while (true)
            {
                // !s.IsExpired matters here in a way it wouldn't for an
                // unthrottled top-up (e.g. SthenoTheSnakeQueen.MaintainPets()):
                // EntityManager only purges expired entities from its list at
                // the very end of its own Update() pass, after every entity
                // (including this Queen) has already ticked for the frame —
                // so a scorpion that died this same frame is still sitting in
                // the list, IsExpired=true, when this check runs. Without the
                // filter, "missing" reads 0 for one extra frame, which is
                // harmless on its own, but this coroutine also depends on
                // that count staying stable while cooldownRemaining ticks
                // down — not filtering here means the accounting is only
                // correct by accident of call order, not by construction.
                int missing =
                    TargetScorpionCount
                    - EntityManager.CountWhere<LittleScorpion>(s =>
                        s.Owner == this && !s.IsExpired
                    );

                if (missing > 0)
                {
                    if (cooldownRemaining <= 0)
                    {
                        EntityManager.Add(
                            new LittleScorpion(this, Position + rand.NextVector2(0f, 40f))
                        );
                        cooldownRemaining = ScorpionRespawnIntervalFrames;
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
