# Match-3 Next Phase Notes

These are the follow-up items found while reviewing the showable match-3 MVP v2 slice.

## Bugs And Risks

- The runtime board and UI are still tightly coupled in `BoardManager`. It is acceptable for the current MVP, but blockers, boosters, and more complex special-piece interactions will be easier to test if the board model is extracted into a pure gameplay service.
- The HUD and board positions are fixed for the prototype canvas. Before mobile/device work, add safe-area and aspect-ratio layout handling so the board cannot collide with status text or end panels.
- Level difficulty is still authored only through board size, moves, target score, piece count, and objective counts. The 20 levels need manual playtest balancing before adding more content.
- Special-piece scoring currently favors simple readability over a full economy. Decide later whether retained special pieces should add a separate bonus score.
- Current audio is zero-cost procedural placeholder sound. Replace only with CC0/free assets or original generated sounds until the project explicitly changes budget policy.

## Upgrade Path

- Retire or isolate the old sliding-puzzle gameplay scripts/tests once match-3 is the permanent direction.
- Playtest the 20-level map and rebalance the first five levels until a new player understands the goal without explanation.
- Add objective preview details to each level-map node before starting a level.
- Add blockers only after the current objective and special-piece loop feels stable.
- Replace the no-op analytics sink with an SDK-backed implementation when retention testing begins.
- Add future rewarded-ad placeholders to the fail flow, but keep real ads/IAP out until retention data supports them.
- Replace procedural placeholder tones with polished CC0/free or original sounds when the interaction timing is stable.
- Replace legacy `Text` UI with TextMeshPro once visual polish begins.
- Add manual QA scenarios for dead-board reshuffle, last-move loss, long cascades, map replay, reset-progress debug flow, and small/large board sizes.
