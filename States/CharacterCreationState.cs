using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.Controls;

namespace Realm.States
{
    // Renamed from CharacterSelectState as part of the character-slot
    // rework — reached only by clicking an empty slot in
    // CharacterSlotsState, which passes the target slotIndex in. Its one
    // job now is "pick a class for this blank slot," still gated by the
    // existing star-based unlock chain; everything about managing existing
    // characters (delete, Erase All Data) moved to CharacterSlotsState,
    // since that's the account-level screen now, not this one.
    public class CharacterCreationState : State
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
            // class's own permanent star record (ClassRecordSystem, an
            // account-wide record independent of any single character's
            // save — see that class's own doc comment for why) rather than
            // a separate persisted flag: stars only ever increase, so "has
            // the previous class ever reached 3 stars" and "is this class
            // permanently unlocked" are the same question — no new save
            // state needed.
            public Player.Class? PreviousClass;

            // Refreshed each Update() from PreviousClass's own Stars below
            // (0 and unused when PreviousClass is null) — cached here so
            // IsLocked and the locked-preview text don't need to search
            // the slots list again themselves.
            public int PreviousClassStars;
            public bool IsLocked;

            // Highest star rating (0-5) this class has ever achieved —
            // permanent, computed fresh each Update() from
            // ClassRecordSystem's own best-ever-HighScore record for this
            // class (see ComputeStars()) rather than stored redundantly.
            // Shown regardless of lock state, since it's a record of what's
            // already been earned by any character of this class, past or
            // present.
            public int Stars;
        }

        // Unlock chain: Wizard starts unlocked; each class after it needs
        // this many stars earned in the class immediately to its left below
        // (Wizard -> Priest -> Archer -> Knight -> Rogue) — see
        // Slot.PreviousClass. Rogue is appended to the end of this same
        // stars-based chain rather than the real game's own "unlocked by
        // reaching level 5 on Archer" condition — this project already
        // fully replaced real RotMG's per-class unlock conditions with this
        // one uniform mechanism for every existing class, so a second,
        // different unlock-condition type (checking Level on a specific
        // save file) solely for Rogue would be inconsistent with that
        // already-established design, not more faithful to it.
        private const int RequiredStarsPerUnlock = 3;

        private const int PortraitSize = 80;
        private const int BorderPadding = 14;

        // Five evenly-spaced slots (Wizard/Priest/Archer/Knight/Rogue, left
        // to right — the same order as the unlock chain above), 150px
        // between adjacent centers.
        private const int SlotOffsetFromCenterOuter = 300;
        private const int SlotOffsetFromCenterInner = 150;
        private const int SlotOffsetFromCenterMid = 0;
        private const int PreviewGap = 10;

        private readonly List<Slot> slots;
        private readonly Button backButton;
        private readonly Menu menu;

        // Which character slot this new character will occupy once a class
        // is picked — passed in from CharacterSlotsState's "Create
        // Character" click on an empty, unlocked slot.
        private readonly int slotIndex;

        public CharacterCreationState(
            Game1 game,
            GraphicsDevice graphicsDevice,
            ContentManager content,
            int slotIndex
        )
            : base()
        {
            this.slotIndex = slotIndex;

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
                        CenterWidth + SlotOffsetFromCenterMid - PortraitSize / 2,
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
                        CenterWidth + SlotOffsetFromCenterInner - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                    PreviousClass = Player.Class.Archer,
                },
                new Slot
                {
                    PlayerClass = Player.Class.Rogue,
                    Portrait = Art.Rogue,
                    PortraitRect = new Rectangle(
                        CenterWidth + SlotOffsetFromCenterOuter - PortraitSize / 2,
                        y,
                        PortraitSize,
                        PortraitSize
                    ),
                    PreviousClass = Player.Class.Knight,
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
            StateManager.CharacterSlots();
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var slot in slots)
            {
                slot.Stars = Player.ComputeStars(ClassRecordSystem.GetBestHighScore(slot.PlayerClass));
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
                    // No selection for a locked class — clicking it should
                    // read as "not yet", not silently do nothing.
                    if (slot.Hover && Input.GetMouseClick())
                        Sound.Play(Sound.Error, 0.4f);
                    continue;
                }

                if (slot.Hover && Input.GetMouseClick())
                {
                    SelectCharacter(slot.PlayerClass);
                }
            }

            backButton.Update(gameTime);
        }

        // Plain-ASCII rendering ("*" instead of a real star glyph) — the
        // game's SpriteFonts only bake in the standard ASCII range (32-126,
        // see Content/Fonts/*.spritefont), so drawing an actual ★ character
        // would throw at runtime, not silently fall back.
        private static string BuildStarsText(int stars) =>
            "Stars: " + new string('*', stars) + new string('-', Player.MaxStars - stars);

        // Constructs the chosen class as a brand-new character, registers it
        // into the slot that opened this screen, and drops the player into
        // the hub.
        private void SelectCharacter(Player.Class playerClass)
        {
            EntityManager.RemovePlayer();

            Util.LoadOrCreatePlayer(playerClass, characterId: null);

            CharacterSlotSystem.AssignCharacterToSlot(slotIndex, Player.Instance.ID, playerClass);
            Util.SaveCharacterSlotsData();

            // LoadOrCreatePlayer() reconstructs Player.Instance from scratch
            // (see Util.ResetPlayer()) — GameSettingsData's fields live
            // directly on Player.Instance, not a separate object, so a fresh
            // instance drops every account-wide setting back to its C#
            // default until this reloads them, same as Game1.StartGame()
            // does after its own initial LoadOrCreatePlayer() call. Without
            // this, every setting silently resets on every character switch.
            Util.LoadGameSettingsData();

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
                string subtitle = "Select a Class";
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
            }

            menu.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        private void DrawPreview(SpriteBatch spriteBatch, Slot slot)
        {
            string statsText = BuildDefaultPreviewText(slot.PlayerClass);

            float bottom = slot.BorderRect.Top - PreviewGap;

            Vector2 statsSize = Art.RetroFont.MeasureString(statsText);
            Vector2 statsPos = new(
                slot.PortraitRect.Center.X - statsSize.X / 2,
                bottom - statsSize.Y
            );

            DrawShadowedText(spriteBatch, statsText, statsPos, Color.Red);
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

        // Mirrors the starting stats set in Wizard()/Archer()'s constructors
        // — every character created from this screen is always brand new
        // (Character Creation only handles empty slots now), so there's
        // never a saved character's real stats to show instead.
        private static string BuildDefaultPreviewText(Player.Class playerClass)
        {
            return playerClass switch
            {
                Player.Class.Wizard => BuildPreviewText(1, 100, 100, 17, 0, 17, 17, 5, 23),
                Player.Class.Archer => BuildPreviewText(1, 150, 100, 17, 0, 22, 15, 5, 15),
                Player.Class.Knight => BuildPreviewText(1, 200, 50, 17, 10, 13, 15, 10, 8),
                Player.Class.Priest => BuildPreviewText(1, 100, 150, 26, 0, 22, 23, 5, 17),
                Player.Class.Rogue => BuildPreviewText(1, 150, 100, 16, 0, 26, 17, 5, 15),
                _ => string.Empty,
            };
        }

        public override void PostUpdate(GameTime gameTime) { }
    }
}
