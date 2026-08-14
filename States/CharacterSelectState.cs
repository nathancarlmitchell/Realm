using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.Controls;
using Realm.Data;

namespace Realm.States
{
    public class CharacterSelectState : State
    {
        private class Slot
        {
            public Player.Class PlayerClass;
            public Texture2D Portrait;
            public Rectangle PortraitRect;
            public Rectangle BorderRect;
            public bool Hover;

            // Delete-with-confirmation, shown below the class label. Only one of
            // the "Delete" link or the Yes/No row is ever visible at a time.
            public bool HasSave;
            public bool ConfirmingDelete;
            public Rectangle DeleteRect;
            public bool DeleteHover;
            public Vector2 ConfirmLabelPos;
            public Rectangle ConfirmYesRect;
            public bool ConfirmYesHover;
            public Rectangle ConfirmNoRect;
            public bool ConfirmNoHover;
        }

        private const int PortraitSize = 80;
        private const int BorderPadding = 14;
        // Widened from the original 2-slot value (110) — with a third slot
        // now sitting dead-center, the old offset left adjacent
        // portraits/borders (108px wide each) almost touching.
        private const int SlotOffsetFromCenter = 195;
        private const int PreviewGap = 10;

        private readonly List<Slot> slots;
        private readonly Button backButton;
        private readonly Menu menu;

        public CharacterSelectState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Game1.Instance.IsMouseVisible = true;

            int y = CenterHeight - 60;

