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
        private class Row
        {
            public KeyBindings.Action Action;
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
        private readonly Button backButton;
        private readonly Button resetButton;

        // Set while waiting for the next key press to bind to a row —
        // blocks every other row/button from reacting to input until it
        // resolves (a key pressed, or Escape to cancel), same reasoning as
        // CharacterSelectState's ConfirmingDelete gating out normal clicks.
        private KeyBindings.Action? listeningFor;

        // First setting on this screen that isn't a rebindable
        // KeyBindings.Action (see Util.SaveGameSettingsData()) — a plain
        // click-to-toggle row rather than another entry in `rows`, since
        // Row is typed around KeyBindings.Action specifically and there's
        // only the one non-keybinding setting so far to justify widening
        // it. Drawn/positioned as one more row directly below the key
        // bindings, in the same two-column layout.
        private const string AutoFireLabel = "Auto-Fire";
        private Rectangle autoFireRect;
        private bool autoFireHover;

        private const int RowHeight = 28;
        private int labelX;
        private int valueX;
        private int rowsTop;

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
            // AutoFireLabel too, so the toggle row below lines up in the
            // same value column even though it isn't a KeyBindings.Action.
            float widestLabel = 0f;
            foreach (var action in KeyBindings.AllActions)
                widestLabel = Math.Max(widestLabel, Art.HudFont.MeasureString(KeyBindings.DisplayName(action)).X);
            widestLabel = Math.Max(widestLabel, Art.HudFont.MeasureString(AutoFireLabel).X);

            labelX = CenterWidth - 160;
            valueX = labelX + (int)widestLabel + 24;

            // +1 to include the Auto-Fire toggle row in the same
            // vertically-centered block as the key bindings above it.
            rowsTop = CenterHeight - ((rows.Count + 1) * RowHeight) / 2;

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
                rowsTop + rows.Count * RowHeight,
                valueX - labelX + 160,
                (int)Art.HudFont.MeasureString("A").Y + 6
            );

            backButton = new Button() { Text = "Back" };
            backButton.Click += (sender, e) => Game1.Instance.ChangeState(returnState);
            backButton.Position = new Vector2(
                CenterWidth - backButton.Rectangle.Width - 10,
                rowsTop + (rows.Count + 1) * RowHeight + 30
            );

            resetButton = new Button() { Text = "Reset to Defaults" };
            resetButton.Click += (sender, e) =>
            {
                KeyBindings.ResetToDefaults();
                Util.SaveKeyBindingsData();
            };
            resetButton.Position = new Vector2(
                CenterWidth + 10,
                rowsTop + (rows.Count + 1) * RowHeight + 30
            );
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

            foreach (var row in rows)
            {
                row.Hover = row.Rect.Intersects(Input.MouseBounds);
                if (row.Hover && Input.GetMouseClick())
                {
                    listeningFor = row.Action;
                }
            }

            autoFireHover = autoFireRect.Intersects(Input.MouseBounds);
            if (autoFireHover && Input.GetMouseClick())
            {
                Player.Instance.AutoFireEnabled = !Player.Instance.AutoFireEnabled;
                Util.SaveGameSettingsData();
            }

            backButton.Update(gameTime);
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
                new Vector2(CenterWidth - titleSize.X / 2, rowsTop - 40),
                Color.White
            );

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

            backButton.Draw(gameTime, spriteBatch);
            resetButton.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        public override void PostUpdate(GameTime gameTime) { }
    }
}
