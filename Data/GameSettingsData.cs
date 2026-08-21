namespace Realm.Data;

// Account-wide, not per-class — same reasoning as KeyBindingsData: a
// general settings store separate from any one character's save. Holds
// non-keybinding settings (KeyBindingsData.cs stays scoped to just
// bindings) — a ready home for whatever future setting gets picked up
// next.
public class GameSettingsData
{
    public bool AutoFireEnabled { get; set; }

    // Defaults to false (off) — bypasses Portal's confirm-before-
    // teleporting prompt entirely when true. See Portal.cs's Update().
    public bool AutoEnterPortalsEnabled { get; set; }

    // Defaults to false (off) — shows collision hitbox outlines
    // independent of the F3 debug HUD. See RealmState/NexusState.Draw().
    public bool ShowHitboxesEnabled { get; set; }

    // Defaults to TRUE (on) — unlike every other setting above, this one
    // warns about something urgent, so it should already be doing its job
    // for anyone who's never touched it. The explicit `= true` here is
    // required, not just documentation: System.Text.Json only overwrites
    // properties actually present in the JSON, so an existing
    // GameSettingsData.json saved before this field existed will
    // deserialize with this property left at its declared default —
    // without `= true` that default would silently be `false`, flipping
    // the setting off for every existing account instead of leaving it on.
    // See Player.cs's flash logic and SettingsState.cs's Graphics tab.
    public bool LowHealthIndicatorEnabled { get; set; } = true;
}
