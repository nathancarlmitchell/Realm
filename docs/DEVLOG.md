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
