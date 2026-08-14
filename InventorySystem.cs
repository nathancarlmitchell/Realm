using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

// https://scottlilly.com/how-to-build-stackable-inventory-for-a-game-in-c/

namespace Realm
{
    public class InventorySystem
    {
        public const int MAXIMUM_SLOTS_IN_INVENTORY = 8;
        public const int MaxPotionCharges = 6;

        // Fixed-size, not a compact list — index i is always slot i, so an
        // empty slot (null) stays where it is rather than everything after it
        // sliding down to fill the gap.
        public readonly InventoryRecord[] InventoryRecords = new InventoryRecord[
            MAXIMUM_SLOTS_IN_INVENTORY
        ];

        // Below the equipment row in the sidebar. PotionChargeBounds sits
        // 48px above this (y - 48) for the potion quick-slots — comfortably
        // clear of the equipment row above (which ends at y=450).
        int x = Game1.SidebarX + 20;
        int y = 530;

        public Rectangle Bounds =>
            new(x, y, (40 * MAXIMUM_SLOTS_IN_INVENTORY) / 2, 40 * 2);

        public bool HasEmptySlot => Array.IndexOf(InventoryRecords, null) != -1;

        // Places into the first empty slot. Callers always check HasEmptySlot
        // first, so there's no full/error handling here.
        public void AddRecord(InventoryRecord record)
        {
            int slot = Array.IndexOf(InventoryRecords, null);
            if (slot != -1)
                InventoryRecords[slot] = record;
        }

        // The slot index a screen point falls within (using the same 4-wide,
        // 2-row layout Update()/Draw() already use), or -1 if the point isn't
        // over the grid at all.
        public int SlotIndexAt(Vector2 point)
        {
            int col = (int)((point.X - x) / 40);
            int row = (int)((point.Y - y) / 40);

            if (col < 0 || col > 3 || row < 0 || row > 1)
                return -1;

            return (row * 4) + col;
        }

        // Places at the given slot if it's empty — used when a drop should
        // land wherever the cursor actually is, e.g. dragging in from the
        // bank or an equip slot — falling back to the first empty slot if the
        // target is out of range or already occupied (same as AddRecord).
        public void AddRecordAt(int slot, InventoryRecord record)
        {
            if (slot >= 0 && slot < InventoryRecords.Length && InventoryRecords[slot] == null)
                InventoryRecords[slot] = record;
            else
                AddRecord(record);
        }

        // Finds record by reference and nulls that slot — no shifting, unlike
        // List.Remove. Returns false if the record isn't actually in here.
        public bool RemoveRecord(InventoryRecord record)
        {
            int slot = Array.IndexOf(InventoryRecords, record);
            if (slot == -1)
                return false;

            InventoryRecords[slot] = null;
            return true;
        }

        private int healthPotionCharges = 0;
        private int manaPotionCharges = 0;

        public int HealthPotionCharges
        {
            get => healthPotionCharges;
            set => healthPotionCharges = Math.Clamp(value, 0, MaxPotionCharges);
        }

        public int ManaPotionCharges
        {
            get => manaPotionCharges;
            set => manaPotionCharges = Math.Clamp(value, 0, MaxPotionCharges);
        }

        // Returns false (and leaves the item untouched) if the pickup couldn't be
        // completed — either a potion charge stack is already full, or there's no
        // free slot for a new stack/item. Callers must check this before treating
        // the item as consumed.
        public bool AddItem(Item item, int quantityToAdd)
        {
            // Health/Mana potions don't live in InventoryRecords — they have their
            // own dedicated charge counters, drawn above the inventory panel.
            if (item.Name == "Health Potion")
                return AddPotionCharge(ref healthPotionCharges);

            if (item.Name == "Mana Potion")
                return AddPotionCharge(ref manaPotionCharges);

            while (quantityToAdd > 0)
            {
                InventoryRecord recordWithRoom = InventoryRecords.FirstOrDefault(x =>
                    x != null
                    && x.InventoryItem.Name == item.Name
                    && x.Quantity < item.MaximumStackableQuantity
                );

                if (recordWithRoom != null)
                {
                    recordWithRoom.AddToQuantity(1);
                    quantityToAdd--;
                    continue;
                }

                if (HasEmptySlot)
                {
                    // Don't set the quantity value here — the loop comes back
                    // around and adds to it via the branch above.
                    AddRecord(new InventoryRecord(item, 0));
                }
                else
                {
                    Sound.Play(Sound.Error, 0.4f);
                    return false;
                }
            }

            Sound.Play(Sound.InventoryMoveItem, 0.5f);
            return true;
        }

