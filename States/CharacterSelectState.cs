using System;
using System.Collections.Generic;
using System.Linq;
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

            // The class immediately before this one in the unlock chain
            // (see slots list order below) — null for Wizard, the chain's
            // starting class, which is always unlocked. Gated on that
            // class's own permanent star record rather than a separate
            // persisted flag: stars only ever increase (Player.ComputeStars
            // is fed by HighScore, which itself only ever increases), so
            // "has the previous class ever reached 3 stars" and "is this
            // class permanently unlocked" are the same question — no new
            // save state needed.
            public Player.Class? PreviousClass;

            // Refreshed each Update() from PreviousClass's own Stars below
            // (0 and unused when PreviousClass is null) — cached here so
            // IsLocked and the locked-preview text don't need to search
            // the slots list again themselves.
            public int PreviousClassStars;
            public bool IsLocked;

            // Highest star rating (0-5) this class has ever achieved —
            // permanent, computed fresh each Update() from that class's
            // saved HasReachedLevel20/HighScore (see ComputeStars()) rather
            // than stored redundantly. Shown regardless of lock state, since
            // it's a record of what's already been earned.
            public int Stars;

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

        // Unlock chain: Wizard starts unlocked; each class after it needs
        // this many stars earned in the class immediately to its left below
        // (Wizard -> Priest -> Archer -> Knight) — see Slot.PreviousClass.
        private const int RequiredStarsPerUnlock = 3;

        private const int PortraitSize = 80;
        private const int BorderPadding = 14;

        // Four evenly-spaced slots (Wizard/Priest/Archer/Knight, left to
        // right — the same order as the unlock chain above), 150px between
        // adjacent centers — replaces the old 3-slot layout's single
        // SlotOffsetFromCenter (195, dead-center + two flanking slots), which
        // has no even-count equivalent. Outer is the two end slots'
        // distance from center, Inner the two middle slots'.
        private const int SlotOffsetFromCenterOuter = 225;
        private const int SlotOffsetFromCenterInner = 75;
        private const int PreviewGap = 10;

        private readonly List<Slot> slots;
        private readonly Button backButton;
        private readonly Menu menu;

        // Full account wipe — deliberately kept off the shared Menu (which
        // centers every button it holds vertically around screen-center in a
        // stack; a second entry there would collide with the class
        // portraits, which already occupy that space) and given its own
        // fixed, low-traffic corner instead, positioned/updated/drawn
        // independently of backButton/menu.
        private enum EraseStage
        {
            None,
            Warning,
            FinalConfirm,
        }

        private EraseStage eraseStage = EraseStage.None;
        private readonly Button eraseAllButton;
        private readonly Button eraseCancelButton;
        private readonly Button eraseConfirmButton;

        public CharacterSelectState(
            Game1 game,
            GraphicsDevice graphicsDevice,
            ContentManager content
        )
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
                        CenterWidth - SlotOffsetFromCenterOuter - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                    PreviousClass = null,
                },
                new Slot
                {
                    PlayerClass = Player.Class.Priest,
                    Portrait = Art.Priest,
                    PortraitRect = new Rectangle(
                        CenterWidth - SlotOffsetFromCenterInner - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                    PreviousClass = Player.Class.Wizard,
                },
                new Slot
                {
                    PlayerClass = Player.Class.Archer,
                    Portrait = Art.Archer,
                    PortraitRect = new Rectangle(
                        CenterWidth + SlotOffsetFromCenterInner - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                    PreviousClass = Player.Class.Priest,
                },
                new Slot
                {
                    PlayerClass = Player.Class.Knight,
                    Portrait = Art.Knight,
                    PortraitRect = new Rectangle(
                        CenterWidth + SlotOffsetFromCenterOuter - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                    PreviousClass = Player.Class.Archer,
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

            eraseAllButton = new Button() { Text = "Erase All Data", PenColor = Color.Red };
            eraseAllButton.Click += (sender, e) =>
            {
                eraseStage = EraseStage.Warning;
                PositionEraseButtons();
            };
            eraseAllButton.Position = new Vector2(
                20,
                Game1.ScreenHeight - eraseAllButton.Rectangle.Height - 20
            );

            eraseCancelButton = new Button() { Text = "Cancel" };
            eraseCancelButton.Click += (sender, e) => eraseStage = EraseStage.None;

            eraseConfirmButton = new Button() { Text = "Continue", PenColor = Color.Red };
            eraseConfirmButton.Click += EraseConfirmButton_Click;

            PositionEraseButtons();
        }

        // Warning screen: Cancel left, Continue right. FinalConfirm screen:
        // reversed — Yes, Erase Everything left, Cancel right. Deliberately
        // not the same layout both screens, per the user's request: a
        // reflexive "click the same spot twice" no longer lands on the
        // destructive button both times.
        private void PositionEraseButtons()
        {
            int y = CenterHeight + 40;

            if (eraseStage == EraseStage.FinalConfirm)
            {
                eraseConfirmButton.Position = new Vector2(
                    CenterWidth - eraseConfirmButton.Rectangle.Width - 10,
                    y
                );
                eraseCancelButton.Position = new Vector2(CenterWidth + 10, y);
            }
            else
            {
                eraseCancelButton.Position = new Vector2(
                    CenterWidth - eraseCancelButton.Rectangle.Width - 10,
                    y
                );
                eraseConfirmButton.Position = new Vector2(CenterWidth + 10, y);
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            StateManager.MainMenu();
        }

        // First click (Warning) advances to a second, more explicit
        // confirmation instead of acting immediately — the user asked for
        // two full confirmations before anything actually gets erased,
        // given how destructive and irreversible this is compared to the
        // single-step per-character delete above.
        private void EraseConfirmButton_Click(object sender, EventArgs e)
        {
            if (eraseStage == EraseStage.Warning)
            {
                eraseStage = EraseStage.FinalConfirm;
                eraseConfirmButton.Text = "Yes, Erase Everything";
                PositionEraseButtons();
            }
            else if (eraseStage == EraseStage.FinalConfirm)
            {
                Util.EraseAllAccountData();

                eraseStage = EraseStage.None;
                eraseConfirmButton.Text = "Continue";
                PositionEraseButtons();

                foreach (var slot in slots)
                {
                    slot.HasSave = false;
                    slot.ConfirmingDelete = false;
                    slot.Stars = 0;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            // The erase-all warning modal blocks everything underneath it
            // while open — no hovering/selecting/deleting a class slot, and
            // the button that opened it shouldn't itself still be clickable.
            if (eraseStage != EraseStage.None)
            {
                eraseCancelButton.Update(gameTime);
                eraseConfirmButton.Update(gameTime);
                return;
            }

            // Every slot's Stars (and HasSave) needs to be known before the
            // lock check below can run — a locked slot's IsLocked reads the
            // PREVIOUS slot's Stars, which must already reflect this frame's
            // save data by then, not a stale value from before this loop.
            foreach (var slot in slots)
            {
                // One read regardless of lock state — the star rating is a
                // permanent record shown either way (see Slot.Stars), unlike
                // HasSave/delete, which only matter for a playable slot.
                PlayerData saved = Util.PeekPlayerData(slot.PlayerClass);
                slot.Stars = Player.ComputeStars(saved?.HighScore ?? 0);
                slot.HasSave = HasDeletableProgress(saved);
            }

            foreach (var slot in slots)
            {
                slot.Hover = slot.BorderRect.Intersects(Input.MouseBounds);

                if (slot.PreviousClass.HasValue)
                {
                    slot.PreviousClassStars = slots
                        .Single(s => s.PlayerClass == slot.PreviousClass.Value)
                        .Stars;
                    slot.IsLocked = slot.PreviousClassStars < RequiredStarsPerUnlock;
                }
                else
                {
                    slot.IsLocked = false;
                }

                if (slot.IsLocked)
                {
                    // No save/delete controls or selection for a locked
                    // class — there's nothing to preview or delete for a
                    // class that's never been playable yet, and clicking it
                    // should read as "not yet", not silently do nothing.
                    if (slot.Hover && Input.GetMouseClick())
                        Sound.Play(Sound.Error, 0.4f);
                    continue;
                }

                UpdateDeleteControls(slot);

                if (slot.Hover && Input.GetMouseClick())
                {
                    SelectCharacter(slot.PlayerClass);
                }
            }

            backButton.Update(gameTime);
            eraseAllButton.Update(gameTime);
        }

        // Lays out and hit-tests the "Delete" link (or, while confirming, the
        // "Delete save? Yes No" row) directly below the class label. Layout is
        // computed here rather than in Draw so the click rectangles and the drawn
        // text always agree on position.
        private void UpdateDeleteControls(Slot slot)
        {
            Vector2 labelSize = Art.RetroFont.MeasureString(slot.PlayerClass.ToString());
            Vector2 starsSize = Art.RetroFont.MeasureString(BuildStarsText(slot.Stars));
            int rowY = (int)(slot.BorderRect.Bottom + 8 + labelSize.Y + 4 + starsSize.Y + 4);

            if (slot.ConfirmingDelete)
            {
                const string confirmText = "Delete save? ";
                const string yesText = "Yes";
                const string noText = "No";

                Vector2 confirmSize = Art.RetroFont.MeasureString(confirmText);
                Vector2 yesSize = Art.RetroFont.MeasureString(yesText);
                Vector2 noSize = Art.RetroFont.MeasureString(noText);

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
                Vector2 deleteSize = Art.RetroFont.MeasureString(deleteText);
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

        // Plain-ASCII rendering ("*" instead of a real star glyph) — the
        // game's SpriteFonts only bake in the standard ASCII range (32-126,
        // see Content/Fonts/*.spritefont), so drawing an actual ★ character
        // would throw at runtime, not silently fall back.
        private static string BuildStarsText(int stars) =>
            "Stars: " + new string('*', stars) + new string('-', Player.MaxStars - stars);

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
                Vector2 subtitleSize = Art.RetroFont.MeasureString(subtitle);
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    subtitle,
                    new Vector2(CenterWidth - subtitleSize.X / 2, 128 / Game1.Scale),
                    Color.White
                );
            }

            foreach (var slot in slots)
            {
                Color borderColor = slot.IsLocked
                    ? Color.DarkGray
                    : (slot.Hover ? Color.Gold : Color.White);
                Color portraitColor = slot.IsLocked ? Color.DarkGray : Color.White;

                spriteBatch.Draw(Art.Border, slot.BorderRect, borderColor);
                spriteBatch.Draw(slot.Portrait, slot.PortraitRect, portraitColor);

                string label = slot.IsLocked
                    ? slot.PlayerClass.ToString() + " (Locked)"
                    : slot.PlayerClass.ToString();
                Vector2 labelSize = Art.RetroFont.MeasureString(label);
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    label,
                    new Vector2(
                        slot.PortraitRect.Center.X - labelSize.X / 2,
                        slot.BorderRect.Bottom + 8
                    ),
                    slot.IsLocked ? Color.Gray : Color.White
                );

                // Always shown, locked or not — a permanent record of what's
                // already been earned, not something tied to whether the
                // class happens to be selectable right now.
                string starsText = BuildStarsText(slot.Stars);
                Vector2 starsSize = Art.RetroFont.MeasureString(starsText);
                DrawShadowedText(
                    spriteBatch,
                    starsText,
                    new Vector2(
                        slot.PortraitRect.Center.X - starsSize.X / 2,
                        slot.BorderRect.Bottom + 8 + labelSize.Y + 4
                    ),
                    Color.Gold
                );

                if (slot.IsLocked)
                {
                    if (slot.Hover)
                        DrawLockedPreview(spriteBatch, slot);

                    continue;
                }

                if (slot.Hover)
                {
                    DrawPreview(spriteBatch, slot);
                }

                if (slot.ConfirmingDelete)
                {
                    DrawShadowedText(
                        spriteBatch,
                        "Delete save? ",
                        slot.ConfirmLabelPos,
                        Color.White
                    );
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
            eraseAllButton.Draw(gameTime, spriteBatch);

            if (eraseStage != EraseStage.None)
                DrawEraseWarning(gameTime, spriteBatch);

            spriteBatch.End();
        }

        // Full-screen dim plus a centered box — deliberately more visually
        // severe than the inline "Delete save? Yes/No" row used per-character
        // above, matching how much more destructive (every class, Fame, and
        // every star at once, with nothing preserved) this action is.
        private void DrawEraseWarning(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                Art.HealthBar,
                new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                Color.Black * 0.75f
            );

            const int boxWidth = 420;
            const int boxHeight = 150;
            Rectangle box = new(
                CenterWidth - boxWidth / 2,
                CenterHeight - boxHeight / 2,
                boxWidth,
                boxHeight
            );
            spriteBatch.Draw(Art.HealthBar, box, Color.Black * 0.9f);
            spriteBatch.Draw(Art.Border, box, Color.DarkRed);

            string message =
                eraseStage == EraseStage.Warning
                    ? "This will permanently erase EVERY character,\nunlock, Fame total, high score, and star.\nThis cannot be undone."
                    : "Are you absolutely sure?\nThis is your last chance to back out.";

            Vector2 messageSize = Art.RetroFont.MeasureString(message);
            DrawShadowedText(
                spriteBatch,
                message,
                new Vector2(CenterWidth - messageSize.X / 2, box.Top + 16),
                Color.OrangeRed
            );

            eraseCancelButton.Draw(gameTime, spriteBatch);
            eraseConfirmButton.Draw(gameTime, spriteBatch);
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

            // This class's own Base Fame — the same per-life XP-derived
            // value the Class Quest tiers/stars are based on (see
            // Player.ComputeBaseFame/ComputeStars) — not the account-wide
            // FameSystem.Fame shown at the top of the menu, which is shared
            // across every class.
            string fameText = $"Fame: {Player.ComputeBaseFame(saved?.ExperienceTotal ?? 0)}";
            string highestFameText =
                $"Highest Fame: {Player.ComputeBaseFame(saved?.HighScore ?? 0)}";

            // Stack bottom-up from just above the portrait: stats block closest to the
            // portrait, then Fame, then Highest Fame on top.
            float bottom = slot.BorderRect.Top - PreviewGap;

            Vector2 statsSize = Art.RetroFont.MeasureString(statsText);
            Vector2 statsPos = new(
                slot.PortraitRect.Center.X - statsSize.X / 2,
                bottom - statsSize.Y
            );

            Vector2 fameSize = Art.RetroFont.MeasureString(fameText);
            Vector2 famePos = new(
                slot.PortraitRect.Center.X - fameSize.X / 2,
                statsPos.Y - fameSize.Y
            );

            Vector2 highestFameSize = Art.RetroFont.MeasureString(highestFameText);
            Vector2 highestFamePos = new(
                slot.PortraitRect.Center.X - highestFameSize.X / 2,
                famePos.Y - highestFameSize.Y
            );

            DrawShadowedText(spriteBatch, statsText, statsPos, Color.Red);
            DrawShadowedText(spriteBatch, fameText, famePos, Color.Gold);
            DrawShadowedText(spriteBatch, highestFameText, highestFamePos, Color.LightBlue);
        }

        // Shown instead of DrawPreview() while hovering a locked slot — how
        // many more stars are needed in the previous class, rather than
        // stats for a class that's never been playable yet.
        private void DrawLockedPreview(SpriteBatch spriteBatch, Slot slot)
        {
            string text =
                $"Requires {RequiredStarsPerUnlock} Stars in {slot.PreviousClass}\n"
                + $"(You have {slot.PreviousClassStars})";

            Vector2 size = Art.RetroFont.MeasureString(text);
            Vector2 pos = new(
                slot.PortraitRect.Center.X - size.X / 2,
                slot.BorderRect.Top - PreviewGap - size.Y
            );

            DrawShadowedText(spriteBatch, text, pos, Color.OrangeRed);
        }

        // Despite the name (kept as-is rather than touching every one of
        // this method's call sites), this now draws a full black outline
        // via Util.DrawOutlinedText rather than a single offset shadow —
        // matching the "all text gets a black outline" treatment used
        // everywhere else now.
        private static void DrawShadowedText(
            SpriteBatch spriteBatch,
            string text,
            Vector2 pos,
            Color color
        )
        {
            Util.DrawOutlinedText(spriteBatch, Art.RetroFont, text, pos, color);
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
            return $"Level: {level}\n"
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
                Player.Class.Priest => BuildPreviewText(1, 100, 150, 26, 0, 22, 23, 5, 17),
                _ => string.Empty,
            };
        }

        public override void PostUpdate(GameTime gameTime) { }
    }
}
