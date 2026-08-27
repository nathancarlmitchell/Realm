using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private static string PlayerDataLocation(Player.Class playerClass) =>
            Path.Combine(AppContext.BaseDirectory, $"PlayerData_{playerClass}.json");

        private static string playerDataLocation => PlayerDataLocation(Player.PlayerClass);

        private static string InventoryDataLocation(Player.Class playerClass) =>
            Path.Combine(AppContext.BaseDirectory, $"InventoryData_{playerClass}.json");

        private static string inventoryDataLocation => InventoryDataLocation(Player.PlayerClass);

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

        private static string weaponDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "WeaponData.json"
        );

        // Bows live in their own catalog file, separate from
        // WeaponData.json — see Data/BowData.cs and LoadBowData() below.
        private static string bowDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "BowData.json"
        );

        private static string armorDataLocation = Path.Combine(
            AppContext.BaseDirectory,
            "ArmorData.json"
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

        // Read-only peek at a class's save data, without touching Player.Instance or
        // Player.PlayerClass. Returns null if that class has no save yet.
        //
        // CharacterSelectState.Update() calls this for every class slot on
        // every single frame, so a missing save file (never-played class,
        // or right after Erase All Data) is a routine, expected outcome
        // here — not something to detect via a caught exception. Checking
        // File.Exists() first avoids throwing (and a debugger reporting) a
        // FileNotFoundException every frame for as long as that screen
        // stays open with a locked/never-played class showing.
        public static PlayerData PeekPlayerData(Player.Class playerClass)
        {
            string path = PlayerDataLocation(playerClass);
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

        // Resets a class's save data (stats, level, equipment, inventory) back to a
        // fresh Level-1 character, the same as a never-played class — except its
        // High Score persists, since that's meant to survive a character's death or
        // deletion rather than reset with everything else. Does not touch
        // Player.Instance if that class happens to be currently loaded in memory —
        // callers that care (e.g. deleting the character you're actively playing)
        // need to reset the live instance themselves, or a later autosave would
        // silently recreate the file from stale in-memory stats.
        public static void DeleteCharacterData(Player.Class playerClass)
        {
            PlayerData existing = PeekPlayerData(playerClass);
            int highScore = existing?.HighScore ?? 0;
            bool hasReachedLevel20 = existing?.HasReachedLevel20 ?? false;

            // Whatever Base Fame + Bonus Fame this character had built up is
            // about to be wiped either way (file deleted outright, or reset
            // back to defaults below) — salvage it into the account-level
            // Fame total before that happens, same conversion GameOverState
            // applies on an actual death. Unlike HighScore, neither of these
            // is otherwise preserved by a delete, so there's no
            // double-counting risk here.
            FameSystem.AddFame(
                Player.ComputeBaseFame(existing?.ExperienceTotal ?? 0) + (existing?.BonusFame ?? 0)
            );
            SaveFameData();

            string inventoryPath = InventoryDataLocation(playerClass);
            if (File.Exists(inventoryPath))
                File.Delete(inventoryPath);

            string playerPath = PlayerDataLocation(playerClass);

            if (highScore <= 0 && !hasReachedLevel20)
            {
                if (File.Exists(playerPath))
                    File.Delete(playerPath);
                return;
            }

            // Preserve the High Score (and the permanent star-rating flag —
            // see Player.HasReachedLevel20) by writing back a fresh default
            // character instead of deleting the file outright. LoadOrCreatePlayer
            // always reconstructs the class from scratch first (ResetPlayer) and
            // then copies several fields (Level, Experience, etc.) straight from
            // the save with no null-check, so this has to be a fully valid Level-1
            // snapshot, not a blank/zeroed one — BuildPlayerData against a
            // just-reset Player.Instance gives exactly that. The swap is safe
            // because nothing else runs on this thread between saving and
            // restoring Player.Instance/PlayerClass. ResetPlayer's freshly
            // constructed instance also naturally has HasBeenPlayed = false,
            // which is what should land in the file here (unlike HighScore,
            // that flag is meant to reset on delete).
            Player previousInstance = Player.Instance;
            Player.Class previousClass = Player.PlayerClass;

            ResetPlayer(playerClass);
            PlayerData defaultData = BuildPlayerData();
            defaultData.HighScore = highScore;
            defaultData.HasReachedLevel20 = hasReachedLevel20;

            Player.Instance = previousInstance;
            Player.PlayerClass = previousClass;

            string json = JsonSerializer.Serialize(defaultData);
            File.WriteAllText(playerPath, json);
        }

        // Full account wipe — every class's PlayerData/InventoryData, the
        // shared BankData, and account-wide FameData. Deliberately does NOT
        // preserve anything (unlike DeleteCharacterData, which keeps a
        // class's HighScore/HasReachedLevel20 on purpose) — the caller is
        // expected to have gotten explicit, doubly-confirmed user intent
        // first, since this is irreversible and there's no separate backup.
        public static void EraseAllAccountData()
        {
            foreach (
                Player.Class playerClass in new[]
                {
                    Player.Class.Wizard,
                    Player.Class.Archer,
                    Player.Class.Knight,
                    Player.Class.Priest,
                }
            )
            {
                string playerPath = PlayerDataLocation(playerClass);
                if (File.Exists(playerPath))
                    File.Delete(playerPath);

                string inventoryPath = InventoryDataLocation(playerClass);
                if (File.Exists(inventoryPath))
                    File.Delete(inventoryPath);
            }

            if (File.Exists(bankDataLocation))
                File.Delete(bankDataLocation);

            if (File.Exists(fameDataLocation))
                File.Delete(fameDataLocation);

            // Clear in-memory state too — Records is a fixed-size array
            // (readonly reference, mutable contents), and class unlocks/
            // Fame-gated UI read FameSystem.Fame live rather than from a
            // separate "unlocked" flag, so zeroing it here is what actually
            // re-locks Archer/Knight immediately. Without clearing both, a
            // later autosave (e.g. entering the Nexus) would silently
            // resurrect the just-deleted data from stale memory.
            Array.Clear(BankSystem.Records, 0, BankSystem.Records.Length);
            FameSystem.Fame = 0;

            Player.Class currentClass = Player.PlayerClass;
            EntityManager.RemovePlayer();
            ResetPlayer(currentClass);
            EntityManager.Add(Player.Instance);
        }

        // There's no single save file to read a "last played class" from anymore now
        // that saves are per-class, so infer it from whichever class's save file was
        // written most recently. Falls back to Wizard if neither exists yet.
        public static Player.Class DetermineLastPlayedClass()
        {
            (Player.Class playerClass, string path)[] candidates =
            [
                (Player.Class.Wizard, PlayerDataLocation(Player.Class.Wizard)),
                (Player.Class.Archer, PlayerDataLocation(Player.Class.Archer)),
                (Player.Class.Knight, PlayerDataLocation(Player.Class.Knight)),
                (Player.Class.Priest, PlayerDataLocation(Player.Class.Priest)),
            ];

            Player.Class? mostRecent = null;
            DateTime mostRecentWriteTime = DateTime.MinValue;

            foreach (var (playerClass, path) in candidates)
            {
                if (!File.Exists(path))
                    continue;

                // Strictly greater (not >=) on later candidates so an exact
                // tie keeps whichever class was checked first — matches the
                // original two-class comparison, which favored Wizard on a
                // tie against Archer.
                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (mostRecent == null || writeTime > mostRecentWriteTime)
                {
                    mostRecent = playerClass;
                    mostRecentWriteTime = writeTime;
                }
            }

            return mostRecent ?? Player.Class.Wizard;
        }

        // Used by the main menu's Nexus button to decide whether jumping straight
        // into gameplay makes sense, or no class has ever actually been played yet
        // (Player.Instance defaults to a fresh Wizard at boot regardless — see
        // DetermineLastPlayedClass — so that alone can't answer this).
        public static bool AnyCharacterHasBeenPlayed() =>
            (PeekPlayerData(Player.Class.Wizard)?.HasBeenPlayed ?? false)
            || (PeekPlayerData(Player.Class.Archer)?.HasBeenPlayed ?? false)
            || (PeekPlayerData(Player.Class.Knight)?.HasBeenPlayed ?? false)
            || (PeekPlayerData(Player.Class.Priest)?.HasBeenPlayed ?? false);

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
            }
        }

        // Constructs the given class and, if it has a save, layers that save's stats
        // (and inventory) on top. Used both at game boot and when a character is
        // chosen from Character Select, so there's exactly one place that knows how
        // to do this correctly (construct first, then apply save data — not the
        // other way around, which discards the loaded stats as soon as the
        // constructor resets them to base values).
        public static void LoadOrCreatePlayer(Player.Class playerClass)
        {
            PlayerData saved = PeekPlayerData(playerClass);

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

                // Spell/Quiver/Shield/Tome are only populated when actually
                // equipped (see SavePlayerData's `as Spell`/`as Quiver`/
                // `as Shield`/`as Tome` — a blank AbilityItem casts to none
                // of them), so all four being null unambiguously means
                // "unequipped" — same signal Weapon/Armor/Ring get from a
                // null Name. Without this else, ResetPlayer's
                // constructor-equipped default Tier-0 ability item was
                // never cleared, so it stayed equipped alongside whatever
                // the player had actually dragged into inventory — the
                // reported duplicate ability item.
                if (saved.Spell != null && saved.Spell.Name != null)
                    Spell.LoadSpell(saved.Spell.Name);
                else if (saved.Quiver != null && saved.Quiver.Name != null)
                    Quiver.LoadQuiver(saved.Quiver.Name);
                else if (saved.Shield != null && saved.Shield.Name != null)
                    Shield.LoadShield(saved.Shield.Name);
                else if (saved.Tome != null && saved.Tome.Name != null)
                    Tome.LoadTome(saved.Tome.Name);
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

        // Snapshots Player.Instance/Player.PlayerClass into a PlayerData DTO. Shared
        // by SavePlayerData (snapshotting the live, currently-playing character) and
        // DeleteCharacterData (snapshotting a freshly-constructed default character,
        // to write back after wiping a save while preserving its High Score).
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

        public static void SaveWeaponData()
        {
            List<WeaponData> weaponData = [];

            Weapon playerWeapon = Player.Instance.Weapon;

            WeaponData weapon = new()
            {
                Type = playerWeapon.Type,
                Description = playerWeapon.Description,
                Tier = playerWeapon.Tier,
                DamageMin = playerWeapon.DamageMin,
                DamageMax = playerWeapon.DamageMax,
                ProjectileMagnitude = playerWeapon.ProjectileMagnitude,
                ProjectileDuration = playerWeapon.ProjectileDuration,
                Name = playerWeapon.Name,
                ImageName = playerWeapon.ImageName,
                ProjectileImageName = playerWeapon.ProjectileImageName,
            };

            weaponData.Add(weapon);

            string json = JsonSerializer.Serialize(weaponData);
            Debug.WriteLine(json);
            File.WriteAllText(weaponDataLocation, json);
            Debug.WriteLine("WeaponData Saved.");
        }

        public static List<Weapon> LoadWeaponData()
        {
            List<WeaponData> weaponData = [];
            List<Weapon> weapons = [];

            try
            {
                using (StreamReader r = new(weaponDataLocation))
                {
                    Debug.WriteLine(weaponDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    Debug.WriteLine(json);
                    try
                    {
                        weaponData = JsonSerializer.Deserialize<List<WeaponData>>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading weapon data: {json}");
                    }
                }

                for (int i = 0; i < weaponData.Count; i++)
                {
                    Texture2D weaponTexture = Game1.Instance.Content.Load<Texture2D>(
                        weaponData[i].ImageName
                    );

                    Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                        weaponData[i].ProjectileImageName
                    );

                    weapons.Add(
                        new Weapon(weaponTexture, projectileTexture)
                        {
                            Type = weaponData[i].Type,
                            Name = weaponData[i].Name,
                            Description = weaponData[i].Description,
                            Tier = weaponData[i].Tier,
                            DamageMin = weaponData[i].DamageMin,
                            DamageMax = weaponData[i].DamageMax,
                            ProjectileMagnitude = weaponData[i].ProjectileMagnitude,
                            ProjectileDuration = weaponData[i].ProjectileDuration,
                            ImageName = weaponData[i].ImageName,
                            ProjectileImageName = weaponData[i].ProjectileImageName,
                            Amplitude = weaponData[i].Amplitude,
                            Frequency = weaponData[i].Frequency,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(weaponDataLocation + ": file not found.");
            }

            return weapons;
        }

        // Bows live in their own catalog file (see Data/BowData.cs) rather
        // than WeaponData.json — unlike every other weapon type, a Bow
        // needs two independent damage ranges and two independent
        // projectile textures (Main/Side), not the single pair WeaponData
        // has. Returns plain Weapon.WeaponType.Bow entries, meant to be
        // merged into the same combined Weapons list as LoadWeaponData()'s
        // result (see Game1.StartGame()) — Weapon.LoadWeapon() and
        // Player.cs's EquipHighestTierWeapon() both search that one list by
        // Name, unaware of which file an entry originally came from.
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

        public static List<Armor> LoadArmorData()
        {
            List<ArmorData> armorData = [];
            List<Armor> armors = [];

            try
            {
                using (StreamReader r = new(armorDataLocation))
                {
                    Debug.WriteLine(armorDataLocation + ": reading data.");
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
                            Type = armorData[i].Type,
                            Tier = armorData[i].Tier,
                            MaxHealthBonus = armorData[i].MaxHealthBonus,
                            MaxManaBonus = armorData[i].MaxManaBonus,
                            AttackBonus = armorData[i].AttackBonus,
                            DefenseBonus = armorData[i].DefenseBonus,
                            SpeedBonus = armorData[i].SpeedBonus,
                            DexterityBonus = armorData[i].DexterityBonus,
                            VitalityBonus = armorData[i].VitalityBonus,
                            WisdomBonus = armorData[i].WisdomBonus,
                            ImageName = armorData[i].ImageName,
                        }
                    );
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(armorDataLocation + ": file not found.");
            }

            return armors;
        }

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
                LowHealthIndicatorEnabled = Player.Instance.LowHealthIndicatorEnabled,
                LowHealthThresholdPercent = Player.Instance.LowHealthThresholdPercent,
                ShowXpDropsEnabled = Player.Instance.ShowXpDropsEnabled,
                AlwaysShowExpEnabled = Player.Instance.AlwaysShowExpEnabled,
                ShowPlayerDamageNumbersEnabled = Player.Instance.ShowPlayerDamageNumbersEnabled,
                ShowEnemyDamageNumbersEnabled = Player.Instance.ShowEnemyDamageNumbersEnabled,
                ShowHitParticlesEnabled = Player.Instance.ShowHitParticlesEnabled,
                ShowCombatIndicatorEnabled = Player.Instance.ShowCombatIndicatorEnabled,
                ShowPlayerHealthBarEnabled = Player.Instance.ShowPlayerHealthBarEnabled,
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
                Player.Instance.LowHealthIndicatorEnabled = data.LowHealthIndicatorEnabled;
                Player.Instance.LowHealthThresholdPercent = data.LowHealthThresholdPercent;
                Player.Instance.ShowXpDropsEnabled = data.ShowXpDropsEnabled;
                Player.Instance.AlwaysShowExpEnabled = data.AlwaysShowExpEnabled;
                Player.Instance.ShowPlayerDamageNumbersEnabled = data.ShowPlayerDamageNumbersEnabled;
                Player.Instance.ShowEnemyDamageNumbersEnabled = data.ShowEnemyDamageNumbersEnabled;
                Player.Instance.ShowHitParticlesEnabled = data.ShowHitParticlesEnabled;
                Player.Instance.ShowCombatIndicatorEnabled = data.ShowCombatIndicatorEnabled;
                Player.Instance.ShowPlayerHealthBarEnabled = data.ShowPlayerHealthBarEnabled;
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

        // Colors a single tooltip line by what it actually contains — white
        // for plain text (name/description), green for a stat bonus line
        // ("+N Stat" or "No bonuses"), red for a damage line, blue for a
        // mana cost line. Shared by both tooltip renderers below so the two
        // never drift apart on what counts as which category.
        private static Color ClassifyTooltipLine(string line)
        {
            if (line.StartsWith('+') || line == "No bonuses")
                return Color.Green;
            if (line.StartsWith("Damage:") || line.StartsWith("Side Damage:"))
                return Color.Red;
            if (line.EndsWith("Mana Cost"))
                return Color.Blue;
            return Color.White;
        }

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
        // string/color — used for the inventory/bank hover tooltip
        // (Equipment.ComparisonLines()). Each line's base color comes from
        // the same ClassifyTooltipLine() rules DrawCategorizedTooltip uses
        // (so the two tooltips agree on what a stat-bonus/damage/mana-cost
        // line looks like), with Gold overriding that whenever the line
        // beats the currently equipped item — a distinct "this is an
        // upgrade" signal that stays visible regardless of the line's own
        // category color, rather than the old flat red-default/dark-green-
        // if-better scheme.
        public static void DrawTooltip(
            SpriteBatch spriteBatch,
            SpriteFont font,
            List<(string Text, bool Better)> lines,
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
                Color color = lines[i].Better ? Color.Gold : ClassifyTooltipLine(lines[i].Text);
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
