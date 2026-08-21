using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm.Controls;

namespace Realm.States
{
    public class SettingsState : State
    {
        private enum SettingsTab
        {
            Controls,
            Gameplay,
            Audio,
            Graphics,
        }

        private class Row
        {
            public KeyBindings.Action Action;
            public Rectangle Rect;
            public bool Hover;
        }

        private class TabInfo
        {
            public SettingsTab Tab;
            public string Label;
            public Rectangle Rect;
            public bool Hover;
        }

        // Gameplay tab's non-keybinding, plain click-to-toggle settings
        // (Auto-Fire, Auto-Enter Portals) — Get/Set close over whichever
        // Player.Instance bool the row actually controls, so adding a
        // future toggle is just one more list entry instead of a new pair
        // of dedicated Rect/Hover fields and a copy of the same
        // Update()/Draw() block. Generalized from Auto-Fire's original
        // single dedicated autoFireRect/autoFireHover fields now that a
        // second real toggle (Auto-Enter Portals) showed up.
        private class ToggleRow
        {
            public string Label;
            public Rectangle Rect;
            public bool Hover;
            public Func<bool> Get;
            public Action<bool> Set;
        }

        // A clamped, steppable int setting (currently just "Low Health
        // Threshold") — same Get/Set-closure shape as ToggleRow, but two
        // small "-"/"+" hit-rects instead of one whole-row click, since
        // there's a range to move through rather than a plain on/off flip.
        private class NumericRow
        {
            public string Label;
            public Rectangle Rect;
            public Rectangle DecrementRect;
            public Rectangle IncrementRect;
            public bool DecrementHover;
            public bool IncrementHover;
            public Func<int> Get;
            public Action<int> Set;
            public int Step;
            public int Min;
            public int Max;
        }

        // Whichever state was active when Settings was opened (Main Menu,
        // or an in-progress Nexus/Realm) — Back returns straight to this
        // exact object via ChangeState() rather than re-navigating, so
        // nothing about that state (camera, entities, an active dungeon)
        // gets reconstructed just from a round trip through Settings.
        private readonly State returnState;

        private readonly List<Row> rows;
        private readonly List<TabInfo> tabs;
        private readonly Button backButton;
        private readonly Button resetButton;

        // Set while waiting for the next key press to bind to a row —
        // blocks every other row/button/tab from reacting to input until
        // it resolves (a key pressed, or Escape to cancel), same reasoning
        // as CharacterSelectState's ConfirmingDelete gating out normal
        // clicks. Since this also blocks tab clicks, there's no need to
        // separately cancel a pending rebind when switching tabs — it's
        // simply not possible to switch while one is in progress.
        private KeyBindings.Action? listeningFor;

        private SettingsTab currentTab = SettingsTab.Controls;

        private readonly List<ToggleRow> gameplayToggles;
        private readonly List<ToggleRow> graphicsToggles;
        private readonly List<NumericRow> graphicsNumerics;

        private const int RowHeight = 28;
        private const int TabHeight = 32;
        private const int TabGap = 16;
        private const int TabPaddingX = 16;
        private const int StepperButtonWidth = 24;
        private const int StepperValueGap = 50; // room for "100%" between the two buttons

        private int labelX;
        private int valueX;
        private int rowsTop;
        private int tabBarY;

