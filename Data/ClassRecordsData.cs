namespace Realm.Data;

// The permanent, account-wide best-HighScore-ever-achieved record for each
// class — independent of any single character's save file. Needed once a
// class can have multiple characters (or none at all, after a delete):
// the star-unlock chain (CharacterCreationState.cs) has to keep working off
// "the best this class has ever scored," which can no longer be read
// straight off "the one save file for this class" the way it could when
// every class had exactly one character. See Systems/ClassRecordSystem.cs
// (in-memory state, updated live in RealmState.cs alongside HighScore
// itself) and Util.SaveClassRecordsData()/LoadClassRecordsData().
//
// Flat named fields rather than a dictionary, matching every other
// Data/*.cs DTO's style — nothing in this project's data layer serializes
// a Dictionary directly.
public class ClassRecordsData
{
    public int WizardBestHighScore { get; set; }
    public int ArcherBestHighScore { get; set; }
    public int KnightBestHighScore { get; set; }
    public int PriestBestHighScore { get; set; }
    public int RogueBestHighScore { get; set; }
}
