using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.CharacterClasses;
using Realm.Data;
using Realm.States;

namespace Realm
{
    public static class Util
    {
        // Keyed by the character's own ID (a Guid — see Player.ID), not by
        // class name — a class can now have zero, one, or many characters
        // (see Systems CharacterSlotSystem.cs/ClassRecordSystem.cs), so
        // class name alone no longer identifies a save file. The old
        // class-named paths (PlayerData_{ClassName}.json) are only ever
        // referenced now by MigrateLegacySavesIfNeeded() below, via its own
        // private legacy-path helper, to pick up real pre-existing saves
        // exactly once.
        private static string PlayerDataLocation(Guid characterId) =>
            Path.Combine(AppContext.BaseDirectory, $"PlayerData_{characterId}.json");

        private static string playerDataLocation => PlayerDataLocation(Player.Instance.ID);

        private static string InventoryDataLocation(Guid characterId) =>
            Path.Combine(AppContext.BaseDirectory, $"InventoryData_{characterId}.json");

        private static string inventoryDataLocation => InventoryDataLocation(Player.Instance.ID);

        // Not per-character — the slot manifest and the permanent per-class
        // star record are both account-wide, same reasoning as
        // bankDataLocation/fameDataLocation below.
        private static string characterSlotsDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "CharacterSlotsData.json"
        );

