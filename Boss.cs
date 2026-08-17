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
        // Display identity — set by each concrete boss's constructor. Not
        // shown anywhere in the UI yet (no boss name/health-bar UI exists),
        // but this is what "give the boss a name" means at the code level,
        // ready for whichever future UI wants it.
        public string Name { get; protected set; }

        protected Boss(Texture2D image, Vector2 position)
            : base(image, position) { }

        // Every boss drops something good, unlike the normal random-chance
        // table every other enemy uses (Enemy.SpawnLoot()).
        protected override void SpawnLoot()
        {
            ItemSpawner.SpawnGuaranteedLoot(this.Position);
        }
    }
}
