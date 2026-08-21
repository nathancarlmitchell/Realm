# Realm — Idea Backlog

Feature ideas discussed for Realm, not yet scheduled. Companion docs: [DEVLOG.md](DEVLOG.md)
(what's shipped) and [BUGFIXES.md](BUGFIXES.md) (bugs found and fixed). Mirrored from this
project's Claude Code memory notes so it travels with the repo instead of staying on one
machine — this file is the canonical copy; update it directly.

Running list of feature ideas discussed for Realm (top-down ARPG, C#/MonoGame,
`C:\Users\Nathan\Downloads\Realm-main-claude\Realm-main`), beyond what's already built. Not
prioritized or scheduled — the user asked to keep these noted for later rather than act on them now.

## Open ideas

- **Add a real ability cooldown timer.** Today `UseAbility()` (Wizard's spell bomb, Archer's shot)
  is gated only by mana cost (`Player.AbilityCost`, 25) — there's no time-based lockout, so a
  player with enough mana can spam it repeatedly. The user chose to visualize the existing
  mana-gate as the HUD cooldown bar for now (see "recently completed" below) rather than add this,
  but flagged a real duration-based cooldown (independent of mana, on top of it) as a future
  addition — needs a duration decision (e.g. 1-2s) when picked up.
- **Per-enemy drop pools — extending the now-complete mechanism to enemies beyond Snake/BigSnake.**
  Both halves of the original ask now exist: category inclusion/exclusion
  (`Enemy.DropPool`/`ItemSpawner.LootCategory`, [DEVLOG.md](DEVLOG.md)
  entry 135) and per-category weighted odds on top of it (`Enemy.DropWeights`,
  [DEVLOG.md](DEVLOG.md) entry 136). Applied so far to
  exactly the two enemies the backlog itself named — `CreateSnake()` (gear-only `DropPool`) and
  `CreateBigSnake()` (potion-leaning `DropWeights`) — every other enemy still defaults to `All`/no
  weighting, unchanged. What's left is purely applying the existing mechanism to more enemy types,
  once specific pools/weights are decided per enemy — no new engine work needed, just picking
  values.
- **Animate the player character.** Blocked on art: no walk-cycle sprite sheets exist yet (just
  static "player"/"archer" textures). The engine-side piece isn't new work — `AnimatedTexture.cs`
  already implements horizontal-strip sprite-sheet animation and is used for the portal
  (`Art.Portal`, `portal.png`, 7 frames). Process discussed: (1) art — one sprite sheet per
  animation state per class, same equal-width-frames-in-a-row layout as `portal.png`; (2) each new
  sheet needs an explicit `#begin`/`#build` block added to `Content/Content.mgcb` — MonoGame's
  content pipeline doesn't auto-scan the folder; (3) code — give `Player` one or more
  `AnimatedTexture` instances, call `UpdateFrame()` from `Player.Update()`, swap `Draw()` to
  `DrawFrame()` instead of the static `Entity.Draw()` texture draw, switching sheets based on
  whether `Velocity` is non-zero. Open design questions before any art gets made: how many states
  (idle/walk minimum; attack/hit/death cost a lot more art), frame count/playback speed, and how
  facing direction works (rotate one sheet vs. separate directional sheets — `Player.Orientation`
  isn't currently wired to face movement/aim direction).
- **More enemy variety, further additions** — three new enemies already shipped (see
  [DEVLOG.md](DEVLOG.md)'s two latest entries): Slime and
  Brute (tinted reskins of existing sprites, no new art needed) and BigSnake (real user-supplied art,
  `Content/Enemies/big_snake.png`). Remaining unused movement+attack combos from `Enemy.cs`'s
  existing behavior toolkit (`FollowPlayer`/`MoveSnake`/`MoveRandomly` ×
  `Spray`/`Shoot`/`Bomb`): `MoveRandomly`+`Shoot`, `FollowPlayer`+`Shoot` are still open for a future
  enemy (`MoveSnake`+`Spray` is still open too, though `MoveSnake`+`Shoot` at a different speed is
  now covered by BigSnake). Real new art (rather than another tinted reskin) is also still an
  option whenever the user wants to supply some.
- **Boss follow-ups, both bosses now need a real playtest pass.** Limon: real hit/death sounds
  (still reusing `Sound.SpriteGod*` as placeholders) and revisiting hand-tuned balance numbers
  after actually playing the fight. Stheno (see [DEVLOG.md](DEVLOG.md) entry 100): also reusing
  placeholder audio (`Sound.Snakes*`); every numeric constant in `Bosses/SthenoTheSnakeQueen.cs`/
  `SthenoPet.cs`/`SthenoSwarm.cs` (phase/cooldown durations, grenade radii/damage, orbit/spiral
  speed, center-check radius) is a first-pass estimate needing an actual playtest to confirm the
  spiral reads as a spiral, the grenade dodge-gaps are actually walkable, pet orbit speed reads as
  "rapidly circling," and swarm charge speed/distance reads as "a straight line." The second-boss
  extension pattern predicted in this item (its own `XyzBoss : Boss` subclass, nothing moved into
  the shared `Boss` base beyond what turned out genuinely common — just `Name`/`Description`/
  `SpawnLoot()`/`DrawHealthBars()`) held up as designed.
- **Remove the test-only boss portals in the Nexus.** `States/NexusState.cs`'s `portalList` has two
  `// TEMP`-commented shortcut portals: `Portal.Destination.BossRealm` at `Player.Instance.Position
  + (-150, -100)` and `Portal.Destination.SthenoBossRealm` at `+ (-150, +100)`, added purely so
  each boss arena could be reached directly for testing without needing to find and kill a
  SpriteGod/BigSnake first. The user said the first one should come out "at some point" — remove
  both portal list entries (and their `bossTestPortalPos`/`sthenoTestPortalPos` variables) once
  both boss fights are done being tested; the real access paths (SpriteGod/BigSnake → dropped
  portal) stay as-is.
- **Multiple rooms/floors** instead of one open world area — locked doors needing a key drop, a
  portal to the next floor.
- **What Fame should unlock — remaining pieces.** Class unlocks (see
  [DEVLOG.md](DEVLOG.md) entry 80) and the per-class star
  rating (entry 81, technically a separate progression metric from Fame, not spent from it) are
  done. The user's full vision is larger: cosmetic skins, additional bank storage slots, alternate
  starting gear tiers for a fresh run (Tier 0 by default today), an unlock that raises all stats to
  max, and eventually a proper shop where Fame is spent directly on specific items (as opposed to
  class unlocks, which just check the account's cumulative total — spending would need Fame to
  actually be consumed, which nothing does yet). No numbers/scope decided for any of these beyond
  class unlocks' 1,000/3,000 thresholds and the star system's 20,000-base-doubling thresholds; pick
  up one piece at a time rather than all at once, same approach as these first two slices.
- **A 4th+ character class beyond Wizard/Archer/Knight**, if ever wanted — the extension pattern is
  now proven three times over (see [DEVLOG.md](DEVLOG.md)
  entry 31 for Knight, the most recent).
- **Flesh out the ability system.** Currently each class has exactly one `UseAbility()` (Space key)
  with damage/cost modified by the equipped `AbilityItem`. Open-ended — could mean multiple
  abilities per class, an ability hotbar, or more distinct ability behaviors per class rather than
  each being a single damage-number roll.
- **Color-coded tier indicator on item icons** — an outline or overlay tinted by `Equipment.Tier`,
  so tier is visible at a glance without hovering. Would touch the same icon draw sites as the
  wrong-class-equipment overlay (see
  [DEVLOG.md](DEVLOG.md) entry 64 —
  `InventorySystem.Draw()`/`BankSystem.Draw()`, likely also each `DrawEquipped()`), reusing the
  same `Art.HealthBar`-stretched-into-a-rect technique that overlay already uses. Needs a
  tier-to-color mapping decided (a fixed palette by tier number, or a gradient) before
  implementation.
- **Scroll-to-zoom, both the main game camera and the minimap, active whenever the mouse is over
  that respective area.** Two separate targets: [Camera.cs](Camera.cs) already has a clamped `Zoom`
  property (`0.5`-`1.5`, set once to a fixed `1f` in `RealmState`'s constructor and never changed
  after) with no input wired to it yet; the minimap ([Overlay.cs](Overlay.cs)'s `DrawMinimap()`,
  `MinimapWorldRadius` constant, currently a fixed 2000-unit view) has no zoom concept at all today.
  Also needs mouse-wheel input added to [Input.cs](Input.cs) from scratch — nothing reads
  `MouseState.ScrollWheelValue` anywhere in the codebase yet. "Mouse is over that area" naturally
  splits into two hit-tests: over the minimap's screen rect (`Overlay.cs`'s existing `mapX`/`mapY`/
  `MinimapSize` bounds) vs. over the rest of the gameplay viewport for the main camera.
- **Teleport via the minimap** — click a spot on the minimap to teleport the player there. Today
  [Overlay.cs](Overlay.cs)'s `DrawMinimap()` is purely visual, no click handling at all; the minimap
  only shows a fixed 2000-unit radius around the player (`MinimapWorldRadius`), so a click position
  would need converting from map-relative screen coordinates back into a world position (the
  inverse of the existing blip-placement math) — straightforward for the open Realm, but worth
  deciding whether this is even allowed in a boss arena (a much smaller, bounded space where
  free teleporting could trivialize or break the fight). Also needs a decision on whether it's a
  free instant teleport or has some cost/limit (cooldown, mana, distance cap) so it doesn't just
  replace normal movement outright.
- **Visual effects — further targets beyond the first one shipped.** A hand-rolled particle system
  now exists (`Particle.cs`, see [DEVLOG.md](DEVLOG.md) entry
  141) — a lightweight `Entity` subclass managed by the normal `EntityManager` pipeline, no MonoGame
  `Effect`/shader usage anywhere (that approach was explicitly considered and passed over). Currently
  wired to exactly one target: `Enemy.WasShot()`'s hit/death bursts. Open follow-ups: a portal glow
  (the other candidate originally raised, not yet built), particle effects on the *player* taking
  damage (deliberately out of scope this round — only enemies were wired up), and any other
  on-screen moment that could use the same `Particle.SpawnBurst()` entry point.

## Completed

Moved to a dedicated log so it isn't duplicated here — see
[DEVLOG.md](DEVLOG.md) for everything built so far, and
[BUGFIXES.md](BUGFIXES.md) for bugs found and fixed along the way. When an item
above is completed, remove it from Open ideas and append it to the completed-features log instead
of leaving it here.
