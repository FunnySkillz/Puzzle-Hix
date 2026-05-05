# Match-3 Next Phase Notes

These are the follow-up items found while reviewing the first playable match-3 prototype.

## Bugs And Risks

- `BoardManager.SwapCreatesMatch` currently assumes the board is already stable and checks for matches anywhere after a trial swap. That is fine for the current loop, because input is blocked during cascades, but the next phase should either assert board stability before input or limit validity to matches caused by the swapped pieces.
- The runtime board and UI are tightly coupled in `BoardManager`. This works for the prototype, but it will make special pieces, blockers, and level objectives harder to test unless the board model is extracted into a pure gameplay service.
- The HUD and board positions are fixed for the prototype canvas. Before mobile/device work, add safe-area and aspect-ratio layout handling so the board cannot collide with status text or end panels.

## Upgrade Path

- Add explicit level data for match-3 settings and connect progression to `SaveService`.
- Retire or isolate the old sliding-puzzle gameplay scripts/tests once match-3 is the permanent direction.
- Add objective types beyond target score, starting with collect-by-color and clear-count goals.
- Add special piece creation for match-4, match-5, T-shape, and L-shape matches.
- Replace legacy `Text` UI with TextMeshPro once visual polish begins.
- Add manual QA scenarios for dead-board reshuffle, last-move loss, long cascades, and small/large board sizes.