        private bool AddPotionCharge(ref int charges)
        {
            if (charges >= MaxPotionCharges)
            {
                Sound.Play(Sound.Error, 0.4f);
                return false;
            }

            charges++;
            Sound.Play(Sound.InventoryMoveItem, 0.5f);
            return true;
        }

        // Read-only check mirroring AddItem's success condition, without any of its
        // side effects (mutation, sound). Lets a caller decide up front whether a
        // pickup will succeed — e.g. LootBag falls back to using a consumable in
        // place instead of picking it up when this is false.
        public bool CanAddItem(Item item)
        {
            if (item.Name == "Health Potion")
                return healthPotionCharges < MaxPotionCharges;

            if (item.Name == "Mana Potion")
                return manaPotionCharges < MaxPotionCharges;

            bool hasRoomInExistingStack = InventoryRecords.Any(x =>
                x != null
                && x.InventoryItem.Name == item.Name
                && x.Quantity < item.MaximumStackableQuantity
            );

            return hasRoomInExistingStack || HasEmptySlot;
        }

        public void DropItem(InventoryRecord record)
        {
            // Act on this exact record, not a name-based search across the whole
            // inventory — two records can legitimately share a Name (e.g. two of
            // the same weapon picked up before either was equipped, since weapons
            // can't stack), and matching by name would touch both.
            record.Quantity--;
            if (record.Quantity <= 0)
            {
                RemoveRecord(record);
            }

            AddToLootBagAtPlayer(record.InventoryItem);
        }

        // Despite the name, this is where every "drop it on the ground" fallback
        // ends up — DropItem (dragging a general inventory item out) and
        // ResolveEquipmentDrag's own ground-drop branch both funnel through
        // here, so the bank redirect only needs to live in one place.
        private void AddToLootBagAtPlayer(Item item)
        {
            // Prefer banking over dropping on the ground while the bank is
            // open — but only if there's room; a full bank falls back to the
            // normal loot-bag behavior below rather than blocking the drop.
            // (Deliberately not applied to a drag released directly over the
            // bank panel — that path already has its own capacity guard that
            // blocks and keeps the item where it was, matching every other
            // "no room" case in this system.)
            if (BankSystem.IsOpen && BankSystem.HasEmptySlot)
            {
                BankSystem.AddRecord(new InventoryRecord(item, 1));
                Sound.Play(Sound.InventoryMoveItem, 0.5f);
                return;
            }

            for (int x = 0; x < ItemSpawner.LootBags.Count; x++)
            {
                // Add item to existing bag.
                if (Player.Instance.Bounds.Intersects(ItemSpawner.LootBags[x].Bounds))
                {
                    ItemSpawner.LootBags[x].Add(item);
                    return;
                }
            }

            List<Item> items = [item];
            LootBag bag = new() { Position = Player.Instance.Position, Items = items };
            ItemSpawner.LootBags.Add(bag);
            EntityManager.Add(bag);
        }

