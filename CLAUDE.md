# CLAUDE.md — Calradic Exchange

Guidance for Claude Code working in this repository. Read this file fully at the start of every session.

## What this project is

**Calradic Exchange** (working title) is a Mount & Blade II: Bannerlord campaign mod that adds a financial exchange to towns. Players buy and sell paper instruments — commodity shares, sector indices, guild shares, kingdom indices, bonds, caravan ventures, forwards and insurance — instead of physical goods. Prices are driven by the live campaign world: raids, sieges, wars, seasons, prosperity, workshop profits.

- Design: `docs/GDD.md` (Turkish)
- Pricing math and default parameters: `docs/ECONOMY_MODEL.md` (Turkish)
- Roadmap with phases, tasks and acceptance criteria: `docs/PLAN.md` (Turkish) — **this is the source of truth for what to work on next**
- Bannerlord API assumptions that MUST be verified by decompiling: `docs/API_NOTES.md`
- Architecture decisions and their reasons: `docs/DECISIONS.md`

## Working with the user

- The user is Çağhan, an experienced Unity/C# and Godot developer who is **new to Bannerlord modding**. Explain Bannerlord-specific concepts briefly when they first come up; do not explain general C#.
- **Talk to the user in Turkish.** Code, identifiers, comments, commit messages and log messages are in English.
- The user does not want to hand-write or copy-paste files. You create, edit, build and deploy everything yourself.
- You cannot play the game. The loop is: you build and deploy → the user plays and reports → you read logs and fix. When asking the user to test, give a short, concrete checklist ("enter Pravend, open the exchange, buy 10 Grain shares, save, reload, check the portfolio").
- When something needs a design decision not covered by the docs, ask instead of inventing. Record the answer in `docs/DECISIONS.md`.
- After finishing a task, tick it in `docs/PLAN.md` and add a one-line note if something deviated from the plan.

## Environment (fill in on first session)

Confirmed with the user on 2026-10-18:

- Bannerlord install path: `G:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord` (Steam)
- Game version (shown in the launcher): `v1.4.8` (from `bin\Win64_Shipping_Client\Version.xml`)
- DLCs enabled: none (no War Sails / NavalDLC module installed — the mod must not hard-depend on any DLC)
- Repo path: `C:\Users\cagha\Desktop\CalradicExchange\CalradicExchange`
- Toolchain: .NET SDK 10.0.10x, git 2.55. Game ships `Newtonsoft.Json.dll` in `bin\Win64_Shipping_Client`.
- Installed (disabled) workshop mods include Harmony, ButterLib, UIExtenderEx, MCM (`Bannerlord.MBOptionScreen`) — useful for Phase 2 MCM testing.

Logs and crash data:
- Game logs: `C:\ProgramData\Mount and Blade II Bannerlord\logs\` (`rgl_log_*.txt`)
- Crash reports: `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`
- Mod's own log: `<game>\Modules\CalradicExchange\Logs\exchange.log` (we write this ourselves, see Logging)

## Architecture rules

```
src/
  CalradicExchange.Core/        netstandard2.0, NO TaleWorlds references
  CalradicExchange.Core.Tests/  xUnit, runs without the game
  CalradicExchange.Sim/         console app: runs the Core against synthetic event streams, writes CSV
  CalradicExchange/             net472 game module: adapters, behaviors, menus, UI
