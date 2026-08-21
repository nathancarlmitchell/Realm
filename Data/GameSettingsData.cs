namespace Realm.Data;

// Account-wide, not per-class — same reasoning as KeyBindingsData: a
// general settings store separate from any one character's save. Holds
// non-keybinding settings (KeyBindingsData.cs stays scoped to just
// bindings); currently just AutoFireEnabled, but a ready home for
// whatever future setting gets picked up next.
public class GameSettingsData
{
    public bool AutoFireEnabled { get; set; }
}
