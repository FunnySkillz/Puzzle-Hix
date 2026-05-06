namespace PuzzleDungeon.Gameplay.Match3
{
    public sealed class NoOpMatch3AnalyticsSink : IMatch3AnalyticsSink
    {
        public void LevelStarted(int levelNumber, ObjectiveType objectiveType, int moves)
        {
        }

        public void SwapResolved(int levelNumber, bool wasValid, int movesRemaining, int score)
        {
        }

        public void LevelEnded(int levelNumber, bool won, int movesRemaining, int score, float sessionSeconds)
        {
        }

        public void LevelRetried(int levelNumber)
        {
        }
    }
}
