using System;
using System.Collections.Generic;
using System.Linq;
using Realm.Data;

namespace Realm
{
    // Account-level character-slot manifest — how many slots are unlocked,
    // and which character (by ID) occupies each. See
    // Data/CharacterSlotsData.cs for the on-disk shape and
    // Util.SaveCharacterSlotsData()/LoadCharacterSlotsData(). Replaces the
    // old "one save file per class name" scheme (Util.cs's now-removed
    // hardcoded 5-class enumerations) — a slot is a generic container,
    // decoupled from class, so two characters of the same class can exist
    // in two different slots.
    public static class CharacterSlotSystem
    {
        public static int UnlockedSlotCount = 2;
        public static List<CharacterSlotEntryData> Entries = new();

        // 3rd slot (going from 2 to 3 unlocked) costs 500 Fame; each slot
        // after that doubles: 500 / 1000 / 2000 / 4000 / ... Uncapped —
        // there's no maximum slot count, only an ever-rising Fame cost.
        public static int CostForNextSlot() => 500 * (int)Math.Pow(2, UnlockedSlotCount - 2);

        public static bool TryPurchaseNextSlot()
        {
            if (!FameSystem.TrySpendFame(CostForNextSlot()))
                return false;

            UnlockedSlotCount++;
            return true;
        }

        public static CharacterSlotEntryData GetEntry(int slotIndex) =>
            Entries.FirstOrDefault(e => e.SlotIndex == slotIndex);

        public static CharacterSlotEntryData FindByCharacterId(Guid characterId) =>
            Entries.FirstOrDefault(e => e.CharacterId == characterId);

        // Called once, right after a brand-new character is constructed in
        // Character Creation — registers it into the manifest before the
        // very first SavePlayerData() call, so that save (and the manifest
        // pointing at it) land together.
        public static void AssignCharacterToSlot(
            int slotIndex,
            Guid characterId,
            Player.Class playerClass
        )
        {
            Entries.RemoveAll(e => e.SlotIndex == slotIndex);
            Entries.Add(
                new CharacterSlotEntryData
                {
                    SlotIndex = slotIndex,
                    CharacterId = characterId,
                    PlayerClass = playerClass,
                    LastPlayedUtc = DateTime.UtcNow,
                }
            );
        }

        public static void TouchLastPlayed(Guid characterId)
        {
            CharacterSlotEntryData entry = FindByCharacterId(characterId);
            if (entry != null)
                entry.LastPlayedUtc = DateTime.UtcNow;
        }

        public static void RemoveCharacterFromSlot(Guid characterId) =>
            Entries.RemoveAll(e => e.CharacterId == characterId);

        public static CharacterSlotEntryData MostRecentlyPlayed() =>
            Entries.OrderByDescending(e => e.LastPlayedUtc).FirstOrDefault();

        // Full account wipe only (Util.EraseAllAccountData()).
        public static void Reset()
        {
            Entries.Clear();
            UnlockedSlotCount = 2;
        }

        public static CharacterSlotsData ToData() =>
            new() { UnlockedSlotCount = UnlockedSlotCount, Slots = new List<CharacterSlotEntryData>(Entries) };

        public static void LoadFromData(CharacterSlotsData data)
        {
            UnlockedSlotCount = data?.UnlockedSlotCount ?? 2;
            Entries = data?.Slots != null
                ? new List<CharacterSlotEntryData>(data.Slots)
                : new List<CharacterSlotEntryData>();
        }
    }
}
