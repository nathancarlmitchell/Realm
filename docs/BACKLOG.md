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
- **Boss follow-ups, remaining two of four** (the top-of-screen name+health bar and a fading
  appearance announcement are done — see
  [DEVLOG.md](DEVLOG.md) entry 75): real hit/death sounds
  for Limon (still reusing `Sound.SpriteGod*` as placeholders — needs the user to supply actual
  audio files) and revisiting the current hand-tuned balance numbers after actually playing the
  fight (needs the user's own feel for the fight, not something to guess at). A future second boss
  should be its own `XyzBoss : Boss` subclass (see entry 53) — only move something into the shared
  `Boss` base if it turns out genuinely common to multiple bosses, not guessed now with only Limon
  built.
- **Remove the test-only boss portal in the Nexus.** `States/NexusState.cs`'s `portalList` has a
  4th portal (`Portal.Destination.BossRealm`, positioned at `Player.Instance.Position + (-150,
  -100)`, marked with a `// TEMP` comment right above it) added purely so the boss arena could be
  reached directly for testing, without needing to find and kill a SpriteGod first. The user said
  this should come out "at some point" — remove that one portal list entry (and its
  `bossTestPortalPos` variable) once the boss fight itself is done being tested; the real access
  path (SpriteGod → dropped portal) stays as-is.
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
- **Update `Portal.Destination` from an enum to an instance-based model.** [Portal.cs](Portal.cs)
  currently uses a fixed `Destination` enum (`Realm`, `CharacterSelect`) with a switch in
  `EnterPortal()` and a parallel switch in the new `DisplayName` property — fine for a small,
  known set of named game states, but it can't represent "go to room #7" once **multiple
  rooms/floors** (above) exists, since that would need a case added per room. Deferred until that
  feature is actually being designed, so the right shape (an ID + display name? a small class
  loaded from a JSON catalog like `ArmorData`/`SpellData`?) can be picked with real requirements
  instead of guessed now.
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
- **Extend the new settings system beyond key bindings.** [KeyBindings.cs](KeyBindings.cs)/
  [Data/KeyBindingsData.cs](Data/KeyBindingsData.cs)/`States/SettingsState.cs` (see
  [DEVLOG.md](DEVLOG.md)'s latest entry) are the first
  settings built, deliberately scoped to just key bindings per the user's own choice when this was
  built. `Player.AutoFireEnabled` (session-only today, resets every launch) was the concrete
  candidate raised at the time for a future second setting — persisting it would mean adding it to
  `KeyBindingsData`-alongside-or-a-sibling-DTO and a toggle row/control on the Settings screen, not
  a new UI or save-file pattern from scratch.
- **Teleport via the minimap** — click a spot on the minimap to teleport the player there. Today
  [Overlay.cs](Overlay.cs)'s `DrawMinimap()` is purely visual, no click handling at all; the minimap
  only shows a fixed 2000-unit radius around the player (`MinimapWorldRadius`), so a click position
  would need converting from map-relative screen coordinates back into a world position (the
  inverse of the existing blip-placement math) — straightforward for the open Realm, but worth
  deciding whether this is even allowed in a boss arena (a much smaller, bounded space where
  free teleporting could trivialize or break the fight). Also needs a decision on whether it's a
  free instant teleport or has some cost/limit (cooldown, mana, distance cap) so it doesn't just
  replace normal movement outright.
- **Confirmation option when entering portals.** Today [Portal.cs](Portal.cs)'s `Update()` triggers
  `EnterPortal()` the instant `Player.Instance.Bounds.Intersects(bounds)` — walking through a portal
  (or getting knocked/kited into one) commits immediately with no prompt, for every destination
  (`Realm`, `CharacterSelect`, `BossRealm`, `Nexus` — the Bank portal is a separate proximity-based
  panel, not a teleport trigger, and wouldn't need this). Needs deciding: a confirmation for every
  portal, or just the more consequential ones (leaving an active Realm run mid-fight vs. the Nexus's
  own low-stakes portals); and whether it's a toggleable setting (ties into the same "no settings/
  config system exists yet" gap as key remapping and auto-fire, above) or just always-on.
- **Improve the portal image and visuals.** Today [Portal.cs](Portal.cs) just plays `Art.Portal`, a
  plain 7-frame `AnimatedTexture` sprite sheet, with no other visual treatment — same look for
  every portal regardless of destination. Open-ended (new/reworked art, a per-destination color
  tint, something more elaborate) — needs the user's own direction on what "improved" means before
  picking an approach.
- **Shaders / custom visual effects in general** — a portal glow, on-hit or on-death particle
  effects, etc. Nothing like this exists in the engine today: no MonoGame `Effect`/shader usage
  anywhere in the codebase (confirmed via a repo-wide search — the only `Effect`-named things are
  `SoundEffect`, unrelated), no particle system, every visual is a plain static or `AnimatedTexture`
  sprite-sheet draw. A real engine addition, not a small tweak — would need deciding on an approach
  (hand-rolled particle system vs. a MonoGame shader/`Effect` pipeline) and a first concrete target
  to build it against (the portal glow and on-hit/on-death particles above are the two candidates
  raised so far) rather than building generic infrastructure with nothing using it yet.

## Completed

Moved to a dedicated log so it isn't duplicated here — see
[DEVLOG.md](DEVLOG.md) for everything built so far, and
[BUGFIXES.md](BUGFIXES.md) for bugs found and fixed along the way. When an item
above is completed, remove it from Open ideas and append it to the completed-features log instead
of leaving it here.
