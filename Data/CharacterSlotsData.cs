using System;
using System.Collections.Generic;

namespace Realm.Data;

// The account-wide character-slot manifest — how many slots are unlocked
// and which character (by ID) occupies each, if any. See
// Systems/CharacterSlotSystem.cs (in-memory state) and
// Util.SaveCharacterSlotsData()/LoadCharacterSlotsData(). Replaces the old
// "one save file per class name" scheme — a slot is a generic container,
// decoupled from class, so two characters of the same class can exist in
// two different slots.
public class CharacterSlotsData
{
    // Starts at 2 (a fresh account gets 2 free slots) — see
    // CharacterSlotSystem.CostForNextSlot() for the Fame cost of every slot
    // beyond that.
    public int UnlockedSlotCount { get; set; } = 2;

    // Only occupied slots get an entry here. A slot index with no matching
    // entry, below UnlockedSlotCount, is empty (available for a new
    // character); an index at or above UnlockedSlotCount is locked.
    public List<CharacterSlotEntryData> Slots { get; set; } = new();
}

public class CharacterSlotEntryData
{
    public int SlotIndex { get; set; }

    // Matches that character's own PlayerData.ID / Player.Instance.ID —
    // the real per-character identity now that save files are keyed by
    // this Guid rather than by class name.
    public Guid CharacterId { get; set; }

    // Cached here so the slots list can show class/portrait without
    // peeking every character's own save file just to render a row.
    public Player.Class PlayerClass { get; set; }

    // Replaces the old DetermineLastPlayedClass()'s File.GetLastWriteTimeUtc
    // scan across 5 hardcoded class-named files — now there's no fixed
    // set of files to scan, so this is tracked explicitly instead.
    public DateTime LastPlayedUtc { get; set; }
}
