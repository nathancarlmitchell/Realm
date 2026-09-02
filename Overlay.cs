using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm.States;

namespace Realm
{
    public static class Overlay
    {
        // The title (and BossRealmState's boss-announcement banner, which
        // shares this same "big pixel-font title" look) always draws
        // Art.RetroFontLarge at this scale or smaller, never larger —
        // RetroFontLarge is already baked at the intended native title
        // size, so 1 means "full size" here, not a stretch-up factor the
        // way it briefly was when this drew the much smaller RetroFont
        // scaled 6x (blurred badly — stretching a small rasterized glyph
        // bitmap has no lossless answer, unlike shrinking a large one,
        // which is what BossRealmState still does for a long boss name).
        public const float TitleScale = 1f;

        public static void DrawTitle(SpriteBatch spriteBatch)
        {
            SpriteFont font = Art.RetroFontLarge;
            string text = "REALM";
            Vector2 size = font.MeasureString(text) * TitleScale;

            int x = (int)((Game1.ScreenWidth / 2) - (size.X / 2));
            int y = 128 / Game1.Scale;

            // Fill color preserved from the old Arial title (DarkMagenta) —
            // only the font and the shadow-style secondary color changed, to
            // a plain black outline matching every other piece of text now.
            Util.DrawOutlinedText(
                spriteBatch,
                font,
                text,
                new Vector2(x, y),
                Color.DarkMagenta,
                TitleScale
            );
        }

        // Account-level total, shared across every class. Drawn top-left in
        // the Nexus/a dungeon, the same corner Score/Hi Score used to occupy
        // before entry 193 removed them — not on the title screen (entry 187
        // put it there originally, centered under the title; moved here
        // instead so it's visible during actual play, not just at the menu).
        private const int FameIconSize = 24;
        private const int FameIconTextGap = 6;
        private const int FameOverlayX = 32;
        private const int FameOverlayY = 32;

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
            Util.DrawOutlinedText(
                spriteBatch,
                font,
                text,
                new Vector2(textX, FameOverlayY),
                Color.White
            );
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
                new Vector2(
                    barRect.X + BarTextPadding,
                    barRect.Y + (barRect.Height - labelSize.Y) / 2f
                ),
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

        // No longer a flat const — the mouse wheel adjusts this while
        // hovering the map (see HandleMinimapZoom()), so it needs to be a
        // mutable field. 2000f is the original fixed value, kept as the
        // starting zoom level; persists for the rest of the session (not
        // reset per state transition) rather than snapping back to default
        // every time the player changes realms, same as a real settings
        // preference would.
        private static float minimapWorldRadius = 2000f;
        private const float MinimapMinWorldRadius = 500f; // most zoomed in
        private const float MinimapMaxWorldRadius = 6000f; // most zoomed out

        // One step per standard wheel notch (120 units of ScrollWheelValue,
        // MonoGame/Windows' usual unit) — scrolling up (positive delta)
        // zooms in (shrinks the world radius shown), matching how zoom
        // conventionally works in most map UIs.
        private const float MinimapZoomStepPerNotch = 250f;

        // Screen position of a world point's blip on the minimap — pure
        // math, shared by the actual drawing below and
        // HandleMinimapBeaconClick()'s hit test, so the two can never
        // silently disagree on where a blip visually landed. Blips outside
        // minimapWorldRadius clamp to the map's edge (independently per
        // axis, not a true radial clamp — simpler math, still points
        // roughly the right direction) rather than being culled, so a
        // nearby-but-off-radius threat/portal/Beacon still shows.
        private static Vector2 ComputeMinimapBlipPosition(
            Vector2 worldPos,
            Vector2 playerPos,
            Vector2 mapCenter,
            int dotSize
        )
        {
            Vector2 offset = worldPos - playerPos;
            float nx = MathHelper.Clamp(offset.X / minimapWorldRadius, -1f, 1f);
            float ny = MathHelper.Clamp(offset.Y / minimapWorldRadius, -1f, 1f);
            return mapCenter + new Vector2(nx, ny) * (MinimapSize / 2f - dotSize / 2f);
        }

