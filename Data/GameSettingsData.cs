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

    // Defaults to TRUE (on) — without `= true` that default would silently
    // be `false` for an old save missing this key, flipping the setting
    // off instead of leaving it on. Gates Overlay.DrawBeaconIndicator().
    public bool ShowQuestIndicatorEnabled { get; set; } = true;

    // Defaults to TRUE (on), same "give it its own explicit default"
    // reasoning as ShowQuestIndicatorEnabled just above. Gates
    // Equipment.DrawTierLabel()'s "T{Tier}" overlay on equipment icons.
    public bool DisplayItemTiersEnabled { get; set; } = true;

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

    // 0-100, defaults to 25 — same "give it its own explicit default"
    // reasoning as LowHealthIndicatorEnabled just above: an old
    // GameSettingsData.json missing this key would otherwise deserialize
    // it at the unstated default (0 for a bare int), which would silently
    // disable the flash/bar entirely (Health < HealthMax * 0% is never
    // true) instead of leaving it at the intended 25%.
    public int LowHealthThresholdPercent { get; set; } = 25;

    // Defaults to FALSE (off) — no explicit default needed, `false` is
    // already the bare-bool default for an old file missing this key.
    // Gates whether Player.DrawHealthBar() shows outside of combat too;
    // see that method's own comment. See SettingsState.cs's Graphics tab.
    public bool AlwaysDisplayPlayerHPEnabled { get; set; }

    // Defaults to TRUE (on) — same "give it its own explicit default"
    // reasoning as LowHealthIndicatorEnabled above. Gates the floating
    // "+XP" number spawned in Enemy.WasShot()'s death branch. See
    // SettingsState.cs's Graphics tab.
    public bool ShowXpDropsEnabled { get; set; } = true;

    // Defaults to false (off) — unlike the toggles above, false IS the
    // correct fallback for an old settings file missing this key, so no
    // explicit `= true` is needed here. Gates the same floating "+XP"
    // number as ShowXpDropsEnabled above, but only once the player reaches
    // Level 20 (see Enemy.WasShot()). See SettingsState.cs's Graphics tab.
    public bool AlwaysShowExpEnabled { get; set; }

    // Defaults to TRUE (on) — same "give it its own explicit default"
    // reasoning as LowHealthIndicatorEnabled above. Gates the player's own
    // "I took damage" number (Player.Hit()). See SettingsState.cs's
    // Graphics tab.
    public bool ShowPlayerDamageNumbersEnabled { get; set; } = true;

    // Defaults to TRUE (on) — same reasoning. Gates the hit number shown
    // over an enemy when the player damages it (Enemy.WasShot()).
    public bool ShowEnemyDamageNumbersEnabled { get; set; } = true;

    // Defaults to TRUE (on) — same reasoning. Gates Enemy.WasShot()'s two
    // Particle.SpawnBurst() calls (hit/death), not Player.LevelUp()'s
    // separate gold swirl.
    public bool ShowHitParticlesEnabled { get; set; } = true;

    // Defaults to TRUE (on) — same reasoning. Gates only the yellow
    // in-combat border around the sidebar HP bar; the sword icon itself
    // always shows. See Overlay.cs's DrawCombatIndicator().
    public bool ShowCombatIndicatorEnabled { get; set; } = true;

    // Audio — see Sound.cs's RefreshMusicState()/ShouldPlaySfx() and
    // SettingsState.cs's Audio tab. MusicEnabled/MusicVolumePercent/
    // SfxVolumePercent all need their own explicit defaults for the same
    // reason as LowHealthIndicatorEnabled/LowHealthThresholdPercent above
    // — an old GameSettingsData.json missing these keys must not silently
    // deserialize to "music off"/"everything silent" instead of the real
    // intended defaults. The three *Muted flags default correctly to
    // false either way, so they don't need one.
    public bool MusicEnabled { get; set; } = true;
    public int MusicVolumePercent { get; set; } = 25; // matches the volume Sound.cs already hardcoded before this setting existed
    public bool MusicMuted { get; set; }
    public int SfxVolumePercent { get; set; } = 100; // 100% preserves every existing Sound.Play() call's own tuned volume unchanged until the user turns it down
    public bool SfxMuted { get; set; }
    public bool WeaponShotsMuted { get; set; }
}
