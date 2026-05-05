# PuzzleDungeon

PuzzleDungeon is a planned mobile hybrid-casual puzzle game for Android and iOS. The project is intended to start as a focused MVP with short play sessions, a clear core loop, and a monetization model built around rewarded ads and a one-time "Remove Ads" purchase.

This repository currently contains a playable Unity Editor match-3 prototype plus the project proposal used to define its broader scope, goals, and delivery approach.

## Current Status

- A Unity Editor prototype is playable from the `MainMenu` scene.
- The current Unity editor version is `2022.3.15f1`.
- `MainMenu` and `PuzzleBoard` are added to build settings.
- The active prototype is an original 8x8 match-3 board loaded from `PuzzleBoard`.
- The player swaps adjacent pieces to create horizontal or vertical matches of 3 or more.
- Valid swaps consume one move, score points, clear matched pieces, apply gravity, spawn replacements, and resolve cascades.
- Invalid swaps snap back and do not consume a move.
- The first objective is score-based: reach the target score before the move counter reaches 0.
- A small Kenney CC0 art subset is imported for prototype board, tile, button, panel, and icon visuals.
- The match-3 pieces also work with simple generated colors if prototype art is missing.
- The Unity product name is `PuzzleDungeon`.

## How To Play

1. Open `Assets/Scenes/MainMenu.unity` and press Play in the Unity Editor.
2. Click `Start Game` to load `PuzzleBoard`.
3. Click a piece, then click an adjacent piece above, below, left, or right to attempt a swap.
4. You can also drag a piece in a cardinal direction to swap with that neighbor.
5. A swap is accepted only if it creates a straight match of 3 or more same-type pieces.
6. Reach the target score before you run out of moves.

## Tuning The Prototype

The active level settings live in `Assets/Resources/Match3BoardConfig.asset`.

- `width` / `height`: board dimensions.
- `cellSize` / `cellSpacing`: UI board scale.
- `availablePieceTypeCount`: how many of the six piece colors are used.
- `startingMoves`: moves available at the start of a round.
- `targetScore`: score needed to win.
- Timing fields: swap, clear, fall, and cascade pacing.

## Documentation

- Full proposal: [docs/project-proposal.md](docs/project-proposal.md)

## Notes

The current prototype is for Unity Editor playtesting only. Android/iOS builds, ads, IAP, analytics, audio, splash screen, special pieces, blockers, and store polish are still outside this prototype pass.

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
3. Verify the 8x8 board renders colored pieces and score/move/target text.
4. Do one valid adjacent swap and one invalid adjacent swap; confirm only the valid swap consumes a move.
5. Confirm matches clear, pieces fall, replacements spawn, and cascades finish before the next input.
6. Reach the target score to see `Level Complete`, then restart.
7. Run out of moves below the target score to see `Game Over`, then return to menu.

## Prototype Art Sources

The current prototype imports a small subset from these free Kenney CC0 packs:

- Puzzle Pack 1: https://kenney.nl/assets/puzzle-pack-1
- UI Pack: https://kenney.nl/assets/ui-pack
- Board Game Icons: https://kenney.nl/assets/board-game-icons

See `Assets/Art/Prototype/Kenney/LICENSES.md` for the local source note.
