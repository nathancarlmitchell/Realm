# Realm — Project Notes for Claude Code

Realm is a top-down ARPG built in C#/MonoGame. This file holds working rules for developing in
this repo. See [docs/DEVLOG.md](docs/DEVLOG.md) for what's been built, [docs/BUGFIXES.md](docs/BUGFIXES.md)
for bugs found and fixed, and [docs/BACKLOG.md](docs/BACKLOG.md) for what's still open — check the
backlog before proposing "what's next" or starting new feature work. All three are living
documents; append new entries directly rather than keeping them only in local memory, so they
travel with the repo across machines.

## Critical: back up real save files before any scripted test

Before any scripted test that uses the real `Player.Instance` and calls a method that might
trigger persistence (`Util.SavePlayerData()`/`SaveInventoryData()`/`SaveBankData()`, or anything
that calls those internally — `InventorySystem.Update()`/`BankSystem.Update()`'s drag-release
handlers both save unconditionally whenever a drag was in progress; constructing a real
`RealmState`/`NexusState`/`BossRealmState` also saves in its constructor; `Player.LevelUp()` also
saves unconditionally the instant `Level` first reaches 20 in a given run — a scripted test that
loops `LevelUp()` up to a fresh Level 20 for the first time will silently write real
`PlayerData_{Class}.json`/`InventoryData_{Class}.json`/`BankData.json`/`FameData.json` files for
whatever class `Player.Instance` currently is; `Player.Hit()` calls `Kill()` the instant `Health`
drops to 0 or below, which calls `StateManager.GameOver()`, which resets the died class back to a
fresh Level 1 via `Util.ResetPlayer()` and then unconditionally saves — a scripted test that sets
`Health` low/negative directly, or calls `Hit()` with `Health` already at or near 0, can trigger a
real death save and silently wipe that class's real `ExperienceTotal`/`HighScore`/`HasBeenPlayed`
progress back to a fresh-character state), **back up the real save files first**:
`bin/Debug/net8.0-windows/PlayerData_*.json`, `InventoryData_*.json`, `BankData.json`,
`FameData.json`, `KeyBindingsData.json`. Copy them aside before the test runs, restore after, or
verify via `diff` that they're unmodified. Don't reason "it'll just re-save the same state it
loaded" as a substitute for an actual backup — that only holds if the test truly never mutates
anything before a persist call fires, and it's easy to get wrong.

**Why:** On 2026-08-18, a scripted test cleared the real inventory/bank arrays to isolate fake test
data, then called a real `Update()` method that saves unconditionally on drag-release — overwriting
the user's real `InventoryData_Wizard.json`/`BankData.json` with no backup taken first. The user's
real prior inventory/bank contents were not recoverable. See [docs/BUGFIXES.md](docs/BUGFIXES.md)
entry 42 for the full incident.

**How to apply:** Prefer constructing isolated test data that never touches the real
`Player.Instance` singleton or other static state (`BankSystem.Records`, `FameSystem.Fame`,
`KeyBindings`, `EnemySpawner`) at all, where possible — not always avoidable, since several of
these subsystems are static/shared. When a real save-file mutation is genuinely intended (e.g. the
user explicitly asks to edit real save data), back up first regardless, and verify the diff only
touched what was intended afterward.

## Testing workflow

The established pattern for verifying a change, used consistently across this project's history:

1. Add temporary verification code directly inside `Game1.StartGame()` (right after
   `EntityManager.Add(Player.Instance);`), writing results to a scratch log file
   (`.WriteLine`s to a `System.IO.StreamWriter`). Reflection (`BindingFlags.NonPublic`) is used
   freely to reach private/protected fields and methods for test-only assertions, without adding
   permanent public API surface just for testing.
2. `dotnet build` — confirm 0 errors before running anything.
3. Launch minimized (see below), let it run a few seconds, stop it, read the log file.
4. Fully revert the temporary code — confirm via `git diff --stat Game1.cs` that no diff remains
   beyond whatever real, intentional change was made this session.
5. Final clean `dotnet build`, then a plain boot-check (minimized, no temp code) confirming the
   real executable starts and stays running with no stderr output.
6. Delete any scratch log files created during testing.

Common gotchas: `Game1.Camera` must be initialized (construct a throwaway `NexusState` + set
`Camera.Pos = Vector2.Zero`) before calling `Player.Update()`/`EntityManager.Update()` in a test, or
it throws `NullReferenceException`. `Controls.Button.Update()` and `Input.Update()` both poll real
OS hardware state directly (`Mouse.GetState()`/`Keyboard.GetState()`), which overwrites whatever a
test preset into `Input.mouse`/`previousMouse` — so a `Button` click or a full `Input.Update()`
pass can't be simulated through those wrappers; test the underlying conditional/handler logic
directly instead against manually-set `Input.mouse`/`previousMouse`/reflected keyboard fields.

## Boot-checks: launch minimized

Launch `Realm.exe` minimized for any boot-check or scripted test run, instead of launching it
directly — the window shouldn't pop up on top of whatever else the user is doing.

```powershell
$proc = Start-Process -FilePath "<path>\Realm.exe" -PassThru -WindowStyle Minimized -WorkingDirectory "<path>"
Start-Sleep -Seconds 3   # let the window/content finish loading
# ... check tasklist / read any log output / etc ...
Stop-Process -Id $proc.Id -Force
```

Confirmed via `IsIconic()` (a `user32.dll` P/Invoke check) that MonoGame/WinForms apps actually
honor `-WindowStyle Minimized` — the window is truly minimized (off-screen, taskbar-only), not just
requested-and-ignored. Always pass `-WorkingDirectory` explicitly — without it, a relative-path
scratch log file (or a real save file the process reads/writes) can land in the shell's current
directory instead of next to the executable.

## GitHub

This repo is public: **https://github.com/nathancarlmitchell/Realm** (default branch `main`).
`gh` CLI is authenticated as `nathancarlmitchell`. `.gitignore` excludes `bin/`, `obj/`, `.vs/`, and
`.claude/settings.local.json` — `bin/Debug/net8.0-windows/` is where the real per-class save files
(`PlayerData_*.json` etc.) live, so they're already kept out of the repo.

## Building

```bash
dotnet build
```

The Content Pipeline (`Content/Content.mgcb`) builds as part of `dotnet build` — a new art/audio
asset needs a matching `#begin`/`#build` block added there (same importer/processor shape as the
adjacent existing entry for that asset type) before `Content.Load<T>()` can find it, or the game
crashes at boot trying to load it.