            slots =
            [
                new Slot
                {
                    PlayerClass = Player.Class.Wizard,
                    Portrait = Art.Wizard,
                    PortraitRect = new Rectangle(
                        CenterWidth - SlotOffsetFromCenter - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                },
                new Slot
                {
                    PlayerClass = Player.Class.Knight,
                    Portrait = Art.Knight,
                    PortraitRect = new Rectangle(
                        CenterWidth - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                },
                new Slot
                {
                    PlayerClass = Player.Class.Archer,
                    Portrait = Art.Archer,
                    PortraitRect = new Rectangle(
                        CenterWidth + SlotOffsetFromCenter - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                },
            ];

            foreach (var slot in slots)
            {
                slot.BorderRect = new Rectangle(
                    slot.PortraitRect.X - BorderPadding,
                    slot.PortraitRect.Y - BorderPadding,
                    slot.PortraitRect.Width + BorderPadding * 2,
                    slot.PortraitRect.Height + BorderPadding * 2
                );
            }

            backButton = new Button() { Text = "Back" };
            backButton.Click += BackButton_Click;
            menu = new Menu([backButton]);
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            StateManager.MainMenu();
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var slot in slots)
            {
                slot.HasSave = HasDeletableProgress(Util.PeekPlayerData(slot.PlayerClass));
                UpdateDeleteControls(slot);

                slot.Hover = slot.BorderRect.Intersects(Input.MouseBounds);

                if (slot.Hover && Input.GetMouseClick())
                {
                    SelectCharacter(slot.PlayerClass);
                }
            }

            backButton.Update(gameTime);
        }

        // Lays out and hit-tests the "Delete" link (or, while confirming, the
        // "Delete save? Yes No" row) directly below the class label. Layout is
        // computed here rather than in Draw so the click rectangles and the drawn
        // text always agree on position.
        private void UpdateDeleteControls(Slot slot)
        {
            Vector2 labelSize = Art.HudFont.MeasureString(slot.PlayerClass.ToString());
            int rowY = (int)(slot.BorderRect.Bottom + 8 + labelSize.Y + 4);

            if (slot.ConfirmingDelete)
            {
                const string confirmText = "Delete save? ";
                const string yesText = "Yes";
                const string noText = "No";

                Vector2 confirmSize = Art.HudFont.MeasureString(confirmText);
                Vector2 yesSize = Art.HudFont.MeasureString(yesText);
                Vector2 noSize = Art.HudFont.MeasureString(noText);

                float rowWidth = confirmSize.X + yesSize.X + 8 + noSize.X;
                float startX = slot.PortraitRect.Center.X - rowWidth / 2;

                slot.ConfirmLabelPos = new Vector2(startX, rowY);

                float yesX = startX + confirmSize.X;
                slot.ConfirmYesRect = new Rectangle(
                    (int)yesX,
                    rowY,
                    (int)yesSize.X,
                    (int)yesSize.Y
                );

                float noX = yesX + yesSize.X + 8;
                slot.ConfirmNoRect = new Rectangle((int)noX, rowY, (int)noSize.X, (int)noSize.Y);

                slot.ConfirmYesHover = slot.ConfirmYesRect.Intersects(Input.MouseBounds);
                slot.ConfirmNoHover = slot.ConfirmNoRect.Intersects(Input.MouseBounds);

                if (slot.ConfirmYesHover && Input.GetMouseClick())
                {
                    DeleteCharacter(slot);
                }
                else if (slot.ConfirmNoHover && Input.GetMouseClick())
                {
                    slot.ConfirmingDelete = false;
                }
            }
            else if (slot.HasSave)
            {
                const string deleteText = "Delete";
                Vector2 deleteSize = Art.HudFont.MeasureString(deleteText);
                slot.DeleteRect = new Rectangle(
                    (int)(slot.PortraitRect.Center.X - deleteSize.X / 2),
                    rowY,
                    (int)deleteSize.X,
                    (int)deleteSize.Y
                );

                slot.DeleteHover = slot.DeleteRect.Intersects(Input.MouseBounds);

                if (slot.DeleteHover && Input.GetMouseClick())
                {
                    slot.ConfirmingDelete = true;
                }
            }
        }

        // DeleteCharacterData preserves High Score by writing back a fresh
        // Level-1 default rather than removing the save file outright (see
        // Util.cs), so PeekPlayerData alone can't tell "deleted" apart from
        // "genuinely being played" — checking Level/Experience isn't enough
        // either, since a character can be played (selected, entered the
        // world) without ever leveling up or scoring. HasBeenPlayed tracks
        // that directly and resets to false on delete, independent of High
        // Score.
        private static bool HasDeletableProgress(PlayerData saved) =>
            saved != null && saved.HasBeenPlayed;

        private static void DeleteCharacter(Slot slot)
        {
            Util.DeleteCharacterData(slot.PlayerClass);

            // If the character being deleted is also the one currently loaded in
            // memory, reset the live instance too — otherwise the next autosave
            // (e.g. StateManager.Nexus()) would silently recreate the file from
            // stale in-memory stats, undoing the delete.
            if (slot.PlayerClass == Player.PlayerClass)
            {
                EntityManager.RemovePlayer();
                Util.ResetPlayer(slot.PlayerClass);
                EntityManager.Add(Player.Instance);
            }

            slot.ConfirmingDelete = false;
            slot.HasSave = false;
        }

        // Constructs (or restores) the chosen class and drops the player into the hub.
        private void SelectCharacter(Player.Class playerClass)
        {
            EntityManager.RemovePlayer();

            Util.LoadOrCreatePlayer(playerClass);

            EntityManager.Add(Player.Instance);

            StateManager.NewGame();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            bool anyHover = false;
            foreach (var slot in slots)
            {
                if (slot.Hover)
                    anyHover = true;
            }

            if (!anyHover)
            {
                string subtitle = "Select a Character";
                Vector2 subtitleSize = Art.HudFont.MeasureString(subtitle);
                spriteBatch.DrawString(
                    Art.HudFont,
                    subtitle,
                    new Vector2(CenterWidth - subtitleSize.X / 2, 128 / Game1.Scale),
                    Color.White
                );
            }

            foreach (var slot in slots)
            {
                Color borderColor = slot.Hover ? Color.Gold : Color.White;

                spriteBatch.Draw(Art.Border, slot.BorderRect, borderColor);
                spriteBatch.Draw(slot.Portrait, slot.PortraitRect, Color.White);

                string label = slot.PlayerClass.ToString();
                Vector2 labelSize = Art.HudFont.MeasureString(label);
                spriteBatch.DrawString(
                    Art.HudFont,
                    label,
                    new Vector2(slot.PortraitRect.Center.X - labelSize.X / 2, slot.BorderRect.Bottom + 8),
                    Color.White
                );

                if (slot.Hover)
                {
                    DrawPreview(spriteBatch, slot);
                }

                if (slot.ConfirmingDelete)
                {
                    DrawShadowedText(spriteBatch, "Delete save? ", slot.ConfirmLabelPos, Color.White);
                    DrawShadowedText(
                        spriteBatch,
                        "Yes",
                        slot.ConfirmYesRect.Location.ToVector2(),
                        slot.ConfirmYesHover ? Color.Gold : Color.OrangeRed
                    );
                    DrawShadowedText(
                        spriteBatch,
                        "No",
                        slot.ConfirmNoRect.Location.ToVector2(),
                        slot.ConfirmNoHover ? Color.Gold : Color.White
                    );
                }
                else if (slot.HasSave)
                {
                    DrawShadowedText(
                        spriteBatch,
                        "Delete",
                        slot.DeleteRect.Location.ToVector2(),
                        slot.DeleteHover ? Color.Gold : Color.White
                    );
                }
            }

            menu.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        private void DrawPreview(SpriteBatch spriteBatch, Slot slot)
        {
            PlayerData saved = Util.PeekPlayerData(slot.PlayerClass);

            string statsText =
                saved != null
                    ? BuildPreviewText(
                        saved.Level,
                        saved.HealthMax,
                        saved.ManaMax,
                        saved.Attack,
                        saved.Defense,
                        saved.Speed,
                        saved.Dexterity,
                        saved.Vitality,
                        saved.Wisdom
                    )
                    : BuildDefaultPreviewText(slot.PlayerClass);

            string scoreText = $"Score: {saved?.ExperienceTotal ?? 0}";
            string highScoreText = $"Hi-Score: {saved?.HighScore ?? 0}";

            // Stack bottom-up from just above the portrait: stats block closest to the
            // portrait, then score, then high score on top.
            float bottom = slot.BorderRect.Top - PreviewGap;

            Vector2 statsSize = Art.HudFont.MeasureString(statsText);
            Vector2 statsPos = new(slot.PortraitRect.Center.X - statsSize.X / 2, bottom - statsSize.Y);

            Vector2 scoreSize = Art.HudFont.MeasureString(scoreText);
            Vector2 scorePos = new(
                slot.PortraitRect.Center.X - scoreSize.X / 2,
                statsPos.Y - scoreSize.Y
            );

            Vector2 highScoreSize = Art.HudFont.MeasureString(highScoreText);
            Vector2 highScorePos = new(
                slot.PortraitRect.Center.X - highScoreSize.X / 2,
                scorePos.Y - highScoreSize.Y
            );

            DrawShadowedText(spriteBatch, statsText, statsPos, Color.Red);
            DrawShadowedText(spriteBatch, scoreText, scorePos, Color.Gold);
            DrawShadowedText(spriteBatch, highScoreText, highScorePos, Color.LightBlue);
        }

        private static void DrawShadowedText(
            SpriteBatch spriteBatch,
            string text,
            Vector2 pos,
            Color color
        )
        {
            spriteBatch.DrawString(Art.HudFont, text, pos + new Vector2(1, 1), Color.Black * 0.6f);
            spriteBatch.DrawString(Art.HudFont, text, pos, color);
        }

        private static string BuildPreviewText(
            int level,
            int healthMax,
            int manaMax,
            int attack,
            int defense,
            float speed,
            int dexterity,
            int vitality,
            int wisdom
        )
        {
            return
                $"Level: {level}\n"
                + $"Health: {healthMax}\n"
                + $"Mana: {manaMax}\n"
                + $"Attack: {attack}\n"
                + $"Defense: {defense}\n"
                + $"Speed: {speed}\n"
                + $"Dexterity: {dexterity}\n"
                + $"Vitality: {vitality}\n"
                + $"Wisdom: {wisdom}";
        }

        // Mirrors the starting stats set in Wizard()/Archer()'s constructors, for classes
        // with no save yet.
        private static string BuildDefaultPreviewText(Player.Class playerClass)
        {
            return playerClass switch
            {
                Player.Class.Wizard => BuildPreviewText(1, 100, 100, 17, 0, 17, 17, 5, 23),
                Player.Class.Archer => BuildPreviewText(1, 150, 100, 17, 0, 22, 15, 5, 15),
                Player.Class.Knight => BuildPreviewText(1, 200, 50, 17, 10, 13, 15, 10, 8),
                _ => string.Empty,
            };
        }

        public override void PostUpdate(GameTime gameTime) { }
    }
}
