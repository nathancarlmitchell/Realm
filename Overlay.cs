using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    public static class Overlay
    {
        public static void DrawTitle(SpriteBatch spriteBatch)
        {
            // Draw title.
            SpriteFont font = Art.TitleFont;
            string text = "Realm";

            int x = (int)((Game1.ScreenWidth / 2) - (font.MeasureString(text).X / 2));
            int y = 128 / Game1.Scale;

            // Draw text shadow.
            spriteBatch.DrawString(
                font,
                text,
                new Vector2(x - 4, y + 4),
                Color.DarkOrange * (1.0f - 0.25f)
            );

            // Draw text.
            spriteBatch.DrawString(font, text, new Vector2(x, y), Color.DarkMagenta);
        }

        // Account-level total, shared across every class — unlike Score/Hi
        // Score below, which belong to whichever class is currently loaded.
        public static void DrawFame(SpriteBatch spriteBatch)
        {
            SpriteFont font = Art.HudFont;
            string text = "Fame: " + FameSystem.Fame;

            int x = (int)((Game1.ScreenWidth / 2) - (font.MeasureString(text).X / 2));
            int y = (128 / Game1.Scale) + 48;

            spriteBatch.DrawString(font, text, new Vector2(x, y), Color.White);
        }

        public static void DrawScore(SpriteBatch spriteBatch)
        {
            // Draw Score.
            var color = Color.Black;
            if (Player.Instance.ExperienceTotal >= Player.Instance.HighScore)
            {
                color = Color.Yellow;
            }
            spriteBatch.DrawString(
                Art.HudFont,
                "Score: " + Player.Instance.ExperienceTotal,
                new Vector2(32, 64),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Hi Score: " + Player.Instance.HighScore,
                new Vector2(32, 92),
                color
            );
        }

        // Sidebar layout. All sections are stacked top-to-bottom at a fixed
        // x, in this order: stats, XP, health, mana, ability, equipment,
        // inventory. Bars are half the scale/height of the old gameplay-area
        // versions (which were sized for a much wider strip) so they fit the
        // narrower sidebar with margin on both sides.
        private const int SidebarPadding = 20;
        private const int SidebarBarScale = 2;
        private const int SidebarBarHeight = 24;

        // Draws the sidebar panel background plus every HUD section, in the
        // order the user asked for. Replaces what used to be four separate
        // calls (DrawStats/DrawHealth/DrawEquipment/DrawInventory) from each
        // gameplay state's Draw() — consolidated here so RealmState and
        // NexusState can't drift out of sync on section order.
        public static void DrawSidebar(SpriteBatch spriteBatch)
        {
            Rectangle panelRect = new(
                Game1.SidebarX,
                0,
                Game1.SidebarWidth,
                Game1.GameplayViewportHeight
            );
            spriteBatch.Draw(Art.HealthBar, panelRect, Color.Black * 0.5f);

            DrawStats(spriteBatch);
            DrawExperience(spriteBatch);
            DrawHealthSection(spriteBatch);
            DrawManaSection(spriteBatch);
            DrawAbilitySection(spriteBatch);
            DrawEquipment(spriteBatch);
            DrawInventory(spriteBatch);
        }

        // The six core stats. Level/Experience text used to live here too,
        // duplicating what the XP section below already shows — dropped in
        // favor of just "Level: N", since the actual progress bar belongs
        // with Experience/ExperienceNextLevel instead.
        private static void DrawStats(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;
            int y = 20;

            Color maxColor = Color.LimeGreen;

            spriteBatch.DrawString(
                Art.HudFont,
                "Level: " + Player.Instance.Level,
                new Vector2(x, y),
                Color.Red
            );

            Color color = Player.Instance.Attack >= Player.Instance.MaxAttack ? maxColor : Color.Red;
            spriteBatch.DrawString(
                Art.HudFont,
                "Attack: " + Player.Instance.Attack,
                new Vector2(x, y + 20),
                color
            );

            color = Player.Instance.Defense >= Player.Instance.MaxDefense ? maxColor : Color.Red;
            spriteBatch.DrawString(
                Art.HudFont,
                "Defense: " + Player.Instance.Defense,
                new Vector2(x, y + 36),
                color
            );

            color = Player.Instance.Speed >= Player.Instance.MaxSpeed ? maxColor : Color.Red;
            spriteBatch.DrawString(
                Art.HudFont,
                "Speed: " + Player.Instance.Speed,
                new Vector2(x, y + 52),
                color
            );

            color = Player.Instance.Dexterity >= Player.Instance.MaxDexterity ? maxColor : Color.Red;
            spriteBatch.DrawString(
                Art.HudFont,
                "Dexterity: " + Player.Instance.Dexterity,
                new Vector2(x, y + 68),
                color
            );

            color = Player.Instance.Vitality >= Player.Instance.MaxVitality ? maxColor : Color.Red;
            spriteBatch.DrawString(
                Art.HudFont,
                "Vitality: " + Player.Instance.Vitality,
                new Vector2(x, y + 84),
                color
            );

            color = Player.Instance.Wisdom >= Player.Instance.MaxWisdom ? maxColor : Color.Red;
            spriteBatch.DrawString(
                Art.HudFont,
                "Wisdom: " + Player.Instance.Wisdom,
                new Vector2(x, y + 100),
                color
            );
        }

        private static void DrawExperience(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;
            int y = 160;

            string expString =
                Player.Instance.Level < 20
                    ? "Exp: " + Player.Instance.Experience + " / " + Player.Instance.ExperienceNextLevel
                    : "Experience: " + Player.Instance.ExperienceTotal;
            spriteBatch.DrawString(Art.HudFont, expString, new Vector2(x, y), Color.White);

            int normalisedExp = (Player.Instance.Experience * 100 / Player.Instance.ExperienceNextLevel * 100) / 100;
            Rectangle goldRect =
                Player.Instance.Level < 20
                    ? new(0, 0, normalisedExp * SidebarBarScale, SidebarBarHeight)
                    : new(0, 0, 100 * SidebarBarScale, SidebarBarHeight);
            Rectangle blackRect = new(0, 0, 100 * SidebarBarScale, SidebarBarHeight);

            Vector2 barPos = new(x, y + 20);
            spriteBatch.Draw(Art.HealthBar, barPos, blackRect, Color.Black * 0.5f, 0f, Vector2.Zero, 1f, 0, 0);
            spriteBatch.Draw(Art.HealthBar, barPos, goldRect, Color.Goldenrod, 0f, Vector2.Zero, 1f, 0, 0);
        }

        private static void DrawHealthSection(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;
            int y = 224;

            Color color = Player.Instance.HealthMax >= Player.Instance.MaxHealth ? Color.LimeGreen : Color.White;
            spriteBatch.DrawString(
                Art.HudFont,
                "HP: " + Player.Instance.Health + " / " + Player.Instance.HealthMax,
                new Vector2(x, y),
                color
            );

            int normalisedHealth = (Player.Instance.Health * 100 / Player.Instance.HealthMax * 100) / 100;
            Rectangle greenRect = new(0, 0, normalisedHealth * SidebarBarScale, SidebarBarHeight);
            Rectangle redRect = new(0, 0, 100 * SidebarBarScale, SidebarBarHeight);

            Vector2 barPos = new(x, y + 20);
            spriteBatch.Draw(Art.HealthBar, barPos, redRect, Color.DarkRed * 0.5f, 0f, Vector2.Zero, 1f, 0, 0);
            spriteBatch.Draw(Art.HealthBar, barPos, greenRect, Color.DarkGreen, 0f, Vector2.Zero, 1f, 0, 0);
        }

        private static void DrawManaSection(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;
            int y = 288;

            Color color = Player.Instance.ManaMax >= Player.Instance.MaxMana ? Color.LimeGreen : Color.White;
            spriteBatch.DrawString(
                Art.HudFont,
                "Mana: " + Player.Instance.Mana + " / " + Player.Instance.ManaMax,
                new Vector2(x, y),
                color
            );

            int normalisedMana = (Player.Instance.Mana * 100 / Player.Instance.ManaMax * 100) / 100;
            Rectangle blueRect = new(0, 0, normalisedMana * SidebarBarScale, SidebarBarHeight);
            Rectangle blackRect = new(0, 0, 100 * SidebarBarScale, SidebarBarHeight);

            Vector2 barPos = new(x, y + 20);
            spriteBatch.Draw(Art.HealthBar, barPos, blackRect, Color.Black * 0.5f, 0f, Vector2.Zero, 1f, 0, 0);
            spriteBatch.Draw(Art.HealthBar, barPos, blueRect, Color.DarkBlue, 0f, Vector2.Zero, 1f, 0, 0);
        }

        // Grouped right after Mana since it's mana-cost based — resolved via
        // AskUserQuestion whether this should move into the sidebar at all
        // (it wasn't in the original list of six items); the user chose to
        // move it, grouped with mana.
        private static void DrawAbilitySection(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;
            int y = 352;
            int abilityBarHeight = SidebarBarHeight / 2;

            Color maxColor = Color.LimeGreen;
            Color defaultColor = Color.White;
            Color color = Player.Instance.Mana >= Player.Instance.AbilityCost ? maxColor : defaultColor;
            string abilityString =
                Player.Instance.Mana >= Player.Instance.AbilityCost
                    ? "Ability: Ready (Cost: " + Player.Instance.AbilityCost + ")"
                    : "Ability: " + Player.Instance.Mana + " / " + Player.Instance.AbilityCost;
            spriteBatch.DrawString(Art.HudFont, abilityString, new Vector2(x, y), color);

            // Clamped since Mana can exceed AbilityCost, unlike Health/Mana
            // which are capped at their Max.
            int normalisedAbility = Math.Min(
                100,
                (Player.Instance.Mana * 100 / Player.Instance.AbilityCost * 100) / 100
            );
            Rectangle cyanRect = new(0, 0, normalisedAbility * SidebarBarScale, abilityBarHeight);
            Rectangle blackRect = new(0, 0, 100 * SidebarBarScale, abilityBarHeight);

            Vector2 barPos = new(x, y + 20);
            spriteBatch.Draw(Art.HealthBar, barPos, blackRect, Color.Black * 0.5f, 0f, Vector2.Zero, 1f, 0, 0);
            spriteBatch.Draw(Art.HealthBar, barPos, cyanRect, Color.DarkCyan, 0f, Vector2.Zero, 1f, 0, 0);
        }

        private static void DrawEquipment(SpriteBatch spriteBatch)
        {
            // Draw weapon.
            Player.Instance.Weapon.DrawEquipped(spriteBatch);

            // Draw ability.
            Player.Instance.AbilityItem.DrawEquipped(spriteBatch);

            // Draw armor.
            Player.Instance.Armor.DrawEquipped(spriteBatch);

            // Draw ring.
            Player.Instance.Ring.DrawEquipped(spriteBatch);
        }

        private static void DrawInventory(SpriteBatch spriteBatch)
        {
            Player.Instance.Inventory.Draw(spriteBatch);
        }

        public static void DrawDebug(SpriteBatch spriteBatch)
        {
            float x = 64;
            float y = 256;
            Vector2 pos = new Vector2(x, y);

            spriteBatch.DrawString(
                Art.HudFont,
                "EntityManager.Count: " + EntityManager.Count,
                pos,
                Color.White
            );

            pos = new Vector2(x, y + 16);
            spriteBatch.DrawString(
                Art.HudFont,
                "Camera.Pos: " + Game1.Camera.Pos,
                pos,
                Color.White
            );

            pos = new Vector2(x, y + 32);
            spriteBatch.DrawString(
                Art.HudFont,
                "Player.Pos: " + Player.Instance.Position,
                pos,
                Color.White
            );
            pos = new Vector2(x, y + 48);
            spriteBatch.DrawString(
                Art.HudFont,
                "Game1.WorldBounds: " + Game1.WorldBounds,
                pos,
                Color.White
            );

            string[] potionBonusLines =
            {
                "PotionAttackBonus: " + Player.Instance.PotionAttackBonus,
                "PotionDefenseBonus: " + Player.Instance.PotionDefenseBonus,
                "PotionSpeedBonus: " + Player.Instance.PotionSpeedBonus,
                "PotionDexterityBonus: " + Player.Instance.PotionDexterityBonus,
                "PotionVitalityBonus: " + Player.Instance.PotionVitalityBonus,
                "PotionWisdomBonus: " + Player.Instance.PotionWisdomBonus,
                "PotionHealthMaxBonus: " + Player.Instance.PotionHealthMaxBonus,
                "PotionManaMaxBonus: " + Player.Instance.PotionManaMaxBonus,
            };

            for (int i = 0; i < potionBonusLines.Length; i++)
            {
                pos = new Vector2(x, y + 72 + (i * 16));
                spriteBatch.DrawString(Art.HudFont, potionBonusLines[i], pos, Color.White);
            }
        }

        private static float muteCooldown = 1.0f;

        public static void ToggleAudio()
        {
            muteCooldown = 1.0f;
        }

        public static void DrawAudio(SpriteBatch spriteBatch)
        {
            if (muteCooldown > 0.0f)
            {
                muteCooldown -= 0.005f;

                if (Game1.Mute)
                {
                    spriteBatch.Draw(
                        Art.Mute,
                        new Vector2(0, Game1.ScreenHeight - (Art.Mute.Height)),
                        Color.White * muteCooldown
                    );
                }
                else
                {
                    spriteBatch.Draw(
                        Art.Unmute,
                        new Vector2(0, Game1.ScreenHeight - (Art.Unmute.Height)),
                        Color.White * muteCooldown
                    );
                }
            }
        }
    }
}
