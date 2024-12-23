using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    public class InventorySystem
    {
        public const int MAXIMUM_SLOTS_IN_INVENTORY = 6;
        public readonly List<InventoryRecord> InventoryRecords = [];

        public void AddItem(Item item, int quantityToAdd)
        {
            Debug.WriteLine("Add: " + quantityToAdd);
            Sound.Play(Sound.InventoryMoveItem, 0.5f);
            while (quantityToAdd > 0)
            {
                // If an object of this item type already exists in the inventory, and has room to stack more items,
                // then add as many as we can to that stack.
                if (
                    InventoryRecords.Exists(x =>
                        (x.InventoryItem.ID == item.ID)
                        && (x.Quantity < item.MaximumStackableQuantity)
                    )
                )
                {
                    Debug.WriteLine("InventoryRecords.Exists");
                    // Get the item we're going to add quantity to
                    InventoryRecord inventoryRecord = InventoryRecords.FirstOrDefault(x =>
                        (x.InventoryItem.ID == item.ID)
                    );
                    if (inventoryRecord != null) { }
                    else
                    {
                        Debug.WriteLine("Null");
                    }
                    // Calculate how many more can be added to this stack
                    //int maximumQuantityYouCanAddToThisStack = (
                    //    item.MaximumStackableQuantity - inventoryRecord.Quantity
                    //);
                    //// Add to the stack (either the full quanity, or the amount that would make it reach the stack maximum)
                    //int quantityToAddToStack = Math.Min(
                    //    quantityToAdd,
                    //    maximumQuantityYouCanAddToThisStack
                    //);
                    if (inventoryRecord.Quantity < item.MaximumStackableQuantity)
                    {
                        int quantityToAddToStack = 1;
                        inventoryRecord.AddToQuantity(quantityToAddToStack);
                        // Decrease the quantityToAdd by the amount we added to the stack.
                        // If we added the total quantityToAdd to the stack, then this value will be 0, and we'll exit the 'while' loop.
                        quantityToAdd -= quantityToAddToStack;
                    }
                    else
                    {
                        quantityToAdd = 0;
                    }
                }
                else
                {
                    Debug.WriteLine("InventoryRecords.NotExists");
                    // We don't already have an existing inventoryRecord for this ObtainableItem object,
                    // so, add one to the list, if there is room.
                    if (InventoryRecords.Count < MAXIMUM_SLOTS_IN_INVENTORY)
                    {
                        // Don't set the quantity value here.
                        // The 'while' loop will take us back to the code above, which will add to the quantity.
                        InventoryRecords.Add(new InventoryRecord(item, 0));
                    }
                    else
                    {
                        // Throw an exception, or somehow let the user know they are out of inventory space.
                        // This exception here is just a quick example. Do something better in your code.
                        Debug.WriteLine("There is no more space in the inventory");
                        return;
                    }
                }
            }
        }

        public bool CanStack(Item item, int quantityToAdd)
        {
            // Return true if item exisits in inventory and can stack
            return InventoryRecords.Exists(x =>
                (x.InventoryItem.ID == item.ID) && (x.Quantity < item.MaximumStackableQuantity)
            );
        }

        public bool HasRoom()
        {
            return Player.Instance.Inventory.InventoryRecords.Count
                < InventorySystem.MAXIMUM_SLOTS_IN_INVENTORY;
        }

        public void DropItem(string name)
        {
            for (int i = 0; i < InventoryRecords.Count; i++)
            {
                InventoryRecord record = InventoryRecords[i];
                if (record.InventoryItem != null)
                {
                    if (record.InventoryItem.Name == name && record.Quantity > 0)
                    {
                        record.Quantity--;
                        if (record.Quantity <= 0)
                        {
                            InventoryRecords.RemoveAt(i);
                            return;
                        }
                    }
                }
            }
        }

        // This should really be called UseItem()
        public void UsePotion(string name)
        {
            for (int i = 0; i < InventoryRecords.Count; i++)
            {
                InventoryRecord record = InventoryRecords[i];
                if (record.InventoryItem != null)
                {
                    if (record.InventoryItem.Name == name && record.Quantity > 0)
                    {
                        switch (record.InventoryItem.Name)
                        {
                            case "Health Potion":
                                if (Player.Health >= Player.HealthMax)
                                    return;
                                Potion.Use(Potions.Health);
                                break;

                            case "Mana Potion":
                                if (Player.Mana >= Player.ManaMax)
                                    return;
                                Potion.Use(Potions.Mana);
                                break;

                            case "Attack Potion":
                                if (Player.Attack >= Player.MaxAttack)
                                    return;
                                Potion.Use(Potions.Attack);
                                break;

                            case "Defense Potion":
                                if (Player.Defense >= Player.MaxDefense)
                                    return;
                                Potion.Use(Potions.Defense);
                                break;

                            case "Speed Potion":
                                if (Player.Speed >= Player.MaxSpeed)
                                    return;
                                Potion.Use(Potions.Speed);
                                break;

                            case "Dexterity Potion":
                                if (Player.Dexterity >= Player.MaxDexterity)
                                    return;
                                Potion.Use(Potions.Dexterity);
                                break;

                            case "Vitality Potion":
                                if (Player.Vitality >= Player.MaxVitality)
                                    return;
                                Potion.Use(Potions.Vitality);
                                break;

                            case "Wisdom Potion":
                                if (Player.Wisdom >= Player.MaxWisdom)
                                    return;
                                Potion.Use(Potions.Wisdom);
                                break;

                            case "Life Potion":
                                if (Player.HealthMax >= Player.MaxHealth)
                                    return;
                                Potion.Use(Potions.Life);
                                break;

                            case "ManaMax Potion":
                                if (Player.ManaMax >= Player.MaxMana)
                                    return;
                                Potion.Use(Potions.ManaMax);
                                break;
                        }

                        record.Quantity--;
                        if (record.Quantity <= 0)
                        {
                            InventoryRecords.RemoveAt(i);
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
            public Item InventoryItem { get; private set; }
            public int Quantity { get; set; }

            public InventoryRecord(Item item, int quantity)
            {
                InventoryItem = item;
                Quantity = quantity;
            }

            public void AddToQuantity(int amountToAdd)
            {
                Quantity += amountToAdd;
            }
        }

        int x = Game1.Viewport.Width - 256;
        int y = Game1.Viewport.Height - 128;

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw inventory border.
            spriteBatch.Draw(Art.Inventory, new Vector2(x, y), Color.White);

            // Loop through items in inventory.
            for (int i = 0; i < InventoryRecords.Count; i++)
            {
                InventoryRecord record = InventoryRecords[i];
                if (record.InventoryItem != null)
                {
                    spriteBatch.Draw(
                        record.InventoryItem.image,
                        new Vector2(x + (i * 40), y),
                        Color.White
                    );

                    // Draw quantity if max stack is greater than 1.
                    string text = string.Empty;
                    if (record.InventoryItem.MaximumStackableQuantity > 1)
                    {
                        text = "" + record.Quantity;
                    }

                    spriteBatch.DrawString(
                        Art.HudFont,
                        text,
                        new Vector2(x + (i * 40) + 4, y),
                        Color.Black
                    );
                }

                var bounds = new Rectangle(
                    x + (i * 40),
                    y,
                    record.InventoryItem.image.Width,
                    record.InventoryItem.image.Height
                );

                // Mouse over inventory item.
                if (bounds.Intersects(Input.MouseBounds))
                {
                    string text = $"{record.InventoryItem.Name}";

                    int textX = (int)(Art.HudFont.MeasureString(text).X / 2);
                    int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                    spriteBatch.DrawString(
                        Art.HudFont,
                        text,
                        new Vector2(x - textX, y - record.InventoryItem.image.Height - textY),
                        Color.Red
                    );

                    // Drop / use item if clicked.
                    if (Input.GetMouseClick())
                    {
                        if (record.InventoryItem is Potion)
                        {
                            // Use potion.
                            Debug.WriteLine(record.InventoryItem.Name);
                            UsePotion(record.InventoryItem.Name);
                            return;
                        }

                        // Drop item.
                        for (int x = 0; x < ItemSpawner.LootBags.Count; x++)
                        {
                            // Add item to exisiting bag
                            if (Player.Instance.Bounds.Intersects(ItemSpawner.LootBags[x].Bounds))
                            {
                                ItemSpawner.LootBags[x].Add(record.InventoryItem);
                                DropItem(record.InventoryItem.Name);
                                return;
                            }
                        }

                        Debug.WriteLine(record.InventoryItem);
                        List<Item> items = [record.InventoryItem];
                        LootBag bag = new() { Position = Player.Instance.Position, Items = items };
                        ItemSpawner.LootBags.Add(bag);
                        EntityManager.Add(bag);
                        DropItem(record.InventoryItem.Name);
                    }
                }
            }
        }
    }
}
