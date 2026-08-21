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
}
