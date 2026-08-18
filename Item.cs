using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // Lets an Item-typed property (e.g. InventoryData.InventoryItem) round-trip a
    // stored Weapon faithfully instead of silently losing its Weapon-specific fields
    // (damage, tier, projectile) on save/reload. UnknownDerivedTypeHandling only
    // covers deserializing JSON with a missing/unrecognized discriminator (e.g. old
    // saves from before this existed) — it does NOT cover serializing an instance
    // whose runtime type isn't registered below. Every concrete Item subtype that
    // can actually end up in a save (Weapon, Potion) MUST be listed here, or saving
    // it throws NotSupportedException.
    [JsonPolymorphic(
        TypeDiscriminatorPropertyName = "$itemType",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
    )]
    [JsonDerivedType(typeof(Item), typeDiscriminator: "Item")]
    [JsonDerivedType(typeof(Weapon), typeDiscriminator: "Weapon")]
    [JsonDerivedType(typeof(Potion), typeDiscriminator: "Potion")]
    [JsonDerivedType(typeof(Armor), typeDiscriminator: "Armor")]
    [JsonDerivedType(typeof(Ring), typeDiscriminator: "Ring")]
    [JsonDerivedType(typeof(Spell), typeDiscriminator: "Spell")]
    [JsonDerivedType(typeof(Quiver), typeDiscriminator: "Quiver")]
    [JsonDerivedType(typeof(Shield), typeDiscriminator: "Shield")]
    public class Item : Entity
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // MonoGame doesn't populate Texture2D.Name from the content path it was
        // loaded from, so this must not fall back to reading image.Name — that was
        // returning null/empty for any item whose texture was already loaded,
        // which meant every saved inventory item lost its content path and came
        // back with a null image (and crashed the first thing that touched it).
        public string ImageName { get; set; }

        public int MaximumStackableQuantity { get; set; }
        public bool Consumable { get; set; } = false;
        public bool Hover;

        public Item()
        {
            MaximumStackableQuantity = 1;
            Hover = false;
        }

        public Item(Texture2D image)
        {
            this.image = image;
            this.Radius = image.Width / 2;

            MaximumStackableQuantity = 1;
            Hover = false;
        }

        public override void Update() { }
    }
}
