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
        protected override void SpawnLoot()
        {
            ItemSpawner.SpawnGuaranteedLoot(this.Position, PointValue, DropPool, DropTierRange);
        }

        // No-op: a boss's health is shown by the dedicated top-of-screen bar
        // (BossRealmState.DrawBossHud()) instead of the small floating bar
        // every other enemy draws over its own sprite — drawing both would
        // be redundant.
        public override void DrawHealthBars(SpriteBatch spriteBatch) { }
    }
}
