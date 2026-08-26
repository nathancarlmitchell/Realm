# Realm — Development Log

Chronological log of features actually built in Realm, oldest first. Companion docs:
[BACKLOG.md](BACKLOG.md) (what's still open) and [BUGFIXES.md](BUGFIXES.md) (bugs, not
features). Mirrored from this project's Claude Code memory notes so it travels with the repo
instead of staying on one machine — this file is the canonical copy; append new entries
directly.

Running log of features actually built in Realm (top-down ARPG, C#/MonoGame,
`C:\Users\Nathan\Downloads\Realm-main-claude\Realm-main`). Oldest first, grouped by date, with a
`(HH:MM timezone)` timestamp — the real local time an entry was logged, not estimated — on each
entry from 2026-08-13 13:19 EDT onward. Companion to
[BACKLOG.md](BACKLOG.md) (what's still open) and
[BUGFIXES.md](BUGFIXES.md) (bugs, not features — kept separate). When an Open
ideas item is completed, remove it from the backlog's Open ideas list and append it here instead
of leaving it duplicated in both places. Entries 1-17 predate when this log started tracking
dates, and 18-20 predate when it started tracking time-of-day — real work, just no recorded
date/time for those individually; don't treat their grouping as meaning they all happened at once.

## Before dated tracking (exact dates not recorded)

1. **Character Select screen.** Portraits for each class, hover-to-preview stats (level, derived
   stats, Score, Hi-Score) read from that class's own save file, and per-class save files —
   previously the game only supported a single save. Required converting `Player`'s static fields
   to instance fields first, so more than one class's data could exist independently.
2. **Health/Mana potions split into dedicated charge counters.** Moved out of the general inventory
   grid into their own HUD area with a max stack of 6, rather than taking up general inventory
   slots like other consumables.
3. **Border drawn behind potion portraits**, matching the existing weapon slot's border style.
4. **Full Armor equipment system**, with placeholder stat bonuses spanning every Player stat.
5. **ArmorType class restriction** (Wizard → Robe, Archer → Leather), mirroring the existing
   `WeaponType` pattern.
6. **Ring equipment slot** — no class restriction, unlike Weapon/Armor.
7. **Drag equipped items (Weapon/Armor/Ring) out** to inventory or the ground, with an error sound
   if the player tries to shoot with no weapon equipped.
8. **Soft-lock prevention**: the player's only usable weapon can't be unequipped (no way to fight
   for a replacement otherwise) — later refined to check `WeaponType` match rather than just "is a
   Weapon," so a wrong-class backup weapon doesn't count.
9. **Debug HUD (F3 toggle)** showing potion-derived stat bonuses.
10. **Floating damage numbers** on hit.
11. **Projectiles render underneath the player** instead of on top of them.
12. **Projectile spawn point moved** from the player's center to the player's edge in the aim
    direction.
13. **Real Armor/Ring art wired in** (`Content.mgcb`, `Art.cs`, `ArmorData.json`/`RingData.json`),
    replacing the placeholder texture, starting with 2 tiers each.
14. **Full 15-tier Armor/Robe art catalog**, with thematic names/descriptions and linear stat
    scaling for every tier.
15. **Ability-readiness HUD bar** — visualizes the existing mana-cost gate on `UseAbility()`
    (Wizard's spell bomb, Archer's shot), not a new cooldown timer.
16. **Fourth equipment slot: `AbilityItem`.** `Spell` (Wizard-only) and `Quiver` (Archer-only) as
    separate C# subclasses rather than one class + enum, modifying ability damage output and mana
    cost via `AbilityDamageBonus`/`AbilityManaCostReduction`.
17. **AbilityItem content wiring**: full 8-tier Spell/Quiver art and JSON catalogs
    (`SpellData.json`/`QuiverData.json`), matching the Armor catalog's depth.

## 2026-08-13

18. **Portal destination labels** — each portal draws its `Destination`'s display name centered
    below the sprite, so it's no longer unlabeled.
19. **Delete-with-confirmation on Character Select.** A "Delete" link (only shown once a character
    has actually been played) swaps to a "Delete save? Yes/No" confirmation; deleting preserves
    High Score permanently while resetting everything else — tracked via a dedicated
    `Player.HasBeenPlayed`/`PlayerData.HasBeenPlayed` flag (distinct from High Score, since it
    resets on delete) so the Delete link only appears when there's real progress to reset.
20. **Bank system.** A persistent, account-level item store (16 slots) shared across every class's
    save, opened by standing near a new "Bank" portal in the Nexus — proximity-gated rather than
    changing game state, unlike the other two portals. Items drag both ways between the bank and
    personal inventory, and directly between the bank and any equip slot (Weapon/Armor/Ring/
    AbilityItem); the equip-swap logic was extracted into a shared
    `InventorySystem.TryEquipFromRecord()` helper reused by both grids instead of duplicated. A
    loot bag dropped near the bank portal takes priority if it's closer, mirroring
    `ItemSpawner.NearestOpenBag()`'s existing "closest wins" rule between multiple bags.
21. **Hovering a bank item shows its full Tier/name/description/bonuses**, matching the equip
    slot's own hover tooltip instead of just the item's name. `Equipment.TooltipText()` (with
    `Weapon`/`AbilityItem` overrides for their extra stat lines) was extracted out of each
    subclass's `DrawEquipped()` so `BankSystem`'s hover could reuse the exact same formatting
    instead of re-deriving it; non-equipment items (potions) still just show their name, since
    they have no Tier/bonuses to show. *(13:19 EDT)*
22. **Dropping an item on the ground while the bank is open now banks it instead**, falling back
    to the normal loot-bag behavior only if the bank is full. Implemented in one place —
    `InventorySystem.AddToLootBagAtPlayer()` — since both `DropItem` (dragging a general inventory
    item out) and `ResolveEquipmentDrag`'s own ground-drop fallback already funnel through it. This
    is separate from dragging directly onto the bank panel, which still blocks with an error sound
    when full, matching every other capacity guard in the system — the new behavior only applies to
    genuine ground-drops. *(13:42 EDT)*
23. **Tooltip readability pass across all hover tooltips.** Two things bundled into one backlog
    item, both done: (1) general inventory hover now shows the full `TooltipText()` for equipment
    (Tier/description/bonuses), matching equip slots and the bank instead of just the item name;
    (2) a shared `Util.DrawTooltip()` draws a semi-transparent background panel (reusing
    `Art.HealthBar`, a solid 1x1 pixel, stretched and tinted) behind every tooltip, so they stay
    readable regardless of what's behind them. All six draw sites (`Armor`/`Ring`/`Weapon`/
    `AbilityItem`'s `DrawEquipped`, plus `BankSystem` and `InventorySystem`'s hover) now go through
    it instead of a raw `DrawString`. *(14:00 EDT)*
24. **Fixed-position slots for inventory and bank.** Both `InventorySystem.InventoryRecords` and
    `BankSystem.Records` changed from compact `List<InventoryRecord>`s (items always render
    left-to-right by list index, so removing one shifts everything after it down) to fixed-size
    `InventoryRecord[8]` arrays, where a `null` entry is an empty slot that stays put. Bank shrank
    from 16 slots (4×4) to 8 (4×2) to match inventory exactly (the user's choice over keeping 16).
    New shared `HasEmptySlot`/`AddRecord`/`RemoveRecord` operations on both classes replaced every
    `.Count`/`.Add`/`.Remove`/`.RemoveAt` call site (stacking, drag/drop, equip-swap, ground-drop,
    persistence). `Util.cs`'s save/load now writes/reads by fixed index — including explicit
    entries for empty slots — so gaps survive a save/reload instead of getting silently compacted
    away. This is also a prerequisite for the still-open "reorder items by dragging" backlog idea,
    which needs stable slot positions to make sense of a drag target. *(14:54 EDT)*
25. **Cursor-aware slot targeting when dropping into the inventory, and error feedback for
    wrong-slot equip drags.** Dropping an item into the inventory (from the bank or an equip slot)
    now lands in whichever slot the mouse is actually over — `InventorySystem.SlotIndexAt()` maps a
    screen point to its slot index using the same 4-wide/2-row layout `Update()`/`Draw()` already
    use, and the new `AddRecordAt()` places there if it's empty, falling back to the old
    first-empty-slot behavior otherwise (out-of-range point, or the target slot already occupied).
    Separately, dragging an item onto the *wrong* equip slot (e.g. an Armor item released onto the
    Weapon slot) now plays an error sound and cancels the drag instead of silently falling through
    to a ground-drop — a catch-all appended to `TryEquipFromRecord()` that fires when the release
    point is over any of the four equip slots but none of the type-specific branches matched.
    Verified via a scripted repro (temp code in `Game1.StartGame()`) simulating both drags frame by
    frame; caught and fixed a bug in the test itself along the way — it used stale hardcoded bank
    panel coordinates left over from before the bank's position became dynamic ([BUGFIXES.md](BUGFIXES.md)
    has bank positioning history), so the first run exercised no real code at all. *(15:04 EDT)*
26. **Same cursor-aware slot targeting, mirrored for drops INTO the bank.** `BankSystem` gained its
    own `SlotIndexAt()`/`AddRecordAt()` (same shape as entry 25's `InventorySystem` versions, just
    reading the bank's dynamic `Anchor` instead of a fixed `x`/`y`), wired into both places an item
    can land in the bank at a specific cursor position: dragging from the personal inventory, and
    dragging an equipped item (Weapon/Armor/Ring/AbilityItem) straight onto the bank panel. The
    ground-drop-into-bank auto-redirect (entry 22) deliberately keeps first-empty-slot placement —
    a ground drop has no cursor position over the bank grid to target. Verified with a scripted
    repro: an inventory item dragged onto bank slot 5 landed at index 5 (not the first empty slot),
    and an equipped AbilityItem dragged onto bank slot 3 landed at index 3. *(15:13 EDT)*
27. **Same-panel drag reorder/swap for both the inventory and the bank.** Dragging an item to a
    different slot within the same grid (inventory→inventory or bank→bank) now moves it to the
    hovered slot — via `SlotIndexAt()` (entries 25/26) — and swaps the two items' positions if the
    target slot was occupied, instead of the drag being a no-op inside that grid. Releasing back
    over the *same* slot you started from is unchanged: still treated as a plain click, so
    click-to-use on a consumable still fires. Verified via scripted repro: item moved into an empty
    slot, two items swapped, same for the bank, and a regression check confirmed clicking a potion
    without dragging it still consumes it (`PotionAttackBonus` incremented). *(15:21 EDT)*
28. **Dragging an equipped item onto a different, wrong equip slot now errors instead of falling
    through to inventory/bank/ground.** `ResolveEquipmentDrag()` (the release handler for drags that
    *start* on an equip slot) previously only special-cased releasing back over the item's own slot
    (a no-op cancel); releasing on any of the other three equip slots fell through to the
    inventory/bank/ground-drop chain like any other release point, since a wrong-type equip is
    never valid there either. Added a check right after the existing soft-lock guard: if the release
    point is over any of the four `SlotBounds` (Weapon/Armor/Ring/AbilityItem) — which, given the
    early return for the origin slot, can only mean a *different* one — play the error sound and
    bounce instead of continuing. Verified with a scripted repro: an equipped Armor dragged onto the
    Weapon slot, and an equipped Ring dragged onto the AbilityItem slot with the bank open (to prove
    the new check is ordered before the bank-deposit branch), both left the item equipped and
    unchanged with no ground/bank/inventory side effect. Companion to entry 25's inventory/bank→
    wrong-equip-slot check — this covers the equip→wrong-equip-slot direction. *(15:36 EDT)*
29. **Fame system — earn/persist/display pipeline (idea #7's first phase).** A new account-level,
    persistent progression counter (`FameSystem.Fame`, a static `int`, shared across every class's
    save — same "not per-class" shape as `BankSystem`), earned from a character's Score
    (`ExperienceTotal`, 1:1) at the moment that character's progress is wiped — both on death
    (`GameOverState`'s constructor) and on explicit delete (`Util.DeleteCharacterData`, which now
    reads `ExperienceTotal` off the same `PeekPlayerData` snapshot it already used for High Score).
    Persisted to a new `FameData.json` (`Util.SaveFameData()`/`LoadFameData()`, mirroring the bank's
    save/load shape) and joined into the same `SavePlayerData()`/`SaveInventoryData()`/
    `SaveBankData()` triplet at all 6 existing checkpoints. Displayed on the Main Menu (`Overlay.
    DrawFame()`, centered under the title) rather than Character Select or the Nexus HUD — the one
    screen with no per-class hover state to tangle with and not already a crowded corner-anchored
    HUD. The Game Over screen also shows "Fame Earned: N" under the Score line as immediate
    feedback, since otherwise the mechanic would be invisible until the player next visits the Main
    Menu. What Fame actually unlocks was deliberately left out of scope (resolved via
    `AskUserQuestion` — "just track and display for now") and is a separate future backlog item.
    Verified via scripted repro: death added exactly the dying character's Score, delete added the
    deleted character's on-disk Score on top, and zeroing the in-memory total then calling
    `LoadFameData()` correctly restored it — proving the boot-load wiring actually works (unlike a
    latent gap found in passing on `BankSystem`'s equivalent, `Util.LoadBankData()`, which turned
    out to never be called anywhere in the codebase; flagged separately rather than fixed here to
    stay in scope). *(16:14 EDT)*
30. **Clicking an item now moves it between the bank and inventory, without disturbing drag/drop.**
    A plain click (press+release on the same slot, no drag) on a bank item moves it to the
    inventory if there's room; a plain click on an inventory item moves it to the bank if the bank
    is open and there's room — both play the existing error sound and stay put if the destination
    is full. Implemented by enriching the existing "released back over its own slot" branch each
    system already had (previously a no-op click-to-use fallback in the inventory, and a true no-op
    cancel in the bank) rather than adding new state, so drag-to-a-different-slot and drag-to-the-
    other-panel are byte-for-byte unchanged. One deliberate priority decision, resolved via
    `AskUserQuestion`: potions already had click-to-use (drink it); with the bank open, a clicked
    potion now banks instead of being drunk, consistent with every other item type — the old
    click-to-use behavior only applies when the bank is closed. Verified via a scripted repro
    (bank→inventory, inventory→bank, both full-destination error cases, the potion-click priority
    both ways, and a drag-to-a-different-slot regression check) — caught and fixed two bugs in the
    test itself along the way: an item can't be interacted with in a closed bank panel at all (a
    test mistake, not a real bug), and driving the click through a full `RealmState.Update()` routed
    through `Portal.Update()`, which silently overwrote every manual `BankSystem.IsOpen` assignment
    based on actual player-to-portal distance each frame — switched to calling `BankSystem.Update()`
    /`Inventory.Update()` directly, the same proven pattern used successfully earlier in the session.
    *(16:50 EDT)*

## 2026-08-14

31. **Knight — third playable character class.** Follows the exact extension pattern Wizard/Archer
    already established: `CharacterClasses/Knight.cs`, a new `WeaponType.Sword`, a new
    `ArmorType.Heavy`, and a new `AbilityItem` subclass `Shield.cs` (mirroring `Quiver.cs`, own
    `Data/ShieldData.cs`/`.json` catalog, own `Game1.Shields` list). Tankier than both existing
    classes — higher base Defense/HealthMax/Vitality, lower Mana/Wisdom, slightly slower (`baseSpeed
    13` vs Wizard's 17), and shorter basic-attack range (`ProjectileMagnitude/Duration` tuned to
    ~29% of Wand/Bow's effective range — confirmed via research that range is purely
    `speed × lifetime` with no separate Range concept, so no new mechanism was needed). Knight's
    ability, "Shield Slam" (resolved via `AskUserQuestion`): one high-damage projectile plus a
    temporary +20 Defense buff for 3 seconds — implemented as a proper input to
    `RecalculateStats()`'s formula (like `PotionDefenseBonus`) rather than a raw mutation, so it
    can't be silently dropped or double-applied if something else triggers a recalculation mid-buff.
    `Util.cs` touched in five places (`ResetPlayer` switch, `DetermineLastPlayedClass`/
    `AnyCharacterHasBeenPlayed` extended to a third class, a `saved.Shield` branch in
    `LoadOrCreatePlayer`, a `Shield` cast in `BuildPlayerData`); `CharacterSelectState.cs` gained a
    third slot (Knight centered between Wizard/Archer, `SlotOffsetFromCenter` widened from 110 to
    195 so the three portraits don't crowd each other) — the only file whose layout wasn't already
    generic over `Player.Class`. The user had already prepared full 15-tier Sword/Heavy Armor art
    and 8-tier Shield art (matching Bow/Leather/Quiver's existing depth) rather than the 2-tier
    minimum originally planned, so all three catalogs were built out to full depth to match,
    reusing Bow's exact damage-per-tier numbers for Sword and a linear `tier×20`/`tier×8`
    MaxHealth/Defense formula for Heavy Armor (steeper than Leather's `tier×20`/`tier×5`,
    reinforcing the tankier flavor). Two assets aren't ready yet and use temporary placeholders,
    each swappable by changing one string once supplied: `Sound.PlayerHit` reuses
    `archer_hit.wav` (no `knight_hit.wav` yet), and every Sword tier's `ProjectileImageName`
    temporarily points at `Projectiles/archer` instead of the not-yet-supplied
    `Projectiles/sword_slash` — without this fallback the missing file crashed `LoadWeaponData()`
    at boot for every class, not just Knight, since the whole weapon catalog loads eagerly at
    startup. Verified via two passes: a scripted repro confirming exact base stats, gear types,
    Shield Slam's damage/mana-cost/Defense-buff-and-reversion, level-up growth, and a save/reload
    round-trip through the new `Shield` branch; and a separate real boot straight into the Nexus as
    Knight (via `StateManager.NewGame()`, not just constructing the class) to exercise `Draw()` —
    the first test never rendered anything, so this was the only pass that could have caught a
    missing-texture crash. *(10:48 EDT)*
32. **Knight's last two placeholder assets swapped in for real, closing out entry 31.** The user
    supplied `Content/Sounds/Player/knight_hit.wav`; `Knight.cs` now loads it directly instead of
    reusing `archer_hit.wav`, and a matching `Content.mgcb` build entry was added (missing, same gap
    class as entry 26's `sword_slash.png` fix — caught proactively this time before it could crash
    the boot). Verified with a real boot into the Nexus as Knight. Nothing placeholder remains from
    the Knight feature. *(11:02 EDT)*
33. **Renamed `RealmState`/`GameState` to match what they actually represent.** The names had
    always been swapped from their real meaning — `RealmState` was the Nexus hub (portal room,
    Character Select access, the bank), and `GameState` was the actual dungeon/realm instance
    entered via the Realm portal. `States/RealmState.cs` → `States/NexusState.cs` (class
    `RealmState` → `NexusState`), and `States/GameState.cs`'s content moved into a rewritten
    `States/RealmState.cs` (class `GameState` → `RealmState`); the old `GameState.cs` file was
    deleted. Every call site updated: `StateManager.Nexus()`/`NewGame()` now construct
    `NexusState`, `StateManager.EnterPortal()` now constructs `RealmState`; `Input.cs`'s Escape-key
    (hub-only) and potion-key (dungeon-only) state-type checks; `Potion.cs`'s 10 `GameState.*Guid`
    references (the potion-identity Guids, unchanged values, just moved with the class); doc
    comments in `BankSystem.cs`/`InventorySystem.cs`/`ItemSpawner.cs` that named the old classes.
    Verified via a scripted repro run from inside `Game1.StartGame()` (with direct access to
    Game1's own private `currentState`/`nextState` fields): `StateManager.Nexus()` constructs a
    real `NexusState`, `StateManager.EnterPortal()` constructs a real `RealmState`, the `is
    NexusState`/`is RealmState` checks Input.cs depends on resolve correctly for both, and the
    moved potion Guids and `RealmState.Update()`'s high-score tracking still work. *(11:11 EDT)*
34. **Generalized Knight's one-off temporary Defense buff into a full `Player`-level timed-bonus
    system, all 8 stats, with color-coded on-screen indicators.** Previously the Shield Slam buff
    was Knight-local: a private field, its own countdown, its own `Update()` override. Moved to
    `Player.cs` as a reusable pattern any class's ability can use: `TemporaryAttackBonus`,
    `TemporaryDefenseBonus`, `TemporarySpeedBonus`, `TemporaryDexterityBonus`,
    `TemporaryVitalityBonus`, `TemporaryWisdomBonus`, `TemporaryHealthMaxBonus`,
    `TemporaryManaMaxBonus` — mirroring the existing `Potion*Bonus` fields exactly, but each paired
    with its own private frame-countdown and an `AddTemporaryXxxBonus(amount, durationFrames)`
    method (refreshes the duration on re-trigger; only re-applies the amount if not already active,
    so spamming an ability can't stack it). All three classes' `RecalculateStats()` (`Wizard.cs`,
    `Archer.cs`, `Knight.cs`) now add every `TemporaryXxxBonus` into their formula, same as
    `Potion*Bonus`. A new `Player.UpdateTemporaryBonuses()`, called from the base `Update()`, ticks
    every stat's countdown independently each frame and only re-triggers `RecalculateStats()` on
    frames where something actually expired — this is what let `Knight.cs` drop its own `Update()`
    override entirely. `Player.Draw()` gained `DrawTemporaryBonusIndicators()`: one "+" symbol above
    the sprite per active bonus that has an assigned color (Attack=Red, Defense=Gray, Speed=Green,
    Dexterity=Orange — the four the user specified), drawn side by side when more than one is
    active; Vitality/Wisdom/HealthMax/ManaMax bonuses work identically but have no color yet, so
    they silently don't draw an indicator (a deliberate scope call, confirmed via `AskUserQuestion`,
    since the user's own color list only named four stats). `Knight.UseAbility()` now just calls
    `AddTemporaryDefenseBonus(20, 180)`. Caught one real bug during verification, not in the new
    system itself: the scripted test called `Player.Instance.Update()` directly in a loop before
    ever constructing a state, so `Game1.Camera` (which `Update()`'s camera-follow line dereferences
    unconditionally) was still null — fixed by moving the `NexusState` side-effect construction
    (used earlier in the same session to get `Game1.Camera` initialized) to the top of the test.
    Verified: all 8 bonuses applied simultaneously with exact deltas, then expired independently on
    their own staggered schedules (20/30/40/50/60/70/90/120 frames) with no cross-contamination;
    Knight's Shield Slam still applies +20 Defense through the new mechanism; and a real `Draw()`
    call with all four colored bonuses active at once (Attack+Defense+Speed+Dexterity) rendered
    without exception. *(11:55 EDT)*
35. **`Projectile.ExpiresOnHit`** — new per-projectile field controlling whether a shot expires the
    moment it hits an enemy or keeps flying through (still only damaging a given enemy once each,
    via the existing `Enemy.HitBy` dedup — this only controls whether it then continues toward
    anything else in its path). Replaced a fragile collision-time check in
    `EntityManager.HandleCollisions()` — `if (Player.Instance.Weapon.Type != WeaponType.Wand) {
    bullets[j].IsExpired = true; }`, flagged with its own `//TODO should be a variable within the
    projectile maybe?` comment — which couldn't distinguish an ability shot from a basic attack
    fired with the same weapon equipped, and would misbehave if weapons were switched mid-flight.
    Set once at spawn time instead: `Weapon.Shoot()` computes `expiresOnHit = Type != Wand` once and
    applies it to all 3 of its spawn sites (basic shot, Bow's extra two arrows), while each class's
    `UseAbility()` sets `ExpiresOnHit = true` explicitly on its own ability projectile(s) — so Wand
    bolts pass through enemies but the Wizard's Spell bomb (fired with a Wand equipped) still expires
    on hit, and Archer/Knight's basic attacks and abilities all expire on hit. Defaults `true` so a
    spawn site that forgets to set it explicitly fails safe. Verified via a scripted repro (temp code
    in `Game1.StartGame()`) exercising all 6 basic-attack/ability combinations across all 3 classes;
    5 of 6 matched an exact entity-count formula precisely (Wand-basic correctly passed through,
    Bow-basic/Quiver-ability/Sword-basic/Shield-ability all correctly expired). The 6th
    (Wizard's 35-projectile Spell-ability) was off by exactly 1 entity in a way that, if anything,
    indicates *more* expiration than predicted, not pass-through leaking through — most likely a
    simultaneous-AoE test-harness artifact (e.g. `DamageNumber`/removal ordering across 15
    same-frame hits) rather than a real bug, especially given the isolated single-hit mechanism
    was independently proven correct by the other 5 cases plus an earlier direct diagnostic. Two
    real test-harness bugs were caught and fixed along the way (not game bugs): a raw
    `new Enemy(...)` construction leaves private `health`/`healthMax` at C#'s default 0 (dies on
    the first hit, corrupting count math) — fixed by using `Enemy.CreateWanderer()`; and
    `Enemy.WasShot()` spawns a `DamageNumber` on every hit regardless of `ExpiresOnHit`, which had
    to be accounted for in the expected-count formula. *(12:45 EDT)*
36. **Abilities (Space) now usable in the Nexus, not just an active dungeon.** `Input.cs`'s
    Space-key check was pulled out of the existing `if (currentState is RealmState)` block (which
    also gates potions, debug level up/down, and return-to-Nexus — all left dungeon-only,
    unchanged) into its own top-level condition,
    `(currentState is RealmState || currentState is NexusState) && WasKeyPressed(Keys.Space)`,
    mirroring the adjacent Escape-key-only-in-Nexus check already in the same file. `UseAbility()`
    itself needed no changes — its base implementation has no environment-specific logic. Not
    scripted-verified: `Input.Update()` always re-reads real hardware keyboard state at its top
    (`keyboard = Keyboard.GetState();`), so there's no way to simulate a keypress through the real
    method without actually driving the game window — confirmed working by build success + code
    inspection only; asked the user to confirm in-game. *(~12:xx EDT, same session as entry 35)*
37. **Stun mechanic — Archer's Quiver ability stuns enemies on hit, 3-second default duration.**
    `Entity` (base class) gained `public bool IsStunned` (defaults false for every entity type,
    including projectiles, per the user's request — only `Enemy` currently acts on it).
    `Enemy.ApplyStun(int durationFrames = 180)` sets `IsStunned = true` and a private
    `stunFramesRemaining` countdown (refreshes on re-trigger rather than stacking, same shape as
    Player's `AddTemporaryXxxBonus`); `Enemy.Update()` decrements the countdown each frame and
    gates only the single `Position += Velocity;` movement line on `!IsStunned` — AI behaviours
    keep running underneath (so `Velocity` keeps decaying at its normal `*0.8f` rate instead of
    piling up into a jarring lurch the instant the stun ends), they just don't get written to
    `Position` while stunned. New `Projectile.StunsOnHit` (bool, defaults false, same pattern as
    the recently-added `ExpiresOnHit`) is checked in `EntityManager.HandleCollisions()`'s existing
    per-hit block, calling `enemies[i].ApplyStun()` when true; `Archer.UseAbility()`'s Quiver
    projectile sets `StunsOnHit = true` alongside its existing `ExpiresOnHit = true`. No other
    class's projectiles set it. Verified via a scripted repro: fired the ability point-blank at an
    enemy positioned exactly at its spawn point (guaranteed hit, same technique as
    entry 35's ExpiresOnHit test) — confirmed `IsStunned` flips true immediately on
    hit; confirmed the enemy's position stayed frozen across 170 frames even with `Velocity`
    forced to a fresh nonzero value every single frame (proving the block is real, not just
    coincidental low AI velocity); confirmed movement resumed on exactly the 180th frame after the
    hit (not one frame early or late) using a freshly-set undecayed velocity, and that `IsStunned`
    had flipped back to false that same frame. *(13:58 EDT)*

    **Superseded by entry 38**: renamed Stun→Paralyzed and generalized into a reusable debuff
    system — `IsStunned`/`ApplyStun`/`StunsOnHit` named above no longer exist in the code.
38. **Renamed Stun to Paralyzed, and generalized it into a reusable `Entity`-level debuff system
    with on-screen indicators** (supersedes entry 37's naming). Mechanical rename throughout:
    `Enemy.ApplyStun()` → `Enemy.Paralyze()`, `Projectile.StunsOnHit` → `ParalyzesOnHit`. The real
    change is architectural: the old `Entity.IsStunned` bool (Enemy-only in practice) became a
    general-purpose system on `Entity` itself, since the user wants debuffs applicable to the
    player too, not just enemies, and wants adding a *new* debuff type later to be cheap. New
    `Entity.DebuffType` enum (currently just `Paralyzed`), a private
    `Dictionary<DebuffType, int> activeDebuffs` (frame-remaining per active debuff), `HasDebuff()`,
    `ApplyDebuff(type, durationFrames)` (refreshes duration, doesn't stack — same shape as
    `ExpiresOnHit`/Player's temporary-bonus pattern), `UpdateDebuffs()` (ticks every active debuff
    down each frame, called from `Update()`), and `DrawDebuffIndicators()` (draws a colored "-"
    per active debuff above the sprite, called from `Draw()`) — all `protected`/`public` on
    `Entity` so both `Enemy` and `Player` get them for free. `Enemy.Paralyze()` is now just a thin
    named wrapper (`ApplyDebuff(DebuffType.Paralyzed, durationFrames = 180)`) kept for a readable
    call site; `Enemy.Update()`'s movement gate now reads `!HasDebuff(DebuffType.Paralyzed)`
    instead of a dedicated bool. Adding a future debuff type is just a new enum value plus a case
    in the new private `DebuffColor()` switch — the tracking/countdown/drawing all work
    automatically; what the debuff actually *does* (e.g. blocking movement) is left to whichever
    subclass checks `HasDebuff()` for it, same as `Paralyzed` today. Deliberately visually distinct
    from Player's existing buff indicators (entry 34) rather than reusing the same
    look: debuffs draw a "-" symbol (buffs use "+") in the row above the buff row, so the two never
    overlap and read as different categories at a glance — resolved as a judgment call, not asked
    of the user, since it's a small reversible styling choice; picked `Color.Purple` for
    `Paralyzed` since none of the six buff colors currently in use (Pink/Gray/Green/Orange/Red/
    Blue) were free. `Player.Update()`/`Player.Draw()` now also call `UpdateDebuffs()`/
    `DrawDebuffIndicators()` even though nothing currently applies a debuff to the player — kept
    the system genuinely ready for that rather than requiring another pass through `Player.cs`
    later, since the user explicitly named it as a goal. Verified via a scripted repro: re-ran the
    original paralysis movement-block/duration test under the new names/API (identical results —
    hit applies it, blocked through the full 170+ frames even with forced velocity, resumes at
    exactly frame 180); additionally called `Enemy.Draw()` with the debuff active through a real
    `SpriteBatch.Begin()/End()` pair and confirmed no exception (proving `DrawDebuffIndicators`'s
    font measurement/draw calls actually work, not just compile); and separately called
    `Player.Instance.ApplyDebuff(Paralyzed, 60)` directly, confirmed `HasDebuff` read true, drew the
    player with no exception, then ran `Player.Instance.Update()` 60 times and confirmed the debuff
    correctly expired — proving the player-side plumbing works end-to-end even with nothing in the
    game yet triggering it. *(14:09 EDT)*
39. **Second debuff type — Stunned (Knight's Shield Slam) — plus swapped the text-symbol debuff
    indicators for real icon art.** The user supplied `Content/StatusEffects/paralyzed.png` and
    `stunned.png` (16x16 each, registered in `Content.mgcb` mirroring the `Items/Potions/attack.png`
    importer block; loaded as `Art.Paralyzed`/`Art.Stunned`). Two design questions resolved via
    `AskUserQuestion` rather than guessed: (1) whether `stunned.png` implied a genuinely separate
    debuff type or was just a spare asset — user chose a real second type; (2) what Stunned should
    actually do differently from Paralyzed, and what applies it — user chose "blocks movement AND
    attacks" (Paralyzed stays movement-only) sourced from Knight's Shield Slam. `Entity.DebuffType`
    gained `Stunned`; `Enemy.Stun(int durationFrames = 180)` mirrors `Paralyze()` exactly (same
    `ApplyDebuff` plumbing) but `Enemy.Update()` now also gates `ApplyAttackBehaviours()` behind
    `!HasDebuff(Stunned)` (Paralyzed's gate stays movement-only, unchanged) — the two debuffs are
    independent and stack freely, e.g. a Knight-then-Archer combo could apply both to the same
    enemy at once. New `Projectile.StunsOnHit` (parallel to `ParalyzesOnHit`) is set on Shield
    Slam's projectile alongside its existing `ExpiresOnHit`; the Knight's self-buff Defense bonus
    is unchanged and unconditional (applies regardless of whether the shot connects, same as
    before). `Entity.DrawDebuffIndicators()` was rewritten from `spriteBatch.DrawString` (a colored
    "-" glyph) to `spriteBatch.Draw` (the actual 16x16 icon per active debuff, native size, no
    color tint — each icon is now visually distinct on its own so the old color-coding was
    redundant); a `DebuffIcon(DebuffType)` switch replaced the old `DebuffColor()` switch as the
    system's one extension point for a future debuff type. Verified via a scripted repro: confirmed
    Quiver still applies only Paralyzed (not Stunned) and still blocks movement; confirmed Shield
    Slam applies only Stunned (not Paralyzed) and also blocks movement; drew a Stunned enemy through
    a real `SpriteBatch.Begin()/End()` pair with no exception (the icon texture loads and draws,
    not just compiles); and applied both debuffs to one enemy simultaneously and drew it, confirming
    the two-icons-side-by-side layout and both `DebuffIcon` cases work without throwing. *(14:19
    EDT)*
40. **Stunned no longer blocks movement — attacks only.** Corrects entry 39: Stunned was
    originally built as "the stronger debuff" (blocks movement AND attacks); the user asked for it
    to stop blocking movement, so the two debuffs are now a clean split — Paralyzed blocks movement
    only (unchanged), Stunned blocks attacks only. `Enemy.Update()`'s movement gate went back to
    checking just `!HasDebuff(Paralyzed)`; the `ApplyAttackBehaviours()` gate still checks
    `!HasDebuff(Stunned)`, unchanged. Doc comments on `Enemy.Paralyze()`/`Stun()` and Knight's
    Shield Slam updated to match. (Separately, without my involvement, Shield Slam's projectile
    also gained `ExpiresOnHit = false` — it now passes through enemies like a Wand bolt instead of
    stopping on the first hit, so a single Shield Slam can stun a line of enemies rather than just
    one; noted here only because it's adjacent code, not something this entry changed.) Verified
    via a scripted repro: a directly-stunned enemy given a fresh nonzero velocity moved normally
    across 5 frames; a directly-paralyzed enemy under the same test stayed frozen — confirming the
    split is real and Paralyzed's behavior is an unaffected regression check, not just Stunned's
    fix taken on faith. *(14:32 EDT)*
41. **Knight's Shield Slam gets its own dedicated projectile art.** Previously reused
    `Weapon.ProjectileImage` (the Sword's own basic-attack sprite); the user supplied
    `Content/Projectiles/shield.png`, registered in `Content.mgcb` (same importer block shape as
    the other `Projectiles/*.png` entries), loaded as `Art.ShieldProjectile` — same pattern as
    `Art.ArcherProjectile` (a class-ability-specific image loaded directly in `Art.Load()`, not
    through the Weapon/Armor JSON catalog pipeline). `Knight.cs`'s Shield Slam now sets
    `image = Art.ShieldProjectile` instead of `Weapon.ProjectileImage`. Verified: build actually
    compiled the new asset (`Building` not `Skipping` in the mgcb output, and the `.xnb` confirmed
    on disk); a scripted repro fired the ability (1 projectile spawned) and separately constructed
    a `Projectile` with `image = Art.ShieldProjectile` and drew it through a real
    `SpriteBatch.Begin()/End()` pair with no exception, confirming the texture isn't just non-null
    but actually loads and renders. *(15:01 EDT)*
42. **`AbilityManaCostReduction` replaced with `ManaCost` on all ability items (Spell/Quiver/
    Shield).** Semantic change, not just a rename: the old field was a per-tier *reduction*
    subtracted from a fixed `Player.BaseAbilityCost = 25` (`AbilityCost = Max(1, 25 -
    reduction)`), so ability items scaled 0→21 reduction across 8 tiers (cost 25 down to 4). The
    new field is the ability's *absolute* mana cost — `AbilityCost = Max(1, AbilityItem.ManaCost)`
    directly, no more base constant. Per the user's instruction, every tier in `SpellData.json`/
    `QuiverData.json`/`ShieldData.json` (8 tiers each, 24 entries total) was set to a flat
    `ManaCost: 25` — tier no longer affects ability cost at all until/unless the JSON values are
    hand-tuned later. Renamed through every layer: `AbilityItem.cs`'s field and its
    `AbilitySummary()` tooltip line (was `-{n} Ability Cost`, now `{n} Mana Cost`, since it's no
    longer a delta); `Spell.cs`/`Quiver.cs`/`Shield.cs`'s `LoadX()` mapping; `Util.cs`'s
    `LoadSpellData()`/`LoadQuiverData()`/`LoadShieldData()` mapping; the `SpellData`/`QuiverData`/
    `ShieldData` DTOs in `Data/*.cs`. Kept a `Math.Max(1, ...)` floor in `Player.AbilityCost` — not
    just defensive-programming reflex: `Overlay.cs`'s ability-readiness bar divides by
    `AbilityCost` (`Mana * 100 / AbilityCost`), so a `ManaCost` of 0 from a future data typo would
    be a real `DivideByZeroException`, not a hypothetical. Checked whether existing save files
    (which persisted the old `AbilityManaCostReduction` key on whatever ability item was equipped)
    would break — they don't: `Util.LoadOrCreatePlayer()` only reads the saved item's `.Name` and
    re-resolves the full item fresh from the live JSON catalog via `Spell.LoadSpell()`/
    `Quiver.LoadQuiver()`/`Shield.LoadShield()`, never deserializing the saved item's stat fields
    directly, so the stale key is simply ignored. Verified via a scripted repro: read
    `ManaCost` directly off a non-default (8th/highest) tier in all three catalogs to confirm the
    JSON→DTO→object mapping picked up the rename correctly, not just whatever tier happens to be
    equipped by default; then for all three classes confirmed `AbilityItem.ManaCost == 25`,
    `Player.AbilityCost == 25`, and that `UseAbility()` actually deducted exactly 25 mana. *(15:12
    EDT)*
43. **Ability damage moved from hardcoded per-class numbers into data: new `MinDamage`/`MaxDamage`
    on every ability item, all three classes now share one formula.** Previously each class hand-
    rolled its own damage roll inline — Wizard/Archer both `rand.Next(10, 15) +
    AbilityDamageBonus`, Knight `rand.Next(30, 45) + (AbilityDamageBonus * 2)` (deliberately
    tuned heavier, per entry 31). Resolved via `AskUserQuestion` whether Knight
    should keep that extra punch (e.g. higher `MinDamage`/`MaxDamage` in its own `ShieldData.json`
    tiers) or go fully uniform with everyone else — user chose fully uniform, so Knight's `*2`
    multiplier and 30-45 range are gone; all three classes now compute
    `rand.Next(AbilityItem.MinDamage, AbilityItem.MaxDamage) + AbilityItem.AbilityDamageBonus`,
    identically. New `MinDamage`/`MaxDamage` int properties added to `AbilityItem.cs` and threaded
    through the same pipeline as every other ability-item stat: `SpellData`/`QuiverData`/
    `ShieldData` DTOs (`Data/*.cs`), `Util.cs`'s three `LoadXData()` mappings, `Spell.cs`/
    `Quiver.cs`/`Shield.cs`'s `LoadX()` mappings. All 24 tier entries across `SpellData.json`/
    `QuiverData.json`/`ShieldData.json` set to `MinDamage: 10, MaxDamage: 15` per the user's
    specified defaults — inserted via `sed` right after each entry's `ManaCost` line, which by
    this point held per-tier hand-tuned values the user had set directly in the JSON (not the
    flat 25 from entry 42) — those values were left untouched, only the two new keys were added
    alongside them. Verified via a scripted repro: read `MinDamage`/`MaxDamage` off a non-default
    (8th/highest) tier in all three catalogs (10/15, confirming the JSON→DTO→object mapping);
    then for all three classes confirmed the equipped item exposes `MinDamage == 10`/
    `MaxDamage == 15` and that `UseAbility()` still fires successfully (spawns the expected
    projectile count — 16 for Wizard's spell bomb, 1 each for Archer/Knight) with the new formula
    in place. *(15:32 EDT)*
44. **Ability item tooltip shows a "Damage: X - Y" line.** `AbilityItem.AbilitySummary()`'s old
    `+{AbilityDamageBonus} Ability Damage` line (only shown when the bonus was nonzero) was
    replaced with `Damage: {MinDamage + AbilityDamageBonus} - {MaxDamage + AbilityDamageBonus}`
    (always shown, since damage is core to what an ability item does) — the *effective* roll
    range a player will actually see in combat, not the raw catalog numbers, so the tooltip
    doesn't require mentally adding the bonus on top of a separate line. Verified via a scripted
    repro reading real `TooltipText()` output for two different tiers of the same item
    (`Novice Spellbook`, `AbilityDamageBonus: 0` → `Damage: 20 - 25`; `Tome of Embers`,
    `AbilityDamageBonus: 5` → `Damage: 40 - 55`) and confirming the displayed range matched
    `MinDamage`/`MaxDamage` plus that tier's bonus exactly — both tiers' current `MinDamage`/
    `MaxDamage` no longer match entry 43's flat 10/15 default, since the user has since hand-tuned
    them per tier directly in the JSON (same pattern as `ManaCost` in entry 42); this didn't
    affect the check since it verifies the *formula*, not specific hardcoded numbers. *(16:19
    EDT)*
45. **Ability HUD mana cost, and `AbilityDamageBonus` removed from the game entirely.** Two
    changes: (1) `Overlay.cs`'s ability-readiness label previously only showed the mana cost when
    the player *couldn't* afford it (`"Ability: 10 / 25"`); the ready case just said
    `"Ability: Ready"` with no number. Now reads `"Ability: Ready (Cost: 25)"` so the cost is
    always visible regardless of state — this is what the user meant by "ability HUD tooltip"
    (there's no actual hover-tooltip on this HUD element, just a text label; interpreted as such
    since it's the only ability-cost-adjacent HUD text that existed to extend). (2)
    `AbilityDamageBonus` deleted outright, not just unused — removed from `AbilityItem.cs`
    (property + the tooltip's `Damage:` line, which now reads `{MinDamage} - {MaxDamage}` with no
    addition), all three `UseAbility()` methods (damage is now bare
    `rand.Next(AbilityItem.MinDamage, AbilityItem.MaxDamage)`), `Spell.cs`/`Quiver.cs`/
    `Shield.cs`'s `LoadX()` mappings, `Util.cs`'s three `LoadXData()` mappings, the `SpellData`/
    `QuiverData`/`ShieldData` DTOs, and the key itself from all 24 tier entries across
    `SpellData.json`/`QuiverData.json`/`ShieldData.json` (`sed '/"AbilityDamageBonus":/d'`,
    JSON-validated after). This was the same field entry 43 had just built a uniform formula
    around — full removal rather than zeroing it out, so a future stat-bonus rework starts from a
    clean slate instead of a dead/unused property lingering in the data. Verified via a scripted
    repro: read a real equipped item's `TooltipText()` and confirmed no bonus artifact remains
    (matches raw `MinDamage`-`MaxDamage`, e.g. `35 - 50` for `Tome of Embers`); drew the HUD in
    both the ready and not-ready ability states through a real `SpriteBatch.Begin()/End()` pair
    with no exception; and confirmed `UseAbility()` still fires and spawns projectiles correctly
    with the bonus term gone from the formula entirely. *(16:29 EDT)*
46. **Every HUD element moved into a new right-side sidebar panel; window widened to make room,
    gameplay area unchanged.** Planned via `EnterPlanMode` given the scope (8 files, an
    architectural prerequisite). Biggest finding during research: nothing in the codebase
    previously distinguished "window size" from "gameplay camera viewport size" —
    `Game1.Viewport`/`ScreenWidth`/`CenterWidth` all resolved to the same backbuffer dimensions,
    and `Camera` captured that same width/height at construction to center the world and clamp
    panning. Simply widening the backbuffer would have stretched the *visible play area* into the
    new strip too. Fixed by introducing `Game1.GameplayViewportWidth/Height` (1280×720, the old
    fixed size) distinct from the window (now 1580×720 — `GameplayViewportWidth + SidebarWidth`,
    `SidebarWidth = 300`); `Camera`'s constructor now takes explicit `viewportWidth`/`viewportHeight`
    ints instead of a `Viewport` struct, and all 3 construction call sites (`Camera.Reset()`,
    `RealmState`/`NexusState` constructors) pass the fixed gameplay dimensions instead of
    `Game1.Viewport` — confirmed via `RealmState.Draw()` that the background tile and all entities
    already render inside a `Camera.GetTransformation()`-transformed block sized to
    `Game1.WorldWidth`/`Height` (not screen size), so fixing Camera's viewport width alone was
    sufficient to keep world-space rendering confined to the original play area with no scissor
    rect or extra clipping needed. `Game1.GetWorldBounds()` (enemy on-screen-attack checks)
    switched from `CenterWidth`/`CenterHeight` (the now-wider window) to the fixed gameplay
    dimensions for the same reason. `LootBag.cs`'s item-picker centering also switched from
    `ScreenWidth/2` to `GameplayViewportWidth/2` so it stays centered over the play area instead of
    drifting toward the sidebar — the Main Menu/Character Select/Game Over screens were
    deliberately left alone (no sidebar to avoid, centering on the new full window width is fine).
    `Overlay.cs`'s `DrawHealth` (previously one method bundling XP/health/mana/ability bars) was
    split into private per-section methods and consolidated with the repositioned `DrawStats`/
    `DrawEquipment`/`DrawInventory` into one new `public DrawSidebar()` entry point, replacing four
    separate calls in both `RealmState.Draw()` and `NexusState.Draw()` (confirmed via grep these
    four were the only callers anywhere, so nothing else needed updating) — draws a semi-
    transparent panel background (reusing the existing `Art.HealthBar`-as-solid-rect technique from
    `Util.DrawTooltip`) then, top to bottom: stats (Level + 6 core stats — dropped the
    Level/Experience/ExperienceNextLevel/ExperienceTotal text that used to live here too, since it
    duplicated the XP section's own progress bar+text immediately below), XP, health, mana, ability
    (resolved via `AskUserQuestion`: not in the user's original list since it wasn't called out
    explicitly, but bundled with health/mana/XP in the old `DrawHealth` — user chose to move it
    into the sidebar too, grouped right after Mana rather than left behind in the gameplay area).
    Bars shrunk from `barScale=4`/`barHeight=40` (sized for the old wide gameplay-area placement,
    max 400px) to `barScale=2`/`barHeight=24` (max 200px) to fit the 300px sidebar with margin.
    Equipment slot anchors (`Weapon`/`Armor`/`Ring`/`AbilityItem`'s static `x`/`y`, each hardcoding
    its own `+40N` offset to sit in a row) and `InventorySystem`'s instance `x`/`y` all repointed
    from `Game1.Viewport.Width - 256`-style formulas to `Game1.SidebarX + 20`-style ones, keeping
    the same relative row/grid shape. Confirmed no other maintenance needed: hover/hit-testing
    (`Equipment.Update()`'s `SlotBounds.Intersects(Input.MouseBounds)`) and
    `InventorySystem.SlotIndexAt()` both derive from these same fields, so drag-drop and tooltips
    follow automatically. `BankSystem.cs` deliberately untouched — it anchors to the Bank portal's
    *world* position via the camera transform, not any window/screen constant, so it was never
    coupled to the old layout and needed no change. Verified via a scripted repro: confirmed the
    window backbuffer is 1580×720 while `Camera.ViewportWidth` (new public accessor) stays 1280 —
    proving gameplay rendering didn't stretch into the sidebar even though the window did; confirmed
    every equipment slot's and the inventory's `X` position is `>= Game1.SidebarX` for all three
    classes; and called `Overlay.DrawSidebar()` through a real `SpriteBatch.Begin()/End()` pass for
    all three classes with no exception. Can't drive the native window to confirm the visual result
    (spacing, legibility, no overlap) — asked the user to check in-game. *(16:47 EDT)*
47. **Window shrunk back to the original 1280×720 total — the sidebar now carves its width out of
    that instead of adding to it.** Immediate follow-up to entry 46: the user wanted the *whole
    window* back to its original size with the sidebar unchanged, meaning the gameplay area has to
    give up the space instead. Since every repositioned file (entry 46) referenced
    `Game1.SidebarX`/`GameplayViewportWidth` symbolically rather than hardcoding pixel values
    (confirmed via a repo-wide grep for the literal `1280` turning up only `Game1.cs` itself), this
    was a single-file change: renamed the old `GameplayViewportWidth/Height = 1280/720` constants
    to `WindowWidth/WindowHeight = 1280/720` (the true window size now), and redefined
    `GameplayViewportWidth = WindowWidth - SidebarWidth` (980) /
    `GameplayViewportHeight = WindowHeight` (720) as compile-time-computed consts. The backbuffer
    assignment in `Initialize()` switched from `GameplayViewportWidth + SidebarWidth` to plain
    `WindowWidth`/`WindowHeight`. Every other file from entry 46 — `Camera`, `GetWorldBounds`,
    `LootBag`, the four equipment classes, `InventorySystem`, `Overlay.DrawSidebar` — needed zero
    changes, since they all read the now-smaller `GameplayViewportWidth`/`Game1.SidebarX` rather
    than a hardcoded number. Net effect: window is 1280×720 again, sidebar is still 300px wide, but
    the visible play area is now 980×720 instead of the original 1280×720 — a real, deliberate
    trade-off the user asked for, not a side effect. Verified via a scripted repro: confirmed the
    backbuffer is back to 1280×720, `GameplayViewportWidth` is 980, `Camera.ViewportWidth` matches
    (980, not the full window), equipment/inventory anchors sit at the new `SidebarX` (980), and
    `DrawSidebar()` still renders with no exception. *(16:51 EDT)*

## 2026-08-15

48. **Boss enemies — the first backlog item, built from scratch (planned via `EnterPlanMode`).**
    Research confirmed none of its three pieces had any existing precedent: no health-threshold/
    phase-transition logic anywhere, no per-enemy loot differentiation (every enemy funneled
    through the same flat-probability `ItemSpawner.Spawn()`), and no "every N levels" trigger
    pattern. Resolved via `AskUserQuestion`: spawns every 5 levels (5/10/15/20 — `Level` caps at
    20, so 4 encounters per run); a two-phase "escalation" attack pattern rather than a bigger
    version of an existing enemy; placeholder art/sounds (`Art.EnemySpriteGod`/
    `Sound.SpriteGodDeath`/`SpriteGodHit`, no real boss asset yet); and — since every enemy
    projectile dealt a flat, shared `10` damage to the player with no per-enemy differentiation —
    the user wanted the boss's shots to specifically hit harder, so `EnemyProjectile` gained a
    `Damage` field (default `10`, preserving every other enemy's existing behavior) and
    `EntityManager`'s player-hit collision reads it instead of the hardcoded constant.
    `Enemy.CreateBoss()`: 2000 HP (vs `SpriteGod`'s 1500), 500 `PointValue` (flows into Experience/
    Score/Fame automatically via the existing `WasShot` reward code, no new plumbing needed),
    `isBoss = true`. Phase 1: `FollowPlayer(0.2f)` + `Spray(3, 8, damage: 20)` (existing `Spray()`
    gained an optional `damage` param, default `10`, so the Seeker using it unparameterized is
    unaffected). A new `BossPhaseWatcher()` behavior coroutine (added via `AddBehaviour`, so it
    always runs) polls health each frame; once it crosses 50%, one-shot-adds a second attack
    (`BossBurst()`, a new full-circle-burst coroutine with its *own* dedicated cooldown field,
    since the existing `projectileCooldownRemaining` is a single shared instance field `Spray()`/
    `Shoot()`/`Bomb()` all already contend for — two simultaneous boss attacks needed their own
    timer) and a second `FollowPlayer(0.15f)` stacked on top of the phase-1 one (behaviors are
    additive, so this is a simple way to get "faster" without removing/replacing anything).
    `BossBurst()` spaces projectiles correctly (`i * (TwoPi / count)`) rather than repeating the
    existing `Bomb()`'s `i * 10` (10 *radians*, not evenly spaced) bug in new code — `Bomb()`
    itself wasn't touched, out of scope. New `Entity.drawScale` field (default `1f`, zero visual
    change for anything that doesn't set it) lets the boss draw at 1.75× its native sprite size;
    `Radius` is scaled to match so the hitbox stays consistent with what's drawn. `WasShot()`'s
    death branch now checks `isBoss` to route to a new `ItemSpawner.SpawnGuaranteedLoot()` instead
    of the normal `Spawn()` — same next-tier-above-equipped selection logic per category (weapon/
    armor/ring/ability item) but without the `1/15` gates, so every category with a next tier
    available always contributes, plus always one random stat potion; single `Art.LootBagGold`
    bag. `EnemySpawner.cs` gained deterministic (not probability-rolled, unlike every other spawn
    type) boss-trigger state — `currentBoss`/`nextBossLevel` — so a boss reliably appears once the
    threshold level is reached rather than maybe-appearing, and only one exists at a time.
    Verified via a scripted repro using reflection for private-field visibility (test-only, no
    permanent public API added): confirmed factory stats/scale/radius match exactly; damaged the
    boss to just above 50% HP and confirmed behavior/attack-list counts were unchanged, then
    crossed the threshold with one more hit and confirmed both the new attack and new movement
    behavior got added in that exact frame — not before, not delayed; killed it and confirmed
    exactly 2 entities spawned (`DamageNumber` + one `LootBag`) matching the guaranteed path, not
    the random one; and drove `EnemySpawner` directly — Level 5 spawns a boss, a second `Update()`
    at the same level does not duplicate it, and Level 10 (after expiring the first) spawns a new
    one. Explicitly deferred (noted in the backlog): real boss art, a dedicated boss health-bar UI,
    a spawn announcement, and rebalancing the first-pass numbers after actually playing a fight.
    *(09:09 EDT)*

49. **Replaced the every-5-levels boss trigger (entry 48) with a SpriteGod-dropped portal into a
    dedicated arena instance.** Same day, same session — the deterministic level-based spawn
    didn't fit what the user actually wanted: SpriteGod (the existing mini-boss, 1500 HP, spawns
    with rising probability as the player levels) now drops a portal on death, optional to enter,
    leading into a self-contained arena for the same `Enemy.CreateBoss()` fight built in entry 48
    (unchanged — 2000 HP, two-phase escalation, etc. — just relocated to a new spawn site).
    Resolved via `AskUserQuestion`: exit returns to the Nexus hub (not "return to exact Realm
    position," which would've needed new position-tracking state that doesn't exist for the open,
    non-instanced Realm world); the arena is a smaller bounded space (2000×2000, vs. the open
    Realm's 500,000×500,000), not just a teleport within the same world; SpriteGod keeps its
    normal random loot roll in addition to dropping the portal. Planned via `EnterPlanMode` —
    research surfaced a real regression risk before any code was written: `Input.cs` gates
    potions/debug-level-keys on `currentState is RealmState` and the ability key on
    `currentState is RealmState || currentState is NexusState`; a naive sibling `State` subclass
    for the arena would satisfy neither check, silently disabling potions *and the ability* during
    the boss fight. Fixed by making the new `States/BossRealmState.cs` **inherit from
    `RealmState`** instead (C#'s `is` matches subtypes), gaining both checks for free with zero
    `Input.cs` changes. `RealmState.cs` gained three `protected virtual` extension points
    (`SpawnsRegularEnemies`, `InstanceWorldWidth`/`Height`, all pure constants) consumed by its
    existing constructor/`Update()`/`Draw()` — confirmed safe to read from the base constructor
    since C# virtual dispatch during construction already resolves to the most-derived override.
    `BossRealmState` overrides these three (spawning off, 2000×2000) and, after `base(...)`, calls
    `EntityManager.Reset()` (base's constructor never has, since `EnterPortal()`→`RealmState` is
    deliberately non-resetting to keep the open Realm feeling persistent — this new instance needs
    the opposite), repositions the player to the arena's bottom, spawns the boss at the top, and
    drops one exit portal. New `Portal.DroppedPortals` static registry (mirrors
    `ItemSpawner.LootBags`'s existing shape exactly) serves *both* new needs at once — SpriteGod's
    dropped portal (added by `Enemy.WasShot()`, which has no reference to "the current state" to
    append to an instance-owned list) and the arena's own exit portal (added by
    `BossRealmState`'s constructor right after `base(...)` already called the new `Portal.Reset()`
    alongside the existing `ItemSpawner.Reset()`, so each fresh entry starts with exactly the
    portal it should have and no leakage from the previous instance). Two new `Destination` enum
    values: `BossRealm` (SpriteGod's drop) and `Nexus` (new — every other Nexus-bound path
    currently goes straight through `StateManager.Nexus()` rather than a world portal). New
    `Enemy.isSpriteGod` field (mirrors the existing `isBoss` field's shape exactly), set by
    `CreateSpriteGod()`, checked in `WasShot()`'s death branch *after* the existing loot logic
    (SpriteGod isn't `isBoss`, so it already goes through normal `ItemSpawner.Spawn()` unchanged —
    the portal drop is strictly additive). `EnemySpawner.cs`'s `currentBoss`/`nextBossLevel` state
    and its every-5-levels block from entry 48 are gone; `CreateSpriteGod()`'s own spawn-chance
    roll is untouched, still the only thing gating how often a SpriteGod (and therefore a shot at
    the boss) is even possible. Verified via a scripted repro (reflection for private-field
    visibility, test-only): killed a `CreateSpriteGod()` directly and confirmed exactly one
    `BossRealm`-destination portal dropped plus normal loot still rolled; constructed a
    `BossRealmState` directly and confirmed `SpawnsRegularEnemies=false`, arena size 2000×2000, a
    `PointValue=500` boss present in `EntityManager`, the player's position actually changed from
    an artificial pre-arena placeholder, and exactly one `Nexus`-destination exit portal existed;
    then simulated the player's `Bounds` overlapping that exact exit portal and calling its own
    `Update()`, confirming `Game1`'s pending state transition was a real `NexusState` instance.
    *(10:00 EDT)*
50. **Enemies gained a `Defense` stat that reduces incoming damage, defaulting to 0.** New
    `public int Defense { get; private set; } = 0;` on `Enemy.cs` (same `PointValue`-style
    encapsulation — public read, only settable by a factory from within the class). `WasShot()`
    now computes `actualDamage = Math.Max(0, damage - Defense)` before applying it to `health` —
    floored at 0 so Defense can never turn a hit into healing. The floating `DamageNumber` also
    switched from showing the raw incoming `damage` to the post-mitigation `actualDamage`, so the
    number on screen reflects what actually happened once some enemy has nonzero Defense. Default
    `0` means every existing enemy (Wanderer/Seeker/Snake/SpriteGod/Boss, none of which set
    Defense) is completely unaffected — `Max(0, damage - 0) == damage`, identical to before this
    change; nothing currently assigns a nonzero value, this just adds the stat and its damage-math
    hook for something to use later. Verified via a scripted repro (reflection to set the
    private-set `Defense` property, test-only): confirmed a fresh enemy's `Defense` reads `0` and a
    single lethal hit still kills it outright (unchanged behavior); set `Defense = 100` on a 150-HP
    enemy and confirmed it survived two separate 150-raw-damage hits (50 actual each) and died
    only on the third, exactly matching `150 - (50×3) = 0`, not some approximate/rounded value; and
    confirmed an enemy with `Defense = 100` took zero damage at all across 20 separate 10-damage
    hits, proving the floor-at-0 behavior rather than accumulating negative/healing damage.
    *(10:44 EDT)*
51. **`FollowPlayer` (Seeker's/the boss's movement behavior) no longer rotates the enemy sprite to
    face its velocity.** `FollowPlayer()`'s `if (Velocity != Vector2.Zero) Orientation =
    Velocity.ToAngle();` line was removed — it still accumulates `Velocity` toward the player
    exactly as before (movement itself is unaffected), it just no longer writes `Orientation` each
    tick. Other movement behaviors (`MoveRandomly`, `MoveSnake`) weren't touched — only asked about
    `FollowPlayer` specifically. Verified via a scripted repro: gave a `CreateSeeker()` enemy a
    known starting `Orientation`, positioned the player somewhere that would generate a very
    different velocity direction, ran 30 real `Update()` frames, and confirmed `Orientation`
    stayed byte-for-byte identical to its starting value throughout while `Velocity` genuinely
    changed (accumulated toward the player as expected) — proving movement and rotation are now
    decoupled, not just that nothing crashed. *(11:17 EDT)*
52. **Debug mode (F3) now draws hitbox outlines for the player and every enemy.** New
    `EntityManager.DrawHitboxes(SpriteBatch)`, scoped to player + enemies only (not projectiles or
    loot, per the user's request) — draws a circle outline at each entity's actual `Position`/
    `Radius`, not a rough bounding box, so it matches exactly what `IsColliding()` (also in
    `EntityManager.cs`) actually checks: `DistanceSquared < (radiusA + radiusB)²`, a true circle,
    not a rectangle. The circle itself is built from 24 line segments (no dedicated circle-outline
    art exists), each drawn by stretching the existing 1×1 white pixel (`Art.HealthBar` — the same
    solid-rect/line technique already used throughout `Overlay.cs`'s bars and `Util.DrawTooltip`)
    and rotating it to match each segment's angle. Enemies draw red, the player draws lime, so
    they're visually distinguishable at a glance; expired entities are skipped. Called from both
    `RealmState.Draw()` and `NexusState.Draw()` (mirroring how each already calls
    `EntityManager.Draw()`) inside the same world-space, camera-transformed `spriteBatch.Begin()`
    block — has to be world-space, not the later screen-space block `Overlay.DrawDebug()`'s text
    lives in, so hitboxes are drawn at the entities' actual world position and pan correctly with
    the camera rather than sitting at a fixed screen location. `BossRealmState` needed no changes
    at all — it inherits `RealmState.Draw()` unmodified. Verified via a scripted repro: ran a real
    `RealmState.Draw()` end-to-end with `Game1._Debug` both `true` and `false` (confirming the
    toggle itself works and neither state throws), and separately called `DrawHitboxes()` directly
    against a mix of alive and already-expired enemies plus the player, confirming expired entities
    are silently skipped rather than throwing or drawing stale hitboxes. *(11:26 EDT)*

## 2026-08-17

53. **`Boss.cs` base class introduced for future boss variety, and the existing arena boss renamed
    "Limon the Sprite Goddess" with real art.** The user asked whether a `Boss.cs` subclass made
    sense for adding more bosses later — it does, matching the exact "shared base + per-variant
    subclass" pattern already proven three times for `Player` (`CharacterClasses/Wizard.cs`/
    `Archer.cs`/`Knight.cs`). New abstract `Boss : Enemy` holds only genuinely shared boss
    infrastructure: a `Name` property (no UI shows it yet — no boss health-bar/name UI exists —
    but this is what "give the boss a name" means at the code level) and an overridden
    `SpawnLoot()` routing to `ItemSpawner.SpawnGuaranteedLoot()` instead of the normal random-roll
    table every other enemy uses. New concrete `LimonTheSpriteGoddess : Boss` carries everything
    specific to this one fight — stats (12000 HP, Defense 16, PointValue 2000 — the live,
    hand-tuned values at the time of the refactor, not entry 48's original first-pass numbers),
    `Art.Limon`, and its two-phase attack pattern (`Spray`/`BossBurst`/`PhaseWatcher`, moved
    verbatim from the old `Enemy.CreateBoss()`/`BossBurst()`/`BossPhaseWatcher()`, which are all
    now deleted — `CreateBoss()`'s only call site, in `BossRealmState.cs`, now does
    `new LimonTheSpriteGoddess(...)` directly). Required several `Enemy.cs` accessibility changes
    (`private`→`protected`) for a subclass constructor to set things directly, mirroring exactly
    how `Wizard()`/`Archer()`/`Knight()` already set `baseHealth`/`baseMana`/etc.: `AddBehaviour`/
    `AddAttackBehaviour`, `FollowPlayer`/`MoveSnake`/`Spray`, `health`/`healthMax` fields (a plan
    to keep these fully private behind a new `HealthFraction` read-only property turned out
    infeasible — a boss constructor needs to *set* starting HP, not just read a fraction — so the
    fields themselves became `protected`, with `HealthFraction` kept alongside as a convenience
    accessor, not a strict boundary), `deathSound`/`hitSound`, and `PointValue`/`Defense`'s
    setters. The old `Enemy.isBoss` bool (checked once, in `WasShot()`'s loot branch) was deleted
    entirely, replaced by real polymorphism: a new `protected virtual void SpawnLoot()` on `Enemy`
    (default: normal `ItemSpawner.Spawn()`) that `Boss` overrides — `WasShot()` just calls
    `SpawnLoot()` unconditionally now. `Boss`/`LimonTheSpriteGoddess` had to drop `public` from
    their class declarations (`CS0060`: a public class can't derive from `Enemy`'s implicit
    `internal` accessibility) — both are internal/default-access, same as `Enemy` itself. The
    user's supplied art, `Content/Enemies/Limon the Sprite Goddess.png`, was wired in via a new
    `Content.mgcb` block (confirmed the spaced filename needs no special quoting in a `#begin`/
    `#build` line — the one real risk flagged during planning) and `Art.Limon` in `Art.cs`,
    mirroring `Art.EnemySpriteGod`'s existing declare/load shape. Sound stays a placeholder
    (`Sound.SpriteGodDeath`/`SpriteGodHit`, reused) — the user supplied new art, not new sound,
    kept out of scope. Verified via a scripted repro (reflection for private-field visibility,
    test-only): confirmed `Name`/`PointValue`/`Defense` match, `is Boss` and `is Enemy` both true
    (polymorphism working through the full chain — relevant since `EntityManager`'s `enemies` list
    and collision handling only ever check `is Enemy`); confirmed `Art.Limon` loads non-null at
    the correct pixel dimensions and the scaled `Radius` math is exact; drew it through a real
    `SpriteBatch.Begin()/End()` pass with no exception; re-ran the same phase-transition regression
    check used for the original boss feature (damage to just above 50% HP, confirm no escalation;
    cross the threshold, confirm `BossBurst`/extra movement both get added in that exact frame) —
    proving the moved coroutines still work identically after relocating out of `Enemy.cs`; and
    killed it, confirming `ItemSpawner.SpawnGuaranteedLoot` fired via `Boss`'s override (not the
    base class's random-roll `SpawnLoot()`). One test-harness bug caught and fixed along the way,
    not a game bug: the phase-transition test's second damage hit used only 1 raw damage, which
    `Math.Max(0, 1 - Defense)` (Defense=16) floors to 0 actual damage — the hit did nothing and the
    health threshold was never actually crossed, so the test wrongly reported no escalation: the
    real code was already working correctly (matches [BUGFIXES.md](BUGFIXES.md)'s repeated pattern of
    test-math errors being mistaken for game bugs). Fixed by accounting for `Defense` in both test
    hits' raw damage values; re-ran and all 6 checks passed. Explicitly deferred: what a future
    second boss's `Boss` subclass would look like (not guessed at now with only one boss built);
    a real boss name/health-bar UI to actually display `Boss.Name` somewhere. Can't visually
    confirm the new art renders correctly in-game (native window) — asked the user to check.
    *(12:07 EDT)*
54. **`LimonTheSpriteGoddess.cs` moved into a new `Bosses/` folder.** Pure reorganization, no
    behavior change — namespace updated `Realm` → `Realm.Bosses` to match the existing
    folder-matches-namespace convention (`CharacterClasses/` → `Realm.CharacterClasses`,
    `States/` → `Realm.States`); `Boss.cs` itself stayed at the project root (not asked to move).
    `using Realm;` added to the moved file (needed for `Boss`/`Art`/`Sound`/`EntityManager`/
    `EnemyProjectile`/`Extensions`, no longer implicitly visible once the file left the `Realm`
    namespace), and `using Realm.Bosses;` added to `States/BossRealmState.cs` for its one
    `new LimonTheSpriteGoddess(...)` call site. Verified via clean build (0 errors). *(12:35 EDT)*
55. **New Limon attack: a constant square-shaped wall of stationary projectiles centered on the
    boss.** Resolved two design questions via `AskUserQuestion` before building, since guessing
    wrong would mean redoing tuned values: (1) active for the *entire* fight, not phase-gated —
    layers underneath `Spray()` (phase 1) and `BossBurst()` (phase 2) rather than replacing either,
    making it the boss's ever-present signature mechanic; (2) a tight ~1000×1000 box (500 half-size)
    within the 2000×2000 arena, not a roomier one. New `SquareWall()` coroutine (added via
    `AddAttackBehaviour` in the constructor, alongside `Spray()`), with its own dedicated
    `wallCooldownRemaining`/`wallCooldown` (45 frames) — same reasoning as `BossBurst`'s separate
    cooldown field: it can't share the base `Enemy` class's single `projectileCooldownRemaining`
    with `Spray()` since both need to fire independently. Each refresh (`SpawnSquareWall()`) traces
    the square's four sides — corners computed as `Position ± (halfSize, halfSize)`, points placed
    via `Vector2.Lerp` along each side — as stationary (`Vector2.Zero` velocity) `EnemyProjectile`s,
    12 damage each, spaced 50 units apart (well under the player's 64-diameter plus a projectile's
    16-diameter, so there's no gap the player can slip through between two neighboring segments) —
    80 projectiles per ring in total (1000/50 = 20 per side × 4 sides). Each ring's projectiles live
    `wallCooldown + 15` frames (60 total) — longer than the 45-frame refresh interval — so the next
    ring is already in place before the previous one expires, reading as one continuous wall rather
    than a flickering repeating burst; recomputing the corners from `Position` fresh on every
    refresh is what keeps the wall centered on the boss as it moves via `MoveSnake()`, rather than
    drifting behind it. Verified via a scripted repro (reflection for `enemiesProjectiles`'
    private-field visibility, test-only, filtering by the wall's exact `duration`/`Damage`/zero-
    `Velocity` signature to isolate it from `Spray()`'s simultaneous projectiles): confirmed both
    `Spray()` and `SquareWall()` are active attack behaviours from frame 0, not added later by
    `PhaseWatcher()`; the very first update spawned exactly 80 wall projectiles, all lying exactly
    on the square's boundary; no second ring appeared across the next 44 updates (proving it
    doesn't spawn every frame); the 45th update produced a second ring on top of the still-alive
    first one, doubling the count (proving the overlap-no-gap timing is exact, not approximate);
    and moving the boss to a new position then advancing past the next refresh point produced a
    ring correctly centered on the *new* position, not the stale spawn-time one. *(13:21 EDT)*
56. **`SquareWall` redesigned from a dense static ring into 2 sweeping projectiles per line.**
    Immediate follow-up to entry 55, same session: the user wanted movement — each of the square's
    4 lines now spawns exactly 2 projectiles, one at each corner, each sweeping to the *opposite*
    corner of that line (crossing paths partway across) rather than 20 stationary points per side.
    `SpawnSquareWall()` now computes each projectile's velocity directly from the geometry —
    `(end - start) / wallTravelFrames` — sized so `Position` (which accumulates `+= Velocity` once
    per frame, per `EnemyProjectile.Update()`) lands exactly on the opposite corner after
    `wallTravelFrames` (100) frames; the reverse-direction projectile at the other end uses the
    negated velocity. `wallCooldown` now reads `= wallTravelFrames` directly (a `const`, replacing
    the old `+ 15`-buffer relationship from entry 55's static version) so the next sweep launches
    the instant the previous one finishes crossing — continuous back-and-forth motion with no idle
    gap, rather than a periodically-refreshed static field. Total per-wave projectile count dropped
    from 80 to 8 (2 × 4 sides) — no longer needs the tight `wallSpacing` field entry 55 added to
    prevent slip-through gaps, since coverage now comes from motion sweeping the full line rather
    than a dense stationary field; that field was deleted. `wallHalfSize` (500) and per-hit damage
    (12) both carried over unchanged — not asked to retune those, only the movement itself.
    Verified via a scripted repro (reflection for `enemiesProjectiles`, test-only): confirmed
    exactly 8 projectiles spawn per wave; for all 4 lines, found one projectile starting at each
    corner whose `Position + Velocity × wallTravelFrames` lands within 1 unit of the *opposite*
    corner of that same line (proving both the starting position and the exact arrival point, not
    just "some" movement); confirmed none of the 8 have zero velocity (i.e. none are stationary,
    the actual defect being fixed); confirmed no second wave spawns during the 99 frames before the
    sweep completes; and confirmed the next wave launches exactly on the 100th frame, doubling the
    tracked count — proving the "next sweep launches as the current one finishes" timing is exact,
    not approximate. *(13:28 EDT)*
57. **Wall projectiles now stay in sync with the boss's position instead of drifting behind it if
    the boss moves mid-sweep.** Entry 56's sweeping pair moved via a fixed world-space `Velocity`
    computed once at spawn — correct only if the boss never moved again before the sweep finished;
    since `MoveSnake()` runs the whole fight, a real fight would see the wall lag behind and
    separate from the boss over each 100-frame sweep. Fixed by no longer moving wall projectiles
    via their own `Velocity` at all (each now spawns with `Velocity = Vector2.Zero`): a new private
    `WallShot` (nested class: `Projectile`, `RelativeStart`, `RelativeEnd`, `Elapsed`) tracks every
    active wall projectile in a new `activeWallShots` list, and a new `UpdateWallShots()` — called
    once per frame from inside the `SquareWall()` coroutine, after the spawn-cooldown check —
    re-derives each one's `Position` fresh as `Position (boss) + Lerp(RelativeStart, RelativeEnd,
    Elapsed / wallTravelFrames)`, where `RelativeStart`/`RelativeEnd` are corner *offsets* from the
    boss's center (e.g. `(-500, -500)`), not world coordinates. Since this reads the boss's
    `Position` fresh every single frame, a wall projectile's distance from the boss stays exactly
    what the sweep's progress dictates regardless of how far the boss has moved since it spawned —
    the whole square translates with the boss instead of drifting behind it. `SpawnWallShot()` also
    sets each projectile's `Orientation` explicitly from the sweep direction
    (`(relativeEnd - relativeStart).ToAngle()`) as a small added touch, since zero `Velocity` would
    otherwise leave every wall projectile facing the same default direction rather than visually
    indicating which way it's sweeping. `activeWallShots` self-prunes (removes an entry once its
    `Projectile.IsExpired`) so it can't leak entries across a fight. Verified via a scripted repro
    (reflection for `enemiesProjectiles`, test-only) that took two debugging passes to get right:
    the first attempt jumped the boss to `(1000, -1000)` to simulate movement, which coincidentally
    pushed it outside `Game1.GetWorldBounds(1.25f)` — the same on-screen gate that already blocks
    every enemy's `ApplyAttackBehaviours()` (and therefore `UpdateWallShots()`) when off-camera —
    so the test wrongly looked like tracking had failed, when the wall-update code simply hadn't
    run that frame at all (working as designed, not a defect); a second attempt with a smaller
    `(200, -150)` jump *still* failed for a related, less obvious reason: `Camera.Pos`'s setter
    clamps to the world's edge bounds, so the test's own `Camera.Pos = Vector2.Zero` actually
    settled at `(490, 360)` (half the gameplay viewport) in this 500,000×500,000 world, not literal
    `(0, 0)` — meaning the on-screen rectangle used for the gate wasn't centered where the test
    assumed, and `(200, -150)` fell just outside its (asymmetric) Y-lower-bound. Fixed by jumping to
    `(400, 300)`, safely inside the actual clamped bounds, confirmed via added debug output before
    removal. With that sorted, all 3 checks passed: exactly 8 wall shots spawn per wave (regression
    check); after the controlled jump, the projectile count is unchanged (no duplication from the
    tracking logic); and — the actual fix being verified — every one of the 8 tracked positions
    matched `boss.Position(after the jump) + Lerp(RelativeStart, RelativeEnd, 2/100)` exactly,
    proving the offset is computed fresh from the boss's current position each frame rather than
    inherited from a stale spawn-time value. Separately noticed (not changed): `wallCooldown` is
    currently hardcoded to `10` frames rather than reading `= wallTravelFrames` (100) as entry 56
    set it — someone/something changed it directly outside this conversation's edits between turns,
    meaning up to ~10 overlapping sweep generations (up to 80 concurrent wall projectiles) can be
    alive at once rather than a single back-and-forth pair; left as-is since it reads as a
    deliberate gameplay-feel tuning choice, not a bug — flagging only for awareness. *(13:40 EDT)*
58. **Second constant Limon attack: XCross, an X-shaped pair of diagonal sweeps through the boss's
    own center.** Resolved via `AskUserQuestion`: active for the whole fight alongside `SquareWall`
    (not gated to phase 2), same as `SquareWall` itself. Reuses `SquareWall`'s exact 4 corners
    (`wallHalfSize` = 500, same box) so the X is inscribed in the same square, but its two
    diagonals — top-left↔bottom-right and top-right↔bottom-left — cross through `Position` itself,
    meaning the square's interior is no longer a fully safe zone; the player now also has to dodge
    something sweeping through the middle, on top of staying inside the perimeter wall. New
    `XCross()` coroutine (own `xCooldownRemaining`/`xCooldown`/`xTravelFrames`, mirroring
    `wallCooldown`/`wallTravelFrames` — kept separate so the two patterns' density can be tuned
    independently, same reasoning as every other per-attack cooldown in this class) spawns exactly
    4 projectiles per wave (2 per diagonal, one from each end sweeping to the opposite end,
    default damage 15 — distinct from the wall's 12 so the two are distinguishable, both for
    testing and as a natural side effect of them being different attacks). Since this needed the
    exact same "track the boss every frame" mechanism this log's entry 57 just built for
    `SquareWall`, that mechanism was generalized rather than duplicated: `WallShot` renamed to
    `SweepingShot` and gained a `TravelFrames` field (previously implicit via the shared
    `wallTravelFrames` constant — now needed per-shot since `SquareWall` and `XCross` could in
    principle use different travel times), and `SpawnWallShot`/`UpdateWallShots` became
    parameterized `SpawnSweepingShot(relativeStart, relativeEnd, damage, travelFrames, targetList)`
    /`UpdateSweepingShots(shots)`, both taking an explicit target list. `SquareWall` and `XCross`
    each keep their own tracking list (`activeWallShots`/`activeXShots`) and call
    `UpdateSweepingShots()` on only their own list from within their own coroutine tick — sharing
    the *code* while keeping the *state* fully separate, so the two attacks can't cross-contaminate
    each other's timing (deliberately not sharing one update call across both coroutines, which
    would have double-ticked every shot's `Elapsed` on frames where both coroutines run, since each
    attack behaviour's `MoveNext()` fires once per frame regardless of how many exist). Verified via
    a scripted repro (reflection for `enemiesProjectiles`/`attackBehaviours`, test-only, filtering
    wall shots by `Damage==12` and X shots by `Damage==15` to tell them apart): confirmed 3 attack
    behaviours active from frame 0 (`Spray`, `SquareWall`, `XCross` — not phase-gated); exactly 4 X
    shots spawn per wave, with both diagonals correctly represented in both directions; a light
    regression check that exactly 8 wall shots still spawn per wave (confirming the
    `WallShot`→`SweepingShot` generalization didn't break `SquareWall`); and — reusing entry 57's
    boss-jump technique — confirmed *both* attacks' tracked shots correctly followed the boss to
    its new position in the same frame, proving the per-list separation works correctly rather than
    one attack's shots accidentally reading the other's state. *(14:07 EDT)*
59. **Limon's Spray attack fires a dedicated projectile sprite (`limon1.png`) instead of the shared
    `enemy_projectile` sprite every other enemy's shots use.** The user had already supplied and
    wired the art themselves (`Content.mgcb` entry, `Art.LimonProjectile` declare/load in `Art.cs`)
    before this request — this entry covers wiring it into `Spray()`'s actual behavior. Since
    `Spray()` is a shared `protected` method on `Enemy` used by other enemies too (e.g.
    `CreateSeeker()`), it gained a new optional trailing `Texture2D projectileImage = null`
    parameter rather than a hardcoded image, so every other caller is unaffected by default.
    `EnemyProjectile`'s own constructor also gained an optional `Texture2D image = null` param
    (`this.image = image ?? Art.EnemyProjectile`) — the image can't be swapped via an object
    initializer after construction, because `Radius` is computed from the image's own
    `Width` *inside* the constructor; setting `image` afterward would leave `Radius` computed
    against the wrong (default) sprite's dimensions. This mattered concretely here: `limon1.png` is
    40×10, nothing like `enemy_projectile.png`'s 16×16, so a naive object-initializer swap would
    have given Limon's Spray shots a visibly wrong hitbox size. `LimonTheSpriteGoddess`'s
    `Spray(6, 8, damage: 50, ...)` call now passes `projectileImage: Art.LimonProjectile`. Verified
    via a scripted repro (reflection for `enemiesProjectiles`/`timeUntilStart`, test-only): confirmed
    `Art.LimonProjectile` loads non-null at 40×10; fired a regular `Enemy.CreateSeeker()`'s `Spray()`
    (an existing, unrelated caller) and confirmed its shots still use `Art.EnemyProjectile` with the
    old 8px radius — the regression check that the new optional parameter doesn't change any
    existing caller's behavior; and fired Limon's own Spray (isolated from the same frame's
    simultaneous `SquareWall`/`XCross` shots by filtering on `Damage==50`, Spray's specific damage)
    and confirmed all of them use `Art.LimonProjectile` with the new 20px radius, proving both the
    image swap and the radius-computed-from-the-right-image fix. *(14:42 EDT)*
60. **Debug hitboxes (F3, this log entry 52) extended to cover both projectile lists —
    player-fired bullets and enemy projectiles — not just player + enemies.** `EntityManager.
    DrawHitboxes()` gained two more loops over its existing private `bullets`/`enemiesProjectiles`
    lists, drawing the same circle-outline technique (`DrawHitboxCircle`, unchanged) at each
    projectile's actual `Position`/`Radius`, skipping expired ones exactly like the existing
    enemy/player loops already do. New colors to stay visually distinct from the existing Red
    (enemies) / Lime (player): Yellow for player bullets, Orange for enemy projectiles. No changes
    needed anywhere else — `RealmState.Draw()`/`NexusState.Draw()`'s existing `DrawHitboxes()` call
    sites automatically pick up the new loops. Verified via a scripted repro: added one live and one
    already-expired instance of both `Projectile` and `EnemyProjectile` to `EntityManager`, then
    called `DrawHitboxes()` through a real `SpriteBatch.Begin()/End()` pass with no exception —
    confirming the new loops render both live projectile types correctly and silently skip the
    expired ones, matching the existing enemy/player behavior. *(14:50 EDT)*
61. **Rectangle-based collision detection, alongside the existing circle math, selectable per
    entity — primarily meant for projectiles.** New `Entity.CollisionShape` enum (`Circle`,
    `Rectangle`, nested on `Entity` matching the existing `DebuffType` convention) and a new
    `public CollisionShape Shape = CollisionShape.Circle;` field — defaults to `Circle` so every
    existing entity's behavior is completely unchanged; a specific projectile instance opts into
    `Rectangle` the same way `ExpiresOnHit`/`ParalyzesOnHit`/`StunsOnHit` are already overridden via
    object initializer (e.g. `new EnemyProjectile(...) { Shape = Entity.CollisionShape.Rectangle }`)
    — suited to something like a wide beam-shaped projectile where a circular hitbox would feel
    wrong. `EntityManager.IsColliding(Entity a, Entity b)` now checks `a.Shape`/`b.Shape` first —
    rectangle math applies if *either* side is `Rectangle` (asymmetric override, so a rectangular
    projectile behaves consistently against a circular enemy too, not just against another
    rectangular entity); only when both sides are `Circle` does it fall through to the original
    distance-vs-combined-radius check, byte-for-byte unchanged. Rectangle collision uses
    `Rectangle.Intersects()` against boxes computed by a new `RectangleBounds()` helper — deliberately
    NOT reusing the existing `Entity.Bounds` property, which anchors `Position` at the rectangle's
    top-left corner rather than centering it; every other place `Position` is used (the circular
    `Radius` check, `Draw()`'s `Size/2f` origin offset) already treats it as the center, so a
    collision rectangle anchored differently would be visibly offset from the sprite. `RectangleBounds()`
    computes a box actually centered on `Position`, sized to the entity's real `Width`/`Height`.
    Verified via a scripted repro (reflection to call the private static `IsColliding`, test-only):
    regression-checked the unchanged default (`Circle`) case still collides/doesn't-collide exactly
    as before at known in-range/out-of-range distances; then proved rectangle collision is genuinely
    different geometry, not a silent fallback, using a diagonal-offset pair (two 16×16 entities offset
    by `(15, 15)`) where circle math says no overlap (~21.2 distance exceeds the combined 16 radius)
    but the two axis-aligned boxes clearly do overlap — confirmed `Rectangle` on either side alone
    (not just both) correctly detects that overlap, confirmed a clearly-separated rectangle pair
    still correctly reports no overlap, and confirmed an expired entity never collides even with an
    otherwise-overlapping rectangle shape. Deliberately left unwired — no existing projectile
    actually sets `Shape` to `Rectangle` yet; this entry is the reusable mechanism the user asked
    for, ready for whichever future projectile needs it. *(15:01 EDT)*
62. **Limon's Spray attack now uses the entry 61 rectangle collision mechanism.** Immediate
    follow-up, same session: `Spray()` (shared by every enemy that uses it, e.g. `CreateSeeker()`)
    gained a new optional trailing `Entity.CollisionShape collisionShape = Entity.CollisionShape.
    Circle` parameter — same pattern as the `projectileImage` parameter added just before it —
    passed straight through as `Shape` on each spawned `EnemyProjectile`. Every other `Spray()`
    caller is unaffected by default. `LimonTheSpriteGoddess`'s `Spray()` call now passes
    `collisionShape: Entity.CollisionShape.Rectangle`, fitting for `limon1.png`'s wide 40×10 shape
    (this log entry 59) — a rectangle hitbox matches that silhouette far better than a circle
    would. Verified via a scripted repro (reflection for `enemiesProjectiles`/`timeUntilStart`,
    test-only): confirmed a regular `Enemy.CreateSeeker()`'s `Spray()` shots still default to
    `Circle` (regression check — the new optional parameter doesn't change any existing caller); and
    confirmed Limon's own Spray shots (isolated by `Damage==50`, distinguishing them from the same
    frame's simultaneous `SquareWall`/`XCross` shots, which deliberately stay `Circle`) all report
    `Shape == Rectangle`. *(15:05 EDT)*
63. **Debug hitboxes (F3) now draw the correct outline shape per entity — a rectangle for anything
    set to `Entity.CollisionShape.Rectangle` (e.g. Limon's Spray shots, entry 62), a circle for
    everything else.** Previously `EntityManager.DrawHitboxes()` always drew a circle regardless of
    `Shape`, so the debug view silently disagreed with what `IsColliding()` actually checked for
    rectangle-shaped entities. New private `DrawHitbox(spriteBatch, entity, color)` branches on
    `entity.Shape` and either calls the existing `DrawHitboxCircle` (Radius-based, unchanged) or a
    new `DrawHitboxRectangle` (4 lines connecting the corners of a `Rectangle`) — all 4 call sites in
    `DrawHitboxes()` (enemies, player, bullets, enemy projectiles) now go through `DrawHitbox`
    instead of calling `DrawHitboxCircle` directly. Critically, the rectangle branch reuses the
    exact same `RectangleBounds()` helper `IsColliding()` itself uses (entry 61) rather than a
    separate box computation — so the debug outline is guaranteed to match real collision geometry
    by construction, not just by coincidence; if `RectangleBounds()` is ever adjusted, both the
    real check and its debug visualization update together automatically. Verified via a scripted
    repro: added a default-`Circle` enemy projectile, a `Rectangle`-shaped enemy projectile, and a
    `Rectangle`-shaped player bullet all at once, then called `DrawHitboxes()` through a real
    `SpriteBatch.Begin()/End()` pass with no exception — confirming the new shape-branching path
    (and the new `DrawHitboxRectangle` line-drawing) actually executes and renders without throwing
    for every entity type that calls into it. *(15:09 EDT)*
64. **Wrong-class equipment sitting in the inventory or bank grid now draws with a 25%-opacity red
    overlay.** New `Equipment.CanEquipByCurrentClass` virtual property (defaults `true`) — `Weapon`
    overrides it with `Type == Player.Instance.WeaponType`, `Armor` with
    `Type == Player.Instance.ArmorType`, `AbilityItem` with
    `Player.Instance.CanEquipAbilityItem(this)` — each reusing the *exact* comparison its own
    `LoadWeapon()`/`LoadArmor()`/`Load{Spell,Quiver,Shield}()` already uses to decide whether the
    item can actually be equipped, so the visual flag can never drift out of sync with real
    equip-ability. `Ring` has no class restriction (confirmed in code — any class can equip any
    Ring) so it inherits the base `true` unmodified. Both `InventorySystem.Draw()` and
    `BankSystem.Draw()` (separate, non-shared draw loops — confirmed no common code path between
    them) gained an identical check right after each slot's icon is drawn: if the slot holds an
    `Equipment` and `!CanEquipByCurrentClass`, draw `Art.HealthBar` (the existing 1×1-white-pixel-
    stretched-into-a-rect technique already used for `Util.DrawTooltip`'s background panel) over the
    same 40×40 slot footprint at `Color.Red * 0.25f`. Equip slots themselves were deliberately left
    untouched — a wrong-class item can never actually become equipped in the first place (the
    `Load*` methods above return `null` and play the error sound on a class mismatch), so the
    scenario this flags only ever arises in the inventory/bank grids. Verified via a scripted repro:
    confirmed `CanEquipByCurrentClass` reads correctly for a matching Wand (`true`) and mismatched
    Bow (`false`) as a Wizard, matching Robe (`true`) and mismatched Leather (`false`) Armor, an
    always-`true` Ring, and a Quiver (`false` — only an Archer can equip one); then populated both
    the inventory and bank grids with a mix of matching/mismatched items and called both `Draw()`
    methods through a real `SpriteBatch.Begin()/End()` pass with no exception, confirming the new
    overlay branch actually executes for both panels. *(16:25 EDT)*
65. **`HealthMax`/`ManaMax` no longer include equipment or temporary bonuses — only base, level
    scaling, and Potion bonuses.** All three classes' `RecalculateStats()` (`Wizard.cs`/`Archer.cs`/
    `Knight.cs`) had identical `HealthMax = baseHealth + ((Level-1)*25) + PotionHealthMaxBonus +
    EquipmentMaxHealthBonus + TemporaryHealthMaxBonus` formulas (same shape for `ManaMax`); the
    `EquipmentMaxHealthBonus`/`EquipmentMaxManaBonus`/`TemporaryHealthMaxBonus`/
    `TemporaryManaMaxBonus` terms were dropped from all three, leaving `PotionHealthMaxBonus`/
    `PotionManaMaxBonus` as the only bonus inputs — deliberately scoped to just the formula change,
    not a removal of the underlying `Equipment.MaxHealthBonus`/`MaxManaBonus` per-item fields (still
    real JSON-driven data, still shown in item tooltips via `BonusSummary()`) or `Player`'s
    `TemporaryHealthMaxBonus`/`AddTemporaryHealthMaxBonus()` timed-bonus machinery (part of the
    deliberately-generic all-8-stats system from an earlier entry in this log — kept intact and
    still callable, it simply no longer feeds into the computed Max stat). Verified via a scripted
    repro, delta-based rather than hardcoded absolutes since `LoadOrCreatePlayer` loads whatever a
    real save file already has (a leveled, previously-played Wizard, not a guaranteed-fresh Level 1
    character): equipped an Armor with `MaxHealthBonus`/`MaxManaBonus` both set to 999 and confirmed
    `HealthMax`/`ManaMax` were byte-for-byte unchanged from before equipping; added +50/+30 to
    `PotionHealthMaxBonus`/`PotionManaMaxBonus` and confirmed both Max stats increased by exactly
    that amount (proving Potion bonuses still apply correctly); then called
    `AddTemporaryHealthMaxBonus`/`AddTemporaryManaMaxBonus` (500 each) and confirmed both Max stats
    stayed exactly at their post-potion values, unaffected. *(16:54 EDT)*

## 2026-08-18

66. **Inventory/bank hover tooltips highlight stat lines that beat the currently equipped item, in
    green.** Backlog item, resolved by scope rather than by asking: comparison only applies to the
    inventory/bank hover tooltips (`InventorySystem.Draw()`/`BankSystem.Draw()`), not the four
    equip-slot `DrawEquipped()` tooltips (`Weapon.cs`/`Armor.cs`/`Ring.cs`/`AbilityItem.cs`) — an
    already-equipped item compared against itself is always equal on every stat, so there's nothing
    meaningful to highlight there. New `Equipment.ComparisonLines(Equipment equipped)` (virtual,
    parallel to the existing `TooltipText()`) returns the same content as individual `(string,
    bool)` line/is-better pairs instead of one flat string — `HeaderLines()` (Tier/Name/Description,
    factored out for reuse, never colored) plus `BonusComparisonLines()` (each of the 8
    `Equipment` bonus fields as its own line, compared against `equipped`'s corresponding field,
    `mine > theirs`). `Weapon`/`AbilityItem` override it the same way they already override
    `TooltipText()`: `Weapon` compares average damage (`(Min+Max)/2`) instead of a raw bonus field;
    `AbilityItem` adds the same average-damage comparison plus a `ManaCost` line — the one stat
    where *lower* is the improvement, and only counted as better when something is actually
    equipped in that slot (comparing against an empty slot's cost of 0 would make every real cost
    look like a downgrade). `equipped` is never a null reference for this comparison — Player's
    Weapon/Armor/Ring/AbilityItem fields are always real (possibly blank/zero-stat, never `null`)
    objects, confirmed from `Player()`'s constructor — so an empty slot naturally compares as
    "anything positive beats nothing" without extra null-handling. New `Util.DrawTooltip` overload
    takes the line list directly, sizing the background panel from the widest line and drawing each
    at its own `SpriteFont.LineSpacing`-stepped Y offset with its own color (existing `Color.Red` or
    new `Color.DarkGreen` for a better line) — the original single-string overload is untouched and
    still used by the four equip-slot tooltips and non-equipment item names (potions). Verified via
    a scripted repro: gave the equipped Ring a known nonzero baseline (so "worse" was actually
    testable, not just "any positive number beats zero"), then confirmed a higher-Attack candidate
    Ring's Attack line reported better while its lower-Defense line correctly didn't; confirmed a
    higher-average-damage candidate Weapon's Damage line reported better and a lower-average one
    didn't; confirmed a cheaper candidate AbilityItem's Mana Cost line reported better while a
    pricier one didn't (proving the inverted lower-is-better direction); confirmed a real mana cost
    is NOT flagged better when compared against a genuinely empty ability-item slot; and populated
    both the inventory and bank grids with comparison-eligible items, calling both real `Draw()`
    methods through a `SpriteBatch.Begin()/End()` pass with no exception. Two test-harness gaps
    fixed along the way, not game bugs: `Util.WrapText()` needs a non-null `Description`, which
    hand-constructed test items don't get by default (every real item loaded from JSON always has
    one); and the real `Draw()` render pass needs a non-null `.image` on whatever's sitting in the
    test slot, for the same reason. *(09:23 EDT)*
67. **Loot bag items now show the same full tooltip on hover as everywhere else — Tier/name/
    description/bonuses for equipment (with entry 66's green comparison highlighting), plus a
    background panel — instead of just a bare item name floating with no panel.** Loot bags were
    the one remaining UI spot still using the old, more primitive treatment: `Item.DrawLoot()` drew
    only `Name` via a raw `spriteBatch.DrawString`, no `Equipment` details, no background. Since
    `Item.DrawLoot()` had exactly one caller (`LootBag.DrawLoot()`, confirmed via a repo-wide grep),
    the logic was moved inline into `LootBag.DrawLoot()` and `Item.DrawLoot()` deleted outright,
    mirroring the exact hover-tooltip pattern already used in `InventorySystem.Draw()`/
    `BankSystem.Draw()`: resolve the item's equipped counterpart (`Weapon`/`Armor`/`Ring`/
    `AbilityItem`), call `Equipment.ComparisonLines()`, and draw through the new per-line
    `Util.DrawTooltip` overload; non-equipment items still just show their name, now through
    `Util.DrawTooltip`'s single-string overload (adding the background panel that was missing
    before, for consistency with every other tooltip in the game). Loot-bag items are drawn centered
    on `x`/`y` (unlike the inventory/bank grids' top-left-anchored slots), so the tooltip position
    still needed the old code's horizontal-centering math (`x - halfWidth`), recomputed against the
    widest line in the list instead of a single string's width. Caught and fixed a real bug while
    wiring the multi-line list type through: `var lines = cond ? ComparisonLines(...) : new
    List<(string, bool)> { ... }` silently loses its tuple element names when the compiler merges
    two differently-named-but-structurally-identical tuple types through a ternary, so `line.Text`
    failed to compile — fixed by declaring `lines` with the explicit `List<(string Text, bool
    Better)>` type instead of `var` in all three call sites (`LootBag.cs`, and the pre-existing ones
    in `InventorySystem.cs`/`BankSystem.cs` from entry 66, which happened to only ever call
    `.Count`/pass the list onward and so never actually hit the same compile error, but carried the
    same latent footgun). Verified via a scripted repro that actually drove real hover detection
    (not just a non-hovered pass) by writing directly to `Input.mouse` — a public static field — at
    the item's exact known screen coordinate rather than moving the real OS cursor: confirmed
    hovering an Equipment item renders the comparison tooltip with no exception and the real
    `Hover` flag flips true (proving the mouse-intersection check genuinely fired, not just that the
    non-hover fallback path compiles); confirmed the same for a non-equipment item (a potion) using
    the plain-name branch. *(10:02 EDT)*
68. **`Overlay.DrawStats()`'s "maxed" green highlight now only fires off a stat's permanent
    (level + potion) value, excluding equipment and temporary bonuses.** Previously compared the
    live, fully-bonused stat (`Player.Instance.Attack`, etc. — base + level + potion + equipment +
    temporary, per each class's `RecalculateStats()` formula) directly against the fixed level cap
    (`MaxAttack`, etc.), so equipping strong gear or triggering a timed buff (e.g. Knight's Shield
    Slam) could make a stat display green even though nothing permanent had actually reached the
    cap — misleading, since unequipping the gear or letting the buff expire would drop it right back
    below. Same underlying distinction as [BUGFIXES.md](BUGFIXES.md)'s HealthMax/ManaMax fix (only
    counting level + `PotionXxxBonus`, not `EquipmentXxxBonus`/`TemporaryXxxBonus`), applied here to
    the sidebar's color logic instead of a stat formula. Added six new public `Player` properties —
    `PermanentAttack`/`PermanentDefense`/`PermanentSpeed`/`PermanentDexterity`/`PermanentVitality`/
    `PermanentWisdom` — each just the live stat minus its own `EquipmentXxxBonus`/`TemporaryXxxBonus`
    (both already tracked live elsewhere in `Player.cs`), rather than re-deriving the full per-class
    level/potion formula a second time; named "Permanent" instead of "BaseX" since `Vitality`
    already has an unrelated field called `BaseVitality` (the level-1 starting value) that a
    same-named property would have collided with. `Overlay.DrawStats()`'s six color comparisons
    switched from `Player.Instance.Attack >= Player.Instance.MaxAttack` to
    `Player.Instance.PermanentAttack >= Player.Instance.MaxAttack` (etc.) — the displayed number
    itself is unchanged, still the full bonused stat; only which value decides the color changed.
    Deliberately left `InventorySystem.cs`'s separate `Attack >= MaxAttack`-style check (which gates
    whether a stat potion still applies) untouched — a different concern, out of scope for this
    request. Verified via a scripted repro on a fresh Wizard: confirmed `PermanentAttack` equals
    `Attack` with no bonuses active; confirmed a large temporary Attack buff pushed `Attack` past
    `MaxAttack` (would have shown green under the old logic) while `PermanentAttack` stayed
    unaffected and correctly still read as not-maxed; confirmed the identical result for a
    high-`AttackBonus` equipped weapon; and confirmed a genuinely earned `PotionAttackBonus` large
    enough to reach the cap on its own correctly made `PermanentAttack` read as maxed — proving the
    highlight now tracks something that can't be faked by gear or a buff, but still fires for real
    permanent progress. *(11:00 EDT)*
69. **Sidebar stat lines now show the cap alongside the value — `"Attack: N / Max"` instead of just
    `"Attack: N"`.** Straightforward follow-on to entry 68: `Overlay.DrawStats()`'s six core-stat
    lines already compared `PermanentAttack` (etc.) against `MaxAttack` to decide the line's color,
    but only ever displayed the raw `Attack` value with no cap in sight — a player had no way to see
    how close to maxed they actually were without checking this log. Per the user's explicit
    request, each line now shows `PermanentAttack` (not the gear/buff-inflated `Attack`) alongside
    `MaxAttack`, so the two numbers displayed together are consistent with each other and with the
    color: `"Attack: " + PermanentAttack + " / " + MaxAttack`, same shape for Defense/Speed/
    Dexterity/Vitality/Wisdom. Verified via a scripted repro: rendered `Overlay.DrawSidebar()`
    through a real `SpriteBatch.Begin()/End()` pass for all three classes with no exception —
    confirms the new string concatenations (including `Speed`'s `float`) format and draw cleanly,
    not just compile. *(11:18 EDT)*
70. **Sidebar stat lines' displayed number switched back to the equipment-inclusive stat — `"Attack:
    " + Attack + " / " + MaxAttack`, not `PermanentAttack`.** Follow-up correction to entry 69: that
    entry displayed `PermanentAttack` (excluding equipment) so the number would always agree with
    the line's "maxed" color, but the user wants gear's contribution visible in the number itself —
    equipped gear is a real, standing part of the stat until unequipped, unlike a timed buff. All
    six lines (Attack/Defense/Speed/Dexterity/Vitality/Wisdom) now show the plain `Player.Instance.X`
    field again (which already includes `EquipmentXxxBonus` and, incidentally, any active
    `TemporaryXxxBonus` too — not specifically excluded, since the request was about equipment) —
    the "maxed" color check is untouched, still comparing `PermanentX` against `MaxX`, so the two
    numbers on a line can legitimately disagree (a decorated character shows a big number in a still-
    red line) — that's the intended reading now, not a bug. Verified via a scripted repro: equipped
    a weapon with a 1000 `AttackBonus` on a fresh Wizard, confirmed `Attack` (1042) reflects it while
    `PermanentAttack` (42) — and therefore the color — stays unaffected and correctly still below
    `MaxAttack` (60); rendered `Overlay.DrawSidebar()` through a real `SpriteBatch.Begin()/End()`
    pass with no exception. *(11:24 EDT)*
72. **Sidebar stat lines revised again to show all three numbers at once — `"Attack: N (Permanent /
    Max)"` — superseding entry 70's plain `"Attack: N / Max"`.** The prior two passes (69, 70)
    couldn't show both "how strong am I right now" and "how close to permanently maxed am I" at the
    same time, since only one number could occupy the `X / Max` slot. Per the user's explicit
    request, each line now shows the live, fully-bonused stat first (`Player.Instance.Attack`,
    unchanged from entry 70 — includes equipment and any active temporary buff), then the permanent
    value and cap in parentheses (`Player.Instance.PermanentAttack` / `Player.Instance.MaxAttack` —
    the same pairing the "maxed" color check already uses). Applied identically to all six core stats (Attack/Defense/Speed/
    Dexterity/Vitality/Wisdom); the color logic is completely unchanged, still comparing
    `PermanentX >= MaxX`. Example: a heavily-geared but not-yet-maxed character reads `"Attack: 142
    (42 / 60)"` — 142 is what's actually dealt in combat right now, 42/60 is real permanent
    progress toward the cap. Verified via a scripted repro: rendered `Overlay.DrawSidebar()` through
    a real `SpriteBatch.Begin()/End()` pass for all three classes with no exception, and logged each
    class's `Attack`/`PermanentAttack`/`MaxAttack` triple to confirm the three values feeding the new
    format are what's actually expected (e.g. Wizard: 42/42/60 with no gear bonus active, matching
    the plain-stat baseline). *(12:07 EDT)*
74. **Sidebar stat values now line up in a vertical column, independent of each line's label
    width.** Immediate follow-up to entry 72 and [BUGFIXES.md](BUGFIXES.md) entry 40's row-spacing fix:
    each stat line was still one concatenated `"LABEL: value"` string, so the value's horizontal
    start position drifted with however wide that particular label rendered (e.g. `"Level:"` is
    noticeably wider than `"ATT:"`, `"DEF:"`, etc.), leaving the numbers visually staggered even
    with even row spacing. `Overlay.DrawStats()` now draws each line as two separate `DrawString`
    calls via a small local `DrawStatLine(label, value, rowY, color)` helper — the label always at
    the row's `x`, the value always at a shared `valueX` computed once from the widest label's
    measured width (`Art.HudFont.MeasureString("Level:").X + 4`) — so every value column aligns
    regardless of which label is on that row. Verified visually: rendered `Overlay.DrawSidebar()` to
    a `RenderTarget2D` and inspected the saved image directly — all 7 values (Level's number and the
    six `N (perm / max)` stat strings) now start at the same x position, forming a clean column.
    *(13:17 EDT)*
75. **Two of the four deferred "boss follow-ups" — a real top-of-screen boss health bar and a
    fade-out boss-appearance announcement — picked up from the backlog** (the other two, real
    hit/death sounds and rebalancing, need user-supplied assets/playtesting feedback and stayed
    deferred). `Boss.cs` gained public `Health`/`HealthMax` read-only views onto `Enemy`'s protected
    health fields (encapsulation preserved — nothing outside `Enemy`/its subclasses can *set* them),
    and a new `EntityManager.ActiveBoss` (`enemies.OfType<Boss>().FirstOrDefault()`) lets the HUD
    find the current boss without `EntityManager` exposing its private `enemies` list. `RealmState`
    gained a `protected virtual void DrawBossHud(SpriteBatch)` extension point (empty by default,
    called from the same screen-space `spriteBatch.Begin()/End()` block as the rest of the HUD in
    `Draw()`) — the same "virtual hook the base doesn't need, the boss arena does" pattern already
    used for `SpawnsRegularEnemies`/`InstanceWorldWidth`/`Height`. `BossRealmState` overrides it to
    draw: (1) the boss's name centered above a 400×24 black/dark-red health bar at the top of the
    gameplay viewport, driven by `Health`/`HealthMax`, replacing the small floating bar every other
    enemy draws over its own sprite — `Enemy.DrawHealthBars()` is now `virtual`, and `Boss` overrides
    it to a no-op so the two bars don't both draw; (2) a fading name banner (`Art.TitleFont`, same
    drop-shadow style as the Main Menu title) for the first 3 seconds after the fight starts (120
    frames fully visible + 60 frames fading via a straight alpha ramp), scaled down to fit the
    gameplay viewport's width instead of drawing at native size — `Art.TitleFont` is sized for the
    Main Menu's single-word title, and Limon's full name at scale 1 overflowed off both edges of the
    screen, caught during visual verification and fixed by measuring the raw string width and
    computing a `Math.Min(1f, (viewportWidth - margin) / rawWidth)` scale factor before drawing.
    Purely visual — no new sound asset, since the user didn't ask for the hit/death-sound follow-up
    in this pass and the fight already has music via the existing `Sound.PlaySong()`. Verified by
    constructing a real `BossRealmState` (which internally spawns the real `LimonTheSpriteGoddess`)
    and rendering its actual `Draw()` to a saved screenshot at two points: immediately after
    entering (confirmed the announcement banner fits on-screen and the health bar reads full), and
    after skipping past the announcement window and applying 5×1000 damage via the real `WasShot()`
    (confirmed the banner is gone and the bar visibly reflects the new `7080/12000` health,
    matching Defense-reduced damage exactly); also confirmed via `git status`/direct JSON read that
    the real `PlayerData_Wizard.json`/`InventoryData_Wizard.json`/`BankData.json` save files
    (necessarily re-saved by `RealmState`'s constructor, since entering any dungeon does that) were
    still valid, unmutated JSON after the test — the load-then-construct order meant nothing was
    saved except the same state that was just loaded. *(13:29 EDT)*
80. **First slice of "what Fame unlocks" (backlog item, deliberately deferred when the Fame
    earn/persist/display pipeline was first built — see entry 29): character class unlocks.**
    Resolved via `AskUserQuestion` across two rounds — the user's full vision is much larger
    (skins, extra bank storage, alternate starting gear tiers, an "raise all stats to max" unlock,
    and a spendable Fame shop for individual purchases), too much to build in one pass, so this
    entry is just the first piece: Wizard starts unlocked; Archer requires 1,000 account-wide Fame;
    Knight requires 3,000. Deliberately gated off the *existing* `FameSystem.Fame` value directly
    rather than adding any new persisted "unlocked" flag — Fame only ever increases (death/delete
    convert Score into it, nothing ever spends it yet), so "has this much Fame ever been earned" and
    "is this permanently unlocked" are the same question, and a class literally can't contribute
    Fame before its own threshold unlocks it, so the single cumulative total already behaves as the
    per-class progression ladder the user described without needing separate per-class-earned
    tracking. `CharacterSelectState.cs`'s `Slot` gained `RequiredFame` (0 for Wizard) and a computed
    `IsLocked` property; `Update()` skips a locked slot's save-peek/delete-controls/selection
    entirely (clicking it plays the error sound instead of calling `SelectCharacter()`); `Draw()`
    grays out the portrait/border/label for a locked slot and shows a `DrawLockedPreview()` message
    ("Requires N Fame (You have M)") on hover instead of the normal stat/score preview. Verified via
    a scripted repro that only read/temporarily overrode the static `FameSystem.Fame` value
    (restored before exit, no `Save*()` or `SelectCharacter()`/`StateManager.NewGame()` call at any
    point — deliberately conservative after entries 42/43's incident) — confirmed via reflection
    into `CharacterSelectState`'s private `slots` list that `IsLocked` read correctly at three Fame
    levels (500: only Wizard unlocked; 1,500: Wizard+Archer; 5,000: all three), and that `Draw()`
    renders without exception. The user's own account (7,493 Fame going into this) already clears
    both thresholds, so nothing changes for their live save — confirmed `FameData.json` read back
    unchanged after the test. The remaining pieces (skins, bank storage, starting gear, stat-max,
    the shop) stay open in the backlog. *(16:19 EDT)*
81. **Third per-class progression metric alongside Level and Score/Fame: a 0-5 star rating**,
    permanent and shown on Character Select. Star 1 is reaching the level cap (20); each star
    beyond that needs an exponentially higher `HighScore` (that class's permanent best-ever run
    Score, already tracked and already survives death/delete — see entry 29): 20,000 for Star 2,
    doubling each star after (40,000 / 80,000 / 160,000 for Stars 3-5). Needed one new piece of
    persisted state — `Player.HasReachedLevel20` (set once inside the shared `LevelUp()` when
    `Level` hits 20, mirroring `HighScore`'s exact "survive death/delete" treatment: preserved
    through `Util.DeleteCharacterData()`'s reset-not-delete path and
    `GameOverState`'s post-death reset, threaded through `Data/PlayerData.cs` and
    `Util.cs`'s `BuildPlayerData()`/`LoadOrCreatePlayer()`) — since raw `Level` itself resets to 1
    on both, so it alone can't answer "has this class *ever* hit 20." Deliberately did **not** add
    a separate persisted star count: `CharacterSelectState.ComputeStars(bool hasReachedLevel20, int
    highScore)` derives 0-5 fresh from those two already-reliable inputs each frame, so there's
    nothing for a stored star number to ever drift out of sync with. Shown as `"Stars: **---"`-style
    plain-ASCII text (not a real ★ glyph — the game's `SpriteFont`s only bake in the standard ASCII
    range 32-126, confirmed via `Content/Fonts/*.spritefont`, and drawing an unsupported character
    throws rather than substituting) directly below each class's label on Character Select,
    unconditionally for every slot — locked or unlocked — since it's a record of what's already
    been earned, not gated by whether the class happens to be selectable right now. Verified via a
    scripted repro that deliberately called no `Save*()`/`DeleteCharacterData`/`GameOverState` path
    (all three write to real per-class save files — kept strictly in-memory after entry 42's
    incident): `ComputeStars()` checked via reflection across 9 (reached20, highScore) combinations,
    matching every threshold boundary exactly (0 when never reached 20; exactly 1/2/3/4/5 at each
    of 0/20,000/40,000/80,000/160,000, capped at 5 beyond); a fresh throwaway `Wizard` confirmed
    `HasReachedLevel20` stays `false` leveling 5→6 and flips `true` leveling 19→20; `BuildPlayerData()`
    confirmed both new/existing fields round-trip into the DTO correctly. Confirmed via direct file
    read afterward that the real `PlayerData_*.json`/`FameData.json` on disk were untouched. Note:
    existing real saves (this account's Archer/Knight both show substantial `HighScore` — 10,974 and
    60,364 — strongly suggesting real past Level-20 runs) will read `HasReachedLevel20 = false` by
    default, since the field didn't exist before this entry and nothing retroactively backfills it
    — flagged to the user rather than silently mutating real save data to grant retroactive credit.
    *(16:39 EDT)*
82. **Star system follow-up: immediate persistence + retroactive Star 1 backfill.** Two asks after
    entry 81 shipped. (1) Stars previously could only get saved to disk at an existing checkpoint
    (death, entering the Nexus/a dungeon, Character Select) — a player who hit Level 20 or crossed
    a new `HighScore` star threshold and then, say, force-quit before the next checkpoint would lose
    that star. Fixed by moving `ComputeStars()` (and its threshold constants) from
    `CharacterSelectState` onto `Player` as `public static`, so both call sites could use it:
    `Player.LevelUp()` now saves immediately the moment `HasReachedLevel20` first flips `true`
    (guarded so it only fires once, at the 19→20 transition), and `RealmState.Update()`'s
    `HighScore`-update block now computes `ComputeStars()` before and after bumping `HighScore`,
    saving immediately only when that comparison shows a new star was actually crossed (not on
    every `HighScore` tick, which happens every frame during active play — only a real threshold
    crossing is worth a disk write). (2) Retroactively granted the account's real Archer (HighScore
    10,974) and Knight (HighScore 60,364) accounts the Star 1 flag flagged as missing in entry 81 —
    per the user's explicit go-ahead, backed up both real `PlayerData_Archer.json`/
    `PlayerData_Knight.json` first (`.bak`, per CLAUDE.md), then set
    `HasReachedLevel20: true` directly in both real files via a targeted single-field edit (verified
    via diff that no other field changed), confirmed both still valid JSON. Since stars are always
    *derived* (never separately stored — see entry 81), this single flag flip alone is what now
    correctly computes Archer at 1 star and Knight at 3 stars (60,364 clears both the 20,000 and
    40,000 thresholds) — no separate star-count edit needed or made. Verified via clean
    `dotnet build` (0 errors) and a direct `Realm.exe` boot-check (process stayed up, no exception
    output) rather than a live in-game persistence test, to avoid any further risk to real save
    files after entry 42's incident — the immediate-save logic itself was verified by code review
    plus the successful build, and the backfill's correctness by hand-computing `ComputeStars()`
    for both accounts' real `HighScore` values.
    *(16:50 EDT)*
83. **Auto-fire toggle for the basic attack**, picked directly off the backlog. Resolved via
    `AskUserQuestion`: toggle key is `C` (a free key — nothing else in `Input.cs` used it), and the
    setting is session-only (resets to off on every launch) rather than persisted — the user's own
    recommended defaults. New `Player.AutoFireEnabled` (`public bool`, deliberately absent from
    `PlayerData`/`Util.BuildPlayerData()`/`LoadOrCreatePlayer()`, which is what actually makes it
    non-persisted — nothing needed to explicitly clear it on load). `Player.Update()`'s shoot
    condition changed from `Input.mouse.LeftButton == ButtonState.Pressed` to
    `(Input.mouse.LeftButton == ButtonState.Pressed || AutoFireEnabled)` — the existing per-frame
    cooldown gate (`projectileCooldown >= projectileCooldownCount`) is untouched, so auto-fire fires
    at the exact same rate holding the mouse button already would, just without needing to hold it.
    `Input.cs`'s universal input block gained the `C`-key toggle, flipping
    `Player.Instance.AutoFireEnabled`. Added a small `"Auto-Fire: ON"` cyan text indicator to
    `Overlay.DrawStats()`, drawn only when the flag is on, sitting in the existing gap between the
    stat block (ends at `y+96`) and `DrawExperience` (starts at `y=160`) so it never collides with
    either section when off — otherwise there'd be no way to tell the toggle actually worked.
    Verified via a scripted repro that deliberately avoided any save-triggering construction
    (`NexusState`'s constructor doesn't save, unlike `RealmState`'s — confirmed via grep before
    using it) and touched no real save file: with the flag off and the mouse held released for 200
    frames of `EntityManager.Update()`, the entity count stayed flat (no shots); with the flag then
    switched on for another 200 frames with the mouse still released, the count rose (5 real
    projectiles fired on cooldown with nothing held); `Overlay.DrawSidebar()` rendered through a real
    `SpriteBatch.Begin()/End()` pass with no exception in both the on and off state. *(08:57 EDT)*
84. **Enemy spawn density scales with distance from where the player entered the current Realm
    instance**, picked off the backlog. Resolved via `AskUserQuestion` among three raised
    directions (level-gated enemy types, spawn density by distance, wave/pack spawning) — the other
    two stay open in the backlog, since either could still layer on top of this one later. "Distance
    from the Nexus" needed reframing: `Player.Instance.Position` is never reset when entering a
    fresh `RealmState` (confirmed via grep — no state constructor besides `Player`'s own does that,
    and `Player`'s constructor only runs once per class), so the player carries their exact absolute
    world position straight over from wherever they were standing in the Nexus — a freshly-generated
    open-world dungeon has no fixed "entrance" of its own to measure from otherwise. New
    `EnemySpawner.SetEntryPosition(Vector2)` captures `Player.Instance.Position` at that exact
    moment; `RealmState`'s constructor calls it (gated on `SpawnsRegularEnemies`, so it's a no-op for
    `BossRealmState`, which never runs `EnemySpawner.Update()` anyway). `EnemySpawner.Update()`'s
    three basic-enemy-type rolls (Seeker/Wanderer/Snake — SpriteGod's separate level-based roll is
    untouched) now use an `effectiveInverseSpawnChance` that lerps from the existing time-based
    `inverseSpawnChance` down toward a new floor (`MinDistanceInverseSpawnChance = 15`, denser than
    the time-ramp's own floor of 20) as `Vector2.Distance(Player.Instance.Position, entryPosition)`
    approaches `MaxDistanceForFullDensity` (20,000 units — roughly a minute of sustained walking at a
    mid-range Speed stat, picked as a reasonable "explore outward" pace rather than derived from
    anything more precise). Verified via a scripted repro that deliberately called no
    `RealmState`/`Save*()` (constructing a real `RealmState` saves unconditionally — used `NexusState`
    for `Camera` setup instead, same as recent tests, and called `EnemySpawner.SetEntryPosition()`
    directly to bypass `RealmState` entirely): with the player planted exactly at a manually-set entry
    position, 600 calls to `EnemySpawner.Update()` spawned 28 enemies; with `EnemySpawner.Reset()`
    called first (isolating the distance effect from the time-based ramp) and the player moved to
    1.5× `MaxDistanceForFullDensity` away, the same 600 calls spawned 132 — roughly the ~4× ratio
    expected from the 60→15 `inverseSpawnChance` range. `RealmState`'s one-line wiring change itself
    was verified by direct code review rather than a live construction, to keep avoiding any
    real-save-file risk. *(09:31 EDT)*
85. **"Erase All Data" button on Character Select** — wipes every class's save, the shared bank,
    account-wide Fame, and every star (derived from the wiped `HasReachedLevel20`/`HighScore`, not
    separately stored — see entry 81), behind two full confirmations per the user's explicit
    request. New `Util.EraseAllAccountData()` deletes all 3 classes' `PlayerData_*.json`/
    `InventoryData_*.json`, `BankData.json`, and `FameData.json` outright — deliberately unlike
    `DeleteCharacterData()` (used by the existing per-character Delete link), which preserves
    `HighScore`/`HasReachedLevel20` on purpose; this wipe preserves nothing, since the user asked
    for high scores and star progress erased too. Also clears in-memory state so nothing gets
    silently resurrected by a later autosave: `Array.Clear(BankSystem.Records, ...)` (a fixed-size
    `readonly` array — contents cleared, not reassigned), `FameSystem.Fame = 0` (which is also what
    re-locks Archer/Knight immediately, since class unlocks read `FameSystem.Fame` live rather than
    a separate stored flag — see entry 80), and a full `Util.ResetPlayer()` of the live instance,
    mirroring `DeleteCharacter()`'s existing same-class-currently-loaded handling. UI: a new
    `"Erase All Data"` button in `CharacterSelectState`, deliberately kept out of the shared `Menu`
    (which centers every button in its list vertically around screen-center in a stack — a second
    entry there would collide with the class portraits already occupying that space) and given its
    own fixed bottom-left position instead. Clicking it opens a two-stage modal (a
    `private enum EraseStage { None, Warning, FinalConfirm }`) — full-screen dim, a centered warning
    box, Cancel, and a confirm button whose text/action changes per stage (`"Continue"` →
    `"Yes, Erase Everything"`) — deliberately more visually severe than the inline "Delete save?
    Yes/No" row the per-character Delete link uses, matching how much more destructive this is.
    `Update()` short-circuits to only updating the modal's own two buttons while it's open, so
    nothing underneath (class slots, the button that opened it) is still clickable through it.
    Verified via a scripted repro that — given this feature deletes real save files by design —
    started by backing up all 8 real save files first (per CLAUDE.md), then called
    the real `Util.EraseAllAccountData()` for real: confirmed `FameSystem.Fame` 7,493 → 0,
    `BankSystem.Records` 5 filled slots → 0, the live `Player.Instance` (Wizard, Level 20, HighScore
    5,813, `HasReachedLevel20` presently `False` for this account) reset to Level 1/HighScore 0, and
    `PeekPlayerData()` returning `null` for all three classes with `BankData.json`/`FameData.json`
    confirmed gone from disk — then immediately restored all 8 files from the backup and verified
    via `diff` that every one was byte-identical to its pre-test state before doing anything else.
    *(09:42 EDT)*
86. **Erase-all modal follow-up: reversed button positions on the second screen, and the final
    confirm button turned red.** The `FinalConfirm` screen now lays out `eraseConfirmButton`
    ("Yes, Erase Everything") on the left and `eraseCancelButton` on the right — the opposite of the
    `Warning` screen's Cancel-left/Continue-right — so a reflexive "click the same spot twice"
    lands on Continue then Cancel, not Continue then the actual destructive action. New
    `PositionEraseButtons()` computes both positions from the current `eraseStage`, called whenever
    the stage changes (opening the modal, advancing `Warning` → `FinalConfirm`, and after a
    completed erase resets back to `None`) rather than once in the constructor. `eraseConfirmButton`
    also switched from `Color.DarkRed` to a plain `Color.Red` PenColor — applies to both its
    "Continue"/"Yes, Erase Everything" states, since it's a single `Button` instance whose text
    changes rather than two separate buttons. Verified via a scripted repro: constructed a real
    `CharacterSelectState` (its constructor does nothing save-related — confirmed via the same code
    read used for entry 85 — so no backup needed this time), used reflection to force `eraseStage`
    through `Warning` then `FinalConfirm` and invoke `PositionEraseButtons()` directly at each step,
    and confirmed Cancel's X (470) was less than Confirm's X (650) on `Warning` but greater (650 vs.
    470) on `FinalConfirm` — a genuine swap, not just two different numbers — plus confirmed
    `eraseConfirmButton.PenColor` reads back exactly `Color.Red` (255,0,0,255). *(09:50 EDT)*
87. **XP field cleanup: removed `Player.Experience`/`ExperienceNextLevel` as separately-tracked
    fields, and the HUD XP bar now shows cumulative total XP instead of resetting to empty on every
    level-up.** Prompted by the user asking directly whether `Experience`/`ExperienceNextLevel`/
    `ExperienceTotal`/`HighScore` had any redundancy and whether the level math could be simplified.
    Answer: `ExperienceTotal` (this life's running score, incremented directly on kill, never reset)
    and `HighScore` (the permanent best-ever `ExperienceTotal`, survives death/delete — see entry 29)
    are genuinely distinct and both stay. `Experience` (progress within the current level, reset to 0
    every level-up) and `ExperienceNextLevel` (that level's own XP requirement) were both fully
    redundant with `Level` + `ExperienceTotal`, given the game's fixed per-level XP formula (`50` for
    Level 1, `50 + level*100` for every level after) — removed as stored fields entirely. New
    `Player.ExperienceRequiredForLevel(int)` (private, the per-level formula) and
    `Player.CumulativeExperienceForLevel(int)` (public static, sums it up to a given level — at most
    19 additions since Level caps at 20, cheap enough to just recompute rather than cache) back a new
    `ExperienceNextLevel` computed property (`CumulativeExperienceForLevel(Level + 1)`) — same name,
    but now returns a *cumulative* threshold instead of a per-level delta, which is what makes it
    directly comparable to `ExperienceTotal`. `Update()`'s level-up check and `LevelUp()` itself both
    simplified accordingly (no more manually resetting `Experience`/reassigning `ExperienceNextLevel`
    on every level-up). Found and fixed one live bug in the process: `Util.LoadOrCreatePlayer()` DID
    restore `Experience` from a save, but the old system independently incremented it in `Enemy.cs`
    on every kill (a second, separate accumulator from `ExperienceTotal`, doing the same job) — the
    two could only ever match by construction, and worse, `LevelUp()`'s unconditional
    `Experience = 0` silently discarded any XP overshoot past a level's exact threshold on a big kill.
    Deriving everything from `ExperienceTotal` alone means no XP is ever discarded anymore. Data
    schema shrunk to match — `Experience`/`ExperienceNextLevel` removed from `PlayerData`/
    `Util.BuildPlayerData()`/`LoadOrCreatePlayer()`; old save files with those now-orphaned JSON keys
    still load fine (`System.Text.Json` silently ignores unmapped properties by default — confirmed
    via a real boot against this account's actual save files, all three still containing the old
    keys). `Overlay.DrawExperience()`'s bar-fill math switched from the old per-level `Experience` to
    `ExperienceTotal` (matching the text line above it, which — per the user's own prior edit to this
    file — already showed `ExperienceTotal`), so the bar's fill now climbs continuously across a
    level-up instead of snapping back to empty, only dropping by however much the next cumulative
    threshold actually rose. Added a `Math.Min(100, ...)` clamp on the fill percentage while there —
    a single large kill can transiently put `ExperienceTotal` past the *current* level's
    `ExperienceNextLevel` for the one frame before `Update()`'s own check catches up and levels the
    character again, which would otherwise draw the gold fill past the bar's own black background for
    that frame (a latent quirk that existed before this change too, via the old separate
    `Experience += PointValue` accumulator — not newly introduced, just now cheap to guard against
    while already touching this exact line). Verified via a scripted repro on a fresh throwaway
    `Wizard` (no `Save*()`/`RealmState` — no real save-file risk): `CumulativeExperienceForLevel`
    checked at 1/2/3/4 (0/50/300/650, matching the formula by hand); confirmed a fresh character
    reads `Level=1, ExperienceTotal=0, ExperienceNextLevel=50` (exactly the old hardcoded initial
    value); confirmed setting `ExperienceTotal=50` and running the level-up check actually leveled to
    2 with the new `ExperienceNextLevel=300`; confirmed `ExperienceTotal=299` (just under 300)
    correctly does NOT qualify for level-up; confirmed a big single jump to `ExperienceTotal=1000`
    (well past the Level-3 threshold) leveled to 3 with `1000 - CumulativeExperienceForLevel(3) = 700`
    XP correctly still counted toward Level 4 (nothing discarded); confirmed the bar-fill formula hit
    153 before the clamp was added and exactly 100 after. Also confirmed via a real boot-check
    (existing save files, all three classes) that loading still works cleanly with the now-unused
    `Experience`/`ExperienceNextLevel` keys still present in the real JSON. *(10:15 EDT)*
88. **Follow-up to entry 87: the XP bar's fill resets to empty on every level-up again, but the
    printed numbers still show the cumulative total, never 0.** The user's actual ask, refined after
    seeing entry 87 land — the bar's *fill* should behave like the pre-87 per-level bar (visually
    empty right at the start of a level), while the text label next to it keeps showing real
    cumulative progress rather than resetting to "0 / N". `Overlay.DrawExperience()`'s fill math now
    subtracts the current level's own starting threshold
    (`Player.CumulativeExperienceForLevel(Level)`) from both `ExperienceTotal` and
    `ExperienceNextLevel` before taking the ratio — `xpIntoLevel`/`xpNeededForLevel` reads as
    "progress since I hit this level" instead of "progress since Level 1", so it's 0% right after a
    level-up and 100% right before the next one, independent of `expString` above it (unchanged,
    still the plain cumulative `ExperienceTotal`/`ExperienceNextLevel` text). No new stored field
    needed — both quantities were already one subtraction away from what entry 87 already computes.
    Verified via a scripted repro on a fresh throwaway `Wizard` (no `Save*()`/`RealmState`): confirmed
    the bar reads 0% at `ExperienceTotal=0`, ~50% approaching the Level 1→2 threshold
    (`ExperienceTotal=25` of 50), then — the actual point of this fix — confirmed that immediately
    after leveling to 2 (`ExperienceTotal=50`), the bar dropped back to 0% while `ExperienceTotal`
    itself still correctly read 50 (not reset to 0), and confirmed ~50% again halfway through Level 2
    (`ExperienceTotal=175` of the 50-300 span). Real-save boot-check also passed. *(10:35 EDT)*
89. **Remappable key bindings + the game's first general settings system**, picked off the backlog.
    Resolved via `AskUserQuestion`: every gameplay action is remappable including movement (not just
    ability/interact keys as originally guessed on the backlog), the Settings screen is reachable
    both from the Main Menu and in-game, and this pass covers just key bindings — no other setting
    (e.g. `Player.AutoFireEnabled`) got folded in yet, staying open on the backlog as the natural
    next thing to add to the same system. New `KeyBindings.cs` (root namespace) holds 9 remappable
    `Action`s (`MoveUp/Down/Left/Right`, `UseAbility`, `UseHealthPotion`, `UseManaPotion`,
    `ReturnToNexus`, `ToggleAutoFire`) as a live `Dictionary<Action, Keys>`, seeded from a
    `Defaults` dictionary and exposing `Get`/`Set`/`FindConflict`/`ResetToDefaults` — deliberately
    excludes F3/PageUp/PageDown/Add/Subtract/M/Escape/Enter (debug and menu-nav keys, not "gameplay"
    in the sense worth exposing). `Input.cs`'s hardcoded `Keys.X` checks for all 9 actions (both the
    `WasKeyPressed()` calls and `GetMovementDirection()`'s 4 `IsKeyDown()` checks) now route through
    `KeyBindings.Get()`. New `Data/KeyBindingsData.cs` (a flat DTO, matching `FameData`'s shape
    exactly — one property per action, not a dictionary, for consistency with the rest of the
    codebase's save-file style) plus `Util.SaveKeyBindingsData()`/`LoadKeyBindingsData()` (same
    shape as every other `Save*`/`Load*` pair — `LoadKeyBindingsData()` now also called from
    `Game1.StartGame()`), backed by a new account-wide `KeyBindingsData.json` (not per-class, same
    reasoning as `BankData`/`FameData`). New `States/SettingsState.cs`: a two-column row list (label
    + current key, same alignment trick as `Overlay.DrawStats()`), click a row to enter "press any
    key" mode (`Input.GetAnyNewKeyPress()`, a new helper returning whichever key was newly pressed
    this frame — needed because `WasKeyPressed()` only checks one specific key, and `Input`'s
    `keyboard`/`previousKeyboard` fields are private), Escape cancels a pending rebind without
    changing anything, and rebinding onto a key another action already holds swaps the two
    (`KeyBindings.FindConflict()`) rather than letting two actions share a key. Every successful
    rebind and every Reset-to-Defaults saves immediately (same "why wait for a checkpoint"
    reasoning as the star system's immediate-save follow-up, entry 82). `SettingsState`'s
    constructor takes the exact `State` instance that opened it (`StateManager.OpenSettings(State
    returnState)`) and its Back button returns to that same object via `Game1.Instance.ChangeState(
    returnState)` directly — not a fresh reconstruction — so opening Settings mid-dungeon and
    backing out doesn't reset the camera, re-save, or otherwise disturb the in-progress
    `RealmState`/`NexusState`. One accepted, unavoidable side effect: `ChangeState()` is the game's
    one guaranteed choke point for `ItemSpawner.Reset()` (entry 43) — opening Settings from an
    active dungeon run still clears any loot bags on the ground, same as every other state
    transition; not worth special-casing around, since that would reintroduce the exact
    ghost-loot-bag bug class entry 43/44 fixed. Reachable via a new fixed (non-remappable) `O` key
    from anywhere except Settings itself, plus a new "Settings" button added to `MenuState`. Verified
    via two scripted repros, both careful about the brand-new `KeyBindingsData.json` this feature
    introduces (confirmed via `ls` beforehand that this account had no such file yet, so nothing
    real was at risk — restored that exact "no file" state after each test regardless): (1) direct
    `KeyBindings`/`Util` calls — default `MoveUp`/`MoveDown` read `W`/`S`; `Set(MoveUp, I)` then
    `FindConflict(MoveDown, I)` correctly returned `MoveUp`; the resulting swap left `MoveUp=S,
    MoveDown=I`; a full `Save`→`ResetToDefaults`→`Load` round-trip correctly restored `S`/`I`, not
    the defaults; `GetMovementDirection()` (via reflection into `Input`'s private keyboard fields,
    same technique as prior sessions' mouse-state tests) confirmed holding `I` now moves down and
    holding the old default `W` does nothing. (2) A real `SettingsState` constructed end-to-end:
    initial `Draw()` and `Draw()` mid-listen both rendered with no exception; simulating a real
    click on the "Move Up" row (via `Input.mouse`, which `SettingsState.Update()` actually reads —
    unlike `Controls.Button`, which polls `Mouse.GetState()` directly and so isn't simulate-able
    this way, same finding as every earlier session that needed this distinction) correctly entered
    listening mode; pressing `I` correctly completed the rebind and exited listening mode. The
    `Controls.Button`-based Back/Reset buttons weren't live-clicked for the reason above; their
    one-line lambda handlers were verified by direct code review instead. Both a clean `dotnet
    build` and a real boot-check against this account's actual (untouched) save files passed.
    *(13:44 EDT)*
90. **Toggle Mute added as a 10th remappable action, default key M** (matching its existing
    hardcoded default) — follow-up to entry 89. Same mechanical change in every spot the other 9
    actions touch: `KeyBindings.Action.ToggleMute` added to the enum/`AllActions`/`DisplayName`
    ("Toggle Mute")/`Defaults` (`Keys.M`)/`ToData()`/`FromData()`; `Data/KeyBindingsData.cs` gained a
    matching `ToggleMute` property; `Input.cs`'s hardcoded `WasKeyPressed(Keys.M)` for
    `Sound.ToggleMute()` now reads `KeyBindings.Get(KeyBindings.Action.ToggleMute)`. Verified via a
    scripted repro (no real save-file risk — same "confirm no `KeyBindingsData.json` exists first,
    restore that exact state after" discipline as entry 89): confirmed the default reads `M`; a
    `Save()`→disk→`Deserialize` round-trip correctly preserved `M`; `Set(ToggleMute, N)` then
    `FindConflict(ToggleAutoFire, N)` correctly returned `ToggleMute`; `ResetToDefaults()` correctly
    restored `M`. One transient anomaly along the way, not a real bug: the very first run immediately
    after rebuilding read `ToggleMute` as `None` instead of `M` — reflection into the private
    `Defaults`/`bindings` dictionaries showed both fully and correctly populated (`M`) even during
    that same anomalous run, and two more consecutive clean re-runs (plus the original simpler
    version of the test) all read `M` correctly every time — pointing to a one-off disk/AV-timing
    fluke on a just-rebuilt DLL's very first launch, not a logic defect; not chased further since it
    didn't reproduce. Clean build and a real boot-check against this account's actual (untouched)
    save files both passed. *(13:56 EDT)*
91. **Key bindings generalized to also accept mouse buttons, so an action can be bound to right-
    (or middle-) click, not just a keyboard key.** The user asked whether right-click specifically
    could be rebindable; answer was yes, but it needed the binding storage type itself widened first
    — `KeyBindings` previously stored a raw `Keys` per action. New `InputBinding.cs` (root
    namespace): a readonly struct wrapping either a `Keys` or a new `MouseButton` enum
    (`Left`/`Right`/`Middle`), with `FromKey()`/`FromMouseButton()` factories, value equality, a
    `ToString()` ("W" or "Mouse Right"), and `Serialize()`/`Deserialize()` to/from a flat `"Key:W"` /
    `"Mouse:Right"` string — chosen over a nested JSON object so `Data/KeyBindingsData.cs` could stay
    one flat `string` property per action (was `Keys`), matching the rest of the codebase's DTO
    style. Left is deliberately never offered as an assignable option (`Input.GetAnyNewInputBinding()`
    only checks Right/Middle beyond the keyboard) — it already has a fixed, unavoidable meaning
    (basic attack fire, every UI click) that binding another action onto it would collide with.
    `KeyBindings.Get/Set/FindConflict/Defaults` all switched from `Keys` to `InputBinding`; `Input.cs`
    gained `WasMouseButtonPressed()`/`IsMouseButtonDown()` (mirroring the keyboard equivalents) and
    binding-level `WasBindingPressed()`/`IsBindingDown()` that dispatch to the keyboard or mouse check
    based on `InputBinding.Kind` — every one of the 10 actions' call sites (both the `WasKeyPressed`
    calls and `GetMovementDirection()`'s 4 held-down checks) now go through these instead of the raw
    keyboard-only versions, so movement could also be bound to a mouse button even though only
    `UseAbility` was the actual ask this round. `SettingsState`'s listening flow swapped
    `Input.GetAnyNewKeyPress()` for the new `Input.GetAnyNewInputBinding()` — no other change needed
    there, since it already stored/displayed whatever `KeyBindings.Get()` returned generically.
    Verified via a scripted repro (same backup discipline as entry 89 — confirmed no real
    `KeyBindingsData.json` existed first, restored that state after): `InputBinding` serialize/
    deserialize round-tripped correctly for both a keyboard and a mouse binding, and equality
    correctly distinguished `Key:Space` from `Mouse:Right`; rebinding `UseAbility` to `Mouse Right`
    then a `Save→disk→Load` cycle correctly preserved it (confirmed the raw JSON literally contains
    `"Mouse:Right"`); `FindConflict` correctly found `UseAbility` when checking `Mouse Right` against
    another action. For the actual gameplay effect, hit the same `Input.Update()`-clobbers-simulated-
    mouse-state issue noted for `Controls.Button` in entry 89 — `Input.Update()` itself calls
    `Mouse.GetState()` and overwrites whatever the test preset, using the just-set "current" value as
    the new "previous" one — so instead of calling `Input.Update()`, the test evaluated the exact
    same `(currentState is RealmState || NexusState) && WasBindingPressed(...)` conditional
    `Input.cs` contains, directly against simulated mouse state: confirmed a left-click no longer
    triggers the ability once it's rebound to right-click, and a right-click does (real `Mana` drop,
    150→130, from the account's actual equipped Wizard gear). Finally confirmed the full real UI path
    end to end — constructing a real `SettingsState`, clicking the "Use Ability" row, then completing
    the rebind with an actual simulated right-click (this path doesn't suffer the `Update()`-clobber
    issue, since `SettingsState.Update()` reads `Input.mouse` directly) — correctly resulted in
    `UseAbility = Mouse Right` and exited listening mode. Clean build and a real boot-check against
    this account's actual save files both passed. *(14:15 EDT)*
92. **Harder enemies now drop higher-tier loot more often**, picked off the backlog. Resolved via
    `AskUserQuestion`: both drop chance and max reachable tier scale with difficulty (recommended
    option), and difficulty is bucketed off the existing `Enemy.PointValue` (already ranks toughness
    — higher score for killing it — so no new field needed, resolving the backlog entry's open
    "new field vs. bucket PointValue" question). Three buckets tuned to sit between the game's real
    `PointValue`s (Snake 2, Seeker 7, Wanderer 15, SpriteGod 200, Limon 2000): `< 10` (Snake/Seeker,
    trash) → drop chance 1-in-20 (worse than the old flat 1-in-15), max tier jump 1 (same as before);
    `< 100` (Wanderer) → 1-in-15 (today's original baseline), jump 1; `< 1000` (SpriteGod) → 1-in-8,
    jump 2; `1000+` (bosses) → 1-in-8, jump 3 — though bosses go through `SpawnGuaranteedLoot()`
    instead, where "chance" is already 100% and only the jump number matters. `ItemSpawner.Spawn()`
    and `SpawnGuaranteedLoot()` both gained an `int pointValue = 0` parameter (default keeps every
    other/future caller compiling unchanged); each category's tier lookup changed from a hardcoded
    `+1` to `Tier + RollTierOffset(maxTierJump)` — a fresh `rand.Next(1, maxTierJump+1)` roll per
    category per drop, so a tough kill doesn't guarantee hitting the maximum every time. `Enemy.cs`'s
    `SpawnLoot()`/`Boss.cs`'s override now pass `this.PointValue` through. One correctness fix caught
    before shipping: `SpawnGuaranteedLoot()`'s whole documented promise is "every category with a
    reachable tier always contributes" — but a single random-offset roll landing past the catalog's
    top tier would now silently come back empty far more often than the old fixed `+1` ever did.
    Fixed with a new `ItemsAtBestAvailableTier<T>()` helper that steps the offset down from the
    rolled value to 1 until it finds a tier with real catalog entries, restoring the original
    guarantee while keeping the randomized ceiling. Verified via a scripted repro (no `Save*()` calls
    — `Spawn()`/`SpawnGuaranteedLoot()` only touch `EntityManager`/`ItemSpawner.LootBags` in memory,
    no real-save-file risk): confirmed all 3 bucket boundaries via reflection at both real and edge
    `PointValue`s (9/10, 99/100, 999/1000); temporarily lowered every equipped tier to 1 first (this
    account's real gear sits near the catalog's actual max tier, which would've left no headroom for
    a +2/+3 roll to ever land and masked the whole test — restored after) then ran 400 identical
    `Spawn()` calls each for a weak (PV 2) and strong (PV 200) enemy: strong dropped loot bags
    noticeably more often (172/400 vs. 103/400) and 31 of those drops reached a Tier 3+ weapon (a
    genuine +2 jump from Tier 1, proving the randomized ceiling actually varies, not just always +1);
    confirmed `SpawnGuaranteedLoot()`'s fallback held — 20/20 runs at low tier still included a
    weapon. Clean build and a real boot-check against this account's actual save files both passed.
    *(14:29 EDT)*
93. **Follow-up to entry 92: weak enemies (Snake/Seeker, the `PointValue < 10` bucket) now always
    drop from a fixed low absolute tier range (0-2) instead of scaling relative to the player's own
    equipped gear.** The user's specific complaint: entry 92 still computed every enemy's drop tier
    as `Player.Tier + roll`, so a Snake killed by a heavily-geared player was handing out
    relatively-scaled-up loot just because the player's own tier was high — a weak enemy should
    always drop the same weak loot regardless of who kills it. New `IsWeakEnemy(pointValue)` (just
    `pointValue < 10`, the existing weak-bucket boundary from entry 92) and `ResolveDropTier(
    pointValue, playerTier, maxTierJump)` — the single place every category's tier computation now
    goes through, branching to a flat `rand.Next(WeakEnemyMinTier, WeakEnemyMaxTier + 1)` (0-2) for
    weak enemies, or the existing `playerTier + RollTierOffset(maxTierJump)` for everything else,
    unchanged. All 4 categories in `Spawn()` (weapon/armor/ring/ability item) switched from their
    inline `Player.Tier + RollTierOffset(...)` to this shared helper. `SpawnGuaranteedLoot()`
    (bosses only) untouched — it never sees a weak `pointValue` in practice. Verified via a scripted
    repro (no `Save*()` calls, no real-save risk): temporarily set the real account's actual equipped
    `Weapon.Tier` to a deliberately high 14 (restored after), then ran 500 `Spawn()` calls for a
    Snake (`PointValue` 2) — got 25 weapon drops, every single one landing in Tier 0-2 (highest seen
    was exactly 2, zero out-of-range), confirming the player's high gear tier was completely ignored
    for the weak bucket as intended; a matching 500-run check for a Wanderer (`PointValue` 15, not
    weak) still correctly tried to scale relative to the same Tier-14 player (rolling for Tier 15,
    which doesn't exist in the catalog — 0 drops, the same catalog-limited outcome entry 92's
    existing behavior would already produce, confirming no regression to the non-weak path). Clean
    build and a real boot-check against this account's actual save files both passed. *(14:57 EDT)*
94. **The last two enemy spawn mechanic directions from the backlog, built together: level-gated
    enemy types and wave/pack spawning.** Resolved via `AskUserQuestion` — the user picked "both"
    over either alone. Both layer on top of entry (the distance-based density mechanic) rather than
    replacing it. New `EnemySpawner.BasicEnemyPool` — a `(requiredLevel, factory)` tuple array
    ordered by the 3 basic types' existing `PointValue` toughness (Snake 2 → Level 1, always
    available so there's never a dead stretch with nothing to fight; Seeker 7 → Level 3; Wanderer
    15 → Level 6) — replaces the old "all three roll independently every frame from Level 1."
    SpriteGod's own separate, already-level-scaling spawn roll is untouched — a distinct "occasional
    special threat," not folded into the regular wave pattern. For wave/pack spawning,
    `EnemySpawner.Update()`'s three independent per-frame `1-in-N` rolls became a single
    `waveCooldownRemaining` timer: every `effectiveInverseSpawnChance` frames (the same value the
    old rolls used as their probability, reused here as a hard interval instead — over N frames the
    old system's 3 independent `1/N` rolls averaged out to about 3 spawns, which is what a
    2-4-enemy wave once every N frames also averages to, so the overall spawn *rate* stays roughly
    the same while the *pattern* changes from a steady trickle to bursts), `SpawnWave()` picks one
    shared anchor position and spawns 2-4 enemies clustered around it (±80 units) drawn at random
    from whichever pool entries the player's current level has unlocked. `Reset()` also resets the
    new cooldown alongside the existing `inverseSpawnChance`. Verified via a scripted repro (no
    `Save*()` calls, no real-save risk — direct `EnemySpawner.Update()`/`Reset()` calls, `Player.
    Instance.Level` temporarily overridden and restored): at Level 1, 2000 frames only ever produced
    Snake (`PointValue` 2) or SpriteGod (200) — never Seeker/Wanderer; at Level 3, Seeker appeared
    but Wanderer (`PointValue` 15) never did; at Level 6, Wanderer finally appeared — confirming the
    gate thresholds fire exactly where set. Confirmed a single wave produced 2-4 enemies (4 in the
    actual run) clustered within ~147 units of each other (well inside the old system's 250-1000
    unit spawn ring, reading as a genuine tight pack); confirmed the frame immediately after a wave
    triggers produces zero additional spawns (a hard interval, not still-independent per-frame
    rolls). One test-harness-only hiccup along the way, not a real bug: the first run crashed with
    a `NullReferenceException` in `Player.Update()` — forgot the established "`Game1.Camera` must be
    initialized via a throwaway `NexusState` before calling `EntityManager.Update()`" setup step;
    fixed by adding it, not a code issue. Clean build and a real boot-check against this account's
    actual save files both passed. *(15:59 EDT)*
95. **Two new enemies — Slime (low-level) and Brute (mid-level)** — picked off the backlog's "more
    enemy variety" item. No spare enemy art exists in the project (confirmed via a repo-wide check —
    only `enemy.png`/`enemy2.png`/`snake.png`/`sprite_god.png` are wired into `Art.cs`), so rather
    than block on new art, both reuse an existing texture with a permanent color tint as the
    distinguishing visual signal — real art can replace the tint later if the user wants. New
    `Enemy.tint` field (defaults `Color.White` — no change for every existing enemy) feeds into the
    same fade-in-alpha multiply the spawn animation already does (`color = tint * (1 -
    timeUntilStart/60f)`, was hardcoded `Color.White * ...`), so a tinted variant still fades in
    exactly like every other enemy, just arriving at its own permanent color instead of white.
    `Enemy.CreateSlime()`: reuses `Art.Snake`, tinted light green, health 20 (between Snake's 5 and
    Seeker's 50), `PointValue` 4 (between Snake's 2 and Seeker's 7, keeping it in the "weak" loot
    bucket from entry 92), behavior `MoveRandomly` + `Spray(2, 3, damage: 8)` — a combo no existing
    enemy used (previously `MoveRandomly` only paired with `Bomb` on Wanderer, `Spray` only with
    `FollowPlayer` on Seeker or bosses), reading as a slow wandering blob instead of Snake's tight
    weaving dash. `Enemy.CreateBrute()`: reuses `Art.Enemy` (Wanderer's sprite), tinted orange-red,
    health 300 (double Wanderer's 150), `PointValue` 120 (comfortably in the "strong" loot bucket,
    same tier as SpriteGod — a genuine mid-to-upper threat, not just a palette-swapped Wanderer),
    behavior `FollowPlayer(0.35f)` + `Bomb(4)` — also a new combo (`FollowPlayer` previously only
    paired with `Spray` on Seeker, `Bomb` previously only with `MoveRandomly`/`MoveSnake`), reading
    as "rushes the player down, then bursts" instead of Wanderer's wander-and-lob. Both wired into
    `EnemySpawner.BasicEnemyPool` (see entry 94) at Level 2 (Slime, right after Snake) and Level 8
    (Brute, above Wanderer's Level 6) — automatically inherit that entry's level-gating and
    wave/pack spawning, and entry 92's difficulty-scaled loot, purely from their `PointValue` with
    no extra plumbing needed anywhere. Verified via a scripted repro (no `Save*()` calls, no real-
    save risk): confirmed both construct as genuine `Enemy` (not `Boss`) with the exact intended
    health/`PointValue`; read the `tint` field via reflection and confirmed it matched the intended
    color exactly; ran `Update()` past the 60-frame fade-in window and confirmed the actual displayed
    `color` field matched the tint exactly (not left at white); iterated the real `BasicEnemyPool`
    array and confirmed both new entries sit at their intended level/PointValue alongside the
    existing three; confirmed `ItemSpawner`'s existing difficulty-bucket functions correctly placed
    Slime (PV 4) in the weak bucket and Brute (PV 120) in the strong bucket with no changes needed to
    that system. Clean build and a real boot-check against this account's actual save files both
    passed. *(16:15 EDT)*
96. **A third new low-level enemy, BigSnake — the first to use real user-supplied art** rather than
    a tinted reskin (unlike entry 95's Slime/Brute). The user supplied `Content/Enemies/big_snake.png`
    directly. Wired in following the exact same pattern as every other enemy texture: a new
    `#begin`/`#build` block in `Content/Content.mgcb` (copied from the adjacent
    `Enemies/sprite_god.png` block — same importer/processor/params, since that's the established
    shape for everything under `Content/Enemies/`) and a matching `Art.BigSnake` declare/load pair in
    `Art.cs`. `Enemy.CreateBigSnake()` reuses `MoveSnake()` — the same weaving movement as the base
    Snake — so it reads as the same family (the user's own framing: "spawns in the same group as the
    other snakes"), with more health (15 vs. Snake's 5) and a faster `Shoot(3)` vs. Snake's `Shoot(2)`
    so it feels like a real step up rather than a plain reskin; `PointValue` 6 keeps it in the "weak"
    loot bucket alongside Snake (entry 92) and just under Seeker's 7. Wired into
    `EnemySpawner.BasicEnemyPool` (entry 94) at Level 1 — the same requirement as Snake itself, so it
    can appear in the very same wave burst as Snake from the start, literally the same spawn group the
    user asked for (not just a nearby level). Verified via a scripted repro (no `Save*()` calls, no
    real-save risk): confirmed `Art.BigSnake` actually loaded as a real, non-null 78×78 texture (not
    silently falling back to something else); confirmed `CreateBigSnake()` constructs a genuine
    `Enemy` (not `Boss`) with the intended health/`PointValue`, and — since it uses real art instead
    of the tint mechanism entries 95's Slime/Brute rely on — confirmed its `tint` field correctly
    stayed at the default `White` (no unintended tinting applied on top of real art); iterated the
    real `BasicEnemyPool` array and confirmed Snake and BigSnake share the exact same `requiredLevel`
    (both 1). Clean build (which also exercises the new `Content.mgcb` block — a spaced-filename-free
    path this time, so no risk of the spaced-path build quirk from `Boss.cs`'s Limon entry) and a real
    boot-check against this account's actual save files both passed. *(16:37 EDT)*
97. **BigSnake turned into a rare mini-boss spawn instead of a regular `BasicEnemyPool` member.**
    Follow-up to entry 96, per the user's explicit ask: fewer BigSnakes, still available from Level
    1, framed as a mini-boss that arrives escorted by a cluster of ordinary Snakes. The user
    separately buffed `CreateBigSnake()`'s own stats directly (health 15→500, `PointValue` 6→250,
    added `Defense = 10`) to actually read as a mini-boss rather than just a slightly-tougher-snake —
    left untouched, this entry only changes how/how-often it spawns. Removed `(1,
    Enemy.CreateBigSnake)` from `BasicEnemyPool` (where it had been exactly as common as every other
    pool member, picked uniformly per wave slot) and gave it its own separate mechanism: a new
    `bigSnakePackCooldownRemaining` timer on a fixed `BigSnakePackInterval` (1800 frames, ~30
    seconds) — deterministic rather than probability-based, so once the timer elapses a pack always
    spawns (reads as a real periodic encounter, not a lucky roll) — `SpawnBigSnakePack()` places one
    BigSnake plus `BigSnakePackSnakeCount` (4) ordinary Snakes clustered around one shared anchor,
    the same clustering technique `SpawnWave()` already uses. Unconditional on level (no gate at
    all), matching "I still want them to spawn at level one." `Reset()` also resets the new cooldown
    alongside the existing ones. Verified via a scripted repro (no `Save*()` calls, no real-save
    risk; periodically cleared the much-more-frequent regular-wave spawns during the ~1800-frame wait
    so that unrelated system didn't hit the 1500-entity cap first): confirmed `BasicEnemyPool` is
    back down to 5 entries with no BigSnake in it; confirmed zero BigSnakes exist right up until the
    interval elapses, then exactly one BigSnake plus exactly 4 additional Snakes appear the instant it
    does; confirmed the BigSnake and its 4 nearest Snakes sit within ~87 units of each other — a
    genuine tight cluster, not scattered independently. One test-authoring bug caught and fixed along
    the way (not a code bug): an off-by-one in the wait loop left the cooldown at 1 instead of 0
    right before the "trigger" call, so the first run showed zero spawns — the actual `EnemySpawner`
    logic already checks the cooldown at the *start* of `Update()` (before decrementing), so it needs
    exactly `BigSnakePackInterval` prior decrements, not `BigSnakePackInterval - 1`. Separately, the
    very first test run also failed on the user's own already-buffed `PointValue` (250, not this
    session's original 6) — fixed by updating the test's reference value, not the code. Clean build
    and a real boot-check against this account's actual save files both passed. *(16:47 EDT)*
76. **Minimap — a small local-area map in the sidebar's top-right corner**, showing the player,
    nearby portals, and nearby enemies. Resolved via `AskUserQuestion`: shows player + portals +
    enemies (not just player alone), placed in the sidebar rather than a separate gameplay-area
    panel. `NexusState` and `RealmState` are sibling `State` subclasses (not parent/child) that
    already both call the one shared `Overlay.DrawSidebar()`, so the minimap lives there as a new
    `Overlay.DrawMinimap()` rather than needing a hook duplicated across both — same reasoning as
    the doc comment already on `DrawSidebar()` ("consolidated here so RealmState and NexusState
    can't drift out of sync"). Shows a fixed 2000-unit radius around the player rather than the
    whole world/instance — the open Realm is 500,000px per side, so a whole-world view would
    collapse every blip onto a single pixel; a local window reads as actually useful and works the
    same way for the much smaller 2000×2000 boss arena too. Blips outside the radius clamp to the
    map's edge per-axis (a square clamp, not a true radial one — simpler math, still points roughly
    the right direction) rather than being culled entirely, so a just-out-of-range threat or portal
    still shows *something*. Needed two small new public accessors, both read-only views onto
    previously-private state rather than exposing the underlying collections directly:
    `EntityManager.EnemyPositions` (positions only, not the `Enemy` objects — all the minimap
    needs) and `Portal.Position` (the `Position` field itself was already private with no getter at
    all). Also needed a new `Portal.NexusPortals` static field, since `NexusState`'s own fixed
    portal set (Realm/CharacterSelect/Bank/BossRealm-test-shortcut) previously lived only in a
    private local field with no way for `Overlay` to read it — set by `NexusState`'s constructor,
    cleared by `RealmState`'s constructor (alongside its existing `Portal.Reset()`) so the Nexus's
    portals correctly stop showing the moment the player leaves it; `Portal.DroppedPortals` (already
    public) covers the dungeon/boss-arena side, where portals are dynamic instead of fixed. Verified
    via a scripted repro that avoided constructing a real `NexusState`/`RealmState` (which would
    trigger real save-file writes) by manipulating the underlying static state directly instead:
    placed the player at a known origin, added enemies at controlled world offsets — one in-range
    up, one in-range right, one deliberately 2.5× past the radius to prove the edge-clamp — plus a
    dropped portal down-left, then rendered `Overlay.DrawSidebar()` to a saved screenshot at 6×
    zoom on just the minimap corner and confirmed every blip's on-screen pixel position matched the
    expected `offset/radius * (mapSize/2 - dotSize/2)` math exactly, including the clamped one
    sitting flush against the map's right edge instead of off past it; separately verified the
    Nexus scenario (fixed `NexusPortals` set, no enemies) showed the right two portal blips and no
    red dots. *(14:03 EDT)*
77. **Minimap enlarged from 100px to 130px**, per the user's request. `Overlay.cs`'s `MinimapSize`
    constant changed (the box automatically stays anchored to the sidebar's top-right corner, so no
    other position math needed updating); blip sizes bumped slightly to match the larger box (player
    7→8, portal/enemy 6/5→7/6). Verified by rendering the real `Overlay.DrawSidebar()` to a saved
    screenshot and confirming the larger map still has clear separation from the stat text block to
    its left, not overlapping it. *(14:28 EDT)*
79. **Dragging an item between the inventory and bank onto an already-occupied slot now swaps the
    two items, instead of bouncing off (full) or silently landing somewhere else (not full).**
    Previously `InventorySystem.cs`'s inventory→bank release handler always called
    `BankSystem.AddRecordAt(targetSlot, draggedRecord)`, whose existing fallback semantics (needed
    for other callers) silently redirect to the first empty slot whenever the target slot is
    occupied — so dropping an inventory item directly onto a filled bank slot either did nothing
    visible (deposited into some other slot instead) or errored if the bank happened to be
    completely full, even though the specific slot under the cursor had a perfectly good item to
    swap with. This was the one drag interaction in the whole inventory/bank system that didn't
    already support occupied-slot swapping — same-panel drags (inventory→inventory,
    bank→bank) already swap on an occupied target slot, from earlier session work. Fixed by
    reading the target slot's occupant directly before deciding what to do: if occupied, swap the
    two records in place (no capacity check needed, since nothing is actually being added — the
    slot count stays the same); only the previous behavior (needs an empty slot somewhere, or
    errors) applies when the target slot is genuinely empty. Applied to both directions —
    `InventorySystem.cs` (inventory→bank, what was asked) and `BankSystem.cs` (bank→inventory,
    which had the identical one-way-deposit gap) — for consistency with the same-panel swap
    behavior both panels already have. Verified via a scripted repro simulating a full press-drag-
    release across two frames each direction (writing directly to `Input.mouse`/`previousMouse`,
    calling `InventorySystem.Update()`/`BankSystem.Update()` directly rather than routing through a
    full state `Update()`, matching this session's established pattern): put "Item A" in inventory
    slot 0 and "Item B" in bank slot 0, dragged inventory→bank and confirmed they swapped
    (`Inventory[0]=Item B, Bank[0]=Item A`), then dragged bank→inventory and confirmed they swapped
    back to the original layout. Hit one test-harness-only snag along the way, not a real bug: the
    test's items initially used `Art.HealthBar` as their icon (the usual pattern for rendering-only
    tests, e.g. entry 36's loot bag fix) — but `Art.HealthBar` is actually a 1×1 pixel texture used
    elsewhere only stretched into rects, and slot click-detection bounds are sized from the item's
    *raw* texture dimensions, so a 1×1 icon collapses the clickable area to a single pixel a
    simulated click near the slot's center or top-left corner will almost always miss. Switched the
    test items to real icon-sized textures (`Art.HealthPotion`/`ManaPotion`) once diagnostic logging
    of the drag state machine's internal fields (`dragItem`/`hover` staying `false` after a
    simulated press) pointed at a hover-detection miss rather than the swap logic itself — worth
    remembering for any future test that needs to simulate a *click*, not just a render, on an
    item slot. *(15:29 EDT)*
78. **Equipment row reordered to Weapon > AbilityItem > Armor > Ring, left to right** (was Weapon,
    Armor, Ring, AbilityItem). Changed the `x` position constant in each of `Weapon.cs` (unchanged,
    still first), `AbilityItem.cs` (`+120` → `+40`, now second), `Armor.cs` (`+40` → `+80`, now
    third), and `Ring.cs` (`+80` → `+120`, now last) — each slot's `SlotBounds`/hover/drag-drop
    hit-testing already derives from this same `x`, so nothing else needed touching. Side effect
    worth noting: `Overlay.DrawEquipment()`'s draw-call order (`Weapon, AbilityItem, Armor, Ring` —
    already in that order from [BUGFIXES.md](BUGFIXES.md) entry 41's tooltip-overlap fix, coincidentally)
    now also matches the slots' actual left-to-right screen order for the first time; the two-pass
    tooltip-then-icon split from that fix stays in place regardless, since a future reorder of
    either sequence could otherwise silently reintroduce the same bug. Verified visually: rendered
    the real `Overlay.DrawSidebar()` for a Knight (whose four equipped pieces — Sword, Shield, Heavy
    armor, Ring — are visually distinct at a glance) and confirmed the icons read left to right as
    Weapon, AbilityItem, Armor, Ring in the saved screenshot. *(14:33 EDT)*
73. **The ability readiness bar now shows a flat grey bar with no text when no ability item is
    equipped**, instead of a misleading "Ready"/mana-countdown state that doesn't actually apply —
    the ability key does nothing with an empty slot (see [BUGFIXES.md](BUGFIXES.md) entry 39's new
    `AbilityItem.IsEquipped` guard in `UseAbility()`), so the HUD shouldn't imply otherwise.
    `Overlay.DrawAbilitySection()` gained an early check: if
    `!Player.Instance.AbilityItem.IsEquipped`, draws a single flat `Color.Gray * 0.5f` rectangle at
    the bar's normal position/size and returns before the "Ability: Ready.../ X / Y" text or the
    usual black-background-plus-cyan-fill two-layer bar — same idea, same short-circuit shape, as
    entry 71's equip-slot placeholders. Verified via a scripted repro: rendered
    `Overlay.DrawSidebar()` to a `RenderTarget2D` and sampled the exact bar-fill pixel in both
    states — with no ability item equipped it read back `(64,64,64)` (uniform grey, confirmed
    `R==G==B`); with a real item equipped and full mana it read back `(0,139,139)` (the normal dark-
    cyan fill) at that same pixel — proving the two states are visually distinct and the grey state
    actually renders, not just compiles. *(12:20 EDT)*
71. **Empty equip slots (Weapon/Armor/Ring/AbilityItem) now show a greyed-out Tier 0 placeholder
    icon instead of just a bare border**, so it's clear at a glance what type of item goes in each
    slot before ever equipping anything. All four `DrawEquipped()` methods (`Weapon.cs`/`Armor.cs`/
    `Ring.cs`/`AbilityItem.cs`) shared the identical shape — draw the border, then `if (!IsEquipped)
    return;` before ever drawing an icon — so an empty slot was just an outline with nothing inside.
    Each gained a private static `PlaceholderImage` property resolving that slot's Tier 0 item
    *for the player's current class* (not a fixed image, since e.g. the Weapon slot means something
    different for each class): `Weapon`/`Armor` filter their own catalog (`Game1.Instance.Weapons`/
    `Armors`) by `Type == Player.Instance.WeaponType`/`ArmorType` and `Tier == 0`; `Ring` has no
    class restriction, so it's just `Tier == 0` off `Game1.Instance.Rings`; `AbilityItem` has no
    shared "Type" enum to filter by at all (Spell/Quiver/Shield are separate C# subclasses, not one
    class with an enum), so it instead concatenates all three catalogs and filters by
    `Player.CanEquipAbilityItem(item)` — the same per-class match already used everywhere else an
    ability item's class-fit matters — alongside `Tier == 0`. No new asset loading needed: every
    catalog list is already fully texture-loaded at boot (`Util.LoadWeaponData()` etc. call
    `Content.Load<Texture2D>` per entry up front), so this just reads an existing `.image` off
    whichever Tier 0 entry matches. `DrawEquipped()`'s empty-slot branch now draws that placeholder
    with `Color.Gray * 0.5f` (greyed out, half-opacity) instead of returning immediately, still
    returning right after so no tooltip/hover logic runs for a slot with nothing really equipped.
    Verified via a scripted repro: for all three classes, unequipped all four slots (`EquipWeapon(new
    Weapon())` etc. — the same "assign a blank placeholder object" pattern already used elsewhere to
    unequip), read each slot's `PlaceholderImage` via reflection (`private static`, not part of any
    public API) and confirmed it exactly matched that class's real catalog-loaded Tier 0
    Weapon/Armor/Ring image reference (not just non-null — the actual same `Texture2D` instance), and
    confirmed the AbilityItem placeholder resolved to *something* non-null (Spell for Wizard, Quiver
    for Archer, Shield for Knight); then called all four `DrawEquipped()` methods through a real
    `SpriteBatch.Begin()/End()` pass for each class with no exception, confirming the new
    `spriteBatch.Draw()` call for the placeholder actually executes, not just compiles.
    *(11:30 EDT)*

## 2026-08-19

98. **Project memory notes moved into the git repo**, picked off the backlog's "sync the memory
    notes across devices" tooling item. The three running logs (this file, [BACKLOG.md](BACKLOG.md),
    [BUGFIXES.md](BUGFIXES.md)) and the two hard behavioral rules (save-file-backup-before-testing,
    launch-minimized-for-boot-checks) previously lived only under this machine's local Claude Code
    memory folder — not portable to another machine automatically. Chose option (1) from the
    backlog's two discussed options: mirrored all three logs into `docs/` at the repo root (frontmatter
    stripped, internal `[[wiki-links]]` rewritten to plain relative markdown links between the three
    files), and inlined the two behavioral rules plus the established testing workflow into a new
    root `CLAUDE.md` — auto-loaded every session in this project, so it now travels with `git clone`/
    `pull` on any machine instead of staying local to one. The old local memory files were deleted
    once the repo copies were confirmed pushed, replaced with a single small reference memory
    pointing at the new repo locations. Flagged to the user before pushing that the repo is public,
    so these dev-process logs (including the documented data-loss incident in
    [BUGFIXES.md](BUGFIXES.md) entry 42) become publicly visible — confirmed as fine, nothing
    sensitive in them.

## 2026-08-20

99. **`Portal.Destination` reworked from a fixed enum into a small class hierarchy**, picked off
    the backlog specifically to unblock adding a second boss soon — the old enum's `BossRealm`
    value routed unconditionally to a hardcoded `LimonTheSpriteGoddess`, with no way for a portal
    to say *which* boss it leads to. [Portal.cs](Portal.cs)'s `Destination` is now an
    `abstract class` nested exactly where the enum used to live, with `Realm`/`CharacterSelect`/
    `Bank`/`Nexus` as private singleton subclasses and a new public `BossDestination` subclass that
    carries a `Func<Vector2, Boss>` factory plus a display name — same "shared base + concrete
    variant" shape already proven by `Boss`/`LimonTheSpriteGoddess` and `CharacterClasses`. The two
    switch statements (`DisplayName`, `EnterPortal()`) collapsed to one-line delegation
    (`dest.DisplayName`, `dest.Enter()`); every existing `Portal.Destination.X` reference
    (`Enemy.cs`'s SpriteGod portal drop, `NexusState.cs`'s `// TEMP` test portal, `Portal.cs`'s own
    `dest == Destination.Bank` proximity check) kept compiling unchanged, since each static field
    kept its old name and no `Equals`/`==` override was added anywhere in the hierarchy — plain
    reference equality against the same singletons, same as an enum comparison. `StateManager.
    EnterBossRealm()` and `BossRealmState`'s constructor both gained a
    `Portal.Destination.BossDestination` parameter, replacing the hardcoded `new
    LimonTheSpriteGoddess(...)` call with `bossDestination.CreateBoss(...)` — adding a second boss
    later is one new `static readonly BossDestination` field plus a drop site for it, no
    `Portal.cs` switch involved. One accessibility wrinkle: `Boss` (`Boss.cs`) is internal, so
    `BossDestination`'s boss-carrying members (`BossName`, `CreateBoss`, its constructor) had to be
    `internal` too — `Func<Vector2, Boss>`'s effective accessibility is capped by its internal type
    argument — while `BossDestination` itself stays `public` so it's still nameable as a parameter
    type from `StateManager`/`BossRealmState`. Verified via a scripted repro (following CLAUDE.md's
    save-backup-first rule, since constructing a real `BossRealmState` saves unconditionally):
    constructed `BossRealmState` through the new parameterized path, confirmed
    `EntityManager.ActiveBoss` was a real `LimonTheSpriteGoddess` with the right `Name`;
    constructed one throwaway `Portal` per destination and confirmed all 5 `DisplayName` strings
    were byte-identical to the old switch's output; confirmed the Bank destination's reference
    equality still held. Real save files were re-saved by the test as expected (fresh item GUIDs,
    reset UI bounds — cosmetic, no lost items/stats) and restored from the pre-test backup
    afterward, diff-verified byte-identical. Clean build and a plain boot-check (no temp code)
    both passed.

100. **Second boss: Stheno the Snake Queen** (9,000 HP, 19 DEF, 3,000 EXP), reached via a portal
     BigSnake now drops on death (mirroring SpriteGod → Limon) plus a matching `// TEMP` Nexus test
     portal. Stationary, unlike Limon — cycles through 3 mutually-exclusive, *time-based* phases
     (Blades, Bursts, Spiral; ~15s each) rather than Limon's health-threshold, additive escalation,
     briefly `Invulnerable` during every transition while summoning 3 "Stheno Swarm" adds. New
     `Bosses/SthenoTheSnakeQueen.cs`/`SthenoPet.cs`/`SthenoSwarm.cs`. Phase 1 fires 4 rotating
     directions of paired blades (one always aimed at the player) plus scattered AoE grenades;
     Phase 2 fires alternating diamond/square grenade bursts (reusing
     `LimonTheSpriteGoddess.SpawnSquareWall()`'s corner-offset technique for static placement
     instead of a sweep, gaps left deliberately open to dodge through); Phase 3 fires a
     6-direction rotating spiral of purple orbs plus grenades aimed at the player. All 5 of her own
     attack coroutines gate on `currentPhase == X && !Invulnerable && PlayerInCenter` — "if her
     target backs out of the center of the room, she stops firing" — with the cooldown timer itself
     frozen while gated shut, so re-entering center never unloads a banked volley; her adds aren't
     gated this way. `SthenoPet` orbits her (position re-derived each frame, same technique as
     Limon's sweeping-shot tracking) trailing stationary green orbs that apply a brand-new `Slow`
     debuff; `SthenoTheSnakeQueen.MaintainPets()` tops the live count back up to 6 every frame,
     covering both "spawn several on entry" and "respawn immediately on death" with one mechanism.
     Deliberately set `PointValue = 1` on pets (not a normal small-enemy value) since they respawn
     immediately and uncapped for the whole fight — a normal value would've turned the room into a
     free XP/loot farm. `SthenoSwarm` charges the player in a straight line (direction captured once
     at spawn, never re-aimed) firing forward, then — if a chasing slot is free — switches to
     chasing with aimed shots; enforces "only 3 chasing Swarms at once" via a new
     `EntityManager.CountWhere<T>(predicate)` helper (generalizes the `OfType<T>()` idiom
     `ActiveBoss` already used internally), with a swarm that finishes charging while the cap is
     full just fizzling instead of queuing. New `Slow` debuff: `Entity.DebuffType` gained a case,
     `EnemyProjectile` gained `SlowsOnHit` (third parallel bool, mirroring `Projectile`'s existing
     `ParalyzesOnHit`/`StunsOnHit` rather than generalizing — matches that established precedent
     exactly), `Player` gained a `Slow()` convenience method and a movement-speed multiplier hook.
     `Enemy.isSpriteGod` (a one-off bool) generalized into `protected Portal.Destination
     portalDropOnDeath` now that a second real instance of "enemy X drops boss Y's portal" exists —
     same "replace repeated bool special-casing once a second instance shows up" cleanup already
     applied to `isBoss`/`Portal.Destination` earlier this session. `Boss.cs` gained a `Description`
     property (not rendered anywhere yet, same status as `BossDestination.BossName`) to carry the
     user-supplied lore text. Art for Stheno's portrait/Pet/Swarm sprites and the new `slowed.png`
     debuff icon were all user-supplied and already on disk, just needed `Content.mgcb`/`Art.cs`
     wiring (discovered mid-implementation that the file's `.mgcb` directives use `/` not `\` —
     wrote 4 new blocks with backslashes first by mis-copying a tool's display rendering, breaking
     `dotnet build` with an opaque "Too many arguments" MGCB parser error; fixed by switching to
     forward slashes, matching the other 193 existing entries). Verified via a scripted repro
     (real-save backup taken first, per CLAUDE.md): stats/Description, `WasShot()` true no-op while
     `Invulnerable`, phase cycling with `Invulnerable` toggling correctly across two full
     transitions, pet count reaching and holding target (including after manually expiring some to
     confirm same-tick respawn), swarm cap enforcement (3 chasing, a 4th self-expiring), BigSnake's
     new portal drop referencing the right destination, and the full
     `Portal.Destination.SthenoBossRealm`/`BossRealmState` path spawning a real
     `SthenoTheSnakeQueen`. Real save files were re-saved by the full-path test as expected and
     restored from backup afterward, diff-verified byte-identical. Clean build and a plain
     boot-check both passed. Tunable numbers throughout (phase/cooldown durations, grenade
     radii/damage, orbit speed, spiral rotation rate, center-check radius) are first-pass estimates,
     not final — flagged to the user that spiral "feel," grenade dodge-gap spacing, and pack timing
     all need an actual playtest pass to confirm.

101. **New enemy movement type, `Enemy.MoveTethered()`** — same weaving randomness as `MoveSnake()`,
     but leashed to a radius around wherever the enemy spawned (`wanderDistance`), with optional
     `speed` and a per-frame `updateChance` (probability of picking a new direction each frame,
     unlike `MoveSnake()`'s fixed 10-frame cadence) — all three requested explicitly by the user.
     Caught a real bug during verification: the first version only checked the incoming step
     against the boundary, not the enemy's already-accumulating `Velocity` (this engine's movement
     carries momentum, decaying 0.8x/frame rather than resetting) — several frames of sustained
     outward drift could build up enough carried momentum to blow past the leash by 20+ units
     before a same-frame redirect caught up. Fixed by predicting against the full candidate
     velocity and zeroing it outright on a violation, re-verified under aggressive test parameters
     (speed 4f, updateChance 1f) holding the boundary to floating-point precision. Not wired to any
     enemy factory yet — available for the next one that wants bounded wandering.
102. **F4 debug/testing key**, evolving across a few quick follow-up requests in the same
     conversation: maxes `Player.Level` to 20 with a real `RecalculateStats()` call (so
     Attack/Defense/etc. actually reflect it, not just the raw field), equips the current class's
     highest-tier item for every slot (Weapon/Armor/Ring/AbilityItem — fresh instances built from
     the matching catalog entry's data fields, same shape as `Weapon.LoadWeapon()`/
     `Armor.LoadArmor()`/etc., via the existing `EquipWeapon()`/`EquipArmor()`/`EquipRing()`/
     `EquipAbilityItem()` methods rather than landing in the inventory), and tops off Health/Mana to
     their new maxes last — after gear is equipped, not right after `RecalculateStats()`, since
     equipping can itself raise `HealthMax`/`ManaMax` further (e.g. a higher-tier armor's
     `MaxHealthBonus`) and topping off first would leave Health/Mana stuck below the true final max.
     Also moved the existing level up/down (`+`/`-`) debug keys, plus F4 itself, out of the
     `RealmState`-only input block into the same `(RealmState || NexusState)` gate `UseAbility`
     already uses, so leveling can be tried out without needing to already be in a dungeon —
     health/mana potions stay `RealmState`-only. Verified via scripted repros at each step
     (including one run against the user's actual real save, which turned out to already be at
     Level 20 with Tier 14 gear from real play — confirmed via ID-swap checks that the method still
     genuinely replaced the equipped items rather than relying on a stat-delta that had nothing
     left to change).
103. **Only bosses drop loot now**, per the user's explicit request. `Enemy.SpawnLoot()`'s base
     implementation (previously `ItemSpawner.Spawn()`, the random-chance drop table every regular
     enemy routed through) is now a no-op; `Boss.SpawnLoot()`'s guaranteed-loot override is
     unchanged. `ItemSpawner.Spawn()` itself is left in the file, not deleted, since it's now the
     flagged starting point for a real per-enemy drop-pool system — see
     [BACKLOG.md](BACKLOG.md)'s new entry. Verified via a scripted repro: killed one instance of
     every regular enemy type (Snake, Slime, Brute, BigSnake, Seeker, Wanderer, SpriteGod) and
     confirmed `ItemSpawner.LootBags` stayed at 0, then killed a `LimonTheSpriteGoddess` and
     confirmed a bag was added.
104. **Follow-up correction to entry 103**: the user clarified "only bosses drop loot" was a
     misreading — they meant only boss *pets* shouldn't drop loot, not regular enemies broadly.
     `Enemy.SpawnLoot()`'s base is back to calling `ItemSpawner.Spawn()` (regular enemies drop loot
     again, same as before entry 103), and a new `protected bool DropsLoot = true;` field gates the
     `SpawnLoot()` call in `WasShot()` — true for every enemy by default, set `false` only on
     `SthenoPet`'s constructor for now (the one actual "boss pet" in the game). `SthenoSwarm` (a
     different kind of add, not called a "pet") was left at the default and still drops normally,
     matching the user's literal wording rather than guessing it should extend further.
     `ItemSpawner.Spawn()`'s "not currently called" comment from entry 103 was removed since it's
     live again. [BACKLOG.md](BACKLOG.md)'s per-enemy-drop-pool entry updated to describe
     `DropsLoot` as a first minimal stepping stone toward that fuller system, not the system itself.
     Verified via a scripted repro: killed 200 Snakes and confirmed `ItemSpawner.LootBags` > 0
     (chance-based, so a large sample rather than expecting every kill to drop); killed 200
     `SthenoPet`s and confirmed exactly 0 (an absolute guarantee via `DropsLoot`, not just low
     odds); killed a boss and confirmed guaranteed loot still fires.
105. **Real art for the Stheno Pet's trailing slow-orb projectile**, user-supplied
     (`Content/Projectiles/Stheno Pet.png`), wired into `Content.mgcb`/`Art.cs` as
     `Art.SthenoPetProjectile` (same import/processor params as every other entry) and swapped in
     for the placeholder `Art.GreenMagic` reuse `SthenoPet.TrailOrbs()` had been using. No other
     behavior change — same damage (0), same `SlowsOnHit`, same duration.
106. **Real art for the Stheno Swarm's own shots**, user-supplied
     (`Content/Projectiles/Stheno Swarm.png`), wired into `Content.mgcb`/`Art.cs` as
     `Art.SthenoSwarmProjectile` and swapped in for the placeholder `Art.SwordSlash` reuse in both
     `SthenoSwarm.ChargeFire()` and `ChaseFire()` — one dedicated look for both firing states,
     matching the Stheno Pet projectile's own single-asset treatment. No behavior change.
107. **Stheno's grenades reworked into a real telegraphed AoE**, replacing the fixed-size
     `Art.RedFire` sprite (which never matched the actual damage radius) with a new
     `GrenadeProjectile` (`GrenadeProjectile.cs`, project root, extends `EnemyProjectile`): spawns
     as a low-opacity grey circle sized to the real explosion radius but with no live hitbox
     (`Radius = 0`), then after `fuseFrames` (25, ~0.4s) "arms" — `Radius` jumps to the real value
     and the circle turns red — giving the player a brief window to see exactly where it'll hurt
     and step out before it does. The circle itself is a new procedurally-generated
     `Art.Circle` (64x64, hard-edged, opaque-inside/transparent-outside), generated once at startup
     the same way `Art.HealthBar` already is (a runtime `Texture2D` rather than loaded art) since a
     solid-color square can't be scaled into a circle — tinted grey/red and scaled to the exact
     world-space radius at draw time. `SthenoTheSnakeQueen.SpawnGrenade()` (shared by all three
     grenade attacks — rapid throw, diamond/square bursts, aimed bombs) now constructs a
     `GrenadeProjectile` instead of a plain `EnemyProjectile`.

     Caught a real collision-system bug while verifying: `EntityManager.IsColliding()`'s
     circle check uses `a.Radius + b.Radius` as the combined hit radius — a "Radius = 0" grenade is
     only truly inert if the *other* side (the player) also has zero radius, which it never does.
     A grenade spawned exactly on the player (the aimed-bomb attack does exactly this) would still
     register as touching them during the telegraph, dealing a hidden 0-damage hit and immediately
     expiring itself before it ever got to arm. Fixed generically in `IsColliding()` itself (not
     special-cased to grenades): either side having `Radius <= 0` now means no collision is
     possible at all, regardless of the other side's size — the semantically correct rule for a
     circular entity with no physical footprint, confirmed via grep that nothing else in the
     codebase relied on the old zero-radius-still-collides behavior. Verified via a scripted repro:
     `Art.Circle` generates correctly (64x64, opaque center, transparent corner); a grenade spawned
     directly on the player takes zero damage and doesn't self-consume for the full telegraph
     window; it arms (`Radius`/color flip) and deals its hit exactly once after the fuse; a grenade
     nobody stands in still expires via its normal duration timer. Real save files backed up first
     per `CLAUDE.md` (the test mutates `Player.Instance.Health` directly) and restored/diff-verified
     afterward. Clean build and a plain boot-check both passed.
108. **`EntityManager`'s `CollisionShape.Rectangle` hitbox now actually rotates with the sprite**,
     per the user's report that it didn't visually align (e.g. Stheno's blade projectiles).
     `RectangleBounds()` — the only thing the old check used — was always an axis-aligned bounding
     box that *encloses* the rotated sprite, not the sprite's true rotated silhouette; at a diagonal
     `Orientation` that box balloons visibly larger than what's actually drawn (its area grows up to
     ~41% for a square sprite, more for an elongated one like a blade). It also collapsed BOTH sides
     into boxes whenever either one opted into `Rectangle` — even the player, despite being a
     circle. New `EntityManager.IsRectangleCircleColliding()` does the real thing instead: transforms
     the circle's center into the rectangle's own local (unrotated) space by undoing its
     `Orientation`, finds the closest point on the axis-aligned box in that local space, and checks
     distance to it — the standard closest-point method for circle-vs-oriented-rectangle collision.
     `IsColliding()` now branches to it whenever exactly one side is `Rectangle` (every real case
     today — a projectile against the player); the old AABB-vs-AABB check is kept only as a
     same-as-before fallback for two `Rectangle` sides colliding with each other, a pairing nothing
     in the codebase currently exercises (not worth building full rotated-rectangle-vs-rotated-
     rectangle/SAT for zero live callers). The F3 debug hitbox outline
     (`DrawHitboxRotatedRectangle()`, replacing the old axis-aligned `DrawHitboxRectangle()`) now
     draws the sprite's actual 4 rotated corners too, so the debug view matches what really gets
     checked instead of the old looser box. Verified via a scripted repro: a 100x20 rect rotated 45°
     correctly rejects a circle sitting inside the old AABB but outside the true rotated silhouette
     (~46.57 units past the short edge), correctly accepts one 30 units along the true long axis, an
     unrotated sanity pair behaves as expected, and the same geometry produces matching results
     through the real `EntityManager.Update()`/`HandleCollisions()` pipeline with a live `Player`
     and `EnemyProjectile` (not just the isolated math). Clean build and a plain boot-check passed.
109. **Real art for Stheno's blade projectile**, user-supplied (`Content/Projectiles/Stheno
     Blade.png`), wired into `Content.mgcb`/`Art.cs` as `Art.SthenoBladeProjectile` and swapped in
     for the placeholder `Art.SwordSlash` reuse in `FireBlade()`. Same day this asset also lines up
     with entry 108's rotated-rectangle fix — the blade's `Shape = CollisionShape.Rectangle` hitbox
     now both looks right and actually matches its rotation. No behavior change beyond the sprite.
110. **`GrenadeProjectile`'s telegraph is now 3 ramping opacity stages instead of one flat grey**,
     per the user's exact spec: 0.15/0.25/0.35 opacity, each stage lasting `fuseFrames / 3`
     (`CurrentTelegraphOpacity()`, checked each `Draw()` call against `elapsed`) — the last stage
     covers any remainder frames from the integer division before arming (turning red with a live
     hitbox) exactly as before. Purely visual; `Update()`'s arming logic and the collision fix from
     entry 108 are untouched. Verified via a scripted repro (reflection into the private `elapsed`
     field and `CurrentTelegraphOpacity()` method, no real save-file risk since nothing here touches
     `Player.Instance`): with `fuseFrames = 99` (a clean 33-frame-per-stage split), all 3 stage
     boundaries (`elapsed` 0, 32, 33, 65, 66, 98) returned the expected opacity. Clean build and a
     plain boot-check both passed.
111. **`GrenadeProjectile`'s armed (red) state also cycles through 3 opacity stages now** — 0.55 /
     0.65 / 0.75, each `ArmedCycleStageLength` (10 frames, ~0.17s) long — but *repeating*
     (`CurrentArmedOpacity()` wraps via `% 3`) for as long as the grenade stays armed, unlike the
     telegraph's one-shot ramp, giving the live hazard a pulsing "this is dangerous now" cue. Purely
     visual, per the user's explicit framing — `Radius` (the hitbox) is set once in `Update()` when
     arming happens and is never touched by either opacity method, identical across all 3 stages.
     Verified via a scripted repro (reflection into `elapsed`/`armed`/`CurrentArmedOpacity()`, no
     real save-file risk): with `fuseFrames = 30`, confirmed the 3 stage boundaries relative to the
     arming frame, confirmed the cycle wraps back to stage 1 after a full 30-frame cycle, and
     confirmed `Radius` stayed at the armed value throughout. Clean build and a plain boot-check
     both passed.
112. **`GrenadeProjectile` no longer expires the instant it first touches the player** — it lingers
     for its full `duration` as a real persistent AoE hazard, per the user's explicit request, while
     only ever damaging the player once. Two new generic `EnemyProjectile` fields make this
     possible: `ExpiresOnHit` (default `true`, mirroring the player's own `Projectile.ExpiresOnHit`
     — every other enemy projectile keeps today's exact behavior) and `HasHitPlayer` (latches `true`
     the first time it actually damages the player). `EntityManager.HandleCollisions()`'s
     enemy-projectile-vs-player block now skips any projectile with `HasHitPlayer` already set,
     marks it after a real hit regardless of `ExpiresOnHit`, and only sets `IsExpired` when
     `ExpiresOnHit` is true — so a non-expiring projectile can never be checked-and-hit twice, and
     everything that already expired on the original single hit keeps doing exactly that (marking
     `HasHitPlayer` and expiring happen in the same frame for them, same as before this existed).
     `GrenadeProjectile`'s constructor sets `ExpiresOnHit = false`; it now only ever goes away via
     its `duration` timeout (or leaving world bounds), never from the collision loop. Verified via a
     scripted repro (real save files backed up first per `CLAUDE.md`, since the test mutates
     `Player.Instance.Health`): an armed grenade sitting exactly on the player damages them once on
     the first overlapping frame, stays alive and un-expired through 50 more overlapping frames with
     zero further health loss, confirms `HasHitPlayer` latched, and finally expires via its own
     duration timer rather than the collision check. Real save files restored/diff-verified
     afterward. Clean build and a plain boot-check both passed.
113. **Black backing added to the player's own "damage taken" numbers**, matching the same
     offset/alpha the title screen's text already uses (`Overlay.DrawTitle()`/
     `GameOverState.Draw()`'s `(-4, 4)` offset, `Color.Black * 0.5f`). New optional
     `DamageNumber(..., bool hasBlackBacking = false)` parameter, drawing a black copy behind the
     real text when true, fading in step with the number's own alpha via a new `currentAlpha` field
     (so the backing never outlives the fading colored text as a stray black artifact). Scoped only
     to `Player.Hit()`'s call site — enemy hit numbers (`Enemy.WasShot()`) are untouched, matching
     the user's request specifically about the player's own damage number. Purely visual, no
     gameplay/state change; verified via a clean build and a plain boot-check.
114. **Dedicated `Art.DamageFont` for floating combat damage numbers**, replacing `Art.HudFont`
     (`DamageNumber.cs`'s two `DrawString` calls — the real number and its entry-113 black backing).
     Per the user's follow-up that the numbers were "still a bit hard to read" even after the
     earlier `Scale = 1.3f` bump (an ordinary-weight font stays low-contrast no matter how large it's
     drawn) — the fix is weight, not just size. New `Content/Fonts/DamageFont.spritefont`, copied
     from `HudFont.spritefont`'s exact XML shape with `<Size>` 12→16 and `<Style>` Regular→Bold; a
     matching `#begin`/`#build` block added to `Content.mgcb` right after `HudFont`'s own (same
     `FontDescriptionImporter`/`FontDescriptionProcessor` params); `Art.cs` gained the
     `DamageFont` field and its `content.Load<SpriteFont>("Fonts/DamageFont")` line, loaded
     alongside `HudFont`/`TitleFont`. Purely visual, no gameplay/state change. Verified: clean build
     compiled the new asset (`DamageFont.xnb` confirmed present in `bin/Debug/net8.0-windows/
     Content/Fonts/`, not skipped), and a plain boot-check (minimized, `IsIconic()`-confirmed)
     showed the process starting and staying alive with no temp code involved.
115. **Boss portals now show their dungeon's name and their own themed art**, first step toward the
     still-open "unique dungeon per boss" backlog idea. `Destination.BossDestination` gained a
     `DungeonName` field, separate from `BossName` (the boss fought inside, e.g. "Limon the Sprite
     Goddess") — `DungeonName` is the room's own identity ("Sprite World" for Limon's realm, "Snake
     Pit" for Stheno's), and `DisplayName` (what `Portal.Draw()`'s label actually shows) now returns
     it instead of the old hardcoded generic `"Boss Fight"` string. User-supplied art (`Content/
     Sprite World Portal.png`, `Content/Snake Pit Portal.png`, both 260×104, a 7-frame animation laid
     out 5-wide/2-row rather than one long strip) wired into `Content.mgcb`/`Art.cs` as
     `Art.SpriteWorldPortal`/`Art.SnakePitPortal`. `AnimatedTexture` (previously single-row-strip
     only — `DrawFrame()` always read `Y=0` and used the full texture height as one frame) gained an
     optional `columns` param to `Load()`; when given, `DrawFrame()` derives `rows` from
     `frameCount`/`columns` and slices both X *and* Y from the frame index, while every existing
     single-row caller (`Art.Portal`, `Portal.png`) is unaffected since omitting `columns` defaults it
     to `frameCount`, reproducing the exact old one-row math. New `Destination.PortalArt()` (`internal
     virtual`, defaults to `Art.Portal`, overridden on `BossDestination` to return a lazily-invoked
     `Func<AnimatedTexture>` set per-instance) is what `Portal`'s constructor now calls instead of
     hardcoding `Art.Portal` — resolved lazily (only invoked once a real `Portal` is constructed, long
     after `Art.Load()` has run) specifically because eagerly evaluating an `Art.*` field inside a
     `static readonly Destination` field initializer would capture `null` (those run before content
     loads). `Portal`'s old fixed `RenderedSize = 96` constant (assumed every portal used the same 64px
     source frame) was replaced with per-instance `RenderedWidth`/`RenderedHeight` properties reading
     a new `AnimatedTexture.FrameWidth`/`FrameHeight` pair, so the label still centers correctly under
     the new sheets' smaller 52px frames instead of assuming the generic swirl's size. Only the two
     boss-realm destinations changed behavior — every other portal (Realm/CharacterSelect/Bank/Nexus,
     and the boss arena's own exit portal) still resolves to the plain swirling `Art.Portal`, unchanged.
     Verified via a scripted repro (temp code in `Game1.StartGame()`, no `Player.Instance` mutation so
     no save-file risk): confirmed `BossRealm`/`SthenoBossRealm`'s `DisplayName` read "Sprite World"/
     "Snake Pit" exactly; confirmed (via reflection into the internal `PortalArt()` method) `BossRealm`
     resolves to the exact `Art.SpriteWorldPortal` reference, `SthenoBossRealm` to `Art.SnakePitPortal`,
     and both `Realm`/`Bank` still resolve to the exact `Art.Portal` reference; confirmed
     `FrameWidth`/`FrameHeight` read 52×52 for both new sheets vs. 64×64 for the generic one; hand-
     verified the source-rectangle math for all 7 real frames against the sheet's actual 5×2 layout;
     and constructed real `Portal` instances with each new destination and called `Draw()` through a
     real `SpriteBatch.Begin()/End()` pair with no exception for both new textures. Clean build
     confirmed both new `.xnb` assets actually compiled (not skipped), and a plain boot-check (no temp
     code) passed.
116. **F3 debug overlay now outlines portals too.** Portal isn't an `Entity` subclass (no `Shape`/
     `Radius`/`Width`/`Height`), so it was never covered by `EntityManager.DrawHitboxes()`'s existing
     per-entity dispatch — added a new optional `IEnumerable<Portal> portals` param instead, drawn
     via a new axis-aligned `DrawHitboxRectangle()` helper (Portal's teleport-trigger rectangle is
     never rotated, so it doesn't need `DrawHitboxRotatedRectangle()`'s orientation math), in cyan to
     stay visually distinct from the existing red/lime/yellow/orange entity outlines. New public
     `Portal.Bounds` exposes the previously-private `bounds` rectangle (the actual teleport-trigger
     area used by `Update()`'s `Player.Instance.Bounds.Intersects(bounds)` check — not the sprite's
     visual footprint) read-only for this purpose. Each state passes whichever portal list is
     actually valid for it right now — `NexusState.Draw()` passes its own fixed `portalList`,
     `RealmState.Draw()` (inherited as-is by `BossRealmState`, which only overrides `DrawBossHud()`)
     passes `Portal.DroppedPortals` — rather than `DrawHitboxes()` reaching for a shared static
     itself, so a stale list left over from a previous state (e.g. `DroppedPortals` isn't cleared on
     returning to the Nexus) never draws a stray outline with no matching sprite on screen; the
     `portals` param defaults to `null` so every other existing caller/behavior is unaffected.
     Verified via a scripted repro (temp code in `Game1.StartGame()`, no `Player.Instance` mutation):
     confirmed a portal's `Bounds` matches the exact rectangle `Update()`'s own trigger check already
     used (`position + (64, 64)`, 32×32); called `EntityManager.DrawHitboxes()` both with a real
     portal list and with the param omitted, through a real `SpriteBatch.Begin()/End()` pair, with no
     exception either way. Clean build and a plain boot-check both passed.
117. **Fixed portal teleport-trigger bounds not scaling with the new dungeon-specific art**, caught by
     the user via the F3 outline added in entry 116. `bounds`'s old formula (`position + (64, 64)`,
     32×32) was a magic-number offset tuned entirely around the generic swirl's 96px rendered size
     (64 = 96 × 2/3, 32 = 96 × 1/3) — it never accounted for a portal drawing at a different size, so
     entry 115's smaller dungeon sheets (78px rendered, not 96px) put the actual trigger zone mostly
     or entirely outside the visible sprite, invisible until the new debug outline made it obvious.
     Rewrote `bounds` to compute the same 2/3-offset, 1/3-size box as a fraction of each portal's own
     `RenderedWidth`/`RenderedHeight` (already available from entry 115's label-centering fix) instead
     of the fixed 64/32 constants — this reproduces the exact old numbers for the generic portal (96 ×
     2/3 = 64, 96 × 1/3 = 32, so no regression there) while correctly landing inside the smaller
     dungeon portals' own footprint (78 × 2/3 = 52, 78 × 1/3 = 26). Verified via a scripted repro (temp
     code in `Game1.StartGame()`, no `Player.Instance` mutation): confirmed the generic portal's
     `Bounds` is still exactly `(264, 364, 32, 32)` for a portal at `(200, 300)` (byte-for-byte
     unchanged from before this fix); confirmed both `Sprite World`/`Snake Pit` portals' `Bounds` now
     sit fully contained within their 78×78 visible sprite, which they did not before. Clean build and
     a plain boot-check both passed.
118. **Fixed the actual visual misalignment behind entry 117's fix**, per the user reporting the F3
     outline still looked wrong after it. Entry 117 only fixed the trigger box's *size* scaling to
     each portal's own rendered footprint — it kept the box anchored to that footprint's bottom-right
     *corner* (a 2/3 offset), inherited unexamined from the original hardcoded 64px/96px numbers. That
     placement was never actually visually verified before: rendering each portal + its F3 outline to
     an offscreen `RenderTarget2D` and inspecting the resulting PNG directly (rather than only
     comparing numbers) showed the box sitting entirely beside the visible sprite, not on it — every
     portal's art (swirl, arch, diamond) is roughly circular/pointed and doesn't fill the corners of
     its own bounding square, so a corner-anchored box was always going to miss it regardless of size.
     Changed `BoundsOffsetFraction` from `2/3` to `1/3` (same `1/3`-size box, now centered in the
     middle third of the footprint instead of tucked in a corner) — confirmed via re-rendered PNGs
     that the box now sits squarely on the swirl, the Snake Pit arch's dark mouth, and both extremes of
     Sprite World's animation (frame 0's small closed icon and frame 6's fully-formed diamond). Noted
     in a code comment as a known remaining limitation: the box is still a single fixed rectangle, not
     re-derived per animation frame, so alignment during Sprite World's smaller in-between frames is
     approximate rather than exact — not worth a per-frame hitbox for a debug outline plus a walk-up
     teleport trigger. Verified via a scripted repro (temp code in `Game1.StartGame()`, no
     `Player.Instance` mutation): rendered all three portals plus a forced Sprite World frame-6 render
     to standalone PNGs via an offscreen `RenderTarget2D` and visually inspected each — this is the
     first time this feature was actually checked by looking at rendered pixels rather than only
     comparing numbers, which is what let entry 117's corner-anchoring bug slip through. Clean build
     and a plain boot-check both passed.
119. **Fixed `Entity.Bounds` itself — the real remaining source of portal collision feeling "slightly
     off"** even after entry 118's centering fix, per the user reporting the outline now matched the
     sprite but actual entry still didn't line up. Entry 118 only fixed the *portal's* side of
     `Player.Instance.Bounds.Intersects(bounds)`; `Player.Instance.Bounds` — the other side — was
     still silently broken. `Entity.Bounds` anchored `Position` at the rectangle's top-left corner
     (`new Rectangle((int)Position.X, (int)Position.Y, Width, Height)`), but `Position` means "center"
     everywhere else in the engine: `Entity.Draw()` renders with `Origin = Size / 2f`, and circular
     (`Radius`-based) collision treats `Position` as the center point too. `EntityManager.cs`'s own
     internal `RectangleBounds()` helper had already noticed and documented this exact discrepancy in
     a comment (built its own separately-centered box specifically to avoid reusing `Entity.Bounds`)
     — but `Player.Instance.Bounds` was still used directly, uncorrected, by `Portal.cs`'s teleport
     check and, incidentally, two other real collision checks that share the same class hierarchy:
     `LootBag.cs`'s pickup check and `ItemSpawner.cs`'s nearest-bag distance (`LootBag : Entity`, so
     both sides of `Player.Instance.Bounds.Intersects(bag.Bounds)` were equally affected). Changed
     `Entity.Bounds` to center on `Position` (`Position - Size/2`, matching `Draw()`/circular
     collision/`RectangleBounds()`'s existing convention) — a root-cause fix rather than special-
     casing Portal.cs, since the same bug would have kept silently affecting loot pickup too. Updated
     `RectangleBounds()`'s stale doc comment (previously described `Entity.Bounds` as wrongly-anchored
     — no longer true) to instead note the one remaining real difference: `Entity.Bounds` still
     doesn't account for rotation, which is the actual reason `RectangleBounds()` still exists as a
     separate helper. Verified via a scripted repro (temp code in `Game1.StartGame()`, read-only
     against the real `Player.Instance` — no mutation, no save-file risk): confirmed
     `Player.Instance.Bounds` is now exactly `Position - Size/2`; confirmed a portal positioned so its
     own rendered footprint is exactly centered on the player's current `Position` now correctly
     returns `true` from `Player.Instance.Bounds.Intersects(portal.Bounds)` (both sides finally
     centered the same way); confirmed a portal 500px away still correctly returns `false` (no
     over-correction into always-true). Clean build and a plain boot-check both passed.
120. **F3 debug overlay now outlines loot bag pickup range too.** `LootBag : Entity`, so it does have
     a `Shape`/`Radius`, but its real pickup check (`LootBag.Update()`) never goes through
     `EntityManager.IsColliding()` at all — it hand-rolls `Player.Instance.Bounds.Intersects(this.Bounds)`
     directly, bypassing `Shape` entirely. Reusing the generic `Shape`-based `DrawHitbox()` dispatch
     here would draw a circle that has nothing to do with the actual check, so this reuses entry
     116/118's rectangle-outline path (`DrawHitboxRectangle()`, `Bounds`) instead, in magenta to stay
     distinct from every other debug color already in use. Read directly off the static
     `ItemSpawner.LootBags` rather than needing a caller-supplied parameter like the portals list —
     unlike `Portal.DroppedPortals`/`NexusPortals` (which differ per state, entry 116's reason for
     requiring an explicit param), `ItemSpawner.LootBags` is a single list already correctly scoped to
     whichever state is current (`Game1.ChangeState()` clears it via `ItemSpawner.Reset()` on every
     transition), so there's no equivalent stale-list risk to guard against. Verified via a scripted
     repro (temp code in `Game1.StartGame()`; `LootBag`s are ephemeral/never persisted, so no save-file
     risk): confirmed a bag's `Bounds` is centered on its `Position` (entry 119's fix applies here
     too, same base class); rendered a real bag plus its new debug outline together to an offscreen
     `RenderTarget2D` and visually inspected the PNG — the lesson from entry 118 (numbers matching
     isn't enough, actually look at the pixels) — confirming the magenta box lands squarely on the
     bag sprite. Clean build and a plain boot-check both passed.
121. **`AnimatedTexture` can now play a one-shot (non-looping) animation**, and Sprite World's portal
     uses it. Previously `UpdateFrame()` always wrapped (`frame %= frameCount`), so every animation —
     including Sprite World's "closed → forming → fully open" sheet — replayed forever, snapping back
     to its small closed-icon frame right after finishing the open sequence. New `Load(...)` param
     `bool loop = true` (defaults preserve every existing caller's behavior byte-for-byte); when
     `false`, `UpdateFrame()` clamps at `frameCount - 1` once reached instead of wrapping, and an early
     return skips the elapsed-time accumulation entirely once already holding there. `Art.cs` passes
     `loop: false` only for `SpriteWorldPortal` — the generic swirl and Snake Pit (both genuine idle
     loops) are untouched and still wrap normally. Verified via a scripted repro (reflection into the
     private `frame` field, no `Player.Instance` involvement so no save-file risk): advanced the
     generic portal and Snake Pit 8 ticks each (enough to cross all 7 frames at 8fps) and confirmed
     both wrapped back off frame 6 as before; advanced Sprite World the same 8 ticks and confirmed it
     landed on frame 6, then advanced it 20 more ticks and confirmed it stayed on frame 6 rather than
     ever wrapping back to 0. Clean build and a plain boot-check both passed.
122. **Bosses now blink red a few times when entering an enraged phase**, starting with Limon (the
     only boss that currently has a real "enraged" concept — Stheno's 3-phase cycle is continuous
     variety, not a health-triggered escalation, so it wasn't wired in there). Built as shared
     infrastructure on `Enemy.cs` (not Boss-specific) so any future boss's own phase-transition point
     can call it too, per the request covering "bosses in general." New `protected void
     FlashRed(int blinkCount = 3, int periodFrames = 8)` sets a tick counter
     (`blinkCount * 2 * periodFrames`); `Update()` ticks it down each frame and derives `blinkOn` from
     which half of the current `periodFrames`-sized block it's in. `Draw()` swaps `color` to
     `Color.Red` for exactly one `base.Draw()` call when `blinkOn`, then restores the original value
     immediately afterward — a temporary swap rather than overwriting `color` outright, since that
     field already carries the spawn fade-in alpha (`Update()`'s `timeUntilStart` branch) and
     permanently clobbering it would break that separately. `LimonTheSpriteGoddess.PhaseWatcher()`
     calls `FlashRed()` once, at the same moment it already sets `enraged = true` and adds the
     phase-2 attack/movement behaviours. Verified via a scripted repro (backed up real save files
     first per `CLAUDE.md`, since the test constructs a throwaway `NexusState` to initialize
     `Game1.Camera` — a documented prerequisite for calling `Enemy.Update()` directly — which saves in
     its own constructor; restored and diff-verified unchanged afterward): dropped a real `Limon`
     instance's health to exactly the 50% threshold and confirmed one `Update()` call set the blink
     counter to exactly 48 (3 blinks × 2 × 8); stepped 50 more frames and recorded the on/off pattern
     (`00000000 11111111` × 3, then off) — three full cycles as specified, ending back off; called
     `Draw()` on every one of those frames and confirmed `color` matched its pre-blink value
     immediately after every single call, proving the swap-and-restore never leaks a permanent red
     tint. Clean build and a plain boot-check both passed.
123. **Portals now require a confirmation before teleporting** — walking in arms a prompt rather than
     teleporting instantly, entered via either a clickable HUD button or a new remappable key bind
     (`KeyBindings.Action.ConfirmPortalEntry`, defaults to `R`, added the same way as every other
     action — enum value, `AllActions`, `DisplayName`, `Defaults`, `ToData()`/`FromData()`,
     `Data/KeyBindingsData.cs` field — so it appears on the Settings screen for free, that screen
     already building its rows generically off `AllActions`). Applies uniformly to every portal that
     actually teleports (Realm/CharacterSelect/BossRealm/SthenoBossRealm/Nexus); the Bank portal is
     unaffected since `Update()`'s Bank branch already returns before reaching the teleport-trigger
     check this touches. `Portal.Update()`'s `Player.Instance.Bounds.Intersects(bounds)` branch no
     longer calls `EnterPortal()` directly — standing in the trigger sets a new static
     `pendingConfirmation` to `this` instead (cleared if the player steps back out), and only the
     confirm key bind (checked right there in `Update()`) or a click on the HUD button (routed through
     a `Click` event) actually calls `EnterPortal()`. New static `Portal.DrawConfirmationPrompt()`
     draws the button (`Realm.Controls.Button`, the same class every menu screen already uses) plus a
     "or press [X]" text hint anchored above whichever portal is pending, converting its world position
     to screen space via `Vector2.Transform(pos, Game1.Camera.GetTransformation())` — the same technique
     `BankSystem.Anchor` already uses to track its own portal on screen. This has to be a *separate*
     draw call from `Portal.Draw()` itself: `Portal.Draw()` runs inside the camera-transformed
     world-space `SpriteBatch` block, which can't also host a raw-screen-space `Button` correctly, so
     each state's untransformed HUD pass (`NexusState`/`RealmState.Draw()`, right where
     `Overlay.DrawSidebar`/`BankSystem.Draw` already live) calls the new method instead. The `Button`
     itself is constructed lazily (a property, not a field initializer) since eagerly reading
     `Art.ButtonTexture`/`Art.HudFont` at Portal's static-init time would run before `Art.Load()` —
     same hazard `Destination.PortalArt()` (entry 115) already works around. One dangling-reference bug
     caught before it shipped: leaving a portal's confirm prompt via any path other than its own confirm
     flow (Escape to menu, the always-available `ReturnToNexus` key bind, dying) would leave
     `pendingConfirmation` pointing at a `Portal` instance belonging to the now-discarded state, so
     `DrawConfirmationPrompt()` would keep rendering a phantom prompt anchored to a stale world position
     on a completely different screen — fixed by adding `Portal.ClearPendingConfirmation()`, called from
     `Game1.ChangeState()` (every state transition funnels through there, not just `RealmState`'s own
     constructor) rather than trusting each individual exit path to remember. A second real bug caught
     by the scripted repro itself, not by inspection: `Util.LoadKeyBindingsData()` reads the player's
     real on-disk save, which predates this action entirely, so `KeyBindingsData.ConfirmPortalEntry`
     deserializes as `null` on every existing save — `KeyBindings.FromData()`'s original unconditional
     `bindings[action] = InputBinding.Deserialize(data.action)` pattern (correct for the other 9
     fields, all of which are guaranteed present) would silently overwrite the Defaults-seeded `R`
     binding with `Deserialize`'s "missing data" fallback (`Keys.None`), unbinding the key half of this
     feature for every existing save until the player happened to open Settings and rebind it by hand.
     Fixed with a guard specific to this one field: only overwrite when `data.ConfirmPortalEntry` is
     actually non-empty, leaving the Defaults value in place otherwise. Verified via a scripted repro
     (temp code in `Game1.StartGame()`; backed up real save files first per `CLAUDE.md`, since the test
     constructs a throwaway `NexusState` for `Game1.Camera` init, which saves in its own constructor —
     restored and diff-verified afterward, `PlayerData_Wizard.json` came back byte-different as expected
     from that real save and was restored from backup): confirmed the default binding reads `R` (not
     `None`) even after a real `Util.LoadKeyBindingsData()` call against the actual old save file;
     simulated keyboard state via reflection into `Input`'s private `keyboard`/`previousKeyboard` fields
     (real hardware polling can't be driven from a test, same documented `CLAUDE.md` limitation as
     `Controls.Button`) to confirm standing on a portal with no key press arms `pendingConfirmation` to
     that exact portal without queuing a state transition; confirmed stepping back off cancels it;
     confirmed a simulated press of the bound key both clears `pendingConfirmation` and queues a real
     state transition; confirmed `DrawConfirmationPrompt()` renders through a real `SpriteBatch`
     `Begin()`/`End()` pair with no exception while a confirmation is pending; and confirmed
     `Game1.ChangeState()` clears a re-armed `pendingConfirmation` regardless of which portal set it.
     Clean build and a plain boot-check both passed.
124. **Moved the portal confirmation prompt (entry 123) from a floating world-space widget above the
     portal into a fixed sidebar section below the inventory grid, and added the dungeon name as a
     heading above the button.** `Portal.DrawConfirmationPrompt()` no longer converts the pending
     portal's world position to screen space via the camera transform — it now anchors at
     `Game1.SidebarX + 20` horizontally and `Player.Instance.Inventory.Bounds.Bottom + 20` vertically
     (the live inventory bounds rather than a hardcoded Y, so this stays correctly positioned if that
     grid's own layout ever changes), stacking three lines top to bottom: the dungeon name
     (`pendingConfirmation.DisplayName` — "Sprite World", "Snake Pit", etc.), the "Enter" button, then
     the "or press [X]" key-bind hint. Stayed in `Portal.cs` rather than moving into `Overlay.cs`
     since it still owns the `Button`/click-wiring itself — `Overlay.cs` doesn't need to know anything
     about portals, it just decides where in the draw order to invoke it (both states now call it
     immediately after `Overlay.DrawSidebar()`, matching the new "below inventory" placement). Verified
     via a scripted repro (temp code in `Game1.StartGame()`; backed up real save files first per
     `CLAUDE.md` since the test constructs a throwaway `NexusState` for `Game1.Camera` init, which
     saves in its own constructor — restored and diff-verified unchanged afterward this time, unlike
     entry 123's incidental Wizard save diff): reflectively armed `pendingConfirmation` with a real
     `BossRealm` portal and rendered `DrawConfirmationPrompt()` alone to an offscreen `RenderTarget2D`
     sized to the sidebar's width, then visually inspected the resulting PNG — confirming "Sprite
     World" / "Enter" / "or press [R]" render as three cleanly stacked, left-aligned lines that fit
     within the sidebar's width with no overflow, per the lesson from entry 118 (verify by looking at
     the actual pixels, not just the coordinate math). Clean build and a plain boot-check both passed.
125. **Portal animations are now instance-specific**, per the user noticing that a new portal dropped
     after an earlier one's animation already finished wouldn't play its own animation — it'd just
     show the already-finished state immediately. Root cause: `Destination.PortalArt()` (entries 115/
     118) handed out the literal shared `Art.Portal`/`Art.SpriteWorldPortal`/`Art.SnakePitPortal`
     `AnimatedTexture` instance itself, so every `Portal` of the same destination shared one global
     frame/elapsed clock — harmless for a looping animation (they just all stay in sync forever), but
     for Sprite World's non-looping one-shot (entry 121), the *first* portal to reach its held last
     frame permanently put every other portal sharing that same object in the finished state too, since
     they were all reading `frame` off the exact same object. New `AnimatedTexture.Clone()` creates a
     fresh instance sharing the original's already-loaded `Texture2D` (no reload, no extra content-pipe
     work) and layout/timing config (frameCount, columns, timePerFrame, loop, Rotation/Scale/Depth/
     Opacity/Origin), but with its own frame/elapsed/paused state reset to a clean start. Both
     `Destination.PortalArt()` implementations (the base swirl default and `BossDestination`'s override)
     now call `.Clone()` on the art before returning it, so every single `Portal` constructed — not just
     ones using the non-looping sheet — gets a genuinely independent animation instance; applied
     uniformly rather than special-cased to just the non-looping case, since even looping portals
     spawned at different times animating in forced lockstep was itself a minor side effect of the same
     shared-instance root cause. Verified via a scripted repro (temp code in `Game1.StartGame()`, no
     `Player.Instance` mutation so no save-file risk): confirmed a constructed portal's `image` is no
     longer reference-equal to `Art.SpriteWorldPortal`, and that two different portals of the same
     destination don't share an `image` with each other either, while both still share the exact same
     underlying `Texture2D` (confirming `Clone()` didn't trigger a reload); drove one portal's clone all
     the way to its held last frame (6), then constructed a brand new portal of the same destination
     *after* that and confirmed its clone started fresh at frame 0 (not stuck at 6) — the actual bug —
     and that it played through its own 0→6 progression independently while the first portal stayed
     held at 6 throughout, unaffected; confirmed `Draw()` still renders correctly through a real
     `SpriteBatch` pass with the cloned texture. Clean build and a plain boot-check both passed.
126. **Fixed portals visually spawning offset down-and-right of the position they're constructed at**,
     per the user noticing this on enemy-dropped portals specifically (e.g. `Portal.DroppedPortals.Add(
     new Portal(this.Position, portalDropOnDeath))` in `Enemy.WasShot()`). Root cause: `Portal`'s
     `position` field was drawn/bounds-checked as a top-left corner (`image.DrawFrame(spriteBatch,
     position)`, no origin offset), but every caller was already passing another entity's own
     `Position` — which means CENTER everywhere else in the engine (`Entity.Draw()` renders with
     `Origin = Size/2f`, exactly like the `Entity.Bounds` bug fixed in entry 119) — so a portal
     constructed at a dying enemy's exact center visually rendered starting AT that point and extending
     only right/down from it, never actually centered there. Added a new private `TopLeft` property
     (`position - (RenderedWidth/2, RenderedHeight/2)`) that converts the now-consistently-CENTER
     `position` into whatever the actual top-left-anchored draw/bounds math needs; `Draw()`'s
     `image.DrawFrame()` call and the trigger `bounds` rectangle both switched from `position` to
     `TopLeft`, and the label positioning simplified accordingly (`position.X - size.X/2` centers under
     the sprite directly, no longer needing to add half the width first). The Bank branch's local
     `center` variable was actually redundant this whole time once `position` itself started meaning
     center — removed the `+ (RenderedWidth/2, RenderedHeight/2)` offset there too, now just using
     `position` directly for both the proximity check and `BankSystem.PortalPosition`. No caller (
     `Enemy.cs`, `BossRealmState.cs`'s exit portal, every fixed `NexusState.cs` portal) needed to
     change at all — they were already passing "where I want this to visually appear," exactly what
     `position` now correctly means; only `Portal.cs`'s own internal interpretation of that value was
     wrong. This also incidentally fixes `Overlay.cs`'s minimap blips (`DrawBlip(portal.Position, ...)`)
     to plot each portal's true center instead of its old top-left corner. Verified via a scripted
     repro (temp code in `Game1.StartGame()`, no `Player.Instance` mutation so no save-file risk):
     confirmed a portal constructed at `(500, 500)` computes `TopLeft` as exactly `(461, 461)` — `(500,
     500)` minus half of Sprite World's 78×78 rendered footprint, i.e. `(39, 39)`; then, per the lesson
     from entry 118 (verify by looking at the actual pixels), rendered a portal plus a small marker at
     the exact position it was constructed with to an offscreen `RenderTarget2D` and visually confirmed
     the marker sits in the middle of the portal sprite rather than at its top-left corner. Clean build
     and a plain boot-check both passed.
127. **Real art for the boss arena's exit portal (`Portal.Destination.Nexus`)**, user-supplied
     (`Content/Portal to Nexus.png`, 56×56). Unlike the other two dungeon-specific sheets (entry 115),
     this is a single static frame, not an animation — loaded as a 1-frame `AnimatedTexture` anyway
     (`Art.NexusPortal.Load(content, "Portal to Nexus", 1, 1)`) rather than a plain `Texture2D`, so it
     drops into `Portal`'s existing `AnimatedTexture`-typed `image` field and `DrawFrame()` call with
     no special-casing — `UpdateFrame()` simply has nowhere to advance to with `frameCount = 1`.
     `Destination.NexusDestination` (previously falling through to the base class's default generic
     swirl) now overrides `PortalArt()` to return `Art.NexusPortal.Clone()`, matching entry 125's
     per-instance-clone convention so multiple exit portals (if that's ever a thing) wouldn't share one
     animation clock. Content.mgcb block added in the same shape as every other portal art. Verified via
     a scripted repro (no `Player.Instance` mutation, no save-file risk): confirmed a `Portal`
     constructed with `Destination.Nexus` gets a genuinely distinct `AnimatedTexture` instance from
     `Art.NexusPortal` (not the same reference) while still sharing its underlying `Texture2D`;
     confirmed `FrameWidth`/`FrameHeight` read exactly 56×56; confirmed `DisplayName` reads "Nexus";
     called `Draw()` through a real `SpriteBatch` pass with no exception; and — per the entry 118 lesson
     — rendered the portal next to a marker at its exact construction position and visually confirmed
     it's centered rather than offset. Clean build (new `.xnb` confirmed compiled, not skipped) and a
     plain boot-check both passed.
128. **All four portal animations drawn at native size (`Scale` 1.5 → 1.0)**, per the user's explicit
     request after asking whether a scale setting was in effect. `Art.cs`'s four `AnimatedTexture`
     constructions (`Portal`/`SpriteWorldPortal`/`SnakePitPortal`/`NexusPortal`) all shared the same
     `Scale = 1.5f` third constructor argument; changed uniformly to `1.0f`. No other code changes
     needed — `Portal`'s trigger bounds, label position, and confirmation-prompt sizing all already
     derive from `RenderedWidth`/`RenderedHeight` (`image.FrameWidth * image.Scale`, entry 115/118),
     so every dependent measurement recomputes correctly at the smaller size automatically. Rendered
     sizes now match each sheet's native source frame exactly: generic swirl 64×64 (was 96×96), Sprite
     World/Snake Pit 52×52 (was 78×78), Nexus 56×56 (was 84×84). Verified via a scripted repro (no
     `Player.Instance` mutation, no save-file risk): confirmed all four `Art.*` instances read
     `Scale == 1` and their native `FrameWidth`/`FrameHeight`; confirmed a portal's `Bounds` rectangle
     recomputed proportionally at the new smaller size with no manual adjustment; rendered a portal
     next to a marker at its construction point and visually confirmed it still renders centered and
     correctly labeled at the new 1:1 scale. Clean build and a plain boot-check both passed.
129. **Real art for the Bank portal (`Portal.Destination.Bank`)** — a 40×40 chest icon, user-supplied
     (`Content/Vault Chest.png`), replacing the generic swirl it had been using by default (it never
     had an overridden `PortalArt()` before this — see entry 115/127, which gave every other
     destination its own art). Same single-static-frame treatment as entry 127's Nexus portal: loaded
     as a 1-frame `AnimatedTexture` (`Art.BankPortal`), `Destination.BankDestination` now overrides
     `PortalArt()` to return `Art.BankPortal.Clone()`. Caught and fixed a real, independently-stale bug
     while touching this: `BankSystem.cs`'s panel-anchor math still had a hardcoded `const int
     PortalOnScreenHalfHeight = 48`, whose own comment named a `Portal.RenderedSize` field that no
     longer exists at all (removed when portal sizing became per-instance/per-art — entry 118) — it had
     already gone stale once (entry 128's scale change alone made the real footprint 64px, not the
     96px this constant assumed) before today's chest swap made it *twice* wrong (real footprint now
     40px). Replaced with a computed property reading the bank portal's actual current art directly
     (`Art.BankPortal.FrameHeight * Art.BankPortal.Scale / 2f`), so it can never drift out of sync with
     whatever art the Bank destination happens to use again. Verified via a scripted repro (real save
     files backed up first per `CLAUDE.md`, since the test constructs a throwaway `NexusState` for
     `Game1.Camera` init — restored and diff-verified unchanged afterward): confirmed a `Bank`-
     destination `Portal` gets its own distinct `AnimatedTexture` sharing `Art.BankPortal`'s underlying
     texture; confirmed `FrameWidth`/`FrameHeight` read exactly 40×40 and `DisplayName` reads "Bank";
     confirmed `BankSystem`'s (reflected, private) `PortalOnScreenHalfHeight` now reads 20 instead of
     the stale 48; exercised the real `BankSystem.Bounds` proximity/anchor path end to end with no
     exception; and — per the entry 118 lesson — rendered the portal next to a marker at its exact
     construction position and visually confirmed it's centered. Clean build (new `.xnb` confirmed
     compiled) and a plain boot-check both passed.
130. **Real art for the main Realm portal (`Portal.Destination.Realm`)** — an 80×80 stone archway,
     user-supplied (`Content/Portal to Realm.png`), replacing the generic swirl. Last of the four
     always-present Nexus destinations to get its own art (Bank/Nexus in entries 127/129, Realm here —
     CharacterSelect is the only one still on the generic swirl, no art supplied for it yet). Same
     single-static-frame treatment as entries 127/129 (`Art.RealmPortal`, a 1-frame `AnimatedTexture`),
     `Destination.RealmDestination` now overrides `PortalArt()`. Both the explicit-position constructor
     and the parameterless `Portal()` constructor (`dest = Destination.Realm` — the `// TEMP` first
     entry in `NexusState`'s fixed portal list) pick up the new art automatically, since both already
     resolve their image generically via `dest.PortalArt()`. Verified via a scripted repro (no
     `Player.Instance.Position` write has any save-file risk — confirmed not part of `PlayerData` back
     in entry 119 — so no backup needed this time): confirmed a `Realm`-destination `Portal` gets its
     own distinct `AnimatedTexture` sharing `Art.RealmPortal`'s underlying texture; confirmed
     `FrameWidth`/`FrameHeight` read exactly 80×80 and `DisplayName` reads "Realm"; confirmed the
     parameterless `Portal()` constructor independently resolves to the same underlying texture, not
     just the explicit-destination one; and — per the entry 118 lesson — rendered the portal next to a
     marker at its exact construction position and visually confirmed it's centered. Clean build (new
     `.xnb` confirmed compiled) and a plain boot-check both passed.
131. **Real art for the Character Select portal (`Portal.Destination.CharacterSelect`)** — a 56×56
     warrior figure, user-supplied (`Content/Character Changer.png`), replacing the generic swirl.
     Last of the four always-present Nexus destinations to get its own art (Bank/Nexus/Realm in
     entries 127/129/130) — every fixed Nexus portal now has dedicated art, closing out the
     `BACKLOG.md` "improve the portal image and visuals" item. Same single-static-frame treatment as
     the prior three (`Art.CharacterSelectPortal`, a 1-frame `AnimatedTexture`),
     `Destination.CharacterSelectDestination` now overrides `PortalArt()`. Verified via a scripted
     repro (no save-file risk): confirmed a `CharacterSelect`-destination `Portal` gets its own
     distinct `AnimatedTexture` sharing `Art.CharacterSelectPortal`'s underlying texture; confirmed
     `FrameWidth`/`FrameHeight` read exactly 56×56 and `DisplayName` reads "Character Select"; and —
     per the entry 118 lesson — rendered the portal next to a marker at its exact construction position
     and visually confirmed it's centered (the label itself ran off the deliberately small 100px test
     canvas, not a real issue — the actual gameplay viewport is far wider). Clean build (new `.xnb`
     confirmed compiled) and a plain boot-check both passed.
132. **Auto-Fire is now persisted, and the Settings screen extends beyond key bindings for the first
     time** — the backlog's own concrete candidate for this item. `Player.AutoFireEnabled` was
     previously session-only (reset to off on every launch, per its own doc comment). New
     `Data/GameSettingsData.cs` (a small account-wide DTO, mirroring `KeyBindingsData.cs`'s own shape)
     plus `Util.SaveGameSettingsData()`/`LoadGameSettingsData()` — a separate `GameSettingsData.json`
     file rather than folding this into `KeyBindingsData.json`, so that file stays scoped to just
     bindings, matching the backlog note's own "sibling DTO" framing. Reads/writes
     `Player.Instance.AutoFireEnabled` directly rather than routing through a dedicated manager class
     the way `KeyBindings.cs` does for bindings — there's only the one setting so far, so a parallel
     static-class layer isn't earning its keep yet. `Input.cs`'s existing toggle handler now calls
     `Util.SaveGameSettingsData()` right after flipping the value, matching the "save immediately,
     don't wait for a checkpoint" convention already established for key bindings (entry 89) and stars
     (entry 82). One real ordering bug caught before it shipped: `Game1.StartGame()` originally called
     the new load alongside `LoadKeyBindingsData()`/`LoadFameData()` etc., all of which run *before*
     `Util.LoadOrCreatePlayer()` — but `LoadOrCreatePlayer()` calls `ResetPlayer()`, which constructs a
     brand new `Wizard`/`Archer`/`Knight` and replaces `Player.Instance` entirely (`instance = this;` in
     `Player`'s own constructor), silently discarding anything set on the old instance beforehand.
     Fixed by moving `Util.LoadGameSettingsData()` to run *after* `LoadOrCreatePlayer()` instead.
     `States/SettingsState.cs` gained its first non-keybinding control: a plain click-to-toggle
     "Auto-Fire: ON/OFF" row directly below the key-binding list (own dedicated `autoFireRect`/
     `autoFireHover` fields rather than widening the existing `Row` class, which is typed around
     `KeyBindings.Action` specifically — not worth generalizing for just one non-keybinding setting
     yet), sharing the same two-column label/value layout and gated the same way as the existing rows
     (ignored while `listeningFor` is armed). Verified via a scripted repro (confirmed no real
     `GameSettingsData.json` existed on this account first, same discipline as entries 89-91 for
     `KeyBindingsData.json` — deleted the test-created file afterward to restore that exact state):
     confirmed a save→flip→load round-trip correctly restored the saved value; confirmed — the actual
     bug this catches — that a value saved, then a *fresh* `Player.Instance` constructed via
     `ResetPlayer()` (the same call `LoadOrCreatePlayer()` makes internally), still loads correctly
     onto that new instance afterward; confirmed the Auto-Fire row's rect doesn't vertically overlap
     the last key-binding row; rendered a real `SettingsState.Draw()` pass with no exception; and
     simulated a real click on the row via `Input.mouse`/`previousMouse` (public fields, so direct
     assignment — `SettingsState.Update()` reads them directly, unlike `Controls.Button`, which polls
     hardware state itself) and confirmed it both toggled the value and persisted it. Caught two
     harness-only mistakes along the way, not code bugs: an outer `SpriteBatch.Begin()/End()` wrapped
     around `SettingsState.Draw()`, which already wraps its own internally — nesting them crashed the
     real game loop's next frame with a corrupted `SpriteBatch` state (double-`Begin()`), fixed by
     removing the redundant outer wrap; and an initial reflection-based mouse-state simulation that
     returned null (`Input.mouse`/`previousMouse` are public fields, not private — `BindingFlags.
     NonPublic` never finds them), fixed by assigning directly instead. Clean build and a plain
     boot-check both passed.
133. **Settings screen gained a tab bar — Controls, Gameplay, Audio, Graphics** — turning entry 132's
     single flat list into a real multi-tab layout. Key bindings (all 10 `KeyBindings.Action` rows)
     stayed on Controls; the Auto-Fire toggle row moved to its own new Gameplay tab. Audio and
     Graphics are both real, clickable tabs today showing a "No settings here yet." placeholder rather
     than anything functional — no volume control or graphics option exists anywhere else in the
     codebase yet to expose there (confirmed via a repo-wide check before building this), so nothing
     was invented to fill them. New `private enum SettingsTab` and a `TabInfo` class (label + `Rect` +
     hover, mirroring `Row`'s existing shape) drive a `List<TabInfo> tabs` built from
     `Enum.GetValues()`, centered as a group above the content area with each tab's width measured
     from its own label text (same `Art.HudFont.MeasureString()` technique the row-column alignment
     already used). Clicking a tab just sets `currentTab`; clicks are gated by the same
     `listeningFor.HasValue` check that already blocked every other control during an in-progress key
     rebind, so there's no separate "cancel the pending rebind when switching tabs" case to handle —
     it's simply not reachable. The whole layout was rebuilt around **fixed** vertical anchors
     (`tabBarY`, `rowsTop`, the Back/Reset button row) sized against the Controls tab's full 10-row
     list, rather than the old per-tab-row-count centering — otherwise the screen would visibly jump
     every time the user switched tabs, since Gameplay has 1 row and Audio/Graphics have 0. `Reset to
     Defaults` (a key-bindings-only action) is now only drawn/updated while `currentTab ==
     SettingsTab.Controls`, rather than always visible but inert on tabs where it doesn't apply, same
     "don't show a control that can't do anything right now" reasoning as entry 73's ability-bar
     placeholder. Active-tab state gets a persistent gold underline (`Art.HealthBar` stretched into a
     2px bar, same technique `Overlay.cs`'s HUD bars already use) independent of hover, since hover
     alone (shared with every other tab's existing Gold-on-hover feedback) wasn't a reliable enough
     "you are here" cue by itself. Verified via a scripted repro (no save-file risk — constructing
     `SettingsState` and clicking tabs doesn't save anything): confirmed exactly 4 tabs exist with
     non-overlapping rects; confirmed the default `currentTab` is `Controls`; confirmed simulated
     clicks (via direct `Input.mouse`/`previousMouse` assignment, same technique as entry 132) on the
     Gameplay and Audio tabs correctly updated `currentTab`; and — per the entry 118 lesson — rendered
     all three tabs (Controls, Gameplay, Audio) to full-window offscreen `RenderTarget2D`s and visually
     inspected each: Controls shows all 10 key-binding rows with Back+Reset, Gameplay shows just the
     Auto-Fire row with Reset correctly absent, Audio shows the placeholder text with Reset absent —
     and the title/tab bar/button positions held exactly still across all three renders, confirming the
     fixed-layout approach actually works. Clean build and a plain boot-check both passed.
134. **Dedicated `Art.SettingsFont` for the Settings screen**, replacing every `Art.HudFont` reference
     in `States/SettingsState.cs` (labels, tab bar, row values, the Back/Reset buttons' own text via
     the `Button(Texture2D, SpriteFont)` overload instead of the default HudFont-using constructor).
     New `Content/Fonts/SettingsFont.spritefont` — same Arial family as `HudFont` for visual
     consistency with the rest of the game, just a size up (14pt vs. 12pt): a menu screen read at a
     normal, unhurried pace doesn't need HudFont's compact in-combat-overlay scale. Wired the same way
     as every other font asset (`Content.mgcb` block, `Art.cs` field/load line). Verified via a
     scripted repro (no save-file risk): confirmed `Art.SettingsFont` is a genuinely distinct
     `SpriteFont` instance from `Art.HudFont`, measuring visibly larger (22px tall vs. 18px for the
     same "A", both under the fixed 28px `RowHeight` so no inter-row overlap); per the entry 118
     lesson, rendered a real `SettingsState` (Controls tab, the most text-dense) to a full-window
     offscreen `RenderTarget2D` and visually inspected it — all 10 rows read cleanly with no overlap,
     tab labels/underline look correct, and a zoomed crop of the "Reset to Defaults" button (the
     longest button label, most likely to clip against its texture's fixed 160px width) confirmed
     real margin on both sides, not touching or overflowing the border. Clean build (new `.xnb`
     confirmed compiled) and a plain boot-check both passed.
135. **Per-enemy drop pools — a real category-inclusion mechanism, applied to Snake.** New
     `ItemSpawner.LootCategory` `[Flags]` enum (`Weapon`/`Armor`/`Ring`/`AbilityItem`/`StatPotion`/
     `HealthManaPotion`/`All`) gates which categories an enemy's loot roll can even draw from, before
     the existing tier/chance math (`DropChanceDenominator()`/`MaxTierJump()`/`ResolveDropTier()`)
     runs at all. Threaded through both loot paths: `ItemSpawner.Spawn()` (the regular random-chance
     table) gained a `LootCategory dropPool = LootCategory.All` parameter, with each of its 6 category
     blocks now gated `dropPool.HasFlag(LootCategory.X) &&` in front of its existing roll check;
     `ItemSpawner.SpawnGuaranteedLoot()` (boss loot) gained the same parameter, with each category
     block now wrapped `if (dropPool.HasFlag(LootCategory.X)) { ... }`, including the previously-
     unconditional stat potion. New `Enemy.DropPool` field (default `All`, so every existing enemy's
     drops are unchanged unless a factory sets it explicitly) is read by both `Enemy.SpawnLoot()` and
     `Boss.SpawnLoot()`, which now pass it through to their respective `ItemSpawner` calls instead of
     omitting the argument. Applied to exactly the one concrete example the backlog itself gave:
     `Enemy.CreateSnake()` now sets `DropPool = Weapon | Armor | Ring | AbilityItem` (gear only, no
     potions). No other `CreateX()` factory was touched — every other enemy, including BigSnake, keeps
     the default `All` pool; the backlog's other half ("with its own odds," BigSnake "leaning toward
     potions") is a weighted-drop-chance axis this entry deliberately doesn't build, left open in the
     backlog rather than guessed. Verified via a scripted repro (temp code in `Game1.StartGame()`, no
     save-file risk — confirmed via reading `Player.cs`'s `EquipWeapon`/`EquipArmor`/`EquipRing`/
     `EquipAbilityItem` that none of them call any `Save*Data()` method, and the test restores the
     player's original equipment afterward regardless): confirmed `Enemy.DropPool` reads `Weapon,
     Armor, Ring, AbilityItem` on a real `Snake` and the unchanged default `All` on a `Wanderer`; ran
     300 real `Spawn()` calls with Snake's restricted pool (49 gear drops, 0 potion drops — the actual
     gate working, not coincidentally silent) against a control of 300 calls with the default `All`
     pool (61 potion drops — proving the category really is being excluded, not just always empty by
     chance); confirmed `SpawnGuaranteedLoot()` with the default pool still includes a stat potion (no
     boss regression). Clean build and a plain boot-check both passed.
136. **Per-enemy drop pools gained the weighting half — a per-category chance multiplier layered on
     top of entry 135's in/out `DropPool` gate.** New `Enemy.DropWeights` field
     (`Dictionary<ItemSpawner.LootCategory, float>`, defaults to an empty dict — no entry for a
     category means weight 1.0, today's unweighted rate, unchanged for any enemy that doesn't opt
     in), read by `Enemy.SpawnLoot()` and passed into `ItemSpawner.Spawn()`'s new `dropWeights`
     parameter. Two new private `ItemSpawner` helpers apply it: `WeightFor()` looks up a category's
     multiplier (defaulting missing entries to `1f`), and `WeightedChance()` divides the category's
     base chance denominator by that multiplier (floored at 1, since `rand.Next(1)` is always 0, so
     an extreme weight can't produce a non-positive `Next()` argument) — a `rand.Next(N) == 0` roll
     gets more frequent as `N` shrinks, so dividing the denominator by the weight is what makes >1
     roll more often and <1 roll less often. Applied to all 6 of `Spawn()`'s category rolls
     (Weapon/Armor/Ring/AbilityItem share the difficulty-scaled `dropChance`; StatPotion/
     HealthManaPotion keep their own separate base chances of 15/10). Deliberately *not* threaded into
     `SpawnGuaranteedLoot()` — every included category there always contributes once a reachable tier
     exists (that's what makes boss loot "guaranteed"), so a chance multiplier has nothing to act on;
     `Boss.SpawnLoot()` is unchanged. Applied to the backlog's own concrete example:
     `Enemy.CreateBigSnake()` now sets `DropWeights` to `StatPotion`/`HealthManaPotion` at `2.5f` and
     `Weapon`/`Armor`/`Ring`/`AbilityItem` at `0.5f` — leans toward potions without excluding gear
     outright the way Snake's `DropPool` does (BigSnake's own `DropPool` stays the default `All`).
     Verified via a scripted repro (temp code in `Game1.StartGame()`, no save-file risk — same
     reasoning and equip-then-restore technique as entry 135's test): ran 300 real `Spawn()` calls
     with BigSnake's actual weighted pool (114 potion drops, 40 gear drops) against a 300-run control
     at the same `PointValue`/`DropPool` with no weights (57 potions, 73 gear) — potions up, gear down,
     in the direction and rough magnitude the 2.5x/0.5x multipliers predict; confirmed an extreme
     1000x weight still produces a drop rather than throwing or silently breaking (the floor-at-1
     logic actually engages); and re-ran Snake's entry-135 baseline test through the new `dropWeights`
     parameter with its own (empty) `DropWeights` dict, confirming gear-only/no-potions behavior is
     byte-for-byte unaffected by an enemy that never opted in. Clean build and a plain boot-check both
     passed.
137. **Per-enemy drop pools gained direct tier control** — a new `Enemy.DropTierRange`
     (`(int Min, int Max)?`, null by default) bypasses `ItemSpawner`'s `PointValue`/player-tier tier
     math entirely, in favor of a fixed absolute tier range an enemy's own factory picks. Previously
     tier was only ever indirect: a fixed low range for "weak" enemies under a `PointValue` threshold,
     or the player's own current tier plus a random jump for everything else — no enemy could just say
     "always drop tier 3-5 gear" regardless of either. `ItemSpawner.ResolveDropTier()` (used by
     `Spawn()`'s 4 gear/ability categories) now checks the override first, before falling through to
     the existing weak-enemy/player-tier branches unchanged. `SpawnGuaranteedLoot()` needed its own
     parallel path — its existing `ItemsAtBestAvailableTier()` *steps down* from a rolled offset until
     it finds a non-empty tier (built to keep boss loot "guaranteed" even as the player's own tier
     climbs), which doesn't fit an enemy-fixed range at all; new `ItemsAtOverrideTier()` does one
     exact-tier roll within the range and filters the catalog to just that tier, with the same
     graceful-empty behavior as everywhere else in the file if that exact tier happens to have no
     entries (the enemy's own range is expected to have real content, not stepped-around). Both
     `Enemy.SpawnLoot()` and `Boss.SpawnLoot()` thread `DropTierRange` through to their respective
     `ItemSpawner` call. Not yet applied to any concrete enemy — built as the general mechanism the
     user asked for ("can I control what tier of equipment is dropped... build that with min/max tier
     options"), same shape as `DropPool`/`DropWeights` before either was applied to a specific enemy.
     Verified via a scripted repro (temp code in `Game1.StartGame()`, calling `ItemSpawner.Spawn()`/
     `SpawnGuaranteedLoot()` directly rather than through a real enemy since nothing wires
     `DropTierRange` yet — no `Player.Instance` mutation at all, so no save-file risk): a `(3,5)`
     override on `Spawn()` produced 50/50 weapon drops every one of which landed in `[3,5]`; the same
     range on `SpawnGuaranteedLoot()` for Weapon and Armor produced in-range drops every time (30 and
     29 of 30 runs respectively — the 1 miss being the tier-roll's own inherent chance of landing on
     an empty sub-tier, not a bug); a `(3,5)` range against Ring (whose real catalog only spans tiers
     0-1) correctly produced zero drops across 30 runs rather than crashing or falling back to a
     nearby tier, and the same category with its real `(0,1)` range produced drops in most runs,
     confirming the override path also works against a 2-entry catalog; and a `tierRange=null` control
     run under the exact same weighted setup computed tier 15 (this account's own equipped Weapon tier
     plus 1, per the untouched `playerTier + RollTierOffset` formula) — outside `[3,5]` and, since the
     catalog tops out at tier 14, correctly produced zero items rather than silently reusing the
     override branch. Clean build and a plain boot-check both passed.
138. **Per-enemy drop pools gained control over which specific stat potion drops, one level deeper
     than the category system above.** Previously `StatPotion` category rolls (both `Spawn()` and
     `SpawnGuaranteedLoot()`) picked uniformly from a fixed list of 8 stat types
     (Attack/Defense/Dexterity/Life/ManaMax/Speed/Vitality/Wisdom, each duplicated inline as its own
     switch block in both methods) with no way to narrow which specific ones a given enemy could hand
     out. New `Enemy.StatPotionPool` (`List<Potions>`, null by default — today's unrestricted,
     unchanged behavior) is read by both `SpawnLoot()` overrides and passed into a new shared
     `ItemSpawner.RollStatPotion()` helper, which the two previously-duplicated switch blocks were
     both replaced with a single call to. `RollStatPotion()` falls back to the full 8-type list
     (`AllStatPotions`, a new `private static readonly Potions[]`) whenever the pool is null *or*
     empty — an enemy that constructs an empty list isn't treated as "roll from nothing," which would
     otherwise be a silent dead category. `HealthManaPotion` is a separate category (Health/Mana, not
     one of the 8) and is untouched by this — `StatPotionPool` only narrows the `StatPotion` category's
     own roll. Not yet applied to any concrete enemy, same as `DropTierRange` (entry 137) before it —
     built as the general mechanism in response to the user asking "can I control what specific stat
     potions are dropped." Verified via a scripted repro (temp code in `Game1.StartGame()`, calling
     `ItemSpawner` directly with an extreme `StatPotion` weight to force a near-guaranteed roll each
     call, same technique as entry 137 — no `Player.Instance` mutation, no save-file risk): a
     2-type pool (`[Attack, Defense]`) via `Spawn()` produced 50/50 drops, every one either Attack or
     Defense, with both actually represented (not just one dominating by chance); a single-entry pool
     (`[Wisdom]`) produced 30/30 Wisdom drops, ruling out a 1-item list being silently treated as
     "empty = unrestricted"; a `null` pool over 60 runs produced all 8 distinct types, confirming the
     unrestricted fallback path still reaches every type, not silently narrowed by the new plumbing;
     and `SpawnGuaranteedLoot()` with the same 2-type pool produced only Attack/Defense across 30 runs,
     confirming boss guaranteed loot respects the pool identically to the regular table. (Potion
     identity was read via each dropped item's `Name` string — `Potion` has no retained `Potions`-enum
     field after construction, just the per-type `Name`/`ID`/image it's built with — so the test
     checked against the exact `"Attack Potion"`/`"Defense Potion"`/`"Wisdom Potion"` strings
     `Potion.cs`'s constructor assigns per type.) Clean build and a plain boot-check both passed.
139. **Two more drop-table levers, both prompted by the user asking for a worked example: "DEX potion
     100% chance, DEF potion 25% chance, Weapon tiers 7-10, Ring tiers 3-4."** Neither existing
     mechanism could represent that spec, for two different reasons — both fixed.

     First, `Enemy.DropTierRange` (entry 137) was a single enemy-wide `(Min, Max)?` applied to every
     gear category alike, but the spec wants *different* ranges for Weapon (7-10) and Ring (3-4) on
     the same enemy. Generalized to `Enemy.DropTierRanges`, a `Dictionary<LootCategory, (int Min, int
     Max)>` keyed per category — mirrors `DropWeights`' existing per-category shape rather than
     `DropTierRange`'s old single-range one. New `ItemSpawner.TierRangeFor()` looks up one category's
     override out of the map (or returns null, falling through to the normal PointValue/player-tier
     formula for that category, unchanged); `ResolveDropTier()` and `SpawnGuaranteedLoot()`'s 4 gear
     blocks were updated to look up their own category's range instead of sharing one. This replaces
     entry 137's field shape outright — nothing used the old single-range form yet (never applied to a
     concrete enemy), so there was no migration to preserve.

     Second, "DEX 100% + DEF 25%, independently" isn't expressible as a single roll picking one type
     out of a pool (entry 138's `StatPotionPool`/`RollStatPotion()`) — a kill needs to be able to drop
     *both* potions at once, which one categorical roll can never produce. Asked the user directly
     (`AskUserQuestion`) whether the two potions should be independent (a kill could drop 0, 1, or
     both) or a single weighted roll between them (mutually exclusive) — user chose independent. New
     `Enemy.GuaranteedPotionChances` (`Dictionary<Potions, float>`, empty by default) and
     `ItemSpawner.RollGuaranteedPotions()`: for each entry, an independent `rand.NextDouble() <
     chance` roll, appending every potion that succeeds — 0 to N results per call, not exactly 1. Both
     `Spawn()`'s and `SpawnGuaranteedLoot()`'s `StatPotion` blocks now check this first: if non-empty,
     it entirely replaces the old single-roll behavior for that enemy's `StatPotion` category (so
     `DropWeights`' `StatPotion` entry and `StatPotionPool` both stop applying — there's no longer a
     single roll for them to modify); if empty, behavior is byte-for-byte the same single-roll path as
     before. Not yet applied to any concrete enemy — same "build the mechanism, demonstrate it, wire
     it to a real enemy later if wanted" pattern as entries 137/138.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, calling `ItemSpawner` directly
     with the user's exact numbers — no `Player.Instance` mutation, no save-file risk): 100 `Spawn()`
     runs produced Dexterity 100/100 (matches 100% exactly, as expected for a probability of 1.0) and
     Defense 29/100 (close to the target 25% — expected statistical noise, not a bug); at least one
     run dropped both potions together, proving the independence (not mutual exclusivity); every
     Weapon drop landed in `[7,10]` and every Ring drop attempt correctly found nothing (`RingData.
     json`'s real catalog only spans tiers 0-1, so a 3-4 request is a legitimate graceful-empty case,
     not a bug — same behavior already verified in entry 137); and `SpawnGuaranteedLoot()` under the
     same table produced Dexterity 40/40 and Defense 9/40 (~22.5%, also within expected noise of 25%).
     Clean build and a plain boot-check both passed.
140. **Per-enemy drop pools gained a literal absolute drop chance per category, distinct from
     `DropWeights`' multiplier.** `DropWeights` (entry 136) scales a *formula* — the actual resulting
     percentage still depends on the enemy's `PointValue`, since it divides the PointValue-derived
     base chance denominator by the weight. The user asked directly how to get an exact, fixed
     percentage instead ("this category is always 25%, full stop," independent of how tough the
     enemy is). New `Enemy.DropChances` (`Dictionary<LootCategory, float>`, empty by default) supplies
     that: a literal 0.0-1.0 probability, checked via a new `ItemSpawner.RollsCategory()` helper that
     takes priority over both the `PointValue` formula and `DropWeights` entirely when a category has
     an entry, falling back to the existing weighted-formula path unchanged otherwise. Applied to
     `Spawn()`'s 5 chance-gated categories — Weapon/Armor/Ring/AbilityItem (previously each an inline
     `rand.Next(WeightedChance(...)) == 0` expression, now a single `RollsCategory(...)` call) and
     both branches inside the `StatPotion`/`HealthManaPotion` blocks. Deliberately not threaded into
     `SpawnGuaranteedLoot()`, matching `DropWeights`' own precedent — its gear categories are already
     deterministic (no chance roll exists there to override) and it doesn't use `HealthManaPotion` at
     all, so there's nowhere for an absolute chance to plug in. Not yet applied to any concrete enemy.
     Verified via a scripted repro (temp code in `Game1.StartGame()`, calling `ItemSpawner.Spawn()`
     directly at `PointValue = 1` — the weakest possible enemy, where the normal formula would give a
     very low chance — specifically to prove the override truly ignores `PointValue`, not just
     coincidentally agrees with it): a `Weapon = 1.0` / `Armor = 0.0` pair produced exactly 100/100
     Weapon drops and 0/100 Armor drops; a `HealthManaPotion = 0.3` override at the same `PointValue =
     1` landed at 67/200 (~33.5%, in the expected statistical-noise range of 30%, and nowhere near
     `PointValue = 1`'s real formula-driven rate); and the same `Weapon` category with no override at
     all, same `PointValue = 1`, landed at only 7/100 — confirming the fallback path still uses the
     normal low-PointValue formula rather than silently defaulting to something else. Clean build and
     a plain boot-check both passed.
141. **Particle effects — the engine's first visual-effects primitive, and the backlog's "shaders /
     custom visual effects" item's first concrete slice.** Two design decisions resolved via
     `AskUserQuestion` before building anything, per the backlog's own note that this needed both an
     approach and a first target picked, not guessed: (1) a hand-rolled particle system rather than a
     MonoGame `Effect`/shader pipeline — no shader usage exists anywhere in the codebase today, and a
     lightweight sprite-based system fits the existing `AnimatedTexture`-based rendering style with far
     less new surface area; (2) on-hit/on-death particle bursts as the first target (over a portal
     glow) — more frequent on screen, so easier to verify actually works during a real playtest, and
     reusable as a general "something got hit/died" reaction rather than a one-off look.

     New `Particle.cs` (`Particle : Entity`) follows the exact "ephemeral `Entity` managed by the
     normal `EntityManager` pipeline" pattern `DamageNumber` already established — own lifespan
     countdown, `IsExpired` when it runs out, no separate particle-system update/draw pass needed at
     all. Unlike `DamageNumber`, it doesn't even override `Draw()` — the base `Entity.Draw()` already
     handles `image`/`color`/`drawScale`, which is all a particle needs. Uses the existing procedurally-
     generated `Art.Circle` (a 64x64 alpha-masked white circle, already built for something else
     entirely) as its texture, so no new art asset or `Content.mgcb` entry was needed. Each particle's
     `Update()` moves it by its own `velocity` (randomized per-particle via the existing
     `Random.NextVector2(minLength, maxLength)` extension — already in `Extensions.cs`, unused until
     now), applies a `0.9` drag multiplier each frame so a burst decelerates rather than flying outward
     in straight lines forever, and drives both fade (`color = baseColor * progress`) and shrink
     (`drawScale = startScale * progress`) off the same `ticksRemaining / lifespanTicks` progress value
     so a particle visibly shrinks as it fades instead of popping at full size right before
     disappearing. `Particle.SpawnBurst(position, color, count, minSpeed, maxSpeed, lifespanTicks,
     startScale)` is the one entry point every call site uses rather than constructing `Particle`
     directly.

     Hooked into `Enemy.WasShot()` at the two points the user specified: a small white 5-particle burst
     on every non-fatal hit (right alongside the existing `DamageNumber` spawn), and a bigger 14-
     particle orange-red burst on death (alongside the existing loot/portal-drop logic). Both fire for
     every `Enemy` including `Boss` subclasses (`WasShot()` isn't overridden by `Boss`), so boss hits
     and boss deaths get the same effect automatically — not scoped further, since the user's ask was
     "wherever an enemy takes damage or dies." Player hits are explicitly out of scope (the
     `AskUserQuestion` answer scoped this to enemies), left as a natural follow-up.

     Verified via a scripted repro (temp code in `Game1.StartGame()`): `SpawnBurst(count: 5)` added
     exactly 5 entities to `EntityManager`; a real `Enemy.WasShot()` hit added exactly 6 (5 particles +
     1 `DamageNumber`, confirming the actual hook point fires, not just the mechanism in isolation); a
     real death (health reflected to 1, `DropsLoot` reflected to `false` first to eliminate the random
     loot bag as a variable) added exactly 20 (5 hit + 14 death particles + 1 `DamageNumber`); a
     particle ticked through its full lifespan correctly reached `IsExpired = true`, while one only 3
     of 10 ticks in correctly had not; and, per the entry-118 lesson that a numeric-only check can miss
     a real rendering bug, rendered a real burst to an offscreen `RenderTarget2D` through
     `EntityManager.Draw()` and confirmed non-black pixels actually appeared (371 of 40,000), proving
     particles genuinely draw rather than just existing as inert data. One real risk caught during the
     test itself, not the feature: the test's setup step (`new NexusState(...)`, needed to initialize
     `Game1.Camera` before any `Update()`/`Draw()` call, per this session's established precedent) saves
     unconditionally per `CLAUDE.md`'s save-backup rule — no backup was taken first, an oversight. Spot-
     checked the real save files afterward instead: `PlayerData_Wizard.json`/`InventoryData_Wizard.
     json` timestamps confirmed they were touched, but their contents were fully intact, legitimate
     real data (a level 20 Wizard, tier-14 gear, real inventory) — safe because nothing mutated
     `Player.Instance` before the save fired, the same narrow exception `CLAUDE.md` calls out as easy
     to get wrong; flagged here as a reminder to back up before constructing a real state object in any
     future test, not just when a test is known to mutate something. Clean build and a plain boot-check
     both passed.
142. **Particle effect for the player leveling up.** `Player.LevelUp()` (the base implementation every
     class's `Wizard`/`Archer`/`Knight.LevelUp()` override funnels into via `base.LevelUp()`, right
     where the existing `Sound.LevelUp` cue already plays) now also calls `Particle.SpawnBurst()` —
     same entry point entry 141's enemy hit/death effects use, given its own distinct look: gold
     rather than white/orange-red, bigger (20 particles vs. 5/14), and longer-lingering (35 ticks vs.
     15/25) — a celebratory moment reads differently from a combat reaction. Fires for every class
     automatically, since all three subclass overrides funnel through the same base method; no
     per-class change needed. Applying entry 141's lesson from the start this time: backed up the real
     save files (`PlayerData_*.json`/`InventoryData_*.json`/`BankData.json`/`FameData.json`/
     `KeyBindingsData.json`/`GameSettingsData.json`) *before* running the scripted repro, per
     `CLAUDE.md`'s rule — the oversight flagged in that entry, actually applied this time rather than
     just noted for later. Also avoided the risk a different way: rather than calling `LevelUp()` on
     the real `Player.Instance` (already Level 20 on this account, so a real call would push it past 20
     with no way back and no threshold to guard against it), the repro constructed a throwaway
     `Wizard()` instead and called `LevelUp()` on that — never touching the real character at all.
     One caught-and-documented gotcha along the way, not a bug: `Player`'s constructor unconditionally
     does `instance = this` (already known from entry 132), so constructing the throwaway `Wizard`
     silently repoints the static `Player.Instance` at it for the remainder of the process — harmless
     here since nothing else in the test or the real game loop reads `Player.Instance` again before
     the process exits, but worth remembering for any future test that constructs a second `Player`
     subclass instance without meaning to replace the real one. Verified via a scripted repro (temp
     code in `Game1.StartGame()`): the throwaway `Wizard`'s one `LevelUp()` call (Level 1 → 2, safely
     under the Level-20 save threshold so `Util.SavePlayerData()` never fired on this fake instance)
     added exactly 20 entities to `EntityManager`, matching the burst's `count`; confirmed via a byte-
     for-byte diff against the pre-test backup that all 10 real save files were completely untouched
     after the run. Clean build and a plain boot-check both passed.
143. **"Auto-Enter Portals" — a new Settings > Gameplay toggle (default OFF) that bypasses the portal
     confirmation prompt.** New `Player.AutoEnterPortalsEnabled` (persisted via `GameSettingsData`,
     same account-wide shape as `AutoFireEnabled`) is checked at the top of `Portal.Update()`'s
     trigger-bounds branch: when true, the portal calls `EnterPortal()` immediately and clears
     `pendingConfirmation` instead of arming it, the same "call `EnterPortal()`, clear
     `pendingConfirmation`" shape the manual click/keypress confirm paths already use, just triggered
     by proximity instead. Placed *after* the existing Bank-destination special case (which already
     returns early on proximity, never reaching the confirm flow at all), so Bank's own always-been-
     instant open/close behavior is unaffected either way. `SettingsState.cs`'s Gameplay tab — which
     only ever had one non-keybinding toggle (Auto-Fire) — was generalized from dedicated
     `autoFireRect`/`autoFireHover` fields into a small reusable `ToggleRow` class (`Label`/`Rect`/
     `Hover`/`Get`/`Set`, the `Get`/`Set` closures each pointing at whichever `Player.Instance` bool
     that row controls) backing a `List<ToggleRow> gameplayToggles`, now built from two entries
     (Auto-Fire, Auto-Enter Portals) — the same "generalize once a second real instance shows up"
     pattern this session has followed repeatedly (`DropPool`→`DropWeights`, `isSpriteGod`→
     `portalDropOnDeath`, `DropTierRange`→`DropTierRanges`). `Update()`/`Draw()`'s Gameplay-tab
     branches both collapsed from one hardcoded block into a loop over `gameplayToggles`, so a third
     future toggle is just one more list entry, not another copy of the block.

     Verified via a scripted repro (temp code in `Game1.StartGame()`), backed up first per `CLAUDE.md`
     — this one genuinely needed it, since triggering the bypass for real calls `EnterPortal()` →
     `StateManager.Nexus()`, which explicitly calls `Util.SavePlayerData()`/`SaveInventoryData()`/
     `SaveBankData()`/`SaveFameData()` and constructs a real `NexusState`: confirmed a save→toggle→
     load round trip on `AutoEnterPortalsEnabled` (mirroring entry 132's `AutoFireEnabled` test);
     confirmed `SettingsState`'s `gameplayToggles` now holds exactly 2 non-overlapping rows with the
     right labels; positioned a real `Portal` exactly at `Player.Instance.Position` (bounds
     intersecting immediately) and confirmed `AutoEnterPortalsEnabled = true` left `pendingConfirmation`
     null after `Update()` (bypassed, not armed) while `AutoEnterPortalsEnabled = false` against a
     second portal still armed `pendingConfirmation` to it (the pre-existing behavior, unaffected — a
     real regression check, not just a fresh test of new code). One real, if harmless, surprise caught
     by the backup discipline: the triggered `StateManager.Nexus()` call ended up saving
     `PlayerData_Knight.json` rather than `PlayerData_Wizard.json` (the class actually active all
     session) — `Util.SavePlayerData()`'s target file is resolved from the static `Player.PlayerClass`,
     not `Player.Instance` directly, and something (unrelated to this feature — no code touched here
     reads or writes `Player.PlayerClass`/`DetermineLastPlayedClass()`) left it pointed at Knight at
     that moment. The resulting diff was a full field-by-field match against the pre-test backup except
     freshly-regenerated GUIDs on the equipped Weapon/Armor/Ring/Shield objects — cosmetic, since
     entry 42 already established equipped items are re-resolved from the live catalog by Name/Tier on
     every load, never looked up by their saved ID — but restored the exact pre-test bytes from backup
     anyway rather than relying on that reasoning alone, since touching a file this test never meant to
     touch at all warranted the conservative response regardless of how benign the diff looked. Worth
     a closer look later if it recurs, but out of scope for this entry. Clean build and a plain
     boot-check both passed.
144. **Level-up effect replaced with a dedicated sparkle-and-swirl, per the user's explicit request for
     "a dense cluster in the center swirling outward" rather than entry 142's straight-line burst.**
     New `SwirlParticle.cs` (`SwirlParticle : Entity`, a second particle "flavor" alongside
     `Particle.cs`, not a variant of it — the underlying motion model is fundamentally different)
     moves in polar coordinates around a tracked center rather than a fixed velocity: `radius` grows
     every tick (the "swirling outward" from a small `startRadius` — the dense starting cluster) while
     `angle` advances every tick (the actual swirl/rotation), with every particle in one burst sharing
     the same randomly-chosen spin direction so it reads as one coherent swirl rather than particles
     scattering past each other both ways. The "sparkle" half is a fast sine-wave "twinkle" layered on
     top of the overall lifespan fade — both `color`'s alpha and `drawScale` oscillate each tick
     (`0.5 + 0.5*sin(...)`, phase/speed randomized per particle so they don't all twinkle in lockstep),
     floored rather than hitting true zero so a dimming particle still reads as present. Center is a
     `Func<Vector2>` re-evaluated every `Update()`, not a `Vector2` captured once at spawn — the swirl
     keeps following the player if they keep moving during the ~0.8s it plays, which a fixed spawn
     point couldn't do. `SwirlParticle.SpawnSwirl()` also alternates each particle between two supplied
     colors (gold/white here) for a two-tone shimmer, rather than one flat color. `Player.LevelUp()`
     now calls this instead of entry 142's `Particle.SpawnBurst()` — `() => Position` as the center
     delegate, 24 particles, 50-tick lifespan, 60px max radius. Verified via a scripted repro (temp
     code in `Game1.StartGame()`, backed up real save files first as a precaution even though nothing
     in this test constructs a real state or calls a Save method — cheap insurance after entry 143's
     surprise): confirmed `radius` grew from 2 to 26 over 20 ticks (matches the configured
     `radiusGrowth`) and `angle` advanced by exactly 2.0 radians over the same 20 ticks (matches the
     configured `angularSpeed`), together proving the outward-spiral motion is real, not just a static
     offset; confirmed the alpha channel genuinely oscillates tick-to-tick (190→149→115→95→94→109→...,
     a real wave, not a constant) — the twinkle actually doing something, not just present in the
     formula; confirmed moving the center delegate's underlying value mid-flight and calling `Update()`
     again moved the particle to the new center, not the frozen spawn point; confirmed a real
     `LevelUp()` call (throwaway `Wizard`, not `Player.Instance` — same reasoning as entry 142, real
     account already Level 20) added exactly 24 entities; and, per the entry-118/141/142 lesson,
     rendered a real burst to an offscreen `RenderTarget2D` and confirmed non-black pixels actually
     appeared. Confirmed via a post-test diff that all 10 real save files were completely untouched.
     Clean build and a plain boot-check both passed.
145. **Default loot bag art moved to `Content/Items/Bags/brown.png` and renamed from `loot_bag`,
     matching the other 5 bag colors' existing location/naming convention** (`Items/Bags/pink.png`,
     `purple.png`, `blue.png`, `white.png`, `gold.png`) — previously the only one of the 6 still sitting
     at the content root under its old name. `Art.LootBag`'s load line and its `Content.mgcb` build
     block both updated to the new path (`content.Load<Texture2D>("Items/Bags/brown")`, `#begin
     Items/Bags/brown.png` / `/build:Items/Bags/brown.png`) — a repo-wide search confirmed no other
     code referenced the old `loot_bag` name. Verified the new `.xnb` actually compiled at the new path
     (`bin/Debug/net8.0-windows/Content/Items/Bags/brown.xnb`, confirmed present, no stale `loot_bag.xnb`
     left behind) and a plain boot-check passed — since `Art.Load()` loads every texture eagerly at
     startup, a broken content reference here would have crashed the boot immediately rather than
     failing silently later.
146. **Loot bag art now driven by the highest tier of equipment actually dropped, not by whichever
     item category happened to roll last.** Previously `bagTexture` was overwritten sequentially as
     each category's block ran (Weapon→Armor→Ring→AbilityItem→StatPotion), so a bag with both a
     Weapon and a Ring always showed whichever of the two was checked *later* in the fixed order,
     regardless of which was actually the better item — not a real tier signal at all. Two design
     questions resolved via `AskUserQuestion` before touching any code: (1) tie-break rule when a bag
     holds items at different tiers — highest tier present wins, so the bag always reflects the best
     item inside it; (2) exact tier cutoffs/colors — the user supplied a full spec (Brown/Pink/Purple/
     Cyan/Blue/Red/White, each with a description and tier ranges per category).

     New per-category rank functions (`BagRankForWeaponOrArmor`/`BagRankForAbilityItem`/
     `BagRankForRing`, each returning a nullable 0-3 rank — 0=Pink, 1=Purple, 2=Cyan, 3=Red, `null` for
     tier 0/"not even worth a Pink bag") encode the user's cutoffs directly against each category's
     real `Tier` field: Weapon/Armor share one scale (both catalogs run 0-14: 1-6→Pink, 7-9→Purple,
     10-12→Cyan, 13-14→Red); AbilityItem has its own since Spell/Quiver/Shield only run 0-7 (1-2→Pink,
     3-4→Purple, 5-6→Cyan, 7→Red); Ring has its own per the spec's numbers (1→Pink, 2-4→Purple,
     5-6→Cyan, 7→Red) but its real catalog is far shallower than the other three — currently only
     tiers 0-1 exist — so Purple/Cyan/Red are effectively unreachable for rings today; a content gap
     in `RingData.json`'s depth, not a bug in this ranking, and out of scope to fix here. New
     `TrackBestBagRank()` folds each category's own rank into a running best-so-far as `Spawn()`/
     `SpawnGuaranteedLoot()` process every category, so the final bag texture reflects the true highest
     rank across the whole bag, not just the last category checked. `Blue` (existing behavior,
     unchanged) only shows when the bag has no ranked equipment at all — a stat-potion-only bag; the
     final fallback (`Art.LootBag`, brown/"public" per the spec) covers everything else, e.g. a bag
     with only a Health/Mana potion. `SpawnGuaranteedLoot()` (boss loot) was previously hardcoded to
     always show the "premium" Gold bag regardless of what dropped — now uses the same ranking,
     computed off each category's actually-*selected* item (not the originally-targeted tier, since
     `ItemsAtBestAvailableTier`'s step-down search can land on a lower tier than requested). Two colors
     from the user's spec are wired (new `Art.LootBagCyan`/`LootBagRed` fields — both `Items/Bags/
     cyan.png`/`red.png` already had `Content.mgcb` build blocks from when the art was originally
     supplied, just never loaded into `Art.cs` or used anywhere) but the spec's `White` band (rarest
     "untiered" items that "change the way characters are played") was deliberately left unconnected —
     this codebase has no unique/untiered-item concept at all today, so there's nothing for it to
     trigger on; building that would be introducing a whole new item system, well beyond "adjust which
     art gets assigned." `Art.LootBagWhite`/`LootBagGold` both stay loaded (real, valid textures) but
     are now unused by `ItemSpawner.cs` — available for whenever an untiered-item system gets built.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, no save-file risk — the
     tier-1-gear-equip-then-restore setup used to create drop headroom never calls a `Save*Data()`
     method, confirmed via direct code reading in an earlier entry this session): forced `Weapon`
     drops at tiers 0/3/8/11/14 via `DropTierRanges` and confirmed the resulting bag was exactly
     Brown/Pink/Purple/Cyan/Red per the cutoffs; a mixed bag (`DropTierRanges` forcing a tier-14 Weapon
     *and* a tier-1 Ring in the same roll) correctly showed Red — the Weapon's higher rank — with both
     items actually present (2 total), proving the cross-category comparison is real and not just
     coincidentally matching whichever category happens to be checked last; a potion-only bag (via
     `GuaranteedPotionChances`) still showed Blue, confirming that path is unaffected; and
     `SpawnGuaranteedLoot()` with a forced tier-14 Weapon showed Red, not the old hardcoded Gold,
     confirming boss loot now goes through the same ranking. Clean build (new `Art.LootBagCyan`/
     `LootBagRed` textures confirmed already compiled at their `.xnb` paths from a prior asset
     registration) and a plain boot-check both passed.
147. **New Settings > Graphics toggle: "Show Hitboxes," default off.** New `Player.ShowHitboxesEnabled`
     (persisted via `GameSettingsData`, same shape as `AutoFireEnabled`/`AutoEnterPortalsEnabled`)
     shows the F3 debug hitbox outlines (`EntityManager.DrawHitboxes()`) independent of the rest of the
     F3 debug bundle. F3/`Game1._Debug` previously gated two separate things together — the hitbox
     outlines and a separate debug HUD panel (`Overlay.DrawDebug()`, potion-derived stat bonuses) — in
     both `RealmState.Draw()` and `NexusState.Draw()` (which `BossRealmState` inherits, covering boss
     fights too). Rather than splitting hitboxes out from F3 entirely (which would silently change
     existing F3 behavior for anyone already using it), the new setting is additive: both draw sites
     changed from `if (Game1._Debug)` to `if (Game1._Debug || Player.Instance.ShowHitboxesEnabled)`
     around the hitbox call specifically — F3 still shows hitboxes *and* the debug panel together as
     before, while the new setting shows just the hitboxes on their own, persisted across sessions,
     without needing the rest of the debug HUD. `SettingsState.cs`'s Graphics tab — previously sharing
     a flat "No settings here yet." placeholder with Audio, since no graphics option existed anywhere
     in the codebase — split into its own case using entry 143's `ToggleRow` mechanism (a second
     `graphicsToggles` list alongside `gameplayToggles`, same `Get`/`Set`-closure shape), so a future
     Graphics setting is just another list entry; Audio keeps the placeholder alone now, since no audio
     setting exists yet either.

     While testing this feature, discovered `PlayerData_Archer.json`/`PlayerData_Knight.json`/
     `InventoryData_Archer.json`/`InventoryData_Knight.json` were missing from disk entirely (not
     reset, not empty — just gone), despite nothing in any test this session targeting those files
     since entry 143's Knight-save incident. Found a surviving backup from earlier today in a temp
     directory and asked the user before touching anything; the user confirmed this was expected and
     asked to leave it as-is, so nothing was restored — noted here only because it was a real mid-task
     pause, not because anything about it involved this entry's actual code changes.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, backed up real save files first
     since the round trip below genuinely calls `Util.SaveGameSettingsData()`): confirmed a
     save→toggle→load round trip on `ShowHitboxesEnabled` correctly restored the saved value (mirroring
     entries 132/143's same pattern for the other two settings); confirmed `SettingsState`'s
     `graphicsToggles` now holds exactly 1 entry labeled "Show Hitboxes." The `Game1._Debug ||
     Player.Instance.ShowHitboxesEnabled` boolean-OR draw-gate itself was verified by direct code
     reading rather than a render pass — the change is a one-line, low-risk boolean addition mirrored
     identically in both files, re-read after editing to confirm both read correctly, so a full
     render-based test (constructing a real `NexusState`, another unconditional-save risk per
     `CLAUDE.md`) wasn't judged worth the added risk for this specific piece. Confirmed via a post-test
     diff that every real save file was untouched except `GameSettingsData.json`, which gained exactly
     the new field, correctly restored to its original value. Clean build and a plain boot-check both
     passed.
148. **Loot bags now despawn on a timer, with a warning blink that speeds up right before they
     disappear.** New `LootBag.lifespanTicksRemaining` counts down every `Update()` — 60 seconds (3600
     ticks) for every bag color, except Orange/Red/White at 120 seconds, per the user's own spec (Red
     and White sit at the top of entry 146's tier ladder, and Orange — not wired to any drop yet — gets
     the same treatment in case it is later; new `Art.LootBagOrange` field added alongside, since it
     didn't exist until now despite `Items/Bags/orange.png` already having a `Content.mgcb` block).
     Lazily initialized on the bag's first `Update()` call rather than in the constructor: every real
     `LootBag` is built via an object initializer (`new LootBag { image = bagTexture, ... }` in
     `ItemSpawner.cs`) which only assigns `image` *after* the constructor body runs — too early there
     to know which color a given bag actually is. `Add()`/`Remove()`/`Clear()` are untouched, so the
     countdown never resets when items go in or out, per the user's explicit ask.

     In the last 10 seconds before despawn, the bag blinks — alternates between opaque and
     `Color.Transparent` via the same `color` field `Particle`/`SwirlParticle` already use for their
     own fades, driven by an accumulating `blinkPhase` whose per-tick increment (`1 / halfPeriod`)
     grows as the remaining time shrinks (`MathHelper.Lerp` from a 20-tick half-period down to 4), so
     the blink starts slow and visibly speeds up right before the bag vanishes rather than blinking at
     one constant rate for the whole warning window. When the countdown actually reaches 0, the bag
     doesn't just get `IsExpired = true` — it's also explicitly removed from `ItemSpawner.LootBags`
     (mirroring the existing pickup-emptied-the-bag path just below in the same file), since nothing in
     `EntityManager` prunes that separate list on its own and `DrawLoot()` reads it directly rather
     than going through `EntityManager` — without this, a despawned bag would stop drawing via the
     normal entity pass but silently keep rendering its (now-invisible-but-still-technically-there)
     interactive contents via `DrawLoot()` forever.

     Confirmed this applies automatically to manually-dropped items too (`InventorySystem.
     AddToLootBagAtPlayer()`'s `new LootBag { Position = Player.Instance.Position, Items = items }`),
     since it's the exact same class — that object initializer never sets `image`, so it keeps
     whatever the constructor already assigned (`Art.LootBag`, Brown), meaning a manually-dropped item
     always gets the normal 60-second lifespan rather than the 120-second one, and adding a second item
     to that same bag (the "add to an existing nearby bag" branch just above in that method) correctly
     doesn't reset its timer either, since it also just calls the untouched `Add()`.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, no save-file risk — `LootBag`/
     `ItemSpawner` never touch `Player.Instance` persistence): confirmed the lazy-init lifespan reads
     ~3599 (3600 - 1 tick) for Brown/Purple and ~7199 for Red/White right after each bag's first
     `Update()`; confirmed `Add()`/`Remove()` leave the countdown completely unchanged; forced the
     countdown to 1 tick on a bag actually present in `ItemSpawner.LootBags` and confirmed both
     `IsExpired` flipped true *and* the bag was actually removed from that list, not just marked
     expired; counted color toggles over 100 identical ticks at two different countdown positions —
     just inside the 10-second warning window (5 toggles) versus close to despawn (10 toggles) —
     confirming the blink genuinely speeds up rather than just existing at a fixed rate; and confirmed
     a fresh bag well outside the warning window stays fully opaque (`A=255`) the whole time. Clean
     build and a plain boot-check both passed.
149. **Fresh characters no longer start with a Ring equipped, for any class.** All three
     `Wizard`/`Archer`/`Knight` constructors previously had an identical
     `Ring = Ring.LoadRing("Ring of Minor Defense");` line among their other starting-gear equips.
     Removed from all three — `Player()`'s own base constructor already leaves `Ring` as a fresh,
     genuinely-unequipped `new Ring()` (`Equipment.IsEquipped` is just `image != null`, and `Ring`'s
     parameterless constructor never sets `image`), so simply not overriding it in the subclass
     constructors is the entire fix; no new mechanism needed. `RecalculateStats()` (already triggered
     right after the starting-gear block in each constructor) needed no changes either — it reads
     `Ring`'s bonus fields the same way whether or not it's "equipped," and an unequipped `Ring`'s
     bonuses are just their default `0`, identical to what happens whenever a player manually
     unequips their ring during a normal session. `Util.DebugMaxLevelAndEquipTopGear()` (the F3-
     adjacent debug helper that also equips a top-tier Ring) was deliberately left untouched — that's
     a debug utility, not the normal character-creation path this request was about. Verified via a
     scripted repro (temp code in `Game1.StartGame()`, throwaway `Wizard`/`Archer`/`Knight` instances,
     not `Player.Instance` — no save-file risk): confirmed all three classes' fresh `Ring.IsEquipped`
     reads `False`, and that `RecalculateStats()` still produces sane, nonzero derived stats
     (`HealthMax`/`Attack` etc.) with no exception. Clean build and a plain boot-check both passed.
150. **Low-health flash — the player sprite flashes red under 25% Health, speeding up the lower it
     gets, gated by a new Settings > Graphics > "Low Health Indicator" toggle (default ON).** Same
     accumulating-`blinkPhase`/`MathHelper.Lerp` shape as entry 148's loot-bag despawn blink: a
     `progress` value (0 right at the 25% threshold, 1 at 0 HP) drives `halfPeriod` from a slow 20-tick
     value down to a fast 5-tick one, and `lowHealthFlashPhase` accumulates `1 / halfPeriod` every
     tick in `Player.Update()`, toggling `color` between `White` and `Red` each time it crosses an
     integer. Leaving the danger zone (Health back at/above 25%, or the setting disabled) resets both
     `color` to `White` and the phase to `0`, so re-entering later always starts the same slow blink
     rather than resuming wherever a stale phase left off. This is the first `GameSettingsData`-backed
     toggle in the session to default **on** rather than off (every prior one — Auto-Fire, Auto-Enter
     Portals, Show Hitboxes — defaults off); handled by giving `GameSettingsData.
     LowHealthIndicatorEnabled` its own explicit `= true` property initializer, not just the
     `Player.cs` field — `System.Text.Json` only overwrites properties actually present in the source
     JSON, so an existing `GameSettingsData.json` saved before this field existed would otherwise
     deserialize this property at its *unstated* default (`false` for a bare `bool`), silently turning
     the indicator off for every account that already has a settings file, not just leaving it at the
     intended on-by-default. `SettingsState.cs`'s Graphics tab gained a second `ToggleRow` entry
     alongside entry 147's "Show Hitboxes," no other changes needed there since that list was already
     built to take more entries.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, throwaway `Wizard`, not
     `Player.Instance` — no save-file risk; `Game1.Camera` initialized directly via `new
     Camera(...)` rather than constructing a real `NexusState`/`RealmState` to satisfy `Player.
     Update()`'s `Game1.Camera.Pos = Position` dependency, avoiding entry 143's unconditional-save
     surprise entirely this time): confirmed Health at 30/100 (above the 25% threshold) never flashed
     red across 60 ticks; confirmed Health pinned at 24/100 flashed 4 times over 100 ticks while Health
     pinned at 2/100 flashed 16 times over the same span, confirming the speed-up is real, not just
     present in the formula (health was pinned each tick in both cases — an earlier version of this
     same check let real health regeneration run alongside the flash and push Health back above the
     threshold within the loop before a single slow blink cycle could complete, reading as "0 toggles"
     — not a code bug, a test-isolation mistake, fixed by re-setting `Health` every iteration rather
     than once before the loop); confirmed disabling the setting suppressed the flash even at 1 HP;
     confirmed deserializing a literal old-shaped JSON string missing the new key still produced `True`
     for `LowHealthIndicatorEnabled`, directly proving the upgrade scenario the explicit `= true`
     initializer exists for; and confirmed `SettingsState`'s `graphicsToggles` now holds exactly 2
     entries. Clean build and a plain boot-check both passed.
151. **A below-sprite player health bar that only appears at low health, and the hardcoded 25%
     threshold replaced with a new "Low Health Threshold" (0-100, default 25) Settings > Graphics
     setting shared by both it and entry 150's flash.** New `Player.LowHealthThresholdPercent`
     (persisted via `GameSettingsData`, same "explicit default matters for old JSON files" reasoning
     as `LowHealthIndicatorEnabled` — an old save missing this int key would otherwise silently
     deserialize to `0`, which makes `Health < HealthMax * 0%` never true and disables the whole
     feature rather than leaving it at the intended 25) replaces the flash's old hardcoded
     `LowHealthThresholdFraction` constant. New private `Player.IsLowHealth` property
     (`LowHealthIndicatorEnabled && HealthMax > 0 && Health < HealthMax * (LowHealthThresholdPercent /
     100f)`) is the single shared condition both the flash (`Update()`) and the new
     `DrawLowHealthBar()` (`Draw()`) check, so the two can never drift out of sync with each other or
     with the setting. The bar itself reuses `Overlay.cs`'s own "stretched `Art.HealthBar` rect"
     technique (a 1x1 pixel texture sized via the source rectangle rather than sampled pixel content)
     — a small dark-red background behind a brighter red fill proportional to `Health/HealthMax` (not
     to the threshold, so a fuller bar reads as "closer to the threshold" and an emptier one as "closer
     to death," like any other health bar), centered beneath the sprite in world space so it scrolls
     with the camera like the player does.

     `SettingsState.cs` gained a new `NumericRow` class alongside the existing `ToggleRow` — a
     different shape was needed since a 0-100 range doesn't fit a single-click on/off flip: two small
     "-"/"+" hit-rects (`DecrementRect`/`IncrementRect`) adjusting the value by a configurable `Step`
     (5 here) and clamping to `Min`/`Max`, versus `ToggleRow`'s one whole-row click. Lives in a new
     `graphicsNumerics` list, positioned directly after `graphicsToggles`' rows in the same column so
     the two lists read as one continuous stack on the Graphics tab despite being separately typed.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, backed up real save files first
     since the round trip genuinely calls `Util.SaveGameSettingsData()`): confirmed a save→toggle→load
     round trip on `LowHealthThresholdPercent` correctly restored the saved value; confirmed
     deserializing a literal old-shaped JSON string missing the key still produced `25`, not `0`;
     confirmed changing the threshold actually changes `IsLowHealth`'s result at a fixed `Health` value
     (45/100 read as low health at a 50% threshold but not at a 10% threshold, proving the setting
     genuinely drives the condition rather than a leftover hardcoded 25% still lurking somewhere); and,
     per the entry-118 lesson, rendered two real passes — the `SettingsState` Graphics tab (saved to a
     PNG and visually inspected: "Low Health Threshold  -  10%  +" renders cleanly, correctly aligned
     in the same value column as the toggles above it, no overlap or clipping) and the player itself at
     low HP (confirmed real red-ish pixels appear below the sprite) versus above the threshold
     (confirmed zero red-ish pixels — the bar correctly doesn't render at all when not needed). Clean
     build and a plain boot-check both passed.
152. **A real Audio tab — Music (on/off, default on), Music Volume, Music Mute, Sound Effects Volume,
     Sound Effects Mute, and a separate Mute Weapon Shots that only silences the player's own basic-
     attack sound.** Previously Audio shared a flat "No settings here yet." placeholder with Graphics,
     since the game had exactly one global on/off (`Game1.Mute`, still bound to the M key via
     `KeyBindings.Action.ToggleMute`) covering music and every sound effect together, with no volume
     concept at all. `Game1.Mute` stays the master override, unchanged — every new setting below only
     applies once it isn't muted globally, so the existing M-key behavior can't regress.

     New `Sound.RefreshMusicState()` is the one place that reconciles "should the track be playing at
     all" (`!Game1.Mute && Player.Instance.MusicEnabled`) from "how loud, if so"
     (`MusicMuted ? 0 : MusicVolumePercent / 100f`) — `Enabled` actually starts/pauses the
     `SongInstance`, while `Muted` just silences the volume without stopping playback (a quick,
     resumable "shut it up" versus a real "don't play music" preference) — called from `Sound.
     PlaySong()` (`RealmState`'s constructor, unchanged call site, now just delegates here), `Sound.
     ToggleMute()` (so M still immediately affects music), and from each of the three Music-related
     `SettingsState` rows' own `Set` closures, so a change is heard on the currently-playing track
     immediately rather than only on the next dungeon entry. New `Sound.ShouldPlaySfx(SoundEffect)`
     gates every other `Sound.Play()` call: `Game1.Mute` first (unchanged), then `Player.Instance.
     SfxMuted`, then — only for `sound == MagicShoot` specifically (the one sound `Weapon.Shoot()`
     plays for every class's basic attack) — `Player.Instance.WeaponShotsMuted`. `SfxVolumePercent`
     multiplies the *existing* per-call volume every `Sound.Play()` site already passes (e.g.
     `Sound.Play(Sound.MagicShoot, 0.3f)`), rather than replacing it, so 100% (the default) preserves
     every sound's current hand-tuned level exactly, and turning it down scales all of them together.

     `SettingsState.cs`'s separate `ToggleRow`/`NumericRow` classes and their three separate per-tab
     lists (`gameplayToggles`/`graphicsToggles`/`graphicsNumerics`) were consolidated into one unified
     `SettingsRow` (a `RowKind.Toggle`-or-`RowKind.Numeric` discriminated class) backing one list per
     tab (`gameplayRows`/`graphicsRows`/`audioRows`) — needed because the Audio tab's 6 settings only
     read naturally in a specific interleaved order (Music, Music Volume, Music Mute, Sound Effects
     Volume, Sound Effects Mute, Mute Weapon Shots), which two separate toggles-then-numerics lists
     could never produce; two toggles and two numerics can now sit in any order within one list.
     `Update()`/`Draw()`'s three near-duplicate per-tab blocks collapsed into shared `UpdateRows()`/
     `DrawRows()` helpers taking whichever tab's list is active, so all three tabs (and any future one)
     share the exact same input/render logic instead of each maintaining its own copy. Also fixed a
     layout constant while building this: the numeric stepper's `StepperValueGap` (`50`) was sized
     around "100%" without real margin — caught via the entry-118 render-and-inspect step, where
     "Sound Effects Volume"'s `100%` visibly crowded its `+` button while `Music Volume`'s shorter
     `40%` had room to spare; widened to `70` and re-rendered to confirm both now match.

     Verified via a scripted repro (temp code in `Game1.StartGame()`, backed up real save files first
     since it genuinely calls `Util.SaveGameSettingsData()`): confirmed a save→toggle→load round trip
     across all 6 new fields; confirmed deserializing an old-shaped JSON missing the audio keys still
     produced the real intended defaults (`MusicEnabled=True`, `MusicVolumePercent=25`,
     `SfxVolumePercent=100`) rather than a bare bool/int's unstated `false`/`0`; confirmed `Sound.Play()`
     with `WeaponShotsMuted=true` still played a non-weapon sound (`Sound.Blip`) while skipping
     `MagicShoot` specifically, and that `SfxMuted=true` skipped everything, both with no exception;
     confirmed `SettingsState`'s three row lists now hold exactly 2/3/6 entries with the Audio list in
     the intended order; and rendered the Audio tab to a PNG, visually confirming (after the
     `StepperValueGap` fix above) all 6 rows read cleanly with no overlap. One thing this pass could
     *not* verify: `RefreshMusicState()`'s actual `SongInstance.Play()`/`.Pause()` transitions — even
     a direct, unwrapped `SongInstance.Play()` call left `SongInstance.State` reading `Stopped`
     afterward with no exception thrown, in this minimized/automated boot-check environment specifically
     (most likely no audio playback device available to a backgrounded process, not a logic bug — the
     `Volume` side of the same calls set exactly the expected values in every scenario, which doesn't
     depend on real hardware). Same category as entry 36's Space-key-in-Nexus check: confirmed by code
     inspection and the parts that *are* mechanically verifiable, but real playback needs the user to
     confirm in a live, focused game window. Clean build and a plain boot-check both passed.
153. **New weapon type: Staff.** Wizard now wields a Staff instead of a Wand — Wand stays fully in
     the game, unused by any class yet, reserved for a future Necromancer/Mystic-style class per the
     user's explicit intent. `Weapon.WeaponType` gained a third case, `Staff`; `WeaponData`/
     `WeaponData.json` gained two new float fields, `Amplitude` and `Frequency`, alongside 15 new
     tier-0-through-14 Staff catalog entries (`Gnarled Staff` through its top tier), each with
     `ProjectileMagnitude=9.6` (18 tiles/sec), `ProjectileDuration=29` (8.55-tile range), `Amplitude=16`
     (0.5 tiles), `Frequency=2`, and the same DamageMin/DamageMax progression as the corresponding Wand
     tier — per spec, "other staves are the same unless explicitly stated otherwise," and no tier-
     specific exception was given. Weapon icon art (`Content/Weapons/Staves/0.png`–`14.png`) and
     dedicated per-tier projectile art (`Content/Weapons/Staves/Projectiles/0.png`–`14.png`, distinct
     colored bolt sprites per tier) were both already supplied by the user; each Staff tier's
     `ProjectileImageName` points at its own `Weapons/Staves/Projectiles/{tier}` rather than reusing
     Wand's shared generic pool (`Projectiles/red_fire`, `Projectiles/blue_magic`, etc.) — confirmed
     with the user directly after noticing the dedicated art sitting unused on disk, since defaulting
     to the shared pool (matching how `DamageMin`/`DamageMax`/everything else was copied tier-for-tier
     from Wand) would have silently ignored real, purpose-made content. `Content.mgcb` gained 30 new
     `#begin`/`/build` blocks total: 15 for the weapon icons, 15 for the dedicated projectile art.

     New `SineWaveProjectile : Projectile` overrides `Update()` completely rather than extending the
     base class's velocity-accumulation approach — position is recomputed fresh every tick as
     `origin + forward * distanceTraveled + perpendicular * (amplitude * sin(2π * frequency * progress
     + phaseOffset))`, avoiding double-counting forward motion between the base class's own approach
     and a second perpendicular term. `Weapon.Shoot()` gained an early `WeaponType.Staff` branch (before
     the existing default-shot/Bow branches, which are otherwise untouched) that spawns exactly two
     `SineWaveProjectile`s at a 0-degree arc gap (`Weapon.Amplitude` is documented as 0 for Staff, so
     both shots share the same aim angle) but opposite phase offsets (`0` and `π`), so the two bolts
     visibly weave apart from each other rather than overlapping. `ExpiresOnHit` for Staff shots follows
     the existing `expiresOnHit = this.Type != WeaponType.Wand` rule unchanged — Staff being anything-
     but-Wand already satisfies "Staff shots do not pass through targets" with no extra code needed.

     One real-save-data risk was caught and resolved before writing any code: an existing save with a
     Wizard whose `WeaponType` was `Wand` (the old default) would silently lose its equipped weapon on
     next load once `Wizard.cs` switched to `WeaponType.Staff`, since `Weapon.LoadWeapon()`'s type-
     mismatch path is silent, not an error. Asked the user directly rather than guessing; they chose to
     accept the reset over an auto-migration to an equivalent-tier Staff — their live Wizard save's
     equipped Wand resets to the starting tier-0 Staff on next load, by their own explicit choice.

     Verified via a two-part scripted repro in `Game1.StartGame()` (real save files backed up first,
     verified byte-identical afterward). Part 1 exercised `SineWaveProjectile` in isolation: forward
     distance after 29 ticks landed at 278.4001 against an expected 278.4 (9.6 px/tick × 29); the
     perpendicular Y range came out to [-15.976535, 15.976535] against an expected amplitude of ±16,
     with 4 zero crossings confirming a real 2-cycle oscillation rather than a single displacement;
     `IsExpired` correctly flipped `true` at exactly `Duration` ticks; and phase `0` vs `π` after one
     tick produced opposite-signed Y values (6.718226 vs -6.7182274) with equal X (9.6 vs 9.6),
     confirming the two shots weave apart symmetrically rather than one just lagging the other. Part 2
     exercised the full integration path: confirmed exactly 15 Staff catalog entries with tier 0 reading
     `Amplitude=16, Frequency=2, Magnitude=9.6, Duration=29` as expected; constructed a throwaway
     `Wizard` and confirmed `WeaponType=Staff` and its starting weapon is `Gnarled Staff`/`Type=Staff`/
     `IsEquipped=True`; called `Weapon.Shoot()` on it and confirmed exactly 2 entities were added (not
     1 like Wand, not 3 like Bow), both real `SineWaveProjectile` instances, both `ExpiresOnHit=True`;
     and advanced both one tick each, confirming X stayed aligned (41.61687 vs 41.583065) while Y had
     already diverged (6.694053 vs -6.7423573) — proving the weave is real end-to-end, not just in the
     isolated Part 1 test. Clean build and a plain boot-check both passed.
154. **Bow reworked into independent Main/Side shots, and moved out of WeaponData.json into its own
     catalog file/loader.** Previously Bow fired three identical `Projectile`s sharing one damage
     range, one piece of art, and a hardcoded ±0.35 rad spread. Per spec: all 3 shots now pierce
     (`ExpiresOnHit = false`, reusing the same `HitBy`-tracking pass-through mechanism Wand bolts
     already used — no new mechanism needed there), the 2 Side shots additionally ignore the target's
     Defense entirely, Main and Side each get their own damage range and projectile art, and the Side
     shots' angle is data-driven (`ArcGapDegrees = 7`, each side offset ±7° from the aim line — 14°
     apart from each other) rather than the old hardcoded constant. `ProjectileMagnitude`/
     `ProjectileDuration` (8.533333 px/tick / 26 ticks) were derived from the spec's 16 tiles/sec and
     0.44s lifetime using this session's established 32px/tile, 60-ticks/sec conversion — the 26-tick
     rounding (from 26.4) puts the real in-engine range about 1.6% under the spec's stated 7.04 tiles,
     an unavoidable artifact of `ProjectileDuration` being an integer tick count. The spec's separate
     "True Range: 4.07 tiles" line for the Side shots doesn't correspond to any mechanic this engine
     has — collision is a plain circle-circle check against the projectile's live position each frame,
     with no concept of a shot's sideways drift from the aim line vs. a target's hitbox width — so it
     was treated as descriptive reference info rather than something to implement; the concrete,
     implementable numbers (speed, duration, angle, piercing, defense-ignore, per-shot damage) are
     all real data now.

     New `Projectile.IgnoresDefense` (mirrors `StunsOnHit`'s/`SlowsOnHit`'s existing shape) flows
     through `EntityManager.HandleCollisions()` into a new `Enemy.WasShot(int damage, bool
     ignoresDefense = false)` overload parameter, which skips the `- Defense` term entirely when set.
     `Weapon` gained `SideDamageMin`/`SideDamageMax`/`SideProjectileImage`/`SideProjectileImageName`/
     `ArcGapDegrees`, alongside the existing `DamageMin`/`DamageMax`/`ProjectileImage`/
     `ProjectileImageName` now read as "Main" for Bow specifically. New `Data/BowData.cs` (a leaner
     DTO — no `Type` field, since every entry is unambiguously `WeaponType.Bow`) and `Data/BowData.json`
     (the 15 Bow tiers, moved out of `WeaponData.json` verbatim aside from the field restructuring)
     are loaded by a new `Util.LoadBowData()`, mirroring `LoadWeaponData()`; `Game1.StartGame()` merges
     its result into the same combined `Weapons` list (`Weapons.AddRange(Util.LoadBowData())`), so
     `Weapon.LoadWeapon()`'s by-name search and `Player.cs`'s `EquipHighestTierWeapon()` (F4) both keep
     working unmodified aside from also copying the 5 new fields — a deliberate fix-it-this-time repeat
     of entry 45's F4 lesson, done proactively rather than waiting for a bug report.

     Tier art: the user renamed `Content/Weapons/Bows/Shortbow.png` to `0.png` conceptually but hadn't
     actually done it on disk yet, so the rename was completed here (`git mv`) to match. New
     `Content/Weapons/Bows/Projectiles/Main/` (art for tiers 0, 7-14 — tiers 1-6 fall back to tier 0's
     art, per spec) and `.../Side/` (art for tiers 0-2, 4-5, 7-14 — tiers 3 and 6 both fall back all the
     way to tier 0's art specifically, not their nearer lower neighbor, confirmed with the user directly
     since the literal "next lowest available" rule and the user's own worked example genuinely
     disagreed on tier 3/6's result). The fallback is baked directly into each tier's
     `MainProjectileImageName`/`SideProjectileImageName` in `BowData.json` — no runtime fallback logic
     needed, same technique as every other weapon type's per-tier art path. 30 new `Content.mgcb`
     blocks (9 Main + 13 Side + the renamed icon). Side damage was set to a provisional 60% of Main's
     range per tier (rounded to the nearest 5) as a starting balance point, noted here as tunable —
     the user asked for the fields to exist, not for specific numbers.

     Verified via a scripted repro (real save files backed up first; throwaway `Archer` instance, so
     `Player.Instance` briefly points at it but nothing persists): confirmed 15 Bow catalog entries;
     Shortbow's Main 25-40 / Side 15-25 damage, `ProjectileMagnitude=8.533333`, `ProjectileDuration=26`,
     `ArcGapDegrees=7`, and a non-null `SideProjectileImage`; `Weapon.Shoot()` added exactly 3
     `Projectile`s, with the Main one `ExpiresOnHit=False`/`IgnoresDefense=False` and both Side ones
     `ExpiresOnHit=False`/`IgnoresDefense=True`; the two Side shots' angles measured 0.12217313 rad off
     Main's (expected `MathHelper.ToRadians(7)` = 0.12217305) with matching speeds; a `Defense=10` test
     enemy (`CreateBigSnake`) lost exactly 40 HP from 50 raw damage with `ignoresDefense=false` (50-10)
     and exactly 50 HP with `ignoresDefense=true` (Defense skipped entirely); and F4's
     `DebugMaxLevelAndEquipTopGear()` on the same throwaway Archer correctly equipped the tier-14 Bow
     with nonzero `SideDamageMin`/`SideDamageMax`, `ArcGapDegrees=7`, and a non-null
     `SideProjectileImage` — confirming the proactive F4 fix actually works, not just compiles. Real
     save files confirmed byte-identical before and after. Clean build and a plain boot-check (which
     exercises `LoadBowData()`'s real content-loading path unconditionally, regardless of which class
     was last played) both passed with no stderr output.
155. **Follow-up to entry 154 — Bow's `ArcGapDegrees` widened from 7 to 14 for all 15 tiers**, doubling
     the Side shots' spread from 14° apart to 28° apart. Pure `Data/BowData.json` data change, no code
     touched — `ArcGapDegrees` was already used directly as each Side shot's own offset from center
     (`±ArcGapDegrees`), so raising the value alone widens the fan; confirmed this was the intended
     reading (as opposed to keeping the same 14° spread and treating the new value as the *total* gap,
     which would have needed halving it in code) before editing, since the two readings produce
     opposite in-game results. Clean build and a plain boot-check both passed.
156. **Quiver ability reworked to fire its own independent, tier-scaled shot fan instead of borrowing
     the equipped Bow's projectile stats.** Previously `Archer.UseAbility()` fired exactly one
     projectile using `Weapon.ProjectileMagnitude`/`ProjectileDuration`/`ProjectileImage` (the equipped
     Bow's own basic-attack stats) with the Quiver contributing only its damage range. Per spec, the
     Quiver now has its own `Shots` (2 for T0-2, 3 for T3-5, 4 for T6-7), `ArcGapDegrees` (7, uniform),
     `ProjectileMagnitude`/`ProjectileDuration` (15 tiles/sec / 1s lifetime → 8.0 px/tick / 60 ticks,
     both exact with no rounding this time — 8.0×60=480px=15 tiles on the nose), and its own dedicated
     `ProjectileImageName` art, all moved onto `Data/QuiverData.cs`/`QuiverData.json` and mirrored onto
     `Quiver.cs` — the existing `MinDamage`/`MaxDamage`/`ManaCost`/stat-bonus fields per tier already
     matched the spec exactly and needed no changes. `Paralyzed for 3 seconds` was already exactly
     right (`Enemy.Paralyze(int durationFrames = 180)` = 180/60 = 3s, unchanged); `Piercing Shots hit
     multiple targets` was already right too (`ExpiresOnHit = false`, unchanged). `Shots pass through
     obstacles` doesn't correspond to anything this engine has — there's no tile/wall collision system
     for projectiles at all (only the player's position gets clamped to a boss arena's bounds; nothing
     ever blocks a projectile) — so it's vacuously already true and needed no code, same category as
     entry 154's "True Range" line.

     `UseAbility()` now loops `Shots` times, firing a symmetric fan where each adjacent pair sits
     `ArcGapDegrees` apart (`angle = aimAngle + (i - (Shots-1)/2f) * arcGapRad`) — an odd Shots count
     centers one shot exactly on the aim line, an even count straddles it evenly with no shot dead
     center. This is a different structure from Bow's Main+Side split (entry 154/155): Quiver has no
     distinguished "center" shot, so unlike `ArcGapDegrees`'s Bow meaning (each Side shot's own offset
     from center), here it directly means the gap between adjacent shots in the fan — verified this
     produces the right total spread (e.g. Tier 0's `Shots=2` → the two shots measured exactly 7° apart
     from each other, matching "arc gap: 7°" as the full gap between them, not each shot's individual
     offset). New art: `Content/AbilityItems/Quivers/Projectiles/{0,2,3,4,5,6,7}.png` (Tier 1 has no
     art of its own and falls back to Tier 0's, confirmed by the user — the same single-gap fallback
     pattern as Bow's Main projectile art, no ambiguity this time since only one tier was missing).
     7 new `Content.mgcb` blocks. `Player.cs`'s `EquipHighestTierAbilityItem()` (F4) got a proactive
     Quiver-specific branch to copy the 5 new fields — applying the entry 45/154 lesson before a bug
     report this time rather than after.

     Verified via a scripted repro (real save files backed up first; throwaway `Archer`, so
     `Player.Instance` briefly points at it but nothing persists): confirmed all 8 catalog entries'
     `Shots`/`ArcGapDegrees`/`ProjectileMagnitude`/`ProjectileDuration`/`ProjectileImageName` (including
     Tier 1's fallback to Tier 0's art); the starting Worn Quiver read `Shots=2`/`ArcGapDegrees=7` with
     a non-null `ProjectileImage`; calling `UseAbility()` spent exactly 45 mana and added exactly 2
     `Projectile`s, both `ExpiresOnHit=False`/`ParalyzesOnHit=True`/`Duration=60`, measured exactly
     0.12217307 rad (~7°, matching `MathHelper.ToRadians(7)` = 0.12217305) apart from each other;
     Tier 3 and Tier 6 read `Shots=3`/`Shots=4` straight from the catalog; and F4's
     `DebugMaxLevelAndEquipTopGear()` correctly equipped the tier-7 Quiver with `Shots=4`,
     `ArcGapDegrees=7`, and a non-null `ProjectileImage` — confirming the proactive F4 fix works.
     (One diagnostic line in the test script itself compared each shot's angle against a naively-computed
     expected aim angle and printed a mismatch — traced to the test's own math, not game code: it
     compared against a raw `Vector2(1000, 0)` direction instead of running it through the same
     camera-transform path `Input.GetMouseAimDirection()` actually uses. The shot-to-shot gap
     measurement, which doesn't depend on that, was exact, and is what actually matters.) Real save
     files confirmed byte-identical before and after. Clean build and a plain boot-check both passed.
157. **A floating "+XP" number above the player's own head on an enemy kill, matching the look of
     damage numbers.** `DamageNumber` (already reused for both enemy-hit and player-hit numbers, see
     entries 22/`Player.Hit()`) gained an optional `prefix` parameter (`""` by default — every existing
     call site is unaffected) so a number can read as a gain ("+15") instead of a hit ("15") while
     sharing the exact same class, float-up/fade-out animation, and font. `Enemy.WasShot()`'s death
     branch — right where `Player.Instance.ExperienceTotal += PointValue;` already runs — now also
     spawns one `new DamageNumber(Player.Instance.Position, PointValue, Color.Goldenrod, prefix: "+")`.
     Deliberately spawned at the *player's* position, not the enemy's that just died (the existing hit
     number already covers that spot) — matching the user's explicit "above the player head" ask.
     `Color.Goldenrod` reuses the exact color `Overlay.DrawExperience()` already fills the sidebar's XP
     bar with, so the floating number visibly reads as "that" resource rather than an arbitrary new
     color.

     Verified via a scripted repro (throwaway `Wizard` positioned away from a throwaway
     `Enemy.CreateWanderer()`, health forced to 1 via reflection, killed with one `WasShot(9999)` call):
     confirmed exactly 2 `DamageNumber`s existed afterward — one reading `"9999"` positioned at the
     enemy's location (the existing hit number, unaffected), and one reading `"+15"` (Wanderer's
     `PointValue`) positioned at the *player's* location with `Color.Goldenrod` (`R:218 G:165 B:32`);
     and confirmed `ExperienceTotal` actually increased by 15. Clean build and a plain boot-check both
     passed.
158. **Follow-up to entry 157 — the +XP number is now bigger, stays on screen longer, and spawns further
     above the player's head**, per direct user feedback after it shipped. `DamageNumber`'s `Scale`/
     `LifespanTicks`/the constructor's hardcoded `-20` vertical spawn offset were all `const`s shared
     by every instance — turned into constructor parameters (`scale`/`lifespanTicks`/`verticalOffset`)
     defaulting to the exact same values (`1.0f`/`40`/`-20f`), so every existing call site (a plain
     enemy hit, `Player.Hit()`) is completely unaffected. The XP call in `Enemy.WasShot()` now passes
     `scale: 1.3f, lifespanTicks: 70, verticalOffset: -45f` — noticeably bigger, ~75% longer on screen,
     and spawning more than double as far above the player as a regular hit number, so it reads as a
     distinct, more prominent event rather than blending in.

     Verified via a scripted repro (throwaway `Wizard`/`Enemy.CreateWanderer()`, same setup as entry
     157): killed the test enemy and reflected into both resulting `DamageNumber`s' now-instance `scale`/
     `lifespanTicks` fields plus their actual spawn position relative to their own origin (enemy for the
     hit number, player for the XP number, not the same point) — confirmed the hit number read exactly
     `scale=1, lifespanTicks=40, 20px above its origin` (unchanged from before this entry) while the XP
     number read exactly `scale=1.3, lifespanTicks=70, 45px above its origin` (the new values). Real save
     files confirmed byte-identical before and after. Clean build and a plain boot-check both passed.
159. **Enemy hit numbers changed from Yellow to Red.** `Enemy.WasShot()`'s `DamageNumber` call — the
     number that appears over an enemy when the player damages it — now uses `Color.Red` instead of
     `Color.Yellow`. Now the same color as the player's own "I took damage" number
     (`Player.Hit()`), which already used Red with a black backing (`hasBlackBacking: true`) — the two
     stay visually distinct via that backing (enemy hits are plain red text, the player's own damage
     has the extra black outline), so no collision in readability despite sharing a color. Verified via
     a scripted repro (throwaway `Enemy.CreateWanderer()`, hit once): confirmed the resulting
     `DamageNumber`'s color read exactly `Color.Red`. Real save files confirmed byte-identical before
     and after. Clean build and a plain boot-check both passed.
160. **New Settings > Graphics toggle: "Show XP Drops," default on.** Gates the floating "+XP" number
     from entries 157/158 — `Enemy.WasShot()`'s death branch now only spawns that `DamageNumber` when
     `Player.ShowXpDropsEnabled` is true; `ExperienceTotal` itself still increments unconditionally
     either way, this only controls the number's visibility. New `Player.ShowXpDropsEnabled = true`
     field and matching `GameSettingsData.ShowXpDropsEnabled { get; set; } = true` DTO property — both
     need the explicit `= true` (not just documentation), same reasoning as
     `LowHealthIndicatorEnabled`'s existing comment: `System.Text.Json` only overwrites properties
     actually present in the JSON, so an existing `GameSettingsData.json` predating this field would
     otherwise deserialize it at the unstated bare-bool default (`false`), silently turning the setting
     off for every existing account instead of leaving it on. `Util.Save/LoadGameSettingsData()` and a
     new `SettingsRow` (`RowKind.Toggle`) in `SettingsState.cs`'s Graphics tab, right after "Low Health
     Threshold," complete the wiring — same shape as every other on/off setting added this session.

     Verified via a scripted repro (real `GameSettingsData.json` backed up first since this genuinely
     calls `Util.SaveGameSettingsData()`, restored immediately after): confirmed a fresh `Player`
     defaults to `ShowXpDropsEnabled = true`; confirmed deserializing an old-shaped JSON missing the key
     still produces `true`, not the bare-bool default; confirmed a real save(`false`)→load() round trip
     actually flips the in-memory value; confirmed the gate itself — with the setting off, killing a
     test enemy added no floating number above the player (only the existing hit number at the enemy),
     and with it on, exactly one appeared. Per the entry-118 lesson (a numeric check alone missed a real
     stepper-spacing bug once), also rendered the Graphics tab itself to a PNG and visually confirmed
     "Show XP Drops" reads "ON," aligned in the same value column as the other three rows, no overlap.
     Real save files confirmed byte-identical before and after (including `GameSettingsData.json`,
     restored to its exact original bytes despite the round-trip test genuinely rewriting it mid-test).
     Clean build and a plain boot-check both passed.
161. **The player's own damage-taken number and the XP gain number now follow the player as they move,
     instead of being left behind in empty space.** Previously every `DamageNumber` only applied a fixed
     upward `FloatVelocity` each tick from a frozen spawn-time position — fine for enemy hit/death
     numbers (the enemy itself dies or stays roughly put), but the player keeps moving after taking a
     hit or getting a kill, so those two numbers would visibly detach and drift in a fixed spot the
     player had already walked away from.

     New optional `followsPlayer` constructor parameter (defaults `false` — every other call site,
     i.e. enemy hit/death numbers, is unaffected and stays anchored to wherever it spawned). When true,
     `Update()` recomputes `Position` every tick as `Player.Instance.Position + spawnOffset +
     floatOffset` instead of just accumulating `Position += FloatVelocity` from a frozen start — the
     spawn jitter and the upward float are both preserved as offsets layered on top of the player's
     *current* position, so the number still jitters and floats up exactly as before, it just does so
     relative to a moving anchor instead of a fixed point. Passed `followsPlayer: true` at the two
     player-anchored call sites: `Player.Hit()`'s own damage-taken number, and `Enemy.WasShot()`'s XP
     gain number (entries 157/158/160).

     Verified via a scripted repro (throwaway `Wizard`, so `Player.Instance` briefly points at it but
     nothing persists): triggered all three kinds of number (an enemy hit, an XP gain, and the player's
     own damage-taken) in one pass, advanced 10 ticks, then moved the test player 4000+ units away, then
     advanced 10 more ticks. The enemy hit number (`followsPlayer=False`) ended up ~5634 units from the
     player's new position (correctly left behind, only the float-up drift moved it at all); both
     player-anchored numbers (`followsPlayer=True`) ended up only ~33-57 units from the player's new
     position (correctly tracking the move, that remaining distance being just their own jitter/float
     offset). Real save files confirmed byte-identical before and after. Clean build and a plain
     boot-check both passed.
162. **The player's own damage-taken and XP numbers now draw on top of the player sprite instead of
     underneath it.** `EntityManager.Draw()` has always drawn the Player in its own final pass, after
     every other entity, specifically so the sprite renders above projectiles/enemies/ground clutter
     regardless of submission order — but that same rule was silently swallowing the two
     player-anchored `DamageNumber`s from entries 157/158/161 too, since they're just another `Entity`
     in the same list and sit right at the player's own position. Whenever the number and the sprite
     overlapped, the player-drawn-last rule painted the sprite right over the number.

     `DamageNumber`'s previously-private `followsPlayer` field became a public `FollowsPlayer`
     property — the exact flag from entry 161 that already distinguishes "this number is anchored to
     the player" from an enemy's own hit/death number — so `EntityManager.Draw()` could single it out.
     Split the existing two-pass draw (everything else, then Player) into three: everything except
     Player and `FollowsPlayer` numbers, then Player, then the `FollowsPlayer` numbers last — so they
     always render above the sprite they float over, the same way the sprite itself always renders
     above everything beneath it. An enemy's own hit/death number is unaffected, still drawn in the
     first pass at its normal spot.

     Verified visually (a numeric check alone can't confirm draw order — same reasoning as entry 152's
     stepper-spacing catch): rendered a throwaway `Wizard` with a large, deliberately zero-offset
     `FollowsPlayer` `DamageNumber` spawned dead-center on the sprite (so the two would clearly overlap)
     to a PNG via a lightweight `Camera` (not a real `NexusState`/`RealmState`, which save
     unconditionally in their constructors) and inspected it directly — the number's leading digit
     visibly paints over the sprite where they overlap, confirming the new draw order. Real save files
     confirmed byte-identical before and after. Clean build and a plain boot-check both passed.
163. **Three new Settings > Graphics toggles, all default on: "Show Player Damage Numbers," "Show
     Enemy Damage Numbers," "Show Hit Particles."** Each independently gates one specific piece of
     combat feedback, separate from the existing "Show XP Drops" (entry 160) which only covers the XP
     gain number:
     - **Show Player Damage Numbers** gates `Player.Hit()`'s own "I took damage" number.
     - **Show Enemy Damage Numbers** gates `Enemy.WasShot()`'s hit number (the one over an enemy when
       the player damages it).
     - **Show Hit Particles** gates `Enemy.WasShot()`'s two `Particle.SpawnBurst()` calls (the white
       burst on a hit, the orange-red burst on a kill) — not `Player.LevelUp()`'s separate gold swirl,
       which uses a different particle flavor (`SwirlParticle`) for a distinct celebratory moment, not
       a combat hit.

     New `Player.ShowPlayerDamageNumbersEnabled`/`ShowEnemyDamageNumbersEnabled`/
     `ShowHitParticlesEnabled` fields and matching `GameSettingsData` DTO properties, all three with
     the explicit `= true` default (not just documentation — same `System.Text.Json` "only overwrites
     properties present in the JSON" reasoning as every prior on-by-default setting this session, so
     an old `GameSettingsData.json` predating these fields deserializes them at `true`, not the unstated
     bare-bool `false`). `Util.Save/LoadGameSettingsData()` and three new `SettingsRow`s in
     `SettingsState.cs`'s Graphics tab, right after "Show XP Drops," complete the wiring.

     Verified via a scripted repro (real `GameSettingsData.json` backed up first, restored immediately
     after the round-trip test): confirmed all three default to `true` on a fresh `Player`; with all
     three off, killing a test enemy and hitting the test player produced exactly one `DamageNumber`
     (the still-independently-on XP number from entry 160, correctly unaffected by these three) and
     zero `Particle`s; with all three back on, the same sequence produced exactly 2 more
     `DamageNumber`s (the enemy hit + player hit numbers) and 19 `Particle`s (5 from the hit burst + 14
     from the death burst); and a real save(`false`)→load() round trip correctly flipped all three
     in-memory values. Per the entry-118 lesson, also rendered the Graphics tab to a PNG and visually
     confirmed all three new rows read correctly and align in the same value column as the other four,
     no overlap. Real save files confirmed byte-identical before and after. Clean build and a plain
     boot-check both passed.
164. **Fixed the Dexterity-to-attack-speed formula to match the real intended curve, and fixed a real
     precision bug found while verifying it.** The old formula (`projectileCooldown += ((Dexterity *
     100) / 150 * 100) / 100`, fire once it reaches a fixed `240`) was pure integer arithmetic that
     didn't correspond to any documented rate — at 50 DEX it produced 8.25 attacks/sec and at 75 DEX
     12.5 attacks/sec, versus the intended 5.833 and 8.0. Replaced with a new `Player.AttacksPerSecond`
     property implementing the real formula directly: `1.5f + 6.5f * (Dexterity / 75f)` — 1.5 A/s at 0
     DEX, scaling to exactly 8.0 A/s at 75 DEX (matching the Wizard's own `MaxDexterity`). No Berserk
     multiplier was added — this engine has no Berserk status effect to hook a ×1.25 into; noted here
     as a spec line that doesn't correspond to any existing mechanic, same category as entry 154's
     "True Range" and entry 156's "Shots pass through obstacles."

     `projectileCooldown` changed from an `int` compared against a fixed `240` to a `float` accumulator
     that adds `AttacksPerSecond / 60` each tick (60 ticks/sec) and fires once it reaches `1.0`. Two
     real bugs were caught and fixed while verifying this, not just replacing the formula outright:
     - **Precision loss from resetting to `0` instead of subtracting `1`.** A first pass reset the
       accumulator to exactly `0` on fire, discarding whatever fraction had overshot past `1.0` that
       tick — at 50 DEX (increment 0.09722/tick, which doesn't evenly divide 1.0) this produced only 54
       real shots over 600 simulated ticks where the formula calls for ~58.3, a ~7% systematic
       shortfall that would only compound the longer a player fought. Fixed by subtracting `1f` instead
       of resetting to `0f`, carrying the leftover fraction into the next cycle — re-verified at exactly
       58/58.3 (50 DEX) and exactly 80/80 (75 DEX, where the increment divides evenly with no
       quantization at all).
     - **Unconditional accumulation while idle.** The original code (and the fix's first pass) added to
       `projectileCooldown` every tick regardless of whether the player was actually holding the
       attack button, so cooldown could bank up indefinitely while idle — the first click after any
       pause fired instantly no matter how long the pause, which isn't "attacks per second" in any
       real sense, and combined with the subtract-1 fix above would have caused a rapid-fire *burst* of
       banked shots on the next several ticks instead of one bonus shot. Fixed by moving the
       accumulation inside the same "is the player trying to fire" check as the fire itself, so
       cooldown only progresses while actually attacking — no idle banking, no burst risk, and partial
       progress toward the next shot is still preserved across a brief release/re-press.

     Verified via a scripted repro (throwaway `Wizard`, so `Player.Instance` briefly points at it but
     nothing persists): confirmed `AttacksPerSecond` reads exactly `1.5`/`5.8333335`/`8` at DEX
     `0`/`50`/`75`; then, with the mouse simulated as continuously held, ran 600 real ticks (`Update()`
     calls, not a shortcut) at 50 and 75 DEX and counted actual fired shots via real `Weapon.Shoot()`
     entity growth — landed at 58 (50 DEX, expected ~58.3) and exactly 80 (75 DEX, expected exactly 80)
     after the fix, versus 54 and 75 before it. Real save files confirmed byte-identical before and
     after. Clean build and a plain boot-check both passed.
165. **Fixed the Speed-to-movement-speed formula to match the real intended curve**, the same
     double-check requested for entry 164's attack speed. The old formula (`Velocity = (int)((Speed /
     75) * 5.6 + 2) * slowMultiplier * Input.GetMovementDirection()`) didn't match any documented
     rate either — converted to tiles/sec via the established 32px/tile, 60-ticks/sec basis, it gave
     3.75 T/s at 0 Speed, 9.375 T/s at 50 Speed, and 13.125 T/s at 75 Speed, versus the intended 4.0 /
     7.733 / 9.6. Its `(int)` cast on the whole px/tick magnitude also threw away real precision on
     top of that (e.g. truncated a true 5.7333 px/tick down to 5 at 50 Speed) — the same class of bug
     as entry 164's reset-to-`0` issue, just a cruder, single-tick version of it rather than a
     compounding one.

     New `Player.TilesPerSecond` property implements the real formula directly: `4f + 5.6f * (Speed /
     75f)` — 4.0 T/s at 0 Speed, scaling to exactly 9.6 T/s at 75 Speed. `Update()`'s Velocity line now
     computes `pixelsPerTick = TilesPerSecond * 32f / 60f` and multiplies that directly into `Velocity`
     with no `(int)` truncation anywhere — `Velocity`/`Position` are both already `Vector2` (float), so
     the old cast wasn't converting between representations, just discarding precision for no reason.
     No Speedy multiplier was added — this engine has no Speedy status effect to hook a ×1.5 into, same
     situation as entry 164's Berserk note.

     Verified via a scripted repro (throwaway `Wizard`, so `Player.Instance` briefly points at it but
     nothing persists): confirmed `TilesPerSecond` reads exactly `4`/`7.7333336`/`9.6` at Speed
     `0`/`50`/`75`; confirmed a real `Update()` tick with no key actually held (this environment can't
     simulate real keyboard input — see CLAUDE.md's `Input.Update()` gotcha) correctly left Position/
     Velocity unchanged; and, since that meant real travel distance couldn't be measured through
     `Update()` directly, reproduced `Update()`'s exact conversion formula with a synthetic direction
     vector and confirmed the resulting px/tick magnitude, converted back to tiles/sec over 60 ticks,
     landed exactly on `TilesPerSecond`'s own value at all three Speed levels with zero rounding loss.
     Real save files confirmed byte-identical before and after. Clean build and a plain boot-check both
     passed.
166. **Fixed the Vitality/Wisdom health-and-mana regen formulas to match the real intended curves**,
     the third and fourth stat-calculation double-checks this session (after entries 164/165's
     Dexterity/Speed). Both old formulas used the same int-tick-count-with-reset-to-0 pattern already
     found broken twice: `healthCooldown += 1 + (int)(0.24 * Vitality)`, fire (+1 HP) at 160; and
     `manaCooldown += 1 + (int)(0.12 * Wisdom)`, fire (+1 MP) at 320. Converted to HP/s and MP/s, these
     gave 0.375/3.75/7.12 HP/s and 0.1875/1.3125/1.875 MP/s at the spec's own example stat values —
     nowhere close to the intended 2.0/11.63/20.05 HP/s and 0.5/6.5/9.5 MP/s.

     New `Player.HealthRegenPerSecond` (`2f + 0.2407f * Vitality`) and `ManaRegenPerSecond` (`0.5f +
     0.12f * Wisdom`) properties — note these are flat linear rates (no `/75` scaling), unlike
     `AttacksPerSecond`/`TilesPerSecond`'s 0-75 range formulas, since the spec expresses VIT/WIS regen
     as a straight per-point bonus with no explicit cap breakpoint. `healthCooldown`/`manaCooldown`
     changed from `int` fields compared against fixed `160`/`320` thresholds to `float` accumulators
     adding `HealthRegenPerSecond / 60`/`ManaRegenPerSecond / 60` each tick and firing (+1 HP/MP) at
     `1.0`, subtracting `1f` (not resetting to `0f`) to carry the leftover fraction forward — the exact
     precision fix from entry 164, applied here proactively before shipping rather than after finding
     the same bug a third time. No Healing bonus was added (the spec's "+20 HP/sec while Healing") —
     this engine has no Healing status effect to add it into, same category as entries 164/165's
     Berserk/Speedy notes.

     Verified via a scripted repro (throwaway `Wizard`, so `Player.Instance` briefly points at it but
     nothing persists): confirmed `HealthRegenPerSecond` reads exactly `11.628`/`20.0525` at
     `40`/`75` Vitality and `ManaRegenPerSecond` reads exactly `6.5`/`9.5` at `50`/`75` Wisdom; then, with
     `HealthMax`/`ManaMax` raised high enough that regen never hit the cap, ran 600 real ticks (10
     seconds) at `40` Vitality / `75` Wisdom simultaneously and confirmed real `Health`/`Mana` gained
     landed at `116`/`94` against the formula's own `116.28`/`95` prediction — within a single unit in
     both directions, that remaining gap being pure integer-HP/MP quantization at the tick boundary
     (Health/Mana are both `int`), not systematic drift. Real save files confirmed byte-identical
     before and after. Clean build and a plain boot-check both passed.
167. **Fixed the Attack damage multiplier's integer-division bug, and found a real asymmetry in the
     Defense damage-reduction cap** — the fifth and sixth stat-calculation double-checks this session
     (after entries 164/165/166's Dexterity/Speed/Vitality/Wisdom). Unlike those four, ATT/DEF weren't
     new formula replacements — the existing code already matched the spec's intended shape almost
     exactly; both bugs here were narrower and more surgical.

     **ATT**: `Weapon.cs`'s `Shoot()` computed `double damgeModifier = (0.5 + Player.Instance.Attack /
     50)` — `Attack` is `int` and `50` is an int literal, so C# evaluates `Attack / 50` as pure integer
     division *before* ever adding the `0.5`, pinning the multiplier at exactly `0.5` for the entire
     `0`-`49` Attack range (every point in there produces the identical, wrong result) and only
     stepping at each multiple of `50` instead of scaling smoothly by 2% per point as the spec
     describes. Fixed with a single-character change: `/ 50` → `/ 50.0`, forcing real floating-point
     division. No Weak/Damaging multiplier — this engine has no such status effects to hook a `0.5`
     floor or `x1.25` bonus into.

     **DEF**: the spec's cap ("every shot does at least 10% of its damage") was *already correctly
     implemented* — but only in `Player.cs`'s `Hit()` (damage the player takes), not in `Enemy.cs`'s
     `WasShot()` (damage the player deals to an enemy), which only floored the reduced damage at `0`.
     A sufficiently defended enemy could end up effectively untouchable by weak attacks, while the same
     enemy hitting the player was already correctly guaranteed to always land at least 10% chip
     damage — a real, one-directional asymmetry in a stat that's supposed to behave identically
     regardless of whose Defense it is. Fixed by mirroring `Hit()`'s exact floor into `WasShot()`:
     `Math.Max(damage - Defense, damage / 10)`, applied only when `ignoresDefense` is false (Bow's
     Side shots still skip Defense — and this floor — entirely, as intended).

     Verified via a scripted repro (throwaway `Wizard`/`Enemy.CreateWanderer()`, real save files backed
     up first): pinned `DamageMin`/`DamageMax` to a single value (100) so `rand.Next()` always returns
     exactly 100, eliminating randomness, then fired real shots at Attack `0`/`25`/`50`/`75` and
     confirmed the implied multiplier read exactly `0.5`/`1.0`/`1.5`/`2.0` — correctly distinguishing
     `0` from `25` for the first time, where the old code gave both an identical `0.5`. For Defense,
     reproduced the spec's own three worked examples directly: `60` damage vs `20` Defense lost exactly
     `40` HP, vs `54` Defense lost exactly `6`, and vs `90` Defense (deliberately over the cap) *still*
     lost exactly `6` — confirming the floor now holds where the old code would have let it drop to
     `0`. Real save files confirmed byte-identical before and after. Clean build and a plain boot-check
     both passed.
168. **"Low Health Threshold" default changed from 25% to 20%.** `Player.LowHealthThresholdPercent`'s
     initializer and `GameSettingsData.LowHealthThresholdPercent`'s matching DTO default (both need
     their own explicit default — the JSON-deserialization-only-overwrites-present-properties reasoning
     from entry 151's original comment, restated there for anyone landing on this line later) changed
     from `25` to `20`. Pure data change, no logic touched. Verified via a scripted repro: confirmed a
     fresh `Player` now defaults to `20`, confirmed deserializing an old-shaped `GameSettingsData.json`
     missing the key still produces `20` rather than the unstated bare-int default of `0`, and rendered
     the Settings > Graphics tab to a PNG, visually confirming "Low Health Threshold" reads "20%"
     cleanly with no spacing regression. Real save files confirmed byte-identical before and after.
     Clean build and a plain boot-check both passed.
169. **Reworked Knight's Shield Slam to match its spec — a genuine 75% damage-taken multiplier instead
     of a flat Defense bonus, and its own fixed shot stats instead of borrowing the equipped Sword's.**
     Stun (`Enemy.Stun(durationFrames: 180)` = 3s) and piercing (`ExpiresOnHit = false`, already
     shared with Bow/Quiver's `HitBy`-tracking pass-through) were already correct and needed no
     changes. Two real gaps found:
     - **Damage Reduction was the wrong mechanism entirely.** The old code called
       `AddTemporaryDefenseBonus(20, 180)` — a flat +20 Defense stat for 3 seconds, which reduces
       damage 1-for-1 like any other point of Defense (subject to entry 167's 10%-of-raw floor).
       The spec wants "you receive 75% damage for 5 seconds" — a direct multiplicative reduction on
       the raw hit, independent of and stacking with Defense, for a different duration entirely. New
       `Player.DamageTakenMultiplier` (defaults `1f`) plus `AddTemporaryDamageTakenMultiplier(float,
       int)`, following the exact same opt-in-temporary-effect shape as the existing
       `Temporary*Bonus` fields but *not* routed through `RecalculateStats()` (it isn't a stat — it's
       applied directly in `Player.Hit()`, multiplying the raw incoming `damage` before Defense's own
       reduction/floor runs). Knight's ability now calls
       `AddTemporaryDamageTakenMultiplier(0.75f, 300)` (5 seconds).
     - **The shot borrowed `Weapon.ProjectileMagnitude`/`ProjectileDuration` from whatever Sword was
       equipped** (`Duration = Weapon.ProjectileDuration + 15`) instead of having its own fixed values
       — every Sword tier is `8` px/tick / `14` ticks in this game's data, so the ability's actual
       range drifted to roughly double the spec's intended `3.2` tiles (`8 × 29 = 232px ≈ 7.25 tiles`)
       regardless of which Sword tier was equipped. New `Knight`-local constants
       `ShieldProjectileMagnitude` (`8.533333` px/tick, from 16 tiles/sec) and
       `ShieldProjectileDuration` (`12` ticks, from 0.2s — `8.533333 × 12 = 102.4px = 3.2` tiles,
       consistent), used directly instead of reading `Weapon`. Kept as plain `Knight.cs` constants
       rather than new `ShieldData.json` fields (unlike Bow/Quiver's reworks) since this spec gives one
       flat set of numbers for all tiers, not a per-tier progression — no data-driven scope to add.
       "Shots pass through obstacles" still doesn't correspond to any mechanic this engine has (same
       as entries 154/156/165's notes) — nothing to implement.

     Verified via a scripted repro (throwaway `Knight`, so `Player.Instance` briefly points at it but
     nothing persists): confirmed `UseAbility()` set `DamageTakenMultiplier` to exactly `0.75` and
     spawned exactly one `Projectile` reading `ExpiresOnHit=False`, `StunsOnHit=True`, `Duration=12`,
     and a velocity magnitude of exactly `8.533333`; confirmed a real `Hit(100)` while the multiplier
     was active and a second `Hit(100)` 300 ticks later (after it expired) differed by exactly `25` HP
     — precisely the 25% reduction, with the fresh Knight's own real starting Defense (`8`, from base
     + Iron Plate) present and identical in both calls, canceling out algebraically and leaving no
     ambiguity that the multiplier itself was the only thing that changed. Real save files confirmed
     byte-identical before and after. Clean build and a plain boot-check both passed.
170. **Shield Slam now fires a per-tier fan of shots — 1 at Tier 0, scaling up to 5 at Tier 6-7 —
     instead of always firing exactly one.** Follow-up to entry 169, same day. Per the user's table
     (T0:1, T1:2, T2:3, T3:3, T4:4, T5:4, T6:5, T7:5), new `Shots`/`ArcGapDegrees` fields added to
     `Data/ShieldData.cs`/`ShieldData.json`/`Shield.cs`, mirroring Quiver's exact per-tier-fan pattern
     from entry 156 — `Knight.UseAbility()` now loops `Shots` times firing the same symmetric-fan
     formula Quiver uses (`angle = aimAngle + (i - (Shots-1)/2f) * arcGapRad`), so an odd count centers
     one shot on the aim line and an even count straddles it evenly; Tier 0's `Shots=1` degenerates
     cleanly to a single straight shot with no special-casing needed. `ArcGapDegrees` set to a uniform
     `10°` per adjacent-shot gap across all 8 tiers — not specified in the request (only shot counts
     were given), chosen as a reasonable starting spread and flagged here as tunable; because total fan
     width scales with `(Shots-1) × ArcGapDegrees`, a fixed per-gap angle already satisfies "higher-tier
     shields have a wider projectile arc" on its own as shot count grows, without needing per-tier angle
     values. Speed/lifetime/art stay the fixed `ShieldProjectileMagnitude`/`ShieldProjectileDuration`
     constants from entry 169, applied identically to every shot in the fan. `Player.cs`'s
     `EquipHighestTierAbilityItem()` (F4) got a proactive Shield-specific branch to copy the two new
     fields, applying the entry 45/154/156 lesson before any bug report this time.

     Verified via a scripted repro (throwaway `Knight`, so `Player.Instance` briefly points at it but
     nothing persists): confirmed all 8 catalog entries' `Shots` matched the user's table exactly;
     confirmed the starting Tier 0 Wooden Shield's `UseAbility()` added exactly 1 entity; confirmed F4
     correctly equipped the Tier 7 Shield with `Shots=5`; confirmed that Shield's `UseAbility()` added
     exactly 5 entities; and measured the angular gap between all 4 adjacent pairs of the resulting
     5-shot fan, landing at exactly `0.174533` rad (10°) in every case. Real save files confirmed
     byte-identical before and after. Clean build and a plain boot-check both passed.
171. **Added Priest, the 4th playable class** — a self-healing, ranged-nova support class using
     Wand+Robe, unlocked at 5,000 Fame (continuing Archer/Knight's 1,000/3,000 escalation). Follows
     the exact extension pattern from entries 31/109/etc.: a new `Data/TomeData.cs` DTO +
     `Data/TomeData.json` 8-tier catalog (values transcribed directly from the user's table) +
     runtime `Tome : AbilityItem` class with a `LoadTome()` static loader, mirroring `Shield.cs`
     exactly. Wand (`Weapon.WeaponType.Wand`, 15 tiers) had sat unused since Wizard switched to
     Staff — explicitly reserved "for a future class" back then — and Robe was already shared with
     Wizard, so zero new weapon/armor catalog data was needed. Stats (HP/MP/ATT/DEF/SPD/DEX/VIT/WIS
     initial+per-level) transcribed directly from the user's table; `Priest.RecalculateStats()`'s
     rates (+1 ATT/lvl, +0.5 DEF/lvl int-truncated, +1 SPD/DEX/VIT/lvl, +2 WIS/lvl) were derived by
     solving the spec's own "Average at 20" column backwards and verified to reproduce it exactly.

     Per the user's spec (Tomes restore health to self and allies via an instant heal + a separate
     "Red Cross Healing" HoT that doesn't stack — only the strongest active one applies — plus a
     cursor-centered AOE damage nova), three scope questions had no clean answer in a single-player
     engine with no party mechanic and were resolved via `AskUserQuestion` before implementing,
     all three answered with the recommended option: (1) group healing → **self-only** (the spec's
     "nearby allies" targeting has nothing to apply to — skipped entirely, not stubbed); (2) the
     nova's "explodes 2 times for 0.8 seconds" → **two discrete damage pulses spread across 0.8s**,
     the total tier damage split evenly between them; (3) WisMod scaling (Wisdom boosting Range/
     Heal/Healing/Damage) → **skipped for now**, shipping flat per-tier table values only, matching
     that no other ability in this game scales off Wisdom yet — each affected value is already an
     isolated per-tier `Tome` field, so a future WisMod multiplier is a small additive change later.
     "Feed Power" and the "Sick" status (both RotMG pet-feeding/negation mechanics with zero
     corresponding system in this engine) were likewise noted and deliberately not implemented,
     consistent with this session's running precedent for out-of-scope spec lines.

     New mechanics needed for the first time by any class: `Player.HealingAmountPerSecond` +
     `healingDurationFrames` (added straight to `HealthRegenPerSecond`, following the exact
     opt-in-temporary-effect shape as the existing `DamageTakenMultiplier` field, but with
     "strongest overrides, weaker attempts while a stronger one is active are ignored entirely"
     semantics instead of DamageTakenMultiplier's "refresh to newest" — matches the spec's explicit
     non-stacking rule); `Player.Heal(amount)` for the instant flat self-heal; a new
     `EntityManager.DamageEnemiesInRadius(center, radius, damage)` for the nova's AOE (no existing
     helper did direct point-blank damage — everything else routed through a `Projectile`/`HitBy`
     collision pass); a new lightweight `NovaPulse : Entity` (a pure scheduled-action timer with a
     no-op `Draw()`, since it has no `image`) for the nova's delayed second pulse; and a new shared
     `Equipment.XpBonusPercent` bonus (Tier 3+ Tomes grant +1% to +8% XP) — added to the base
     `Equipment` class rather than only `Tome`, keeping every equip slot's "any bonus can theoretically
     land here" symmetry intact even though only Tome populates it today. `Enemy.WasShot()`'s death
     branch now scales the XP grant (and its displayed `DamageNumber`) by
     `Player.Instance.EquipmentXpBonusPercent`, a true no-op multiply-by-1 for every other class's
     default 0%. Applied the entry 45/154/156/169/170 F4 lesson proactively again, before any bug
     report this time: `EquipHighestTierAbilityItem()` got a Tome-specific branch copying
     `Range`/`HealAmount`/`HealingAmountPerSecond`/`HealingDurationSeconds`, plus generic
     `XpBonusPercent` copying added for all four `AbilityItem` subclasses at once.

     `Player.Class` gained `Priest`, which — as usual — meant sweeping every place that exhaustively
     enumerates playable classes rather than just the enum: `Util.cs`'s
     `EraseAllAccountData()`/`DetermineLastPlayedClass()`/`AnyCharacterHasBeenPlayed()`/
     `ResetPlayer()`, and `CharacterSelectState`'s slot layout (reworked from a single
     `SlotOffsetFromCenter` dead-center-plus-two-flanking-slots layout, which has no even-count
     equivalent, into `SlotOffsetFromCenterOuter`/`Inner`, four evenly-150px-spaced slots) and
     `BuildDefaultPreviewText()`'s switch.

     Verified via a scripted repro (throwaway `Priest`, so `Player.Instance` briefly points at it):
     confirmed all 8 base stats matched the spec exactly at Level 1 and, after 19 simulated
     `LevelUp()` calls, matched the "Average at 20" column exactly; confirmed starting equipment
     (Fire Wand/Cloth Robe/Tier-0 Healing Tome, with the Tome's `HealAmount=0`/
     `HealingAmountPerSecond=50`/`HealingDurationSeconds=10`/`Range=6.0` matching the table exactly);
     confirmed `CanEquipAbilityItem` accepts only `Tome` and rejects `Spell`/`Quiver`/`Shield`;
     equipped the Tier-7 Tome and confirmed `UseAbility()` deducted mana, applied the instant heal,
     set `HealingAmountPerSecond`, and that `HealthRegenPerSecond` reflected it; confirmed the
     "strongest wins" rule directly (a weaker `ApplyHealing()` call while a stronger one was active
     was ignored; a stronger one still overrode); confirmed a kill's XP gain and displayed
     `DamageNumber` were both scaled by exactly the Tier-7 Tome's 8% bonus; confirmed the nova's
     first pulse hit an in-range target for exactly `MinDamage/2` while a target sitting where an
     *unclamped* cast toward a far-away cursor would have landed took no damage (proving the
     `Range`-based clamp actually engaged, not just that something got hit); manually advanced the
     scheduled `NovaPulse` 48 ticks and confirmed the second pulse landed on the same in-range target
     for the total `MinDamage` while the far target still took nothing, and that the pulse
     self-expired after firing; confirmed F4 (`EquipHighestTierAbilityItem`) equipped the Tier-7
     Tome with all four Tome-specific fields correctly populated; confirmed `Util.ResetPlayer(Priest)`
     constructs a real `Priest`; and rendered `CharacterSelectState`'s new 4-slot layout to an
     offscreen PNG (same technique as entry 118) and visually confirmed all four portraits render
     with even spacing and no overlap. One test assumption was itself wrong, not the code: Defense
     was asserted as exactly `0`/`9` at Level 1/20, but the starting Cloth Robe's own `+2
     DefenseBonus` correctly stacks on top of the level formula (giving `2`/`11`) — confirmed
     correct by checking `ArmorData.json` directly, not a Priest bug.

     Also surfaced (and documented in `CLAUDE.md`, see that file's own changelog) a save-triggering
     path not previously on the "back up first" list: `Player.LevelUp()` itself saves unconditionally
     the instant `Level` first reaches 20, which the scripted repro's `LevelUp()` loop tripped —
     creating real `PlayerData_Priest.json`/`InventoryData_Priest.json` files (re-saving, but not
     altering, the real unmodified `BankData.json`/`FameData.json` alongside them). Real save files
     for every already-played class were confirmed byte-identical to a pre-test backup throughout;
     the two newly-created Priest files (fabricated Level 20/0 XP test-fixture data, not real
     progress) were deleted afterward so a real Priest's first playthrough starts genuinely fresh.
     Clean build and a plain boot-check both passed.

     While staging the commit, found two untracked art assets sitting in `Content/` with no prior
     mention: dedicated per-tier Wand projectile art for 9 of the 15 tiers
     (`Content/Weapons/Wands/Projectiles/{0,1,5,6,7,8,10,13,14}.png`, matching the file's own tier
     numbers directly) and a 16x16 `Content/StatusEffects/healing.png` in the exact format of the
     existing Paralyzed/Stunned/Slowed status icons. Both fit Priest thematically (Wand is Priest's
     weapon; "Healing" is exactly the status Tomes apply) but neither was asked for, so flagged via
     `AskUserQuestion` before wiring either in — confirmed to wire in both. `Data/WeaponData.json`'s
     9 affected Wand tiers now point `ProjectileImageName` at their own dedicated
     `Weapons/Wands/Projectiles/{tier}` art instead of the shared generic bolts (`red_fire`,
     `blue_magic`, `purple_magic`, `pink_bolt`, `green_magic`) they were reusing before (tiers 13/14
     had actually been pointing at `Projectiles/evocation_magic`/`retribution_magic`, names that
     matched no real content at all — those two tiers' projectiles were silently broken before this
     fix). The other 6 Wand tiers (2/3/4/9/11/12) still intentionally share generic art, since no
     dedicated art exists for them yet. `healing.png` needed a real integration path rather than a
     drop-in: `Player.HealingAmountPerSecond`/`ApplyHealing()`'s existing magnitude-plus-
     strongest-wins logic (entry 171, above) has no equivalent in `Entity`'s generic
     `DebuffType`/`activeDebuffs` dictionary (which only tracks a duration, no magnitude, and simply
     overwrites on reapply — it was deliberately *not* used for Healing for exactly this reason).
     Rather than replacing the Player-only fields, added `DebuffType.Healing` (+ `Art.Healing` in the
     `DebuffIcon()` switch) purely as a cosmetic mirror: `ApplyHealing()` now also calls
     `ApplyDebuff(DebuffType.Healing, durationFrames)` in lockstep, but only inside the branch that
     already passed the strongest-wins check — so the floating icon (rendered automatically by
     `Player`'s existing `UpdateDebuffs()`/`DrawDebuffIndicators()` calls, already wired for the
     `Slow` debuff) appears exactly when, and for exactly as long as, the real effect is actually
     active, without becoming the source of truth for it.

     Verified via a second scripted repro: loaded every one of the 15 Wand tiers'
     `ProjectileImageName` directly via `Content.Load<Texture2D>` and confirmed all 15 succeed
     (the 9 newly-dedicated tiers, including the two previously-broken ones, and the 6 still-shared
     tiers); confirmed `Art.Healing` loaded; confirmed a fresh Priest's starting Fire Wand equips
     without error; confirmed `ApplyHealing()` sets `HasDebuff(DebuffType.Healing)` true; confirmed
     `Player.Draw()` (which calls `DrawDebuffIndicators()`) renders without throwing now that Healing
     is a real active debuff backed by real art. First build attempt after adding the
     `StatusEffects/healing.png` `Content.mgcb` block failed (`mgcb`'s command-line parser choked
     with "Too many arguments") — traced to a transcription slip copying that block's
     `/importer`/`/processor`/`/processorParam` lines with a leading backslash instead of forward
     slash; fixed and rebuilt clean. Real save files confirmed byte-identical before and after. Clean
     build and a plain boot-check both passed.
172. **Added Vital Combat**, a new in-combat (IC) / out-of-combat (OOC) system: a single hit whose
     raw damage clears a Defense-scaled Combat Trigger puts the player IC for up to 7 seconds
     (reduced by Vitality), during which VIT/WIS-driven regen is halved. Visualized in the sidebar
     by a yellow border around the HP bar (togglable) and a small badge above it that lights up and
     shows the current trigger on hover.

     The design doc itself had an internal contradiction: an opening paragraph described DEF
     brackets at 15/30/45, but a second bulleted list gave 15/35/65/125, and only the second set
     reproduces the doc's own three worked examples (Archer 25 DEF -> 22, Rogue 45 DEF -> 35, Knight
     77 DEF -> 48) — checked by hand against both layouts before writing any code. Went with
     15/35/65/125 as the real intended bracket edges and treated the first paragraph as a leftover
     draft, not a second scaling to reconcile. `Player.CombatTrigger` implements this as four
     brackets at 100%/75%/50%/25%, each folding its own contribution into the next bracket's
     starting value (15/30/45/60) rather than recomputing from 0 every time, permanently capped at
     60 beyond 125 DEF (the doc's own "0% rate" bracket) and floored at 1 (the doc's "starts at 1
     damage" line, otherwise meaningless at exactly 0 Defense where the 1:1 bracket would give a
     trigger of 0).

     Two real ambiguities were resolved via `AskUserQuestion` before writing any code, both answered
     with the recommended option: (1) whether the trigger check compares the *raw* incoming hit or
     the Defense-*mitigated* HP actually lost — went with raw, since comparing against the mitigated
     value would double-count Defense (it already shrinks both the compared value and, separately,
     the threshold itself); (2) no sword icon art exists anywhere in `Content/` for the "lights up"
     indicator — went with a plain placeholder (a tinted square via `Art.HealthBar`, the same
     solid-color-rectangle technique `Util.DrawTooltip`'s background panel already uses), a one-line
     `spriteBatch.Draw` swap away from real art later.

     New `Player.cs` state: `InCombat`/`inCombatFrames` (same opt-in-temporary-effect shape as
     `DamageTakenMultiplier`/`HealingAmountPerSecond`), `CombatTrigger` (the bracket formula above,
     reading the live `Defense` field), `InCombatDurationFrames` (`7 - Vitality * 0.04` seconds,
     clamped to a 1-frame floor rather than 0 — a literal 0 would let `UpdateTemporaryBonuses()`'s
     `> 0` tick-down guard never fire, leaving `InCombat` stuck true forever; purely theoretical,
     since no class's Vitality cap comes close to the 175 VIT needed to actually hit it), and
     `RegisterHit(int rawDamage)` (called from `Hit()` with the hit *before* Defense's own
     reduction, entering or refreshing combat back to the full duration on a qualifying hit).
     `HealthRegenPerSecond`/`ManaRegenPerSecond` now multiply only their Vitality-/Wisdom-driven
     term by `0.5f` while `InCombat` — the flat per-second base and a Priest Tome's
     `HealingAmountPerSecond` bonus are both left untouched, matching the design doc's own
     "regeneration *caused by* VIT and WIS" wording. The doc's "Pets take 2 more seconds to trigger
     while IC" line has nothing to hook into — this engine has no pet system at all — so it's noted
     here, not implemented, same as this session's running precedent for other out-of-scope spec
     lines (Feed Power, Sick, "Shots pass through obstacles").

     New account-wide `ShowCombatIndicatorEnabled` setting (defaults on), added end-to-end following
     the established `ShowHitParticlesEnabled`-shaped pattern exactly: `Player.cs` field,
     `Data/GameSettingsData.cs` DTO property (with its own explicit `= true` default so an existing
     settings file predating this field still deserializes to "on", not a silently-`false` bare
     default), `Util.cs` save/load wiring, and a new `SettingsRow` on the Settings > Graphics tab.
     Gates only the yellow border — the badge's own idle-vs-lit-up appearance always shows,
     regardless of the setting, matching the design doc's own wording only conditioning the border
     on "if they have it enabled".

     `Overlay.cs` gained `DrawCombatIndicator()` (called right after `DrawHealthSection()`, so its
     border draws on top of the already-drawn HP bar rather than underneath it) plus a small
     `DrawBorder()` helper (four `Art.HealthBar` strips) — the codebase had no colored-border
     helper to reuse; the equipment slots' `Art.Border` texture is drawn plain-white-tinted only,
     not built for recoloring per state. The hover tooltip is anchored to the badge's *left*,
     computed from `Art.HudFont.MeasureString()` up front, rather than trusting
     `Util.DrawTooltip()`'s own `ClampTooltipX` alone — the badge sits at the sidebar's own right
     edge, and a first attempt anchoring the tooltip near it (`icon.X - 100`) rendered with its
     right edge clipped past the visible window in an offscreen-rendered test PNG; anchoring by the
     tooltip's own measured width instead fixed it immediately, confirmed by re-rendering.

     Verified via a scripted repro (throwaway `Wizard`, reflection into the private
     `UpdateTemporaryBonuses()` to advance frames without a full `Update()`/`Camera`/`Input` setup):
     confirmed `CombatTrigger` against all three worked examples plus every bracket edge (15/35/65/
     125), the sub-1 floor at 0 DEF, and the permanent cap past 125 DEF; confirmed a sub-trigger hit
     doesn't enter combat while a qualifying one does; confirmed IC duration at 25 VIT lands at
     exactly 360 frames (6s), including that a mid-duration qualifying re-hit refreshes the full
     360 frames from the *refresh* point rather than extending or stacking from the original hit;
     confirmed `HealthRegenPerSecond`/`ManaRegenPerSecond` numerically match hand-computed
     IC-halved and OOC-full values, and that a Tome's `HealingAmountPerSecond` contributes
     identically either way; confirmed `GameSettingsData`'s new field defaults to `true` both fresh
     and when deserializing JSON missing the key entirely (an isolated in-memory
     `JsonSerializer` round-trip, deliberately never touching the real `GameSettingsData.json` file);
     and rendered `Overlay.DrawSidebar()` to two offscreen PNGs (IC and OOC) and visually confirmed
     the border/badge/tooltip all appear, disappear, and recolor correctly between the two states
     (this is also what caught the tooltip clipping bug above — never would have been visible from
     the passing numeric checks alone). Real save files confirmed byte-identical before and after,
     including the account's real `PlayerData_Priest.json`/`InventoryData_Priest.json` created by
     actual play since entry 171. Clean build and a plain boot-check both passed.
173. **`CombatTrigger` (entry 172) now excludes equipment's Defense contribution**, per user request.
     `Player.cs`'s `CombatTrigger` read the live `Defense` field directly, which already folds in
     `EquipmentDefenseBonus` (Weapon/Armor/Ring/AbilityItem combined) alongside base/level/potion/
     temporary Defense — letting gear alone buy a higher trigger (and therefore less-often regen
     halving) just by wearing tankier armor, no different from a permanent stat investment. Changed
     the one line feeding the bracket formula to `Defense - EquipmentDefenseBonus` instead of
     `Defense` — deliberately narrower than the existing `PermanentDefense` property (entry 109ish,
     `Defense - EquipmentDefenseBonus - TemporaryDefenseBonus`), which also strips
     `TemporaryDefenseBonus`; only equipment was asked to be excluded here, so a temporary Defense
     buff still counts toward the trigger same as before. The existing `Math.Max(1, ...)` floor
     (entry 172) already covers the case where equipment now exceeds Defense, so no extra clamping
     was needed.

     Verified via a scripted repro (throwaway `Wizard`, starting gear's own `DefenseBonus` zeroed out
     first so it wouldn't contaminate the numbers): confirmed 45 Defense with zero equipment gives
     the same Trigger 35 as before this change; confirmed the key case — 65 Defense with 20 of it
     from Armor now gives Trigger 35 (the 45 non-equipment portion), not the 45 a same-magnitude
     hit would've given pre-fix (bracket 3 on the full 65); confirmed the same raw 65 Defense with
     the Armor bonus removed correctly jumps back to Trigger 45, isolating that the *equipment*
     portion specifically is what's being excluded, not some fixed offset; confirmed an equipment
     bonus at or above total Defense still floors at Trigger 1 rather than going negative. Real save
     files confirmed byte-identical before and after. Clean build and a plain boot-check both passed.
174. **`CombatTrigger` (entries 172/173) now also excludes `TemporaryDefenseBonus`**, per user
     follow-up. Entry 173 had already excluded `EquipmentDefenseBonus` while deliberately keeping
     `TemporaryDefenseBonus` (a temporary Defense buff still counted) since only equipment exclusion
     had been asked for at the time; asked again to exclude it too, and `Defense -
     EquipmentDefenseBonus - TemporaryDefenseBonus` is exactly what the existing `PermanentDefense`
     property (used elsewhere by `Overlay.DrawStats()`'s "is this maxed" check) already computes —
     simplified `CombatTrigger` to read `PermanentDefense` directly instead of hand-repeating the
     same subtraction a second time.

     Verified via a scripted repro (throwaway `Wizard`, equipment/temporary Defense zeroed first):
     confirmed a temporary-only buff (65 Defense, 20 from `TemporaryDefenseBonus`) gives the same
     Trigger 35 as the equivalent equipment-free baseline, not the 45 the full 65 would give;
     confirmed removing the buff at the same raw 65 jumps back to 45, isolating that the temporary
     portion specifically drives the difference; confirmed equipment and temporary bonuses excluded
     together still net out correctly (10 + 10 of each, same Trigger 35 as either alone); confirmed
     the existing floor-at-1 still holds when a temporary bonus alone would push it negative. Real
     save files confirmed byte-identical before and after. Clean build and a plain boot-check both
     passed.
175. **Fixed "the priest is unable to equip a tome"** — see [BUGFIXES.md](BUGFIXES.md) entry 51 for
     the user-facing summary. Root cause: `InventorySystem.cs`'s `TryEquipFromRecord()` (the
     drag-and-drop handler for dropping an inventory item onto an equip slot) has a `switch` over the
     dragged item's concrete type for the ability-item slot specifically — `Spell`/`Quiver`/`Shield`
     each map to their own `LoadX()` factory — that was never given a `Tome` case when Tome was added
     (entry 171), so a dragged Tome fell straight through to `_ => null` and the whole drag was a
     silent no-op. This is precisely the bug class flagged in entries 45/154/156/170 — a new
     `AbilityItem` subclass not propagated to every old exhaustive switch over the other three — just
     never actually caught for the one spot that matters most: the primary way a player equips
     anything at all.

     Given that history, treated this as a prompt for a full audit rather than a one-line fix: spawned
     an Explore agent to grep the entire codebase for every `Spell`/`Quiver`/`Shield`-specific branch
     and check each for a matching `Tome` case. Found three more real gaps, all fixed together:
     `Item.cs`'s `[JsonDerivedType]` list (used for `System.Text.Json`'s polymorphic serialization of
     an `Item`-typed slot, e.g. an inventory/bank entry) never got a `Tome` entry — per that file's
     own doc comment, a runtime type absent from this list throws `NotSupportedException` the instant
     `System.Text.Json` tries to *serialize* it (only *deserializing* has a fallback), so an
     unequipped Tome sitting in inventory or the bank would have crashed on the next save; and two
     `Concat()` chains in `ItemSpawner.cs` (`Spawn()`'s regular per-kill loot roll, and
     `SpawnGuaranteedLoot()`'s guaranteed/boss-style roll) that build the "any class's ability item"
     drop pool from `Spells`/`Quivers`/`Shields` but never `Tomes` — meaning a Tome could never drop
     as loot anywhere in the game, for any class, independent of the equip bug. All three already had
     a correctly-Tome-aware sibling elsewhere in the same codebase to model the fix on
     (`AbilityItem.PlaceholderImage`'s own 4-way `Concat()`, and `Player.EquipHighestTierAbilityItem()`'s
     already-correct `Tome` switch case from entry 171) — this was a case of the fix pattern already
     being established and just not applied everywhere, not a new design decision.

     Verified via a scripted repro: called the real, public `InventorySystem.TryEquipFromRecord()`
     directly (with `Input.mouse` preset over the ability-item slot's bounds, since the method reads
     `Input.MouseBounds` itself but nothing upstream needs simulating) with a dragged Tier-1 Tome
     against a fresh Priest already wearing its starting Tier-0 Tome — confirmed it returns `true`,
     the Priest's `AbilityItem` becomes the Tier-1 Tome, and the old Tier-0 Tome correctly swaps back
     into the dragged record's inventory slot; confirmed a Tome serializes and deserializes cleanly
     through its base `Item` type via `JsonSerializer` with no exception; confirmed a Tome can
     actually be selected across 200 forced tier-0 `AbilityItem` rolls in both `Spawn()` and
     `SpawnGuaranteedLoot()` (with 4 equally-weighted candidate types at that tier, the odds of 200
     rolls never once landing on Tome are effectively zero if the fix is working, and exactly zero
     — impossible — if it isn't, making this a clean pass/fail signal rather than a flaky one). Real
     save files confirmed byte-identical before and after. Clean build and a plain boot-check both
     passed.
176. **Vital Combat's hover tooltip (entry 172) now also shows the Combat Duration value in
     seconds**, per user request. New `Player.CombatDurationSeconds` (`Math.Max(0f, 7f - Vitality *
     0.04f)`) alongside the existing frame-counted `InCombatDurationFrames` — recomputed directly
     from the same formula rather than dividing `InCombatDurationFrames / 60f`, so the display isn't
     affected by that property's own 1-frame floor (a safety clamp for the actual timer, not a
     meaningful value to show — `0.0s` reads fine on a tooltip even though the real timer can never
     literally hit 0 frames). `Overlay.cs`'s tooltip text gained a third line, `"Combat Duration:
     " + CombatDurationSeconds.ToString("0.0") + "s"`, alongside the existing status/Combat Trigger
     lines — no repositioning logic needed, since the tooltip's background panel and its
     already-established anchor-by-measured-width positioning (entry 172's own tooltip-clipping fix)
     both size themselves off `Art.HudFont.MeasureString()` on the whole (now 3-line) string.

     Verified via a scripted repro: confirmed `CombatDurationSeconds` reads `7.0`/`6.0`/`5.0` at
     0/25/50 Vitality, matching the "1 second per 25 VIT" spec directly; confirmed it floors at
     `0.0` (not negative) at an extreme 175 Vitality; rendered `Overlay.DrawSidebar()` to an offscreen
     PNG with the player forced `InCombat` and the mouse hovering the badge, and visually confirmed
     all three tooltip lines ("In Combat" / "Combat Trigger: 8" / "Combat Duration: 6.0s") render
     fully on-screen with no clipping. Real save files confirmed byte-identical before and after.
     Clean build and a plain boot-check both passed.
177. **Fixed the Wand's projectile speed/lifetime/range not matching its own spec** — see
     [BUGFIXES.md](BUGFIXES.md) entry 52 for the user-facing summary. Requested as "make sure the
     wand matches these specs" (18 tiles/sec, 0.5s lifetime, 9 tile range, piercing). Piercing was
     already correct — `Weapon.Shoot()`'s `expiresOnHit = this.Type != WeaponType.Wand && this.Type
     != WeaponType.Bow` already excludes Wand, so its shots already pass through enemies via
     `EntityManager`'s `HitBy`-tracking pass-through, same mechanism Bow uses. Speed/lifetime weren't:
     converted the spec through this project's established 32px/tile, 60 ticks/sec basis (confirmed
     against the already-correct Staff entries, which use `ProjectileMagnitude: 9.6` for the same 18
     tiles/sec target) to `ProjectileMagnitude: 9.6`/`ProjectileDuration: 30` — a clean, exact
     conversion this time (`9.6 * 30 = 288px = 9 tiles` exactly, unlike the Staff's own 0.475s spec,
     which landed on a genuine half-tick boundary requiring a rounding call). All 15 Wand tiers in
     `Data/WeaponData.json` had instead shared `ProjectileMagnitude: 12`/`ProjectileDuration: 32`
     (22.5 tiles/sec, 0.5333s, 12-tile range) — not close to any plausible rounding of the spec, just
     wrong; fixed via a single `replace_all` edit (confirmed beforehand that the exact `12`/`32` pair
     appeared nowhere else in the file, e.g. on a Staff or Bow entry, so the blind replace couldn't
     touch anything else).

     Verified via a scripted repro: confirmed all 15 Wand catalog entries now read
     `9.6`/`30` directly; equipped a Wand-wielding Priest, called the real `Weapon.Shoot()` (no mocks
     — `Input.mouse` preset so `Input.GetMouseAimDirection()` resolves to a real nonzero direction),
     and measured the actual spawned `Projectile`'s `Velocity.Length()`/`Duration`, converting back to
     tiles/sec and seconds the same way the spec itself was converted — landed within floating-point
     rounding of exactly 18 tiles/sec, 0.5s, and a 9-tile range, and confirmed `ExpiresOnHit` reads
     `false` on the real spawned projectile (not just in the source). Real save files confirmed
     byte-identical before and after. Clean build and a plain boot-check both passed.
178. **Double-checked the health regeneration calculation** — reported as "it feels like the player
     is gaining health too quickly." No calculation bug found: `HealthRegenPerSecond`'s formula and
     the `healthCooldown` fractional accumulator (verified correct back in entry 166) both still
     compute and apply exactly as designed, and Vital Combat's IC halving (entry 172) genuinely does
     take effect during real ticks. What the report actually traces to is two VIT-driven effects
     compounding at high Vitality, confirmed directly against the user's own real Level-20 Knight
     save (60 Vitality): at 60 VIT, OOC regen is `2 + 0.2407*60 = 16.44` HP/sec — a full heal from
     near-zero in well under a minute standing still — and even sustained IC only roughly halves
     that to `~9.22` HP/sec. Worse, `CombatDurationSeconds` (`7 - Vitality*0.04`) is *also*
     VIT-driven in the same direction: at 60 VIT a single qualifying hit only holds IC for `4.6`
     seconds before automatically reverting to full-speed regen, so unless the player keeps getting
     hit roughly every 4.6 seconds, most of an actual fight ends up regenerating at close to the
     full OOC rate rather than the halved one. High Vitality simultaneously raises the regen rate
     and shrinks how long combat suppresses it — working exactly as each formula was specified
     individually, but compounding into something that reads as "heals too fast" in practice for a
     tanky, high-VIT build. No code changed as a result — flagged this compounding effect to the
     user as a design/tuning question (their call on whether to adjust it) rather than assuming a
     specific fix, since both formulas already match their own explicit specs.

     Verified via a scripted repro (throwaway `Wizard`, `HealthMax` raised so regen never capped):
     confirmed the formula at 0/20/60 Vitality against hand-computed values, including the 60-VIT
     case matching the real Knight save exactly; ran 600 real `Update()` ticks (10 real seconds) OOC
     and separately with IC continuously refreshed via `RegisterHit()` (reflected directly, not
     `Hit()`, since `Hit()` also deals real damage that would otherwise corrupt the Health
     measurement) — actual HP gained landed within ~0.5 HP of the formula's own prediction in both
     cases (164 vs 164.42 OOC, 92 vs 92.21 continuously-IC), ruling out any double-tick/duplicate-
     accumulator bug; separately reproduced the exact "single hit, no follow-up" scenario behind the
     report and confirmed the actual gain (131 HP) matches a hand-computed blend of 4.6s at the IC
     rate plus the remaining 5.4s at the full OOC rate (131.2 predicted) almost exactly — direct
     confirmation of the compounding explanation above, not a bug.

     Also surfaced (and now documented in `CLAUDE.md`) a fourth real save-triggering path not
     previously on the "back up first" list: setting `Health` to 0 (or calling `Hit()` while `Health`
     is already at/near 0) can drive it negative, triggering `Player.Kill()` ->
     `StateManager.GameOver()` -> `Util.ResetPlayer()` + an unconditional save — which the test's
     first draft of its IC-sustaining loop did by accident (re-using `Hit()` to keep re-triggering
     combat, starting from `Health = 0`), silently resetting the real `PlayerData_Wizard.json`'s
     `ExperienceTotal`/`HighScore`/`HasBeenPlayed` back to a fresh Level 1 character. Caught via the
     routine post-test `diff` against the pre-test backup (per the standing rule — this is exactly
     the case it exists for); restored immediately from that backup and reconfirmed byte-identical
     across every save file. The test was rewritten to drive `RegisterHit()` directly instead of
     `Hit()` for the IC-sustaining loop, avoiding the death path entirely. Also worth noting: a real
     `Realm.exe` the user was actively playing was still running when this task began — testing was
     paused (confirmed via `AskUserQuestion`) until the user closed it, to avoid two processes
     contending for the same save files.
179. **Added a first, deliberately simple biome system**: concentric distance rings around wherever
     the player entered the current Realm instance, each with its own ground tint and enemy subset,
     harder biomes further out. Discussed as an open design question first ("how would we implement
     a biome system?") — found that `EnemySpawner.Update()` already computes `distanceFromEntry`
     every frame (previously only used to scale spawn *density*), so biome *selection* by distance
     turned out to be nearly free to hook in, rather than needing a new mechanic from scratch. The
     user picked concentric rings over angular/sector-based variety, explicitly asking to "keep it
     simple" — which also settled how the ground itself gets drawn: `RealmState.Draw()` previously
     painted one giant tiled rectangle covering the whole world with `Art.Tile`; `DrawBiomeRings()`
     instead paints several full squares (each biome's own `2 × MaxDistance` side length, centered on
     `EnemySpawner.EntryPosition`) back-to-front, largest first — each nearer square's opaque draw
     just overdraws the farther one already there, which is what actually creates the visible rings.
     No real ring/donut geometry, no tilemap engine, nothing new needed beyond one more loop around
     the exact same `spriteBatch.Draw()` call already in the file. New `Data/BiomeData.cs` +
     `Data/BiomeData.json` (`Name`/`MinDistance`/`MaxDistance`/`GroundTileImageName`/`TintR,G,B`/
     `EnemyNames`) — no separate runtime type the way Weapon/Armor/Tome need (`Data/{X}Data.cs` + a
     matching `{X}.cs`), since a biome isn't an equippable `Item` with a texture slot, just config,
     so `Util.LoadBiomeData()` is used directly with no per-entry mapping step. 4 sample biomes
     shipped (Meadow 0-8000, Forest 8000-20000, Highlands 20000-40000, Blighted Wastes 40000+),
     roughly mirroring `EnemySpawner.BasicEnemyPool`'s existing level-unlock order (Snake/Slime ->
     Slime/Seeker -> Seeker/Wanderer -> Wanderer/Brute) — placeholder numbers, easy to retune, same
     as `BasicEnemyPool`'s own level thresholds already were. Every biome currently points at the
     same `Art.Tile` texture and is told apart purely by `TintR/G/B` (a plain multiply-tint on
     `spriteBatch.Draw()`, no new art needed for v1) — the schema already has a per-biome
     `GroundTileImageName` field ready for real distinct ground art whenever that exists, with zero
     further code changes needed to wire it in.

     `EnemySpawner.BasicEnemyPool` gained a `name` alongside its existing `(requiredLevel, factory)`
     pairs, cross-referenced against the current biome's `EnemyNames` — a second, independent gate
     layered on top of the level requirement, not a replacement for it (a biome can't grant early
     access to a still-level-locked type; it only narrows an already-unlocked type down to the rings
     it thematically belongs in). New `GetCurrentBiome()` resolves `Game1.Instance.Biomes` against
     `distanceFromEntry`, falling back to "no filter" if the catalog somehow doesn't cover a given
     distance — a data gap shouldn't be able to stop enemies from spawning outright. `RealmState`
     resolves each biome's texture once in its own constructor (a `List<(BiomeData, Texture2D)>`
     sorted ascending by `MaxDistance`) rather than re-`Content.Load()`-ing every `Draw()` call, and
     gates the whole ring system behind the existing `SpawnsRegularEnemies` flag — `BossRealmState`
     (a small bounded arena, not the open world biomes are meant for) keeps the original single flat
     tile untouched, same as before biomes existed.

     Verified via a scripted repro: confirmed the catalog loads all 4 biomes in the right order;
     confirmed `GetCurrentBiome()` resolves correctly at 8 distances spanning every ring and both of
     its boundaries (`MinDistance` inclusive, exactly at the 8000/20000/40000 edges); confirmed
     `SpawnWave()` (called directly, at max level so only the biome filter is actually being tested)
     spawned exclusively Snake/Slime while positioned in Meadow and exclusively Wanderer/Brute while
     positioned in Blighted Wastes, across 100 calls each — never once producing a PointValue outside
     that biome's own roster; constructed a real `RealmState` (per `CLAUDE.md`, this saves
     unconditionally — save files were backed up first and the one real file it touched,
     `PlayerData_Wizard.json`, was restored and reconfirmed byte-identical afterward) and confirmed
     it populated exactly 4 biome rings; rendered a real frame with the camera centered exactly on
     the Meadow/Forest boundary and confirmed the two sides render visibly different pixel colors —
     the rendered PNG shows a clean vertical seam between a warm brown-tinted Meadow and a
     darker olive-tinted Forest, exactly where the 8000-unit boundary should fall. Clean build and a
     plain boot-check both passed.
180. **Added Beach — the biome system's (entry 179) first real, art-backed biome — plus its basic
     enemy (Pirate) and mini-boss (Beached Buccaneer).** The user supplied 16 enemy sprites under
     `Content/Biomes/Beach/` and asked for Beach to be "the spawn point and easiest in the realm";
     given the scale of designing stats/behavior for 16 brand-new enemy types from scratch, asked
     which should be basic-wave vs. mini-boss vs. held back rather than guessing, and the user chose
     to specify them one at a time, starting with Beached Buccaneer (mini-boss) and Pirate (its
     basic-wave escort) — full stats/attacks/behavior/taunt dialogue given directly. The other 14
     sprites remain unwired (see [BACKLOG.md](BACKLOG.md)'s biome follow-ups entry), waiting on the
     same treatment.

     "Beach... spawn point" reused the biome system's existing 0-8000 ring exactly as Meadow (the
     placeholder biome from entry 179) had occupied — Beach simply replaces Meadow there, tinted
     sandy-tan instead of white. Meadow's own `EnemyNames` (`Snake`/`Slime`) had to go somewhere or
     both types would've become unreachable — `Slime` already appeared in Forest's roster, but
     `Snake` didn't, so Forest picked it up too (`["Snake", "Slime", "Seeker"]`), keeping every
     pre-existing enemy type spawnable.

     **Pirate** (`Enemy.CreatePirate()`, `Enemy.cs`) is a plain factory method — no dedicated class,
     matching Snake's own simplicity (HP 5, PointValue 2, near-identical stats). Its behavior needed
     one new generic coroutine, `ShootIfInRange()`: every other existing attack coroutine
     (`Shoot`/`Spray`/`Bomb`) fires unconditionally once its cooldown is ready, but the spec's "fire a
     single shot... if they get close enough" needed a distance gate none of them have. Added
     alongside them in `Enemy.cs`'s `#region Attack Behaviors` as a reusable building block, not a
     Pirate-only one-off.

     **Beached Buccaneer** (`Bosses/BeachedBuccaneer.cs`) is a genuinely new architectural case for
     this codebase: a tougher `Enemy` that spawns with an escort pack (Snake/BigSnake's exact
     relationship — see `EnemySpawner.SpawnBigSnakePack()`), but with real bespoke behavior (a
     health-phase transition, a randomized dual attack, taunt dialogue) too complex to express as a
     bare factory method composing only the shared generic coroutines the way `CreateBigSnake()`
     does. Given `AddBehaviour`/`AddAttackBehaviour`/`HealthFraction`/`FlashRed()` are all already
     `protected` on `Enemy` itself (not `Boss` — confirmed via a dedicated research pass before
     writing any code), a plain `Enemy` subclass, structured like `LimonTheSpriteGoddess` (a `Boss`)
     but inheriting `Enemy` directly, was the right fit: no portal/arena (a `Boss` would suppress the
     normal in-world health bar and expect a `BossRealmState` HUD instead, neither of which this
     mini-boss wants).

     Mapped onto existing mechanics wherever one already fit, rather than building new systems:
     "becomes Wooden Shield Armored" -> `Defense += 2`, borrowing the Tier 0 Wooden Shield's own
     `DefenseBonus` from `Data/ShieldData.json` directly rather than inventing a new number; "red AoE
     grenades" -> the existing `GrenadeProjectile` class (already fully built, previously unused by
     anything — its telegraph-then-arm-red visual already *is* "red AoE grenade", no new art or class
     needed); the AoE's target position clamped to its own Range from the boss, same clamp-to-range
     pattern as Priest's Tome nova (`CharacterClasses/Priest.cs`); "walks aimlessly... until
     approached, then chases" -> a one-way latch driving `MoveTethered()`'s and `FollowPlayer()`'s own
     enumerators directly (calling `.GetEnumerator()` once and stepping whichever is active each
     tick) rather than running both simultaneously, which would just sum their two `Velocity`
     contributions instead of switching between them; "with either white bolts or red AoE grenades"
     -> one shared attack cooldown (the AoE's own stated 2-second `Cooldown` — the projectile attack
     had no separately-stated one) randomly choosing between the two each time it fires.

     The taunt dialogue needed a genuinely new piece: no floating-text mechanic in this codebase
     supports a multi-word wrapped sentence anchored to an arbitrary enemy (`DamageNumber`'s
     `FollowsPlayer` only ever tracks `Player.Instance`, and single damage/XP numbers never needed
     `Util.WrapText`). New `TauntBubble.cs`, modeled on `DamageNumber`'s live-follow shape but
     purpose-built — and deliberately does *not* call `Util.DrawTooltip` for its background panel
     despite the visual similarity, since that helper's `ClampTooltipX` assumes screen-space HUD
     coordinates and would silently mis-position a bubble rendered in world space. New
     `Enemy.TauntWhenPlayerNear()` (generic, alongside `ShootIfInRange` above) drives it periodically
     while a player is in range. All 5 taunt lines plus the 50%-health enrage line are the user's own
     text, reproduced verbatim, typos and dialect spelling included — not "corrected," since there
     was no way to tell an intentional pirate-speak misspelling from an actual typo, and the user's
     exact words were what got sent.

     `EnemySpawner.SpawnBeachedBuccaneerPack()` mirrors `SpawnBigSnakePack()`'s exact shape (interval,
     escort count, anchor+offset clustering) but — unlike BigSnake, which fires regardless of
     location — is gated behind `GetCurrentBiome()?.Name == "Beach"` in `Update()`, since a beach
     pirate showing up in the middle of Blighted Wastes would be jarring. `Data/Content.mgcb`/`Art.cs`
     register the two new sprites under their real location, `Biomes/Beach/` (not `Enemies/`, which
     is where every prior enemy's art has lived — the user's own folder choice), plus
     `Projectiles/white_bolt.png`, which existed unbuilt on disk since the very start of this session
     (visible in git status from turn one) but had never been wired into `Content.mgcb`/`Art.cs`
     until now.

     Verified via a scripted repro: confirmed Beach's catalog entry occupies the old Meadow slot with
     exactly `["Pirate"]` as its roster, and that Forest's roster now includes `Snake`; confirmed 100
     `SpawnWave()` calls at Beach's entry point produced only Pirate (`PointValue` 2), never anything
     else; confirmed Pirate's stats and that it genuinely doesn't fire while the player is far away
     but does once in range; confirmed Beached Buccaneer's stats, that it doesn't attack pre-aggro,
     that aggro+an attack (bolt or grenade) both fire once the player closes to range, that a taunt
     bubble spawns, and that `WasShot()` down past 50% health raises `Defense` from 2 to 4 and fires
     the enrage taunt; confirmed the real `EnemySpawner.Update()` (not the private spawn method
     directly, which would have bypassed the very gate under test) spawns a Buccaneer+Pirates while
     the player is in Beach and nothing at all while in Forest. An offscreen render caught the
     `FollowPlayer()` `NaN` bug above (entry/[BUGFIXES.md](BUGFIXES.md) entry 53) — the render came
     back completely blank, which numeric assertions alone hadn't caught, since `IsExpired` was still
     `false` and every stat check still passed on a `NaN`-positioned entity. After that fix, a
     second render confirmed both sprites and a real taunt line rendering correctly together. Real
     save files confirmed byte-identical before and after. Clean build and a plain boot-check both
     passed.

## 2026-08-25

181. **Added Beach's second basic enemy (Bandit) and second mini-boss (Bandit Leader)**, continuing
     the same one-at-a-time spec-and-implement pattern as entry 180. Full stats/attacks/behavior/
     taunt dialogue given directly by the user.

     Bandit (HP 50/DEF 1/EXP 5) reads its two listed attacks as one shared-cooldown mechanic rather
     than two independent attacks: a shorter-range dagger stab (3.6 tiles, +1 damage) that replaces
     the longer-range shot (6 tiles) once the player closes distance, matching the spec's explicit
     "they only use it when you get close." Uses the existing `FollowPlayer()` coroutine — the
     "will not track your movement" flavor text describes the game's existing straight-line,
     non-homing projectiles rather than a new mechanic. "Protects: Bandit Leader" is read as a
     pack-relationship label (matching Pirate/Beached Buccaneer's existing pattern) rather than a
     distinct bodyguard AI. New file `Bandit.cs` at the project root (not `Bosses/`) — a deliberate
     folder distinction from the mini-boss-tier dedicated-subclass files, since this one is
     basic-tier.

     Bandit Leader (HP 280/DEF 2/EXP 88) reads its two listed attacks as genuinely concurrent (own
     independent cooldowns) rather than Beached Buccaneer's mutually-exclusive random choice — a
     deliberate re-reading based on the differing phrasing ("attacking... [and] throwing" vs. "with
     either X or Y"). Its "runs away... when low on health" behavior needed a new reusable
     coroutine, `Enemy.FleePlayer()` — a mirror image of the existing `FollowPlayer()`, same
     zero-vector guard, just accelerating away instead of toward. No percentage was given for "low
     on health," so 25% was chosen (lower than Beached Buccaneer's spec'd 50% enrage point, since
     fleeing is a more drastic response than a temporary buff) — flagged as a tunable constant, not
     a spec'd value. Its AoE grenade throw reuses the pre-existing, previously-unused
     `GrenadeProjectile` class, same as Beached Buccaneer's. The "Catch!" taunt fires on only 35% of
     throws (not every one) to avoid reading as spam over a sustained fight — also tunable. Neither
     enemy's projectile art was specified this time (unlike Beached Buccaneer's explicit
     `white_bolt.png`), so both reuse the already-loaded `Art.SwordSlash` as a thematic fit — flagged
     to the user rather than assumed silently correct.

     New reusable `Enemy` coroutines added alongside `FleePlayer()`: `ShootIfInRange()` (a
     distance-gated single shot, unlike the unconditional `Shoot`/`Spray`/`Bomb`) and
     `TauntWhenPlayerNear()`'s sibling usage confirmed still generic enough for a second consumer.
     New dedicated-subclass file `Bosses/BanditLeader.cs`, mirroring `BeachedBuccaneer.cs`'s shape
     (bespoke instance state — a one-time flee latch, its own AoE cooldown — that doesn't fit the
     generic coroutines alone). `EnemySpawner.SpawnBanditLeaderPack()` mirrors
     `SpawnBeachedBuccaneerPack()`'s shape, gated the same way behind `GetCurrentBiome()?.Name ==
     "Beach"`. `Data/BiomeData.json`'s Beach roster now reads `["Pirate", "Bandit"]`.
     `Content.mgcb`/`Art.cs` register both sprites under `Biomes/Beach/`.

     Found and fixed a real crash bug during scripted testing: see
     [BUGFIXES.md](BUGFIXES.md)'s SpriteFont-glyph entry — a literal Unicode ellipsis in Bandit
     Leader's flee taunt would have thrown an uncaught `ArgumentException` and crashed the entire
     game the instant a real player triggered it. Fixed the specific string and added a
     general-purpose `TauntBubble.SanitizeForFont()` defensive sanitizer for all future taunt text.

     Verified via 25 scripted checks: Beach's roster reflects both enemies; Bandit's stats and its
     dual-range attack (dagger damage at close range, lower damage at mid range, no fire beyond
     range); Bandit Leader's stats, that it chases while healthy and flees once at/below 25% health,
     that the flee taunt fires, that the sanitizer neutralizes unsupported characters without
     throwing, that its AoE grenades fire with the right damage and the "Catch!" taunt appears over
     repeated throws; and the real `EnemySpawner.Update()` spawning a Bandit Leader pack (with Bandit
     escorts) while in Beach and nothing while in Forest. A render confirmed both new sprites draw
     correctly. Real save files confirmed byte-identical before and after. Clean build and a plain
     boot-check both passed.

182. **Added Beach's third mini-boss (Scorpion Queen) and third basic enemy (Little Scorpion)**,
     continuing the same one-at-a-time spec pattern as entries 180-181, but structurally different
     from both prior pairs: the Queen "does not attack" at all (no `AddAttackBehaviour` calls
     whatsoever — confirmed via a reflection check on her `attackBehaviours` list, not just by
     absence of incoming damage), and rather than `EnemySpawner` spawning her escort pack directly
     (`SpawnBeachedBuccaneerPack()`/`SpawnBanditLeaderPack()`'s shape), the Queen manages her own
     escort of Little Scorpions internally: 10 spawned immediately in her constructor, then a slow
     trickle (one every ~5s, not an instant top-up) replacing any that die, via a new
     `MaintainScorpions()` coroutine. `EnemySpawner.SpawnScorpionQueenPack()` (gated to Beach same as
     the other two) now just drops the Queen alone — no separate escort loop needed.

     Little Scorpion (HP 10/DEF 0/EXP 2) "wanders around close to the Scorpion Queen" needed a real
     mechanic, not just flavor text (contrast Bandit's "Protects: Bandit Leader," read as a label
     only) — `Enemy.MoveTethered()` gained an optional `anchor` parameter so a wander can leash to
     another live Enemy's current Position instead of just its own spawn point, re-read every frame.
     Backward-compatible default (`anchor: null`) preserves every existing self-tethered caller
     (`BeachedBuccaneer`, `SthenoTheSnakeQueen`). Verified this actually tracks a moving anchor (not
     just approximates it) by spawning a scorpion intentionally far from the Queen and confirming it
     closes the distance over time — with the old fixed-origin behavior it would have had no reason
     to move toward her at all. Little Scorpion's single-shot, range-gated attack (Damage 7, Range 8
     tiles) reuses `ShootIfInRange()` directly; "closest player" is read the same way every other
     enemy's aim logic already is, since this game has no multiplayer. Not added to
     `EnemySpawner.BasicEnemyPool` (unlike Pirate/Bandit) — it never appears standalone, only ever
     spawned by a live Queen to tether to, so `Data/BiomeData.json`'s Beach roster is unchanged this
     time. New files `Bosses/ScorpionQueen.cs` and `LittleScorpion.cs` (project root, basic-tier,
     matching `Bandit.cs`'s folder convention).

     Found and fixed a real bug in `MaintainScorpions()` during scripted testing: its "how many
     scorpions does the Queen have left" count didn't filter out already-dead ones
     (`!s.IsExpired`), relying on `EntityManager`'s end-of-frame purge to remove them from the list
     first. In real gameplay that purge always runs before the Queen's coroutine sees stale data
     again next frame, so the practical impact there is a harmless one-frame lag — but a test that
     ticks an enemy's `Update()` directly (this project's established scripted-test pattern, see
     CLAUDE.md) never triggers that purge at all, so the coroutine saw the same 10 "alive" (but
     actually dead) scorpions forever and never replaced them. Added the missing filter directly in
     `ScorpionQueen.MaintainScorpions()`'s count so the accounting is correct by construction instead
     of by incidental call order.

     Also chased down why a first render came back showing only the player sprite, nothing else, no
     exception anywhere: a freshly-constructed `Enemy` starts fully transparent
     (`color = Color.Transparent` in its own constructor) and only fades to opaque across its first
     60 `Update()` ticks — every entity used in this session's earlier scripted renders had already
     been ticked past that window for other assertions before being rendered, so this never surfaced
     until a render step used entities that were *only* ever constructed, added, and immediately
     drawn. Not a bug — working as designed (the same fade-in every enemy gets on a real spawn) — but
     worth a note here since it cost real debugging time: any future render-confirmation test needs
     to tick new entities ~60+ times before drawing them, or they'll silently render as invisible
     with no error to point at why.

     Verified via 21 scripted checks: the Queen's stats and that she has zero attack behaviours;
     that she spawns with exactly 10 Little Scorpions immediately; that she wanders but stays bounded
     near her spawn point; Little Scorpion's stats and its range-gated single shot; that a scorpion
     spawned far from the Queen closes the distance (anchor-based tether); that killing all of the
     Queen's scorpions is followed by zero immediate respawns and exactly one after the slow
     interval; and the real `EnemySpawner.Update()` spawning a Scorpion Queen while in Beach and none
     while in Forest. A render confirmed both new sprites draw correctly once ticked past their
     spawn fade-in. Real save files confirmed byte-identical before and after. Clean build and a
     plain boot-check both passed.

183. **Added Beach's fourth mini-boss (Sandsman King) and its two escort types (Sandsman Archer,
     Sandsman Sorcerer)**, continuing the same one-at-a-time spec pattern as entries 180-182. Full
     stats/attacks/behavior given directly by the user; this batch also explained the three
     previously-unwired projectile sprites (`Green Arrow.png`, `Purple Mystic Shot.png`,
     `Dark Blue Magic.png`) sitting untracked in `Content/Projectiles/` since earlier this session.

     The King introduces a genuinely new shape: a *separate* Trigger Range (10 tiles, gates the
     wander-vs-chase switch) from Attack Range (8.4 tiles, gates the shot itself) — mirrors
     BeachedBuccaneer's `WanderThenChase()` one-way aggro latch, renamed `AggroWatcher()`. Since
     Attack Range < Trigger Range, a player close enough to actually get shot has necessarily already
     crossed the trigger, so the attack didn't need its own separate aggro check. His two escort
     types (`Spawns: Sandsman Archer (Max: 2, Cooldown: 10s), Sandsman Sorcerer (Max: 3, Cooldown:
     8s)`) read as *no* initial burst, unlike ScorpionQueen's explicit "spawns with 10" — both start
     at 0 and fill in gradually, one per their own stated cooldown, since only Max/Cooldown were
     given this time. Two near-identical `MaintainArchers()`/`MaintainSorcerers()` coroutines
     (deliberately not merged into one generic helper — two ~15-line bodies within a single file
     didn't clear the bar for that abstraction).

     `Enemy.ShootIfInRange()` gained an optional `cooldownFrames` parameter — the King's 10s and the
     Archer's 1s attack cooldowns are both far from the shared 250-tick(~4.2s) default every existing
     caller (Pirate, Little Scorpion) relies on, and that field is private, not protected, so a
     per-call override was the only way to keep using the shared helper instead of hand-duplicating
     its aim/fire logic a third and fourth time. `cooldownFrames: null` (the default) is a byte-for-
     byte no-op for every existing caller.

     Sandsman Archer's "orbits around Sandman King firing arrows at any player close to it" reuses
     the same live-recenter-every-frame technique as `SthenoPet.Orbit()` (Bosses/SthenoPet.cs), written
     bespoke here rather than promoted to a shared Enemy.cs helper (Stheno's own file isn't being
     touched this session). Its Attacks block lists Range (11.9 tiles) *larger* than Trigger Range (10
     tiles) — the reverse of the King's own — but the Archer never has a separate "notice, then react"
     state (always orbiting, always alert), so Trigger Range has no distinct mechanical role for it;
     Range alone drives its `ShootIfInRange`. Flagged as an interpretation call, not silently dropped.

     Sandsman Sorcerer's two listed attacks ("wanders aimlessly firing purple... once approached fires
     a fast and strong short ranged dark blue...") read as the same distance-based
     closer-range-replaces-farther-range mechanic as Bandit.cs's dagger/ranged split — one shared
     cooldown, Dark Blue Magic checked first (closer, stronger), falling back to Purple Mystic Shot.
     No Cooldown was given for either Sorcerer attack (unlike the King/Archer, which both state one
     explicitly) — falls back to the same 250-tick default Enemy's own Shoot()/Spray()/
     `ShootIfInRange()` already use elsewhere, flagged as a judgment call. Sorcerer's own "wanders
     aimlessly" has no stated anchor (unlike Little Scorpion's explicit tie to the Queen), so it uses
     a self-tethered `MoveTethered()` around its own spawn point. Neither escort was added to
     `EnemySpawner.BasicEnemyPool` (same reasoning as Little Scorpion) — both only ever appear as the
     King's own escorts, so `Data/BiomeData.json`'s Beach roster is unchanged again this round.

     Scripted testing surfaced two real timing/robustness gaps — not in the enemies' own logic, but
     in how the test verified them, worth recording since the same shape will bite the next
     escort-heavy enemy's test too: (1) an orbiting or wandering enemy's Position keeps moving *during*
     a fire-range test loop, so a Player.Instance.Position set once before the loop can drift back out
     of range before the shot lands — fixed by re-anchoring the player's position to the moving
     enemy's current Position on every iteration instead of once up front; (2) `Game1.GetWorldBounds()`
     (the on-screen gate `Enemy.Update()` checks before running any attack coroutine at all) is
     centered on `Camera.Pos`, and `Camera.Pos`'s own setter clamps to a world-edge minimum around
     (490, 360) — a test enemy spawned near the actual world origin (`Vector2.Zero`, this session's
     go-to test position through entry 182) can never truly be camera-centered no matter what
     `Camera.Pos` is explicitly set to, so its on-screen status silently depends on unrelated
     positioning instead of the camera call that looks like it should control it. Fixed by spawning
     the test King well away from the origin (`(5000, 5000)`) and explicitly re-centering
     `Camera.Pos` on him immediately after construction. Both together explained an initially
     confusing intermittent failure (passed most runs, failed roughly 1 in 3) that had nothing to do
     with the actual attack logic under test.

     Verified via 34 scripted checks across all three enemies' stats, the King's wander→aggro→attack
     progression and its own Attack-Range gate, both escort types' gradual no-burst spawn-and-cap
     behavior, the Archer's orbit (constant radius, changing angle) and range-gated shot, the
     Sorcerer's dual-range attack escalation, and the real `EnemySpawner.Update()` spawning a
     Sandsman King while in Beach and none while in Forest — repeated across 8 consecutive runs after
     the two fixes above to confirm the earlier flakiness was actually gone, not just not-observed
     once. A render confirmed all three new sprites draw correctly. Real save files were re-verified
     byte-identical after the test run; one file (`PlayerData_Wizard.json`) differed on first check —
     only in the equipped Weapon/Armor/Ring/Spell's own instance `ID` GUIDs, not in any level/XP/stat
     data — and was restored from backup. That specific diff shape (equipped-item IDs regenerating,
     nothing else) looked like a pre-existing save/load quirk unrelated to this session's changes, not
     something introduced here; flagged to the user rather than silently written off. Clean build and
     a plain boot-check both passed.

184. **Added Beach's fifth mini-boss (Giant Crab)**, continuing the same one-at-a-time spec pattern
     as entries 180-183. No escort this time (no "Spawns:" field) — a single `Bosses/GiantCrab.cs`.

     `Green Arrow.png`/`Purple Mystic Shot.png`/`Dark Blue Magic.png` turned out to be the last of
     that mid-session art drop (see entry 183) — Giant Crab's own dedicated `Beam.png`/`Blue Bolt.png`
     appeared partway through this entry's own work, discovered as untracked files rather than
     mentioned directly. Initially wired using already-loaded placeholders (`Art.WhiteBolt`/a
     newly-added `Art.BlueMagic` reading the pre-existing-but-unused `blue_magic.png`) before
     noticing the real assets sitting untracked; swapped over once found, `Art.BlueMagic` removed
     again since nothing else used it. Re-verified with a second, narrower render check after the
     swap rather than the full suite (pure asset-reference change, no logic touched) — worth its own
     mention since even that narrower check needed two more fixes before the art was actually visible:
     `Camera.Pos` has to be centered on the enemy *before* any ticking starts (constructing
     `NexusState` sets it from whatever `Player.Instance.Position` was left over from the save file,
     nowhere near a freshly-placed test enemy, and every attack coroutine — not just movement — is
     gated on `Game1.GetWorldBounds()` reading that same `Camera.Pos`), and a spawned projectile needs
     its own `Update()` ticked individually to actually move — `crab.Update()` only advances the
     crab's own coroutines, not the projectiles it spawned, which just sit invisibly stacked under
     its own (much bigger) sprite otherwise. The corrected render matches the spec's "shockwave-like
     blast" description well: four white bars fanned out from the crab at staggered distances, exactly
     from the four beam tiers' differing speeds.

     Introduces Aim Tracking — the crab's main attack fires at the player's *predicted* Position
     (current `Player.Instance.Position` plus current `Player.Instance.Velocity` extrapolated a fixed
     30 ticks forward, applied once at the exact moment a wave fires, not continuously re-aimed),
     the first Beach enemy to do this. No specific lookahead value was given in the spec, so 30 ticks
     is a judgment call. The spec's four "Beam" rows (Damage 1/4/7/11, Speed 2/4/6/8 tiles/sec, Range
     0.4/1.6/3.6/6.4 tiles) read as one simultaneous 4-projectile volley, not four independent
     attacks — "a shockwave-like blast... if all four connect" only makes sense as one wave, and
     Range÷Speed gives a suspiciously clean linear duration progression (0.2s/0.4s/0.6s/0.8s at 60fps
     → 12/24/36/48 ticks), which is what actually produces the spreading "shockwave" look: all four
     fire from the same point at the same instant toward the same predicted spot, and the
     faster/farther-reaching ones simply outlast the slower/shorter ones. `EnemyProjectile.duration`
     is set explicitly per tier rather than left at its 250-tick default.

     The separate "Blue Bolt" row (Damage 10, Speed 7, Range 12.6) explicitly "will not track your
     movement" — aimed at the player's real, current Position each shot, the opposite of the beam
     wave. The two attacks alternate on a timer (`Phase.Beam`/`Phase.BlueBolt`, mirroring
     `SthenoTheSnakeQueen`'s time-based phase cycling more than the health-threshold style seen in
     `BeachedBuccaneer`/`LimonTheSpriteGoddess`) — "occasionally... at a frequent pace" gives no
     explicit phase durations or per-shot cooldowns for either state, so Beam's 8s duration/1.5s
     volley cooldown and Blue Bolt's 2.5s duration/0.33s shot cooldown are all judgment calls,
     tunable. "When they spot you, they will chase" implied a real pre-aggro wander state (unlike
     Pirate/Little Scorpion's always-on range-gated fire) — same one-way `AggroWatcher()` latch as
     BeachedBuccaneer/SandsmanKing, using Blue Bolt's own Range (12.6 tiles, the single largest
     number given) as the "spot" trigger since no separate detection range was stated.

     Scripted testing surfaced two more test-only timing traps, worth recording alongside entry 183's
     (this is now the second mini-boss in a row where the test's own setup — not the enemy's actual
     logic — was the source of an early failure): (1) `FireBeamWave()` deliberately has no distance
     gate (each beam's own short duration already limits its reach), which means the very first tick
     aggro triggers on *also* fires a real wave in that same `Update()` call (`ApplyBehaviours()` sets
     `hasAggroed` immediately before `ApplyAttackBehaviours()` runs, same tick) — consuming the
     90-tick volley cooldown before a later, deliberately-staged test ever got to run, and since the
     wave keeps auto-firing every 90 ticks forever, no fixed number of ticks can reliably land on a
     freshly-cleared cooldown. Fixed by polling for the *next* wave to actually fire (via a
     projectile-count change) and setting up the controlled Position/Velocity scenario immediately
     before it, rather than trying to out-guess the timing with a fixed wait. (2) Checking a cycling
     phase once after a long fixed wait can catch it after it already cycled all the way back around
     — a 900-tick wait against a phase that was already partway through its own duration completed a
     full Beam → Blue Bolt → Beam lap and read back as "still Beam," which looked like a failure to
     ever switch at all. Fixed by polling every tick and stopping the instant the phase actually
     flips, rather than checking once at the end of an arbitrary wait.

     Verified via 18 scripted checks: stats; the wander→aggro transition at Spot Range; the beam
     wave's exact 4-projectile count, damage set `{1,4,7,11}`, and duration set
     `{12,24,36,48}` ticks; that the wave's aim genuinely tracks the player's predicted position
     (confirmed against a deliberately large sideways `Velocity`) rather than their current one; the
     Beam → Blue Bolt phase transition; that Blue Bolt fires with damage 10 and aims at the player's
     literal current position even under a deliberately huge `Velocity` that a predictive aim would
     have badly missed with; and the real `EnemySpawner.Update()` spawning a Giant Crab while in
     Beach and none while in Forest — all stable across 6 repeated runs after the two fixes above. A
     render confirmed the new sprite draws correctly. Real save files confirmed byte-identical after
     this round. `PlayerData_Wizard.json` needed restoring again after the later art-swap
     verification pass, same known equipped-item-ID quirk as entry 183 (see the standing
     investigation task) — this time double-checked that the surrounding Level/ExperienceTotal/stat
     values genuinely matched the backup field-by-field before concluding it was the same quirk and
     not a real regression, rather than trusting the diff's truncated first line. Clean build and a
     plain boot-check both passed.

185. **Added Beach's five "Regular Enemies"** — Little Blue/Green/Pink Jelly, Piratess, and Sand
     Devil — the largest single batch this biome has gotten, and the first labeled "Regular Enemies"
     rather than mini-boss/escort pairs. This is also the last of the original 16-sprite Beach art
     drop except Greedy Crab (see [BACKLOG.md](BACKLOG.md)).

     The three Jellies introduce a genuinely new spawn shape: "spawns in groups of 2-7... Mean 5,
     Std. Deviation 1" — a same-type cluster sized from a real Gaussian, not the fixed escort counts
     or uniform 2-4 mixed waves every earlier pack used. `System.Random` has no built-in normal
     sampler, so `EnemySpawner.SampleGroupSize()` implements one directly (Box-Muller transform),
     rounded and clamped to the spec's own stated [2, 7]. Verified with 2000 samples rather than a
     handful — a statistical claim like "mean close to 5" needs a large enough sample to mean
     anything; a small one could pass or fail on pure luck either way. All three Jellies share one
     `EnemySpawner.SpawnGroupPack(factory)` helper (their own dedicated pack interval each, gated to
     Beach) instead of three duplicated pack methods — a second and third real consumer of the exact
     same shape justified generalizing it immediately rather than writing it once, then twice, then
     refactoring on the third (this session's usual bar is "generalize on the second real need";
     three arriving in the same message made that moot). Deliberately not part of
     `EnemySpawner.BasicEnemyPool` — "spawns in groups" already fully describes how each one ever
     appears, so adding them there too would double up two different spawn mechanisms for the same
     enemy.

     Little Blue Jelly's "V-shape pattern" (2 shots, Angle 10°) and Little Green Jelly's "star shape"
     (5 shots, Angle 72°) turned out to be the same underlying formula: a new `Enemy.FanShot()`
     coroutine (mirroring `ShootIfInRange()`'s range/cooldown-override shape, generalized the same
     deliberate way as the Jelly pack helper) fires `shots` projectiles spaced `angleStep` apart,
     centered on the aim direction. For 2 shots at a small step that reads as a narrow V; for 5 shots
     at exactly 360°/5 = 72°, centering becomes irrelevant and the same formula produces a full,
     aim-independent 5-point star — confirmed by checking that all 5 fired angles are exact multiples
     of 72° apart from each other, not by checking any specific rotation. Little Pink Jelly has no
     Shots/Angle at all (a single shot), so it just reuses `ShootIfInRange()` directly rather than
     calling `FanShot()` with `shots: 1`. All three Jellies' "Aim: 0.2" matches the existing
     `rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f)` jitter already baked into every
     shot-firing coroutine in this codebase — read as confirming/reusing that exact existing spread
     rather than a new tunable parameter. None of the three states an explicit Wander Speed that maps
     cleanly onto `MoveTethered()`'s own accel-per-tick scale (0.05-0.2 for every "lazy"/idle wanderer
     this session) — Green/Pink's stated "Wander Speed: 4" would be 20-40x that if taken as a literal
     conversion input, so all three instead reuse the same slow-drift value established by
     BeachedBuccaneer/SandsmanKing's own pre-aggro wander; flagged as an interpretation call, not a
     silent substitution.

     Piratess and Sand Devil, by contrast, are ordinary `BasicEnemyPool` entries (added to
     `Data/BiomeData.json`'s Beach roster alongside Pirate/Bandit) — no grouping, no escort tie.
     Piratess is Pirate's near-twin (HP 6 vs. 5, otherwise identical stats) but got its own dedicated
     class file rather than a bare `Enemy.CreateX()` factory, matching this session's more recent
     convention (Bandit.cs, LittleScorpion.cs) instead of Pirate's older one; unlike the original
     `CreatePirate()`, its projectile speed is properly tiles/sec-converted rather than left as a raw
     px/tick value, and it explicitly uses `Art.SwordSlash` (the spec's own stated aesthetic) instead
     of `CreatePirate()`'s implicit default projectile.

     Sand Devil is the most mechanically involved "regular" enemy yet — a real two-phase cycle
     (Chase/Circle) despite being basic-tier, a pattern every other use of this session was a
     mini-boss. Chase pursues the player via `FollowPlayer()` while firing, but swaps to
     `MoveRandomly()` (widened from `private` to `protected` — previously only `CreateWanderer()`
     used it internally) instead of continuing to close in once within 2 tiles ("it will wander
     erratically if it moves within 2 tiles of the player") — a live per-tick check, not a one-time
     latch, so it can resume a real chase if the player backs away again mid-phase. After 3 seconds it
     switches to Circle, which repositions directly onto a fixed 3-tile ring around the player each
     tick (same direct-`Position`-overwrite technique as `SthenoPet.Orbit()`/`SandsmanArcher.Orbit()`)
     rather than accelerating into place, so the transition into circling is immediate. Circle never
     attacks — the spec's own description of it never mentions firing, reading as a pure
     repositioning/breather window. Rotation direction and rate ("rotate clockwise for 3 seconds")
     aren't numerically specified — confirmed that increasing angle reads as clockwise in this
     engine's Y-down screen space by checking `Extensions.FromPolar()`'s plain cos/sin, then picked
     one full lap over the 3-second phase as a clean, deliberate circle (not a slow creep or a
     dizzying spin); both flagged as tunable judgment calls.

     "Wavy shots" (a comment, no numbers) needed a genuinely new projectile behavior no existing class
     had: `WavyProjectile.cs`, computing Position directly each tick from distance traveled plus a
     perpendicular sine offset (rather than accumulating a wave-modulated Velocity, which would trace
     a driftier S-curve instead of a clean sine wave), with `Velocity` zeroed before calling
     `base.Update()` so its own `Position += Velocity` doesn't double-move it on top of the
     hand-computed position — `EnemyProjectile`'s shared duration/off-screen-expiry bookkeeping still
     runs unmodified underneath. Kept as its own dedicated subclass (matching `GrenadeProjectile`'s
     precedent) rather than adding wave parameters to `EnemyProjectile` itself, since this is the only
     enemy that needs it so far.

     Verified via 44 scripted checks: all five enemies' stats; Blue Jelly's out-of-range gate and its
     V-shot's exact 2-projectile count/damage/10°-angle-gap; Green Jelly's out-of-range gate and its
     star's exact 5-projectile count/damage, confirmed all 72° apart from each other; Pink Jelly's
     out-of-range gate and single-shot damage; Piratess chasing (closing distance over time),
     out-of-range gate, and shot damage; Sand Devil's phase starting state, that it closes distance
     while chasing, that it fires a real `WavyProjectile` with the right damage, that the projectile's
     path actually deviates from a straight line over time (not just that it was constructed), that
     forcing Circle phase (via reflection, skipping the real 3-second wait) holds it at ~3 tiles from
     the player, and that it doesn't fire at all during Circle; `SampleGroupSize()`'s sample mean and
     range bounds over 2000 draws; the real `EnemySpawner.Update()` spawning a 2-7-sized Little Blue
     Jelly group while in Beach and none while in Forest; and Piratess/Sand Devil actually appearing
     across 200 real ambient wave rolls. All 44 checks passed cleanly on the first run and stayed
     stable across 3 repeats — the camera-centering, far-away-player, and per-tick-re-anchoring
     lessons from entries 183-184 applied proactively from the start this time, rather than discovered
     mid-test again. A render confirmed all five new sprites draw correctly together. Real save files
     confirmed byte-identical before and after (no restore needed this round). Clean build and a plain
     boot-check both passed.

186. **Reworked the Fame system**: Base Fame now accrues automatically from a character's own
     cumulative XP throughout its life (previously Fame was a single flat account-wide `int` that
     only ever changed via a 1:1 conversion of `ExperienceTotal` at death/delete — see
     `FameSystem.cs`, `States/GameOverState.cs`, `Util.DeleteCharacterData()`). Went through
     `EnterPlanMode`/`ExitPlanMode` given the size (7 files, several real design decisions) — first
     surfaced two scope questions before planning: whether the existing 5-star rating (`Player.
     ComputeStars`, `HighScore`-doubling-based, entry 81) should be replaced by the new spec's
     Fame-based "Class Quests" or kept as a separate second tracker (user chose: replace), and how
     much of the spec's wider XP-formula rework (HP/10 base-XP fallback, next-level %-caps,
     Exaltations, XP Boosters, dungeon-wide modifiers) to build now (user chose: the core Fame
     rework plus the next-level XP cap, not Exaltations/Boosters/dungeon modifiers — none of those
     three exist anywhere in this codebase, and each is its own separate, much larger system).

     `Player.ComputeBaseFame(int experienceTotal)` (new, static + pure, same shape as `ComputeStars`)
     converts XP to Fame at 1-per-900 before Level 20's XP threshold, 1-per-2000 after — the
     boundary is computed live from the existing `CumulativeExperienceForLevel(20)` (19,850 XP under
     this engine's own leveling curve) rather than the spec's literal "18050," since this game's
     `ExperienceRequiredForLevel` formula was never RotMG's and hardcoding either number risked the
     two silently drifting apart if the curve is ever retuned. `Player.BaseFame` is a plain derived
     property (`ComputeBaseFame(ExperienceTotal)`) — not a separately-persisted field, since it's a
     pure function of already-persisted state, same reasoning as `ExperienceNextLevel`. New
     `Player.BonusFame` (persisted, `PlayerData.BonusFame`) is real infrastructure with nothing
     plugged into it yet — "obtained from certain achievements" per the spec, but no such achievement
     exists in this codebase today, so it's a plain per-life counter (`AddBonusFame()`) sitting at 0
     until something calls it.

     `ComputeStars` (renamed in spirit to "Class Quests," same method name) dropped its
     `hasReachedLevel20` gate entirely and now thresholds `ComputeBaseFame(highScore)` against
     `{20, 500, 1500, 5000, 15000}` instead of `HighScore` doubling from 20,000. Reusing `HighScore`
     (not live `ExperienceTotal`) is what keeps stars permanent across death — `HighScore` already
     survives death/delete, and Base Fame is monotonic in XP, so "the most Base Fame ever displayed"
     is exactly `ComputeBaseFame(HighScore)`, no new persisted star count needed. **Real behavior
     change, flagged directly**: since Star 1's threshold (20 Fame ≈ 18,000 XP) now lands slightly
     *before* the actual Level 20 XP threshold (19,850) under this curve, a character can earn Star 1
     without ever reaching the level cap — a deliberate reading of "gaining certain amounts of Fame
     during your character's lifetime," not an oversight. Every existing save's displayed star count
     recomputes automatically the moment this ships (no migration needed, `ComputeStars` was always
     a pure function with no separately-stored count).

     `Enemy.WasShot()`'s XP award gained a cap step before any multiplier: `PointValue` (this
     engine's own "specified base XP value, a parameter found in the game XML," per the spec's own
     wording) is capped to `NextLevelXpCapFraction` (new protected field, 0.1 for every enemy today)
     of the XP needed for the player's *next* level, matching the spec's own worked example
     precisely (cap first, multiplier after — a multiplier can still push the final total back above
     what the cap alone allows). Applies the same way at Level 20 itself, where "next level" is a
     theoretical 21 that's never reachable — the cap still meaningfully limits farming a
     high-`PointValue` enemy at the level cap. **Real balance change, flagged directly**: a Level 1
     character killing a Giant Crab (`PointValue` 86) used to net the full 86 XP; it's now capped to
     10% of the ~50 XP needed for Level 2, i.e. 5 XP — first-pass, needs a real playtest. The spec's
     HP/10 base-XP fallback formula wasn't implemented: every enemy in this codebase already has an
     explicit `PointValue`, so that branch would be unreachable dead code for all current content —
     it's a fallback for a future enemy authored with no `PointValue` at all, which isn't how any
     enemy here is built today. The spec's 20%-cap "quest monster" variant has no concept to hang off
     of either (no quest-monster system exists) — left as an overridable field rather than building a
     whole taxonomy just to set one number.

     "The icon... disappears" at Level 20, "reactivate[d]... under Video Settings: Always Show EXP" —
     new `Player.AlwaysShowExpEnabled` (defaults off, same account-wide `GameSettingsData`
     persistence shape as every other Graphics toggle), gating the floating "+XP" `DamageNumber`
     instead of `ShowXpDropsEnabled` once `Level >= 20`; below 20, `ShowXpDropsEnabled` still gates
     it exactly as before. New "Always Show EXP" row in `SettingsState.cs`'s Graphics tab, right
     after the existing "Show XP Drops" row, identical `SettingsRow` shape.

     `Overlay.DrawExperience()`'s `Level >= 20` branch — previously just `"Experience: {total}"` with
     an always-full bar — now shows `"Class Quest: {currentFame} / {nextThreshold} Fame"` (or
     "(Complete)" past the 5th tier) with the bar filling 0-100% within the *current* Class Quest
     tier, the same "progress within the current bracket" shape the `Level < 20` branch's XP bar
     already used. Deliberately reads live `ExperienceTotal` (this run's actively-growing Base Fame),
     not the permanent `HighScore` Character Select's stars use — the HUD should reflect what this
     specific run is building toward right now, not the character's all-time best.

     Verified via 29 scripted checks, none needing a camera/render setup this time (`WasShot()` isn't
     gated by on-screen status the way attack coroutines are, so this was mostly pure-function and
     direct-mutation testing): `ComputeBaseFame` at values straddling the Level-20 XP boundary against
     hand-derived expected fame; `ComputeStars`/Class Quest thresholds at fame values straddling all
     5 tiers (via an `XpForFame()` test helper inverting the piecewise formula, since a naive
     `fame*900` only holds below the 22-fame boundary); the XP cap at Level 1 (capped to 5) and
     Level 15 (uncapped, full 100) against hand-computed expectations; all 4 combinations of the
     Level-20 icon-gate switch (initially 2 false failures traced to a confound — `WasShot()` also
     spawns a *separate* "you dealt damage" `DamageNumber` gated on a different, untouched setting,
     which a raw before/after `DamageNumber` count couldn't tell apart from the XP one; fixed by
     disabling that other setting for the block); that a real `SettingsState`'s Graphics tab actually
     contains the new row and that its `GetBool`/`SetBool` delegates correctly read/write
     `AlwaysShowExpEnabled`; the fame-earned-on-death formula; `BonusFame` round-tripping through a
     real `Util.SavePlayerData()`/`LoadOrCreatePlayer()` cycle; and a render of the reworked
     `Overlay.DrawExperience()` at Level 20 confirming the new text/bar fill render correctly
     together (called directly via reflection rather than the full `DrawSidebar()`, which also draws
     unrelated equipment/inventory sections needing more cold-start setup than this test provided).
     Deliberately did **not** construct a real `GameOverState` anywhere in this test — its
     constructor unconditionally resets `Player.Instance` to a fresh Level-1 character and saves,
     which would have actually "killed" whichever real class was currently loaded; the fame-earned
     formula was verified directly against `Player.Instance.BaseFame`/`BonusFame` instead. Every
     `Player.Instance` field this test touched (`Level`, `ExperienceTotal`, `BonusFame`, `HighScore`,
     and three settings toggles) was snapshotted up front and restored in a `finally` block
     regardless of outcome, then re-saved to disk — a stronger guarantee than previous sessions' tests
     (which only ever reverted temp *code*, relying on the end-of-session save-file diff as the sole
     safety net for *state*). Real save files were re-verified after: `GameSettingsData.json` and
     `PlayerData_Wizard.json` both differed from the pre-test backup, but both differences turned out
     benign on inspection — `GameSettingsData.json`'s new `AlwaysShowExpEnabled` key is the intended
     schema addition (found the same file also had `AutoFireEnabled` unexpectedly flipped true,
     unrelated to anything touched this session, corrected by hand), and `PlayerData_Wizard.json`'s
     only differences were the same already-flagged equipped-item-ID quirk (see entry 183's standing
     investigation task) plus the new `BonusFame` key defaulting to 0 — confirmed field-by-field
     rather than assumed. Clean build and a plain boot-check both passed.

187. **Wired in the pre-existing `Content/Overlay/Fame Icon.png` asset** next to the account-wide
     Fame text on the main menu — the asset sat untracked in the repo with nothing referencing it
     until now. Added its `Content.mgcb` build block (copied from the adjacent `Overlay/unmute.png`
     entry's importer/processor settings) and a `Art.FameIcon` texture field. Of three candidate
     "Fame text" locations in the codebase (`Overlay.DrawFame()`, `GameOverState`'s "Fame Earned"
     line, `CharacterSelectState`'s locked-class tooltip), chose `Overlay.DrawFame()` — the one
     method literally named for this — as a judgment call rather than asking, given the low-risk,
     single-obvious-answer nature of the task. That method's only caller, `MenuState.Draw()`, had
     `Overlay.DrawFame(spriteBatch);` commented out with no explanation; re-enabled it, since the
     icon would otherwise render nowhere. `DrawFame()` now measures the icon+text pair as one unit
     and centers that combined width, rather than centering just the text as before.
     Doing so surfaced a real, previously-invisible bug: the method's vertical position used a
     hardcoded `y = (128/Scale) + 48` offset that didn't account for `Art.TitleFont`'s actual
     rendered height, so the "Fame: N" line rendered overlapping the middle of the large "Realm"
     title letters the instant it was ever drawn — almost certainly the reason this call was
     disabled in the first place, though no comment recorded why. Fixed by deriving the offset from
     `Art.TitleFont.MeasureString("Realm").Y` instead, giving correct clearance regardless of the
     title font's actual size. Verified via a temporary `Game1.StartGame()` render test (drew
     `Overlay.DrawTitle`+`DrawFame` to an offscreen `RenderTarget2D` with `FameSystem.Fame`
     temporarily set to 4200, saved to a scratch PNG, inspected visually, restored `FameSystem.Fame`
     in a `finally` block) — confirmed the icon and text sit side-by-side, vertically centered on
     each other, fully clear of the title. Temp code fully reverted (`git diff --stat Game1.cs`
     clean) and the scratch PNG deleted. Clean build, plain minimized boot-check, and a full
     real-save-file diff against a pre-change backup all passed with zero differences.

188. **Fixed a Slow debuff surviving player death onto the next character**, reported directly by
     the user after testing entry 187's neighborhood of the code (dying while Slowed left the
     freshly-reset character Slowed too). Root cause: `EntityManager.cs`'s enemy-projectile/player
     collision handler (`HandleCollisions()`) called `Player.Instance.Hit(...)` and then, on the very
     next line, `Player.Instance.Slow()` for a `SlowsOnHit` projectile — re-reading the static
     `Player.Instance` singleton fresh both times instead of caching it once. A lethal `Hit()` call
     runs its entire death pipeline *synchronously* before returning control (`Hit()` -> `Kill()` ->
     `StateManager.GameOver()` -> `GameOverState`'s constructor -> `Util.ResetPlayer()`, which
     constructs a brand-new `Wizard()`/`Archer()`/`Knight()`/`Priest()` and reassigns
     `Player.Instance` to it right there in the base `Player()` constructor). So by the time the
     `Slow()` line ran, `Player.Instance` was already the new character, not the one the projectile
     actually hit — applying a debuff meant to die with the old life onto the new one instead. Fixed
     in `EntityManager.cs` by caching `Player.Instance` into a local (`hitPlayer`) before calling
     `Hit()`, then gating the `Slow()` call on `Player.Instance == hitPlayer` — true only if that hit
     didn't kill (and therefore didn't replace) the player. This was the only call site of
     `Player.Instance.Hit(` anywhere in the codebase, and the only place a post-`Hit()` effect
     (`SlowsOnHit`) could run into this ordering hazard; no other incoming-debuff path exists yet
     (only outgoing `StunsOnHit`, which never targets the player).
     Investigated first whether the death pipeline itself (`Kill()` -> `Util.ResetPlayer()`,
     entry point for every death) actually clears buffs/debuffs, since the user's original ask was
     phrased that way — traced it thoroughly (every `Temporary*Bonus` field, `DamageTakenMultiplier`,
     `HealingAmountPerSecond`, and `Entity`'s generic `activeDebuffs` dictionary are all per-instance,
     none `static`, and `ResetPlayer()` discards the whole `Player` object rather than mutating
     fields on it) and confirmed that path was already correct — a fresh character genuinely can't
     inherit instance-level state from a discarded one. The actual bug was narrower and timing-based:
     a debuff being *applied* in the same tick the kill happens, after `Player.Instance` had already
     been swapped out from under the call.
     Verified via a temporary `Game1.StartGame()` test reproducing the exact scenario against the
     real `Player.Instance` (backed up all real save files first, per this project's standing rule):
     applied `Slow()` directly, spawned a lethal (`Damage = 999999`) `SlowsOnHit` `EnemyProjectile`
     at the player's exact position, and drove one real `EntityManager.Update()` tick — the same
     collision path real gameplay uses. Confirmed `HasDebuff(Slow)` was `true` immediately before the
     hit and `false` on the resulting fresh Level 1 character afterward. The test's real death did
     genuinely reset and re-save the live Wizard's `PlayerData_Wizard.json` (expected — this was an
     actual, intentional kill of whatever real class was loaded, the same already-flagged
     equipped-item-ID-regeneration quirk from entries 183/186 appeared here too) — restored from the
     pre-test backup afterward, verified byte-identical. Temp code fully reverted (`git diff --stat
     Game1.cs` clean), scratch log deleted, clean build, and a plain boot-check all passed, with
     every real save file confirmed unchanged from backup at the end.

189. **Fixed the level-up XP formula overshooting by 100 XP per level from Level 2 onward** — the
     user supplied the authoritative spec table (XP-to-next-level starting at 50, +100 per level:
     50/150/250/.../1850 for Levels 2-20, cumulative 18,050 by Level 20) and asked for it to be
     checked against `Player.ExperienceRequiredForLevel()`. The implemented formula,
     `level == 1 ? 50 : 50 + (level * 2 * 50)`, reduces to `50 + 100*level` for Level 2+ — exactly
     100 XP too much per transition, compounding to 19,850 cumulative XP by Level 20 instead of
     18,050. This is the same "18050 vs 19850" mismatch flagged (but deliberately left alone,
     assumed to be intentional divergence from a RotMG-style spec) during entry 186's Fame rework —
     turned out to be this bug all along, not an intentional difference in tuning. Fixed to
     `(100 * level) - 50`, which naturally produces 50 at level 1 with no special case needed (the
     old code's special-casing of Level 1 down to 50, instead of the general formula's otherwise-150,
     was working around this same off-by-100 error one level early). Verified via a temporary
     `Game1.StartGame()` test checking `Player.CumulativeExperienceForLevel(level)` for all 20 levels
     against the user's exact spec numbers — all 20 passed exactly, including the Level 20 total
     (18,050). Pure static-function fix with no `Player.Instance` mutation involved, so no save-file
     risk in the test itself; still backed up and re-verified all real save files as byte-identical
     afterward per the standing rule. Clean build and a plain boot-check both passed. Downstream
     effect worth noting: the Fame system's Level-20 XP-rate boundary (`BaseFameRateBeforeLevel20`/
     `AfterLevel20` in `Player.cs`) reads `CumulativeExperienceForLevel(20)` live rather than a
     hardcoded number, so it automatically now uses the correct 18,050 threshold too — no separate
     fix needed there. Existing saved characters' `Level` field isn't retroactively recomputed (it's
     stored, not derived), so nothing about this changes on load; only the going-forward pace of
     leveling changes, and any character already sitting on more `ExperienceTotal` than the new,
     lower next-level threshold requires will simply level up on their very next kill instead of
     needing more grinding first.

190. **Reordered the class unlock chain to Wizard -> Priest -> Archer -> Knight** (previously
     Wizard -> Archer -> Knight -> Priest — entry 80 introduced Archer/Knight's Fame-gated unlocks,
     entry 171 later added Priest into the same chain at 5,000 Fame) and replaced the unlock
     requirement with "3 stars in the previous class," per the user's explicit spec — both the
     Character Select portrait order and `Slot.IsLocked`'s gating logic changed together in
     `CharacterSelectState.cs`, since the two were the same list. Previously each class needed a flat
     amount of account-wide `FameSystem.Fame` (`ArcherFameRequirement`/`KnightFameRequirement`/
     `PriestFameRequirement` = 1000/3000/5000) regardless of which specific class actually earned
     that Fame. Replaced `Slot.RequiredFame`/its computed `IsLocked` with
     `Slot.PreviousClass` (the class immediately to its left in the reordered `slots` list; `null`
     for Wizard, which stays always-unlocked) and a plain `IsLocked` field recomputed each `Update()`
     from that specific previous class's own permanent star record — `Player.ComputeStars(previous
     class's saved HighScore) < 3`, using the same Fame-thresholded `ComputeStars` the Fame rework
     (entry 186) already introduced for the account-level Character Select star display. `Update()`
     now runs in two passes: first reading every slot's `Stars`/`HasSave` from disk (needed before
     any lock check, since a locked slot's requirement depends on a DIFFERENT slot's freshly-read
     Stars, not its own), then a second pass computing `IsLocked` and running the existing
     hover/click/delete logic. `DrawLockedPreview()`'s hint text changed from "Requires N Fame (You
     have M)" to "Requires 3 Stars in {PreviousClass} (You have {PreviousClassStars})" to match.
     Verified via a temporary `Game1.StartGame()` test constructing a real `CharacterSelectState`
     (safe against real save data — `Util.PeekPlayerData()` is read-only, no backup needed) and, via
     reflection, checking the `slots` list's order and `PortraitRect.X` positions against the
     expected Wizard/Priest/Archer/Knight left-to-right layout (all 4 passed), then calling the real
     `Update()` once and confirming each slot's `IsLocked` matched an independently-computed
     `ComputeStars(previous class's real saved HighScore) < 3` check (all 4 passed) — including that
     a class which already had stored stars from before this reorder (Archer, at 1 star from old
     save data) is correctly locked anyway if ITS OWN new prerequisite, Priest, hasn't been unlocked
     yet; historical stars on a class don't grandfather it past the new chain. Temp code fully
     reverted (`git diff --stat Game1.cs` clean), scratch log deleted, clean build, plain boot-check,
     and a full real-save-file diff all passed with zero unexpected differences (none expected, given
     the read-only nature of everything this test touched).

191. **Fixed `Util.PeekPlayerData()` throwing `FileNotFoundException` on every frame for any
     never-played (or just-erased) class**, reported directly by the user after using entry 190's
     newly-reordered Character Select's "Erase All Data" and seeing a repeating "Exception thrown:
     System.IO.FileNotFoundException" in the debugger. Root cause: `PeekPlayerData()` opened a
     `StreamReader` directly and relied on catching `FileNotFoundException` to detect "this class has
     no save yet" — a legitimate, expected outcome for a locked/never-played class or right after an
     account wipe, not a truly exceptional condition. `CharacterSelectState.Update()` calls
     `PeekPlayerData()` for all 4 class slots on every single frame (needed for the Stars/lock display
     — see entry 190), so as long as that screen stays open with any class missing a save file, the
     game was throwing (and immediately catching) that exception 60+ times a second, forever — a
     debugger reports every one of those via a first-chance exception notification regardless of the
     catch, which is exactly the "over and over" the user saw. Fixed by checking `File.Exists()`
     before ever opening the file, returning `null` immediately with no exception involved at all —
     same return value for the same inputs, just without using an exception as normal control flow
     for a routine case. Left the JSON-corruption `catch` branch (a real, rare failure mode) as-is.
     Other `Load*Data()` methods in `Util.cs` (inventory/bank/Fame/key bindings/settings) have the
     same try/catch-on-FileNotFoundException shape, but none of them are called from a per-frame
     `Update()` loop — only once at boot — so they don't reproduce this symptom and were deliberately
     left alone rather than changed everywhere on general principle.
     Verified directly against the user's own live, already-erased save state (Archer/Knight/Priest
     genuinely had no `PlayerData_*.json`/`InventoryData_*.json` at the time of testing — confirmed via
     `ls` before touching anything) via a temporary `Game1.StartGame()` test: hooked
     `AppDomain.CurrentDomain.FirstChanceException` (the same mechanism a debugger uses to report
     "Exception thrown," which fires whether or not the exception is later caught — a plain
     try/catch around the calls would NOT have proven anything, since the old code already caught the
     exception internally too) around 240 calls to `PeekPlayerData()` reproducing
     `CharacterSelectState.Update()`'s exact all-4-classes-every-frame pattern for 60 simulated
     frames. Confirmed 0 `FileNotFoundException`s were raised (first-chance or otherwise) across all
     240 calls, and that Wizard (which does have a save) still correctly returned non-null while
     Archer/Knight/Priest still correctly returned `null`. Purely read-only (`File.Exists`/
     `StreamReader` reads only, no writes anywhere in `PeekPlayerData`), so no save-file risk in the
     test itself — still backed up and re-verified the real save files as byte-identical afterward,
     and confirmed Archer/Knight/Priest's save files were still genuinely absent (not accidentally
     regenerated by anything else `Game1.StartGame()` does). Temp code fully reverted (`git diff
     --stat Game1.cs` clean), scratch log deleted, clean build, and a plain boot-check both passed.

192. **Made the F4 debug key also grant enough XP/HighScore for 3 stars**, on top of its existing
     max-Level-and-top-gear behavior — requested directly to make it easier to test entry 190's new
     class-unlock chain (each class needs 3 stars in the class before it) without actually grinding a
     character there first. Added `Player.ExperienceForBaseFame(int targetFame)`, inverting
     `ComputeBaseFame()`'s two piecewise rates (rather than hardcoding a specific XP number that would
     silently drift out of sync if `BaseFameRateBeforeLevel20`/`AfterLevel20` or the leveling curve —
     entry 189's recent fix — are ever retuned again), and `Player.DebugGrantThreeStarsFame()`, which
     sets `ExperienceTotal` (and `HighScore`, via `Math.Max` — never lowering either if already
     higher) to `ExperienceForBaseFame(ClassQuestFameThresholds[2])`, the "3 stars" tier. Setting
     `HighScore` directly (not just `ExperienceTotal`) matters here specifically because
     `RealmState.Update()`'s live HighScore-sync-from-ExperienceTotal (entry 186) only runs inside an
     active dungeon — relying on it alone would silently do nothing if F4 is pressed in the Nexus,
     where the debug key is equally usable. Wired into `Input.cs`'s existing F4 handler alongside the
     unchanged `DebugMaxLevelAndEquipTopGear()` call, not merged into that method — the two are
     independent debug conveniences with no shared reasoning to justify one now doing both jobs under
     the other's name. At the current leveling curve, the 3-star XP threshold works out to
     2,978,050 (computed live, not hardcoded in the fix itself — this number is just what it happens
     to evaluate to right now).
     Verified via a temporary `Game1.StartGame()` test using a throwaway `Wizard()` (its constructor
     sets `Player.Instance` to itself, restored to the real original instance in a `finally` block —
     the same safe pattern used for earlier shot-catalog testing this session; nothing here was ever
     saved to disk): confirmed a fresh character's `DebugGrantThreeStarsFame()` call lands at exactly
     3 stars (not 2, not 4) via `ComputeStars()`; confirmed one XP below that computed value lands at
     exactly 2 stars, proving the formula produces the true minimum rather than just "enough"; and
     confirmed a character already sitting on more `ExperienceTotal`/`HighScore` than the 3-star
     requirement is left completely unchanged (the `Math.Max` guards). Also hand-verified the
     arithmetic independently against `ComputeBaseFame()`'s own two branches before running the test,
     to confirm the test's expectations weren't just circularly checking the code against itself.
     Ran into (and had to fix) a real `sed`-caused incident partway through this session's editing:
     a `sed -i 's/.../.../g'` used to bulk-fix a namespace reference in `Game1.cs`'s temp test code
     silently stripped every CRLF line ending from the whole file down to bare LF (confirmed via
     `file` reporting "with CRLF line terminators" missing, and `grep -c $'\r'` returning 0) — `git
     diff --stat` then showed a spurious ~231/~231 line "rewrite" even after the temp test itself was
     correctly reverted, since git treats a lone-LF line as different content from the repo's
     CRLF-committed version, not merely a whitespace difference. Fixed with a second `sed -i
     's/$/\r/'` pass restoring CRLF (confirmed zero pre-existing `\r` characters first, so this
     couldn't double up), which brought `git diff --stat Game1.cs` back to clean with the UTF-8 BOM
     still intact throughout (never affected — only the line endings were). Worth remembering for any
     future `sed -i` edit to a tracked `.cs` file in this repo, which is CRLF throughout
     (`core.autocrlf=true`): plain `sed -i` on Windows/git-bash normalizes to LF as a side effect of
     writing the file back out, even when the substitution itself has nothing to do with line endings.
     Clean build, plain boot-check, and a full real-save-file diff all passed with zero differences.

193. **Removed Score/Hi Score from the top-left gameplay overlay, and replaced Score/Hi-Score with
     Fame/Highest Fame on the Character Select preview** — requested directly by the user as
     "no longer needed," now that entries 186/190 built a whole Fame-based progression system
     (Base Fame, Class Quests, the unlock chain) on top of the same underlying `ExperienceTotal`/
     `HighScore` numbers Score/Hi-Score were just raw-printing. Deleted `Overlay.DrawScore()`
     entirely (it only ever drew "Score: {ExperienceTotal}" / "Hi Score: {HighScore}" at a fixed
     top-left position) along with its two call sites, `NexusState.Draw()` and `RealmState.Draw()` —
     the only two places it was ever called. On `CharacterSelectState.cs`'s hover preview
     (`DrawPreview()`), replaced the `scoreText`/`highScoreText` local variables (and their "Score: "/
     "Hi-Score: " labels) with `fameText`/`highestFameText`, now reading `Player.ComputeBaseFame
     (ExperienceTotal)` / `ComputeBaseFame(HighScore)` instead of the raw XP numbers directly —
     deliberately this class's own per-life Base Fame (the same value Class Quests/stars are based
     on), not the account-wide `FameSystem.Fame` already shown at the top of the menu (entry 187),
     which is a different, shared-across-every-class number. The underlying `HighScore`/
     `ExperienceTotal` fields themselves are untouched — still the real persisted/live values feeding
     `ComputeStars()`/the unlock chain/the erase-all-data warning text; only what gets displayed and
     how changed.
     Verified with a temporary `Game1.StartGame()` test rendering `CharacterSelectState`'s real
     `DrawPreview()` (via reflection, since it's private) for the Wizard slot to an offscreen
     `RenderTarget2D`, using the actual real save data — confirmed the output PNG shows "Fame: 1500"
     and "Highest Fame: 1500" (the real Wizard character's current numbers, itself a nice
     confirmation that entry 192's new F4 debug key had already been used for real) with no
     overlapping or clipped text, replacing where "Score:"/"Hi-Score:" used to render. Build
     succeeding at all (with `DrawScore()` fully deleted) already confirmed no orphaned call sites
     remained beyond the two removed. Ran into the same `sed`-strips-CRLF hazard flagged in entry 192
     again in an earlier draft of this fix — this time avoided it entirely by using the Edit tool
     instead of `sed` for every substitution in this change, so no cleanup pass was needed here. Temp
     code fully reverted (`git diff --stat Game1.cs` clean), scratch PNG deleted, clean build, plain
     boot-check, and a full real-save-file diff all passed with zero differences.

194. **Moved the account-wide Fame display off the title screen and into the top-left gameplay
     corner** (the Nexus and dungeons) — where Score/Hi Score used to sit before entry 193 removed
     them — per direct user request. `Overlay.DrawFame()` (entry 187: centered under the "Realm"
     title, `MenuState`'s only caller) was rewritten to draw at a fixed top-left position
     (`FameOverlayX`/`Y` = 32/64, matching Score's old top line) instead of centering itself under
     `DrawTitle()`'s measured width/height — the centering math and its `Game1.ScreenWidth`/
     `Art.TitleFont` dependencies are gone entirely, since a fixed-position corner element doesn't
     need them. Removed the call from `MenuState.Draw()`; added it to `NexusState.Draw()` and
     `RealmState.Draw()` (the exact two places `Overlay.DrawScore()` used to be called from before
     entry 193 deleted it) — `BossRealmState` needed no separate change, since it inherits
     `RealmState.Draw()` wholesale rather than overriding it.
     Verified via a temporary `Game1.StartGame()` test rendering two real, independent scenarios to
     offscreen `RenderTarget2D`s: `MenuState`'s actual `Draw()` (confirming the title screen shows
     only "Realm" and the four menu buttons, no Fame text anywhere) and `Overlay.DrawFame()` called
     alone (confirming "Fame: 0" plus its icon render cleanly at the new top-left position, matching
     Score's old footprint, with no clipping). Also checked by hand that nothing else already occupies
     that corner — the minimap sits top-right (`Game1.SidebarX`-anchored) and the F3 debug overlay
     starts at y=256, well clear of Fame's y=64. Temp code fully reverted (`git diff --stat Game1.cs`
     clean), scratch PNGs deleted, clean build, plain boot-check, and a full real-save-file diff all
     passed with zero differences.

195. **Reworked the XP/HP/MP sidebar bars per a detailed user spec**: labels shortened ("Exp:" ->
     "XP", "HP:" -> "HP", "Mana:" -> "MP" — all colon-free now), the label and its numbers moved from
     a separate text row above each bar to inside the bar itself (label left-aligned, numbers
     center-aligned, both vertically centered within the bar), HP/MP's number turns gold when
     currently full (`Health >= HealthMax`/`Mana >= ManaMax` — replacing the old, unrelated
     "permanent stat maxed" LimeGreen/White distinction, which was about `HealthMax >= MaxHealth`,
     a totally different and much rarer condition), the vertical gap between the three bars is much
     tighter (a fixed 6px between bars now, vs. the old ~64px-per-section spacing that assumed a
     separate text row), and the Level-20 branch's "Class Quest: N / M Fame" text is replaced with
     the same shape as the other three bars — label "Fame", numbers "N / M" (or "N (Complete)" past
     the 5th tier), no more redundant trailing " Fame" now that the label itself says it.
     Added one shared helper, `Overlay.DrawBarText()`, since all four bars (XP/Fame, HP, MP) now
     follow the exact same "label left, numbers centered, both vertically centered in the bar rect"
     shape — avoids four near-identical copies of the same two `MeasureString`/`DrawString` pairs.
     Three new layout constants (`XpBarY`/`HpBarY`/`MpBarY`, the last two derived from the one before
     plus `SidebarBarHeight` + a new `SidebarBarGap` = 6) replace the old independent hardcoded
     `y = 160`/`224`/`288` literals in `DrawExperience`/`DrawHealthSection`/`DrawManaSection` — needed
     since `DrawCombatIndicator`'s yellow in-combat border and sword-badge icon are anchored to the
     HP bar specifically and had to move in lockstep with it (previously both used a local `y = 224`
     matching HP's old text-row position; now both reference the shared `HpBarY` constant, with the
     badge re-centered on the bar's height since there's no longer a separate text row above it to
     align with). Deliberately left `DrawAbilitySection`/`DrawEquipment`/`DrawInventory` untouched —
     out of scope, and they're positioned independently rather than relative to Mana's bottom, so
     tightening XP/HP/MP just leaves a bit more empty space above Ability now rather than breaking
     anything.
     Verified via a temporary `Game1.StartGame()` test rendering `Overlay.DrawSidebar()` for two
     throwaway `Wizard()` scenarios to offscreen `RenderTarget2D`s (never touching real save data):
     Level 15 with Health at exactly half HealthMax and Mana at exactly ManaMax — confirmed "XP" bar
     shows label+numbers inside the bar, "MP" numbers render gold (full) while "HP" numbers stay
     white (half); and Level 20 with partial Base Fame, Health at exactly HealthMax, Mana at a third
     of ManaMax — confirmed the top bar now reads "Fame" / "60 / 500" instead of "Class Quest: ...",
     and this time "HP" (now full) renders gold while "MP" (partial) stays white, the inverse pairing
     from the first scenario, ruling out either color always defaulting to gold regardless of the
     actual full/not-full state. Both renders also visually confirmed the tighter bar spacing and the
     sword-badge/combat-border repositioning look correct with no overlap. Temp code fully reverted
     (`git diff --stat Game1.cs` clean), scratch PNGs deleted, clean build, plain boot-check, and a
     full real-save-file diff all passed with zero differences.

196. **Let equipment (and temporary buffs) push HealthMax/ManaMax above MaxHealth/MaxMana**, per
     direct user request. Investigating turned up a real, previously dead-code bug: `Player.cs`
     already defined `EquipmentMaxHealthBonus`/`EquipmentMaxManaBonus` (summed from Weapon/Armor/
     Ring/AbilityItem, same shape as every other `EquipmentXBonus`) and `TemporaryHealthMaxBonus`/
     `TemporaryManaMaxBonus` (with full tick-down/expiry infrastructure already wired into
     `UpdateTemporaryBonuses()`), but not one of the four classes' `RecalculateStats()` formulas
     (`Wizard.cs`/`Archer.cs`/`Knight.cs`/`Priest.cs`) actually added either into `HealthMax`/
     `ManaMax` — unlike every other stat (Attack/Defense/Vitality/Wisdom/Speed/Dexterity), which
     already summed base+level+Potion+Equipment+Temporary. `HealthMax`/`ManaMax` were the one pair
     silently stuck at base+level+Potion only, so gear with a `MaxHealthBonus`/`MaxManaBonus` field
     (real items exist — Ring of Minor Defense +5, Quiver/Shield/Spell/Tome's higher tiers up to +40)
     had zero effect on them. Added the missing `+ EquipmentMaxHealthBonus + TemporaryHealthMaxBonus`/
     `+ EquipmentMaxManaBonus + TemporaryManaMaxBonus` terms to all four classes' formulas.
     Since every other stat already had this same equipment/temporary-inclusive shape, they'd also
     already needed (and had) a `PermanentX` counterpart (`PermanentAttack`, etc. — base+level+Potion
     only, excluding gear/temporary) for two purposes: `Overlay.DrawStats()`'s "is this maxed"
     highlight, and (more load-bearingly) `InventorySystem.UsePotionEffect()`'s stat-potion gating,
     which correctly checks `PermanentAttack >= MaxAttack` rather than raw `Attack`, so a temporarily
     gear-boosted stat can't block a potion that would still raise the real, permanent value.
     HealthMax/ManaMax had never needed this split before (nothing else fed into them), so there was
     no `PermanentHealthMax`/`PermanentManaMax` — and `UsePotionEffect()`'s "Life Potion"/"ManaMax
     Potion" cases checked raw `HealthMax`/`ManaMax` directly. Wiring in equipment/temporary bonuses
     without also fixing this would have made those two potions start getting incorrectly blocked
     the instant any equipped item's `MaxHealthBonus`/`MaxManaBonus` alone pushed the raw value past
     the cap — added `Player.PermanentHealthMax`/`PermanentManaMax` (identical shape to the other six)
     and switched both `UsePotionEffect()` cases to check those instead, matching every other stat
     potion's existing pattern. Updated the doc comment above the `PermanentX` properties, which
     explicitly said at the time it was written that HealthMax/ManaMax "already only count" Potion
     bonuses — no longer true.
     Verified via a temporary `Game1.StartGame()` test using a throwaway `Wizard()` (never touching
     real save data): confirmed a fresh Level 1 character's `PermanentHealthMax` exactly equals
     `HealthMax` (no gear bonus yet); confirmed `DebugMaxLevelAndEquipTopGear()`'s top-tier gear
     brought `HealthMax` above its no-gear `PermanentHealthMax` value while `PermanentHealthMax`
     itself stayed under `MaxHealth`; confirmed a Life Potion succeeds in that state (previously would
     have needed to check the still-correct condition); and — the definitive check — used Life
     Potions in a loop until `PermanentHealthMax` reached exactly `MaxHealth` (700), confirming the
     *next* potion is correctly blocked at that point even though raw `HealthMax` sat at 740 (700 +
     the ~40 equipment bonus), proving both halves at once: equipment genuinely pushes the live stat
     above the old hard cap, and the potion gate correctly tracks the permanent value rather than
     being fooled by gear. Clean build, plain boot-check, and a full real-save-file diff all passed
     with zero differences.

197. **Show HP/MP equipment bonuses in parentheses next to the bar numbers** — e.g. "615 / 615
     (+40)" — a direct follow-up to entry 196's fix letting equipment push `HealthMax`/`ManaMax`
     past their cap, so that bonus is now visible rather than silently folded into the total.
     `Player.EquipmentMaxHealthBonus`/`EquipmentMaxManaBonus` were `protected` (only meant for each
     class's own `RecalculateStats()` to read) — widened to `public`, same precedent as
     `EquipmentXpBonusPercent` just above them (also public, also read by code outside `Player`).
     `Overlay.DrawHealthSection()`/`DrawManaSection()` append `" (+" + bonus + ")"` to the numbers
     string whenever that bonus is nonzero, before handing the whole thing to `DrawBarText()` — the
     suffix becomes part of the same centered block rather than a separately-positioned element, so
     "615 / 615 (+40)" centers as one unit exactly like the format the user asked for.
     Verified by rendering `Overlay.DrawSidebar()` for the real, currently-loaded live Wizard
     character (safe — nothing in this render path writes anything) to an offscreen `RenderTarget2D`:
     that real character happened to already be sitting at exactly `HealthMax 615 (+40 equipment)`
     from entry 196's own testing, an exact match for the user's own example — the output PNG
     confirmed "HP 615 / 615 (+40)" and "MP 398 / 398 (+115)" both render correctly, gold (full)
     as expected from entry 195's coloring rule, with the bonus suffix cleanly inside the bar. No
     temp code was added to `Game1.cs` beyond the render call itself, fully reverted after (`git
     diff --stat Game1.cs` clean), scratch PNG deleted, clean build, plain boot-check, and a full
     real-save-file diff all passed with zero differences.

198. **Repositioned Fame/HP/MP to sit directly above the Ability bar, widened every sidebar bar to
     match the sidebar's left/right padding, and made the bar text bold with a thin outline** — three
     more direct HUD polish requests. Introduced `AbilityY` (the Ability section's position, formerly
     an unnamed `352` literal) as the new anchor and derived `MpBarY`/`HpBarY`/`XpBarY` backward from
     it (`AbilityY - gap - height`, chained), replacing their old forward derivation from a `160`
     starting point right below the stat block — the three bars now sit flush against Ability with
     the same tight `SidebarBarGap` used between themselves, leaving the stat block's own position
     untouched above (a real, visible gap now sits between the stat block and the bars, an accepted
     side effect of moving the bars down rather than the stats down). Replaced the old fixed
     `100 * SidebarBarScale` (200px) bar width with `SidebarBarWidth = Game1.SidebarWidth -
     (SidebarPadding * 2)` (260px) — computed as a compile-time constant since `Game1.SidebarWidth`
     is itself `const` — applied to every bar's background/fill rectangle and text-positioning rect
     across Fame/XP, HP, MP, and Ability (its readiness bar and its "no ability equipped" empty
     placeholder), so the right edge now sits exactly `SidebarPadding` from the sidebar's own right
     edge, matching the left side. Fill-percentage math changed from `percent * SidebarBarScale`
     (implicitly 2px/percent) to `percent * SidebarBarWidth / 100`, so fill width scales with the new
     bar width automatically rather than needing its own separate constant.
     For bold text, reused `Art.DamageFont` (already Bold Arial, built in entry 114 for floating
     combat damage numbers) in `DrawBarText()` instead of the sidebar's usual `Art.HudFont` — no new content
     asset needed. For the outline, added `DrawOutlinedText()`: draws the string at 8 surrounding
     1px offsets in black underneath the real colored draw, a true fully-surrounding outline rather
     than `DrawShadowedText`'s existing single bottom-right drop-shadow technique (which wouldn't
     read as an "outline" from every angle). Scoped to just the bar text (`DrawBarText`, used by
     Fame/XP/HP/MP) per the request's own wording — Ability's own separate `DrawString` call, the
     stat block, and everything else keep their existing plain `HudFont` rendering.
     Verified by rendering the real, currently-loaded live Wizard character's `Overlay.DrawSidebar()`
     to an offscreen `RenderTarget2D` (safe, no writes) and visually inspecting a 3x-zoomed crop:
     confirmed Fame/HP/MP now sit directly above "Ability Ready (Cost 90)" with no gap beyond the
     usual tight `SidebarBarGap`; confirmed all three bars (plus Ability's) extend to the sidebar's
     right edge with a margin visually matching the left side; and confirmed the label/number text
     ("Fame", "HP 615 / 615 (+40)", "MP 398 / 398 (+115)") renders visibly bolder and fully outlined
     in black on all sides, not just a corner shadow. Noted one minor, unrequested side effect: the
     Vital Combat sword-badge icon (entry 172, right-anchored to the sidebar's edge) now
     sits slightly on top of the HP bar's own right edge instead of in the open margin that used to
     exist there, since that margin is what got widened away — left alone since it wasn't part of
     this request and still reads fine, but worth a follow-up if it turns out to bother the user in
     practice. Temp code fully reverted (`git diff --stat Game1.cs` clean), scratch PNGs deleted,
     clean build, plain boot-check, and a full real-save-file diff all passed with zero differences.

199. **Fixed the Vital Combat sword-badge/HP-bar overlap flagged in entry 198**, moving the badge to
     sit just below the minimap (right-aligned to the minimap's own right edge) instead of at the HP
     bar's right edge — the previous position only worked because the HP bar used to stop short of
     the sidebar's right edge; entry 198 widened every bar to fill that space, leaving the badge
     sitting on top of the bar's corner. New `CombatIconY` constant derives from the existing
     `MinimapPadding`/`MinimapSize` constants (`MinimapPadding + MinimapSize + MinimapPadding`) rather
     than a fresh literal, so the badge stays correctly placed if the minimap's own size ever changes.
     The badge's hover tooltip was anchored above-left of the icon before — with the icon now sitting
     right under the minimap, an above-anchored tooltip would itself overlap the minimap, so it's now
     anchored below-left instead, where there's open space clear down to the Fame/HP/MP bars.
     Verified by rendering `Overlay.DrawSidebar()` for the real, currently-loaded live Wizard
     character to an offscreen `RenderTarget2D` — confirmed the badge now sits cleanly below the
     minimap with no overlap on the HP bar or its text. Temp code fully reverted (`git diff --stat
     Game1.cs` clean), scratch PNG deleted, clean build, plain boot-check, and a full real-save-file
     diff all passed with zero differences.

200. **Added a "retro video game" font for the Fame/XP/HP/MP bar text**, per direct user request.
     Every existing font in this project (`HudFont`/`TitleFont`/`DamageFont`/`SettingsFont`)
     references an installed system font family (Arial) — no genuine pixel/8-bit font was available
     on the system (checked the full installed font list; closest candidate, "OCR A Extended," reads
     as robotic/OCR rather than retro-game), so this needed a real font file. Asked the user how to
     source one rather than guessing or downloading without asking, per this project's own
     file-download rule; user chose to have one downloaded. Fetched `PressStart2P-Regular.ttf`
     directly from Google Fonts' official `google/fonts` GitHub repo (SIL Open Font License, free to
     bundle) and added it to `Content/Fonts/` alongside a new `RetroFont.spritefont` — unlike every
     other font here, its `<FontName>` points at the bundled `.ttf` file sitting next to it rather
     than a system family name, which MonoGame's `FontDescriptionImporter` supports directly. New
     `Content.mgcb` block (same importer/processor shape as the other four fonts) and `Art.RetroFont`
     field/load line. Scoped to exactly where the request applies: `Overlay.DrawBarText()` (Fame/XP/
     HP/MP bar label+numbers, entries 195/198) now uses `Art.RetroFont` instead of `Art.DamageFont` —
     everything else (stats block, Ability's own text, tooltips, menus) keeps its existing font
     untouched.
     First attempt used 10pt (chosen since Press Start 2P's design is much wider/taller per point
     than Arial, so straight reuse of DamageFont's 14pt would have been far too large) — a render
     test caught real overlap between the left-aligned label and the center-aligned numbers (e.g.
     "MP398" running together) at that size, since Press Start 2P's glyphs are still wide enough at
     10pt to eat into the gap `DrawBarText`'s existing layout math assumed. Dropped to 8pt, re-rendered,
     and confirmed clean separation with no overlap on any of the three bars. Verified by rendering
     `Overlay.DrawSidebar()` for the real, currently-loaded live Wizard character to an offscreen
     `RenderTarget2D` and visually inspecting a 3x-zoomed crop both before and after the size fix —
     final render confirmed all three bars read clearly in the new pixel font with proper label/number
     spacing, the existing bold-via-font-choice + black outline (entry 198) still applying correctly
     on top of it, and no regressions elsewhere in the sidebar. Temp code fully reverted (`git diff
     --stat Game1.cs` clean), scratch PNGs deleted, clean build, plain boot-check, and a full
     real-save-file diff all passed with zero differences.

201. **Wired in a real Combat Badge icon and switched the retro bar font from Press Start 2P to
     Jersey10**, both direct follow-ups after entry 200 shipped. For the icon: the user dropped
     `Content/Overlay/Combat Badge.png` (a 32x32 sword sprite) into the project — added its
     `Content.mgcb` block (same importer/processor shape as the adjacent `Fame Icon.png` entry) and
     an `Art.CombatBadge` field/load line, then swapped `DrawCombatIndicator()`'s
     `spriteBatch.Draw(Art.HealthBar, iconRect, iconColor)` placeholder-square draw for
     `Art.CombatBadge` — same gold/dim-gray tint logic, no other changes, exactly the "one-line
     change" the placeholder's own removed comment had predicted back in entry 172.
     For the font: the user wasn't sold on Press Start 2P. Rather than guess at a replacement,
     downloaded four more free (SIL Open Font License) candidates from the same trusted `google/fonts`
     repo — VT323 (retro terminal), Silkscreen (a more compact true pixel font), Jersey10 (a chunky
     pixel-sports-jersey style), and Pixelify Sans (a rounder, semi-pixel font) — and rendered all
     five side-by-side in the actual `DrawBarText`-style layout ("HP 615 / 615 (+40)" on a real green
     bar) via a temporary comparison harness in `Game1.StartGame()`, sent the composite image to the
     user directly, and asked which one to use. Jersey10 won. Repointed the existing `RetroFont`
     asset at `Jersey10-Regular.ttf` (14pt — confirmed no label/numbers overlap in the comparison
     render, unlike Press Start 2P's first attempt in entry 200) rather than creating a
     differently-named font, since `Art.RetroFont`/`Overlay.DrawBarText()` already referred to "the
     retro font" generically and had no reason to change identity just because the underlying font
     file did. Removed `PressStart2P-Regular.ttf` and the three unchosen comparison fonts'
     `.ttf`/`.spritefont`/`Content.mgcb` entries entirely — only `Jersey10-Regular.ttf` and the
     (repointed) `RetroFont.spritefont` remain.
     Verified by rendering the real, currently-loaded live Wizard character's full `Overlay.
     DrawSidebar()` to an offscreen `RenderTarget2D` one final time and inspecting a 3x-zoomed crop:
     confirmed the sword badge renders as real pixel art (not a solid tinted square) sitting cleanly
     below the minimap, and confirmed Jersey10 reads clearly with proper label/numbers spacing and
     the existing black outline still applying correctly on top of it. Also manually re-checked the
     built `bin/.../Content/Fonts/` output afterward to confirm no orphaned `.xnb` files remained from
     the four discarded comparison fonts. Temp code (both the font-comparison harness and the final
     verification render) fully reverted (`git diff --stat Game1.cs` clean), scratch PNGs deleted,
     clean build, plain boot-check, and a full real-save-file diff all passed with zero differences.

202. **Moved the Vital Combat sword badge to just above the Fame bar (left-aligned), and made
     Jersey10 (entry 201's `RetroFont`) the base font for the whole in-game HUD, tooltips, and
     damage numbers/XP drops** — two more direct requests. Since "the game going forward" was
     ambiguous about how far a base-font swap should reach (menus/buttons? Character Select? enemy
     speech bubbles? portal/boss labels?), asked the user to pick a scope tier before touching
     anything; they chose the narrowest of three — HUD + tooltips + damage numbers only, explicitly
     leaving `Controls/Button.cs` (every menu button in the game), `States/CharacterSelectState.cs`,
     `TauntBubble.cs`, `Portal.cs`'s dungeon-name labels, and `BossRealmState.cs`'s boss-name display
     on their existing fonts.
     Badge reposition: `CombatIconY` now derives from `XpBarY - SidebarBarGap - CombatIconSize`
     instead of the minimap-relative position entry 199 gave it, and the icon's `x` is
     `Game1.SidebarX + SidebarPadding` (matching the bars) instead of a right-edge anchor. Its hover
     tooltip anchor flipped back to above-left of the icon (entry 199's "anchor below" fix was
     specifically to dodge the minimap, which is no longer adjacent to the badge's new position; open
     space above it again, clear of the stat block).
     Base font swap: promoted `Overlay.cs`'s private `DrawOutlinedText()` helper to a public
     `Util.DrawOutlinedText()` (now with an optional `scale` parameter for `DamageNumber`'s scaled
     draws, and an outline alpha that tracks the passed color's own alpha channel so a fading number's
     outline fades with it) — both `Util.DrawTooltip()` overloads now call it internally instead of a
     plain `DrawString`, so every tooltip picks up the outline automatically regardless of which font
     is passed in. Then swapped `Art.HudFont` → `Art.RetroFont` at every remaining call site within
     scope: all of `Overlay.cs` (DrawFame, DrawStats — including the "Level:" column-width
     measurement, DrawAbilitySection, DrawDebug, the combat indicator's tooltip), the four equipment
     tooltip classes (`Weapon.cs`/`Armor.cs`/`Ring.cs`/`AbilityItem.cs`) plus their shared base
     (`Equipment.cs`)'s `TooltipText()`/`HeaderLines()` — including their `Util.WrapText()` calls,
     which needed the same font to keep word-wrap widths accurate — `BankSystem.cs`/
     `InventorySystem.cs`/`LootBag.cs`'s item tooltips and stack-quantity/potion-charge count labels,
     and `Player.cs`'s `DrawTemporaryBonusIndicators()` (the floating colored "+" buff icons above
     the player). Every color argument at every one of these call sites was left untouched — only the
     font and (for anything not already going through `Util.DrawTooltip`) the outline changed.
     `DamageNumber.cs` swapped `Art.DamageFont` for `Art.RetroFont` too, and its `Draw()` now calls
     `Util.DrawOutlinedText()` unconditionally — this replaced (rather than kept alongside) the old
     `hasBlackBacking` mechanism, a single bottom-right drop shadow previously applied only to the
     player's own damage-taken number (entry 113); a full outline now serves
     every damage number and XP-gain number equally, so the special case (field, constructor
     parameter, `BackingOffset`/`BackingAlpha` consts) was dead weight once nothing distinguished it
     anymore — removed entirely, including updating its one caller (`Player.Hit()`) to drop the
     `hasBlackBacking: true` argument. `Art.DamageFont` itself had zero remaining consumers afterward,
     so it was deleted outright (field, load line, `Content.mgcb` block, `.spritefont` file) rather
     than left as dead content.
     Verified via a temporary `Game1.StartGame()` test rendering three real scenarios to offscreen
     `RenderTarget2D`s: the full `Overlay.DrawSidebar()` (confirmed the stat block's existing 16px
     row spacing has no vertical overlap with Jersey10 at 14pt — a real risk given how much wider/
     taller this font is than Arial, checked directly via a 4x-zoomed crop — and confirmed the badge's
     new position/tooltip-anchor); a weapon tooltip forced on via reflection (`hover` is protected)
     showing the wrapped description, red color, and outline all correct; and three constructed
     `DamageNumber`s (red player-damage, red enemy-hit, goldenrod scaled XP-gain) confirmed all three
     now share the same crisp outline with their original colors and the XP number's outline scaling
     proportionally with its 1.3x text scale. A `Player.cs` HudFont usage
     (`DrawTemporaryBonusIndicators`) was missed on the first sweep — caught by a final repo-wide
     grep for `Art.HudFont`/`Art.DamageFont` restricted to the in-scope files, confirming zero
     remaining matches there and exactly six files left with `Art.HudFont` (all of them the
     deliberately out-of-scope ones the user chose to exclude). Clean build, plain boot-check, and a
     full real-save-file diff all passed with zero differences.

203. **Colored the equip-slot hover tooltip by content type**: white for name/description, green for
     stat bonus lines, red for damage, blue for mana cost — replacing the flat single-color (Red)
     tooltip entry 202 left in place when it swapped the font. Scoped specifically to the four equip
     slots' own hover tooltip (`Weapon`/`Armor`/`Ring`/`AbilityItem.DrawTooltip(SpriteBatch)`, built
     from each item's `TooltipText()` string) — the separate inventory/bank "compare to what's
     equipped" tooltip (`ComparisonLines()`, `List<(Text,Better)>`) keeps its existing red/dark-green
     upgrade-highlight scheme untouched, since that's a different feature (is this an upgrade?) with
     its own already-meaningful color code, not a content-category scheme, and the user's request
     didn't ask to touch it.
     Added `Util.DrawCategorizedTooltip()`: splits the composed tooltip string into lines (normalizing
     `Environment.NewLine`/bare `\n` — `TooltipText()` mixes both, `Environment.NewLine` joining
     sections and `Util.WrapText()`'s own bare `\n` inside a wrapped description) and classifies each
     line by simple content rules — starts with `+` or is exactly "No bonuses" → green; starts with
     "Damage:" or "Side Damage:" → red; ends with "Mana Cost" → blue; anything else → white — then
     draws each with `Util.DrawOutlinedText()` at its own color. Found and fixed one real formatting
     bug this surfaced: `AbilityItem.AbilitySummary()` joined "Damage: X - Y" and "N Mana Cost" onto
     one comma-separated line, which can't be given two different colors — changed the join separator
     from `", "` to `Environment.NewLine` so they're independently colorable lines, matching how every
     other section was already one-line-per-concept.
     Verified via a temporary `Game1.StartGame()` test forcing each equip slot's hover on (via
     reflection — `hover` is `protected`) and rendering its real tooltip to an offscreen
     `RenderTarget2D`: confirmed a Weapon tooltip shows white name/description with a red Damage line;
     confirmed an AbilityItem tooltip — the richest case, exercising all four categories in one
     tooltip — shows white name/description, a green "+40 MaxHealth, +40 MaxMana, +7 Wisdom" bonus
     line, a red "Damage: 115 - 220" line, and a blue "60 Mana Cost" line, each correctly separated
     onto its own line and colored; and confirmed an Armor tooltip shows white name/description with
     a green bonus line. Temp code fully reverted (`git diff --stat Game1.cs` clean), scratch PNGs
     deleted, clean build, plain boot-check, and a full real-save-file diff all passed with zero
     differences.

204. **Three more direct follow-ups**: extended the retro font to Portal/boss-name text, added a gold
     equipment-bonus "+N" to the HUD stat lines, and brought the bank/inventory comparison tooltip's
     colors in line with entry 203's scheme.
     Portal/boss text: swapped `Art.HudFont` → `Art.RetroFont` in `Portal.cs` (dungeon-name label,
     the entry-confirmation hint, and each dropped portal's own floating label) and
     `States/BossRealmState.cs` (the boss name above its health bar) — explicitly excluded from
     entry 202's scope at the time, now brought in. Colors and the existing drop-shadow rendering
     style (a separate offset black copy, not `Util.DrawOutlinedText`'s full outline) were left
     exactly as they were, per this request's own "preserve colors for now" — only the font changed.
     HUD stat bonus: `Overlay.DrawStats()`'s local `DrawStatLine()` gained an optional `equipBonus`
     parameter — when nonzero, draws "+N" in gold immediately to the right of the stat's existing
     value text, same "call out the gear-only contribution in gold" idea as the HP/MP bars' "(+N)"
     from entry 197, just as its own separate segment rather than appended into the value string.
     Wired to each of the six stat lines' own `Player.EquipmentXBonus` (ATT/DEF/SPD/DEX/VIT/WIS) —
     widened those from `protected` to `public` in `Player.cs`, same precedent as
     `EquipmentMaxHealthBonus`/`EquipmentMaxManaBonus` in entry 196. "Level:" has no equipment-bonus
     concept, so it keeps the parameter's `0` default (nothing drawn).
     Bank/inventory tooltip colors: extracted `DrawCategorizedTooltip()`'s line classifier into a
     shared private `Util.ClassifyTooltipLine()`, then reworked the `List<(Text,Better)>` overload of
     `Util.DrawTooltip()` (the one `Equipment.ComparisonLines()` feeds, used by
     `BankSystem`/`InventorySystem`/`LootBag`'s hover tooltips) to use it for each line's base color
     instead of a flat `textColor` parameter — with `Color.Gold` overriding that whenever `Better` is
     true, a deliberate design choice (not explicitly requested, but a natural extension of the
     session's existing "gold = a gear-related callout" language) so an item beating what's equipped
     still reads as a distinct, unmistakable signal regardless of which category color the line would
     otherwise get, rather than reusing green (already the stat-bonus category color, which would
     have made "this is an upgrade" and "this is a normal stat line" visually indistinguishable). This
     dropped the now-unused `textColor`/`betterColor` parameters entirely — updated all three real
     callers (`BankSystem.cs`/`InventorySystem.cs`/`LootBag.cs`) to the new 4-argument shape, and also
     changed their separate plain-string fallback tooltip (a bare item name for non-equipment items
     like potions) from flat Red to White, matching the categorized scheme's own "plain text" default.
     Verified via a temporary `Game1.StartGame()` test rendering three scenarios to offscreen
     `RenderTarget2D`s: the real sidebar's stat block, confirming gold "+N" appears next to
     ATT/DEF/DEX/VIT/WIS (each matching that stat's actual equipped gear bonus) with SPD correctly
     showing no "+N" (no equipped item grants a Speed bonus) and no overlap with the existing
     "(x / y)" text; a real `Portal()` constructed and drawn directly (came back blank — the portal's
     `Position` is in world space and this test's `SpriteBatch.Begin()` has no camera transform, a
     test-harness limitation rather than a rendering bug, so this specific check was inconclusive and
     relies instead on the font already being proven correct in dozens of other contexts this
     session); and `Util.DrawTooltip()`'s list overload called directly with a representative 6-line
     set (name, description, a non-better bonus, a better bonus, non-better damage, better mana cost)
     — confirmed white/white/green/red render correctly by category and both "better" lines (defense
     bonus, mana cost) render gold regardless of their underlying category, proving the override rule
     works independent of content type. Clean build, plain boot-check, and a full real-save-file diff
     all passed with zero differences.

205. **Made Jersey10 the base font essentially everywhere**: added the missing black outline to
     Portal/boss text, then extended the retro font to Character Select, every menu button, the
     Settings screen, and the title screen (including the boss-fight announcement banner, which
     shares the title's exact look) — closing out almost every exclusion from entry 202's scope
     question in one pass, per five explicit follow-up requests plus a new blanket rule: "all text
     should have a black outline unless specified."
     Portal/boss outline: `Portal.cs`'s dungeon-name label, entry-confirmation hint, and each
     portal's floating label all used a manual single-offset colored shadow (the same style
     `CharacterSelectState`'s old `DrawShadowedText` used) rather than a true outline — replaced
     with `Util.DrawOutlinedText()`, dropping the separate shadow-color draw entirely.
     `BossRealmState.cs`'s boss-name label got the same treatment (it had no shadow/outline at all
     before).
     Character Select: swapped `Art.HudFont` → `Art.RetroFont` throughout, and fixed
     `DrawShadowedText()` (its local helper — kept the name rather than touching its ten call sites)
     to call `Util.DrawOutlinedText()` internally instead of drawing its own offset shadow, upgrading
     every caller to a full outline in one change; the two remaining raw `spriteBatch.DrawString`
     calls (the "Select a Character" subtitle and each class's name label) were converted directly.
     Buttons: `Controls/Button.cs`'s three constructors' default font changed from `Art.HudFont` to
     `Art.RetroFont` (the third, explicit-font overload used only by Settings' Back/Reset buttons,
     was untouched — it already takes whatever font its caller passes), and `Button.Draw()` now
     calls `Util.DrawOutlinedText()` instead of a plain `DrawString` — this alone covers every button
     in the game (Main Menu, Character Select's Back/Erase, Game Over's three buttons) except
     Settings', which passes its own font explicitly.
     Settings: swapped `Art.SettingsFont` → `Art.RetroFont` throughout `SettingsState.cs` (font sizes
     happened to already match — both 14pt) and converted every one of its ~10 `DrawString` call
     sites (tab labels, key-binding rows, toggle/slider rows, the "Settings" title) to
     `Util.DrawOutlinedText()`, preserving each row's existing Gold-on-hover/White logic.
     `Art.SettingsFont` itself now has zero remaining consumers, but — unlike `Art.DamageFont` in
     entry 202 — was left in place rather than deleted, since removing an asset wasn't part of this
     request and it costs nothing to leave loaded.
     Title screen: `Overlay.DrawTitle()` (the "Realm" logo) swapped `Art.TitleFont` (96pt Arial) for
     `Art.RetroFont` (14pt) drawn at a new `Overlay.TitleScale` (6x) — a pixel font needs a real
     `scale` multiplier rather than a bigger point size to stay crisp, and `Util.DrawOutlinedText()`
     already scales its outline offset by that same factor, so the outline automatically thickens
     proportionally instead of looking too thin at 6x. Fill color (DarkMagenta) preserved; the old
     DarkOrange offset-shadow became a plain black outline, per this request's own new blanket rule.
     `BossRealmState.cs`'s boss-announcement banner shared this exact `Art.TitleFont`-based look, so
     it moved to `Art.RetroFont` + `Overlay.TitleScale` too — its existing "shrink to fit the
     viewport" logic (previously capping at native scale 1.0) now caps at `TitleScale` instead, so a
     short boss name still gets the full title-sized treatment while a long multi-word one scales
     down further to fit, exactly as before. `Art.TitleFont` keeps one real remaining consumer
     (`GameOverState.cs`'s "Score"/"Fame Earned" text) — not requested this round, so left untouched
     and the asset was not removed.
     Verified via a temporary `Game1.StartGame()` test rendering five scenarios to offscreen
     `RenderTarget2D`s (careful this time to only wrap `Begin()`/`End()` around raw draw calls, not
     around `State.Draw()` methods that already manage their own — an early attempt crashed with
     "Begin cannot be called again until End has been called" until this was fixed): a real
     `MenuState.Draw()` confirmed "Realm" renders large, crisp, and outlined with no overflow, and
     all four menu buttons render correctly; a real `CharacterSelectState.Draw()` confirmed class
     names, "Stars: -----", and both the "Back" and "Erase All Data" buttons render correctly with
     colors preserved; a real `SettingsState.Draw()` confirmed the tab bar, key-binding rows, and
     both buttons render correctly with the active tab's Gold highlight intact; and a direct
     `Util.DrawOutlinedText()` call at both a long multi-word test boss name and a short one
     confirmed the announcement banner's fit-to-viewport math actually produces a smaller scale for
     the long name (3.14) and the full `TitleScale` cap (6.0) for the short one, both rendering
     correctly outlined. A real `Portal()` constructed and drawn directly came back blank — portals
     draw in world space and this test's plain `SpriteBatch.Begin()` has no camera transform, a
     test-harness limitation (not a rendering bug) already flagged the same way in entry 204, so this
     specific check remains unverified by render and relies on the font/outline pattern already being
     proven correct everywhere else. Clean build, plain boot-check, and a full real-save-file diff all
     passed with zero differences.

206. **Fixed blurry title/boss-announcement text and hard-to-read black-on-black button text**,
     both flagged directly after entry 205 shipped. Root cause of the blur: entry 205 drew
     `Art.RetroFont` (baked at a small native 14pt) stretched 6x via `SpriteBatch.DrawString`'s
     `scale` parameter — a small rasterized glyph bitmap has no lossless way to get bigger, and the
     default linear sampler smears it on upscale. Root cause of the button issue: `Button.Draw()`
     started outlining text in black (entry 205) without also checking `PenColor`'s own default,
     which was still `Color.Black` — a black outline under black fill renders as one undifferentiated
     blob, hiding the pixel font's own letterform detail rather than merely being low-contrast.
     For the blur: added two more baked-at-native-size Jersey10 assets rather than any runtime
     scaling trick — `Art.RetroFontLarge` (84pt, matching the old 14pt-at-6x target size) for
     `Overlay.DrawTitle()` and `BossRealmState`'s boss-announcement banner, and `Art.RetroFontButton`
     (18pt vs `RetroFont`'s 14pt, "slightly larger" per the second request) for
     `Controls/Button.cs`. `Overlay.TitleScale` changed meaning from "stretch small font 6x" to "cap
     at native size, shrink below 1 if needed" (redefined `1f` instead of `6f`) — downscaling a large
     baked bitmap loses fine detail gracefully, the opposite problem from upscaling a small one, so
     `BossRealmState`'s existing "shrink a long boss name to fit the viewport" logic needed no
     structural change, just the new font and cap value.
     For the buttons: changed all three of `Button`'s constructors' default `PenColor` from
     `Color.Black` to `Color.White` — callers that already set `PenColor` explicitly afterward (the
     Erase All Data/confirm buttons' Red/DarkRed) are unaffected, since this only changes what "the
     caller didn't set one" now means.
     Verified via a temporary `Game1.StartGame()` test rendering a real `MenuState.Draw()` and a
     direct boss-announcement draw (both long and short test names) to offscreen `RenderTarget2D`s,
     then inspecting 3x-zoomed crops: confirmed "Realm" and "Slime" (drawn at `RetroFontLarge`'s
     native scale, no stretching at all) both show hard, fully crisp pixel edges with zero softness —
     a direct, visible contrast against entry 205's noticeably blurred version of the same text —
     and confirmed the long boss name (downscaled to ~3.14x smaller than native, since it would
     otherwise overflow the viewport) stayed equally crisp, proving shrinking doesn't reintroduce the
     problem upscaling did. Also confirmed the menu buttons now show white fill with a clearly
     visible black outline and legible letterform detail (no more solid-black blob), at a visibly
     larger, easier-to-read size. Clean build, plain boot-check, and a full real-save-file diff all
     passed with zero differences.

207. **Made every button use the same font as Settings' own buttons**, per direct feedback that the
     dedicated 18pt `Art.RetroFontButton` from entry 206 should instead match Settings' Back/Reset
     buttons, which already draw at `Art.RetroFont` (14pt) via `Button`'s explicit-font constructor.
     Changed `Button()`'s and `Button(Texture2D)`'s default `font` field from `Art.RetroFontButton`
     to `Art.RetroFont` — the third constructor (`Button(Texture2D, SpriteFont)`, Settings' own) was
     already on `Art.RetroFont` and needed no change. This left `Art.RetroFontButton` with zero
     remaining consumers, so it was removed outright the same way `Art.DamageFont` was in entry
     201/202: deleted `Content/Fonts/RetroFontButton.spritefont`, its `Content.mgcb` build block, and
     the `Art.RetroFontButton` field/load line. Verified via `dotnet build` (0 errors) and a plain
     minimized boot-check; all real save files confirmed byte-identical to a pre-test backup
     afterward.