        public SettingsState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content, State returnState)
            : base()
        {
            Game1.Instance.IsMouseVisible = true;

            this.returnState = returnState;

            rows = [];
            foreach (var action in KeyBindings.AllActions)
                rows.Add(new Row { Action = action });

            gameplayToggles =
            [
                new ToggleRow
                {
                    Label = "Auto-Fire",
                    Get = () => Player.Instance.AutoFireEnabled,
                    Set = v => Player.Instance.AutoFireEnabled = v,
                },
                new ToggleRow
                {
                    Label = "Auto-Enter Portals",
                    Get = () => Player.Instance.AutoEnterPortalsEnabled,
                    Set = v => Player.Instance.AutoEnterPortalsEnabled = v,
                },
            ];

            graphicsToggles =
            [
                new ToggleRow
                {
                    Label = "Show Hitboxes",
                    Get = () => Player.Instance.ShowHitboxesEnabled,
                    Set = v => Player.Instance.ShowHitboxesEnabled = v,
                },
                new ToggleRow
                {
                    Label = "Low Health Indicator",
                    Get = () => Player.Instance.LowHealthIndicatorEnabled,
                    Set = v => Player.Instance.LowHealthIndicatorEnabled = v,
                },
            ];

            graphicsNumerics =
            [
                new NumericRow
                {
                    Label = "Low Health Threshold",
                    Get = () => Player.Instance.LowHealthThresholdPercent,
                    Set = v => Player.Instance.LowHealthThresholdPercent = v,
                    Step = 5,
                    Min = 0,
                    Max = 100,
                },
            ];

            // Widest label sets where the key-name column starts, same
            // column-alignment trick as Overlay.DrawStats() — includes the
            // Gameplay/Graphics tabs' toggle/numeric labels too, so their
            // rows line up in the same column as the Controls tab's rows,
            // even though only one tab's content is visible at a time.
            float widestLabel = 0f;
            foreach (var action in KeyBindings.AllActions)
                widestLabel = Math.Max(widestLabel, Art.SettingsFont.MeasureString(KeyBindings.DisplayName(action)).X);
            foreach (var toggle in gameplayToggles)
                widestLabel = Math.Max(widestLabel, Art.SettingsFont.MeasureString(toggle.Label).X);
            foreach (var toggle in graphicsToggles)
                widestLabel = Math.Max(widestLabel, Art.SettingsFont.MeasureString(toggle.Label).X);
            foreach (var numeric in graphicsNumerics)
                widestLabel = Math.Max(widestLabel, Art.SettingsFont.MeasureString(numeric.Label).X);

            labelX = CenterWidth - 160;
            valueX = labelX + (int)widestLabel + 24;

            // Fixed layout, independent of which tab (and therefore how
            // many rows) is currently active — content has to stay in the
            // same place across tab switches, or the whole screen would
            // jump around every time the user clicked a different tab.
            // Sized against the Controls tab (10 rows), the tallest one.
            tabBarY = CenterHeight - 200;
            rowsTop = tabBarY + TabHeight + 20;

            for (int i = 0; i < rows.Count; i++)
            {
                Vector2 rowSize = Art.SettingsFont.MeasureString("A");
                rows[i].Rect = new Rectangle(
                    labelX,
                    rowsTop + i * RowHeight,
                    valueX - labelX + 160,
                    (int)rowSize.Y + 6
                );
            }

            for (int i = 0; i < gameplayToggles.Count; i++)
            {
                gameplayToggles[i].Rect = new Rectangle(
                    labelX,
                    rowsTop + i * RowHeight,
                    valueX - labelX + 160,
                    (int)Art.SettingsFont.MeasureString("A").Y + 6
                );
            }

            for (int i = 0; i < graphicsToggles.Count; i++)
            {
                graphicsToggles[i].Rect = new Rectangle(
                    labelX,
                    rowsTop + i * RowHeight,
                    valueX - labelX + 160,
                    (int)Art.SettingsFont.MeasureString("A").Y + 6
                );
            }

            // Continues right after the Graphics tab's toggle rows, same
            // column, same row height — the two lists together read as one
            // continuous stack even though they're separately typed.
            for (int i = 0; i < graphicsNumerics.Count; i++)
            {
                int rowY = rowsTop + (graphicsToggles.Count + i) * RowHeight;
                int rowHeightPx = (int)Art.SettingsFont.MeasureString("A").Y + 6;
                graphicsNumerics[i].Rect = new Rectangle(labelX, rowY, valueX - labelX + 160, rowHeightPx);
                graphicsNumerics[i].DecrementRect = new Rectangle(valueX, rowY, StepperButtonWidth, rowHeightPx);
                graphicsNumerics[i].IncrementRect = new Rectangle(
                    valueX + StepperButtonWidth + StepperValueGap,
                    rowY,
                    StepperButtonWidth,
                    rowHeightPx
                );
            }

            // Tab bar, centered as a group above the content area.
            tabs = [];
            foreach (SettingsTab tab in Enum.GetValues(typeof(SettingsTab)))
                tabs.Add(new TabInfo { Tab = tab, Label = tab.ToString() });

            float totalTabWidth = TabGap * (tabs.Count - 1);
            foreach (var tab in tabs)
                totalTabWidth += Art.SettingsFont.MeasureString(tab.Label).X + TabPaddingX * 2;

            int tabX = CenterWidth - (int)(totalTabWidth / 2);
            foreach (var tab in tabs)
            {
                int tabWidth = (int)Art.SettingsFont.MeasureString(tab.Label).X + TabPaddingX * 2;
                tab.Rect = new Rectangle(tabX, tabBarY, tabWidth, TabHeight);
                tabX += tabWidth + TabGap;
            }

            // Also fixed, for the same reason as rowsTop/tabBarY above —
            // sized to comfortably clear the Controls tab's full row list
            // regardless of which tab happens to be showing right now.
            int buttonsY = rowsTop + rows.Count * RowHeight + 30;

            backButton = new Button(Art.ButtonTexture, Art.SettingsFont) { Text = "Back" };
            backButton.Click += (sender, e) => Game1.Instance.ChangeState(returnState);
            backButton.Position = new Vector2(CenterWidth - backButton.Rectangle.Width - 10, buttonsY);

            resetButton = new Button(Art.ButtonTexture, Art.SettingsFont) { Text = "Reset to Defaults" };
            resetButton.Click += (sender, e) =>
            {
                KeyBindings.ResetToDefaults();
                Util.SaveKeyBindingsData();
            };
            resetButton.Position = new Vector2(CenterWidth + 10, buttonsY);
        }

