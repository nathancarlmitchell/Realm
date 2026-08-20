namespace Realm.Data;

// Account-wide, not per-class — same reasoning as FameData/BankData: key
// bindings are a player preference, not something tied to any one
// character's save. Each field is a serialized InputBinding ("Key:W" /
// "Mouse:Right") rather than a raw Keys value, since a binding can now be
// either a keyboard key or a mouse button — see InputBinding.Serialize().
public class KeyBindingsData
{
    public string MoveUp { get; set; }
    public string MoveDown { get; set; }
    public string MoveLeft { get; set; }
    public string MoveRight { get; set; }
    public string UseAbility { get; set; }
    public string UseHealthPotion { get; set; }
    public string UseManaPotion { get; set; }
    public string ReturnToNexus { get; set; }
    public string ToggleAutoFire { get; set; }
    public string ToggleMute { get; set; }
    public string ConfirmPortalEntry { get; set; }
}
