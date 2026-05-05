# PuzzleDungeon

PuzzleDungeon is a planned mobile hybrid-casual puzzle game for Android and iOS. The project is intended to start as a focused MVP with short play sessions, a clear core loop, and a monetization model built around rewarded ads and a one-time "Remove Ads" purchase.

This repository currently contains a playable Unity Editor prototype plus the project proposal used to define its broader scope, goals, and delivery approach.

## Current Status

- A Unity Editor prototype is playable from the `MainMenu` scene.
- The current Unity editor version is `2022.3.15f1`.
- `MainMenu` and `PuzzleBoard` are added to build settings.
- The prototype uses adjacent orthogonal sliding: select a tile, then select an adjacent empty cell.
- Five prototype levels are assigned to the board scene.
- A small Kenney CC0 art subset is imported for prototype board, tile, button, panel, and icon visuals.
- The Unity product name is `PuzzleDungeon`.

## Documentation

- Full proposal: [docs/project-proposal.md](docs/project-proposal.md)

## Notes

The current prototype is for Unity Editor playtesting only. Android/iOS builds, ads, IAP, analytics, audio, splash screen, and store polish are still outside this prototype pass.

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
3. Verify board renders cells/tiles and move counter text appears.
4. Do one valid move and one invalid move; confirm expected logs and no board corruption.
5. Complete level 1, go next level/menu, restart Play Mode, and verify progression resume behavior.

## Prototype Art Sources

The current prototype imports a small subset from these free Kenney CC0 packs:

- Puzzle Pack 1: https://kenney.nl/assets/puzzle-pack-1
- UI Pack: https://kenney.nl/assets/ui-pack
- Board Game Icons: https://kenney.nl/assets/board-game-icons

See `Assets/Art/Prototype/Kenney/LICENSES.md` for the local source note.
