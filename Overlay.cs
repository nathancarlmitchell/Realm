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

        // Account-level total, shared across every class. Drawn top-left in
        // the Nexus/a dungeon, the same corner Score/Hi Score used to occupy
        // before entry 193 removed them — not on the title screen (entry 187
        // put it there originally, centered under the title; moved here
        // instead so it's visible during actual play, not just at the menu).
        private const int FameIconSize = 24;
        private const int FameIconTextGap = 6;
        private const int FameOverlayX = 32;
        private const int FameOverlayY = 64;

        public static void DrawFame(SpriteBatch spriteBatch)
        {
            SpriteFont font = Art.RetroFont;
            string text = "Fame: " + FameSystem.Fame;
            Vector2 textSize = font.MeasureString(text);

            Rectangle iconRect = new(
                FameOverlayX,
                FameOverlayY + (int)((textSize.Y - FameIconSize) / 2f),
                FameIconSize,
                FameIconSize
            );
            spriteBatch.Draw(Art.FameIcon, iconRect, Color.White);

            int textX = FameOverlayX + FameIconSize + FameIconTextGap;
            Util.DrawOutlinedText(spriteBatch, font, text, new Vector2(textX, FameOverlayY), Color.White);
        }

        // Sidebar layout. All sections are stacked top-to-bottom at a fixed
        // x, in this order: stats, XP, health, mana, ability, equipment,
        // inventory.
        private const int SidebarPadding = 20;
        private const int SidebarBarHeight = 24;

        // Every bar spans the sidebar's full width minus matching left/right
        // padding, rather than a fixed pixel width that only used the left
        // margin — Game1.SidebarWidth is itself a const, so this stays a
        // compile-time constant too.
        private const int SidebarBarWidth = Game1.SidebarWidth - (SidebarPadding * 2);

        // Fame/XP, HP, and MP stack directly against each other (label and
        // numbers draw inside the bar itself, so there's no separate text
        // row above each one to space out) with only a small gap between
        // them, positioned to sit immediately above the Ability bar
        // (AbilityY) rather than immediately below the stat block above —
        // derived backward from AbilityY so they always end up flush against
        // it regardless of where Ability itself ever moves to.
        private const int AbilityY = 352;
        private const int SidebarBarGap = 6;
        private const int MpBarY = AbilityY - SidebarBarGap - SidebarBarHeight;
        private const int HpBarY = MpBarY - SidebarBarGap - SidebarBarHeight;
        private const int XpBarY = HpBarY - SidebarBarGap - SidebarBarHeight;

        private const int BarTextPadding = 6;

        // Draws a bar's label left-aligned and its numbers center-aligned,
        // both vertically centered within the bar itself — the shared shape
        // every XP/HP/MP/Fame bar below now uses instead of a separate text
        // row above the bar. Uses Art.RetroFont (the game's base HUD font as
        // of entry 202 — see Art.cs) via Util.DrawOutlinedText so the text
        // stays readable sitting directly on top of a busy-colored bar fill.
        private static void DrawBarText(
            SpriteBatch spriteBatch,
            Rectangle barRect,
            string label,
            string numbers,
            Color numbersColor
        )
        {
            SpriteFont font = Art.RetroFont;

            Vector2 labelSize = font.MeasureString(label);
            Util.DrawOutlinedText(
                spriteBatch,
                font,
                label,
                new Vector2(barRect.X + BarTextPadding, barRect.Y + (barRect.Height - labelSize.Y) / 2f),
                Color.White
            );

            Vector2 numbersSize = font.MeasureString(numbers);
            Util.DrawOutlinedText(
                spriteBatch,
                font,
                numbers,
                new Vector2(
                    barRect.X + (barRect.Width - numbersSize.X) / 2f,
                    barRect.Y + (barRect.Height - numbersSize.Y) / 2f
                ),
                numbersColor
            );
        }

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

            DrawMinimap(spriteBatch);
            DrawStats(spriteBatch);
            DrawExperience(spriteBatch);
            DrawHealthSection(spriteBatch);
            DrawCombatIndicator(spriteBatch);
            DrawManaSection(spriteBatch);
            DrawAbilitySection(spriteBatch);
            DrawEquipment(spriteBatch);
            DrawInventory(spriteBatch);
        }

        // A small local-area map in the sidebar's top-right corner — player
        // (always centered, white), portals (cyan, both the Nexus's fixed
        // set and any dungeon's DroppedPortals), and enemies (red, includes
        // a boss if one's alive). Shows a fixed-radius window around the
        // player rather than the whole world/instance — the open Realm is
        // 500,000px per side, so "whole world" would make every blip
        // collapse onto a single pixel; a local window is what actually
        // reads as useful, and works the same way for the much smaller boss
        // arena too. Blips outside the radius clamp to the map's edge
        // (independently per axis, not a true radial clamp — simpler math,
        // still points roughly the right direction) rather than being
        // culled, so a nearby-but-off-radius threat/portal still shows.
        private const int MinimapSize = 130;
        private const int MinimapPadding = 10;
        private const float MinimapWorldRadius = 2000f;

        private static void DrawMinimap(SpriteBatch spriteBatch)
        {
            int mapX = Game1.SidebarX + Game1.SidebarWidth - MinimapSize - MinimapPadding;
            int mapY = MinimapPadding;

            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(mapX, mapY, MinimapSize, MinimapSize),
                Color.Black * 0.6f
            );

            Vector2 mapCenter = new(mapX + MinimapSize / 2f, mapY + MinimapSize / 2f);
            Vector2 playerPos = Player.Instance.Position;

            void DrawBlip(Vector2 worldPos, Color color, int dotSize)
            {
                Vector2 offset = worldPos - playerPos;
                float nx = MathHelper.Clamp(offset.X / MinimapWorldRadius, -1f, 1f);
                float ny = MathHelper.Clamp(offset.Y / MinimapWorldRadius, -1f, 1f);
                Vector2 dotPos =
                    mapCenter + new Vector2(nx, ny) * (MinimapSize / 2f - dotSize / 2f);

                spriteBatch.Draw(
                    Art.HealthBar,
                    new Rectangle(
                        (int)(dotPos.X - dotSize / 2f),
                        (int)(dotPos.Y - dotSize / 2f),
                        dotSize,
                        dotSize
                    ),
                    color
                );
            }

            foreach (Portal portal in Portal.DroppedPortals)
                DrawBlip(portal.Position, Color.Cyan, 5);

            if (Portal.NexusPortals != null)
                foreach (Portal portal in Portal.NexusPortals)
                    DrawBlip(portal.Position, Color.Cyan, 5);

            foreach (Vector2 enemyPos in EntityManager.EnemyPositions)
                DrawBlip(enemyPos, Color.Red, 4);

            // Player last, always dead center, so it's never hidden under a
            // portal/enemy blip that happens to land on the same spot.
            DrawBlip(playerPos, Color.White, 6);
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

            // "Level:" is the widest label of the seven, so it sets where
            // every line's value starts — drawing the label and value as two
            // separate strings (rather than one concatenated string) is what
            // lets the values line up in a column regardless of each label's
            // own width, instead of drifting based on how long that line's
            // particular label happens to be.
            float valueX = x + Art.RetroFont.MeasureString("Level:").X + 4;

            void DrawStatLine(string label, string value, int rowY, Color color)
            {
                Util.DrawOutlinedText(spriteBatch, Art.RetroFont, label, new Vector2(x, rowY), color);
                Util.DrawOutlinedText(spriteBatch, Art.RetroFont, value, new Vector2(valueX, rowY), color);
            }

            DrawStatLine("Level:", Player.Instance.Level.ToString(), y, Color.Red);

            Color color =
                Player.Instance.PermanentAttack >= Player.Instance.MaxAttack ? maxColor : Color.Red;
            DrawStatLine(
                "ATT:",
                Player.Instance.Attack
                    + " ("
                    + Player.Instance.PermanentAttack
                    + " / "
                    + Player.Instance.MaxAttack
                    + ")",
                y + 16,
                color
            );

            color =
                Player.Instance.PermanentDefense >= Player.Instance.MaxDefense
                    ? maxColor
                    : Color.Red;
            DrawStatLine(
                "DEF:",
                Player.Instance.Defense
                    + " ("
                    + Player.Instance.PermanentDefense
                    + " / "
                    + Player.Instance.MaxDefense
                    + ")",
                y + 32,
                color
            );

            color =
                Player.Instance.PermanentSpeed >= Player.Instance.MaxSpeed ? maxColor : Color.Red;
            DrawStatLine(
                "SPD:",
                Player.Instance.Speed
                    + " ("
                    + Player.Instance.PermanentSpeed
                    + " / "
                    + Player.Instance.MaxSpeed
                    + ")",
                y + 48,
                color
            );

            color =
                Player.Instance.PermanentDexterity >= Player.Instance.MaxDexterity
                    ? maxColor
                    : Color.Red;
            DrawStatLine(
                "DEX:",
                Player.Instance.Dexterity
                    + " ("
                    + Player.Instance.PermanentDexterity
                    + " / "
                    + Player.Instance.MaxDexterity
                    + ")",
                y + 64,
                color
            );

            color =
                Player.Instance.PermanentVitality >= Player.Instance.MaxVitality
                    ? maxColor
                    : Color.Red;
            DrawStatLine(
                "VIT:",
                Player.Instance.Vitality
                    + " ("
                    + Player.Instance.PermanentVitality
                    + " / "
                    + Player.Instance.MaxVitality
                    + ")",
                y + 80,
                color
            );

            color =
                Player.Instance.PermanentWisdom >= Player.Instance.MaxWisdom ? maxColor : Color.Red;
            DrawStatLine(
                "WIS:",
                Player.Instance.Wisdom
                    + " ("
                    + Player.Instance.PermanentWisdom
                    + " / "
                    + Player.Instance.MaxWisdom
                    + ")",
                y + 96,
                color
            );

            // Sits in the gap between the stat block (ends at y+96) and
            // DrawExperience (starts at y=160) — only drawn when on, so it
            // never collides with either when off.
            if (Player.Instance.AutoFireEnabled)
            {
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "Auto-Fire: ON",
                    new Vector2(x, y + 116),
                    Color.Cyan
                );
            }
        }

        private static void DrawExperience(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;

            string label;
            string numbers;
            int normalisedFill;

            if (Player.Instance.Level < 20)
            {
                label = "XP";
                numbers = Player.Instance.ExperienceTotal + " / " + Player.Instance.ExperienceNextLevel;

                // The bar fills 0-100% within just the current level (empty
                // right at the start of a level, full right before the next
                // one) — unlike numbers above, which shows the cumulative
                // total and never resets. XpIntoLevel/xpNeededForLevel are
                // ExperienceTotal/ExperienceNextLevel with the current
                // level's own starting threshold subtracted from both, so
                // the ratio reads as "progress since I hit this level"
                // instead of "progress since I hit Level 1". Clamped to
                // 100 — a single large XP gain can briefly put
                // ExperienceTotal past the current ExperienceNextLevel for
                // the one frame before Update()'s own level-up check
                // catches up, which would otherwise draw the fill past the
                // bar's own background for that frame.
                int levelStartXp = Player.CumulativeExperienceForLevel(Player.Instance.Level);
                int xpIntoLevel = Player.Instance.ExperienceTotal - levelStartXp;
                int xpNeededForLevel = Player.Instance.ExperienceNextLevel - levelStartXp;
                normalisedFill = Math.Min(100, (xpIntoLevel * 100 / xpNeededForLevel * 100) / 100);
            }
            else
            {
                // "Once the character reaches level 20, their base fame and
                // class quest progress will be displayed instead [of XP
                // progress to the next level]." Uses the live
                // ExperienceTotal (this run's own currently-growing Base
                // Fame) rather than HighScore — unlike Character Select's
                // permanent star display, this HUD panel should reflect
                // what this run is actively building toward right now.
                int stars = Player.ComputeStars(Player.Instance.ExperienceTotal);
                int currentFame = Player.Instance.BaseFame;

                label = "Fame";

                if (stars >= Player.MaxStars)
                {
                    numbers = currentFame + " (Complete)";
                    normalisedFill = 100;
                }
                else
                {
                    int nextThreshold = Player.ClassQuestFameThresholds[stars];
                    int previousThreshold =
                        stars == 0 ? 0 : Player.ClassQuestFameThresholds[stars - 1];
                    numbers = currentFame + " / " + nextThreshold;

                    int fameIntoTier = currentFame - previousThreshold;
                    int fameNeededForTier = nextThreshold - previousThreshold;
                    normalisedFill = Math.Min(
                        100,
                        (fameIntoTier * 100 / fameNeededForTier * 100) / 100
                    );
                }
            }

            Rectangle goldRect = new(0, 0, normalisedFill * SidebarBarWidth / 100, SidebarBarHeight);
            Rectangle blackRect = new(0, 0, SidebarBarWidth, SidebarBarHeight);

            Vector2 barPos = new(x, XpBarY);
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                blackRect,
                Color.Black * 0.5f,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                goldRect,
                Color.Goldenrod,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            Rectangle barRect = new(x, XpBarY, SidebarBarWidth, SidebarBarHeight);
            DrawBarText(spriteBatch, barRect, label, numbers, Color.White);
        }

        private static void DrawHealthSection(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;

            int normalisedHealth =
                (Player.Instance.Health * 100 / Player.Instance.HealthMax * 100) / 100;
            Rectangle greenRect = new(0, 0, normalisedHealth * SidebarBarWidth / 100, SidebarBarHeight);
            Rectangle redRect = new(0, 0, SidebarBarWidth, SidebarBarHeight);

            Vector2 barPos = new(x, HpBarY);
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                redRect,
                Color.DarkRed * 0.5f,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                greenRect,
                Color.DarkGreen,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            Color numbersColor =
                Player.Instance.Health >= Player.Instance.HealthMax ? Color.Gold : Color.White;
            string numbers = Player.Instance.Health + " / " + Player.Instance.HealthMax;
            if (Player.Instance.EquipmentMaxHealthBonus > 0)
                numbers += " (+" + Player.Instance.EquipmentMaxHealthBonus + ")";

            Rectangle barRect = new(x, HpBarY, SidebarBarWidth, SidebarBarHeight);
            DrawBarText(spriteBatch, barRect, "HP", numbers, numbersColor);
        }

        // Vital Combat's two HUD indicators: a yellow outline around the HP
        // bar itself while InCombat (gated behind ShowCombatIndicatorEnabled,
        // anchored to HpBarY like before), and a small sword badge (real art,
        // Art.CombatBadge — a placeholder tinted square before) that "lights
        // up" (gold vs dim gray tint) unconditionally — per the design doc's
        // own wording, only the border is behind the setting. The badge sits
        // just above the Fame bar, left-aligned to match the bars below it
        // (moved here from below the minimap per direct user request — that
        // position itself was only a fix for entry 198 widening the HP bar
        // out from under the badge's original spot).
        private const int CombatIconSize = 20;
        private const int CombatBorderThickness = 2;
        private const int CombatIconY = XpBarY - SidebarBarGap - CombatIconSize;

        private static void DrawCombatIndicator(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;

            if (Player.Instance.InCombat && Player.Instance.ShowCombatIndicatorEnabled)
            {
                Rectangle barRect = new(
                    x,
                    HpBarY,
                    SidebarBarWidth,
                    SidebarBarHeight
                );
                DrawBorder(spriteBatch, barRect, Color.Yellow, CombatBorderThickness);
            }

            Rectangle iconRect = new(x, CombatIconY, CombatIconSize, CombatIconSize);
            Color iconColor = Player.Instance.InCombat ? Color.Gold : Color.DarkGray;
            spriteBatch.Draw(Art.CombatBadge, iconRect, iconColor);

            if (iconRect.Intersects(Input.MouseBounds))
            {
                string status = Player.Instance.InCombat ? "In Combat" : "Out of Combat";
                string text =
                    status
                    + Environment.NewLine
                    + "Combat Trigger: "
                    + Player.Instance.CombatTrigger
                    + Environment.NewLine
                    + "Combat Duration: "
                    + Player.Instance.CombatDurationSeconds.ToString("0.0")
                    + "s";
                Color textColor = Player.Instance.InCombat ? Color.Gold : Color.White;

                // Anchored above-left of the icon — the badge sits well
                // clear of the stat block above it, so there's open space
                // for a tooltip to expand upward without overlapping
                // anything.
                Vector2 textSize = Art.RetroFont.MeasureString(text);
                Vector2 tooltipPos = new(iconRect.X, iconRect.Y - textSize.Y - 10);
                Util.DrawTooltip(spriteBatch, Art.RetroFont, text, tooltipPos, textColor);
            }
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(Art.HealthBar, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(Art.HealthBar, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height),
                color
            );
        }

        private static void DrawManaSection(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;

            int normalisedMana = (Player.Instance.Mana * 100 / Player.Instance.ManaMax * 100) / 100;
            Rectangle blueRect = new(0, 0, normalisedMana * SidebarBarWidth / 100, SidebarBarHeight);
            Rectangle blackRect = new(0, 0, SidebarBarWidth, SidebarBarHeight);

            Vector2 barPos = new(x, MpBarY);
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                blackRect,
                Color.Black * 0.5f,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                blueRect,
                Color.DarkBlue,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            Color numbersColor =
                Player.Instance.Mana >= Player.Instance.ManaMax ? Color.Gold : Color.White;
            string numbers = Player.Instance.Mana + " / " + Player.Instance.ManaMax;
            if (Player.Instance.EquipmentMaxManaBonus > 0)
                numbers += " (+" + Player.Instance.EquipmentMaxManaBonus + ")";

            Rectangle barRect = new(x, MpBarY, SidebarBarWidth, SidebarBarHeight);
            DrawBarText(spriteBatch, barRect, "MP", numbers, numbersColor);
        }

        // Grouped right after Mana since it's mana-cost based — resolved via
        // AskUserQuestion whether this should move into the sidebar at all
        // (it wasn't in the original list of six items); the user chose to
        // move it, grouped with mana.
        private static void DrawAbilitySection(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;
            int y = AbilityY;
            int abilityBarHeight = SidebarBarHeight / 2;

            // No ability item equipped means there's nothing to ready up for
            // (UseAbility() itself now errors out rather than doing anything
            // — see the AbilityItem.IsEquipped guard added to each class) —
            // show a flat grey bar with no readiness text instead of a
            // ready/charging state that doesn't actually apply.
            if (!Player.Instance.AbilityItem.IsEquipped)
            {
                Rectangle emptyRect = new(0, 0, SidebarBarWidth, abilityBarHeight);
                spriteBatch.Draw(
                    Art.HealthBar,
                    new Vector2(x, y + 20),
                    emptyRect,
                    Color.Gray * 0.5f,
                    0f,
                    Vector2.Zero,
                    1f,
                    0,
                    0
                );
                return;
            }

            Color maxColor = Color.LimeGreen;
            Color defaultColor = Color.White;
            Color color =
                Player.Instance.Mana >= Player.Instance.AbilityCost ? maxColor : defaultColor;
            string abilityString =
                Player.Instance.Mana >= Player.Instance.AbilityCost
                    ? "Ability: Ready (Cost: " + Player.Instance.AbilityCost + ")"
                    : "Ability: " + Player.Instance.Mana + " / " + Player.Instance.AbilityCost;
            Util.DrawOutlinedText(spriteBatch, Art.RetroFont, abilityString, new Vector2(x, y), color);

            // Clamped since Mana can exceed AbilityCost, unlike Health/Mana
            // which are capped at their Max.
            int normalisedAbility = Math.Min(
                100,
                (Player.Instance.Mana * 100 / Player.Instance.AbilityCost * 100) / 100
            );
            Rectangle cyanRect = new(0, 0, normalisedAbility * SidebarBarWidth / 100, abilityBarHeight);
            Rectangle blackRect = new(0, 0, SidebarBarWidth, abilityBarHeight);

            Vector2 barPos = new(x, y + 20);
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                blackRect,
                Color.Black * 0.5f,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                cyanRect,
                Color.DarkCyan,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );
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

            // Tooltips drawn in a separate pass, after every slot's
            // border/icon above, so a tooltip is never at risk of a later
            // slot's icon painting over it — worth keeping even now that
            // this draw order matches the slots' actual left-to-right screen
            // order (Weapon, AbilityItem, Armor, Ring), since a future
            // reorder of either one could silently reintroduce that bug. At
            // most one of these actually draws anything, since only one slot
            // can be hovered at a time.
            Player.Instance.Weapon.DrawTooltip(spriteBatch);
            Player.Instance.AbilityItem.DrawTooltip(spriteBatch);
            Player.Instance.Armor.DrawTooltip(spriteBatch);
            Player.Instance.Ring.DrawTooltip(spriteBatch);
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

            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                "EntityManager.Count: " + EntityManager.Count,
                pos,
                Color.White
            );

            pos = new Vector2(x, y + 16);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                "Camera.Pos: " + Game1.Camera.Pos,
                pos,
                Color.White
            );

            pos = new Vector2(x, y + 32);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                "Player.Pos: " + Player.Instance.Position,
                pos,
                Color.White
            );
            pos = new Vector2(x, y + 48);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
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
                Util.DrawOutlinedText(spriteBatch, Art.RetroFont, potionBonusLines[i], pos, Color.White);
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
