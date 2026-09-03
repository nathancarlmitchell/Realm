using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // The Snake Pit Treasure Room's own trigger — modeled directly on
    // BeachBeacon.cs (a plain Entity, inert until the player walks within
    // Radius, activates once and stays activated). Unlike BeachBeacon, this
    // fires a callback on activation (TreasureRoomController.cs passes one
    // in to spring the ambush) rather than just flipping its own
    // IsActivated flag for something else to poll later — there's no
    // per-frame poller for this one, just a one-shot event.
    //
    // No dedicated "red button" art was supplied, so this reuses Art.
    // HealthBar tinted red and drawn small — same placeholder-art
    // precedent Cube God's own "cube system" already established.
    public class TreasureRoomButton : Entity
    {
        private readonly Action onActivated;
        private bool activated;

        public TreasureRoomButton(Vector2 position, Action onActivated)
        {
            this.onActivated = onActivated;
            image = Art.HealthBar;
            Position = position;
            drawScale = 12f;
            Radius = 16f;
            color = Color.Red;
        }

        public override void Update()
        {
            if (
                !activated
                && Vector2.DistanceSquared(Position, Player.Instance.Position) <= Radius * Radius
            )
            {
                activated = true;
                color = Color.DarkRed; // visually "pressed"
                Sound.Play(Sound.LootAppears, 0.4f);
                onActivated();
            }
        }
    }
}
