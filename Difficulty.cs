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
    }
}
