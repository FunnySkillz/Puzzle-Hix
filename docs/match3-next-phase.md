# Match-3 Next Phase Notes

These are the follow-up items found while reviewing the playable match-3 MVP slice.

## Bugs And Risks

- The runtime board and UI are still tightly coupled in `BoardManager`. It is acceptable for the current MVP, but blockers, boosters, and more complex special-piece interactions will be easier to test if the board model is extracted into a pure gameplay service.
- The HUD and board positions are fixed for the prototype canvas. Before mobile/device work, add safe-area and aspect-ratio layout handling so the board cannot collide with status text or end panels.
- Level difficulty is authored only through board size, moves, target score, piece count, and objective counts. The 20 levels need manual playtest balancing before adding more content.
- Special-piece scoring currently favors simple readability over a full economy. Decide whether creating a special should count the retained piece toward collect/clear goals and bonus score.

## Upgrade Path

- Retire or isolate the old sliding-puzzle gameplay scripts/tests once match-3 is the permanent direction.
- Playtest the 20-level MVP ladder and rebalance the first five levels until a new player understands the goal without explanation.
- Add a level select screen for replaying unlocked levels and inspecting objectives before starting.
- Add blockers only after the current objective and special-piece loop feels stable.
- Replace the no-op analytics sink with an SDK-backed implementation when retention testing begins.
- Add future rewarded-ad placeholders to the fail flow, but keep real ads/IAP out until retention data supports them.
- Add audio hooks and lightweight sound effects for swap, invalid swap, clear, cascade, special creation, win, and fail.
- Replace legacy `Text` UI with TextMeshPro once visual polish begins.
- Add manual QA scenarios for dead-board reshuffle, last-move loss, long cascades, and small/large board sizes.
