namespace PuzzleDungeon.Gameplay.Match3
{
    public interface IMatch3AnalyticsSink
    {
        void LevelStarted(int levelNumber, ObjectiveType objectiveType, int moves);
        void SwapResolved(int levelNumber, bool wasValid, int movesRemaining, int score);
        void LevelEnded(int levelNumber, bool won, int movesRemaining, int score, float sessionSeconds);
        void LevelRetried(int levelNumber);
    }
}
