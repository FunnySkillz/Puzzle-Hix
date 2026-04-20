namespace PuzzleDungeon.Core
{
    /// <summary>
    /// Evaluates move validity, win state, and fail state for the pure puzzle core.
    /// </summary>
    public class GameRules
    {
        private readonly int moveLimit;
        private readonly Position goalPosition;
        private readonly string goalTileId;

        /// <summary>
        /// Creates a ruleset with a move limit and a goal condition based on a target tile at a target cell.
        /// </summary>
        public GameRules(int moveLimit, Position goalPosition, string goalTileId)
        {
            if (moveLimit < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(moveLimit), "Move limit cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(goalTileId))
            {
                throw new System.ArgumentException("Goal tile id must be a non-empty value.", nameof(goalTileId));
            }

            this.moveLimit = moveLimit;
            this.goalPosition = goalPosition;
            this.goalTileId = goalTileId;
            MovesUsed = 0;
        }

        /// <summary>
        /// Creates a default ruleset for bootstrap usage before a concrete puzzle goal is configured.
        /// </summary>
        public GameRules()
            : this(0, new Position(0, 0), "goal")
        {
        }

        /// <summary>
        /// Gets the number of successful moves used so far.
        /// </summary>
        public int MovesUsed { get; private set; }

        /// <summary>
        /// Resets the internal move counter for a new level attempt.
        /// </summary>
        public void ResetMoveCounter()
        {
            MovesUsed = 0;
        }

        /// <summary>
        /// Attempts to move a tile and returns a structured result describing success or failure.
        /// </summary>
        public MoveResult TryMove(BoardState boardState, Position from, Position to)
        {
            if (boardState == null)
            {
                return MoveResult.Failure(MovesUsed, false, "Board state is required.");
            }

            if (HasLost(boardState))
            {
                return MoveResult.Failure(MovesUsed, true, "Move limit reached.");
            }

            if (!boardState.IsInBounds(from) || !boardState.IsInBounds(to))
            {
                return MoveResult.Failure(MovesUsed, false, "Move is out of bounds.");
            }

            if (from == to)
            {
                return MoveResult.Failure(MovesUsed, false, "Source and destination cannot be the same cell.");
            }

            if (boardState.IsCellEmpty(from))
            {
                return MoveResult.Failure(MovesUsed, false, "Source cell is empty.");
            }

            if (!boardState.IsCellEmpty(to))
            {
                return MoveResult.Failure(MovesUsed, false, "Destination cell is already occupied.");
            }

            if (!boardState.MoveTile(from, to))
            {
                return MoveResult.Failure(MovesUsed, false, "Move could not be applied.");
            }

            // Count only successful moves so limit behavior stays deterministic in tests.
            MovesUsed++;

            bool isWin = HasWon(boardState);
            bool isFail = HasLost(boardState);

            if (isWin)
            {
                boardState.MarkCompleted();
            }

            return MoveResult.Success(MovesUsed, isWin, isFail);
        }

        /// <summary>
        /// Returns true when a move can still be attempted under current rule state.
        /// </summary>
        public bool CanApplyMove(BoardState boardState)
        {
            if (boardState == null)
            {
                return false;
            }

            return !HasWon(boardState) && !HasLost(boardState);
        }

        /// <summary>
        /// Returns true when the goal tile is located at the goal position or the board is marked completed.
        /// </summary>
        public bool HasWon(BoardState boardState)
        {
            if (boardState == null)
            {
                return false;
            }

            if (boardState.IsCompleted)
            {
                return true;
            }

            if (!boardState.IsInBounds(goalPosition))
            {
                return false;
            }

            Tile tileAtGoal = boardState.GetTile(goalPosition);
            return tileAtGoal != null && tileAtGoal.Id == goalTileId;
        }

        /// <summary>
        /// Returns true when the move limit is reached before the board has reached a win state.
        /// </summary>
        public bool HasLost(BoardState boardState)
        {
            if (boardState == null)
            {
                return false;
            }

            if (moveLimit <= 0)
            {
                return false;
            }

            if (HasWon(boardState))
            {
                return false;
            }

            return MovesUsed >= moveLimit;
        }
    }
}