        public override void Update(GameTime gameTime)
        {
            if (listeningFor.HasValue)
            {
                if (Input.WasKeyPressed(Keys.Escape))
                {
                    listeningFor = null;
                    return;
                }

                InputBinding? pressed = Input.GetAnyNewInputBinding();
                if (pressed.HasValue)
                {
                    KeyBindings.Action action = listeningFor.Value;
                    InputBinding binding = pressed.Value;

                    // Swap rather than leave two actions sharing a
                    // key/button — whichever action used to hold this
                    // binding takes over the one being freed up.
                    KeyBindings.Action? conflict = KeyBindings.FindConflict(action, binding);
                    if (conflict.HasValue)
                        KeyBindings.Set(conflict.Value, KeyBindings.Get(action));

                    KeyBindings.Set(action, binding);
                    Util.SaveKeyBindingsData();

                    listeningFor = null;
                }

                return;
            }

            foreach (var tab in tabs)
            {
                tab.Hover = tab.Rect.Intersects(Input.MouseBounds);
                if (tab.Hover && Input.GetMouseClick())
                {
                    currentTab = tab.Tab;
                }
            }

            if (currentTab == SettingsTab.Controls)
            {
                foreach (var row in rows)
                {
                    row.Hover = row.Rect.Intersects(Input.MouseBounds);
                    if (row.Hover && Input.GetMouseClick())
                    {
                        listeningFor = row.Action;
                    }
                }
            }

            if (currentTab == SettingsTab.Gameplay)
            {
                foreach (var toggle in gameplayToggles)
                {
                    toggle.Hover = toggle.Rect.Intersects(Input.MouseBounds);
                    if (toggle.Hover && Input.GetMouseClick())
                    {
                        toggle.Set(!toggle.Get());
                        Util.SaveGameSettingsData();
                    }
                }
            }

            if (currentTab == SettingsTab.Graphics)
            {
                foreach (var toggle in graphicsToggles)
                {
                    toggle.Hover = toggle.Rect.Intersects(Input.MouseBounds);
                    if (toggle.Hover && Input.GetMouseClick())
                    {
                        toggle.Set(!toggle.Get());
                        Util.SaveGameSettingsData();
                    }
                }

                foreach (var numeric in graphicsNumerics)
                {
                    numeric.DecrementHover = numeric.DecrementRect.Intersects(Input.MouseBounds);
                    numeric.IncrementHover = numeric.IncrementRect.Intersects(Input.MouseBounds);

                    if (numeric.DecrementHover && Input.GetMouseClick())
                    {
                        numeric.Set(Math.Max(numeric.Min, numeric.Get() - numeric.Step));
                        Util.SaveGameSettingsData();
                    }
                    else if (numeric.IncrementHover && Input.GetMouseClick())
                    {
                        numeric.Set(Math.Min(numeric.Max, numeric.Get() + numeric.Step));
                        Util.SaveGameSettingsData();
                    }
                }
            }

            backButton.Update(gameTime);

            // Only meaningful for key bindings — inert (not updated, not
            // drawn) on every other tab rather than shown but doing
            // nothing.
            if (currentTab == SettingsTab.Controls)
                resetButton.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            string title = "Settings";
            Vector2 titleSize = Art.SettingsFont.MeasureString(title);
            spriteBatch.DrawString(
                Art.SettingsFont,
                title,
                new Vector2(CenterWidth - titleSize.X / 2, tabBarY - 40),
                Color.White
            );

            foreach (var tab in tabs)
            {
                bool active = tab.Tab == currentTab;
                Color color = (active || tab.Hover) ? Color.Gold : Color.White;

                Vector2 labelSize = Art.SettingsFont.MeasureString(tab.Label);
                Vector2 labelPos = new(
                    tab.Rect.X + (tab.Rect.Width - labelSize.X) / 2,
                    tab.Rect.Y + (tab.Rect.Height - labelSize.Y) / 2
                );
                spriteBatch.DrawString(Art.SettingsFont, tab.Label, labelPos, color);

                // Persistent underline for whichever tab is actually
                // active, independent of hover — hover alone (shared with
                // every other tab's Gold-on-hover feedback) isn't a
                // reliable enough "you are here" cue by itself.
                if (active)
                {
                    spriteBatch.Draw(
                        Art.HealthBar,
                        new Rectangle(tab.Rect.X, tab.Rect.Bottom - 2, tab.Rect.Width, 2),
                        Color.Gold
                    );
                }
            }

            switch (currentTab)
            {
                case SettingsTab.Controls:
                    foreach (var row in rows)
                    {
                        Color color = row.Hover ? Color.Gold : Color.White;
                        string label = KeyBindings.DisplayName(row.Action);
                        string value =
                            listeningFor == row.Action
                                ? "Press any key or mouse button... (Esc to cancel)"
                                : KeyBindings.Get(row.Action).ToString();

                        spriteBatch.DrawString(Art.SettingsFont, label, new Vector2(labelX, row.Rect.Y), color);
                        spriteBatch.DrawString(Art.SettingsFont, value, new Vector2(valueX, row.Rect.Y), color);
                    }
                    break;

                case SettingsTab.Gameplay:
                    foreach (var toggle in gameplayToggles)
                    {
                        Color toggleColor = toggle.Hover ? Color.Gold : Color.White;
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            toggle.Label,
                            new Vector2(labelX, toggle.Rect.Y),
                            toggleColor
                        );
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            toggle.Get() ? "ON" : "OFF",
                            new Vector2(valueX, toggle.Rect.Y),
                            toggleColor
                        );
                    }
                    break;

