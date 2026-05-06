using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    [CreateAssetMenu(fileName = "Match3LevelData", menuName = "PuzzleDungeon/Match 3/Level Data")]
    public class Match3LevelData : ScriptableObject
    {
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private int moves = 25;
        [SerializeField] private int targetScore = 1000;
        [SerializeField] private int availablePieceTypeCount = 6;
        [SerializeField] private ObjectiveType objectiveType = ObjectiveType.Score;
        [SerializeField] private ColorGoal[] colorGoals = new ColorGoal[0];
        [SerializeField] private int clearPiecesTarget;

        public int LevelNumber => Mathf.Max(1, levelNumber);
        public int Width => Mathf.Clamp(width, 3, 10);
        public int Height => Mathf.Clamp(height, 3, 10);
        public int Moves => Mathf.Max(1, moves);
        public int TargetScore => Mathf.Max(10, targetScore);
        public int AvailablePieceTypeCount => Mathf.Clamp(availablePieceTypeCount, 3, 6);
        public ObjectiveType ObjectiveType => objectiveType;
        public ColorGoal[] ColorGoals => colorGoals ?? new ColorGoal[0];
        public int ClearPiecesTarget => Mathf.Max(0, clearPiecesTarget);
        public string LevelId => $"match3_level_{LevelNumber:00}";

        public void ConfigureForTesting(
            int number,
            int boardWidth,
            int boardHeight,
            int moveCount,
            int scoreTarget,
            int pieceTypeCount,
            ObjectiveType objective,
            ColorGoal[] goals,
            int clearTarget)
        {
            levelNumber = number;
            width = boardWidth;
            height = boardHeight;
            moves = moveCount;
            targetScore = scoreTarget;
            availablePieceTypeCount = pieceTypeCount;
            objectiveType = objective;
            colorGoals = goals ?? new ColorGoal[0];
            clearPiecesTarget = clearTarget;
        }
    }
}
