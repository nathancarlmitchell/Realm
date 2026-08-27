namespace Realm
{
    // Single global knob scaling all enemy-to-player damage, so tuning
    // overall difficulty doesn't require touching every individual enemy's
    // attack damage number one at a time. Applied in Player.Hit() — see
    // that method's own comment for exactly where in the damage pipeline
    // it's applied.
    //
    // Raised to 2x per the user's own direct playtest feedback: even a
    // fresh Level 1 character felt too easy, since the player could largely
    // tank hits and lean on leveling/HP regen instead of needing to dodge.
    // Doubling incoming damage is meant to push play toward actually
    // dodging attacks rather than trading hits. First-pass value — expect
    // this to get retuned after the next playthrough, especially once
    // enemy HP values are also revisited (see BACKLOG.md).
    //
    // A future "Hardcore" mode (see BACKLOG.md's Open ideas) is expected to
    // raise this further for players who opt in, on top of whatever this
    // baseline ends up tuned to.
    public static class Difficulty
    {
        public const float EnemyDamageMultiplier = 2f;

        // Single global knob scaling every enemy's FollowPlayer() chase
        // acceleration (Enemy.cs) — applied once inside FollowPlayer()
        // itself rather than retuning each of its ~10 call sites (Seeker,
        // Brute, Pirate, Bandit, Piratess, Limon, and several
        // bosses/mini-bosses each pass their own baked-in acceleration).
        // Reported directly from a playtest: enemies moving toward the
        // player felt a little slow. 1.4x is a first-pass guess — expect
        // retuning after the next playthrough, same as EnemyDamageMultiplier
        // above.
        public const float EnemyChaseSpeedMultiplier = 1.4f;

        // Single global knob scaling every enemy's health/healthMax —
        // applied once, at spawn time, via Enemy.ApplyHealthDifficultyScaling()
        // (called from EntityManager.AddEntity() for every Enemy-typed
        // entity) rather than retuning every individual factory/boss's own
        // health/healthMax values by hand. Requested directly by the user
        // as its own explicit ask, independent of EnemyDamageMultiplier/
        // EnemyChaseSpeedMultiplier above (those came from playtest
        // feedback about feel; this is a deliberate "make enemies tankier"
        // dial). 2x to start — expect retuning after a playthrough, same
        // as the other two.
        public const float EnemyHealthMultiplier = 2f;
    }
}
