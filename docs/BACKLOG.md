# Realm — Idea Backlog

Feature ideas discussed for Realm, not yet scheduled. Companion docs: [DEVLOG.md](DEVLOG.md)
(what's shipped) and [BUGFIXES.md](BUGFIXES.md) (bugs found and fixed). Mirrored from this
project's Claude Code memory notes so it travels with the repo instead of staying on one
machine — this file is the canonical copy; update it directly.

Running list of feature ideas discussed for Realm (top-down ARPG, C#/MonoGame,
`C:\Users\Nathan\Downloads\Realm-main-claude\Realm-main`), beyond what's already built. Not
prioritized or scheduled — the user asked to keep these noted for later rather than act on them now.

## Open ideas

- **Destructible-tile follow-ups.** The core mechanic shipped (`DungeonMap.DamageTile()`,
  `TileDefData.DestructibleHealth` — see [DEVLOG.md](DEVLOG.md)) as narrowly scoped: only player
  projectiles damage a destructible tile, and a broken tile always becomes a random floor-candidate
  tile with nothing left behind. Open beyond that: whether enemy fire should also be able to break
  one (currently it can't — a deliberate genre-convention default, not settled with the user);
  whether a broken tile should drop loot or leave a rubble/visual variant instead of plain floor;
  any hit/break VFX or sound (`Particle.SpawnBurst()` is the existing reusable entry point, not
  wired up here).
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
- **Boss follow-ups, all four bosses now need a real playtest pass.** Limon: real hit/death sounds
  (still reusing `Sound.SpriteGod*` as placeholders) and revisiting hand-tuned balance numbers
  after actually playing the fight. Stheno (see [DEVLOG.md](DEVLOG.md) entry 100): also reusing
  placeholder audio (`Sound.Snakes*`); every numeric constant in `Bosses/SthenoTheSnakeQueen.cs`/
  `SthenoPet.cs`/`SthenoSwarm.cs` (phase/cooldown durations, grenade radii/damage, orbit/spiral
  speed, center-check radius) is a first-pass estimate needing an actual playtest to confirm the
  spiral reads as a spiral, the grenade dodge-gaps are actually walkable, pet orbit speed reads as
  "rapidly circling," and swarm charge speed/distance reads as "a straight line." Cube God (see
  [DEVLOG.md](DEVLOG.md), latest entry): reuses `Sound.DefaultHit` entirely (no dedicated audio at
  all, not even a borrowed family like Limon/Stheno get); every numeric constant across
  `Bosses/CubeGod.cs`/`CubeOverseer.cs`/`CubeDefender.cs`/`CubeBlaster.cs` (HP/Defense/PointValue,
  shotgun damage/pellet-count/cooldown, phase-flash thresholds, Overseer/minion respawn intervals)
  is a first-pass scale-down from RotMG's own multiplayer numbers, not derived from real balance
  testing — needs an actual playthrough to confirm the shotgun volleys are dodgeable, the "cube
  system" escort density feels right, and the fight reads as tougher than Stheno without being
  unfair. The second-boss extension pattern predicted in this item (its own `XyzBoss : Boss`
  subclass, nothing moved into the shared `Boss` base beyond what turned out genuinely common —
  just `Name`/`Description`/`SpawnLoot()`/`DrawHealthBars()`) held up as designed, now proven a
  third time by Cube God, and a fourth by Dreadstump the Pirate King (Pirate Cave, see
  [DEVLOG.md](DEVLOG.md)): also reuses `Sound.DefaultHit` entirely, same placeholder-audio status as
  Cube God; every numeric constant in `Bosses/DreadstumpThePirateKing.cs` is read directly off the
  wiki's own attack table (not scaled down or estimated), but the health-percentage phase
  thresholds, the ship-cannon lane positions (simplified from the wiki's 6 exact cannons to 4
  representative ones), and the alternating-attack/self-Armor cooldown values are all first-pass —
  needs an actual playthrough to confirm the phase transitions read as escalating rather than
  jarring, the cannon lanes are dodgeable, and the fight feels appropriately easy for the
  "beginner dungeon" boss it's meant to be. Also needs a real playtest pass: Snake Pit's own
  Treasure Room mini-boss, `Enemies/SnakePit/SnakepitGuard.cs` (see [DEVLOG.md](DEVLOG.md)) — a
  plain `Enemy` subclass rather than `Boss` (it fights inline in the dungeon, not a
  `BossRealmState`), same "wiki numbers as-is, first pass" status as every boss above: HP/DEF/PV
  and every attack's damage/speed/range are the wiki's own numbers, but the phase-2 oscillation
  speed, every attack's cooldown, and the Ring/AbilityItem drop tiers (the wiki calls out Weapon/
  Armor tiers explicitly but not those two) are all estimates. The Treasure Room itself is also a
  simplification worth another look eventually: the room keeps its normal generated rectangular
  shape rather than the wiki's own distinctly-shaped "long room," and the wiki's "faster Snake
  Spinner bursts triggered whenever [the Guard] reaches either end of its oscillation range" wasn't
  implemented — phase 2 only has its own continuous attacks, no extra burst tied to reaching an
  end.
- **Remove the test-only boss portals in the Nexus.** `States/NexusState.cs`'s `portalList` has
  three `// TEMP`-commented shortcut portals: `Portal.Destination.BossRealm` at
  `Player.Instance.Position + (-150, -100)`, `Portal.Destination.SthenoBossRealm` at
  `+ (-150, +100)`, and `Portal.Destination.CubeGodBossRealm` at `+ (-150, +300)`, added purely so
  each boss arena could be reached directly for testing without needing to find and kill a
  SpriteGod/BigSnake/Cube first. The user said the first one should come out "at some point" —
  remove all three portal list entries (and their `bossTestPortalPos`/`sthenoTestPortalPos`/
  `cubeTestPortalPos` variables) once all three boss fights are done being tested; the real access
  paths (SpriteGod/BigSnake/Cube → dropped portal) stay as-is.
- **Multiple rooms/floors** instead of one open world area — locked doors needing a key drop, a
  portal to the next floor.
- **What Fame should unlock — remaining pieces.** Class unlocks (see
  [DEVLOG.md](DEVLOG.md) entry 80) are done. The per-class star rating (entry 81) is **no longer a
  separate metric from Fame** — the Fame rework (entry 186) redefined it to run on Fame thresholds
  directly (`Player.ComputeStars`, "Class Quests"), replacing the old `HighScore`-doubling basis.
  The user's full vision for Fame-as-currency is still larger than what exists: cosmetic skins,
  additional bank storage slots, alternate starting gear tiers for a fresh run (Tier 0 by default
  today), an unlock that raises all stats to max, and eventually a proper shop where Fame is spent
  directly on specific items (as opposed to class unlocks, which just check the account's cumulative
  total — spending would need Fame to actually be consumed, which nothing does yet — entry 186 only
  adds *earning* Fame faster/differently, not spending it). No numbers/scope decided for any of
  these beyond class unlocks' 1,000/3,000 thresholds and the Class Quest system's 20/500/1500/5000/
  15000 Fame thresholds; pick up one piece at a time rather than all at once, same approach as these
  first slices.
- **Fame/XP rework follow-ups, deliberately left out of entry 186's scope.** The user's own Fame
  rework spec also described a wider XP-formula overhaul this pass didn't build: Exaltations (a
  stat-based bonus, "+5% XP for every 8 stat Exaltations"), a consumable XP Booster item, and
  dungeon-wide XP modifiers (events/dungeon modifiers affecting the whole instance) — none of these
  three systems exist anywhere in this codebase today, and each would be its own substantial
  addition (a new per-class Exaltation-tracking mechanic, a new consumable item type, a new
  dungeon-modifier system) rather than a tweak to something already there. The next-level XP cap
  (10%/20%) from the same spec *was* implemented (`Enemy.NextLevelXpCapFraction`).