        private static void DrawMinimap(SpriteBatch spriteBatch)
        {
            int mapX = Game1.SidebarX + Game1.SidebarWidth - MinimapSize - MinimapPadding;
            int mapY = MinimapPadding;
            Rectangle mapRect = new(mapX, mapY, MinimapSize, MinimapSize);

            spriteBatch.Draw(Art.HealthBar, mapRect, Color.Black * 0.6f);

            HandleMinimapZoom(mapRect);

            Vector2 mapCenter = new(mapX + MinimapSize / 2f, mapY + MinimapSize / 2f);
            Vector2 playerPos = Player.Instance.Position;

            void DrawBlip(Vector2 worldPos, Color color, int dotSize)
            {
                Vector2 dotPos = ComputeMinimapBlipPosition(
                    worldPos,
                    playerPos,
                    mapCenter,
                    dotSize
                );

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

            // Shown as soon as it exists (before activation too) — a
            // landmark worth heading toward, same idea as a portal blip.
            if (BeachBeacon.ActiveInstance != null)
                DrawBlip(BeachBeacon.ActiveInstance.Position, Color.Cyan, BeaconBlipSize);

            // Player last, always dead center, so it's never hidden under a
            // portal/enemy blip that happens to land on the same spot.
            DrawBlip(playerPos, Color.White, 6);

            HandleMinimapBeaconClick(mapCenter, playerPos);
        }

        // Scroll wheel zoom, only while the mouse is actually over the
        // minimap — split out from DrawMinimap() (input handling, not
        // rendering) so it's independently testable without needing a
        // working SpriteBatch, same reasoning as HandleMinimapBeaconClick()
        // below. ScrollWheelValue is cumulative for the whole session, not
        // per-tick, so the delta (this frame vs. last) is what actually
        // matters — Input.mouse/previousMouse are already the real,
        // per-frame-refreshed OS mouse state (Input.Update()), unlike
        // Controls/Button.cs's own separate Mouse.GetState() polling.
        private static void HandleMinimapZoom(Rectangle mapRect)
        {
            if (!mapRect.Contains(Input.MousePosition))
                return;

            int scrollDelta = Input.mouse.ScrollWheelValue - Input.previousMouse.ScrollWheelValue;
            if (scrollDelta == 0)
                return;

            float notches = scrollDelta / 120f;
            minimapWorldRadius = MathHelper.Clamp(
                minimapWorldRadius - notches * MinimapZoomStepPerNotch,
                MinimapMinWorldRadius,
                MinimapMaxWorldRadius
            );
        }

        // The Beacon's own minimap blip is a small dot — a bit of forgiving
        // padding around its exact pixel footprint (ClickPaddingRadius)
        // keeps the click from being a frustrating pixel-hunt while still
        // requiring the Beacon's blip specifically, not just anywhere on
        // the map.
        private const int BeaconBlipSize = 5;
        private const float BeaconBlipClickPaddingRadius = 4f;

        // Click-to-teleport: only meaningful once this Realm instance's
        // Beacon has actually been activated (walked up to at least once),
        // and only when the click actually lands on the Beacon's own blip
        // — not anywhere on the minimap. No cost or cooldown — a free,
        // repeatable return trip once unlocked. Edge-triggered (release
        // right after a press), same "just clicked" check
        // Controls/Button.cs itself uses, so holding the button down
        // doesn't re-fire every frame. Split out from DrawMinimap() itself
        // (input handling, not rendering) so it's independently testable
        // without needing a working SpriteBatch.
        private static void HandleMinimapBeaconClick(Vector2 mapCenter, Vector2 playerPos)
        {
            BeachBeacon beacon = BeachBeacon.ActiveInstance;

            // ActiveInstance only filters out an expired/torn-down Beacon
            // (see its own comment) — it says nothing about whether this
            // one has actually been reached yet, so IsActivated needs its
            // own explicit check here too.
            if (beacon == null || !beacon.IsActivated)
                return;

            Vector2 blipPos = ComputeMinimapBlipPosition(
                beacon.Position,
                playerPos,
                mapCenter,
                BeaconBlipSize
            );
            float clickRadius = BeaconBlipSize / 2f + BeaconBlipClickPaddingRadius;
            bool clickedBlip =
                Vector2.DistanceSquared(Input.MousePosition, blipPos) <= clickRadius * clickRadius;

            if (
                clickedBlip
                && Input.mouse.LeftButton == ButtonState.Released
                && Input.previousMouse.LeftButton == ButtonState.Pressed
            )
            {
                Player.Instance.Position = beacon.Position;
                Game1.Camera.Pos = Player.Instance.Position;
            }
        }

        // A small arrow that orbits the player on screen, always pointing
        // toward the Beach Beacon's world location — visible whenever one
        // exists in this Realm instance (BeachBeacon.ActiveInstance already
        // self-filters a stale/expired one out, same as the minimap blip
        // above), including before it's activated, when it's most useful
        // for actually finding the thing. Anchored to the gameplay
        // viewport's exact center rather than Player.Instance.Position
        // directly: Game1.Camera.Pos == the player's position every frame
        // (Player.Update()), and Camera.GetTransformation() always maps
        // that world point to precisely the viewport's center — the one
        // documented exception being right at a world edge, where
        // Camera.Pos's own barrier clamp can pull the camera (and so the
        // player's on-screen position) away from center. Not worth
        // compensating for here — Beach sits at the world's origin ring,
        // nowhere near an edge a real playthrough would reach.
        private const float BeaconIndicatorOrbitRadius = 70f;

        // Pure position/rotation math, split out from the actual Draw()
        // call below so it's independently testable without needing a
        // working SpriteBatch/GraphicsDevice. playerPosition/targetPosition
        // are both world coordinates; the returned Position is a screen
        // coordinate (anchored to the viewport center, not the player's raw
        // world position — see DrawIndicatorArrowTowards()'s own comment on
        // why). Generic over the target — originally written just for the
        // Beach Beacon, reused as-is by DungeonState's boss-portal indicator
        // (see DrawIndicatorArrowTowards below).
        private static (Vector2 Position, float Rotation) ComputeIndicatorTransform(
            Vector2 playerPosition,
            Vector2 targetPosition
        )
        {
            float angle = (targetPosition - playerPosition).ToAngle();
            Vector2 playerScreenPos = new(
                Game1.GameplayViewportWidth / 2f,
                Game1.GameplayViewportHeight / 2f
            );
            Vector2 position =
                playerScreenPos + Extensions.FromPolar(angle, BeaconIndicatorOrbitRadius);

            // The source art points up (native forward = -Y, angle -π/2),
            // but this engine's rotation convention everywhere else
            // (Entity.Orientation, fed straight from Velocity.ToAngle()
            // with no offset) assumes a sprite's native forward is +X
            // (angle 0, pointing right) — every projectile/enemy sprite is
            // drawn that way. A +90° correction bridges the two so this
            // arrow's actual on-screen tip lands on `angle`, not 90° off
            // from it.
            float rotation = angle + MathHelper.PiOver2;

            return (position, rotation);
        }

        public static void DrawBeaconIndicator(SpriteBatch spriteBatch)
        {
            BeachBeacon beacon = BeachBeacon.ActiveInstance;
            if (beacon != null)
                DrawIndicatorArrowTowards(spriteBatch, beacon.Position);
        }

        // Generic version of the Beach Beacon arrow above — an orbiting
        // arrow pointing at any fixed world position, gated by the same
        // "Quest Indicator" setting. Second real caller: DungeonState draws
        // one pointing at its boss room's portal.
        public static void DrawIndicatorArrowTowards(SpriteBatch spriteBatch, Vector2 targetPosition)
        {
            if (!Player.Instance.ShowQuestIndicatorEnabled)
                return;

            if (targetPosition == Player.Instance.Position)
                return; // standing exactly on it — no direction to show

            var (arrowPos, rotation) = ComputeIndicatorTransform(
                Player.Instance.Position,
                targetPosition
            );
            Vector2 origin = new(Art.IndicatorArrow.Width / 2f, Art.IndicatorArrow.Height / 2f);

            spriteBatch.Draw(
                Art.IndicatorArrow,
                arrowPos,
                null,
                Color.White,
                rotation,
                origin,
                1f,
                SpriteEffects.None,
                0f
            );
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

            // equipBonus: this stat's own gear contribution (Player.
            // EquipmentXBonus), shown as a gold "+N" to the right of the
            // value — same "call out the gear-only piece in gold" idea as
            // the HP/MP bars' "(+N)" (see DrawHealthSection/DrawManaSection),
            // just as its own separate segment here instead of appended
            // into the value string. Omitted entirely when zero, matching
            // the bars' same "only show it if it's actually nonzero" rule.
            void DrawStatLine(
                string label,
                string value,
                int rowY,
                Color color,
                float equipBonus = 0
            )
            {
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    label,
                    new Vector2(x, rowY),
                    color
                );
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    value,
                    new Vector2(valueX, rowY),
                    color
                );

                if (equipBonus != 0)
                {
                    Vector2 valueSize = Art.RetroFont.MeasureString(value);
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        "+" + equipBonus,
                        new Vector2(valueX + valueSize.X + 8, rowY),
                        Color.Gold
                    );
                }
            }

