# Realm — Bug Fix Log

Chronological log of bugs found and fixed in Realm, oldest first. Companion docs:
[DEVLOG.md](DEVLOG.md) (features, not bugs) and [BACKLOG.md](BACKLOG.md) (what's still open).
Mirrored from this project's Claude Code memory notes so it travels with the repo instead of
staying on one machine — this file is the canonical copy; append new entries directly.

Running log of confirmed bugs found and fixed in Realm (top-down ARPG, C#/MonoGame,
`C:\Users\Nathan\Downloads\Realm-main-claude\Realm-main`). Oldest first, grouped by date, with a
`(HH:MM timezone)` timestamp — the real local time an entry was logged, not estimated — on each
entry from 2026-08-13 onward. Each entry is what the bug was and how it was fixed — not a full
changelog of features, just the fixes. Keep appending new entries as bugs are found and fixed in
future sessions, under a heading for that day. Entries 1-10 predate when this log started
tracking dates, and 11-20 predate when it started tracking time-of-day — real fixes, just no
recorded date/time for those individually; don't treat their grouping as meaning they all
happened at once.

## Before dated tracking (exact dates not recorded)

1. **Collection-modified crash in the dungeon draw loop.** `GameState.Draw()` iterated a live
   entity collection while something else was still modifying it, throwing
   `InvalidOperationException: Collection was modified`. Fixed by iterating a `.ToList()` snapshot
   instead of the live collection.
2. **Loot bags didn't consume correctly when the player's inventory was full.** Picking up a loot
   bag with a full inventory had broken "use in place" fallback logic. Fixed the consume-when-full
   handling in the loot bag pickup path.
3. **Re-equipping into an empty slot could insert a blank item into inventory.** The equip-swap
   logic always put whatever was "previously equipped" back into the inventory record being
   replaced, but once slots could be empty (after unequip-to-inventory was added), this could
   insert an imageless placeholder item, later crashing the draw loop. Fixed by checking
   `currentWeapon.IsEquipped` (etc.) before deciding to swap the old item back in vs. just removing
   the dragged record.
4. **`NullReferenceException` saving an unequipped item.** `System.Text.Json` serializes inherited
   public properties by default, and a blank (unequipped) equipped item's `Width`/`Height`
   (computed from a null `image`) threw during `SavePlayerData()`. Fixed by null-guarding
   `Width`/`Height` in `Entity.cs`.
5. **Stat bonuses (from potions and gear) were wiped on level-up.** First fix attempt added an
   `EarnedXxxBonus` accumulator alongside the direct stat mutation.
6. **That accumulator double-counted equipment bonuses on save/reload.** The accumulator approach
   from #5 wasn't reset on reload, so re-equipping the same saved gear added its bonus a second
   time. Fixed with a full redesign: persisted `PotionXxxBonus` fields (potion-only) plus
   live-summed `EquipmentXxxBonus` properties that read `Weapon/Armor/Ring.X` directly every time
   instead of accumulating, with `Level++` moved before `RecalculateStats()` and the stat formula
   changed to use `(Level - 1)` so construction, reload, and level-up all agree.
7. **Loot drops always resolved to Wizard-type gear (Wand/Robe), regardless of player class.**
   `ItemSpawner`'s weapon/armor drop logic used `FirstOrDefault(x => x.Tier == N)`, which
   deterministically matched whichever entry was listed first in the catalog JSON (Wands/Robes
   before Bows/Leather) — not actually randomized by class. Fixed via `Where(...).ToList()` +
   `rand.Next(count)` indexing so drops are genuinely random among all matching-tier candidates
   (still not filtered to the player's own class — wrong-class drops are intentional).
8. **`CS0104: 'Color' is ambiguous` compile error.** `Player.cs` had a leftover
   `using System.Drawing;` that collided with MonoGame's `Microsoft.Xna.Framework.Color` once a
   `Color.Red` reference was added for floating damage numbers. Fixed by fully qualifying
   `Microsoft.Xna.Framework.Color.Red`.
9. **Ability-readiness HUD bar clipped off the bottom of the screen.** Fixed by moving it above the
   experience bar and halving its height.
10. **Boot crash deserializing a save with an equipped Spell/Quiver.**
    `System.InvalidOperationException: Each parameter in the deserialization constructor... must
    bind to an object property` — `Spell`/`Quiver` only had a `Texture2D`-taking constructor, but
    `System.Text.Json` independently requires a parameterless constructor for deserialization.
    Fixed by adding `public Spell() { }` / `public Quiver() { }`, matching the existing
    Weapon/Armor/Ring/Potion convention.

## 2026-08-13

11. **Character Select's Delete link kept showing after actually deleting a character.**
    `DeleteCharacterData` preserves High Score by writing back a fresh default save rather than
    removing the file outright, so a simple "does a save file exist" check still saw a file after
    deletion. First fix attempt switched the check to "does this save show real progress"
    (`Level > 1 || ExperienceTotal > 0`).
12. **That progress-based check hid Delete for characters that had been played but had zero
    progress.** A character that was selected and entered but never leveled up or scored anything
    looked identical to a freshly-reset one under the #11 heuristic. Fixed by adding a dedicated
    `Player.HasBeenPlayed` / `PlayerData.HasBeenPlayed` flag, set the moment a character is
    actually entered (see #14) and — unlike High Score — reset to `false` whenever the save is
    wiped back to defaults, so it accurately tracks "is there anything here to delete."
13. **`HasBeenPlayed` briefly needed nullable/legacy-fallback handling for old saves**, since saves
    written before the field existed would deserialize it as `false` and incorrectly hide Delete
    for genuinely-played characters. Rather than maintain that complexity, the old save files were
    simply deleted and the field kept as a plain non-nullable `bool` — no real users' data needed
    preserving.
14. **The main menu's "Nexus" button silently auto-started a Wizard when no character had ever been
    played**, instead of sending the player to Character Select. `Player.Instance` always defaults
    to a boot-time Wizard even with no save (see `Util.DetermineLastPlayedClass`), and the Nexus
    button jumped straight into gameplay with whatever was currently loaded. Fixed by adding
    `Util.AnyCharacterHasBeenPlayed()` and a `StateManager.EnterNexus()` that checks it before
    deciding between `NewGame()` and `SelectClass()`; the check was initially duplicated inline in
    `MenuState.cs` and `Input.cs`, then consolidated into `EnterNexus()` itself.
15. **Game Over's "New Game" button (and its Enter-key shortcut) bypassed the above check
    entirely**, calling `StateManager.NewGame()` directly — so dying with your only-ever-played
    character and clicking "New Game" would silently respawn it instead of redirecting to Character
    Select once death reset it back to "unplayed" (see #16). Fixed by routing both through
    `EnterNexus()` instead.
16. **Dying briefly re-marked the reset character as `HasBeenPlayed = true`**, which was
    inconsistent with every other field the death-reset wipes back to default (matching a delete).
    Fixed by removing that line — death now leaves `HasBeenPlayed` false, same as an explicit
    Delete.
17. **Entering a dungeon saved player data but not inventory data.** `GameState`'s constructor
    called `Util.SavePlayerData()` on dungeon entry but never `Util.SaveInventoryData()`, unlike
    every other save site in the game — a latent risk of the two files disagreeing if equipment was
    changed right before entering a dungeon. Fixed by adding the missing call.
18. **Leaving the Nexus for Character Select silently discarded in-progress changes on one route
    but not another.** `StateManager.SelectClass()` (the in-world Character Select portal, and the
    Main Menu's Character Select button) had no save calls at all, so an equipment drag made just
    before leaving this way was lost on reselecting the same character — while the Escape-key route
    (`MainMenu()`) correctly persisted it. Fixed by adding matching
    `SavePlayerData()`/`SaveInventoryData()` calls to `SelectClass()`, mirroring `Nexus()`.
19. **Dragging the equipped ability item (Spell/Quiver) into inventory and returning to Character
    Select produced a duplicate ability item.** Unlike Weapon/Armor/Ring, `LoadOrCreatePlayer`'s
    Spell/Quiver reload logic had no `else` branch to explicitly unequip the slot when the save
    showed nothing equipped — so `ResetPlayer`'s constructor-equipped default Tier-0 Spell/Quiver
    was never cleared, leaving that phantom default equipped alongside the real item that correctly
    loaded into inventory. Fixed by adding the missing
    `else Player.Instance.EquipAbilityItem(new AbilityItem());`, matching the other three slots.
20. **Drag-and-drop resolved one frame late, using stale mouse position — surfaced as bank drags
    always dropping to the ground.** `Input.MouseReleased()` checked
    `previousMouse.LeftButton == Released`, which is true for every frame after a release, not just
    the frame of the actual Pressed→Released transition — so drag resolution ran a frame late,
    using whatever mouse position was current by then. Scripted tests with a perfectly static mouse
    never caught it, but a real mouse drifts in that extra frame — often enough to miss a small,
    precise drop target like the 160×160 bank panel and fall through to the much larger "drop on
    the ground" catch-all instead. Fixed by properly detecting the transition:
    `previousMouse.LeftButton == Pressed && mouse.LeftButton == Released`.
21. **Bank panel could get clipped at the top of the screen.** `BankSystem` drew itself at a fixed
    screen position (`y = 64`), unrelated to where the Bank portal actually was on screen — the
    camera follows the player (`Player.cs`'s `Game1.Camera.Pos += Velocity`), so the portal's own
    on-screen position shifts depending on which direction the player approached from, but the
    panel didn't move with it. Fixed by anchoring the panel to the portal's world position instead:
    `Portal.Update()` now shares the portal's world-space center via `BankSystem.PortalPosition`,
    and `BankSystem` transforms that through `Game1.Camera.GetTransformation()` (the same transform
    used to draw everything else in world-space) each frame to compute where to draw itself —
    above and centered on the portal, tracking it as the camera moves. *(13:26 EDT)*
22. **Dragging an item toward the bank rendered underneath the bank panel.** `RealmState.Draw()`
    called `BankSystem.Draw()` after `Overlay.DrawInventory()`, so the bank's own border/items —
    drawn later — painted over the inventory's drag ghost whenever it was positioned over the bank
    panel. Fixed by splitting both `InventorySystem` and `BankSystem`'s ghost rendering into a
    separate `DrawDragGhost()`, called for both *after* both panels' main content, so whichever
    ghost is active always draws on top regardless of which panel it's over. `GameState.cs` (the
    dungeon HUD, which also draws the inventory but has no bank) needed the same
    `DrawDragGhost()` call added, since pulling that logic out of `Draw()` would otherwise have
    silently dropped its drag ghost entirely. *(13:53 EDT)*
23. **Self-inflicted: cleaning up a temp verification block from `Game1.StartGame()` accidentally
    deleted the real `Quivers = Util.LoadQuiverData();` line above it**, since the removal edit's
    match spanned from that line down through the temp block. Went unnoticed for two feature turns
    because every boot-check in between happened to load the Wizard (whose constructor doesn't
    touch `Quivers`) — only surfaced as a crash (`ArgumentNullException` in `Quiver.LoadQuiver`,
    called from `Archer`'s constructor) once a boot check happened to load Archer instead. Fixed by
    restoring the line; a lesson for future cleanup edits touching lines adjacent to real code, not
    just the temp block itself — re-verify boot-checks cover both playable classes, not just
    whichever one a prior test happened to leave as "last played." *(15:21 EDT)*
24. **Bank contents vanished on every restart, even though they'd been saved correctly.**
    `Util.SaveBankData()` wrote `BankSystem.Records` to `BankData.json` at every save checkpoint as
    intended, but `Util.LoadBankData()` — fully implemented and correct — was never actually called
    anywhere; `Game1.StartGame()` called `LoadWeaponData`/`LoadArmorData`/`LoadRingData`/
    `LoadSpellData`/`LoadQuiverData` at boot but not `LoadBankData`, so `BankSystem.Records` only
    ever existed in memory for the current process lifetime. Fixed by adding
    `Util.LoadBankData();` to `Game1.StartGame()` alongside the other shared-catalog loads, before
    `Util.LoadOrCreatePlayer(...)`. Verified with a headless standalone harness (referencing the
    built `Realm.dll` directly, no window/content needed since Save/Load only touch file I/O and the
    static `Records` array): populated `BankSystem.Records[0]`, called `SaveBankData()`, cleared the
    slot, called `LoadBankData()`, confirmed the item reappeared — passed. Also did a real boot smoke
    test of `Realm.exe` (stayed running several seconds, no stderr output) and confirmed the real
    `BankData.json`/`PlayerData_*.json` save files were untouched (byte-identical to pre-test
    backups) throughout. *(16:13 EDT)*
26. **Moving an item into the bank and then closing the game (without ever leaving the current
    state) lost the change.** `Util.SaveBankData()`/`SaveInventoryData()`/`SavePlayerData()` only
    ever fired at state-transition checkpoints (`StateManager.Nexus()`/`MainMenu()`/`SelectClass()`/
    `NewGame()`, `GameOverState`, `GameState`'s constructor) — a player who dragged or clicked an
    item into the bank while standing at it, then quit without walking away or dying, had nothing
    persist at all. Rather than adding a save call to each of the many individual mutation sites
    that can touch `BankSystem.Records` (drag/click to bank, drag/click from bank, bank↔equip-slot
    swaps, bank-to-bank reorder, the ground-drop-into-bank auto-redirect) — easy to miss one, which
    is exactly how this bug happened in the first place — both `InventorySystem.Update()` and
    `BankSystem.Update()` now save unconditionally right after handling any release that had an
    actual drag/click in progress, regardless of which branch fired, so a future branch added to
    either release handler can't silently forget to persist its own mutation. Verified by reading
    the raw `BankData.json`/`InventoryData_Wizard.json`/`PlayerData_Wizard.json` files directly off
    disk immediately after simulated bank↔inventory clicks and a bank→equip-slot drag — each file
    reflected the change immediately, with no state-transition checkpoint invoked at all. *(16:59 EDT)*
25. **Loot bags couldn't be picked up from in the Nexus (the main portal room).** `LootBag`'s
    click-to-pickup logic lives inside `DrawLoot()` itself (unusual, but that's where it is) —
    `GameState.Draw()` (dungeons) iterates `ItemSpawner.LootBags` and calls `bag.DrawLoot(...)`
    each frame, but `RealmState.Draw()` (the Nexus) never did, so a bag dropped there rendered its
    icon (via the normal `EntityManager.Draw()` pass) but its contents/click-handling never ran —
    walking up to it did nothing. Fixed by adding the same `ItemSpawner.LootBags.ToList()` /
    `bag.DrawLoot(spriteBatch)` loop to `RealmState.Draw()`, matching `GameState.Draw()` exactly.
    Verified with a scripted repro: constructed a real `RealmState`, dropped a loot bag at the
    player's feet, simulated a click on the rendered item — landed in inventory and the emptied bag
    was removed from `ItemSpawner.LootBags`, all while running through `RealmState`, not `GameState`.
    *(16:38 EDT)*

## 2026-08-14

26. **Sword's basic-attack projectile crashed the game at boot once its real art was wired in.**
    Part of the Knight feature (see [DEVLOG.md](DEVLOG.md)
    entry 31): the user supplied `Content/Projectiles/sword_slash.png` and pointed all 15 Sword
    tiers' `ProjectileImageName` at it in `Data/WeaponData.json`, but the corresponding
    `Content.mgcb` build entry was deliberately left out during that feature (the file didn't exist
    yet at the time) — `Content.Load<Texture2D>("Projectiles/sword_slash")` then failed with
    `ContentLoadException: The content file was not found` inside `Util.LoadWeaponData()`, which
    runs unconditionally at boot for every class, not just Knight. Fixed by adding the missing
    `#begin`/`#build` block to `Content.mgcb` (same shape as the other `Projectiles/*.png` entries),
    now that the source file actually exists. Verified with a real boot straight into the Nexus as
    Knight — same pattern as the original feature's verification — confirming both
    `LoadWeaponData()` (all 15 Sword tiers) and the actually-equipped weapon's `ProjectileImage`
    load without throwing. *(10:57 EDT)*

## 2026-08-15

27. **Player could walk straight past the edge of the new `BossRealmState` arena (see
    [DEVLOG.md](DEVLOG.md) entry 49), and once they did,
    the camera got permanently stuck off-center — never recentering even after walking back to the
    middle.** Root cause of the second symptom, found before writing any fix: `Player.Update()`
    followed the camera via `Game1.Camera.Pos += Velocity` — the *same* delta the player's own
    `Position` received, not a direct sync to `Position` itself. `Camera.Pos`'s setter (`Camera.cs`)
    clamps to the world's edges, so the moment the player got close enough to a boundary for that
    clamp to engage, the *stored* camera position silently diverged from the *unclamped* value the
    `+=` math assumed — and because both `Position` and `Camera.Pos` kept receiving the identical
    `Velocity` every frame afterward, that gap became permanent, not just temporary near the edge:
    it would persist even after the player moved back into open space, since nothing ever
    recomputed the camera position from scratch. This was always latently present, just never
    triggered before — the open Realm world is 500,000px, so a player would need to walk almost the
    entire map in one direction to ever get close enough to the edge for `Camera.Pos`'s clamp to
    engage; the boss arena's 2000×2000 bounds (small enough that the clamp's own ~490px margin
    covers a large fraction of it) made it trigger constantly. Fixed at the source, not just
    papered over for the arena: `Player.cs` now does `Game1.Camera.Pos = Position;` (direct sync)
    instead of `+= Velocity`, so the camera is recomputed fresh from the player's real position
    every frame regardless of any clamping that happened on a previous frame — this fixes the
    recentering bug for every state, not just the new arena, since it was a real latent bug in the
    shared camera-follow code, not something arena-specific. Separately — since nothing in
    `Player.Update()` ever clamped `Position` itself, only `Camera.Pos` — the *first* symptom
    (walking past the edge) needed its own fix: `BossRealmState` gained a `public override
    void Update(GameTime gameTime)` (confirmed legal — `RealmState.Update()` isn't `sealed`) that
    runs `base.Update()` then clamps `Player.Instance.Position` to
    `[Player.Radius, InstanceWorldWidth/Height - Player.Radius]` on both axes (using the player's
    own collision radius so the sprite doesn't visually clip through the wall), then explicitly
    re-syncs `Game1.Camera.Pos` to match the just-clamped position — belt-and-suspenders with the
    `Player.cs` fix, guaranteeing the camera reflects the *final* post-wall-clamp position for that
    frame rather than the pre-clamp one `Player.Update()` already computed earlier in the same
    `Update()` call. Verified via a scripted repro: pushed the player far beyond the arena on both
    the positive and negative side of both axes and confirmed `Position` clamped to exactly
    `radius`/`worldSize - radius` each time (not just roughly close); confirmed the camera clamped
    to its own correct edge value there too (a *different*, larger margin than the player's wall —
    expected letterboxing near a true boundary, not a bug); then moved the player back to the
    arena's exact center and confirmed the camera fully recentered to match exactly — the specific
    scenario the user reported as broken. *(10:19 EDT)*

28. **A scaled-up enemy's health bar (e.g. the boss's `drawScale = 1.75`, entry 48) sat underneath
    the sprite instead of below it.** `Enemy.DrawHealthBars()`'s offset math (`x = Position.X -
    image.Width/4`, `y = Position.Y + image.Height/2`) used the sprite's raw, *unscaled*
    `image.Width`/`Height` — but `Entity.Draw()` (the entry `drawScale` extension point) actually
    renders the sprite `drawScale`× bigger around the same center `Position`, so a scaled-up
    enemy's real on-screen edges extend further out than the bar math assumed, putting the bar
    inside the sprite's footprint instead of clear of it. Only visible once something actually set
    `drawScale` away from its default `1` (the boss is currently the only enemy that does) — every
    other enemy was unaffected, since `× 1` is a no-op. Fixed by multiplying both offsets by
    `drawScale`, mirroring exactly the same factor `Entity.Draw()` itself already applies. Verified
    via a scripted repro: damaged both a normal enemy (`drawScale = 1`, unaffected case) and the
    boss (`drawScale = 1.75`, the actually-affected case) below their max health and called
    `DrawHealthBars()` through a real `SpriteBatch.Begin()/End()` pair for each, confirming neither
    threw with the new scale-aware math in place. *(10:34 EDT)*

## 2026-08-17

29. **Rectangle collision hitboxes (see
    [DEVLOG.md](DEVLOG.md) entries 61-62) looked like
    their width/height were swapped for rotated non-square projectiles**, reported by the user
    while watching Limon's Spray shots (now `limon1.png`, 40×10) via the F3 debug hitbox view.
    Not a literal field-swap — `Entity.Width`/`Height` correctly mapped to `image.Width`/`Height`,
    and `EntityManager.RectangleBounds()` correctly passed them to `Rectangle`'s `width`/`height`
    constructor params in the right order. The real cause: `Entity.Draw()` rotates the sprite by
    `Orientation` (`EnemyProjectile` sets it from `Velocity.ToAngle()`, so a projectile visually
    turns to face its travel direction), but `RectangleBounds()` built its box from the sprite's
    raw, *unrotated* pixel `Width`/`Height` — so a Spray shot fired toward a player positioned
    above/below the boss rotates ~90° and renders ~10 wide × 40 tall, while its hitbox was still
    computed as 40 wide × 10 tall, exactly backwards from what's on screen. Fixed by making
    `RectangleBounds()` account for `Orientation` using the standard closed-form AABB-of-a-rotated-
    rectangle formula (`rotatedHalfWidth = halfWidth·|cos θ| + halfHeight·|sin θ|`, and the
    equivalent swapped sum for height) rather than manually rotating and re-bounding all 4 corners —
    algebraically equivalent, simpler to read. `Orientation = 0` reduces to the original unrotated
    box exactly (`cos 1, sin 0`), so nothing changes for anything that doesn't rotate. Verified via
    a scripted repro (reflection for the private static `RectangleBounds`/`IsColliding`, test-only):
    confirmed a `limon1`-sized (40×10) entity at `Orientation = 0` still produces a 40×10 box
    (regression check); confirmed the same entity at `Orientation = 90°` now produces a 10×40 box,
    matching the rotated visual exactly; and — the concrete practical proof — confirmed two such
    90°-rotated shots positioned 20 units apart on the axis perpendicular to travel no longer
    falsely collide (their now-correctly-thin ~10-wide boxes don't overlap at that distance), where
    the old rotation-unaware code would have used the full 40-wide box and wrongly reported a hit.
    *(15:14 EDT)*
30. **`ItemSpawner.Spawn()`'s random ability-item loot drop never included Shields (Knight's
    catalog), only Spells and Quivers.** Reported by the user as "only ever dropping the Spell
    ability item." The candidate pool at line ~122 concatenated `Game1.Instance.Spells` and
    `Game1.Instance.Quivers` (both filtered to the next tier above whatever the player has
    equipped) before picking one at random, but never referenced `Game1.Instance.Shields` at all —
    a leftover gap from when Shield/Knight was added after this drop logic was originally written
    for just Wizard/Archer. Fixed by adding a third `.Concat(Game1.Instance.Shields.Where(x =>
    x.Tier == Player.Instance.AbilityItem.Tier + 1))` to the chain, so all three ability-item
    catalogs are represented. Verified via a scripted repro (statistical, since the drop itself is
    randomized — a 1/15 chance to attempt an ability-item drop, then a random pick among whatever's
    in the pool): called `ItemSpawner.Spawn()` 3000 times as a fresh Wizard and tallied the concrete
    type of every `AbilityItem` that landed in a loot bag — got 71 Spells, 71 Quivers, and 76
    Shields, roughly even as expected (each tier has exactly one candidate per catalog, so all three
    should be equally likely) and, critically, confirming Shields drop at all, which they never did
    before this fix. *(16:39 EDT)*
31. **The identical missing-Shields bug also existed in `SpawnGuaranteedLoot()`** (boss/guaranteed
    drops), a separate method from `Spawn()` — entry 30's fix only touched `Spawn()`, and the user
    asked to check the other drop categories for the same class of bug, which surfaced this second,
    untouched copy of the exact same 2-catalog `Spells`/`Quivers` concatenation missing `Shields`.
    Fixed the same way: added `.Concat(Game1.Instance.Shields.Where(x => x.Tier ==
    Player.Instance.AbilityItem.Tier + 1))`. While checking, also confirmed the weapon and armor
    drop categories in both methods are **not** affected by this class of bug — `WeaponData.json`/
    `ArmorData.json` are each a single unified catalog file covering all types (confirmed via direct
    inspection: 15 entries each for `Type: 0/1/2` in both files — Wand/Bow/Sword and Robe/Leather/
    Heavy are all present in the one shared `Game1.Instance.Weapons`/`Armors` list already, unlike
    Spell/Quiver/Shield which are three genuinely separate typed lists), and Ring has no per-class
    catalog at all (no class restriction, single list). Verified via a scripted repro: called
    `SpawnGuaranteedLoot()` 300 times as a fresh Wizard and tallied ability-item types — 117 Spells,
    90 Quivers, 93 Shields, confirming all three now drop; also confirmed every one of the 300
    resulting bags still had exactly 5 items (weapon + armor + ring + ability item + potion, each
    always contributing one when a next tier exists) — a regression check that the fix didn't
    disturb the method's "always contributes" guarantee for the other categories. *(16:43 EDT)*

## 2026-08-18

32. **Fresh character creation crashed for all three classes** — surfaced by a routine final
    boot-check (loading whichever class `Util.DetermineLastPlayedClass()` picked) throwing
    `NullReferenceException` in `Player.EquipmentAttackBonus` during `Knight`'s constructor, deep in
    an unrelated tooltip feature's verification pass. Root cause: `RingData.json`'s starting Tier-0
    ring had been renamed from "Plain Ring" to "Ring of Minor Defense" (a hand-tuning edit made
    directly to the data file, outside this conversation's own changes), but `Wizard.cs`/`Archer.cs`/
    `Knight.cs` all still hardcoded `Ring.LoadRing("Plain Ring")` as their default starting ring.
    `Ring.LoadRing()`'s failure path (name not found in the catalog) plays an error sound and returns
    `null` *without* calling `Player.Instance.EquipRing()` — so the outer assignment (`Ring =
    Ring.LoadRing("Plain Ring");`) sets `Player.Instance.Ring` directly to `null`, bypassing
    `RecalculateStats()` entirely at that moment (no immediate crash) — the crash only happened
    later, at whichever *next* successful equip call (Weapon/Armor/AbilityItem) triggered
    `RecalculateStats()` and dereferenced the now-null `Ring`. This affected every class, but had
    gone unnoticed because most boot-checks all session loaded an *existing* saved character (which
    re-equips whatever ring name was actually saved, not the hardcoded default), so it only surfaces
    for a genuinely fresh, never-before-played character going through `Util.ResetPlayer()`. Fixed
    by updating all three classes' hardcoded ring name to `"Ring of Minor Defense"`, matching the
    current catalog. Verified via a scripted repro: constructed a fresh `Wizard`/`Archer`/`Knight`
    directly (bypassing any save file) and confirmed all three now construct without exception, each
    with `Ring.IsEquipped == true`; followed by a real, unmodified boot (no temp code) that
    previously crashed on this exact path and now stays running normally. *(10:02 EDT)*
33. **Knight couldn't equip a new Tier 1 (or any) Shield at all — dragging one onto the AbilityItem
    slot silently did nothing.** `InventorySystem.TryEquipFromRecord()` (the shared drag-release
    handler used by both `InventorySystem.cs` and `BankSystem.cs` — confirmed `BankSystem` calls
    this exact same method, so the bug affected both drag sources identically) resolves which
    factory to call for the AbilityItem slot via `draggedRecord.InventoryItem switch { Spell s =>
    ..., Quiver q => ..., _ => null }` — `Shield` was never added as a case when the Knight class
    was built, so a dragged Shield always fell to `_ => null`. Because the outer `if` block still
    matched (a `Shield` *is* an `AbilityItem`) and unconditionally `return`ed `true` regardless of
    whether the inner switch actually resolved anything, the drag was silently swallowed with no
    error sound and no equip — the exact "nothing happens" symptom reported, and the same missing-
    case-in-a-switch shape as [DEVLOG.md](DEVLOG.md)'s entries 30/31 ability-item drop
    bugs (Shield being the class most recently bolted onto a Spell/Quiver-only switch, twice now).
    Fixed by adding `Shield sh => Shield.LoadShield(sh.Name),` to the switch. Verified via a scripted
    repro: constructed a fresh Knight, placed a Tier 1 "Iron Shield" `InventoryRecord` in the
    inventory, simulated the mouse sitting over the AbilityItem slot at drag-release (writing
    directly to `Input.mouse`, same technique as recent hover-tooltip tests) and called
    `TryEquipFromRecord()` directly — confirmed `Player.Instance.AbilityItem` became the real
    equipped Shield (`is Shield` true, correct name), where before the fix it would have silently
    stayed whatever was equipped previously. *(10:10 EDT)*
34. **A real Shield the user had already picked up and saved to their live Knight inventory
    (a Tier 1 "Iron Shield") showed no tooltip on hover and couldn't be equipped at all** — a
    different, more severe bug than entry 33's drag-switch gap, since this one meant the item had
    already lost its real identity *in the save file itself*. Root cause: `Item.cs`'s
    `[JsonPolymorphic]`/`[JsonDerivedType]` attribute list — which every polymorphic `Item` subclass
    that can end up in a save MUST be registered in, per that file's own doc comment, "or saving it
    throws `NotSupportedException`" — was missing `Shield` entirely, unlike `Weapon`/`Potion`/
    `Armor`/`Ring`/`Spell`/`Quiver`, all registered. Confirmed directly in the user's own real
    `InventoryData_Knight.json`: the saved Iron Shield entry read `"$itemType":"Item"` with every
    `AbilityItem`/`Equipment`-specific field (`Tier`, all 8 bonuses, `ManaCost`, `MinDamage`,
    `MaxDamage`) missing — it had silently degraded to a generic base `Item` on some earlier save,
    losing its `is Shield`/`is AbilityItem`/`is Equipment` identity entirely. This explains both
    symptoms exactly: no tooltip, because the hover code's `is Equipment` check now failed for it
    (falling through to whichever fallback exists, not the full stat tooltip); unable to equip,
    because none of `TryEquipFromRecord()`'s four type checks (`is Weapon`/`Armor`/`Ring`/
    `AbilityItem`) matched a plain `Item`, hitting the bottom "wrong type for this slot" bounce
    instead. Fixed the registration gap (`[JsonDerivedType(typeof(Shield), typeDiscriminator:
    "Shield")]`), which prevents any *future* Shield from degrading on save — but doesn't
    retroactively repair data already written in the degraded form, since JSON deserialization
    matches on the *persisted* discriminator string, not a guessed "real" type. Backed up the real
    save file, then hand-repaired the one corrupted entry directly in
    `InventoryData_Knight.json` — changed `"$itemType":"Item"` to `"Shield"` and restored the
    missing stat fields from `ShieldData.json`'s canonical Tier 1 "Iron Shield" entry (Tier 1,
    DefenseBonus 3, ManaCost 85, MinDamage 100, MaxDamage 140), keeping the original item `ID` so
    it's still recognizably the same item. Verified in three stages: (1) an isolated
    `JsonSerializer.Serialize`/`Deserialize` round-trip on a freshly-constructed `Shield` (bypassing
    the real save file entirely) confirmed the code fix alone makes Shield round-trip correctly now;
    (2) validated the hand-repaired JSON is well-formed and the discriminator reads `Shield`; (3) a
    full read-only pass through the user's *actual* repaired save (`Util.LoadOrCreatePlayer(Knight)`
    — never calling any `Save*` method, confirmed the file was still byte-identical afterward) found
    the real Iron Shield record, confirmed it now deserializes as a genuine `Shield` with the correct
    `Tier`/`ManaCost`, confirmed `ComparisonLines()` produces a real 5-line tooltip, and confirmed
    dragging it onto the AbilityItem slot equips it for real. *(10:25 EDT)*
35. **Comparison tooltips (see entries 66/67 in
    [DEVLOG.md](DEVLOG.md)) overlapped their own text for
    items with a long description, and got cut off by the right edge of the window for anything
    anchored near the sidebar.** Two separate root causes. First: `Equipment.HeaderLines()` split
    the description (already word-wrapped by `Util.WrapText()`) on `Environment.NewLine`
    (`"\r\n"` on Windows) to turn it into individual tooltip lines — but `WrapText()` actually
    inserts a bare `"\n"` between wrapped lines, so the split never matched and an entire
    multi-line wrapped description collapsed into one list entry containing raw embedded `\n`
    characters. Since the per-line tooltip renderer positions each list entry at a fixed
    `font.LineSpacing` increment (unaware of embedded newlines inside a single entry), a
    description that visually spans several physical lines overlapped whatever entry (e.g. a
    bonus line) was positioned right after it. Fixed by splitting on `'\n'` instead. Second:
    `Util.DrawTooltip()` (both overloads) always drew tooltips extending rightward from their
    anchor with no screen-bounds checking, clipping anything anchored close enough to the right
    edge — most commonly the sidebar's own equip/inventory/bank slots (`Game1.SidebarX` at 980,
    `WindowWidth` at 1280, leaving only 300px of margin). Fixed by adding a private
    `Util.ClampTooltipX(float x, float width)` helper, clamping the tooltip's X to
    `[edgeMargin, WindowWidth - width - padding - edgeMargin]` (never moving it right of its real
    anchor, so already-narrow tooltips stay put), applied in both `DrawTooltip` overloads right
    before computing the background rectangle. Verified via a scripted repro: built a `Ring` with
    a deliberately long (200+ character) description, called `ComparisonLines()`, and confirmed
    the description now spans 6 separate list entries (previously would have been 1); called the
    private `ClampTooltipX` via reflection with a position/width combination that would extend
    ~220px past the window's right edge and confirmed the returned X keeps `x + width` within
    `WindowWidth`, and with a position already safely on-screen and confirmed it's returned
    unchanged. *(10:36 EDT)*
36. **A loot bag's item images were drawn on top of the tooltip for a different, earlier item in
    the same row.** `LootBag.DrawLoot()` laid out items left-to-right (64px apart) in a single
    loop, drawing each item's border + icon and then — inline, in that same iteration — the
    tooltip for whichever item was currently hovered. Since `SpriteBatch` preserves draw-call
    order, a tooltip drawn during item 0's iteration (often much wider/taller than the 64px item
    spacing, especially after entry 35 above made long descriptions properly multi-line) could
    still be on screen when the loop reached item 1, 2, etc. — whose border/icon draw calls,
    happening later in the same frame, painted right over it wherever the two overlapped. Fixed by
    splitting the loop into two passes: the first draws every item's border/icon and just records
    which index (if any) is hovered, and a second pass — running only after every item in the row
    has already been drawn — draws that one hovered item's tooltip last, so nothing drawn afterward
    can paint over it. Verified via a scripted repro: built a 2-item loot bag (item A given a long,
    multi-line description to guarantee its tooltip reaches into item B's column), hovered item A,
    rendered to a `RenderTarget2D`, and sampled the exact center pixel of item B's icon — before the
    fix this would read back as the icon's own raw, unblended color (confirmed via a direct
    `Texture2D.GetData` read of the same texel), but after the fix it read back visibly blended with
    the tooltip's translucent background tint (`(216,216,216)` vs. the icon's raw `(255,255,255)`),
    confirming the tooltip now draws on top of a later item's icon instead of underneath it.
    *(10:45 EDT)*
37. **A stat potion could be blocked by equipment alone, even with real permanent room left below
    the cap.** Reported as "unable to drink a Defense Potion even though base Defense is below the
    cap." `InventorySystem.UsePotionEffect()`'s six stat-potion gates (Attack/Defense/Speed/
    Dexterity/Vitality/Wisdom) compared the live, fully-bonused stat (`Player.Instance.Defense`,
    etc. — includes `EquipmentXxxBonus`) against the fixed level cap (`MaxDefense`, etc.), so
    strong-enough gear alone could push the displayed stat past its cap and block the potion, even
    though nothing permanent (level + `PotionXxxBonus`) had actually reached it — same root
    misdiagnosis as [DEVLOG.md](DEVLOG.md) entry 68's sidebar highlight bug, just gating
    an action instead of a color this time; deliberately left out of that entry's scope as "a
    different concern" since the user hadn't reported it yet. Fixed by switching all six gates from
    the raw stat to entry 68's `Player.PermanentAttack`/`PermanentDefense`/`PermanentSpeed`/
    `PermanentDexterity`/`PermanentVitality`/`PermanentWisdom` (already excludes equipment and
    temporary bonuses) — no new plumbing needed, since those properties already existed for exactly
    this "is this actually maxed" question. The Health/Mana/Life/ManaMax potion gates were already
    correct and untouched — `HealthMax`/`ManaMax`/`Health`/`Mana` never included equipment bonuses
    in the first place (see [BUGFIXES.md](BUGFIXES.md)'s earlier Max-stat-formula fix). Verified via a
    scripted repro: equipped an `Armor` with a 1000 `DefenseBonus` on a fresh Wizard, confirmed raw
    `Defense` (1010) exceeded `MaxDefense` (25) — the exact condition that used to wrongly block —
    while `PermanentDefense` (9, matching the starting gear's small existing bonus) correctly stayed
    below it; then called `UsePotionEffect("Defense Potion")` directly and confirmed it returned
    `true` and `PotionDefenseBonus` actually incremented, where before the fix it would have
    returned `false` with no effect. *(11:12 EDT)*
38. **The equipped ring icon looked off-center inside its equip slot border** — not a code bug.
    `Ring.DrawEquipped()` draws the border and the ring image at the identical anchor position with
    no origin offset, exactly the same pattern `Weapon`/`Armor`/`AbilityItem` all use, and their
    icons look correctly centered — so the draw call itself was never the problem. Diagnosed by
    rendering the actual slot at 8x zoom with a point-filter sampler to a saved PNG (rather than
    guessing) and visually inspecting it: the border showed a thick margin on the top/left and none
    at all on the bottom/right, meaning the *ring artwork itself* wasn't centered within its own
    40x40 canvas. Confirmed by measuring each source PNG's actual opaque-pixel bounding box: both
    `Content/Rings/0.png` and `1.png` had 3px of empty padding on the left (and, for tier 0, the
    top) but 0px on the right/bottom — the ring graphic touched two edges of its canvas and left a
    gap on the other two. Checked whether this was a broader pattern before fixing anything: sampled
    several Weapon/Armor/AbilityItem source images the same way and found 0px padding on all four
    sides for every one of them — those artists drew their icons to fill the full 40x40 tile edge to
    edge, so there was nothing for a runtime centering fix to correct there; only Ring's art has
    internal padding, and it happens to be asymmetric. Rather than add generic runtime bounding-box-
    detection/centering code to `Ring.cs` for a problem that's really just 2 misaligned source files,
    fixed the assets directly: wrote a script that decodes each PNG (handling both files' different
    formats — tier 0 is true-color RGBA, tier 1 is palette-indexed with a `tRNS` transparency chunk)
    into raw RGBA, measures the opaque bounding box, shifts the pixel content by
    `(padLeft-padRight)/2` horizontally and `(padTop-padBottom)/2` vertically (rounded down — the odd
    3px total padding can't split perfectly evenly, so the best achievable result is off by 1px, not
    0), and re-encodes as a standard RGBA PNG. Verified visually both before and after by rendering
    the real equipped Tier 0 ring through the actual `Ring.DrawEquipped()` code path at 8x zoom and
    inspecting the saved screenshot: before, the border's margins were clearly asymmetric (thick top/
    left, none bottom/right); after, all four margins read as visually even. *(11:42 EDT)*
39. **Using an ability (Space) with no ability item equipped silently "worked" instead of erroring**
    — a blank/unequipped `AbilityItem` has every stat field at C#'s default `0`
    (`MinDamage`/`MaxDamage`/`ManaCost` all 0), and `Random.Next(0, 0)` doesn't throw, it just
    returns `0` — so `UseAbility()` happily fired real projectiles for 0 damage and (since
    `AbilityCost = Math.Max(1, 0) = 1`) drained 1 Mana each time, with no feedback that anything was
    wrong. All three classes already had an identical guard for the adjacent "no weapon equipped"
    case (`if (!Weapon.IsEquipped) { Sound.Play(Sound.Error, 0.4f); return; }`) but never had the
    equivalent for the ability slot. Fixed by adding the same guard, checking
    `!AbilityItem.IsEquipped`, right after the existing weapon check in `Wizard.cs`/`Archer.cs`/
    `Knight.cs`'s `UseAbility()` — matching the codebase's existing style of duplicating this small
    check per class rather than introducing a new shared hook. Verified via a scripted repro for all
    three classes: unequipped the AbilityItem slot, called `UseAbility()`, and confirmed neither
    `EntityManager.Count` (no projectile spawned) nor `Mana` changed — proving the guard returns
    before any side effect, not just before the visible ones; then equipped a real ability item and
    confirmed `UseAbility()` still fires normally (a real projectile spawns), proving the new guard
    doesn't block legitimate use. Caught a test-harness-only issue along the way, not a real bug:
    Wizard's ability path calls `Input.GetMousePosition()`, which needs both `Input.mouse` and
    `Game1.Camera` initialized — neither exists yet this early in `Game1.StartGame()` without first
    constructing a state and setting a mouse position, the same two setup steps already established
    by earlier tests this session. *(12:07 EDT)*
42. **Self-inflicted: a scripted test overwrote the user's real inventory and bank save data,
    causing a real crash entering the Nexus.** While verifying the inventory↔bank drag-swap feature
    (entry 79 in [DEVLOG.md](DEVLOG.md)), the test cleared the real
    `Player.Instance.Inventory.InventoryRecords`/`BankSystem.Records` arrays to isolate two known
    fake items ("Item A"/"Item B", `image` set directly, no `ImageName`), then called
    `Player.Instance.Inventory.Update()`/`BankSystem.Update()` directly to simulate a drag release.
    That release handler saves unconditionally whenever a drag was in progress — so it wrote the
    fake, mostly-empty state straight over the real `InventoryData_Wizard.json`/`BankData.json` on
    disk, with no backup taken first. Went unnoticed until the user hit
    `NullReferenceException: ... InventoryItem.image was null` entering the Nexus — the saved "Item
    A" had `ImageName: null`, and `InventoryRecord`'s constructor only hydrates `.image` from
    `ImageName` `if (item.ImageName is not null)`, so a null `ImageName` leaves `.image` permanently
    null, crashing the first draw/hover check that touches it. Confirmed via direct inspection: both
    files contained exactly the fake single-item, mostly-null-slots pattern the test had set up.
    `PlayerData_Wizard.json` (level, stats, equipped gear, score) was untouched — the corruption was
    scoped to the general inventory grid and the shared bank. Fixed the acute crash by nulling out
    both broken entries (backing up the corrupted files first, in case they're ever useful) so the
    game boots again; the user's actual prior inventory/bank contents were not recoverable, since
    the overwrite had no backup taken before it happened. Checked Archer/Knight's inventory data and
    `FameData.json` for the same pattern — found none, so the damage appears scoped to Wizard's
    inventory and the account-wide bank. See CLAUDE.md for the process fix (back up
    real save files before any future scripted test that could trigger persistence) so this doesn't
    happen again. *(15:38 EDT)*
43. **Loot bags dropped on the ground could survive a state change and keep rendering/functioning
    in a state they no longer belonged to.** `ItemSpawner.Reset()` (clears the static `LootBags`
    list) was only ever called from `RealmState`'s own constructor — so entering a dungeon
    correctly cleared bags left over from before, but every *other* transition
    (`StateManager.Nexus()`/`MainMenu()`/`SelectClass()`/`NewGame()`/`GameOver()`, none of which
    call `ItemSpawner.Reset()`) left any bags still on the ground fully intact: still in
    `ItemSpawner.LootBags`, still drawn and click-to-pick-up-able via the next state's own
    `DrawLoot()` loop (`NexusState.Draw()`/`RealmState.Draw()` both iterate that same list), at
    whatever world position they'd been dropped at in the *previous* state — a bag from a dungeon
    could sit there rendering in the Nexus, or persist indefinitely across several Nexus↔Character-
    Select round trips, until the player finally re-entered a dungeon and `RealmState`'s constructor
    incidentally cleared it. Fixed by moving the clear to `Game1.ChangeState()` instead of any
    individual state's constructor — every single transition in the game (all of
    `StateManager.cs`'s methods) funnels through that one method, so this is the one choke point
    that guarantees "on every state change," including any future state added later without
    needing to remember to add the call there too. Verified via a scripted repro that deliberately
    touched nothing on `Player.Instance`/`Inventory`/`BankSystem` and called no persistence method
    at all (kept deliberately minimal after entry 42's real-data-corruption incident from an
    over-broad test): added 2 fake `LootBag`s directly to `ItemSpawner.LootBags`, called
    `ChangeState(null)`, and confirmed the count dropped from 2 to 0. *(15:50 EDT)*
44. **Follow-up to entry 43 — the loot bag's icon still kept rendering after the fix, even though
    it could no longer be interacted with**, reported by the user right after entry 43 shipped.
    Entry 43's `ItemSpawner.Reset()` only cleared the `LootBags` list (the interactive click-to-
    pickup tracking `DrawLoot()` reads), but each bag is *also* a plain `Entity` registered
    separately with `EntityManager` for its icon draw — clearing the list never touched that second
    registration. Not every state resets `EntityManager` on entry either: `NexusState`/
    `BossRealmState` do, but `RealmState` (regular dungeons) doesn't — so a bag dropped in the
    Nexus and left behind would carry its `Entity` straight into the next dungeon, rendering its
    icon at its old (Nexus-relative) position forever, with nothing able to interact with it since
    `LootBags` no longer references it. Fixed by having `ItemSpawner.Reset()` mark each bag
    `IsExpired = true` before clearing the list, so `EntityManager`'s own cleanup drops it on its
    next `Update()` regardless of whether the destination state resets `EntityManager` too — the
    same expire-then-let-EntityManager-clean-up mechanism every other despawn in the game already
    uses. Verified via a scripted repro (deliberately touching nothing on `Player.Instance`'s
    `Inventory`/`BankSystem`, calling no persistence method, after entry 42's incident): added a
    `LootBag` to both `EntityManager` and `ItemSpawner.LootBags`, called `ItemSpawner.Reset()`,
    confirmed `IsExpired` flipped `true` immediately, then called `EntityManager.Update()` and
    confirmed the entity count dropped by exactly one — the bag is actually gone, not just
    logically forgotten. *(15:57 EDT)*
40. **The sidebar's 7 stat lines (Level, ATT, DEF, SPD, DEX, VIT, WIS) had uneven vertical
    spacing** — the gap between Level and ATT was 20px while every other gap between lines was
    16px, a leftover from when these lines' `y` offsets were hand-tuned individually rather than
    derived from one consistent step. Fixed by normalizing every row to the same 16px step:
    `y + 0/16/32/48/64/80/96` instead of the previous `y + 0/20/36/52/68/84/100`. Verified visually
    by rendering `Overlay.DrawSidebar()` to a `RenderTarget2D`, translating the sidebar's `SidebarX`
    offset to 0 so the whole stat block fits in a small saved PNG, and inspecting it directly — all
    7 lines now read as evenly spaced, with the previously-oversized Level→ATT gap gone. *(13:11
    EDT)*
41. **The equipped-item tooltip in the sidebar's equip-slot row could be painted over by a
    different, later-drawn slot's icon** — the same draw-order bug class as entries 22 and 36
    (bank/inventory drag-ghost, loot bag), now found in a third spot. `Overlay.DrawEquipment()`
    calls each slot's `DrawEquipped()` in the order `Weapon, AbilityItem, Armor, Ring` — not the
    same order the four slots actually sit on screen (`Weapon, Armor, Ring, AbilityItem`, 40px
    apart) — and each `DrawEquipped()` drew its own border, icon, *and* hover tooltip all inline, in
    one call. Since `SpriteBatch` preserves draw-call order, a tall/wide tooltip for the (leftmost,
    drawn-first) Weapon slot could still be on screen when `AbilityItem` (drawn second, but
    positioned to the right of Armor/Ring visually) drew its own border+icon over it. Fixed the same
    way as entries 22/36: split each of `Weapon.cs`/`Armor.cs`/`Ring.cs`/`AbilityItem.cs`'s
    `DrawEquipped()` into two methods — `DrawEquipped()` now only draws the border/icon/placeholder,
    and a new `DrawTooltip()` draws the hover tooltip alone — then `Overlay.DrawEquipment()` calls
    all four `DrawEquipped()`s first, then all four `DrawTooltip()`s in a second pass (at most one
    actually draws anything, since only one slot can be hovered at a time), so a tooltip is always
    painted last regardless of which slot it belongs to. Verified via a scripted repro: equipped a
    Weapon with a deliberately long, multi-line description (image swapped to the opaque
    `Art.HealthBar` for a known raw color) and a plain `AbilityItem` (also `Art.HealthBar`),
    simulated hovering the Weapon slot by setting `Equipment`'s protected `hover` field directly via
    reflection, rendered the real `Overlay.DrawSidebar()`, and sampled the exact center pixel of the
    AbilityItem slot's icon — before the fix this would read back as the icon's own raw, unblended
    color; after the fix it read back visibly blended with the tooltip's translucent background tint
    (`(216,216,216)` vs. the icon's raw `(255,255,255)`), confirming the Weapon tooltip now draws on
    top of the AbilityItem slot instead of being erased by it. *(14:20 EDT)*
45. **F4's debug max-level-and-equip-top-gear equipped the correct top-tier Staff, but its shots flew
    in a straight line instead of weaving** — reported by the user right after the Staff weapon
    (entry 153 in [DEVLOG.md](DEVLOG.md)) shipped. `Player.cs`'s `EquipHighestTierWeapon()` (the
    method behind F4) builds its own fresh `Weapon copy` via an object initializer, separate from
    (and not kept in sync with) `Weapon.LoadWeapon()`'s own copy in the normal equip path — and that
    initializer was never updated when `Amplitude`/`Frequency` were added for Staff, so the F4 copy
    silently defaulted both to `0`. `Weapon.Shoot()`'s Staff branch passes `this.Amplitude`/
    `this.Frequency` straight into `SineWaveProjectile`'s perpendicular-offset formula
    (`amplitude * sin(...)`), so a `0` amplitude collapses the sine wave to nothing — the shots still
    fired, just dead straight, matching "equips the correct weapon but seems to break the projectile
    arc path." Fixed by adding `Amplitude = best.Amplitude, Frequency = best.Frequency,` to
    `EquipHighestTierWeapon()`'s object initializer, matching `LoadWeapon()`'s copy exactly. Verified
    via a scripted repro (throwaway `Wizard` instance, so `Player.Instance` briefly points at it but
    nothing persists — no persistence method is reachable from `EquipWeapon()`/`RecalculateStats()`):
    called `DebugMaxLevelAndEquipTopGear()`, confirmed the equipped weapon read `Amplitude=16,
    Frequency=2` (not `0`), called `Weapon.Shoot()`, and advanced both spawned `SineWaveProjectile`s
    5 ticks — their Y positions diverged by ~17.8 units (a real weave) versus what would have been a
    ~0 delta before the fix. Real save files were confirmed byte-identical before and after. Clean
    build and a plain boot-check both passed.

    Separately, while investigating this bug, `PlayerData_Wizard.json` on disk was found to no longer
    match the backup taken before this session's Staff-feature testing — the live Wizard's Level 3 /
    Fire Wand save had become a fresh Level 1 / Gnarled Staff save, with `InventoryData_Wizard.json`
    left untouched. The mechanism wasn't pinned down (plausibly `Weapon.LoadWeapon()`'s silent
    type-mismatch return during a real load of the old Wand-equipped save, though the code path read
    afterward doesn't fully explain a Level reset on its own). Flagged to the user directly rather
    than guessing or restoring unilaterally, both save states were preserved as backups, and the user
    confirmed the current Level 1 state should stand as-is — no data was altered as a result.
46. **The player's real basic-attack rate never matched any documented formula, and was measurably
    slower than even its own broken formula intended.** Reported as "double check the weapon/DEX
    attack speed calculation" — see [DEVLOG.md](DEVLOG.md) entry 164 for the full formula fix and
    rationale. Two distinct bugs surfaced specifically while verifying the replacement, both in
    `Player.cs`'s `Update()`: resetting the fire-rate accumulator to `0` instead of subtracting `1`
    discarded the overshoot fraction every cycle, undercounting real fire rate by ~7% at 50 Dexterity
    (confirmed via a 600-tick scripted simulation: 54 real shots fired where the formula calls for
    ~58.3); and accumulating that cooldown unconditionally, even while the player wasn't holding the
    attack button, let it bank up indefinitely while idle, so the very first click after any pause
    fired instantly regardless of Dexterity — not a Bugfixes-worthy issue on its own, but one that
    would have turned into a rapid-fire burst once the reset-to-`0` bug above was fixed to
    subtract-`1` instead. Both fixed together: accumulation now only happens while the player is
    actually trying to fire, and the leftover fraction carries forward via `-= 1f` instead of being
    discarded. Re-verified via the same 600-tick simulation: exactly 58 shots at 50 Dexterity (vs. the
    ~58.3 the formula predicts) and exactly 80 at 75 Dexterity (an exact match, since 8 attacks/sec
    divides the 60-tick second evenly with no rounding at all).
47. **The player's real movement speed never matched any documented formula either** — the same
    "double check the calculation" request as entry 46, this time for Speed instead of Dexterity. See
    [DEVLOG.md](DEVLOG.md) entry 165 for the full fix. `Player.cs`'s `Update()` computed Velocity
    magnitude as `(int)((Speed / 75) * 5.6 + 2)` — converted to tiles/sec it gave 3.75/9.375/13.125 at
    0/50/75 Speed, versus the intended 4.0/7.733/9.6, and the `(int)` cast on top threw away real
    precision for no reason (`Velocity`/`Position` are already `Vector2`/float, nothing needed an int).
    Replaced with a new `Player.TilesPerSecond` property (`4f + 5.6f * (Speed / 75f)`) and a
    float-only `pixelsPerTick = TilesPerSecond * 32f / 60f` conversion, no truncation anywhere.
    Verified via a scripted repro: confirmed `TilesPerSecond` reads exactly `4`/`7.7333336`/`9.6` at
    Speed `0`/`50`/`75`, and — since this environment can't simulate real keyboard input to measure
    travel through a real `Update()` tick — reproduced `Update()`'s exact conversion math with a
    synthetic direction vector and confirmed it round-trips back to precisely `TilesPerSecond`'s own
    value at all three Speed levels with zero rounding loss.
48. **Health and mana regen never matched any documented formula either** — the third and fourth
    "double check the calculation" request this session, after entries 46/47's Dexterity/Speed. See
    [DEVLOG.md](DEVLOG.md) entry 166 for the full fix. `Player.cs`'s `Update()` computed both regen
    rates with the same int-tick-count-and-reset-to-0 pattern already found broken twice: converted to
    HP/s and MP/s, the old formulas gave 0.375/3.75/7.12 HP/s and 0.1875/1.3125/1.875 MP/s at the
    spec's own example stat values, versus the intended 2.0/11.63/20.05 and 0.5/6.5/9.5. Replaced with
    `Player.HealthRegenPerSecond` (`2f + 0.2407f * Vitality`) and `ManaRegenPerSecond` (`0.5f + 0.12f *
    Wisdom`), and switched `healthCooldown`/`manaCooldown` to float accumulators that subtract `1f`
    (not reset to `0f`) on regen — applying entry 46's precision fix proactively this time, before
    shipping rather than after a report. Verified via a scripted repro: confirmed both properties read
    the spec's exact example values, then ran 600 real `Update()` ticks (10 seconds) with
    `HealthMax`/`ManaMax` raised so regen never hit the cap and confirmed real Health/Mana gained
    landed within a single unit of the formula's own prediction in both directions (the gap being pure
    integer quantization on the `int Health`/`Mana` fields, not drift).
49. **Attack's damage multiplier was pinned at 0.5 for the entire 0-49 Attack range, and Defense's
    10%-damage floor only existed for damage taken by the player, not damage dealt to enemies.** The
    fifth/sixth "double check the calculation" requests this session (after entries 46-48). See
    [DEVLOG.md](DEVLOG.md) entry 167 for the full writeup. `Weapon.cs`'s `Shoot()` computed
    `0.5 + Player.Instance.Attack / 50` — `Attack` is `int`, so `Attack / 50` evaluated as pure integer
    division before the `0.5` was ever added, making every Attack value 0-49 produce the identical
    `0.5` multiplier instead of scaling smoothly by 2% per point. Fixed with `/ 50.0`. Separately,
    `Player.cs`'s `Hit()` already correctly floored damage-to-the-player at 10% of the raw hit
    (`Math.Max(damage - Defense, damage / 10)`), but `Enemy.cs`'s `WasShot()` (damage the player deals
    to an enemy) only floored at `0` — a real asymmetry that let a sufficiently defended enemy become
    effectively untouchable, when the same stat guaranteed the reverse direction could never fully
    block a hit. Fixed by mirroring `Hit()`'s exact floor into `WasShot()` (skipped when
    `ignoresDefense` is set, e.g. Bow Side shots). Verified via a scripted repro: pinned weapon damage
    to a fixed value to eliminate randomness and confirmed the Attack multiplier read exactly
    0.5/1.0/1.5/2.0 at Attack 0/25/50/75 (previously 0 and 25 gave the identical, wrong 0.5); and
    reproduced the spec's own worked Defense examples exactly, including a deliberately-over-the-cap
    case (60 damage vs 90 Defense) that now correctly lands at 6 damage instead of 0.
50. **Knight's Shield Slam gave a flat +20 Defense buff instead of the spec's 75% damage-taken
    reduction, and its shot's range drifted with whatever Sword tier was equipped instead of holding
    a fixed 3.2 tiles.** Reported as "check the shield ability against these specs." See
    [DEVLOG.md](DEVLOG.md) entry 169 for the full writeup. `AddTemporaryDefenseBonus(20, 180)` reduced
    damage 1-for-1 like ordinary Defense (and only for 3s, not the spec's 5s) — a different mechanism
    entirely from a genuine damage-taken multiplier. New `Player.DamageTakenMultiplier` +
    `AddTemporaryDamageTakenMultiplier()`, applied directly in `Hit()` before Defense's own reduction,
    stacking with it rather than replacing it. Separately, the shot used `Weapon.ProjectileMagnitude`/
    `ProjectileDuration` (every Sword tier is `8`px/tick / `14` ticks) instead of its own fixed values,
    giving roughly double the spec's intended range regardless of equipped Sword tier. Fixed with
    Knight-local constants for the spec's own 16 tiles/sec / 0.2s / 3.2-tile numbers. Verified via a
    scripted repro: confirmed the ability sets the multiplier to exactly `0.75` and spawns a shot with
    the exact spec'd duration/speed, and confirmed two `Hit(100)` calls (one during the effect, one
    300 ticks after it expired) differed by exactly `25` HP — the real Defense present in both calls
    canceled out algebraically, leaving no ambiguity that the multiplier was the only variable.
51. **The Priest was unable to equip a Tome** — reported directly as "the priest is unable to equip a
    tome." See [DEVLOG.md](DEVLOG.md) entry 175 for the full writeup. Root cause:
    `InventorySystem.TryEquipFromRecord()`'s ability-item drag-drop swap `switch`ed on the dragged
    item's concrete type (`Spell`/`Quiver`/`Shield`) to decide which `LoadX()` factory to call, but
    was never updated with a `Tome` case when Tome was added — a Tome dragged onto the ability-item
    slot fell through to `_ => null` and silently did nothing. The exact same class of bug (a new
    `AbilityItem` subclass added without updating every old exhaustive switch over the other three)
    had already bitten this project multiple times before Tome even existed (entries 45, 154/156,
    170) — a full audit turned up three more instances of it specifically for Tome: `Item.cs`'s
    `[JsonDerivedType]` polymorphic-serialization list (missing `Tome` entirely — an unequipped Tome
    sitting in inventory/bank would throw `NotSupportedException` the moment it tried to save) and
    two loot-pool `Concat()` chains in `ItemSpawner.cs` (`Spawn()`'s regular per-kill pool and
    `SpawnGuaranteedLoot()`'s guaranteed pool), both missing `Game1.Instance.Tomes` — meaning Tomes
    could never drop as loot anywhere in the game, for any class. All four fixed together. Verified
    via a scripted repro: confirmed a real `InventorySystem.TryEquipFromRecord()` call with a dragged
    Tome now returns `true` and correctly swaps it onto a Priest's ability-item slot (with the
    previously-equipped Tome swapping back into the dragged record); confirmed a Tome round-trips
    through `JsonSerializer.Serialize`/`Deserialize` via its base `Item` type without throwing;
    confirmed a Tome can actually appear across 200 forced tier-0 `AbilityItem` rolls in both
    `Spawn()` and `SpawnGuaranteedLoot()` (statistically airtight given all 4 candidate types share
    equal odds). Real save files confirmed byte-identical before and after.
52. **The Wand's projectile didn't match its own spec** (18 tiles/sec, 0.5s lifetime, 9 tile range,
    piercing) — reported as "make sure the wand matches these specs." See
    [DEVLOG.md](DEVLOG.md) entry 177 for the full writeup. Piercing was already correct (Wand shots
    already skip `ExpiresOnHit`, same mechanism as Bow). Speed and lifetime weren't: all 15 Wand
    tiers in `Data/WeaponData.json` shared `ProjectileMagnitude: 12`/`ProjectileDuration: 32`
    (22.5 tiles/sec over 0.53s, a 12-tile range) instead of the spec's `9.6`/`30` (18 tiles/sec over
    exactly 0.5s, a clean 9-tile range with no rounding needed, unlike the Staff's own 0.475s spec
    from an earlier check this session). Fixed by updating all 15 tiers to `9.6`/`30`. Verified via a
    scripted repro: confirmed the catalog data directly, then fired a real `Weapon.Shoot()` off a
    Wand-wielding Priest and measured the actual spawned `Projectile`'s velocity magnitude and
    `Duration`, converting back to tiles/sec and seconds — landed within floating-point rounding of
    exactly 18/0.5/9, and confirmed `ExpiresOnHit` was already `false`. Real save files confirmed
    byte-identical before and after.
53. **`Enemy.FollowPlayer()` could freeze an enemy's position at `NaN` forever** if it and the player
    ever landed on the exact same `Position` — found while scripting a test for the new Beached
    Buccaneer mini-boss (entry 180), not reported by the user. See [DEVLOG.md](DEVLOG.md) entry 180
    for the full writeup. `ScaleTo()` divides by the vector's own `Length()`; a zero vector (enemy
    and player exactly coincident) divides by zero, and the resulting `NaN` propagates into `Velocity`
    then `Position` on that tick and every one after. Not reachable through ordinary spawning/movement
    (exact floating-point coincidence essentially never happens in real play) but shared by every enemy
    using `FollowPlayer()` — Seeker, Brute, Limon, and now Beached Buccaneer. Fixed with a one-line
    guard skipping the `ScaleTo()` call when the vector is already zero, a no-op for every other caller
    since none of them ever hit that case. Verified via the same scripted repro that surfaced it:
    confirmed a `BeachedBuccaneer` spawned exactly on the player's position no longer produces `NaN`
    `Position` after being ticked, and a follow-up offscreen render confirmed both its sprite and a
    nearby Pirate's render correctly (this was also the render pass that had been coming back
    completely blank before the fix — the guard was the actual cause, not a camera/z-order issue as
    first suspected).

54. **Any enemy taunt line containing "smart" typography (curly quotes, an em/en dash, a Unicode
    ellipsis) would crash the entire game the instant it tried to render** — found while scripting a
    test for the new Bandit Leader mini-boss (entry 181), not reported by the user; never reachable
    in prior taunts (Beached Buccaneer's, checked directly and confirmed clean) purely by luck of
    which characters got typed. `SpriteFont.MeasureString()`/`DrawString()` both throw
    `System.ArgumentException` on any character outside the font's glyph set, and neither
    `TauntBubble`'s constructor (which calls `MeasureString` via `Util.WrapText`) nor its `Draw()`
    (which calls both again directly) had any handling for it — there is no exception handling
    anywhere in the real game loop to catch a throw like this, so it would have taken down the whole
    process. The immediate trigger was a literal Unicode ellipsis ("…") in Bandit Leader's flee
    taunt, copied verbatim from the user's own spec text; `Art.HudFont` has no glyph for it. Fixed
    two ways: replaced that specific character with "..." (three ASCII periods) directly in
    `BanditLeader.cs`, and added a general-purpose `TauntBubble.SanitizeForFont()` sanitizer applied
    to all taunt text going forward — maps common smart-typography characters (curly quotes,
    em/en dashes, ellipsis) to their closest ASCII equivalent, then strips any remaining character
    the font still can't render via `SpriteFont.Characters.Contains()`, as a last-resort safety net
    rather than trusting every future hand-written taunt to stay ASCII-only. Verified via a
    dedicated scripted check constructing a `TauntBubble` with curly quotes and an ellipsis and
    confirming both the constructor and a full `Draw()` call complete without throwing.

## 2026-08-27

55. **Sand Devil (`SandDevil.cs`) could end up directly on top of the player, and its Circle phase
    could drift instead of tracing a clean ring** — reported by the user during play, both symptoms
    root-caused in the same `PhaseWatcher()` method by code inspection (no repro steps beyond "circle
    phase" and "spawning directly on the player"). (1) Chase phase's "wander erratically" sub-state
    (triggered within `CloseThreshold`, 2 tiles) drove `MoveRandomly()` — a blind random walk with no
    bias away from the player at all — so nothing stopped it from wandering further in instead of
    out, up to and including onto the player's own exact position; this is almost certainly what
    read as "spawning directly on the player" (nothing in `EnemySpawner.GetSpawnPosition()`'s own
    250-unit minimum distance could produce that once the enemy is already alive and moving). (2) The
    Circle phase directly sets `Position` onto a ring around the player every tick rather than
    accelerating there, but `Enemy.Update()` applies `Position += Velocity` right after that same
    tick's behaviour runs regardless of phase — so any Velocity left over from Chase (both
    `FollowPlayer()` and `MoveRandomly()` accumulate into it) kept bleeding into the ring position
    afterward, worst right at the Chase→Circle transition when residual Velocity is largest, making
    the circle drift instead of staying a clean ring. Fixed (1) by clamping `Position` back out to
    `CloseThreshold` from the player after each erratic-wander tick whenever it would otherwise end
    up closer, and (2) by zeroing `Velocity` every tick of the Circle phase (not just once at the
    transition), so the leftover-Velocity bleed can never contaminate the ring calculation. Verified
    via a temporary `Game1.StartGame()` test: force-set one Sand Devil's phase to Chase via
    reflection with `Position` set exactly onto the player's own position (the worst case) and ran 30
    `EntityManager.Update()` ticks, confirming its minimum distance to the player over that window
    stayed at ~63.4 units (matching the ~64-unit clamp target within float precision) instead of
    collapsing to 0; force-set a second Sand Devil's phase to Circle with a large simulated leftover
    Velocity `(50, 50)` and ran 30 more ticks, confirming its distance to the player stayed within
    ~7.6e-6 units of the exact 96-unit `CircleRadius` (float noise, no real drift) and that `Velocity`
    read back as exactly zero every single tick. Reverted the temp code (`git diff --stat Game1.cs`
    clean), deleted the scratch log, ran a final clean build + plain boot-check, and confirmed all
    real save files byte-identical to a pre-test backup. See [DEVLOG.md](DEVLOG.md) entry 214 and
    [BACKLOG.md](BACKLOG.md) (the open item this closes out). *(08:41 EDT)*