- **A 5th+ character class beyond Wizard/Archer/Knight/Priest**, if ever wanted — the extension
  pattern is now proven four times over (see [DEVLOG.md](DEVLOG.md) entry 171 for Priest, the most
  recent).
- **Flesh out the ability system.** Currently each class has exactly one `UseAbility()` (Space key)
  with damage/cost modified by the equipped `AbilityItem`. Open-ended — could mean multiple
  abilities per class, an ability hotbar, or more distinct ability behaviors per class rather than
  each being a single damage-number roll.
- **Color-coded tier indicator on item icons** — a text label version already shipped (see
  [DEVLOG.md](DEVLOG.md) entries 247/248: `Equipment.DrawTierLabel()`, a plain white "T{Tier}" in
  the bottom-right corner of every equip slot/inventory/bank/loot-bag icon, gated by "Display Item
  Tiers" in Settings > Graphics). Still open, and genuinely different from what shipped: an actual
  *color-coded* outline or overlay tinted by `Equipment.Tier` itself, so tier reads at a glance from
  color alone rather than needing to read a two-character label. Would reuse the same
  `Art.HealthBar`-stretched-into-a-rect technique the wrong-class-equipment overlay already uses
  (entry 64). Needs a tier-to-color mapping decided (a fixed palette by tier number, or a gradient)
  before implementation.
- **Free-form teleport via the minimap** — click *any* spot on the minimap to teleport the player
  there, the original broader idea. What shipped instead (entries 234/236) is narrower and
  purpose-built: clicking the Beach Beacon's own blip specifically teleports to that one fixed,
  already-discovered location, nothing else is clickable. The general "click anywhere, go there"
  version is still open, and still carries the same open questions this item always had: whether
  it's even allowed in a boss arena (a much smaller, bounded space where free teleporting could
  trivialize or break the fight), and whether it's a free instant teleport or has some cost/limit
  (cooldown, mana, distance cap) so it doesn't just replace normal movement outright.