        // Applies a potion's effect by name, with no inventory bookkeeping (no
        // charge/quantity decrement, no sound) — the one place that knows what each
        // potion actually does. Shared by UsePotion (using one already held) and by
        // LootBag (drinking one off the ground in place, when it can't be picked
        // up). Returns false if the relevant stat is already maxed.
        public bool UsePotionEffect(string name)
        {
            switch (name)
            {
                case "Health Potion":
                    if (Player.Instance.Health >= Player.Instance.HealthMax)
                        return false;
                    Potion.Use(Potions.Health);
                    return true;

                case "Mana Potion":
                    if (Player.Instance.Mana >= Player.Instance.ManaMax)
                        return false;
                    Potion.Use(Potions.Mana);
                    return true;

                case "Attack Potion":
                    if (Player.Instance.Attack >= Player.Instance.MaxAttack)
                        return false;
                    Potion.Use(Potions.Attack);
                    return true;

                case "Defense Potion":
                    if (Player.Instance.Defense >= Player.Instance.MaxDefense)
                        return false;
                    Potion.Use(Potions.Defense);
                    return true;

                case "Speed Potion":
                    if (Player.Instance.Speed >= Player.Instance.MaxSpeed)
                        return false;
                    Potion.Use(Potions.Speed);
                    return true;

                case "Dexterity Potion":
                    if (Player.Instance.Dexterity >= Player.Instance.MaxDexterity)
                        return false;
                    Potion.Use(Potions.Dexterity);
                    return true;

                case "Vitality Potion":
                    if (Player.Instance.Vitality >= Player.Instance.MaxVitality)
                        return false;
                    Potion.Use(Potions.Vitality);
                    return true;

                case "Wisdom Potion":
                    if (Player.Instance.Wisdom >= Player.Instance.MaxWisdom)
                        return false;
                    Potion.Use(Potions.Wisdom);
                    return true;

                case "Life Potion":
                    if (Player.Instance.HealthMax >= Player.Instance.MaxHealth)
                        return false;
                    Potion.Use(Potions.Life);
                    return true;

                case "ManaMax Potion":
                    if (Player.Instance.ManaMax >= Player.Instance.MaxMana)
                        return false;
                    Potion.Use(Potions.ManaMax);
                    return true;

                default:
                    return false;
            }
        }

        // This should really be called UseItem()
        public void UsePotion(string name)
        {
            if (name == "Health Potion")
            {
                if (healthPotionCharges <= 0)
                {
                    Sound.Play(Sound.NoMana, 0.4f);
                    return;
                }
                if (!UsePotionEffect(name))
                    return;

                healthPotionCharges--;
                Sound.Play(Sound.UsePotion, 0.4f);
                return;
            }

            if (name == "Mana Potion")
            {
                if (manaPotionCharges <= 0)
                {
                    Sound.Play(Sound.NoMana, 0.4f);
                    return;
                }
                if (!UsePotionEffect(name))
                    return;

                manaPotionCharges--;
                Sound.Play(Sound.UsePotion, 0.4f);
                return;
            }

            for (int i = 0; i < InventoryRecords.Length; i++)
            {
                InventoryRecord record = InventoryRecords[i];
                if (record != null)
                {
                    if (record.InventoryItem.Name == name && record.Quantity > 0)
                    {
                        if (!UsePotionEffect(record.InventoryItem.Name))
                            return;

                        record.Quantity--;
                        if (record.Quantity <= 0)
                        {
                            InventoryRecords[i] = null;
                        }

                        Sound.Play(Sound.UsePotion, 0.4f);
                    }
                    else
                    {
                        Sound.Play(Sound.NoMana, 0.4f);
                    }
                }
            }
        }

        public class InventoryRecord
        {
            public Item InventoryItem { get; set; }

            public int Quantity { get; set; }

            public InventoryRecord(Item item, int quantity)
            {
                InventoryItem = item;
                Quantity = quantity;
                if (item.ImageName is not null)
                    item.image = Game1.Instance.Content.Load<Texture2D>(item.ImageName);
            }

            public void AddToQuantity(int amountToAdd)
            {
                Quantity += amountToAdd;
            }
        }

        private bool hover = false;
        private bool mousePressed = false;
        private bool dragItem = false;
        private int inventorySlot = 0;

        // Dragging an equipped item (Weapon/Armor/Ring) back out, as opposed to
        // dragItem above which is for items already sitting in InventoryRecords.
        private bool draggingEquipped = false;
        private Equipment draggedEquipment = null;

        // Bounds for the Health (column 0) / Mana (column 1) charge portraits,
        // drawn just above the inventory panel using the same 40px slot spacing.
        private Rectangle PotionChargeBounds(Texture2D texture, int column)
        {
            return new Rectangle(x + (column * 40), y - 48, texture.Width, texture.Height);
        }

