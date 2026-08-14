namespace Realm
{
    // Account-level, persistent progression counter — shared across every
    // class's save, like BankSystem, rather than tied to any one Player
    // instance. Earned when a character's run ends (death or delete), from
    // that character's Score; never resets.
    public static class FameSystem
    {
        public static int Fame;

        // Adds Score-derived Fame from a character whose run just ended
        // (death or delete). No-op on non-positive amounts so a class with 0
        // Score doesn't need a guard at every call site.
        public static void AddFame(int amount)
        {
            if (amount > 0)
                Fame += amount;
        }
    }
}