- **Visual effects — further targets beyond the two shipped.** A hand-rolled particle system now
  exists (`Particle.cs`, see [DEVLOG.md](DEVLOG.md) entries
  141-142) — a lightweight `Entity` subclass managed by the normal `EntityManager` pipeline, no
  MonoGame `Effect`/shader usage anywhere (that approach was explicitly considered and passed over).
  Wired to two targets so far: `Enemy.WasShot()`'s hit/death bursts, and `Player.LevelUp()`'s gold
  celebration burst. Open follow-ups: a portal glow (the original other candidate, not yet built),
  particle effects on the *player* taking damage (deliberately out of scope — only enemy hits/deaths
  were wired up), and any other on-screen moment that could use the same `Particle.SpawnBurst()`
  entry point.
- **Biome system follow-ups.** A first version shipped as concentric distance rings only (see
  [DEVLOG.md](DEVLOG.md) entry 179) — the user explicitly chose rings over lateral variety to keep
  v1 simple. Beach (the innermost ring, replacing the original placeholder "Meadow") originally
  shipped with real enemy art and five mini-bosses (Pirate + Beached Buccaneer, entry 180; Bandit +
  Bandit Leader, entry 181; Little Scorpion + Scorpion Queen, entry 182; Sandsman Archer/Sorcerer +
  Sandsman King, entry 183; Giant Crab, entry 184) alongside five regular enemies (Little
  Blue/Green/Pink Jelly, Piratess, Sand Devil, entry 185) — later reclassified (entry 213) so
  Beached Buccaneer is the only Beach mini-boss left; Bandit Leader/Scorpion Queen/Sandsman
  King/Giant Crab are now regular `BasicEnemyPool` members like everything else, though each still
  runs its own bespoke escort behavior when it spawns. That same entry also gave every Beach enemy
  its own drop-rate table (`Enemy.BeachDropPool`/`BeachDropChances`/`BeachDropTierRanges`) — Beach no
  longer needs "biome-biased loot tiers" as an open item, though the underlying mechanism is still a
  per-enemy override, not `ItemSpawner` reading biome directly; another biome wanting the same
  treatment means giving its own enemies a similar table, not a generic biome-aware knob in
  `ItemSpawner` itself. Only Greedy Crab, of the original 16-sprite art drop, remains unwired. Real
  per-biome *ground* art is still the open item (`Data/BiomeData.json`'s `GroundTileImageName` is
  wired for it — every biome, Beach included, still just points at the shared `Art.Tile` texture,
  told apart only by a color tint). Other open follow-ups: Greedy Crab waiting on the user to specify
  its stats/behavior/tier; angular/sector-based variety within a ring (so two players don't always
  see the same biome at the same distance); Forest/Highlands/Blighted Wastes have no mini-boss of
  their own at all, unlike Beach's one remaining one; and retuning the 4 biomes' distance
  thresholds/enemy rosters, which were placeholder guesses mirroring `EnemySpawner.BasicEnemyPool`'s
  existing level order, not derived from real playtesting.
