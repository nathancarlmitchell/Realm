using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // Shared infrastructure for every boss — abstract since a bare "Boss"
    // has no identity/art/attack pattern of its own; each real boss (e.g.
    // LimonTheSpriteGoddess) is its own concrete subclass. Only what's
    // genuinely common to every boss lives here; a boss-specific attack
    // pattern or stat belongs on that boss's own subclass instead.
    abstract class Boss : Enemy
    {
        // Display identity — set by each concrete boss's constructor.
        public string Name { get; protected set; }

        // Flavor/lore text — set by each concrete boss's constructor. Not
        // rendered anywhere yet (no boss lore UI exists today), stored for
        // whenever one is built, same status as Portal.Destination.
        // BossDestination's own BossName field.
        public string Description { get; protected set; }

        // Public read-only views onto Enemy's protected health fields —
        // Enemy hides these to keep them encapsulated from everything else,
        // but the boss arena's HUD (BossRealmState.DrawBossHud()) needs them
        // to draw a real name+health bar instead of the small per-enemy one.
        public int Health => health;
        public int HealthMax => healthMax;

        protected Boss(Texture2D image, Vector2 position)
            : base(image, position) { }

        // Every boss drops something good, unlike the normal random-chance
        // table every other enemy uses (Enemy.SpawnLoot()). Still threads
        // DropPool through (inherited from Enemy, defaults to All so no
        // existing boss's drops change) — a future boss wanting a themed
        // guaranteed-loot pool (e.g. never drops rings) can set it in its
        // own constructor exactly like a regular enemy's factory would.
        protected override void SpawnLoot(List<Item> extraItems = null)
        {
            ItemSpawner.SpawnGuaranteedLoot(
                this.Position,
                PointValue,
                DropPool,
                DropTierRanges,
                StatPotionPool,
                GuaranteedPotionChances,
                extraItems
            );
        }

        // No-op: a boss's health is shown by the dedicated top-of-screen bar
        // (BossRealmState.DrawBossHud()) instead of the small floating bar
        // every other enemy draws over its own sprite — drawing both would
        // be redundant.
        public override void DrawHealthBars(SpriteBatch spriteBatch) { }

        // Every boss guarantees a way straight back to the open Realm (not
        // Nexus — see Portal.Destination.Realm/RealmDestination), dropped
        // one tile above wherever it actually died. Separate from
        // BossRealmState's own exit portal (dropped at arena-entry time,
        // near the player's start position, unconditionally and regardless
        // of whether/where the boss ends up dying) — this one is guaranteed
        // by the kill itself, right at the fight's own end point, same
        // "one tile" convention every other world-unit conversion in this
        // codebase uses (32px). Requested directly.
        protected override void OnDeath()
        {
            Portal.DroppedPortals.Add(
                new Portal(Position - new Vector2(0, 32), Portal.Destination.Realm)
            );
        }
    }
}