ModuleData/                     instruments.json, settings defaults, localization XML
GUI/                            Gauntlet prefabs (Phase 2+)
```

1. **All economics live in `Core`.** Pricing, order execution, fees, portfolio, dividends, bonds, events-to-shock mapping. `Core` must compile and test without any game DLL.
2. **The game module is a thin adapter.** It reads game state into plain `WorldSnapshot` DTOs, translates `CampaignEvents` into `MarketEvent` DTOs, and calls `Core`. No pricing math in the game module.
3. **Data-driven instruments.** Instruments, sector weights, event magnitudes and default parameters live in `ModuleData/instruments.json` and `ModuleData/settings.json`, not in code. Item and workshop IDs are referenced by their game `StringId`.
4. **Determinism inside Core.** Core takes an injected RNG (`IRandom`) and a clock (game day number). Same seed + same events = same prices. This is what makes unit tests and the simulator meaningful. (The game layer reseeds on load, see DECISIONS ADR-004.)
5. **No Harmony unless unavoidable.** Prefer `CampaignEvents` and public APIs. If a Harmony patch becomes necessary, write down why in `docs/DECISIONS.md` first.
6. **Never hard-crash the campaign.** Every event handler and daily tick is wrapped in try/catch that logs and continues. A broken exchange must not break someone's save.

## Save-game rules (important)

- Persist all mod state as **one JSON string** via `SyncData` in a single `CampaignBehaviorBase` (key `"cx_state_v1"`). Do not register custom types with `SaveableTypeDefiner`. Reason: removing the mod or changing a class must not corrupt the user's save (ADR-002).
- The JSON root has a `schemaVersion` field. Any change to persisted shape bumps it and adds a migration in `Core/Persistence/Migrations`.
- Test every persistence change with a round-trip unit test (serialize → deserialize → equal).

## Verify, don't guess

Bannerlord's API is poorly documented and changes between versions. **Before using any game API, verify it against the decompiled DLL** of the user's installed version.

- Tool: `ilspycmd` (`dotnet tool install -g ilspycmd`).
- Core campaign types: `<game>\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll`. SandBox types: `<game>\Modules\SandBox\bin\Win64_Shipping_Client\`.
- Decompile into `.decompiled/` (git-ignored) and grep there; decompiled types are huge, don't paste them whole.
- `docs/API_NOTES.md` lists every API this design assumes, with a status column. When you verify one, update its status and correct the signature. Never ship code against an API still marked `UNVERIFIED`.
- Item IDs, item category IDs and workshop type IDs must be checked against the game's XML in `<game>\Modules\SandBoxCore\ModuleData\` and `<game>\Modules\Native\ModuleData\`.

## Build, deploy, test

- Core: `dotnet build src/CalradicExchange.Core` and `dotnet test src/CalradicExchange.Core.Tests`. Run tests before every deploy.
- Simulator: `dotnet run --project src/CalradicExchange.Sim -- --days 336 --seed 42 --out sim/out.csv`. Use it to sanity-check any pricing change (no runaway prices, no prices at zero, reasonable volatility). Summarize the result for the user in numbers.
- Game module: build `src/CalradicExchange` and copy `SubModule.xml`, `bin/Win64_Shipping_Client/*.dll`, `ModuleData/` and `GUI/` to `<game>\Modules\CalradicExchange\`. Automate this as a post-build step or a `deploy.ps1`.
- Game DLL references must be `Private=false` (not copied).
- The game must be closed when deploying (DLLs are locked while running). If the copy fails with a sharing violation, tell the user to close the game.

## Debug tooling (build early, Phase 0–1)

Console commands (`[CommandLineFunctionality.CommandLineArgumentFunction("...", "exchange")]`), available via the in-game developer console (`Ctrl + ~` or `Alt + ~` depending on version/keyboard layout; check and note which one works for the user — Turkish Q keyboards may need a different key):

- `exchange.dump_prices` — prints all instrument prices and fundamentals
- `exchange.dump_portfolio`
- `exchange.fire_event <type> <targetId>` — injects a synthetic MarketEvent
- `exchange.set_price <instrumentId> <value>`
- `exchange.export_csv` — writes price history to the mod's Logs folder

## Logging

- Write our own log file at `Modules\CalradicExchange\Logs\exchange.log` (rotate, keep last 3). Log: startup, version, every event received, daily tick summary (count of instruments, min/max move), every trade, every exception with stack trace.
- Player-facing messages go through `InformationManager.DisplayMessage` and must be localized (`TextObject` with `{=cx_xxx}` IDs).

## Code style

- C# 9 language features max for the net472 project unless the template's `LangVersion` says otherwise; check before using newer syntax.
- `CalradicExchange.*` namespaces. One public type per file.
- Money is `int` denars in the game layer; Core uses `decimal` for prices and rounds to int only at execution/settlement.
- Prefer small, pure functions in Core; keep game-state access in clearly named adapter classes (`TownReader`, `VillageReader`, `EventBridge`).

## Git

- Small commits with clear English messages. Commit after each completed PLAN task.
- Never commit `.decompiled/`, game DLLs, or build output.

## Definition of done (per task)

1. Builds with no warnings we introduced.
2. Core tests pass; new logic has tests.
3. Simulator run still looks sane if pricing changed.
4. Deployed to the game folder.
5. The user has a concrete test checklist, and `docs/PLAN.md` is updated.