- **Change tier and item name text color.** Flagged for a future pass; no specific colors decided
  yet. Today's tier/name text color is whatever each draw site already sets directly (e.g.
  `Util.DrawTooltip`'s categorized tooltip-line colors from entry 203, `Overlay`'s stat-line colors,
  each item's own `Color` field where one exists) — needs a decision on which specific text (item
  name specifically vs. its tier indicator, and where each currently renders — equipment tooltips,
  inventory/bank slots, loot bag popup) should change and to what, before implementing.
- **Update loot bag inventory display background.** Flagged for a future pass; no specific look
  decided yet. The loot bag popup's current background is whatever `LootBag.cs`/`InventorySystem.cs`
  draw today — needs a decision on the actual desired appearance before implementing.
- **Finalize font settings and main menu graphics.** A polish/tuning pass on the retro-font rollout
  (entries 195-216) and the title screen background (entries 209-210) — flagged as not yet
  considered "done," but without specific complaints yet beyond what's already been addressed
  (blurry upscaling, black-on-black buttons, button font mismatch, and now the button font itself —
  entry 216 gave buttons their own dedicated Micro5 font, separate from the Jersey10 base font
  again). Needs the user to point out what specifically still looks off once they've spent more
  time looking at it in actual play.
- **Playtest the Beach biome — round 2.** Round 1 (see [DEVLOG.md](DEVLOG.md) entry 228) added a
  nearby-enemy spawn density cap (`EnemySpawner.MaxNearbyEnemies`/`NearbyEnemyRadius`) and a global
  2x enemy damage multiplier (`Difficulty.EnemyDamageMultiplier`). The enemy-HP question that round
  flagged has since been acted on too (entry 233): a matching global `Difficulty.EnemyHealthMultiplier`
  (also 2x) now scales every enemy's health at spawn time. A further round of drop-rate tuning also
  landed since (entry 232 halved every rate outright; the user has since hand-tuned
  `Enemy.BeachDropChances` and some Beach mini-boss stats further still, on top of that). None of
  these — damage x2, HP x2, the density cap, the halved-then-further-hand-tuned drop rates, or the
  reclassified Bandit Leader/Scorpion Queen/Sandsman King/Giant Crab blending into the regular wave
  — have had a real playthrough since landing together. This is a "look at it in play and see" item
  across the board, not a single known target.

- **"Hardcore" mode.** An opt-in mode that raises `Difficulty.EnemyDamageMultiplier` further above
  its regular baseline (currently 2x — see [DEVLOG.md](DEVLOG.md), latest entry), plus other possible
  restrictions floated alongside it — e.g. being unable to escape to the Nexus mid-run (no portal
  retreat once committed). Neither the harder-difficulty multiplier value nor the exact set/shape of
  restrictions has been decided; likely needs its own persisted flag (character-level, like
  `HasReachedLevel20`, or a character-creation-time choice) once picked up, plus UI to actually
  choose it. Purely an idea for now — not scoped or scheduled.

- **Playtest the character-slots screen** (`States/CharacterSlotsState.cs` — see [DEVLOG.md](DEVLOG.md)
  entry 292). Verified so far by code review, a clean build, and two real boot/migration cycles
  (confirming the actual save-file migration is correct and idempotent) — but the click-through UI
  itself (scrolling with more than a screenful of slots, the purchase and delete inline confirms,
  reaching Character Creation from an empty slot, layout/readability of the equipped-item icon row)
  hasn't been interactively played yet. The delete "X" icon is currently just an outlined "X"
  character drawn at icon size (no dedicated art asset exists for it) — worth a real icon if it reads
  as too plain in play.
