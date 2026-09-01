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
    // The account-level character-slots screen — reached via
    // StateManager.CharacterSlots() (renamed from the old SelectClass(),
    // which used to land straight on the 5-class picker). A vertical,
    // scrollable list of character slots: occupied ones can be played or
    // deleted, the one empty-and-unlocked slot at a time can create a new
    // character (via CharacterCreationState), and exactly one locked "next"
    // slot is always shown, purchasable with account Fame. Also owns the
    // account-wide "Erase All Data" flow, moved here wholesale from the old
    // CharacterSelectState — it wipes every character, not just one class,
    // so it belongs on this account-level screen rather than inside
    // per-slot Character Creation.
    public class CharacterSlotsState : State
    {
        private class Row
        {
            public int SlotIndex;
            public bool IsLocked;
            public bool IsEmpty;
            public CharacterSlotEntryData Entry;
            public PlayerData Saved;

            // Absolute on-screen bounds this frame, after the current
            // scroll offset — used for both drawing and hit-testing.
            public Rectangle Bounds;
            public bool Hover;

            public Rectangle DeleteIconRect;
            public bool DeleteIconHover;

            public Rectangle ConfirmYesRect;
            public bool ConfirmYesHover;
            public Rectangle ConfirmNoRect;
            public bool ConfirmNoHover;
        }

        private const int ListWidth = 760;
        private const int RowHeight = 96;
        private const int RowSpacing = 12;
        private const int RowPadding = 14;
        private const int PortraitSize = 64;
        private const int ItemIconSize = 32;
        private const int ItemIconGap = 6;
        private const int DeleteIconSize = 24;

        // One step per standard wheel notch (120 units of ScrollWheelValue)
        // — same pattern already established by Overlay.HandleMinimapZoom()/
        // Camera's own scroll-to-zoom, just driving a list offset instead of
        // a zoom level.
        private const float ScrollStepPerNotch = 40f;

        private static readonly RasterizerState ScissorRasterizerState =
            new() { ScissorTestEnable = true };

        private readonly Button backButton;
        private readonly Menu menu;

        private Rectangle listViewport;
        private float scrollOffsetY;
        private List<Row> rows = [];

        // Only one delete/purchase confirmation is ever open at a time —
        // opening a different one implicitly cancels whichever was open,
        // same single-conversation-at-a-time behavior the old per-slot
        // delete confirm already had.
        private int? confirmingDeleteSlotIndex;
        private bool confirmingPurchase;

        // Full-screen "Erase All Data" flow — moved here verbatim from the
        // old CharacterSelectState, unchanged in shape (still its own
        // two-stage confirm, still positioned/updated/drawn independently
        // of the row list and the Back button).
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

        public CharacterSlotsState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Game1.Instance.IsMouseVisible = true;

            int listTop = 110;
            int listBottom = ScreenHeight - 110;
            listViewport = new Rectangle(
                CenterWidth - ListWidth / 2,
                listTop,
                ListWidth,
                listBottom - listTop
            );

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

                confirmingDeleteSlotIndex = null;
                confirmingPurchase = false;
                scrollOffsetY = 0;

                // Rebuild immediately rather than waiting for next frame's
                // Update() — otherwise this frame's Draw() would render the
                // just-erased characters for one frame before the erase
                // modal (already cleared above) stops covering them.
                BuildRows();
            }
        }

        private int TotalContentHeight() =>
            rows.Count * RowHeight + Math.Max(0, rows.Count - 1) * RowSpacing;

        private float MaxScrollOffset() =>
            Math.Max(0, TotalContentHeight() - listViewport.Height);

        // Builds this frame's row list — every unlocked slot (occupied or
        // empty) plus exactly one locked "next" row, matching "unlocking a
        // slot reveals the next locked one" literally rather than showing
        // several future locked rows at once.
        private void BuildRows()
        {
            rows = [];

            for (int i = 0; i <= CharacterSlotSystem.UnlockedSlotCount; i++)
            {
                bool isLocked = i >= CharacterSlotSystem.UnlockedSlotCount;
                CharacterSlotEntryData entry = isLocked ? null : CharacterSlotSystem.GetEntry(i);
                PlayerData saved =
                    entry != null ? Util.PeekPlayerData(entry.CharacterId) : null;

                rows.Add(
                    new Row
                    {
                        SlotIndex = i,
                        IsLocked = isLocked,
                        IsEmpty = !isLocked && entry == null,
                        Entry = entry,
                        Saved = saved,
                    }
                );
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (eraseStage != EraseStage.None)
            {
                eraseCancelButton.Update(gameTime);
                eraseConfirmButton.Update(gameTime);
                return;
            }

            BuildRows();
            UpdateScroll();
            UpdateRowLayoutAndInput();

            backButton.Update(gameTime);
            eraseAllButton.Update(gameTime);
        }

        private void UpdateScroll()
        {
            if (!listViewport.Contains(Input.MousePosition))
                return;

            int scrollDelta = Input.mouse.ScrollWheelValue - Input.previousMouse.ScrollWheelValue;
            if (scrollDelta == 0)
                return;

            float notches = scrollDelta / 120f;
            // Scrolling up (positive delta) moves the list content down
            // (offset decreases) — conventional list-scroll direction.
            scrollOffsetY = MathHelper.Clamp(
                scrollOffsetY - notches * ScrollStepPerNotch,
                0,
                MaxScrollOffset()
            );
        }

        private void UpdateRowLayoutAndInput()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                int y = listViewport.Y + i * (RowHeight + RowSpacing) - (int)scrollOffsetY;
                row.Bounds = new Rectangle(listViewport.X, y, ListWidth, RowHeight);

                // A row scrolled fully outside the viewport doesn't accept
                // hover/click at all — mirrors the scissor clip that
                // already keeps it from being drawn.
                bool visible = row.Bounds.Bottom > listViewport.Top
                    && row.Bounds.Top < listViewport.Bottom;
                row.Hover = visible && row.Bounds.Contains(Input.MousePosition);

                if (row.IsLocked)
                {
                    UpdateLockedRow(row, visible);
                }
                else if (row.IsEmpty)
                {
                    UpdateEmptyRow(row, visible);
                }
                else
                {
                    UpdateOccupiedRow(row, visible);
                }
            }
        }

        private void UpdateLockedRow(Row row, bool visible)
        {
            if (!visible)
                return;

            if (confirmingPurchase)
            {
                const string yesText = "Yes";
                const string noText = "No";
                Vector2 yesSize = Art.RetroFont.MeasureString(yesText);
                Vector2 noSize = Art.RetroFont.MeasureString(noText);

                int rowCenterY = row.Bounds.Center.Y;
                row.ConfirmYesRect = new Rectangle(
                    row.Bounds.Center.X - (int)(yesSize.X + 8 + noSize.X) / 2,
                    rowCenterY + 14,
                    (int)yesSize.X,
                    (int)yesSize.Y
                );
                row.ConfirmNoRect = new Rectangle(
                    row.ConfirmYesRect.Right + 8,
                    rowCenterY + 14,
                    (int)noSize.X,
                    (int)noSize.Y
                );

                row.ConfirmYesHover = row.ConfirmYesRect.Contains(Input.MousePosition);
                row.ConfirmNoHover = row.ConfirmNoRect.Contains(Input.MousePosition);

                if (row.ConfirmYesHover && Input.GetMouseClick())
                {
                    if (CharacterSlotSystem.TryPurchaseNextSlot())
                    {
                        // TryPurchaseNextSlot() only mutates FameSystem.Fame/
                        // CharacterSlotSystem.UnlockedSlotCount in memory —
                        // without saving both files here, a successful
                        // purchase would silently revert on next boot
                        // (Fame back up, slot back to locked) unless some
                        // unrelated later event happened to trigger a save
                        // first.
                        Util.SaveFameData();
                        Util.SaveCharacterSlotsData();
                    }
                    else
                    {
                        Sound.Play(Sound.Error, 0.4f);
                    }

                    confirmingPurchase = false;
                }
                else if (row.ConfirmNoHover && Input.GetMouseClick())
                {
                    confirmingPurchase = false;
                }
            }
            else if (row.Hover && Input.GetMouseClick())
            {
                confirmingPurchase = true;
                confirmingDeleteSlotIndex = null;
            }
        }

        private void UpdateEmptyRow(Row row, bool visible)
        {
            if (visible && row.Hover && Input.GetMouseClick())
            {
                Game1.Instance.ChangeState(
                    new CharacterCreationState(
                        Game1.Instance,
                        Game1.Instance.GraphicsDevice,
                        Game1.Instance.Content,
                        row.SlotIndex
                    )
                );
            }
        }

        private void UpdateOccupiedRow(Row row, bool visible)
        {
            if (!visible)
                return;

            Vector2 labelSize = Art.RetroFont.MeasureString("X");
            row.DeleteIconRect = new Rectangle(
                row.Bounds.Right - RowPadding - DeleteIconSize,
                row.Bounds.Y + RowPadding,
                DeleteIconSize,
                DeleteIconSize
            );
            row.DeleteIconHover = row.DeleteIconRect.Contains(Input.MousePosition);

            if (confirmingDeleteSlotIndex == row.SlotIndex)
            {
                const string yesText = "Yes";
                const string noText = "No";
                Vector2 yesSize = Art.RetroFont.MeasureString(yesText);
                Vector2 noSize = Art.RetroFont.MeasureString(noText);

                int confirmY = row.Bounds.Bottom - (int)yesSize.Y - 8;
                row.ConfirmYesRect = new Rectangle(
                    row.DeleteIconRect.Right - (int)(yesSize.X + 8 + noSize.X),
                    confirmY,
                    (int)yesSize.X,
                    (int)yesSize.Y
                );
                row.ConfirmNoRect = new Rectangle(
                    row.ConfirmYesRect.Right + 8,
                    confirmY,
                    (int)noSize.X,
                    (int)noSize.Y
                );

                row.ConfirmYesHover = row.ConfirmYesRect.Contains(Input.MousePosition);
                row.ConfirmNoHover = row.ConfirmNoRect.Contains(Input.MousePosition);

                if (row.ConfirmYesHover && Input.GetMouseClick())
                {
                    DeleteCharacter(row);
                }
                else if (row.ConfirmNoHover && Input.GetMouseClick())
                {
                    confirmingDeleteSlotIndex = null;
                }

                return;
            }

            if (row.DeleteIconHover && Input.GetMouseClick())
            {
                confirmingDeleteSlotIndex = row.SlotIndex;
                confirmingPurchase = false;
                return;
            }

            // Click anywhere else on an occupied row plays that character.
            if (row.Hover && !row.DeleteIconHover && Input.GetMouseClick())
            {
                PlayCharacter(row.Entry);
            }
        }

        private void DeleteCharacter(Row row)
        {
            Guid characterId = row.Entry.CharacterId;
            Player.Class playerClass = row.Entry.PlayerClass;

            Util.DeleteCharacterData(characterId);

            // If the character being deleted is also the one currently
            // loaded in memory, reset the live instance too — otherwise the
            // next autosave would silently recreate the file from stale
            // in-memory stats, undoing the delete.
            if (characterId == Player.Instance.ID)
            {
                EntityManager.RemovePlayer();
                Util.ResetPlayer(playerClass);
                // Same reasoning as PlayCharacter()/CharacterCreationState's
                // SelectCharacter() — ResetPlayer() just handed us a brand
                // new Player.Instance, so the account-wide settings living
                // on it need reloading too.
                Util.LoadGameSettingsData();
                EntityManager.Add(Player.Instance);
            }

            confirmingDeleteSlotIndex = null;
        }

        private void PlayCharacter(CharacterSlotEntryData entry)
        {
            EntityManager.RemovePlayer();

            Util.LoadOrCreatePlayer(entry.PlayerClass, entry.CharacterId);

            CharacterSlotSystem.TouchLastPlayed(entry.CharacterId);
            Util.SaveCharacterSlotsData();

            // LoadOrCreatePlayer() reconstructs Player.Instance from scratch
            // — GameSettingsData's fields live directly on it, so a fresh
            // instance drops every account-wide setting back to its C#
            // default until this reloads them.
            Util.LoadGameSettingsData();

            EntityManager.Add(Player.Instance);

            StateManager.NewGame();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            string title = "Characters";
            Vector2 titleSize = Art.RetroFont.MeasureString(title);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                title,
                new Vector2(CenterWidth - titleSize.X / 2, 40),
                Color.White
            );

            // Reuses the same account-wide Fame display already shown
            // top-left during actual gameplay (Overlay.cs) — relevant here
            // too, since it's exactly what a slot purchase spends.
            Overlay.DrawFame(spriteBatch);

            spriteBatch.End();

            DrawRows(spriteBatch);

            spriteBatch.Begin();
            menu.Draw(gameTime, spriteBatch);
            eraseAllButton.Draw(gameTime, spriteBatch);

            if (eraseStage != EraseStage.None)
                DrawEraseWarning(gameTime, spriteBatch);

            spriteBatch.End();
        }

        private void DrawRows(SpriteBatch spriteBatch)
        {
            Rectangle previousScissor = Game1.Instance.GraphicsDevice.ScissorRectangle;
            Game1.Instance.GraphicsDevice.ScissorRectangle = listViewport;

            spriteBatch.Begin(rasterizerState: ScissorRasterizerState);

            foreach (Row row in rows)
            {
                if (row.Bounds.Bottom <= listViewport.Top || row.Bounds.Top >= listViewport.Bottom)
                    continue;

                if (row.IsLocked)
                    DrawLockedRow(spriteBatch, row);
                else if (row.IsEmpty)
                    DrawEmptyRow(spriteBatch, row);
                else
                    DrawOccupiedRow(spriteBatch, row);
            }

            spriteBatch.End();

            Game1.Instance.GraphicsDevice.ScissorRectangle = previousScissor;
        }

        private void DrawRowFrame(SpriteBatch spriteBatch, Row row, Color borderColor)
        {
            spriteBatch.Draw(Art.HealthBar, row.Bounds, Color.Black * 0.5f);
            spriteBatch.Draw(Art.Border, row.Bounds, borderColor);
        }

        private void DrawLockedRow(SpriteBatch spriteBatch, Row row)
        {
            DrawRowFrame(spriteBatch, row, row.Hover ? Color.Gold : Color.DarkGray);

            int cost = CharacterSlotSystem.CostForNextSlot();
            // Plain hyphen, not an em dash — the game's SpriteFonts only
            // bake in the standard ASCII range (32-126, see
            // Content/Fonts/*.spritefont), so an em dash here throws
            // ArgumentException the instant this row is hovered/drawn,
            // same class of bug CharacterCreationState.BuildStarsText()'s
            // own comment already warns about for a literal star glyph.
            string text = $"Locked - Unlock for {cost} Fame";
            Vector2 textSize = Art.RetroFont.MeasureString(text);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                text,
                new Vector2(
                    row.Bounds.Center.X - textSize.X / 2,
                    row.Bounds.Y + RowPadding
                ),
                Color.Gray
            );

            if (confirmingPurchase)
            {
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "Yes",
                    row.ConfirmYesRect.Location.ToVector2(),
                    row.ConfirmYesHover ? Color.Gold : Color.OrangeRed
                );
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "No",
                    row.ConfirmNoRect.Location.ToVector2(),
                    row.ConfirmNoHover ? Color.Gold : Color.White
                );
            }
        }

        private void DrawEmptyRow(SpriteBatch spriteBatch, Row row)
        {
            DrawRowFrame(spriteBatch, row, row.Hover ? Color.Gold : Color.White);

            string text = "+ Create Character";
            Vector2 textSize = Art.RetroFont.MeasureString(text);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                text,
                new Vector2(row.Bounds.Center.X - textSize.X / 2, row.Bounds.Center.Y - textSize.Y / 2),
                row.Hover ? Color.Gold : Color.White
            );
        }

        private void DrawOccupiedRow(SpriteBatch spriteBatch, Row row)
        {
            DrawRowFrame(spriteBatch, row, row.Hover ? Color.Gold : Color.White);

            Rectangle portraitRect = new(
                row.Bounds.X + RowPadding,
                row.Bounds.Y + (RowHeight - PortraitSize) / 2,
                PortraitSize,
                PortraitSize
            );
            spriteBatch.Draw(Art.Border, portraitRect, Color.White);
            Texture2D portrait = ClassPortrait(row.Entry.PlayerClass);
            if (portrait != null)
                spriteBatch.Draw(portrait, portraitRect, Color.White);

            int textX = portraitRect.Right + RowPadding;

            string className = row.Entry.PlayerClass.ToString();
            Vector2 classNameSize = Art.RetroFont.MeasureString(className);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                className,
                new Vector2(textX, row.Bounds.Y + RowPadding),
                Color.White
            );

            // Equipped items — read-only catalog lookups (never through
            // Weapon.LoadWeapon()/Armor.LoadArmor()/etc., which equip onto
            // the live Player.Instance) so browsing this list never mutates
            // whichever character actually happens to be loaded right now.
            int iconY = row.Bounds.Y + (int)(RowPadding + classNameSize.Y + 8);
            Texture2D[] itemIcons =
            [
                ResolveIcon(Game1.Instance.Weapons, row.Saved?.Weapon?.Name),
                ResolveIcon(Game1.Instance.Armors, row.Saved?.Armor?.Name),
                ResolveIcon(Game1.Instance.Rings, row.Saved?.Ring?.Name),
                ResolveAbilityIcon(row.Saved),
            ];

            for (int i = 0; i < itemIcons.Length; i++)
            {
                Rectangle iconRect = new(
                    textX + i * (ItemIconSize + ItemIconGap),
                    iconY,
                    ItemIconSize,
                    ItemIconSize
                );
                spriteBatch.Draw(Art.Border, iconRect, Color.Gray);
                if (itemIcons[i] != null)
                    spriteBatch.Draw(itemIcons[i], iconRect, Color.White);
            }

            string fameText =
                $"Fame: {Player.ComputeBaseFame(row.Saved?.ExperienceTotal ?? 0)}   "
                + $"Highest: {Player.ComputeBaseFame(row.Saved?.HighScore ?? 0)}";
            Vector2 fameSize = Art.RetroFont.MeasureString(fameText);
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                fameText,
                new Vector2(textX, iconY + ItemIconSize + 8),
                Color.Gold
            );

            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                "X",
                row.DeleteIconRect.Location.ToVector2(),
                row.DeleteIconHover ? Color.Gold : Color.OrangeRed
            );

            if (confirmingDeleteSlotIndex == row.SlotIndex)
            {
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "Delete?",
                    new Vector2(
                        row.ConfirmYesRect.X
                            - Art.RetroFont.MeasureString("Delete? ").X,
                        row.ConfirmYesRect.Y
                    ),
                    Color.White
                );
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "Yes",
                    row.ConfirmYesRect.Location.ToVector2(),
                    row.ConfirmYesHover ? Color.Gold : Color.OrangeRed
                );
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    "No",
                    row.ConfirmNoRect.Location.ToVector2(),
                    row.ConfirmNoHover ? Color.Gold : Color.White
                );
            }
        }

        private static Texture2D ClassPortrait(Player.Class playerClass) =>
            playerClass switch
            {
                Player.Class.Wizard => Art.Wizard,
                Player.Class.Archer => Art.Archer,
                Player.Class.Knight => Art.Knight,
                Player.Class.Priest => Art.Priest,
                Player.Class.Rogue => Art.Rogue,
                _ => null,
            };

        // Resolves an equipped item's icon purely from the shared catalog
        // list — Game1.Instance.Weapons/Armors/Rings already hold live,
        // pre-loaded textures on each entry's own `image` field (see
        // Equipment.cs/Entity.cs) — never through the item's own
        // LoadX(name) factory, which equips onto Player.Instance as a side
        // effect. Returns null for an unequipped slot (itemName null) or an
        // item that no longer resolves (e.g. removed/renamed since this
        // character last saved), same as an unequipped slot visually.
        private static Texture2D ResolveIcon<T>(List<T> catalog, string itemName)
            where T : Entity
        {
            if (itemName == null)
                return null;

            return catalog.FirstOrDefault(item => NameOf(item) == itemName)?.image;
        }

        private static string NameOf(Entity entity) =>
            entity switch
            {
                Weapon w => w.Name,
                Armor a => a.Name,
                Ring r => r.Name,
                _ => null,
            };

        // AbilityItem is one of five possible saved fields (Spell/Quiver/
        // Shield/Tome/Cloak — only ever one populated, same "which one is
        // non-null with a real Name" signal Util.LoadOrCreatePlayer()'s own
        // equip logic already uses) rather than a single field like Weapon/
        // Armor/Ring, so it needs its own resolution instead of going
        // through the generic ResolveIcon<T>() above.
        private static Texture2D ResolveAbilityIcon(PlayerData saved)
        {
            if (saved == null)
                return null;

            if (saved.Spell?.Name != null)
                return Game1.Instance.Spells.FirstOrDefault(s => s.Name == saved.Spell.Name)?.image;
            if (saved.Quiver?.Name != null)
                return Game1
                    .Instance.Quivers.FirstOrDefault(q => q.Name == saved.Quiver.Name)
                    ?.image;
            if (saved.Shield?.Name != null)
                return Game1
                    .Instance.Shields.FirstOrDefault(s => s.Name == saved.Shield.Name)
                    ?.image;
            if (saved.Tome?.Name != null)
                return Game1.Instance.Tomes.FirstOrDefault(t => t.Name == saved.Tome.Name)?.image;
            if (saved.Cloak?.Name != null)
                return Game1.Instance.Cloaks.FirstOrDefault(c => c.Name == saved.Cloak.Name)?.image;

            return null;
        }

        // Full-screen dim plus a centered box — unchanged from the old
        // CharacterSelectState's version.
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
            Util.DrawOutlinedText(
                spriteBatch,
                Art.RetroFont,
                message,
                new Vector2(CenterWidth - messageSize.X / 2, box.Top + 16),
                Color.OrangeRed
            );

            eraseCancelButton.Draw(gameTime, spriteBatch);
            eraseConfirmButton.Draw(gameTime, spriteBatch);
        }

        public override void PostUpdate(GameTime gameTime) { }
    }
}