                case SettingsTab.Graphics:
                    foreach (var toggle in graphicsToggles)
                    {
                        Color toggleColor = toggle.Hover ? Color.Gold : Color.White;
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            toggle.Label,
                            new Vector2(labelX, toggle.Rect.Y),
                            toggleColor
                        );
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            toggle.Get() ? "ON" : "OFF",
                            new Vector2(valueX, toggle.Rect.Y),
                            toggleColor
                        );
                    }

                    foreach (var numeric in graphicsNumerics)
                    {
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            numeric.Label,
                            new Vector2(labelX, numeric.Rect.Y),
                            Color.White
                        );

                        Color decColor = numeric.DecrementHover ? Color.Gold : Color.White;
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            "-",
                            new Vector2(numeric.DecrementRect.X, numeric.Rect.Y),
                            decColor
                        );

                        string valueText = $"{numeric.Get()}%";
                        Vector2 valueTextSize = Art.SettingsFont.MeasureString(valueText);
                        float valueCenterX = (numeric.DecrementRect.Right + numeric.IncrementRect.X) / 2f;
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            valueText,
                            new Vector2(valueCenterX - valueTextSize.X / 2f, numeric.Rect.Y),
                            Color.White
                        );

                        Color incColor = numeric.IncrementHover ? Color.Gold : Color.White;
                        spriteBatch.DrawString(
                            Art.SettingsFont,
                            "+",
                            new Vector2(numeric.IncrementRect.X, numeric.Rect.Y),
                            incColor
                        );
                    }
                    break;

                case SettingsTab.Audio:
                    // Nothing to expose yet — no volume control exists
                    // anywhere in the codebase today (confirmed via a
                    // repo-wide check before building this). Placeholder
                    // rather than an empty-looking tab, so it reads as "not
                    // built yet" instead of "broken."
                    const string placeholder = "No settings here yet.";
                    Vector2 placeholderSize = Art.SettingsFont.MeasureString(placeholder);
                    spriteBatch.DrawString(
                        Art.SettingsFont,
                        placeholder,
                        new Vector2(CenterWidth - placeholderSize.X / 2, rowsTop),
                        Color.Gray
                    );
                    break;
            }

            backButton.Draw(gameTime, spriteBatch);
            if (currentTab == SettingsTab.Controls)
                resetButton.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        public override void PostUpdate(GameTime gameTime) { }
    }
}
