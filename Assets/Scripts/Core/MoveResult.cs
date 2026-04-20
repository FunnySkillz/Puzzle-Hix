namespace PuzzleDungeon.Core
{
    /// <summary>
    /// Represents the outcome of a move attempt, including rule-state flags and failure details.
    /// </summary>
    public sealed class MoveResult
    {
        private MoveResult(bool isSuccess, bool isWin, bool isFail, int movesUsed, string message)
        {
            IsSuccess = isSuccess;
            IsWin = isWin;
            IsFail = isFail;
            MovesUsed = movesUsed;
            Message = message;
        }

        /// <summary>
        /// Gets a value indicating whether the move was executed.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether the board reached a winning state after the move.
        /// </summary>
        public bool IsWin { get; }

        /// <summary>
        /// Gets a value indicating whether the board reached a failing state.
        /// </summary>
        public bool IsFail { get; }

        /// <summary>
        /// Gets the number of successful moves used so far.
        /// </summary>
        public int MovesUsed { get; }

        /// <summary>
        /// Gets a human-readable status message for debugging and tests.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Creates a successful move result.
        /// </summary>
        public static MoveResult Success(int movesUsed, bool isWin, bool isFail)
        {
            return new MoveResult(true, isWin, isFail, movesUsed, "Move applied.");
        }

        /// <summary>
        /// Creates a failed move result.
        /// </summary>
        public static MoveResult Failure(int movesUsed, bool isFail, string message)
        {
            return new MoveResult(false, false, isFail, movesUsed, message);
        }
    }
}
