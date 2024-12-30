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
using Realm.Data;
using Realm.States;

namespace Realm
{
    public static class Util
    {
        private static string playerDataLocation = "PlayerData.json";
        private static string weaponDataLocation = "WeaponData.json";
        private static string inventoryDataLocation = "InventoryData.json";

        //private static string skinDataLocation = "SkinData.json";
        //private static string trophyDataLocation = "TrophyData.json";

        private static readonly string defaultPlayerData = @"{""ExperienceTotal"":0}";

        //private static readonly string defaultSkinData =
        //    @"[{""Name"":""anim_idle_default"",""Selected"":true,""Locked"":false,""Cost"":0,""Frames"":2,""FPS"":2},
        //        {""Name"":""anim_idle_pink"",""Selected"":false,""Locked"":true,""Cost"":3,""Frames"":2,""FPS"":2},
        //        {""Name"":""anim_idle_clear"",""Selected"":false,""Locked"":true,""Cost"":5,""Frames"":2,""FPS"":2},
        //        {""Name"":""anim_idle_rubiks"",""Selected"":false,""Locked"":true,""Cost"":10,""Frames"":4,""FPS"":12},
        //        {""Name"":""anim_idle_companion"",""Selected"":false,""Locked"":true,""Cost"":15,""Frames"":4,""FPS"":8},
        //        {""Name"":""anim_idle_dice"",""Selected"":false,""Locked"":true,""Cost"":10,""Frames"":6,""FPS"":12},
        //        {""Name"":""anim_idle_locked"",""Selected"":false,""Locked"":true,""Cost"":10,""Frames"":2,""FPS"":2},
        //        {""Name"":""anim_idle_tv"",""Selected"":false,""Locked"":true,""Cost"":15,""Frames"":4,""FPS"":16}]";

        //private static readonly string defaultTrophyData =
        //    @"[{""Name"":""Tiny Wings"",""Selected"":false,""Locked"":true,""Description"":""Score 1,000 points"",""Frames"": 1,""FPS"": 1},
        //        {""Name"":""Angry Birds"",""Selected"":false,""Locked"":true,""Description"":""Score 5,000 points"",""Frames"": 1,""FPS"": 1},
        //        {""Name"":""Flight Simulator"",""Selected"":false,""Locked"":true,""Description"":""Score 10,000 points"",""Frames"": 1,""FPS"": 1},
        //        {""Name"":""Sunscreen"",""Selected"":false,""Locked"":true,""Description"":""Unlock all skins"",""Frames"": 1,""FPS"": 1},
        //        {""Name"":""Bezos"",""Selected"":false,""Locked"":true,""Description"":""Save 100 coins"",""Frames"": 1,""FPS"": 1}]";

        //public static void CheckOS()
        //{
        //    if (OperatingSystem.IsAndroid())
        //    {
        //        Debug.WriteLine("Device running Android.");
        //        // AppContext.BaseDirectory = "/data/user/0/FlappyBox.Android/files".
        //        gameDataLocation = AppContext.BaseDirectory + "/" + gameDataLocation;
        //        skinDataLocation = AppContext.BaseDirectory + "/" + skinDataLocation;
        //        trophyDataLocation = AppContext.BaseDirectory + "/" + trophyDataLocation;
        //    }
        //}

        public static void LoadPlayerData()
        {
            PlayerData playerData = new();

            try
            {
                using (StreamReader r = new(playerDataLocation))
                {
                    Debug.WriteLine(playerDataLocation + ": reading data.");
                    string json = r.ReadToEnd();
                    try
                    {
                        playerData = JsonSerializer.Deserialize<PlayerData>(json);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        Debug.WriteLine($"Error loading game data: {json}");
                        json = defaultPlayerData;

                        playerData = JsonSerializer.Deserialize<PlayerData>(json);
                        Debug.WriteLine($"Default data loaded: {json}");
                    }
                    Debug.WriteLine(json);
                }

                Player.ID = playerData.ID;
                Player.Name = playerData.Name;
                Player.Description = playerData.Description;
                //Player.Health = playerData.HealthMax;
                Player.HealthMax = playerData.HealthMax;
                //Player.Mana = playerData.ManaMax;
                Player.ManaMax = playerData.ManaMax;
                Player.Attack = playerData.Attack;
                Player.Defense = playerData.Defense;
                Player.Vitality = playerData.Vitality;
                Player.Wisdom = playerData.Wisdom;
                Player.Speed = playerData.Speed;
                Player.Dexterity = playerData.Dexterity;
                Player.Experience = playerData.Experience;
                Player.ExperienceNextLevel = playerData.ExperienceNextLevel;
                Player.ExperienceTotal = playerData.ExperienceTotal;
                Player.Level = playerData.Level;
                Weapon.LoadWeapon(playerData.Weapon.Name);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(playerDataLocation + ": file not found.");
            }
        }

