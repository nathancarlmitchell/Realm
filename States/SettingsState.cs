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

        private enum RowKind
        {
            Toggle,
            Numeric,
        }

        // Every non-keybinding setting on the Gameplay/Graphics/Audio tabs
        // — a plain on/off flip (Toggle) or a clamped, steppable int
        // (Numeric), picked by Kind. Unified into one type (rather than
        // the separate ToggleRow/NumericRow classes this started as) once
        // the Audio tab needed toggles and numerics interleaved in a
        // specific order (Music, Music Volume, Music Mute, ...) — two
        // separate lists could only ever render as two separate blocks,
        // toggles-then-numerics, not the order the settings actually read
        // best in. Get/Set close over whichever Player.Instance field the
        // row actually controls, so adding a future setting to any of
        // these three tabs is just one more list entry.
        private class SettingsRow
        {
            public RowKind Kind;
            public string Label;
            public Rectangle Rect;

            // Toggle
            public bool Hover;
            public Func<bool> GetBool;
            public Action<bool> SetBool;

            // Numeric
            public Rectangle DecrementRect;
            public Rectangle IncrementRect;
            public bool DecrementHover;
            public bool IncrementHover;
            public Func<int> GetInt;
            public Action<int> SetInt;
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

        private readonly List<SettingsRow> gameplayRows;
        private readonly List<SettingsRow> graphicsRows;
        private readonly List<SettingsRow> audioRows;

        private const int RowHeight = 28;
        private const int TabHeight = 32;
        private const int TabGap = 16;
        private const int TabPaddingX = 16;
        private const int StepperButtonWidth = 24;
        private const int StepperValueGap = 70; // room for "100%" between the two buttons, with real padding on both sides

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

            gameplayRows =
            [
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Auto-Fire",
                    GetBool = () => Player.Instance.AutoFireEnabled,
                    SetBool = v => Player.Instance.AutoFireEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Auto-Enter Portals",
                    GetBool = () => Player.Instance.AutoEnterPortalsEnabled,
                    SetBool = v => Player.Instance.AutoEnterPortalsEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show Quest Indicator",
                    GetBool = () => Player.Instance.ShowQuestIndicatorEnabled,
                    SetBool = v => Player.Instance.ShowQuestIndicatorEnabled = v,
                },
            ];

            graphicsRows =
            [
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show Hitboxes",
                    GetBool = () => Player.Instance.ShowHitboxesEnabled,
                    SetBool = v => Player.Instance.ShowHitboxesEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Low Health Indicator",
                    GetBool = () => Player.Instance.LowHealthIndicatorEnabled,
                    SetBool = v => Player.Instance.LowHealthIndicatorEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Numeric,
                    Label = "Low Health Threshold",
                    GetInt = () => Player.Instance.LowHealthThresholdPercent,
                    SetInt = v => Player.Instance.LowHealthThresholdPercent = v,
                    Step = 5,
                    Min = 0,
                    Max = 100,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Always Display Player HP",
                    GetBool = () => Player.Instance.AlwaysDisplayPlayerHPEnabled,
                    SetBool = v => Player.Instance.AlwaysDisplayPlayerHPEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show XP Drops",
                    GetBool = () => Player.Instance.ShowXpDropsEnabled,
                    SetBool = v => Player.Instance.ShowXpDropsEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Always Show EXP",
                    GetBool = () => Player.Instance.AlwaysShowExpEnabled,
                    SetBool = v => Player.Instance.AlwaysShowExpEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show Player Damage Numbers",
                    GetBool = () => Player.Instance.ShowPlayerDamageNumbersEnabled,
                    SetBool = v => Player.Instance.ShowPlayerDamageNumbersEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show Enemy Damage Numbers",
                    GetBool = () => Player.Instance.ShowEnemyDamageNumbersEnabled,
                    SetBool = v => Player.Instance.ShowEnemyDamageNumbersEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show Hit Particles",
                    GetBool = () => Player.Instance.ShowHitParticlesEnabled,
                    SetBool = v => Player.Instance.ShowHitParticlesEnabled = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Show Combat Indicator",
                    GetBool = () => Player.Instance.ShowCombatIndicatorEnabled,
                    SetBool = v => Player.Instance.ShowCombatIndicatorEnabled = v,
                },
            ];

            // Order matches how the settings actually read best together —
            // Music's own on/off, then its volume, then its quick-mute,
            // followed by the same Volume/Mute pair for Sound Effects, and
            // finally the one narrower mute scoped to just the player's own
            // weapon-fire sound (Weapon.Shoot()'s Sound.MagicShoot call).
            // The three Music-affecting rows also call
            // Sound.RefreshMusicState() on top of the usual Set(), so a
            // change takes effect on the currently-playing track
            // immediately rather than only on the next dungeon entry.
            audioRows =
            [
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Music",
                    GetBool = () => Player.Instance.MusicEnabled,
                    SetBool = v =>
                    {
                        Player.Instance.MusicEnabled = v;
                        Sound.RefreshMusicState();
                    },
                },
                new SettingsRow
                {
                    Kind = RowKind.Numeric,
                    Label = "Music Volume",
                    GetInt = () => Player.Instance.MusicVolumePercent,
                    SetInt = v =>
                    {
                        Player.Instance.MusicVolumePercent = v;
                        Sound.RefreshMusicState();
                    },
                    Step = 5,
                    Min = 0,
                    Max = 100,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Music Mute",
                    GetBool = () => Player.Instance.MusicMuted,
                    SetBool = v =>
                    {
                        Player.Instance.MusicMuted = v;
                        Sound.RefreshMusicState();
                    },
                },
                new SettingsRow
                {
                    Kind = RowKind.Numeric,
                    Label = "Sound Effects Volume",
                    GetInt = () => Player.Instance.SfxVolumePercent,
                    SetInt = v => Player.Instance.SfxVolumePercent = v,
                    Step = 5,
                    Min = 0,
                    Max = 100,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Sound Effects Mute",
                    GetBool = () => Player.Instance.SfxMuted,
                    SetBool = v => Player.Instance.SfxMuted = v,
                },
                new SettingsRow
                {
                    Kind = RowKind.Toggle,
                    Label = "Mute Weapon Shots",
                    GetBool = () => Player.Instance.WeaponShotsMuted,
                    SetBool = v => Player.Instance.WeaponShotsMuted = v,
                },
            ];

            // Widest label sets where the value column starts, same
            // column-alignment trick as Overlay.DrawStats() — includes
            // every tab's row labels too, so they all line up in the same
            // column even though only one tab's content is visible at a
            // time.
            float widestLabel = 0f;
            foreach (var action in KeyBindings.AllActions)
                widestLabel = Math.Max(widestLabel, Art.RetroFont.MeasureString(KeyBindings.DisplayName(action)).X);
            foreach (var row in gameplayRows)
                widestLabel = Math.Max(widestLabel, Art.RetroFont.MeasureString(row.Label).X);
            foreach (var row in graphicsRows)
                widestLabel = Math.Max(widestLabel, Art.RetroFont.MeasureString(row.Label).X);
            foreach (var row in audioRows)
                widestLabel = Math.Max(widestLabel, Art.RetroFont.MeasureString(row.Label).X);

            labelX = CenterWidth - 160;
            valueX = labelX + (int)widestLabel + 24;

            // Fixed layout, independent of which tab (and therefore how
            // many rows) is currently active — content has to stay in the
            // same place across tab switches, or the whole screen would
            // jump around every time the user clicked a different tab.
            // Sized against the Controls tab (10 rows) and the Audio tab
            // (6 rows), the two tallest.
            tabBarY = CenterHeight - 200;
            rowsTop = tabBarY + TabHeight + 20;

            for (int i = 0; i < rows.Count; i++)
            {
                Vector2 rowSize = Art.RetroFont.MeasureString("A");
                rows[i].Rect = new Rectangle(
                    labelX,
                    rowsTop + i * RowHeight,
                    valueX - labelX + 160,
                    (int)rowSize.Y + 6
                );
            }

            LayoutRows(gameplayRows);
            LayoutRows(graphicsRows);
            LayoutRows(audioRows);

            // Tab bar, centered as a group above the content area.
            tabs = [];
            foreach (SettingsTab tab in Enum.GetValues(typeof(SettingsTab)))
                tabs.Add(new TabInfo { Tab = tab, Label = tab.ToString() });

            float totalTabWidth = TabGap * (tabs.Count - 1);
            foreach (var tab in tabs)
                totalTabWidth += Art.RetroFont.MeasureString(tab.Label).X + TabPaddingX * 2;

            int tabX = CenterWidth - (int)(totalTabWidth / 2);
            foreach (var tab in tabs)
            {
                int tabWidth = (int)Art.RetroFont.MeasureString(tab.Label).X + TabPaddingX * 2;
                tab.Rect = new Rectangle(tabX, tabBarY, tabWidth, TabHeight);
                tabX += tabWidth + TabGap;
            }

            // Also fixed, for the same reason as rowsTop/tabBarY above —
            // sized to comfortably clear the tallest tab's full row list
            // regardless of which tab happens to be showing right now.
            int tallestRowCount = Math.Max(rows.Count, audioRows.Count);
            int buttonsY = rowsTop + tallestRowCount * RowHeight + 30;

            backButton = new Button(Art.ButtonTexture, Art.RetroFontButton) { Text = "Back" };
            backButton.Click += (sender, e) => Game1.Instance.ChangeState(returnState);
            backButton.Position = new Vector2(CenterWidth - backButton.Rectangle.Width - 10, buttonsY);

            resetButton = new Button(Art.ButtonTexture, Art.RetroFontButton) { Text = "Reset to Defaults" };
            resetButton.Click += (sender, e) =>
            {
                KeyBindings.ResetToDefaults();
                Util.SaveKeyBindingsData();
            };
            resetButton.Position = new Vector2(CenterWidth + 10, buttonsY);
        }

        // Assigns Rect (and DecrementRect/IncrementRect for Numeric rows)
        // to every row in a tab's list, stacked in list order at the same
        // fixed column/row height every other tab uses.
        private void LayoutRows(List<SettingsRow> tabRows)
        {
            int rowHeightPx = (int)Art.RetroFont.MeasureString("A").Y + 6;
            for (int i = 0; i < tabRows.Count; i++)
            {
                int rowY = rowsTop + i * RowHeight;
                tabRows[i].Rect = new Rectangle(labelX, rowY, valueX - labelX + 160, rowHeightPx);

                if (tabRows[i].Kind == RowKind.Numeric)
                {
                    tabRows[i].DecrementRect = new Rectangle(valueX, rowY, StepperButtonWidth, rowHeightPx);
                    tabRows[i].IncrementRect = new Rectangle(
                        valueX + StepperButtonWidth + StepperValueGap,
                        rowY,
                        StepperButtonWidth,
                        rowHeightPx
                    );
                }
            }
        }

        private static void UpdateRows(List<SettingsRow> tabRows)
        {
            foreach (var row in tabRows)
            {
                if (row.Kind == RowKind.Toggle)
                {
                    row.Hover = row.Rect.Intersects(Input.MouseBounds);
                    if (row.Hover && Input.GetMouseClick())
                    {
                        row.SetBool(!row.GetBool());
                        Util.SaveGameSettingsData();
                    }
                }
                else
                {
                    row.DecrementHover = row.DecrementRect.Intersects(Input.MouseBounds);
                    row.IncrementHover = row.IncrementRect.Intersects(Input.MouseBounds);

                    if (row.DecrementHover && Input.GetMouseClick())
                    {
                        row.SetInt(Math.Max(row.Min, row.GetInt() - row.Step));
                        Util.SaveGameSettingsData();
                    }
                    else if (row.IncrementHover && Input.GetMouseClick())
                    {
                        row.SetInt(Math.Min(row.Max, row.GetInt() + row.Step));
                        Util.SaveGameSettingsData();
                    }
                }
            }
        }

        private void DrawRows(SpriteBatch spriteBatch, List<SettingsRow> tabRows)
        {
            foreach (var row in tabRows)
            {
                if (row.Kind == RowKind.Toggle)
                {
                    Color toggleColor = row.Hover ? Color.Gold : Color.White;
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        row.Label,
                        new Vector2(labelX, row.Rect.Y),
                        toggleColor
                    );
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        row.GetBool() ? "ON" : "OFF",
                        new Vector2(valueX, row.Rect.Y),
                        toggleColor
                    );
                }
                else
                {
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        row.Label,
                        new Vector2(labelX, row.Rect.Y),
                        Color.White
                    );

                    Color decColor = row.DecrementHover ? Color.Gold : Color.White;
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        "-",
                        new Vector2(row.DecrementRect.X, row.Rect.Y),
                        decColor
                    );

                    string valueText = $"{row.GetInt()}%";
                    Vector2 valueTextSize = Art.RetroFont.MeasureString(valueText);
                    float valueCenterX = (row.DecrementRect.Right + row.IncrementRect.X) / 2f;
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        valueText,
                        new Vector2(valueCenterX - valueTextSize.X / 2f, row.Rect.Y),
                        Color.White
                    );

                    Color incColor = row.IncrementHover ? Color.Gold : Color.White;
                    Util.DrawOutlinedText(
                        spriteBatch,
                        Art.RetroFont,
                        "+",
                        new Vector2(row.IncrementRect.X, row.Rect.Y),
                        incColor
                    );
                }
            }
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
                UpdateRows(gameplayRows);

            if (currentTab == SettingsTab.Graphics)
                UpdateRows(graphicsRows);

            if (currentTab == SettingsTab.Audio)
                UpdateRows(audioRows);

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
            Vector2 titleSize = Art.RetroFont.MeasureString(title);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                title,
                new Vector2(CenterWidth - titleSize.X / 2, tabBarY - 40),
                Color.White
            );

            foreach (var tab in tabs)
            {
                bool active = tab.Tab == currentTab;
                Color color = (active || tab.Hover) ? Color.Gold : Color.White;

                Vector2 labelSize = Art.RetroFont.MeasureString(tab.Label);
                Vector2 labelPos = new(
                    tab.Rect.X + (tab.Rect.Width - labelSize.X) / 2,
                    tab.Rect.Y + (tab.Rect.Height - labelSize.Y) / 2
                );
                Util.DrawOutlinedText(spriteBatch, Art.RetroFont, tab.Label, labelPos, color);

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

                        Util.DrawOutlinedText(spriteBatch, Art.RetroFont, label, new Vector2(labelX, row.Rect.Y), color);
                        Util.DrawOutlinedText(spriteBatch, Art.RetroFont, value, new Vector2(valueX, row.Rect.Y), color);
                    }
                    break;

                case SettingsTab.Gameplay:
                    DrawRows(spriteBatch, gameplayRows);
                    break;

                case SettingsTab.Graphics:
                    DrawRows(spriteBatch, graphicsRows);
                    break;

                case SettingsTab.Audio:
                    DrawRows(spriteBatch, audioRows);
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