- **Give Wanderer/Brute/Seeker/Slime/SpriteGod a real drop table.** Consequence of removing the
  implicit PointValue-scaled drop-chance fallback (see [DEVLOG.md](DEVLOG.md) entry 321) — these
  five open-Realm enemies had no explicit `Enemy.DropChances` at all (fully relying on that
  fallback), so as of that change they drop nothing on death until each is given real numbers, the
  same treatment Beach and Pirate Cave already got (`BeachDropChances`/`PirateCaveDropChances`).
  Accepted deliberately for now per direct instruction, not an oversight — no tables/percentages
  decided yet.
- **Organize project file structure.** Requested directly. About 50 `.cs` files sit loose in the
  repo root (`Player.cs`, `Enemy.cs`, `Entity.cs`, `Item.cs`, `Weapon.cs`, `Armor.cs`, `Portal.cs`,
  `Util.cs`, `Sound.cs`, every other core/system/item-type file, etc.), alongside folders that
  already group things logically: `States/`, `Bosses/`, `CharacterClasses/`, `Controls/`, `Data/`,
  `Dungeon/`, `Enemies/`, `Particles/`, `Projectiles/`. No target layout decided yet — candidate
  groupings: a `Systems/` folder for the `*System.cs` singletons (`BankSystem`, `InventorySystem`,
  `FameSystem`, `CharacterSlotSystem`, `ClassRecordSystem`); an `Items/` folder for the
  equipment/item hierarchy (`Item`, `Equipment`, `Weapon`, `Armor`, `Ring`, `Shield`, `Cloak`,
  `Tome`, `Quiver`, `Spell`, `AbilityItem`, `Potion`); leaving the true engine-level files at root
  (`Game1`, `Entity`, `EntityManager`, `Camera`, `Extensions`, `Program`). Every type lives in one
  flat `namespace Realm` regardless of folder today — C# doesn't require folder structure to match
  namespace, so this is a pure filesystem move, not a namespace/`using` rewrite — but worth doing as
  its own dedicated commit (not mixed into a feature change) so the diff is reviewable as a pure
  move, and worth double-checking nothing outside the `.csproj`'s default glob (e.g. a hardcoded
  path anywhere) assumes a file's current location.
- **Playtest the Snake Eye Ring / UT item system** (see [DEVLOG.md](DEVLOG.md), latest entry). First
  pass, several numbers are estimates rather than confirmed values: the 2% `UniqueItemDropChances`
  rate on both Stheno the Snake Queen and Snakepit Guard (the wiki doesn't publish an exact rate,
  just "isn't hard to obtain"), and Speedy's 1.5x movement multiplier (a flat-multiplier
  approximation of real RotMG's "sets Speed to its stat maximum," since this engine's simpler
  Speed-stat model has no equivalent to set to). Worth an actual playthrough to confirm the proc
  reads as noticeable/useful and the drop rate feels right before the same
  `UniqueItemDropChances`/`ReactiveProcBuff` machinery gets reused for a second UT item.

- **Craig the Intern's art sits unused.** Supplied alongside the rest of the Sprite World
  enemy roster (`Content/Dungeons/Sprite World/Craig the Intern.png`), but the wiki gives him no
  combat stats at all (HP/DEF: N/A) — he's a non-hostile NPC cameo, not a real enemy, so he wasn't
  wired into any enemy class or `Data/DungeonType_SpriteWorld.json`'s own `EnemyNames`. A possible
  future use: a purely decorative, non-combat cameo somewhere in the dungeon (the wiki itself notes
  some room platforms reference him as pixel art), not an actual fight.

## Completed

Moved to a dedicated log so it isn't duplicated here — see
[DEVLOG.md](DEVLOG.md) for everything built so far, and
[BUGFIXES.md](BUGFIXES.md) for bugs found and fixed along the way. When an item
above is completed, remove it from Open ideas and append it to the completed-features log instead
of leaving it here.
