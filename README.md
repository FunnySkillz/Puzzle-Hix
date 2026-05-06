# PuzzleDungeon

PuzzleDungeon is a planned mobile hybrid-casual puzzle game for Android and iOS. The project is intended to start as a focused MVP with short play sessions, a clear core loop, and a monetization model built around rewarded ads and a one-time "Remove Ads" purchase.

This repository currently contains a playable Unity Editor match-3 MVP slice plus the project proposal used to define its broader scope, goals, and delivery approach.

## Current Status

- A Unity Editor prototype is playable from the `MainMenu` scene.
- The current Unity editor version is `2022.3.15f1`.
- `MainMenu` and `PuzzleBoard` are added to build settings.
- The active prototype is an original match-3 board loaded from `PuzzleBoard`.
- The MVP ladder has 20 authored level assets connected through `Match3LevelCatalog`.
- The player swaps adjacent pieces to create horizontal or vertical matches of 3 or more.
- Valid swaps consume one move, score points, clear matched pieces, apply gravity, spawn replacements, and resolve cascades.
- Invalid swaps snap back and do not consume a move.
- Objectives now include score targets, collect-by-color goals, clear-count goals, and mixed score plus collect goals.
- Match-4, match-5, and T/L shape matches create simple readable special pieces.
- Local progress resumes through `SaveService`, including current level, highest unlocked level, and best move count per completed level.
- Lightweight analytics event hooks exist behind `IMatch3AnalyticsSink`; the default implementation is no-op and no SDK is integrated yet.
- A small Kenney CC0 art subset is imported for prototype board, tile, button, panel, and icon visuals.
- The match-3 pieces also work with simple generated colors if prototype art is missing.
- The Unity product name is `PuzzleDungeon`.

## How To Play

1. Open `Assets/Scenes/MainMenu.unity` and press Play in the Unity Editor.
2. Click `Start Game` to load `PuzzleBoard`.
3. Click a piece, then click an adjacent piece above, below, left, or right to attempt a swap.
4. You can also drag a piece in a cardinal direction to swap with that neighbor.
5. A swap is accepted only if it creates a straight match of 3 or more same-type pieces.
6. Complete the objective before you run out of moves.
7. After winning, use `Next`; after failing, use `Retry`, or return to `Menu`.

## Tuning The Prototype

The level ladder lives in `Assets/Resources/Match3LevelCatalog.asset`, which references 20 level assets under `Assets/Resources/Match3Levels/`.

- Edit each `Match3LevelData` asset to tune `width`, `height`, `moves`, `targetScore`, `availablePieceTypeCount`, `objectiveType`, color goals, and clear-piece goals.
- Edit the catalog order to change level progression.
- Use `Assets/Resources/Match3BoardConfig.asset` for shared presentation and feel settings such as cell size, spacing, drag threshold, swap timing, clear timing, fall timing, cascade delay, hint delay, and piece colors.

## Documentation

- Full proposal: [docs/project-proposal.md](docs/project-proposal.md)
- Next-phase notes: [docs/match3-next-phase.md](docs/match3-next-phase.md)

## Notes

The current MVP is for Unity Editor playtesting only. Android/iOS builds, ads, IAP, SDK analytics, audio, splash screen, blockers, economy, boosters, and store polish are still outside this pass.

## Validation Checklist (Local)

Use this checklist before calling the current state prototype-ready.

### 1) Compile sanity

- Open Unity and allow a full refresh/recompile.
- Confirm Console has no `CS` compile errors.

Optional CLI compile check (Windows):

```powershell
"D:\Unity\Editor\2022.3.15f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\unity_projects\PuzzleDungeon\New Unity Project" -quit -logFile "Temp\compile.log"
```

### 2) Automated tests

Run EditMode tests:

```powershell
"D:\Unity\Editor\2022.3.15f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\unity_projects\PuzzleDungeon\New Unity Project" -runTests -testPlatform EditMode -testResults "TestResults\EditMode.xml" -logFile "TestResults\EditMode.log"
```

Run PlayMode smoke tests:

```powershell
"D:\Unity\Editor\2022.3.15f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\unity_projects\PuzzleDungeon\New Unity Project" -runTests -testPlatform PlayMode -testResults "TestResults\PlayMode.xml" -logFile "TestResults\PlayMode.log"
```

### 3) Manual smoke flow

1. Start from `MainMenu` scene and press Play.
2. Click `Start Game`, verify transition to `PuzzleBoard`.
3. Verify the board renders colored pieces plus level, score, moves, target, objective, and status text.
4. Do one valid adjacent swap and one invalid adjacent swap; confirm only the valid swap consumes a move.
5. Confirm matches clear, pieces fall, replacements spawn, and cascades finish before the next input.
6. Create a match-4, match-5, or T/L match and confirm a readable special piece appears.
7. Complete level 1, press `Next`, then return to menu and re-enter to confirm saved progression resumes.
8. Run out of moves before completing an objective to see `Game Over`, then retry.

## Prototype Art Sources

The current prototype imports a small subset from these free Kenney CC0 packs:

- Puzzle Pack 1: https://kenney.nl/assets/puzzle-pack-1
- UI Pack: https://kenney.nl/assets/ui-pack
- Board Game Icons: https://kenney.nl/assets/board-game-icons

See `Assets/Art/Prototype/Kenney/LICENSES.md` for the local source note.
