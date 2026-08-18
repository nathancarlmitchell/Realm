using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // A persistent, account-level item store shared across every class's save —
    // unlike InventorySystem, which is per-Player and rebuilt on every class
    // switch/reset. Opened by standing near the Bank portal in the Nexus (see
    // Portal.cs/NexusState.cs), not by changing game state — IsOpen just gates
    // whether Update()/Draw() do anything.
    public static class BankSystem
    {
        public const int MAXIMUM_SLOTS_IN_BANK = 8;
        private const int ColumnsPerRow = 4;

        // Fixed-size, not a compact list — index i is always slot i, so an
        // empty slot (null) stays where it is rather than everything after it
        // sliding down to fill the gap. Matches InventorySystem's own
        // InventoryRecords for the same reason.
        public static readonly InventorySystem.InventoryRecord[] Records =
            new InventorySystem.InventoryRecord[MAXIMUM_SLOTS_IN_BANK];

        public static bool IsOpen;

        public static bool HasEmptySlot => Array.IndexOf(Records, null) != -1;

        // Places into the first empty slot. Callers always check HasEmptySlot
        // first, so there's no full/error handling here.
        public static void AddRecord(InventorySystem.InventoryRecord record)
        {
            int slot = Array.IndexOf(Records, null);
            if (slot != -1)
                Records[slot] = record;
        }

        // Finds record by reference and nulls that slot — no shifting, unlike
        // List.Remove. Returns false if the record isn't actually in here.
        public static bool RemoveRecord(InventorySystem.InventoryRecord record)
        {
            int slot = Array.IndexOf(Records, record);
            if (slot == -1)
                return false;

            Records[slot] = null;
            return true;
        }

        // The slot index a screen point falls within (same 4-wide, 2-row
        // layout Update()/Draw() use), or -1 if the point isn't over the
        // grid at all. Reads Anchor fresh since the panel tracks the portal
        // on screen every frame rather than sitting at a fixed position.
        public static int SlotIndexAt(Vector2 point)
        {
            Point anchor = Anchor;
            int col = (int)((point.X - anchor.X) / 40);
            int row = (int)((point.Y - anchor.Y) / 40);
            if (col < 0 || col >= ColumnsPerRow || row < 0 || row >= MAXIMUM_SLOTS_IN_BANK / ColumnsPerRow)
                return -1;
            return (row * ColumnsPerRow) + col;
        }

        // Places at the given slot if it's empty — used when a drop should
        // land wherever the cursor actually is, e.g. dragging in from the
        // inventory or an equip slot — falling back to the first empty slot
        // if the target is out of range or already occupied (same fallback
        // semantics as InventorySystem's own AddRecordAt).
        public static void AddRecordAt(int slot, InventorySystem.InventoryRecord record)
        {
            if (slot >= 0 && slot < Records.Length && Records[slot] == null)
                Records[slot] = record;
            else
                AddRecord(record);
        }

        // World-space center of the Bank portal, set by Portal.Update() every
        // frame the bank is interactable.
        public static Vector2 PortalPosition;

        private const int PanelWidth = 40 * ColumnsPerRow;
        private const int PanelHeight = 40 * (MAXIMUM_SLOTS_IN_BANK / ColumnsPerRow);

        // Portal.RenderedSize (96px) / 2 — half the bank portal's on-screen
        // footprint, so the panel clears the sprite itself rather than
        // overlapping it.
        private const int PortalOnScreenHalfHeight = 48;
        private const int GapAbovePortal = 24;

        // Recomputed every frame from the portal's world position (via the
        // camera's own transform, the same one used to draw everything else in
        // world-space) so the panel tracks the portal on screen as the camera
        // follows the player, instead of sitting at a fixed screen position
        // that could end up clipped depending on which direction the player
        // approached from.
        private static Point Anchor
        {
            get
            {
                Vector2 screenPos = Vector2.Transform(
                    PortalPosition,
                    Game1.Camera.GetTransformation()
                );
                return new Point(
                    (int)(screenPos.X - (PanelWidth / 2f)),
                    (int)(screenPos.Y - PortalOnScreenHalfHeight - GapAbovePortal - PanelHeight)
                );
            }
        }

        public static Rectangle Bounds
        {
            get
            {
                Point anchor = Anchor;
                return new Rectangle(anchor.X, anchor.Y, PanelWidth, PanelHeight);
            }
        }

        private static bool hover = false;
        private static bool mousePressed = false;
        private static bool dragItem = false;
        private static int bankSlot = 0;

        public static void Update()
        {
            hover = false;

            // If the panel closed (player walked away from the portal) mid-drag,
            // cancel it rather than leave the drag state stuck — the item just
            // stays in the bank, same as releasing back over its own slot would.
            if (!IsOpen)
            {
                dragItem = false;
                mousePressed = false;
                return;
            }

            Point anchor = Anchor;

            for (int i = 0; i < Records.Length; i++)
            {
                InventorySystem.InventoryRecord record = Records[i];
                if (record == null)
                    continue;

                int posX = anchor.X + ((i % ColumnsPerRow) * 40);
                int posY = anchor.Y + ((i / ColumnsPerRow) * 40);

                var bounds = new Rectangle(
                    posX,
                    posY,
                    record.InventoryItem.image.Width,
                    record.InventoryItem.image.Height
                );

                if (bounds.Intersects(Input.MouseBounds))
                {
                    hover = true;

                    if (Input.MousePressed())
                    {
                        if (!mousePressed && hover)
                        {
                            bankSlot = i;
                            dragItem = true;
                        }

                        mousePressed = true;
                        return;
                    }
                }
            }

            if (Input.MousePressed())
            {
                mousePressed = true;
            }

            if (Input.MouseReleased())
            {
                if (dragItem)
                {
                    InventorySystem.InventoryRecord draggedRecord = Records[bankSlot];

                    // Same equip-swap semantics as InventorySystem's own drag
                    // release handling, reused directly rather than duplicated.
                    if (!InventorySystem.TryEquipFromRecord(Records, draggedRecord))
                    {
                        if (Player.Instance.Inventory.Bounds.Intersects(Input.MouseBounds))
                        {
                            int targetSlot = Player.Instance.Inventory.SlotIndexAt(
                                Input.MousePosition
                            );
                            InventorySystem.InventoryRecord occupant =
                                targetSlot != -1
                                    ? Player.Instance.Inventory.InventoryRecords[targetSlot]
                                    : null;

                            if (occupant != null)
                            {
                                // Dragged onto an occupied inventory slot —
                                // swap instead of just depositing, same as an
                                // in-panel drag onto an occupied slot.
                                Player.Instance.Inventory.InventoryRecords[targetSlot] =
                                    draggedRecord;
                                Records[bankSlot] = occupant;
                                Sound.Play(Sound.InventoryMoveItem, 0.5f);
                            }
                            else if (!Player.Instance.Inventory.HasEmptySlot)
                            {
                                Sound.Play(Sound.Error, 0.4f);
                            }
                            else
                            {
                                RemoveRecord(draggedRecord);
                                Player.Instance.Inventory.AddRecordAt(targetSlot, draggedRecord);
                                Sound.Play(Sound.InventoryMoveItem, 0.5f);
                            }
                        }
                        else if (Bounds.Intersects(Input.MouseBounds))
                        {
                            int targetSlot = SlotIndexAt(Input.MousePosition);
                            if (targetSlot != -1 && targetSlot != bankSlot)
                            {
                                // Dragged to a different bank slot — move it
                                // there, swapping with whatever (if anything)
                                // already occupied that slot.
                                InventorySystem.InventoryRecord occupant = Records[targetSlot];
                                Records[targetSlot] = draggedRecord;
                                Records[bankSlot] = occupant;
                                Sound.Play(Sound.InventoryMoveItem, 0.5f);
                            }
                            else
                            {
                                // Released back over its own slot without
                                // dragging it anywhere — treat it as a click
                                // and move it to the inventory instead of the
                                // old no-op cancel.
                                if (!Player.Instance.Inventory.HasEmptySlot)
                                {
                                    Sound.Play(Sound.Error, 0.4f);
                                }
                                else
                                {
                                    RemoveRecord(draggedRecord);
                                    Player.Instance.Inventory.AddRecord(draggedRecord);
                                    Sound.Play(Sound.InventoryMoveItem, 0.5f);
                                }
                            }
                        }

                        // Released anywhere else — cancel. No ground-drop from
                        // the bank.
                    }

                    // Persist immediately rather than waiting for the next
                    // state-transition checkpoint (Nexus/MainMenu/etc.) —
                    // closing the game right after moving something into or
                    // out of the bank while still standing at it previously
                    // lost that change entirely. Saved unconditionally (even
                    // for a cancelled drag) rather than tracked per-branch, so
                    // a future branch added here can't accidentally forget to
                    // persist its own mutation.
                    Util.SaveBankData();
                    Util.SaveInventoryData();
                    Util.SavePlayerData();
                }

                mousePressed = false;
                dragItem = false;
            }
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen)
                return;

            Point anchor = Anchor;

            spriteBatch.Draw(Art.Inventory, new Vector2(anchor.X, anchor.Y), Color.White);

            // Every slot, empty ones just draw nothing, so removing an item
            // leaves a visible gap at its fixed position instead of
            // everything after it sliding down.
            for (int i = 0; i < Records.Length; i++)
            {
                InventorySystem.InventoryRecord record = Records[i];
                if (record == null)
                    continue;

                int posX = anchor.X + ((i % ColumnsPerRow) * 40);
                int posY = anchor.Y + ((i / ColumnsPerRow) * 40);

                spriteBatch.Draw(record.InventoryItem.image, new Vector2(posX, posY), Color.White);

                // Flag equipment the current class can't actually equip —
                // e.g. a Bow sitting in the shared bank while playing Wizard.
                if (record.InventoryItem is Equipment bankEquipment && !bankEquipment.CanEquipByCurrentClass)
                {
                    spriteBatch.Draw(Art.HealthBar, new Rectangle(posX, posY, 40, 40), Color.Red * 0.25f);
                }

                string quantityText = string.Empty;
                if (record.InventoryItem.MaximumStackableQuantity > 1)
                {
                    quantityText = "" + record.Quantity;
                }

                spriteBatch.DrawString(
                    Art.HudFont,
                    quantityText,
                    new Vector2(posX + 4, posY),
                    Color.Black
                );

                var bounds = new Rectangle(
                    posX,
                    posY,
                    record.InventoryItem.image.Width,
                    record.InventoryItem.image.Height
                );

                if (bounds.Intersects(Input.MouseBounds))
                {
                    // Full Tier/name/description/bonuses for equipment, same
                    // as hovering the equip slot itself — plain items (e.g.
                    // potions) just show their name, matching the old
                    // behavior since they have no Tier/bonuses to show.
                    // Equipment additionally gets each bonus line compared
                    // against whatever's currently equipped in that slot.
                    if (record.InventoryItem is Equipment equipment)
                    {
                        Equipment equippedCounterpart = equipment switch
                        {
                            Weapon => Player.Instance.Weapon,
                            Armor => Player.Instance.Armor,
                            Ring => Player.Instance.Ring,
                            AbilityItem => Player.Instance.AbilityItem,
                            _ => null,
                        };

                        List<(string Text, bool Better)> lines = equippedCounterpart != null
                            ? equipment.ComparisonLines(equippedCounterpart)
                            : new List<(string Text, bool Better)> { (equipment.TooltipText(), false) };

                        int textY = (int)(lines.Count * Art.HudFont.LineSpacing / 2);

                        Util.DrawTooltip(
                            spriteBatch,
                            Art.HudFont,
                            lines,
                            new Vector2(posX, posY - (record.InventoryItem.image.Height * 2) - textY),
                            Color.Red,
                            Color.DarkGreen
                        );
                    }
                    else
                    {
                        string text = record.InventoryItem.Name;
                        int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                        Util.DrawTooltip(
                            spriteBatch,
                            Art.HudFont,
                            text,
                            new Vector2(posX, posY - (record.InventoryItem.image.Height * 2) - textY),
                            Color.Red
                        );
                    }
                }
            }
        }

        // Drawn separately from — and after — Draw() so a drag toward the
        // player's inventory (or held over the bank itself) isn't covered up
        // by whichever panel's own contents draw later in NexusState's order.
        public static void DrawDragGhost(SpriteBatch spriteBatch)
        {
            if (!IsOpen || !dragItem)
                return;

            Point anchor = Anchor;
            Texture2D draggedImage = Records[bankSlot].InventoryItem.image;

            int slotPosX = anchor.X + ((bankSlot % ColumnsPerRow) * 40);
            int slotPosY = anchor.Y + ((bankSlot / ColumnsPerRow) * 40);

            var originalSlotBounds = new Rectangle(
                slotPosX,
                slotPosY,
                draggedImage.Width,
                draggedImage.Height
            );

            // Only draw the drag ghost once the cursor has actually left the
            // slot it came from — same rule as InventorySystem's inventory
            // drag ghost.
            if (!originalSlotBounds.Intersects(Input.MouseBounds))
            {
                spriteBatch.Draw(draggedImage, Input.MousePosition, Color.White * 0.5f);
            }
        }
    }
}
