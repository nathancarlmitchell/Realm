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

        // First setting on this screen that isn't a rebindable
        // KeyBindings.Action (see Util.SaveGameSettingsData()) — a plain
        // click-to-toggle row rather than another entry in `rows`, since
        // Row is typed around KeyBindings.Action specifically and there's
        // only the one non-keybinding setting so far to justify widening
        // it. Lives on the Gameplay tab.
        private const string AutoFireLabel = "Auto-Fire";
        private Rectangle autoFireRect;
        private bool autoFireHover;

        private const int RowHeight = 28;
        private const int TabHeight = 32;
        private const int TabGap = 16;
        private const int TabPaddingX = 16;

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

            // Widest label sets where the key-name column starts, same
            // column-alignment trick as Overlay.DrawStats() — includes
            // AutoFireLabel too, so the Gameplay tab's toggle row lines up
            // in the same column as the Controls tab's rows, even though
            // only one tab's content is visible at a time.
            float widestLabel = 0f;
            foreach (var action in KeyBindings.AllActions)
                widestLabel = Math.Max(widestLabel, Art.HudFont.MeasureString(KeyBindings.DisplayName(action)).X);
            widestLabel = Math.Max(widestLabel, Art.HudFont.MeasureString(AutoFireLabel).X);

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
                Vector2 rowSize = Art.HudFont.MeasureString("A");
                rows[i].Rect = new Rectangle(
                    labelX,
                    rowsTop + i * RowHeight,
                    valueX - labelX + 160,
                    (int)rowSize.Y + 6
                );
            }

            autoFireRect = new Rectangle(
                labelX,
                rowsTop,
                valueX - labelX + 160,
                (int)Art.HudFont.MeasureString("A").Y + 6
            );

            // Tab bar, centered as a group above the content area.
            tabs = [];
            foreach (SettingsTab tab in Enum.GetValues(typeof(SettingsTab)))
                tabs.Add(new TabInfo { Tab = tab, Label = tab.ToString() });

            float totalTabWidth = TabGap * (tabs.Count - 1);
            foreach (var tab in tabs)
                totalTabWidth += Art.HudFont.MeasureString(tab.Label).X + TabPaddingX * 2;

            int tabX = CenterWidth - (int)(totalTabWidth / 2);
            foreach (var tab in tabs)
            {
                int tabWidth = (int)Art.HudFont.MeasureString(tab.Label).X + TabPaddingX * 2;
                tab.Rect = new Rectangle(tabX, tabBarY, tabWidth, TabHeight);
                tabX += tabWidth + TabGap;
            }

            // Also fixed, for the same reason as rowsTop/tabBarY above —
            // sized to comfortably clear the Controls tab's full row list
            // regardless of which tab happens to be showing right now.
            int buttonsY = rowsTop + rows.Count * RowHeight + 30;

            backButton = new Button() { Text = "Back" };
            backButton.Click += (sender, e) => Game1.Instance.ChangeState(returnState);
            backButton.Position = new Vector2(CenterWidth - backButton.Rectangle.Width - 10, buttonsY);

            resetButton = new Button() { Text = "Reset to Defaults" };
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
                autoFireHover = autoFireRect.Intersects(Input.MouseBounds);
                if (autoFireHover && Input.GetMouseClick())
                {
                    Player.Instance.AutoFireEnabled = !Player.Instance.AutoFireEnabled;
                    Util.SaveGameSettingsData();
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
            Vector2 titleSize = Art.HudFont.MeasureString(title);
            spriteBatch.DrawString(
                Art.HudFont,
                title,
                new Vector2(CenterWidth - titleSize.X / 2, tabBarY - 40),
                Color.White
            );

            foreach (var tab in tabs)
            {
                bool active = tab.Tab == currentTab;
                Color color = (active || tab.Hover) ? Color.Gold : Color.White;

                Vector2 labelSize = Art.HudFont.MeasureString(tab.Label);
                Vector2 labelPos = new(
                    tab.Rect.X + (tab.Rect.Width - labelSize.X) / 2,
                    tab.Rect.Y + (tab.Rect.Height - labelSize.Y) / 2
                );
                spriteBatch.DrawString(Art.HudFont, tab.Label, labelPos, color);

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

                        spriteBatch.DrawString(Art.HudFont, label, new Vector2(labelX, row.Rect.Y), color);
                        spriteBatch.DrawString(Art.HudFont, value, new Vector2(valueX, row.Rect.Y), color);
                    }
                    break;

                case SettingsTab.Gameplay:
                    Color autoFireColor = autoFireHover ? Color.Gold : Color.White;
                    spriteBatch.DrawString(
                        Art.HudFont,
                        AutoFireLabel,
                        new Vector2(labelX, autoFireRect.Y),
                        autoFireColor
                    );
                    spriteBatch.DrawString(
                        Art.HudFont,
                        Player.Instance.AutoFireEnabled ? "ON" : "OFF",
                        new Vector2(valueX, autoFireRect.Y),
                        autoFireColor
                    );
                    break;

                case SettingsTab.Audio:
                case SettingsTab.Graphics:
                    // Nothing to expose yet on either tab — no volume
                    // control or graphics option exists anywhere in the
                    // codebase today (confirmed via a repo-wide check
                    // before building this). Placeholder rather than an
                    // empty-looking tab, so it reads as "not built yet"
                    // instead of "broken."
                    const string placeholder = "No settings here yet.";
                    Vector2 placeholderSize = Art.HudFont.MeasureString(placeholder);
                    spriteBatch.DrawString(
                        Art.HudFont,
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