        public void Update()
        {
            hover = false;

            // Potion charges are click-to-use quick slots, not draggable inventory
            // items — handle them before the drag state machine below.
            if (Input.GetMouseClick())
            {
                if (PotionChargeBounds(Art.HealthPotion, 0).Intersects(Input.MouseBounds))
                {
                    UsePotion("Health Potion");
                    return;
                }

                if (PotionChargeBounds(Art.ManaPotion, 1).Intersects(Input.MouseBounds))
                {
                    UsePotion("Mana Potion");
                    return;
                }
            }

            // Start dragging an equipped item out of its slot. Only a slot with
            // something in it is draggable — an empty slot has nothing to pick up.
            if (Input.MousePressed() && !mousePressed)
            {
                if (
                    Player.Instance.Weapon.IsEquipped
                    && Player.Instance.Weapon.SlotBounds.Intersects(Input.MouseBounds)
                )
                {
                    draggedEquipment = Player.Instance.Weapon;
                    draggingEquipped = true;
                    mousePressed = true;
                    return;
                }

                if (
                    Player.Instance.Armor.IsEquipped
                    && Player.Instance.Armor.SlotBounds.Intersects(Input.MouseBounds)
                )
                {
                    draggedEquipment = Player.Instance.Armor;
                    draggingEquipped = true;
                    mousePressed = true;
                    return;
                }

                if (
                    Player.Instance.Ring.IsEquipped
                    && Player.Instance.Ring.SlotBounds.Intersects(Input.MouseBounds)
                )
                {
                    draggedEquipment = Player.Instance.Ring;
                    draggingEquipped = true;
                    mousePressed = true;
                    return;
                }

                if (
                    Player.Instance.AbilityItem.IsEquipped
                    && Player.Instance.AbilityItem.SlotBounds.Intersects(Input.MouseBounds)
                )
                {
                    draggedEquipment = Player.Instance.AbilityItem;
                    draggingEquipped = true;
                    mousePressed = true;
                    return;
                }
            }

            for (int i = 0; i < InventoryRecords.Length; i++)
            {
                InventoryRecord record = InventoryRecords[i];
                if (record == null)
                    continue;

                int posX = x + (i * 40);
                int posY = y;

                if (i > 3)
                {
                    posX = x + ((i - 4) * 40);
                    posY = y + 40;
                }

                var bounds = new Rectangle(
                    posX,
                    posY,
                    record.InventoryItem.image.Width,
                    record.InventoryItem.image.Height
                );

                if (bounds.Intersects(Input.MouseBounds))
                {
                    hover = true;

                    // Start item drag. What happens next (consume, drop, equip)
                    // is decided on release, not here.
                    if (Input.MousePressed())
                    {
                        if (!mousePressed && hover)
                        {
                            inventorySlot = i;
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

            // Mouse released.
            if (Input.MouseReleased())
            {
                bool wasInteracting = dragItem || draggingEquipped;

                if (dragItem)
                {
                    InventoryRecord draggedRecord = InventoryRecords[inventorySlot];

                    // TryEquipFromRecord handles it (equipped, or bounced with
                    // an error sound on a class/type mismatch) if released over
                    // a matching equip slot — otherwise try the bank, then fall
                    // back to ground-drop/consumable-click.
                    if (!TryEquipFromRecord(InventoryRecords, draggedRecord))
                    {
                        if (BankSystem.IsOpen && BankSystem.Bounds.Intersects(Input.MouseBounds))
                        {
                            if (!BankSystem.HasEmptySlot)
                            {
                                Sound.Play(Sound.Error, 0.4f);
                            }
                            else
                            {
                                RemoveRecord(draggedRecord);
                                BankSystem.AddRecordAt(
                                    BankSystem.SlotIndexAt(Input.MousePosition),
                                    draggedRecord
                                );
                                Sound.Play(Sound.InventoryMoveItem, 0.5f);
                            }
                        }
                        else if (!Bounds.Intersects(Input.MouseBounds))
                        {
                            // Outside inventory bounds — drop it on the ground.
                            DropItem(draggedRecord);
                        }
                        else
                        {
                            int targetSlot = SlotIndexAt(Input.MousePosition);
                            if (targetSlot != -1 && targetSlot != inventorySlot)
                            {
                                // Dragged to a different slot within the same
                                // grid — move it there, swapping with whatever
                                // (if anything) already occupied that slot.
                                InventoryRecord occupant = InventoryRecords[targetSlot];
                                InventoryRecords[targetSlot] = draggedRecord;
                                InventoryRecords[inventorySlot] = occupant;
                                Sound.Play(Sound.InventoryMoveItem, 0.5f);
                            }
                            else if (BankSystem.IsOpen)
                            {
                                // A plain click (no drag) while the bank is
                                // open banks the item — takes priority over
                                // click-to-use below, so a potion clicked
                                // with the bank open gets banked rather than
                                // drunk; drag it out instead if that's what
                                // you want while at the bank.
                                if (!BankSystem.HasEmptySlot)
                                {
                                    Sound.Play(Sound.Error, 0.4f);
                                }
                                else
                                {
                                    RemoveRecord(draggedRecord);
                                    BankSystem.AddRecord(draggedRecord);
                                    Sound.Play(Sound.InventoryMoveItem, 0.5f);
                                }
                            }
                            else if (draggedRecord.InventoryItem.Consumable)
                            {
                                // Released back over its own slot without
                                // dragging it anywhere — treat it as a click
                                // and use it.
                                UsePotion(draggedRecord.InventoryItem.Name);
                            }
                        }
                    }
                }
                else if (draggingEquipped)
                {
                    ResolveEquipmentDrag(draggedEquipment);
                }

                // Persist immediately rather than waiting for the next
                // state-transition checkpoint — closing the game right after
                // moving something into or out of the bank (or equipping,
                // dropping, using a potion...) previously lost that change.
                // Saved unconditionally whenever a drag was actually in
                // progress, rather than tracked per-branch, so a future
                // branch added here can't accidentally forget to persist its
                // own mutation.
                if (wasInteracting)
                {
                    Util.SavePlayerData();
                    Util.SaveInventoryData();
                    Util.SaveBankData();
                }

                mousePressed = false;
                dragItem = false;
                draggingEquipped = false;
                draggedEquipment = null;
            }
        }

        // Resolves a drag-out from an equip slot: cancel if released back over
        // its own slot, move to the inventory grid if there's room, otherwise
        // drop it on the ground — then unequip either way (moved or dropped).
        private void ResolveEquipmentDrag(Equipment equipment)
        {
            if (equipment.SlotBounds.Intersects(Input.MouseBounds))
                return;

            // Never allow unequipping the only weapon the player can actually
            // use. Shooting and abilities both require a weapon, so with
            // nothing equippable to swap to, they'd have no way to fight for a
            // replacement — an unrecoverable soft lock short of dying on
            // purpose. A weapon of the wrong WeaponType (e.g. a Wand sitting in
            // an Archer's inventory) doesn't count — LoadWeapon would reject
            // equipping it anyway.
            bool hasUsableBackupWeapon = InventoryRecords.Any(x =>
                x != null && x.InventoryItem is Weapon w && w.Type == Player.Instance.WeaponType
            );
            if (equipment is Weapon && !hasUsableBackupWeapon)
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            // Released over a DIFFERENT equip slot than the one this item came
            // from (the early return above already handled "released back over
            // its own slot") — always the wrong type for that slot, since each
            // slot only ever accepts one Equipment subtype. Bounce with an
            // error instead of falling through to inventory/bank/ground.
            if (
                Player.Instance.Weapon.SlotBounds.Intersects(Input.MouseBounds)
                || Player.Instance.Armor.SlotBounds.Intersects(Input.MouseBounds)
                || Player.Instance.Ring.SlotBounds.Intersects(Input.MouseBounds)
                || Player.Instance.AbilityItem.SlotBounds.Intersects(Input.MouseBounds)
            )
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            if (Bounds.Intersects(Input.MouseBounds))
            {
                if (!HasEmptySlot)
                {
                    Sound.Play(Sound.Error, 0.4f);
                    return;
                }

                AddRecordAt(SlotIndexAt(Input.MousePosition), new InventoryRecord(equipment, 1));
                Sound.Play(Sound.InventoryMoveItem, 0.5f);
            }
            else if (BankSystem.IsOpen && BankSystem.Bounds.Intersects(Input.MouseBounds))
            {
                if (!BankSystem.HasEmptySlot)
                {
                    Sound.Play(Sound.Error, 0.4f);
                    return;
                }

                BankSystem.AddRecordAt(
                    BankSystem.SlotIndexAt(Input.MousePosition),
                    new InventoryRecord(equipment, 1)
                );
                Sound.Play(Sound.InventoryMoveItem, 0.5f);
            }
            else
            {
                AddToLootBagAtPlayer(equipment);
            }

            switch (equipment)
            {
                case Weapon:
                    Player.Instance.EquipWeapon(new Weapon());
                    break;
                case Armor:
                    Player.Instance.EquipArmor(new Armor());
                    break;
                case Ring:
                    Player.Instance.EquipRing(new Ring());
                    break;
                case AbilityItem:
                    Player.Instance.EquipAbilityItem(new AbilityItem());
                    break;
            }
        }

        // Attempts to equip whatever draggedRecord holds, if the release point
        // is over the matching equip slot — shared by InventorySystem's own
        // drag-release handling and BankSystem's, since both need identical
        // equip-swap semantics from a dragged record sitting in *some* list.
        // Returns false (no-op) if the record's item type doesn't match
        // whatever slot the mouse happens to be over, so callers know to try
        // something else (bank/ground/consumable) instead. A true return means
        // this was handled — either the item got equipped, or a Load*() call
        // bounced it with an error sound on a class/type mismatch; either way
        // the caller shouldn't do anything further with the record.
        public static bool TryEquipFromRecord(InventoryRecord[] source, InventoryRecord draggedRecord)
        {
            // Swap Weapon — only weapons can be equipped this way.
            if (
                draggedRecord.InventoryItem is Weapon
                && Input.MouseBounds.Intersects(Player.Instance.Weapon.SlotBounds)
            )
            {
                Weapon currentWeapon = Player.Instance.Weapon;
                Weapon newWeapon = Weapon.LoadWeapon(draggedRecord.InventoryItem.Name);

                if (newWeapon != null)
                {
                    // The slot may have been empty (unequipped) — only put
                    // something back in the record's spot if there was a real
                    // item there to swap out.
                    if (currentWeapon.IsEquipped)
                        draggedRecord.InventoryItem = currentWeapon;
                    else
                    {
                        int slot = Array.IndexOf(source, draggedRecord);
                        if (slot != -1)
                            source[slot] = null;
                    }
                }

                return true;
            }

            // Swap Armor — only armor can be equipped this way.
            if (
                draggedRecord.InventoryItem is Armor
                && Input.MouseBounds.Intersects(Player.Instance.Armor.SlotBounds)
            )
            {
                Armor currentArmor = Player.Instance.Armor;
                Armor newArmor = Armor.LoadArmor(draggedRecord.InventoryItem.Name);

                if (newArmor != null)
                {
                    if (currentArmor.IsEquipped)
                        draggedRecord.InventoryItem = currentArmor;
                    else
                    {
                        int slot = Array.IndexOf(source, draggedRecord);
                        if (slot != -1)
                            source[slot] = null;
                    }
                }

                return true;
            }

            // Swap Ring — only rings can be equipped this way.
            if (
                draggedRecord.InventoryItem is Ring
                && Input.MouseBounds.Intersects(Player.Instance.Ring.SlotBounds)
            )
            {
                Ring currentRing = Player.Instance.Ring;
                Ring newRing = Ring.LoadRing(draggedRecord.InventoryItem.Name);

                if (newRing != null)
                {
                    if (currentRing.IsEquipped)
                        draggedRecord.InventoryItem = currentRing;
                    else
                    {
                        int slot = Array.IndexOf(source, draggedRecord);
                        if (slot != -1)
                            source[slot] = null;
                    }
                }

                return true;
            }

            // Swap AbilityItem — only a Spell or Quiver can be equipped this
            // way; which factory to call depends on the dragged item's
            // concrete type (both are valid drag sources onto this one slot).
            if (
                draggedRecord.InventoryItem is AbilityItem
                && Input.MouseBounds.Intersects(Player.Instance.AbilityItem.SlotBounds)
            )
            {
                AbilityItem currentAbilityItem = Player.Instance.AbilityItem;
                AbilityItem newAbilityItem = draggedRecord.InventoryItem switch
                {
                    Spell s => Spell.LoadSpell(s.Name),
                    Quiver q => Quiver.LoadQuiver(q.Name),
                    _ => null,
                };

                if (newAbilityItem != null)
                {
                    if (currentAbilityItem.IsEquipped)
                        draggedRecord.InventoryItem = currentAbilityItem;
                    else
                    {
                        int slot = Array.IndexOf(source, draggedRecord);
                        if (slot != -1)
                            source[slot] = null;
                    }
                }

                return true;
            }

            // None of the type checks above matched, but the release point is
            // still over one of the four equip slots — the wrong item type
            // for that slot (e.g. a Weapon dragged onto the Armor slot).
            // Bounce with an error sound instead of falling through to
            // whatever the caller does next (bank/ground/consumable), which
            // would otherwise silently drop the item.
            if (
                Player.Instance.Weapon.SlotBounds.Intersects(Input.MouseBounds)
                || Player.Instance.Armor.SlotBounds.Intersects(Input.MouseBounds)
                || Player.Instance.Ring.SlotBounds.Intersects(Input.MouseBounds)
                || Player.Instance.AbilityItem.SlotBounds.Intersects(Input.MouseBounds)
            )
            {
                Sound.Play(Sound.Error, 0.4f);
                return true;
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw inventory border.
            spriteBatch.Draw(Art.Inventory, new Vector2(x, y), Color.White);

            DrawPotionCharge(spriteBatch, Art.HealthPotion, healthPotionCharges, 0);
            DrawPotionCharge(spriteBatch, Art.ManaPotion, manaPotionCharges, 1);

            // Loop through items in inventory — every slot, empty ones just
            // draw nothing, so removing an item leaves a visible gap at its
            // fixed position instead of everything after it sliding down.
            for (int i = 0; i < InventoryRecords.Length; i++)
            {
                InventoryRecord record = InventoryRecords[i];
                if (record == null)
                    continue;

                int posX = x + (i * 40);
                int posY = y;

                if (i > 3)
                {
                    posX = x + ((i - 4) * 40);
                    posY = y + 40;
                }

                spriteBatch.Draw(record.InventoryItem.image, new Vector2(posX, posY), Color.White);

                // Draw quantity if max stack is greater than 1.
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

                // Mouse over inventory item. Full Tier/name/description/bonuses
                // for equipment, same as hovering the equip slot or a bank item —
                // plain items (e.g. potions) just show their name, since they
                // have no Tier/bonuses to show.
                if (bounds.Intersects(Input.MouseBounds))
                {
                    string text = record.InventoryItem is Equipment equipment
                        ? equipment.TooltipText()
                        : record.InventoryItem.Name;

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

        // Drawn separately from — and after — Draw() so a drag can be dragged
        // over another panel (the bank) without that panel's own contents,
        // drawn later in NexusState's draw order, covering the ghost up.
        public void DrawDragGhost(SpriteBatch spriteBatch)
        {
            if (dragItem)
            {
                Texture2D draggedImage = InventoryRecords[inventorySlot].InventoryItem.image;

                int slotPosX = x + (inventorySlot * 40);
                int slotPosY = y;
                if (inventorySlot > 3)
                {
                    slotPosX = x + ((inventorySlot - 4) * 40);
                    slotPosY = y + 40;
                }

                var originalSlotBounds = new Rectangle(
                    slotPosX,
                    slotPosY,
                    draggedImage.Width,
                    draggedImage.Height
                );

                // Only draw the drag ghost once the cursor has actually left the
                // slot it came from — otherwise it renders right on top of the
                // item that's already drawn there.
                if (!originalSlotBounds.Intersects(Input.MouseBounds))
                {
                    spriteBatch.Draw(draggedImage, Input.MousePosition, Color.White * 0.5f);
                }
            }
            else if (draggingEquipped && draggedEquipment != null)
            {
                // Same rule as above — only draw once the cursor leaves the slot.
                if (!draggedEquipment.SlotBounds.Intersects(Input.MouseBounds))
                {
                    spriteBatch.Draw(draggedEquipment.image, Input.MousePosition, Color.White * 0.5f);
                }
            }
        }

        private void DrawPotionCharge(SpriteBatch spriteBatch, Texture2D texture, int charges, int column)
        {
            Rectangle bounds = PotionChargeBounds(texture, column);
            Color tint = charges > 0 ? Color.White : Color.White * 0.3f;

            spriteBatch.Draw(Art.Border, new Vector2(bounds.X, bounds.Y), Color.White);
            spriteBatch.Draw(texture, new Vector2(bounds.X, bounds.Y), tint);

            if (charges > 0)
            {
                spriteBatch.DrawString(
                    Art.HudFont,
                    charges.ToString(),
                    new Vector2(bounds.X + 4, bounds.Y),
                    Color.Black
                );
            }
        }
    }
}
