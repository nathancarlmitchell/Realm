using System.Collections.Generic;
using Realm.Data;

namespace Realm
{
    // Account-level, persistent per-class record of the best HighScore any
    // character of that class has ever achieved — independent of any one
    // character's own save file, same "account-wide, shared across every
    // class's save" reasoning FameSystem.cs already documents for itself.
    // Needed once a class can have multiple characters (or none currently
    // existing): the star-unlock chain (CharacterCreationState.cs) reads
    // this instead of "the one save file for this class"'s HighScore, which
    // stopped being a meaningful concept once a class stopped mapping to
    // exactly one character. Updated live in RealmState.cs's existing
    // HighScore-bump block, alongside HighScore itself, and preserved
    // through character deletion (Util.DeleteCharacterData() no longer
    // needs its own "preserve HighScore" trick because of this).
    public static class ClassRecordSystem
    {
        private static readonly Dictionary<Player.Class, int> best = new()
        {
            { Player.Class.Wizard, 0 },
            { Player.Class.Archer, 0 },
            { Player.Class.Knight, 0 },
            { Player.Class.Priest, 0 },
            { Player.Class.Rogue, 0 },
        };

        public static int GetBestHighScore(Player.Class playerClass) => best[playerClass];

        // Only ever raises the record — a lower HighScore (e.g. a fresh
        // character of a class that already has a high-scoring record
        // elsewhere) never regresses it.
        public static void RecordHighScore(Player.Class playerClass, int highScore)
        {
            if (highScore > best[playerClass])
                best[playerClass] = highScore;
        }

        // Full account wipe only (Util.EraseAllAccountData()) — mirrors
        // FameSystem.Fame's own reset there.
        public static void Reset()
        {
            best[Player.Class.Wizard] = 0;
            best[Player.Class.Archer] = 0;
            best[Player.Class.Knight] = 0;
            best[Player.Class.Priest] = 0;
            best[Player.Class.Rogue] = 0;
        }

        // Data/ClassRecordsData.cs uses flat named fields (matching every
        // other Data/*.cs DTO's style), not a dictionary — these two
        // convert between that on-disk shape and this class's in-memory
        // Dictionary, used only by Util.SaveClassRecordsData()/
        // LoadClassRecordsData().
        public static ClassRecordsData ToData() =>
            new()
            {
                WizardBestHighScore = best[Player.Class.Wizard],
                ArcherBestHighScore = best[Player.Class.Archer],
                KnightBestHighScore = best[Player.Class.Knight],
                PriestBestHighScore = best[Player.Class.Priest],
                RogueBestHighScore = best[Player.Class.Rogue],
            };

        public static void LoadFromData(ClassRecordsData data)
        {
            best[Player.Class.Wizard] = data?.WizardBestHighScore ?? 0;
            best[Player.Class.Archer] = data?.ArcherBestHighScore ?? 0;
            best[Player.Class.Knight] = data?.KnightBestHighScore ?? 0;
            best[Player.Class.Priest] = data?.PriestBestHighScore ?? 0;
            best[Player.Class.Rogue] = data?.RogueBestHighScore ?? 0;
        }
    }
}
