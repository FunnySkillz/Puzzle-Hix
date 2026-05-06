namespace PuzzleDungeon.Gameplay.Match3
{
    public sealed class LevelMapNodeState
    {
        public LevelMapNodeState(int levelIndex, Match3LevelData level, bool isUnlocked, bool isCurrent, int stars, int bestMoves)
        {
            LevelIndex = levelIndex;
            LevelNumber = level != null ? level.LevelNumber : levelIndex + 1;
            ObjectiveType = level != null ? level.ObjectiveType : ObjectiveType.Score;
            IsUnlocked = isUnlocked;
            IsCurrent = isCurrent;
            Stars = stars;
            BestMoves = bestMoves;
        }

        public int LevelIndex { get; }
        public int LevelNumber { get; }
        public ObjectiveType ObjectiveType { get; }
        public bool IsUnlocked { get; }
        public bool IsCurrent { get; }
        public bool IsCompleted => Stars > 0;
        public int Stars { get; }
        public int BestMoves { get; }
    }
}