        private static string classRecordsDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "ClassRecordsData.json"
        );

        // Not per-class like PlayerData/InventoryData — the bank is shared
        // across every class's save.
        private static string bankDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "BankData.json"
        );

        // Not per-class, same reasoning as bankDataLocation — Fame is an
        // account-level total, not tied to any one class's save.
        private static string fameDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "FameData.json"
        );

        // Not per-class — key bindings are a player preference, same
        // reasoning as bankDataLocation/fameDataLocation above.
        private static string keyBindingsDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "KeyBindingsData.json"
        );

        // Not per-class, same reasoning as keyBindingsDataLocation — a
        // separate file (rather than folding into KeyBindingsData.json)
        // so that file stays scoped to just bindings, per
        // Data/GameSettingsData.cs's own doc comment.
        private static string gameSettingsDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "GameSettingsData.json"
        );

        // Every weapon type now lives in its own catalog file — see
        // Data/WandData.cs/LoadWandData() below. There is no more shared
        // WeaponData.json/WeaponData.cs; Staff (see staffDataLocation just
        // below) was the last type still using that generic shape, removed
        // once it was split out too.
        private static string wandDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "WandData.json"
        );

        // Staves live in their own catalog file — see Data/StaffData.cs and
        // LoadStaffData() below.
        private static string staffDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "StaffData.json"
        );

        // Bows live in their own catalog file, separate from
        // WeaponData.json — see Data/BowData.cs and LoadBowData() below.
        private static string bowDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "BowData.json"
        );

        // Swords live in their own catalog file, separate from
        // WeaponData.json — see Data/SwordData.cs and LoadSwordData() below.
        private static string swordDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "SwordData.json"
        );

        // Daggers live in their own catalog file, separate from
        // WeaponData.json — see Data/DaggerData.cs and LoadDaggerData()
        // below.
        private static string daggerDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "DaggerData.json"
        );

        // Rogue's ability item — see Data/CloakData.cs and LoadCloakData()
        // below.
        private static string cloakDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "CloakData.json"
        );

        // Each ArmorType lives in its own catalog file — see Data/ArmorData.cs
        // (the shared per-tier shape all three use) and LoadRobeData()/
        // LoadLeatherData()/LoadHeavyData() below.
        private static string robeDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "RobeData.json"
        );
        private static string leatherDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "LeatherData.json"
        );
        private static string heavyDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "HeavyData.json"
        );

        private static string ringDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "RingData.json"
        );

        private static string spellDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "SpellData.json"
        );

        private static string quiverDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "QuiverData.json"
        );

        private static string shieldDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "ShieldData.json"
        );

        private static string tomeDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "TomeData.json"
        );

        private static string biomeDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "BiomeData.json"
        );

        // One file per tileset (Data/TileSet_{Name}.json), not a shared array —
        // see Data/TileSetData.cs's own doc comment for why. LoadTileSetData(name)
        // below builds the actual path from this.
        private static string TileSetDataLocation(string name) =>
            Path.Combine(AppContext.BaseDirectory, $"TileSet_{name}.json");

        // One file per dungeon type (Data/DungeonType_{Name}.json), same
        // reasoning as TileSetDataLocation above. LoadDungeonTypeData(name)
        // below builds the actual path from this.
        private static string DungeonTypeDataLocation(string name) =>
            Path.Combine(AppContext.BaseDirectory, $"DungeonType_{name}.json");

        // Read-only peek at a character's save data, without touching
        // Player.Instance or Player.PlayerClass. Returns null if that
        // character ID has no save (shouldn't normally happen for an
        // occupied slot, but defensively handled the same way).
        //
        // CharacterSlotsState.Update() calls this for every occupied slot
        // on every single frame, so a missing save file is a routine,
        // expected outcome here — not something to detect via a caught
        // exception. Checking File.Exists() first avoids throwing (and a
        // debugger reporting) a FileNotFoundException every frame for as
        // long as that screen stays open.
        public static PlayerData PeekPlayerData(Guid characterId)
        {
            string path = PlayerDataLocation(characterId);
            if (!File.Exists(path))
                return null;

            try
            {
                using StreamReader r = new(path);
                string json = r.ReadToEnd();
                return JsonSerializer.Deserialize<PlayerData>(json);
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        // Permanently deletes a character: awards its Fame (same conversion
        // GameOverState applies on an actual death — Base Fame from
        // ExperienceTotal, plus BonusFame), removes its PlayerData/
        // InventoryData files outright, and clears it from the slot
        // manifest. Simpler than it used to be — this no longer needs to
        // preserve a "fresh default character" stub to keep HighScore
        // around, since a class's permanent star record is now tracked
        // independently by ClassRecordSystem (updated live as HighScore
        // rises — see RealmState.cs — not read off this character's own
        // file), and a deleted slot should genuinely become empty again,
        // ready for any class, not silently repopulated with a Level-1
        // stub of the same class.
        public static void DeleteCharacterData(Guid characterId)
        {
            PlayerData existing = PeekPlayerData(characterId);

            FameSystem.AddFame(
                Player.ComputeBaseFame(existing?.ExperienceTotal ?? 0) + (existing?.BonusFame ?? 0)
            );
            SaveFameData();

            string inventoryPath = InventoryDataLocation(characterId);
            if (File.Exists(inventoryPath))
                File.Delete(inventoryPath);

            string playerPath = PlayerDataLocation(characterId);
            if (File.Exists(playerPath))
                File.Delete(playerPath);

            CharacterSlotSystem.RemoveCharacterFromSlot(characterId);
            SaveCharacterSlotsData();
        }

        // Full account wipe — every character slot's PlayerData/
        // InventoryData, the shared BankData, account-wide FameData, the
        // slot manifest, and the permanent per-class star records.
        // Deliberately does NOT preserve anything (unlike
        // DeleteCharacterData, which — before this rework's simplification
        // — used to keep a class's HighScore/HasReachedLevel20 on purpose)
        // — the caller is expected to have gotten explicit, doubly-confirmed
        // user intent first, since this is irreversible and there's no
        // separate backup.
        public static void EraseAllAccountData()
        {
            // Copy the list before iterating — nothing here mutates
            // CharacterSlotSystem.Entries mid-loop today, but
            // CharacterSlotSystem.Reset() a few lines down does, and a
            // defensive copy costs nothing.
            foreach (CharacterSlotEntryData entry in CharacterSlotSystem.Entries.ToList())
            {
                string playerPath = PlayerDataLocation(entry.CharacterId);
                if (File.Exists(playerPath))
                    File.Delete(playerPath);

                string inventoryPath = InventoryDataLocation(entry.CharacterId);
                if (File.Exists(inventoryPath))
                    File.Delete(inventoryPath);
            }

            if (File.Exists(bankDataLocation))
                File.Delete(bankDataLocation);

            if (File.Exists(fameDataLocation))
                File.Delete(fameDataLocation);

            if (File.Exists(characterSlotsDataLocation))
                File.Delete(characterSlotsDataLocation);

            if (File.Exists(classRecordsDataLocation))
                File.Delete(classRecordsDataLocation);

            // Clear in-memory state too — Records is a fixed-size array
            // (readonly reference, mutable contents), and class unlocks/
            // Fame-gated UI read FameSystem.Fame live rather than from a
            // separate "unlocked" flag, so zeroing it here is what actually
            // re-locks Archer/Knight immediately. Without clearing both, a
            // later autosave (e.g. entering the Nexus) would silently
            // resurrect the just-deleted data from stale memory. Same
            // reasoning now extends to the slot manifest and class records.
            Array.Clear(BankSystem.Records, 0, BankSystem.Records.Length);
            FameSystem.Fame = 0;
            CharacterSlotSystem.Reset();
            ClassRecordSystem.Reset();

            Player.Class currentClass = Player.PlayerClass;
            EntityManager.RemovePlayer();
            ResetPlayer(currentClass);
            // ResetPlayer() just built a brand new Player.Instance —
            // GameSettingsData's fields live on it directly, so reload them
            // here too (GameSettingsData.json itself isn't touched by this
            // wipe, only PlayerData/InventoryData/BankData/FameData/the two
            // new account-wide files are).
            LoadGameSettingsData();
            EntityManager.Add(Player.Instance);
        }

        // Replaces the old DetermineLastPlayedClass() — there's no fixed
        // set of 5 class-named files to scan anymore (a class can have
        // zero, one, or many characters), so this reads the slot
        // manifest's own LastPlayedUtc instead of File.GetLastWriteTimeUtc.
        // Returns null when no character exists yet (a fresh account, or
        // right after Erase All Data) — callers must handle that instead of
        // falling back to a hardcoded class, since there's no character to
        // actually load in that case.
        public static CharacterSlotEntryData DetermineLastPlayedCharacter() =>
            CharacterSlotSystem.MostRecentlyPlayed();

        // Used by the main menu's Nexus button to decide whether jumping straight
        // into gameplay makes sense, or no character has ever actually been played
        // yet (Player.Instance defaults to a fresh in-memory Wizard at boot
        // regardless when there's nothing to load — see Game1.StartGame() — so
        // that alone can't answer this).
        public static bool AnyCharacterHasBeenPlayed() =>
            CharacterSlotSystem.Entries.Any(entry =>
                PeekPlayerData(entry.CharacterId)?.HasBeenPlayed ?? false
            );

        // Constructs the given class at its base stats, discarding whatever the
        // current Player.Instance is — no save is read or applied.
        public static void ResetPlayer(Player.Class playerClass)
        {
            Player.PlayerClass = playerClass;

            switch (playerClass)
            {
                case Player.Class.Wizard:
                    _ = new Wizard();
                    break;
                case Player.Class.Archer:
                    _ = new Archer();
                    break;
                case Player.Class.Knight:
                    _ = new Knight();
                    break;
                case Player.Class.Priest:
                    _ = new Priest();
                    break;
                case Player.Class.Rogue:
                    _ = new Rogue();
                    break;
            }
        }

        // Constructs the given class and, if characterId names an existing
        // save, layers that save's stats (and inventory) on top. Used both
        // at game boot (loading the last-played character) and when a
        // character is chosen from a populated slot, so there's exactly one
        // place that knows how to do this correctly (construct first, then
        // apply save data — not the other way around, which discards the
        // loaded stats as soon as the constructor resets them to base
        // values). Pass characterId: null for a brand-new character (an
        // empty slot in Character Creation) — there's no save to layer, and
        // Player.Instance.ID stays whatever ResetPlayer's constructor
        // freshly generated, which becomes that character's permanent
        // identity from here on (the caller reads it back afterward to
        // register the new slot — see CharacterCreationState.SelectCharacter()).
        public static void LoadOrCreatePlayer(Player.Class playerClass, Guid? characterId)
        {
            PlayerData saved = characterId.HasValue ? PeekPlayerData(characterId.Value) : null;

            ResetPlayer(playerClass);

            if (saved != null)
            {
                Player.Instance.ID = saved.ID;
                Player.Instance.Name = saved.Name;
                Player.Instance.Description = saved.Description;
                Player.Instance.ExperienceTotal = saved.ExperienceTotal;
                Player.Instance.BonusFame = saved.BonusFame;
                Player.Instance.HighScore = saved.HighScore;
                Player.Instance.HasBeenPlayed = saved.HasBeenPlayed;
                Player.Instance.HasReachedLevel20 = saved.HasReachedLevel20;
                Player.Instance.Level = saved.Level;

                Player.Instance.PotionAttackBonus = saved.PotionAttackBonus;
                Player.Instance.PotionDefenseBonus = saved.PotionDefenseBonus;
                Player.Instance.PotionSpeedBonus = saved.PotionSpeedBonus;
                Player.Instance.PotionDexterityBonus = saved.PotionDexterityBonus;
                Player.Instance.PotionVitalityBonus = saved.PotionVitalityBonus;
                Player.Instance.PotionWisdomBonus = saved.PotionWisdomBonus;
                Player.Instance.PotionHealthMaxBonus = saved.PotionHealthMaxBonus;
                Player.Instance.PotionManaMaxBonus = saved.PotionManaMaxBonus;

                Player.Instance.Inventory.HealthPotionCharges = saved.HealthPotionCharges;
                Player.Instance.Inventory.ManaPotionCharges = saved.ManaPotionCharges;

                // Deliberately NOT restoring HealthMax/ManaMax/Attack/Defense/
                // Vitality/Wisdom/Speed/Dexterity directly from saved.* here —
                // those are derived values that already had whatever gear was
                // equipped at save time baked in. Re-equipping that same gear
                // below (via EquipWeapon/EquipArmor/EquipRing) recomputes them
                // correctly from Level + Potion*Bonus (restored above) + the
                // now-equipped item's live bonus; restoring the raw baked
                // totals AND re-equipping would double-count the gear bonus.
                //
                // A saved Weapon/Armor/Ring is never actually null after a JSON
                // round-trip (it's a real object, just blank) — a null Name is
                // what indicates the slot was unequipped when saved. Without this
                // check, an unequipped slot would silently re-equip the class's
                // constructor-default item on every reload.
                if (saved.Weapon != null)
                {
                    if (saved.Weapon.Name != null)
                        Weapon.LoadWeapon(saved.Weapon.Name);
                    else
                        Player.Instance.EquipWeapon(new Weapon());
                }

                if (saved.Armor != null)
                {
                    if (saved.Armor.Name != null)
                        Armor.LoadArmor(saved.Armor.Name);
                    else
                        Player.Instance.EquipArmor(new Armor());
                }

                if (saved.Ring != null)
                {
                    if (saved.Ring.Name != null)
                        Ring.LoadRing(saved.Ring.Name);
                    else
                        Player.Instance.EquipRing(new Ring());
                }

                // Spell/Quiver/Shield/Tome/Cloak are only populated when
                // actually equipped (see SavePlayerData's `as Spell`/
                // `as Quiver`/`as Shield`/`as Tome`/`as Cloak` — a blank
                // AbilityItem casts to none of them), so all five being null
                // unambiguously means "unequipped" — same signal Weapon/
                // Armor/Ring get from a null Name. Without this else,
                // ResetPlayer's constructor-equipped default Tier-0 ability
                // item was never cleared, so it stayed equipped alongside
                // whatever the player had actually dragged into inventory —
                // the reported duplicate ability item.
                if (saved.Spell != null && saved.Spell.Name != null)
                    Spell.LoadSpell(saved.Spell.Name);
                else if (saved.Quiver != null && saved.Quiver.Name != null)
                    Quiver.LoadQuiver(saved.Quiver.Name);
                else if (saved.Shield != null && saved.Shield.Name != null)
                    Shield.LoadShield(saved.Shield.Name);
                else if (saved.Tome != null && saved.Tome.Name != null)
                    Tome.LoadTome(saved.Tome.Name);
                else if (saved.Cloak != null && saved.Cloak.Name != null)
                    Cloak.LoadCloak(saved.Cloak.Name);
                else
                    Player.Instance.EquipAbilityItem(new AbilityItem());

                Player.Instance.Health = Player.Instance.HealthMax;
                Player.Instance.Mana = Player.Instance.ManaMax;
            }

            LoadInventoryData();
        }

        public static void SavePlayerData()
        {
            PlayerData playerData = BuildPlayerData();

            string json = JsonSerializer.Serialize(playerData);
            Debug.WriteLine(json);
            File.WriteAllText(playerDataLocation, json);

            Debug.WriteLine("GameData Saved.");
        }

        // Snapshots Player.Instance/Player.PlayerClass into a PlayerData DTO,
        // used by SavePlayerData to persist the live, currently-playing
        // character.
        private static PlayerData BuildPlayerData()
        {
            return new PlayerData
            {
                ID = Player.Instance.ID,
                Name = Player.Instance.Name,
                Description = Player.Instance.Description,
                PlayerClass = Player.PlayerClass,
                //Health = Player.Instance.Health,
                HealthMax = Player.Instance.HealthMax,
                //Mana = Player.Instance.Mana,
                ManaMax = Player.Instance.ManaMax,
                Attack = Player.Instance.Attack,
                Defense = Player.Instance.Defense,
                Vitality = Player.Instance.Vitality,
                Wisdom = Player.Instance.Wisdom,
                Speed = Player.Instance.Speed,
                Dexterity = Player.Instance.Dexterity,
                ExperienceTotal = Player.Instance.ExperienceTotal,
                BonusFame = Player.Instance.BonusFame,
                HighScore = Player.Instance.HighScore,
                HasBeenPlayed = Player.Instance.HasBeenPlayed,
                HasReachedLevel20 = Player.Instance.HasReachedLevel20,
                Level = Player.Instance.Level,
                Weapon = Player.Instance.Weapon,
                Armor = Player.Instance.Armor,
                Ring = Player.Instance.Ring,
                Spell = Player.Instance.AbilityItem as Spell,
                Quiver = Player.Instance.AbilityItem as Quiver,
                Shield = Player.Instance.AbilityItem as Shield,
                Tome = Player.Instance.AbilityItem as Tome,
                Cloak = Player.Instance.AbilityItem as Cloak,
                HealthPotionCharges = Player.Instance.Inventory.HealthPotionCharges,
                ManaPotionCharges = Player.Instance.Inventory.ManaPotionCharges,
                PotionAttackBonus = Player.Instance.PotionAttackBonus,
                PotionDefenseBonus = Player.Instance.PotionDefenseBonus,
                PotionSpeedBonus = Player.Instance.PotionSpeedBonus,
                PotionDexterityBonus = Player.Instance.PotionDexterityBonus,
                PotionVitalityBonus = Player.Instance.PotionVitalityBonus,
                PotionWisdomBonus = Player.Instance.PotionWisdomBonus,
                PotionHealthMaxBonus = Player.Instance.PotionHealthMaxBonus,
                PotionManaMaxBonus = Player.Instance.PotionManaMaxBonus,
            };
        }

        // Staves live in their own catalog file (see Data/StaffData.cs)
        // with a per-tier XpBonusPercent (see Equipment.XpBonusPercent)
        // matching the real wiki's "XP Bonus" column
        // (https://www.realmeye.com/wiki/staves). Keeps its own
        // Amplitude/Frequency fields, since Staff is still the only weapon
        // type that uses them. Returns plain Weapon.WeaponType.Staff
        // entries, meant to be merged into the same combined Weapons list
        // as the other Load*Data() results (see Game1.StartGame()) —
        // Weapon.LoadWeapon() and Player.cs's EquipHighestTierWeapon() both
        // search that one
        // list by Name, unaware of which file an entry originally came
        // from.
        public static List<Weapon> LoadStaffData()
        {
            List<StaffData> staffData = [];
            List<Weapon> staves = [];

            try
            {
                using (StreamReader r = new(staffDataLocation))
                {
                    Debug.WriteLine(staffDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        staffData = JsonSerializer.Deserialize<List<StaffData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading staff data: {json}");
                    }
                }

                for (int i = 0; i < staffData.Count; i++)
                {
                    Texture2D staffTexture = Game1.Instance.Content.Load<Texture2D>(
                        staffData[i].ImageName
                    );

                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        staffData[i].ProjectileImageName
                    );

                    staves.Add(
                        new Weapon(staffTexture, projectileTexture)
                        {
                            Type = Weapon.WeaponType.Staff,
                            Name = staffData[i].Name,
                            Description = staffData[i].Description,
                            Tier = staffData[i].Tier,
                            DamageMin = staffData[i].DamageMin,
                            DamageMax = staffData[i].DamageMax,
                            ProjectileMagnitude = staffData[i].ProjectileMagnitude,
                            ProjectileDuration = staffData[i].ProjectileDuration,
                            Amplitude = staffData[i].Amplitude,
                            Frequency = staffData[i].Frequency,
                            XpBonusPercent = staffData[i].XpBonusPercent,
                            ImageName = staffData[i].ImageName,
                            ProjectileImageName = staffData[i].ProjectileImageName,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(staffDataLocation + ": file not found.");
            }

            return staves;
        }

        // Wands live in their own catalog file (see Data/WandData.cs) with
        // a per-tier XpBonusPercent (see Equipment.XpBonusPercent) matching
        // the real wiki's "XP Bonus" column
        // (https://www.realmeye.com/wiki/wands). Returns plain
        // Weapon.WeaponType.Wand entries, meant to be merged into the same
        // combined Weapons list as LoadStaffData()'s/LoadBowData()'s/
        // LoadSwordData()'s/LoadDaggerData()'s results (see
        // Game1.StartGame()) — Weapon.LoadWeapon() and Player.cs's
        // EquipHighestTierWeapon() both search that one list by Name,
        // unaware of which file an entry originally came from.
        public static List<Weapon> LoadWandData()
        {
            List<WandData> wandData = [];
            List<Weapon> wands = [];

            try
            {
                using (StreamReader r = new(wandDataLocation))
                {
                    Debug.WriteLine(wandDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        wandData = JsonSerializer.Deserialize<List<WandData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading wand data: {json}");
                    }
                }

                for (int i = 0; i < wandData.Count; i++)
                {
                    Texture2D wandTexture = Game1.Instance.Content.Load<Texture2D>(
                        wandData[i].ImageName
                    );

                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        wandData[i].ProjectileImageName
                    );

                    wands.Add(
                        new Weapon(wandTexture, projectileTexture)
                        {
                            Type = Weapon.WeaponType.Wand,
                            Name = wandData[i].Name,
                            Description = wandData[i].Description,
                            Tier = wandData[i].Tier,
                            DamageMin = wandData[i].DamageMin,
                            DamageMax = wandData[i].DamageMax,
                            ProjectileMagnitude = wandData[i].ProjectileMagnitude,
                            ProjectileDuration = wandData[i].ProjectileDuration,
                            XpBonusPercent = wandData[i].XpBonusPercent,
                            ImageName = wandData[i].ImageName,
                            ProjectileImageName = wandData[i].ProjectileImageName,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(wandDataLocation + ": file not found.");
            }

            return wands;
        }

        // Bows live in their own catalog file (see Data/BowData.cs) —
        // unlike every other weapon type, a Bow needs two independent
        // damage ranges and two independent projectile textures
        // (Main/Side). Returns plain Weapon.WeaponType.Bow entries, meant
        // to be merged into the same combined Weapons list as the other
        // Load*Data() results (see Game1.StartGame()) — Weapon.LoadWeapon()
        // and Player.cs's EquipHighestTierWeapon() both search that one
        // list by Name, unaware of which file an entry originally came
        // from.
        public static List<Weapon> LoadBowData()
        {
            List<BowData> bowData = [];
            List<Weapon> bows = [];

            try
            {
                using (StreamReader r = new(bowDataLocation))
                {
                    Debug.WriteLine(bowDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        bowData = JsonSerializer.Deserialize<List<BowData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading bow data: {json}");
                    }
                }

                for (int i = 0; i < bowData.Count; i++)
                {
                    Texture2D bowTexture = Game1.Instance.Content.Load<Texture2D>(
                        bowData[i].ImageName
                    );

                    Texture2D mainProjectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        bowData[i].MainProjectileImageName
                    );

                    Texture2D sideProjectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        bowData[i].SideProjectileImageName
                    );

                    bows.Add(
                        new Weapon(bowTexture, mainProjectileTexture)
                        {
                            Type = Weapon.WeaponType.Bow,
                            Name = bowData[i].Name,
                            Description = bowData[i].Description,
                            Tier = bowData[i].Tier,
                            DamageMin = bowData[i].MainDamageMin,
                            DamageMax = bowData[i].MainDamageMax,
                            ProjectileMagnitude = bowData[i].ProjectileMagnitude,
                            ProjectileDuration = bowData[i].ProjectileDuration,
                            ImageName = bowData[i].ImageName,
                            ProjectileImageName = bowData[i].MainProjectileImageName,
                            SideDamageMin = bowData[i].SideDamageMin,
                            SideDamageMax = bowData[i].SideDamageMax,
                            SideProjectileImage = sideProjectileTexture,
                            SideProjectileImageName = bowData[i].SideProjectileImageName,
                            ArcGapDegrees = bowData[i].ArcGapDegrees,
                            XpBonusPercent = bowData[i].XpBonusPercent,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(bowDataLocation + ": file not found.");
            }

            return bows;
        }

        // Swords live in their own catalog file (see Data/SwordData.cs)
        // with a per-tier XpBonusPercent (see Equipment.XpBonusPercent)
        // matching the real wiki's "XP Bonus" column
        // (https://www.realmeye.com/wiki/swords). Returns plain
        // Weapon.WeaponType.Sword entries, meant to be merged into the same
        // combined Weapons list as the other Load*Data() results (see
        // Game1.StartGame()) — Weapon.LoadWeapon() and Player.cs's
        // EquipHighestTierWeapon() both search that one list by Name,
        // unaware of which file an entry originally came from.
        public static List<Weapon> LoadSwordData()
        {
            List<SwordData> swordData = [];
            List<Weapon> swords = [];

            try
            {
                using (StreamReader r = new(swordDataLocation))
                {
                    Debug.WriteLine(swordDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        swordData = JsonSerializer.Deserialize<List<SwordData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading sword data: {json}");
                    }
                }

                for (int i = 0; i < swordData.Count; i++)
                {
                    Texture2D swordTexture = Game1.Instance.Content.Load<Texture2D>(
                        swordData[i].ImageName
                    );

                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        swordData[i].ProjectileImageName
                    );

                    swords.Add(
                        new Weapon(swordTexture, projectileTexture)
                        {
                            Type = Weapon.WeaponType.Sword,
                            Name = swordData[i].Name,
                            Description = swordData[i].Description,
                            Tier = swordData[i].Tier,
                            DamageMin = swordData[i].DamageMin,
                            DamageMax = swordData[i].DamageMax,
                            ProjectileMagnitude = swordData[i].ProjectileMagnitude,
                            ProjectileDuration = swordData[i].ProjectileDuration,
                            XpBonusPercent = swordData[i].XpBonusPercent,
                            ImageName = swordData[i].ImageName,
                            ProjectileImageName = swordData[i].ProjectileImageName,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(swordDataLocation + ": file not found.");
            }

            return swords;
        }

        // Daggers live in their own catalog file (see Data/DaggerData.cs) —
        // same reasoning as LoadSwordData() above (a per-tier
        // XpBonusPercent). Returns plain Weapon.WeaponType.Dagger entries,
        // meant to be merged into the same combined Weapons list as the
        // other Load*Data() results (see Game1.StartGame()) —
        // Weapon.LoadWeapon() and Player.cs's EquipHighestTierWeapon() both
        // search that one list by Name, unaware of which file an entry
        // originally came from.
        public static List<Weapon> LoadDaggerData()
        {
            List<DaggerData> daggerData = [];
            List<Weapon> daggers = [];

            try
            {
                using (StreamReader r = new(daggerDataLocation))
                {
                    Debug.WriteLine(daggerDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        daggerData = JsonSerializer.Deserialize<List<DaggerData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading dagger data: {json}");
                    }
                }

                for (int i = 0; i < daggerData.Count; i++)
                {
                    Texture2D daggerTexture = Game1.Instance.Content.Load<Texture2D>(
                        daggerData[i].ImageName
                    );

                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        daggerData[i].ProjectileImageName
                    );

                    daggers.Add(
                        new Weapon(daggerTexture, projectileTexture)
                        {
                            Type = Weapon.WeaponType.Dagger,
                            Name = daggerData[i].Name,
                            Description = daggerData[i].Description,
                            Tier = daggerData[i].Tier,
                            DamageMin = daggerData[i].DamageMin,
                            DamageMax = daggerData[i].DamageMax,
                            ProjectileMagnitude = daggerData[i].ProjectileMagnitude,
                            ProjectileDuration = daggerData[i].ProjectileDuration,
                            XpBonusPercent = daggerData[i].XpBonusPercent,
                            ImageName = daggerData[i].ImageName,
                            ProjectileImageName = daggerData[i].ProjectileImageName,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(daggerDataLocation + ": file not found.");
            }

            return daggers;
        }

        // Rogue's ability item, own catalog file (see Data/CloakData.cs) —
        // same shape as LoadSpellData()/LoadQuiverData()/LoadShieldData()/
        // LoadTomeData() below.
        public static List<Cloak> LoadCloakData()
        {
            List<CloakData> cloakData = [];
            List<Cloak> cloaks = [];

            try
            {
                using (StreamReader r = new(cloakDataLocation))
                {
                    Debug.WriteLine(cloakDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        cloakData = JsonSerializer.Deserialize<List<CloakData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading cloak data: {json}");
                    }
                }

                for (int i = 0; i < cloakData.Count; i++)
                {
                    Texture2D cloakTexture = Game1.Instance.Content.Load<Texture2D>(
                        cloakData[i].ImageName
                    );

                    cloaks.Add(
                        new Cloak(cloakTexture)
                        {
                            Name = cloakData[i].Name,
                            Description = cloakData[i].Description,
                            Tier = cloakData[i].Tier,
                            MaxHealthBonus = cloakData[i].MaxHealthBonus,
                            MaxManaBonus = cloakData[i].MaxManaBonus,
                            AttackBonus = cloakData[i].AttackBonus,
                            DefenseBonus = cloakData[i].DefenseBonus,
                            SpeedBonus = cloakData[i].SpeedBonus,
                            DexterityBonus = cloakData[i].DexterityBonus,
                            VitalityBonus = cloakData[i].VitalityBonus,
                            WisdomBonus = cloakData[i].WisdomBonus,
                            ManaCost = cloakData[i].ManaCost,
                            ImageName = cloakData[i].ImageName,
                            XpBonusPercent = cloakData[i].XpBonusPercent,
                            InvisibilityDurationFrames = cloakData[i].InvisibilityDurationFrames,
                            BaseFlatDamage = cloakData[i].BaseFlatDamage,
                            FlatDamagePerWisOver34 = cloakData[i].FlatDamagePerWisOver34,
                            BasePercentDamage = cloakData[i].BasePercentDamage,
                            PercentDamagePerWisOver34 = cloakData[i].PercentDamagePerWisOver34,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(cloakDataLocation + ": file not found.");
            }

            return cloaks;
        }

        // Shared by LoadRobeData()/LoadLeatherData()/LoadHeavyData() below —
        // all three ArmorTypes use the exact same per-tier shape
        // (Data/ArmorData.cs), differing only in which catalog file backs
        // them and which Armor.ArmorType gets hardcoded onto the result
        // (the JSON itself carries no Type field, same reasoning as the
        // per-weapon-type loaders not reading WeaponType from JSON either).
        private static List<Armor> LoadArmorType(string dataLocation, Armor.ArmorType type)
        {
            List<ArmorData> armorData = [];
            List<Armor> armors = [];

            try
            {
                using (StreamReader r = new(dataLocation))
                {
                    Debug.WriteLine(dataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        armorData = JsonSerializer.Deserialize<List<ArmorData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading armor data: {json}");
                    }
                }

                for (int i = 0; i < armorData.Count; i++)
                {
                    Texture2D armorTexture = Game1.Instance.Content.Load<Texture2D>(
                        armorData[i].ImageName
                    );

                    armors.Add(
                        new Armor(armorTexture)
                        {
                            Name = armorData[i].Name,
                            Description = armorData[i].Description,
                            Type = type,
                            Tier = armorData[i].Tier,
                            MaxHealthBonus = armorData[i].MaxHealthBonus,
                            MaxManaBonus = armorData[i].MaxManaBonus,
                            AttackBonus = armorData[i].AttackBonus,
                            DefenseBonus = armorData[i].DefenseBonus,
                            SpeedBonus = armorData[i].SpeedBonus,
                            DexterityBonus = armorData[i].DexterityBonus,
                            VitalityBonus = armorData[i].VitalityBonus,
                            WisdomBonus = armorData[i].WisdomBonus,
                            XpBonusPercent = armorData[i].XpBonusPercent,
                            ImageName = armorData[i].ImageName,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(dataLocation + ": file not found.");
            }

            return armors;
        }

        public static List<Armor> LoadRobeData() =>
            LoadArmorType(robeDataLocation, Armor.ArmorType.Robe);

        public static List<Armor> LoadLeatherData() =>
            LoadArmorType(leatherDataLocation, Armor.ArmorType.Leather);

        public static List<Armor> LoadHeavyData() =>
            LoadArmorType(heavyDataLocation, Armor.ArmorType.Heavy);

        public static List<Ring> LoadRingData()
        {
            List<RingData> ringData = [];
            List<Ring> rings = [];

            try
            {
                using (StreamReader r = new(ringDataLocation))
                {
                    Debug.WriteLine(ringDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        ringData = JsonSerializer.Deserialize<List<RingData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading ring data: {json}");
                    }
                }

                for (int i = 0; i < ringData.Count; i++)
                {
                    Texture2D ringTexture = Game1.Instance.Content.Load<Texture2D>(
                        ringData[i].ImageName
                    );

                    rings.Add(
                        new Ring(ringTexture)
                        {
                            Name = ringData[i].Name,
                            Description = ringData[i].Description,
                            Tier = ringData[i].Tier,
                            MaxHealthBonus = ringData[i].MaxHealthBonus,
                            MaxManaBonus = ringData[i].MaxManaBonus,
                            AttackBonus = ringData[i].AttackBonus,
                            DefenseBonus = ringData[i].DefenseBonus,
                            SpeedBonus = ringData[i].SpeedBonus,
                            DexterityBonus = ringData[i].DexterityBonus,
                            VitalityBonus = ringData[i].VitalityBonus,
                            WisdomBonus = ringData[i].WisdomBonus,
                            ImageName = ringData[i].ImageName,
                            XpBonusPercent = ringData[i].XpBonusPercent,
                            IsUntiered = ringData[i].IsUntiered,
                            ReactiveProcBuff = string.IsNullOrEmpty(ringData[i].ReactiveProc)
                                ? null
                                : Enum.Parse<Entity.DebuffType>(ringData[i].ReactiveProc),
                            ReactiveProcDurationFrames = ringData[i].ReactiveProcDurationFrames,
                            ReactiveProcCooldownFrames = ringData[i].ReactiveProcCooldownFrames,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(ringDataLocation + ": file not found.");
            }

            return rings;
        }

        public static List<Spell> LoadSpellData()
        {
            List<SpellData> spellData = [];
            List<Spell> spells = [];

            try
            {
                using (StreamReader r = new(spellDataLocation))
                {
                    Debug.WriteLine(spellDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        spellData = JsonSerializer.Deserialize<List<SpellData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading spell data: {json}");
                    }
                }

                for (int i = 0; i < spellData.Count; i++)
                {
                    Texture2D spellTexture = Game1.Instance.Content.Load<Texture2D>(
                        spellData[i].ImageName
                    );
                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        spellData[i].ProjectileImageName
                    );

                    spells.Add(
                        new Spell(spellTexture)
                        {
                            Name = spellData[i].Name,
                            Description = spellData[i].Description,
                            Tier = spellData[i].Tier,
                            MaxHealthBonus = spellData[i].MaxHealthBonus,
                            MaxManaBonus = spellData[i].MaxManaBonus,
                            AttackBonus = spellData[i].AttackBonus,
                            DefenseBonus = spellData[i].DefenseBonus,
                            SpeedBonus = spellData[i].SpeedBonus,
                            DexterityBonus = spellData[i].DexterityBonus,
                            VitalityBonus = spellData[i].VitalityBonus,
                            WisdomBonus = spellData[i].WisdomBonus,
                            ManaCost = spellData[i].ManaCost,
                            MinDamage = spellData[i].MinDamage,
                            MaxDamage = spellData[i].MaxDamage,
                            ImageName = spellData[i].ImageName,
                            ProjectileMagnitude = spellData[i].ProjectileMagnitude,
                            ProjectileDuration = spellData[i].ProjectileDuration,
                            ProjectileImage = projectileTexture,
                            ProjectileImageName = spellData[i].ProjectileImageName,
                            DamagePerWisOver42 = spellData[i].DamagePerWisOver42,
                            XpBonusPercent = spellData[i].XpBonusPercent,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(spellDataLocation + ": file not found.");
            }

            return spells;
        }

        public static List<Quiver> LoadQuiverData()
        {
            List<QuiverData> quiverData = [];
            List<Quiver> quivers = [];

            try
            {
                using (StreamReader r = new(quiverDataLocation))
                {
                    Debug.WriteLine(quiverDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        quiverData = JsonSerializer.Deserialize<List<QuiverData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading quiver data: {json}");
                    }
                }

                for (int i = 0; i < quiverData.Count; i++)
                {
                    Texture2D quiverTexture = Game1.Instance.Content.Load<Texture2D>(
                        quiverData[i].ImageName
                    );
                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        quiverData[i].ProjectileImageName
                    );

                    quivers.Add(
                        new Quiver(quiverTexture)
                        {
                            Name = quiverData[i].Name,
                            Description = quiverData[i].Description,
                            Tier = quiverData[i].Tier,
                            MaxHealthBonus = quiverData[i].MaxHealthBonus,
                            MaxManaBonus = quiverData[i].MaxManaBonus,
                            AttackBonus = quiverData[i].AttackBonus,
                            DefenseBonus = quiverData[i].DefenseBonus,
                            SpeedBonus = quiverData[i].SpeedBonus,
                            DexterityBonus = quiverData[i].DexterityBonus,
                            VitalityBonus = quiverData[i].VitalityBonus,
                            WisdomBonus = quiverData[i].WisdomBonus,
                            ManaCost = quiverData[i].ManaCost,
                            MinDamage = quiverData[i].MinDamage,
                            MaxDamage = quiverData[i].MaxDamage,
                            ImageName = quiverData[i].ImageName,
                            Shots = quiverData[i].Shots,
                            ArcGapDegrees = quiverData[i].ArcGapDegrees,
                            ProjectileMagnitude = quiverData[i].ProjectileMagnitude,
                            ProjectileDuration = quiverData[i].ProjectileDuration,
                            ProjectileImage = projectileTexture,
                            ProjectileImageName = quiverData[i].ProjectileImageName,
                            XpBonusPercent = quiverData[i].XpBonusPercent,
                            DamagePerWisOver34 = quiverData[i].DamagePerWisOver34,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(quiverDataLocation + ": file not found.");
            }

            return quivers;
        }

        public static List<Shield> LoadShieldData()
        {
            List<ShieldData> shieldData = [];
            List<Shield> shields = [];

            try
            {
                using (StreamReader r = new(shieldDataLocation))
                {
                    Debug.WriteLine(shieldDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        shieldData = JsonSerializer.Deserialize<List<ShieldData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading shield data: {json}");
                    }
                }

                for (int i = 0; i < shieldData.Count; i++)
                {
                    Texture2D shieldTexture = Game1.Instance.Content.Load<Texture2D>(
                        shieldData[i].ImageName
                    );

                    shields.Add(
                        new Shield(shieldTexture)
                        {
                            Name = shieldData[i].Name,
                            Description = shieldData[i].Description,
                            Tier = shieldData[i].Tier,
                            MaxHealthBonus = shieldData[i].MaxHealthBonus,
                            MaxManaBonus = shieldData[i].MaxManaBonus,
                            AttackBonus = shieldData[i].AttackBonus,
                            DefenseBonus = shieldData[i].DefenseBonus,
                            SpeedBonus = shieldData[i].SpeedBonus,
                            DexterityBonus = shieldData[i].DexterityBonus,
                            VitalityBonus = shieldData[i].VitalityBonus,
                            WisdomBonus = shieldData[i].WisdomBonus,
                            ManaCost = shieldData[i].ManaCost,
                            MinDamage = shieldData[i].MinDamage,
                            MaxDamage = shieldData[i].MaxDamage,
                            ImageName = shieldData[i].ImageName,
                            Shots = shieldData[i].Shots,
                            ArcGapDegrees = shieldData[i].ArcGapDegrees,
                            XpBonusPercent = shieldData[i].XpBonusPercent,
                            DamagePerWisOver34 = shieldData[i].DamagePerWisOver34,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(shieldDataLocation + ": file not found.");
            }

            return shields;
        }

        public static List<Tome> LoadTomeData()
        {
            List<TomeData> tomeData = [];
            List<Tome> tomes = [];

            try
            {
                using (StreamReader r = new(tomeDataLocation))
                {
                    Debug.WriteLine(tomeDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        tomeData = JsonSerializer.Deserialize<List<TomeData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading tome data: {json}");
                    }
                }

                for (int i = 0; i < tomeData.Count; i++)
                {
                    Texture2D tomeTexture = Game1.Instance.Content.Load<Texture2D>(
                        tomeData[i].ImageName
                    );

                    tomes.Add(
                        new Tome(tomeTexture)
                        {
                            Name = tomeData[i].Name,
                            Description = tomeData[i].Description,
                            Tier = tomeData[i].Tier,
                            MaxHealthBonus = tomeData[i].MaxHealthBonus,
                            MaxManaBonus = tomeData[i].MaxManaBonus,
                            AttackBonus = tomeData[i].AttackBonus,
                            DefenseBonus = tomeData[i].DefenseBonus,
                            SpeedBonus = tomeData[i].SpeedBonus,
                            DexterityBonus = tomeData[i].DexterityBonus,
                            VitalityBonus = tomeData[i].VitalityBonus,
                            WisdomBonus = tomeData[i].WisdomBonus,
                            ManaCost = tomeData[i].ManaCost,
                            MinDamage = tomeData[i].MinDamage,
                            MaxDamage = tomeData[i].MaxDamage,
                            ImageName = tomeData[i].ImageName,
                            XpBonusPercent = tomeData[i].XpBonusPercent,
                            Range = tomeData[i].Range,
                            HealAmount = tomeData[i].HealAmount,
                            HealingAmountPerSecond = tomeData[i].HealingAmountPerSecond,
                            HealingDurationSeconds = tomeData[i].HealingDurationSeconds,
                            HealAmountPerWisOver70 = tomeData[i].HealAmountPerWisOver70,
                            HealingRatePerWisOver70 = tomeData[i].HealingRatePerWisOver70,
                            DamagePerWisOver70 = tomeData[i].DamagePerWisOver70,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(tomeDataLocation + ": file not found.");
            }

            return tomes;
        }

        // No runtime-type mapping step, unlike every Load*Data() above —
        // BiomeData isn't an equippable Item with its own texture slot,
        // just plain config, so the deserialized list is used directly.
        public static List<BiomeData> LoadBiomeData()
        {
            List<BiomeData> biomeData = [];

            try
            {
                using (StreamReader r = new(biomeDataLocation))
                {
                    Debug.WriteLine(biomeDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        biomeData = JsonSerializer.Deserialize<List<BiomeData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading biome data: {json}");
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(biomeDataLocation + ": file not found.");
            }

            return biomeData;
        }

        // Loads a single named tileset (Data/TileSet_{name}.json — see
        // Data/TileSetData.cs). Unlike every other LoadXData() above, this
        // throws on a bad/missing/empty catalog rather than silently returning
        // an empty result — a dungeon genuinely cannot generate without a real
        // tileset (no walkable tiles = no floor, no solid tiles = no walls), so
        // failing loudly here beats DungeonGenerator failing confusingly later.
        public static TileSetData LoadTileSetData(string name)
        {
            string path = TileSetDataLocation(name);
            string json;

            using (StreamReader r = new(path))
            {
                Debug.WriteLine(path + ": reading data.");
                json = r.ReadToEnd();
                Debug.WriteLine(json);
            }

            TileSetData tileSet;
            try
            {
                tileSet = JsonSerializer.Deserialize<TileSetData>(json);
            }
            catch (System.Text.Json.JsonException e)
            {
                throw new InvalidOperationException($"{path}: malformed tileset JSON.", e);
            }

            if (tileSet == null || tileSet.Tiles == null || tileSet.Tiles.Count == 0)
            {
                throw new InvalidOperationException($"{path}: tileset defines no tiles.");
            }

            if (!tileSet.Tiles.Any(t => t.CanPassThrough))
            {
                throw new InvalidOperationException(
                    $"{path}: tileset has no CanPassThrough tile — a dungeon needs at least one "
                        + "walkable tile to use as floor."
                );
            }

            if (!tileSet.Tiles.Any(t => !t.CanPassThrough))
            {
                throw new InvalidOperationException(
                    $"{path}: tileset has no non-CanPassThrough tile — a dungeon needs at least "
                        + "one solid tile to use as wall."
                );
            }

            List<int> duplicateIds = tileSet
                .Tiles.GroupBy(t => t.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{path}: duplicate tile Id(s) {string.Join(", ", duplicateIds)} — each tile "
                        + "needs a unique Id within its own tileset, or later entries silently "
                        + "overwrite earlier ones."
                );
            }

            return tileSet;
        }

        // Loads a single named dungeon type (Data/DungeonType_{name}.json —
        // see Data/DungeonTypeData.cs). Same "throw on a bad config rather
        // than silently returning something broken" shape as LoadTileSetData
        // above — a dungeon type with no real enemy or an unresolvable boss
        // reference can't actually run, so it's better to fail loudly here
        // than to have DungeonState fail confusingly (or worse, silently
        // spawn nothing) later.
        public static DungeonTypeData LoadDungeonTypeData(string name)
        {
            string path = DungeonTypeDataLocation(name);
            string json;

            using (StreamReader r = new(path))
            {
                Debug.WriteLine(path + ": reading data.");
                json = r.ReadToEnd();
                Debug.WriteLine(json);
            }

            DungeonTypeData dungeonType;
            try
            {
                dungeonType = JsonSerializer.Deserialize<DungeonTypeData>(json);
            }
            catch (System.Text.Json.JsonException e)
            {
                throw new InvalidOperationException($"{path}: malformed dungeon type JSON.", e);
            }

            if (dungeonType == null || string.IsNullOrWhiteSpace(dungeonType.TileSetName))
            {
                throw new InvalidOperationException($"{path}: dungeon type has no TileSetName.");
            }

            if (
                dungeonType.EnemyNames == null
                || EnemySpawner.ResolveFactories(dungeonType.EnemyNames).Length == 0
            )
            {
                throw new InvalidOperationException(
                    $"{path}: dungeon type's EnemyNames resolved to zero real enemies."
                );
            }

            if (
                string.IsNullOrWhiteSpace(dungeonType.BossName)
                || !Portal.Destination.BossesByName.ContainsKey(dungeonType.BossName)
            )
            {
                throw new InvalidOperationException(
                    $"{path}: dungeon type's BossName '{dungeonType.BossName}' doesn't match any "
                        + "known boss."
                );
            }

            return dungeonType;
        }

        public static void SaveInventoryData()
        {
            List<InventoryData> inventoryData = [];

            // Every slot, including empty (null) ones, written as an explicit
            // null-item entry — so the saved list stays exactly
            // MAXIMUM_SLOTS_IN_INVENTORY long and LoadInventoryData can place
            // each entry back at the same index, preserving gaps instead of
            // compacting them away.
            for (int i = 0; i < Player.Instance.Inventory.InventoryRecords.Length; i++)
            {
                InventorySystem.InventoryRecord record = Player.Instance.Inventory.InventoryRecords[
                    i
                ];

                inventoryData.Add(
                    new InventoryData
                    {
                        InventoryItem = record?.InventoryItem,
                        Quantity = record?.Quantity ?? 0,
                    }
                );
            }

            string json = JsonSerializer.Serialize(inventoryData);
            Debug.WriteLine(json);
            File.WriteAllText(inventoryDataLocation, json);
            Debug.WriteLine("InventoryData Saved.");
        }

        public static void LoadInventoryData()
        {
            List<InventoryData> inventoryData = [];
            try
            {
                using (StreamReader r = new(inventoryDataLocation))
                {
                    Debug.WriteLine(inventoryDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        inventoryData = JsonSerializer.Deserialize<List<InventoryData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading inventory data: {json}");
                    }

                    // Placed at the same index it was saved from — not
                    // appended — so positions survive the round trip. Old
                    // saves (a compact list with no gaps, from before this
                    // format) still load correctly this way, since their
                    // sequential indices are already exactly slots 0..N-1.
                    int count = Math.Min(
                        inventoryData.Count,
                        InventorySystem.MAXIMUM_SLOTS_IN_INVENTORY
                    );
                    for (int i = 0; i < count; i++)
                    {
                        if (inventoryData[i].InventoryItem == null)
                            continue;

                        string itemName = inventoryData[i].InventoryItem.Name;

                        // Older saves (from before Health/Mana potions got their own
                        // dedicated charge counters) may still have them recorded
                        // here — fold them into the counters instead of re-adding
                        // them to the general grid.
                        if (itemName == "Health Potion")
                        {
                            Player.Instance.Inventory.HealthPotionCharges += inventoryData[
                                i
                            ].Quantity;
                            continue;
                        }

                        if (itemName == "Mana Potion")
                        {
                            Player.Instance.Inventory.ManaPotionCharges += inventoryData[
                                i
                            ].Quantity;
                            continue;
                        }

                        Player.Instance.Inventory.InventoryRecords[i] =
                            new InventorySystem.InventoryRecord(
                                inventoryData[i].InventoryItem,
                                inventoryData[i].Quantity
                            );
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(inventoryDataLocation + ": file not found.");
            }
        }

        public static void SaveBankData()
        {
            List<InventoryData> bankData = [];

            // Every slot, including empty (null) ones — same reasoning as
            // SaveInventoryData, so positions survive a reload.
            for (int i = 0; i < BankSystem.Records.Length; i++)
            {
                InventorySystem.InventoryRecord record = BankSystem.Records[i];

                bankData.Add(
                    new InventoryData
                    {
                        InventoryItem = record?.InventoryItem,
                        Quantity = record?.Quantity ?? 0,
                    }
                );
            }

            string json = JsonSerializer.Serialize(bankData);
            Debug.WriteLine(json);
            File.WriteAllText(bankDataLocation, json);
            Debug.WriteLine("BankData Saved.");
        }

        public static void LoadBankData()
        {
            List<InventoryData> bankData = [];
            try
            {
                using (StreamReader r = new(bankDataLocation))
                {
                    Debug.WriteLine(bankDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        bankData = JsonSerializer.Deserialize<List<InventoryData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading bank data: {json}");
                    }

                    // Placed at the same index it was saved from — see
                    // LoadInventoryData for why. A pre-shrink save with more
                    // than MAXIMUM_SLOTS_IN_BANK entries (from when the bank
                    // was 16 slots) simply loses whatever doesn't fit past the
                    // new cap.
                    int count = Math.Min(bankData.Count, BankSystem.MAXIMUM_SLOTS_IN_BANK);
                    for (int i = 0; i < count; i++)
                    {
                        if (bankData[i].InventoryItem == null)
                            continue;

                        BankSystem.Records[i] = new InventorySystem.InventoryRecord(
                            bankData[i].InventoryItem,
                            bankData[i].Quantity
                        );
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(bankDataLocation + ": file not found.");
            }
        }

        public static void SaveFameData()
        {
            FameData fameData = new() { Fame = FameSystem.Fame };
            string json = JsonSerializer.Serialize(fameData);
            File.WriteAllText(fameDataLocation, json);
            Debug.WriteLine("FameData Saved.");
        }

        public static void LoadFameData()
        {
            try
            {
                using StreamReader r = new(fameDataLocation);
                string json = r.ReadToEnd();
                FameData fameData = JsonSerializer.Deserialize<FameData>(json);
                FameSystem.Fame = fameData?.Fame ?? 0;
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(fameDataLocation + ": file not found.");
            }
        }

        public static void SaveCharacterSlotsData()
        {
            string json = JsonSerializer.Serialize(CharacterSlotSystem.ToData());
            File.WriteAllText(characterSlotsDataLocation, json);
            Debug.WriteLine("CharacterSlotsData Saved.");
        }

        public static void LoadCharacterSlotsData()
        {
            try
            {
                using StreamReader r = new(characterSlotsDataLocation);
                string json = r.ReadToEnd();
                CharacterSlotsData data = JsonSerializer.Deserialize<CharacterSlotsData>(json);
                CharacterSlotSystem.LoadFromData(data);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(characterSlotsDataLocation + ": file not found.");

                // No manifest yet — either a genuinely fresh account, or an
                // install that predates the character-slot rework and still
                // has real characters saved under the old
                // PlayerData_{ClassName}.json naming. Runs exactly once:
                // after this, CharacterSlotsData.json exists either way.
                MigrateLegacySavesIfNeeded();
            }
        }

        public static void SaveClassRecordsData()
        {
            string json = JsonSerializer.Serialize(ClassRecordSystem.ToData());
            File.WriteAllText(classRecordsDataLocation, json);
            Debug.WriteLine("ClassRecordsData Saved.");
        }

        public static void LoadClassRecordsData()
        {
            try
            {
                using StreamReader r = new(classRecordsDataLocation);
                string json = r.ReadToEnd();
                ClassRecordsData data = JsonSerializer.Deserialize<ClassRecordsData>(json);
                ClassRecordSystem.LoadFromData(data);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(classRecordsDataLocation + ": file not found.");
            }
        }

        // The pre-slot-system save naming — one PlayerData/InventoryData
        // pair per class, keyed by class name instead of by character ID.
        // Only ever referenced here, by the one-time migration below.
        private static string LegacyPlayerDataLocation(Player.Class playerClass) =>
            Path.Combine(AppContext.BaseDirectory, $"PlayerData_{playerClass}.json");

        private static string LegacyInventoryDataLocation(Player.Class playerClass) =>
            Path.Combine(AppContext.BaseDirectory, $"InventoryData_{playerClass}.json");

        // One-time migration from the old "one save per class name" scheme
        // to the new per-character-ID one — see LoadCharacterSlotsData()
        // above, the only caller. Copies (never moves, until every migrated
        // character and both new account-wide files are confirmed written)
        // each existing legacy PlayerData_{Class}.json/
        // InventoryData_{Class}.json pair to its new PlayerData_{ID}.json/
        // InventoryData_{ID}.json name (ID taken from the legacy file's own
        // already-populated PlayerData.ID — every real save already has
        // one, just never used as the file's own key before now), registers
        // a slot for it, and seeds ClassRecordSystem from its HighScore so
        // the star-unlock chain doesn't regress. Grandfathers
        // UnlockedSlotCount up to however many real characters were found
        // (floored at the normal starting 2) — an existing player who
        // legitimately already has up to 5 characters under the old model
        // isn't asked to re-buy slots they already earned. If anything
        // throws partway through, every old file is left untouched and no
        // manifest is written — fails toward "nothing happened" rather than
        // a half-migrated account.
        private static void MigrateLegacySavesIfNeeded()
        {
            try
            {
                List<CharacterSlotEntryData> migratedEntries = [];
                int slotIndex = 0;

                foreach (
                    Player.Class playerClass in new[]
                    {
                        Player.Class.Wizard,
                        Player.Class.Archer,
                        Player.Class.Knight,
                        Player.Class.Priest,
                        Player.Class.Rogue,
                    }
                )
                {
                    string legacyPlayerPath = LegacyPlayerDataLocation(playerClass);
                    if (!File.Exists(legacyPlayerPath))
                        continue;

                    PlayerData legacyData;
                    using (StreamReader r = new(legacyPlayerPath))
                    {
                        legacyData = JsonSerializer.Deserialize<PlayerData>(r.ReadToEnd());
                    }

                    if (legacyData == null)
                        continue;

                    Guid characterId = legacyData.ID;
                    DateTime lastPlayedUtc = File.GetLastWriteTimeUtc(legacyPlayerPath);

                    File.Copy(legacyPlayerPath, PlayerDataLocation(characterId), overwrite: true);

                    string legacyInventoryPath = LegacyInventoryDataLocation(playerClass);
                    if (File.Exists(legacyInventoryPath))
                    {
                        File.Copy(
                            legacyInventoryPath,
                            InventoryDataLocation(characterId),
                            overwrite: true
                        );
                    }

                    migratedEntries.Add(
                        new CharacterSlotEntryData
                        {
                            SlotIndex = slotIndex++,
                            CharacterId = characterId,
                            PlayerClass = playerClass,
                            LastPlayedUtc = lastPlayedUtc,
                        }
                    );

                    ClassRecordSystem.RecordHighScore(playerClass, legacyData.HighScore);
                }

                CharacterSlotSystem.Entries = migratedEntries;
                CharacterSlotSystem.UnlockedSlotCount = Math.Max(2, migratedEntries.Count);

                SaveCharacterSlotsData();
                SaveClassRecordsData();

                // Only delete the old files once every copy and both new
                // account-wide files are confirmed written.
                foreach (CharacterSlotEntryData entry in migratedEntries)
                {
                    string legacyPlayerPath = LegacyPlayerDataLocation(entry.PlayerClass);
                    if (File.Exists(legacyPlayerPath))
                        File.Delete(legacyPlayerPath);

                    string legacyInventoryPath = LegacyInventoryDataLocation(entry.PlayerClass);
                    if (File.Exists(legacyInventoryPath))
                        File.Delete(legacyInventoryPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Legacy save migration failed, leaving old files untouched: " + ex);
                CharacterSlotSystem.Reset();
                ClassRecordSystem.Reset();
            }
        }

        public static void SaveKeyBindingsData()
        {
            string json = JsonSerializer.Serialize(KeyBindings.ToData());
            File.WriteAllText(keyBindingsDataLocation, json);
            Debug.WriteLine("KeyBindingsData Saved.");
        }

        public static void LoadKeyBindingsData()
        {
            try
            {
                using StreamReader r = new(keyBindingsDataLocation);
                string json = r.ReadToEnd();
                KeyBindingsData data = JsonSerializer.Deserialize<KeyBindingsData>(json);
                KeyBindings.FromData(data);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(keyBindingsDataLocation + ": file not found.");
            }
        }

        // Reads/writes Player.Instance.AutoFireEnabled directly rather than
        // routing through a dedicated manager class the way KeyBindings.cs
        // does for bindings — there's only the one setting so far, so a
        // whole parallel static-class layer isn't earning its keep yet;
        // add one if/when a second setting needs the same generic
        // get/set/reset shape KeyBindings already has.
        public static void SaveGameSettingsData()
        {
            var data = new GameSettingsData
            {
                AutoFireEnabled = Player.Instance.AutoFireEnabled,
                AutoEnterPortalsEnabled = Player.Instance.AutoEnterPortalsEnabled,
                ShowHitboxesEnabled = Player.Instance.ShowHitboxesEnabled,
                ShowQuestIndicatorEnabled = Player.Instance.ShowQuestIndicatorEnabled,
                DisplayItemTiersEnabled = Player.Instance.DisplayItemTiersEnabled,
                LowHealthIndicatorEnabled = Player.Instance.LowHealthIndicatorEnabled,
                LowHealthThresholdPercent = Player.Instance.LowHealthThresholdPercent,
                AlwaysDisplayPlayerHPEnabled = Player.Instance.AlwaysDisplayPlayerHPEnabled,
                ShowXpDropsEnabled = Player.Instance.ShowXpDropsEnabled,
                AlwaysShowExpEnabled = Player.Instance.AlwaysShowExpEnabled,
                ShowPlayerDamageNumbersEnabled = Player.Instance.ShowPlayerDamageNumbersEnabled,
                ShowEnemyDamageNumbersEnabled = Player.Instance.ShowEnemyDamageNumbersEnabled,
                ShowHitParticlesEnabled = Player.Instance.ShowHitParticlesEnabled,
                ShowCombatIndicatorEnabled = Player.Instance.ShowCombatIndicatorEnabled,
                MusicEnabled = Player.Instance.MusicEnabled,
                MusicVolumePercent = Player.Instance.MusicVolumePercent,
                MusicMuted = Player.Instance.MusicMuted,
                SfxVolumePercent = Player.Instance.SfxVolumePercent,
                SfxMuted = Player.Instance.SfxMuted,
                WeaponShotsMuted = Player.Instance.WeaponShotsMuted,
            };
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(gameSettingsDataLocation, json);
            Debug.WriteLine("GameSettingsData Saved.");
        }

        public static void LoadGameSettingsData()
        {
            try
            {
                using StreamReader r = new(gameSettingsDataLocation);
                string json = r.ReadToEnd();
                GameSettingsData data = JsonSerializer.Deserialize<GameSettingsData>(json);
                Player.Instance.AutoFireEnabled = data.AutoFireEnabled;
                Player.Instance.AutoEnterPortalsEnabled = data.AutoEnterPortalsEnabled;
                Player.Instance.ShowHitboxesEnabled = data.ShowHitboxesEnabled;
                Player.Instance.ShowQuestIndicatorEnabled = data.ShowQuestIndicatorEnabled;
                Player.Instance.DisplayItemTiersEnabled = data.DisplayItemTiersEnabled;
                Player.Instance.LowHealthIndicatorEnabled = data.LowHealthIndicatorEnabled;
                Player.Instance.LowHealthThresholdPercent = data.LowHealthThresholdPercent;
                Player.Instance.AlwaysDisplayPlayerHPEnabled = data.AlwaysDisplayPlayerHPEnabled;
                Player.Instance.ShowXpDropsEnabled = data.ShowXpDropsEnabled;
                Player.Instance.AlwaysShowExpEnabled = data.AlwaysShowExpEnabled;
                Player.Instance.ShowPlayerDamageNumbersEnabled = data.ShowPlayerDamageNumbersEnabled;
                Player.Instance.ShowEnemyDamageNumbersEnabled = data.ShowEnemyDamageNumbersEnabled;
                Player.Instance.ShowHitParticlesEnabled = data.ShowHitParticlesEnabled;
                Player.Instance.ShowCombatIndicatorEnabled = data.ShowCombatIndicatorEnabled;
                Player.Instance.MusicEnabled = data.MusicEnabled;
                Player.Instance.MusicVolumePercent = data.MusicVolumePercent;
                Player.Instance.MusicMuted = data.MusicMuted;
                Player.Instance.SfxVolumePercent = data.SfxVolumePercent;
                Player.Instance.SfxMuted = data.SfxMuted;
                Player.Instance.WeaponShotsMuted = data.WeaponShotsMuted;
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(gameSettingsDataLocation + ": file not found.");
            }
        }

        public static int CenterText(String text, SpriteFont font, int x)
        {
            return x - ((int)font.MeasureString(text).X / 2);
        }

        // Tooltips are always drawn extending rightward from their anchor
        // (the hovered item/slot), so anything anchored near the right edge
        // of the window — e.g. the sidebar's inventory/bank/equip slots —
        // would otherwise run off-screen. Shifts the X left just enough to
        // keep the whole panel on-screen, without moving it right of where
        // it was actually anchored (a narrow tooltip stays put).
        private static float ClampTooltipX(float x, float width)
        {
            const int edgeMargin = 4;
            const int padding = 4;
            float maxX = Game1.WindowWidth - width - padding - edgeMargin;
            if (x > maxX)
                x = maxX;
            if (x < edgeMargin)
                x = edgeMargin;
            return x;
        }

        // World-space solid filled circle, e.g. an ability's AoE blast flash
        // (Priest's Nova — see CharacterClasses/Priest.cs's UseAbility()/the
        // NovaRadiusFlash entity it spawns on cast). Rasterized as a stack
        // of 1px-tall horizontal strips (same stretched-1x1-texture
        // technique the rest of this file's drawing helpers already use),
        // each stretched to that row's chord width via the circle equation
        // -- cheap enough for a short-lived one-shot effect, and avoids
        // needing a dedicated filled-circle texture asset.
        public static void DrawFilledCircle(
            SpriteBatch spriteBatch,
            Vector2 center,
            float radius,
            Color color
        )
        {
            int rows = (int)Math.Ceiling(radius * 2f);
            for (int i = 0; i <= rows; i++)
            {
                float y = -radius + i;
                float halfWidth = (float)Math.Sqrt(Math.Max(0f, radius * radius - y * y));
                if (halfWidth <= 0f)
                    continue;

                spriteBatch.Draw(
                    Art.HealthBar,
                    center + new Vector2(-halfWidth, y),
                    null,
                    color,
                    0f,
                    Vector2.Zero,
                    new Vector2(halfWidth * 2f, 1f),
                    SpriteEffects.None,
                    0f
                );
            }
        }

        // A thin, fully-surrounding 1px black outline (not a single
        // bottom-right drop shadow) — drawn as 8 offset copies underneath
        // the real text. The outline's alpha tracks the passed-in color's
        // own alpha (color.A/255), so a fading DamageNumber's outline fades
        // at the same rate as its fill instead of snapping to fully-opaque
        // black while the fill is nearly invisible. scale forwards to
        // DrawString's own scale parameter (DamageNumber draws at various
        // scales; everything else stays at the default 1).
        private static readonly Vector2[] OutlineOffsets =
        {
            new(-1, -1), new(0, -1), new(1, -1),
            new(-1, 0), new(1, 0),
            new(-1, 1), new(0, 1), new(1, 1),
        };

        public static void DrawOutlinedText(
            SpriteBatch spriteBatch,
            SpriteFont font,
            string text,
            Vector2 position,
            Color color,
            float scale = 1f
        )
        {
            Color outlineColor = Color.Black * (color.A / 255f);
            foreach (Vector2 offset in OutlineOffsets)
            {
                spriteBatch.DrawString(
                    font,
                    text,
                    position + (offset * scale),
                    outlineColor,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
            spriteBatch.DrawString(
                font,
                text,
                position,
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }

        // Draws text with a semi-transparent background panel sized to fit it,
        // so hover tooltips (equip slots, bank, inventory) stay readable
        // regardless of what's behind them in the game world/UI. Reuses
        // Art.HealthBar — a solid white 1x1 pixel already loaded for the
        // health bar fill — stretched and tinted, the standard MonoGame way to
        // draw a solid-color rectangle without a dedicated texture. Text
        // itself is drawn outlined (DrawOutlinedText) rather than flat, so it
        // reads clearly against the tooltip's own semi-transparent panel.
        public static void DrawTooltip(
            SpriteBatch spriteBatch,
            SpriteFont font,
            string text,
            Vector2 position,
            Color textColor
        )
        {
            const int padding = 4;
            Vector2 size = font.MeasureString(text);
            position.X = ClampTooltipX(position.X, size.X);

            Rectangle background = new(
                (int)(position.X - padding),
                (int)(position.Y - padding),
                (int)(size.X + (padding * 2)),
                (int)(size.Y + (padding * 2))
            );

            spriteBatch.Draw(Art.HealthBar, background, Color.WhiteSmoke * 0.75f);
            DrawOutlinedText(spriteBatch, font, text, position, textColor);
        }

        // What kind of content a tooltip line actually contains — shared by
        // both tooltip renderers below so the two never drift apart on what
        // counts as which category. Stat covers both "+N Stat" (a positive
        // value/delta) and "-N Stat" (a negative delta, e.g. "-1 Defense" —
        // only ever produced by Equipment.BonusComparisonLines()'s Worse
        // case) as well as the "No bonuses" fallback text.
        private enum TooltipLineCategory
        {
            Plain,
            Stat,
            Damage,
            ManaCost,
        }

        private static TooltipLineCategory CategorizeTooltipLine(string line)
        {
            if (line.StartsWith('+') || line.StartsWith('-') || line == "No bonuses")
                return TooltipLineCategory.Stat;
            if (line.StartsWith("Damage:") || line.StartsWith("Side Damage:"))
                return TooltipLineCategory.Damage;
            if (line.EndsWith("Mana Cost"))
                return TooltipLineCategory.ManaCost;
            return TooltipLineCategory.Plain;
        }

        // A category's own color with no comparison data available — white
        // for plain text (name/description), green for a stat line, red for
        // a damage line, blue for a mana cost line.
        private static Color CategoryBaseColor(TooltipLineCategory category) =>
            category switch
            {
                TooltipLineCategory.Stat => Color.Green,
                TooltipLineCategory.Damage => Color.Red,
                TooltipLineCategory.ManaCost => Color.Blue,
                _ => Color.White,
            };

        // Used by DrawCategorizedTooltip below — an equip slot's own hover
        // tooltip just shows the item's own absolute values with nothing to
        // compare against, so every line gets its plain category color.
        private static Color ClassifyTooltipLine(string line) =>
            CategoryBaseColor(CategorizeTooltipLine(line));

        // Same background-panel technique as the single-string overload
        // above, but colors each line by ClassifyTooltipLine() rather than
        // drawing the whole tooltip in one flat color. Used by each equip
        // slot's own hover tooltip (Weapon/Armor/Ring/AbilityItem.
        // DrawTooltip()), which composes a single TooltipText() string.
        public static void DrawCategorizedTooltip(
            SpriteBatch spriteBatch,
            SpriteFont font,
            string text,
            Vector2 position
        )
        {
            string[] lines = text.Replace("\r\n", "\n").Split('\n');

            const int padding = 4;
            float width = 0f;
            foreach (string line in lines)
                width = Math.Max(width, font.MeasureString(line).X);
            float height = lines.Length * font.LineSpacing;

            position.X = ClampTooltipX(position.X, width);

            Rectangle background = new(
                (int)(position.X - padding),
                (int)(position.Y - padding),
                (int)(width + (padding * 2)),
                (int)(height + (padding * 2))
            );
            spriteBatch.Draw(Art.HealthBar, background, Color.WhiteSmoke * 0.75f);

            for (int i = 0; i < lines.Length; i++)
            {
                DrawOutlinedText(
                    spriteBatch,
                    font,
                    lines[i],
                    position + new Vector2(0, i * font.LineSpacing),
                    ClassifyTooltipLine(lines[i])
                );
            }
        }

        // Same background-panel technique as the single-string overload
        // above, but draws each line with its own color instead of one flat
        // string/color — used for the inventory/bank/loot-bag hover
        // tooltip (Equipment.ComparisonLines()). Stat lines (the ones
        // Equipment.BonusComparisonLines() produces) and Damage lines
        // (Weapon/AbilityItem's own ComparisonLines() override) both use
        // the same real three-way scheme: Gold when this item's value
        // matches what's equipped (TooltipComparison.Same), Green when
        // it's an upgrade, Red when it's a downgrade — regardless of
        // category, so a Damage line reads exactly like a stat line now.
        // TooltipComparison.WrongClass overrides all of that to Gray,
        // whatever category it's on — set only by Weapon/AbilityItem's
        // Damage line when CanEquipByCurrentClass is false, since
        // "better/worse than equipped" is meaningless for an item this
        // class can't even wear. Mana Cost and header text (name/tier/
        // description) keep the older scheme — Gold only on Better, else
        // the category's own base color — deliberately unchanged; they
        // never produce Worse/WrongClass.
        public static void DrawTooltip(
            SpriteBatch spriteBatch,
            SpriteFont font,
            List<(string Text, TooltipComparison Comparison)> lines,
            Vector2 position
        )
        {
            const int padding = 4;

            float width = 0f;
            foreach (var line in lines)
                width = Math.Max(width, font.MeasureString(line.Text).X);
            float height = lines.Count * font.LineSpacing;
            position.X = ClampTooltipX(position.X, width);

            Rectangle background = new(
                (int)(position.X - padding),
                (int)(position.Y - padding),
                (int)(width + (padding * 2)),
                (int)(height + (padding * 2))
            );

            spriteBatch.Draw(Art.HealthBar, background, Color.WhiteSmoke * 0.75f);

            for (int i = 0; i < lines.Count; i++)
            {
                TooltipLineCategory category = CategorizeTooltipLine(lines[i].Text);
                Color color =
                    lines[i].Comparison == TooltipComparison.WrongClass
                        ? Color.Gray
                        : category == TooltipLineCategory.Stat
                            || category == TooltipLineCategory.Damage
                            ? lines[i].Comparison switch
                            {
                                TooltipComparison.Better => Color.Green,
                                TooltipComparison.Worse => Color.Red,
                                _ => Color.Gold,
                            }
                            : lines[i].Comparison == TooltipComparison.Better
                                ? Color.Gold
                                : CategoryBaseColor(category);
                DrawOutlinedText(
                    spriteBatch,
                    font,
                    lines[i].Text,
                    position + new Vector2(0, i * font.LineSpacing),
                    color
                );
            }
        }

        public static string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            StringBuilder sb = new();
            float lineWidth = 0f;
            float spaceWidth = spriteFont.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = spriteFont.MeasureString(word);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }

            return sb.ToString();
        }
    }
}
