using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    public class InventorySystem
    {
        private static InventorySystem instance;
        public static InventorySystem Instance
        {
            get
            {
                if (instance == null)
                    instance = new InventorySystem();
                return instance;
            }
        }

        private const int MAXIMUM_SLOTS_IN_INVENTORY = 10;
        public readonly List<InventoryRecord> InventoryRecords = new List<InventoryRecord>();

        //public InventorySystem()
        //{
        //    Debug.WriteLine("InventorySystem()");
        //    instance = this;
        //    //AddItem(new Potion(), 1);
        //    //AddItem(new Potion(), 1);
        //}

        public void AddItem(Item item, int quantityToAdd)
        {
            Debug.WriteLine("Add: " + quantityToAdd);
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
                    InventoryRecord inventoryRecord = InventoryRecords.First(x =>
                        (x.InventoryItem.ID == item.ID)
                        && (x.Quantity < item.MaximumStackableQuantity)
                    );
                    // Calculate how many more can be added to this stack
                    int maximumQuantityYouCanAddToThisStack = (
                        item.MaximumStackableQuantity - inventoryRecord.Quantity
                    );
                    // Add to the stack (either the full quanity, or the amount that would make it reach the stack maximum)
                    int quantityToAddToStack = Math.Min(
                        quantityToAdd,
                        maximumQuantityYouCanAddToThisStack
                    );
                    inventoryRecord.AddToQuantity(quantityToAddToStack);
                    // Decrease the quantityToAdd by the amount we added to the stack.
                    // If we added the total quantityToAdd to the stack, then this value will be 0, and we'll exit the 'while' loop.
                    quantityToAdd -= quantityToAddToStack;
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
                        throw new Exception("There is no more space in the inventory");
                    }
                }
            }
        }

        public class InventoryRecord
        {
            public Item InventoryItem { get; private set; }
            public int Quantity { get; private set; }

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

        public void Draw(SpriteBatch spriteBatch) { }
    }
}
