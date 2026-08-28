using Microsoft.Xna.Framework;

namespace Realm
{
    // A single landmark crystal, spawned once per Realm instance at a
    // random point within the Beach biome ring (see
    // States/RealmState.cs's constructor, which rolls the actual
    // position) — Beach is always the innermost ring (Data/BiomeData.json:
    // MinDistance 0), so every regular dungeon entry gets exactly one.
    // Inert (dim) until the player physically walks within Radius of it
    // (the same Radius used for the F3 debug-hitbox circle — see the
    // constructor); once activated (permanently, for the rest of this
    // Realm instance), clicking the minimap teleports the player straight
    // to it — see Overlay.DrawMinimap()'s click handling.
    //
    // A plain Entity rather than a Portal (no animation, no Destination to
    // route through) or an Enemy (no combat/loot) — it just needs to sit
    // in the world, get spotted, and be clickable-from-the-map afterward,
    // so it rides the normal EntityManager Update()/Draw() pipeline like
    // everything else and nothing more.
    public class BeachBeacon : Entity
    {
        // The Beacon for the current Realm instance, if any. Read by
        // Overlay's minimap (both for the blip and the click-to-teleport
        // check). ActiveInstance (not the raw field) is what callers
        // outside this class should use — see its own comment below.
        private static BeachBeacon instance;

        // Filters out a stale reference automatically: EntityManager.Reset()
        // (called by every state transition — NexusState/BossRealmState/
        // RealmState's own constructor, and StateManager's various exits)
        // marks every non-Player entity IsExpired the moment the player
        // leaves this Realm instance, so a Beacon from a previous dungeon
        // can never leak into the Nexus's or a different dungeon's
        // minimap — no extra per-state Reset() call needed to remember.
        public static BeachBeacon ActiveInstance =>
            instance != null && !instance.IsExpired ? instance : null;

        public bool IsActivated { get; private set; }

        public BeachBeacon(Vector2 position)
        {
            image = Art.BeachBeacon;
            Position = position;

            // Also doubles as the activation distance below (Update()) —
            // a single source of truth rather than two numbers that could
            // drift out of sync, per the user's own direct request.
            // Previously a separate ActivationRadius const (3 tiles = 96);
            // Radius is currently image.Width * 2f, which lands in
            // roughly the same range, so this doesn't meaningfully change
            // today's activation distance. Worth keeping in mind going
            // forward though: Radius also drives the F3 debug-hitbox
            // circle's size, so changing it for debug-visibility reasons
            // alone would now silently change the real activation range
            // too.
            Radius = image.Width * 2f;

            // Dim while inert — the source art has only the one sprite, no
            // separate lit/unlit state, so a tint is the only way to show
            // the difference. Update() below switches this to White the
            // instant it activates.
            color = Color.Gray;

            instance = this;
        }

        public override void Update()
        {
            if (
                !IsActivated
                && Vector2.DistanceSquared(Position, Player.Instance.Position) <= Radius * Radius
            )
            {
                IsActivated = true;
                color = Color.White;

                // Reused rather than adding a new sound asset for this —
                // LootAppears is already this game's generic "something
                // notable just happened" cue (loot drops), a better fit
                // here than e.g. Sound.LevelUp's much bigger, specifically
                // level-up-flavored moment.
                Sound.Play(Sound.LootAppears, 0.4f);
            }
        }
    }
}