            // Level used to be the first row here — moved to sit just
            // above the XP/Fame bar instead (centered, DrawExperience()),
            // per direct request. Removing it tightened this block by one
            // row (16px), so ATT is now first at y+0 instead of y+16, and
            // so on down through WIS.
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
                y,
                color,
                Player.Instance.EquipmentAttackBonus
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
                y + 16,
                color,
                Player.Instance.EquipmentDefenseBonus
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
                y + 32,
                color,
                Player.Instance.EquipmentSpeedBonus
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
                y + 48,
                color,
                Player.Instance.EquipmentDexterityBonus
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
                y + 64,
                color,
                Player.Instance.EquipmentVitalityBonus
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
                y + 80,
                color,
                Player.Instance.EquipmentWisdomBonus
            );

            // Sits in the gap between the stat block (ends at y+80, now
            // that Level moved out — see the top of this method) and
            // DrawExperience (starts at y=160) — only drawn when on, so it
            // never collides with either when off.
            if (Player.Instance.AutoFireEnabled)
            {
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "Auto-Fire: ON",
                    new Vector2(x, y + 100),
                    Color.Cyan
                );
            }
        }

        private static void DrawExperience(SpriteBatch spriteBatch)
        {
            int x = Game1.SidebarX + SidebarPadding;

            // Moved here from the top of the stat block (DrawStats()) per
            // direct request — sits just above this bar now, centered
            // across its width, rather than in the label/value column with
            // the other six stats. Shares CombatIconY's row (the existing
            // gap right above this bar, previously only holding the Combat
            // Badge) rather than opening a new one: the badge is small and
            // left-aligned, so a centered label doesn't collide with it.
            string levelText = "Level: " + Player.Instance.Level;
            Vector2 levelSize = Art.RetroFont.MeasureString(levelText);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                levelText,
                new Vector2(x + (SidebarBarWidth - levelSize.X) / 2f, CombatIconY),
                Color.Red
            );

            string label;
            string numbers;
            int normalisedFill;

            if (Player.Instance.Level < 20)
            {
                label = "XP";
                numbers =
                    Player.Instance.ExperienceTotal + " / " + Player.Instance.ExperienceNextLevel;

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

            Rectangle goldRect = new(
                0,
                0,
                normalisedFill * SidebarBarWidth / 100,
                SidebarBarHeight
            );
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
            Rectangle greenRect = new(
                0,
                0,
                normalisedHealth * SidebarBarWidth / 100,
                SidebarBarHeight
            );
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
                Rectangle barRect = new(x, HpBarY, SidebarBarWidth, SidebarBarHeight);
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

        private static void DrawBorder(
            SpriteBatch spriteBatch,
            Rectangle rect,
            Color color,
            int thickness
        )
        {
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(rect.X, rect.Y, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(rect.X, rect.Y, thickness, rect.Height),
                color
            );
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
            Rectangle blueRect = new(
                0,
                0,
                normalisedMana * SidebarBarWidth / 100,
                SidebarBarHeight
            );
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
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                abilityString,
                new Vector2(x, y),
                color
            );

            // Clamped since Mana can exceed AbilityCost, unlike Health/Mana
            // which are capped at their Max.
            int normalisedAbility = Math.Min(
                100,
                (Player.Instance.Mana * 100 / Player.Instance.AbilityCost * 100) / 100
            );
            Rectangle cyanRect = new(
                0,
                0,
                normalisedAbility * SidebarBarWidth / 100,
                abilityBarHeight
            );
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
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    potionBonusLines[i],
                    pos,
                    Color.White
                );
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