        public static void SavePlayerData()
        {
            PlayerData playerData = new()
            {
                ID = Player.ID,
                Name = Player.Name,
                Description = Player.Description,
                //Health = Player.Health,
                HealthMax = Player.HealthMax,
                //Mana = Player.Mana,
                ManaMax = Player.ManaMax,
                Attack = Player.Attack,
                Defense = Player.Defense,
                Vitality = Player.Vitality,
                Wisdom = Player.Wisdom,
                Speed = Player.Speed,
                Dexterity = Player.Dexterity,
                Experience = Player.Experience,
                ExperienceNextLevel = Player.ExperienceNextLevel,
                ExperienceTotal = Player.ExperienceTotal,
                Level = Player.Level,
                Weapon = Player.Instance.Weapon,
            };

            string json = JsonSerializer.Serialize(playerData);
            Debug.WriteLine(json);
            File.WriteAllText(playerDataLocation, json);

            Debug.WriteLine("GameData Saved.");
        }

        public static void SaveWeaponData()
        {
            List<WeaponData> weaponData = [];

            Weapon playerWeapon = Player.Instance.Weapon;

            WeaponData weapon = new()
            {
                Type = playerWeapon.Type,
                Description = playerWeapon.Description,
                Teir = playerWeapon.Teir,
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
                            Teir = weaponData[i].Teir,
                            DamageMin = weaponData[i].DamageMin,
                            DamageMax = weaponData[i].DamageMax,
                            ProjectileMagnitude = weaponData[i].ProjectileMagnitude,
                            ProjectileDuration = weaponData[i].ProjectileDuration,
                            ImageName = weaponData[i].ImageName,
                            ProjectileImageName = weaponData[i].ProjectileImageName,
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

        public static void SaveInventoryData()
        {
            List<InventoryData> inventoryData = [];

            for (int i = 0; i < Player.Instance.Inventory.InventoryRecords.Count; i++)
            {
                InventoryData item = new()
                {
                    InventoryItem = Player.Instance.Inventory.InventoryRecords[i].InventoryItem,
                    Quantity = Player.Instance.Inventory.InventoryRecords[i].Quantity,
                };

                inventoryData.Add(item);
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
                        //json = defaultSkinData;
                        //skinData = JsonSerializer.Deserialize<List<SkinData>>(json);
                        //Debug.WriteLine($"Default data loaded: {json}");
                    }

                    for (int i = 0; i < inventoryData.Count; i++)
                    {
                        Player.Instance.Inventory.InventoryRecords.Add(
                            new InventorySystem.InventoryRecord(
                                inventoryData[i].InventoryItem,
                                inventoryData[i].Quantity
                            )
                        );
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine(inventoryDataLocation + ": file not found.");
            }
        }

        //public static void LoadSkinData(ContentManager content)
        //{
        //    List<SkinData> skinData = new List<SkinData>();
        //    SkinsState.Skins = new List<Skin>();

        //    try
        //    {
        //        using (StreamReader r = new StreamReader(skinDataLocation))
        //        {
        //            Debug.WriteLine(skinDataLocation + ": reading data.");
        //            string json = r.ReadToEnd();
        //            try
        //            {
        //                skinData = JsonSerializer.Deserialize<List<SkinData>>(json);
        //            }
        //            catch (System.Text.Json.JsonException)
        //            {
        //                Debug.WriteLine($"Error loading skin data: {json}");
        //                json = defaultSkinData;
        //                skinData = JsonSerializer.Deserialize<List<SkinData>>(json);
        //                Debug.WriteLine($"Default data loaded: {json}");
        //            }

        //            for (int i = 0; i < skinData.Count; i++)
        //            {
        //                Skin skin = new(content, skinData[i].Name)
        //                {
        //                    Name = skinData[i].Name,
        //                    Selected = skinData[i].Selected,
        //                    Locked = skinData[i].Locked,
        //                    Cost = skinData[i].Cost,
        //                    Frames = skinData[i].Frames,
        //                    FPS = skinData[i].FPS,
        //                };
        //                skin.LoadTexture(content, skin.Name);
        //                SkinsState.Skins.Add(skin);
        //            }
        //        }
        //    }
        //    catch (System.IO.FileNotFoundException)
        //    {
        //        Debug.WriteLine(skinDataLocation + ": file not found.");
        //        using (FileStream fs = File.Create(skinDataLocation))
        //        {
        //            Debug.WriteLine(skinDataLocation + ": file created.");
        //            byte[] data = new UTF8Encoding(true).GetBytes(defaultSkinData);
        //            fs.Write(data, 0, data.Length);
        //        }
        //    }
        //}

        //public static void SaveSkinData()
        //{
        //    if (SkinsState.Skins is not null)
        //    {
        //        List<SkinData> skinData = new List<SkinData>();
        //        for (int i = 0; i < SkinsState.Skins.Count; i++)
        //        {
        //            SkinData skin = new()
        //            {
        //                Name = SkinsState.Skins[i].Name,
        //                Selected = SkinsState.Skins[i].Selected,
        //                Locked = SkinsState.Skins[i].Locked,
        //                Cost = SkinsState.Skins[i].Cost,
        //                Frames = SkinsState.Skins[i].Frames,
        //                FPS = SkinsState.Skins[i].FPS,
        //            };
        //            skinData.Add(skin);
        //        }
        //        string json = JsonSerializer.Serialize(skinData);
        //        File.WriteAllText(skinDataLocation, json);
        //        Debug.WriteLine("SkinData Saved.");
        //    }
        //}

        //public static void LoadTrophyData(ContentManager content)
        //{
        //    List<TrophyData> trophyData = new List<TrophyData>();
        //    TrophyState.Trophys = new List<Trophy>();

        //    try
        //    {
        //        using (StreamReader r = new StreamReader(trophyDataLocation))
        //        {
        //            Debug.WriteLine(trophyDataLocation + ": reading data.");
        //            string json = r.ReadToEnd();
        //            try
        //            {
        //                trophyData = JsonSerializer.Deserialize<List<TrophyData>>(json);
        //            }
        //            catch (System.Text.Json.JsonException)
        //            {
        //                Debug.WriteLine("Invalid JSON:" + json);
        //            }
        //            for (int i = 0; i < trophyData.Count; i++)
        //            {
        //                Trophy trophy = new(content, trophyData[i].Name)
        //                {
        //                    Name = trophyData[i].Name,
        //                    Selected = false,
        //                    Locked = trophyData[i].Locked,
        //                    Description = trophyData[i].Description,
        //                    Frames = trophyData[i].Frames,
        //                    FPS = trophyData[i].FPS,
        //                };
        //                //trophy.LoadTexture(content, skin.Name);
        //                TrophyState.Trophys.Add(trophy);
        //            }
        //        }
        //    }
        //    catch (System.IO.FileNotFoundException)
        //    {
        //        Debug.WriteLine(trophyDataLocation + ": file not found.");
        //        using (FileStream fs = File.Create(trophyDataLocation))
        //        {
        //            Debug.WriteLine(trophyDataLocation + ": file created.");
        //            byte[] data = new UTF8Encoding(true).GetBytes(defaultTrophyData);
        //            fs.Write(data, 0, data.Length);
        //        }
        //        LoadTrophyData(content);
        //    }
        //}

        //public static void SaveTrophyData()
        //{
        //    if (TrophyState.Trophys is not null)
        //    {
        //        List<TrophyData> trophyData = new List<TrophyData>();
        //        for (int i = 0; i < TrophyState.Trophys.Count; i++)
        //        {
        //            TrophyData trophy = new()
        //            {
        //                Name = TrophyState.Trophys[i].Name,
        //                Selected = false,
        //                Locked = TrophyState.Trophys[i].Locked,
        //                Description = TrophyState.Trophys[i].Description,
        //                Frames = TrophyState.Trophys[i].Frames,
        //                FPS = TrophyState.Trophys[i].FPS,
        //            };
        //            trophyData.Add(trophy);
        //        }
        //        string json = JsonSerializer.Serialize(trophyData);
        //        File.WriteAllText(trophyDataLocation, json);
        //        Debug.WriteLine("TrophyData Saved.");
        //    }
        //}

        //public static void ResetTrophyData()
        //{

        //}

        public static int CenterText(String text, SpriteFont font, int x)
        {
            return x - ((int)font.MeasureString(text).X / 2);
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
