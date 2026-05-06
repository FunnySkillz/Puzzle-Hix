using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Tracks score, moves, objectives, and terminal state for one match-3 level attempt.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        private readonly Dictionary<PieceType, int> collectedCounts = new Dictionary<PieceType, int>();
        private readonly Dictionary<PieceType, int> colorGoals = new Dictionary<PieceType, int>();

        private UIManager uiManager;
        private Match3LevelData currentLevel;

        public int Score { get; private set; }
        public int MovesRemaining { get; private set; }
        public int MovesUsed { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentLevelNumber => currentLevel != null ? currentLevel.LevelNumber : 1;
        public ObjectiveType ObjectiveType => currentLevel != null ? currentLevel.ObjectiveType : ObjectiveType.Score;
        public int ClearedPieceCount { get; private set; }
        public int CascadeCount { get; private set; }
        public int ClearPiecesTarget => currentLevel != null ? currentLevel.ClearPiecesTarget : 0;
        public bool IsGameOver { get; private set; }
        public bool HasWon { get; private set; }

        public void Initialize(Match3LevelData level, BoardConfig fallbackConfig, UIManager ui)
        {
            uiManager = ui;
            currentLevel = level;
            Score = 0;
            MovesUsed = 0;
            MovesRemaining = level != null ? level.Moves : fallbackConfig.StartingMoves;
            TargetScore = level != null ? level.TargetScore : fallbackConfig.TargetScore;
            IsGameOver = false;
            HasWon = false;
            ClearedPieceCount = 0;
            CascadeCount = 0;
            RebuildColorGoals();
            RefreshHud();
            uiManager?.HideEndGame();
        }

        public void InitializeForTest(int moves, int targetScore, UIManager ui)
        {
            Match3LevelData testLevel = ScriptableObject.CreateInstance<Match3LevelData>();
            testLevel.ConfigureForTesting(1, 8, 8, moves, targetScore, 6, ObjectiveType.Score, null, 0);
            Initialize(testLevel, ScriptableObject.CreateInstance<BoardConfig>(), ui);
        }

        public void ConsumeMove()
        {
            if (IsGameOver)
            {
                return;
            }

            MovesRemaining = Mathf.Max(0, MovesRemaining - 1);
            MovesUsed++;
            RefreshHud();
        }

        public void RecordClearedPieces(IEnumerable<PieceType> clearedTypes, int scoreAmount)
        {
            if (IsGameOver)
            {
                return;
            }

            int clearedThisStep = 0;

            foreach (PieceType pieceType in clearedTypes)
            {
                clearedThisStep++;

                if (!collectedCounts.ContainsKey(pieceType))
                {
                    collectedCounts[pieceType] = 0;
                }

                collectedCounts[pieceType]++;
            }

            ClearedPieceCount += clearedThisStep;
            Score += Mathf.Max(0, scoreAmount);
            RefreshHud();
        }

        public void RecordCascade()
        {
            if (!IsGameOver)
            {
                CascadeCount++;
            }
        }

        public int GetCollectedCount(PieceType pieceType)
        {
            return collectedCounts.TryGetValue(pieceType, out int count) ? count : 0;
        }

        public int GetColorGoal(PieceType pieceType)
        {
            return colorGoals.TryGetValue(pieceType, out int goal) ? goal : 0;
        }

        public bool EvaluateEndState()
        {
            if (IsGameOver)
            {
                return true;
            }

            if (IsObjectiveComplete())
            {
                IsGameOver = true;
                HasWon = true;
                return true;
            }

            if (MovesRemaining <= 0)
            {
                IsGameOver = true;
                HasWon = false;
                return true;
            }

            return false;
        }

        public bool IsObjectiveComplete()
        {
            switch (ObjectiveType)
            {
                case ObjectiveType.CollectColor:
                    return AreColorGoalsComplete();
                case ObjectiveType.ClearPieces:
                    return ClearPiecesTarget > 0 && ClearedPieceCount >= ClearPiecesTarget;
                case ObjectiveType.ScoreAndCollect:
                    return Score >= TargetScore && AreColorGoalsComplete();
                default:
                    return Score >= TargetScore;
            }
        }

        public string BuildObjectiveText()
        {
            switch (ObjectiveType)
            {
                case ObjectiveType.CollectColor:
                    return $"Collect {BuildColorGoalText()}";
                case ObjectiveType.ClearPieces:
                    return $"Clear {Mathf.Min(ClearedPieceCount, ClearPiecesTarget)}/{ClearPiecesTarget} pieces";
                case ObjectiveType.ScoreAndCollect:
                    return $"Score {Score}/{TargetScore} + collect {BuildColorGoalText()}";
                default:
                    return $"Score {Score}/{TargetScore}";
            }
        }

        private void RebuildColorGoals()
        {
            collectedCounts.Clear();
            colorGoals.Clear();

            if (currentLevel == null)
            {
                return;
            }

            ColorGoal[] goals = currentLevel.ColorGoals;

            for (int i = 0; i < goals.Length; i++)
            {
                if (goals[i].TargetCount > 0)
                {
                    colorGoals[goals[i].PieceType] = goals[i].TargetCount;
                }
            }
        }

        private bool AreColorGoalsComplete()
        {
            if (colorGoals.Count == 0)
            {
                return false;
            }

            foreach (KeyValuePair<PieceType, int> goal in colorGoals)
            {
                if (GetCollectedCount(goal.Key) < goal.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private string BuildColorGoalText()
        {
            if (colorGoals.Count == 0)
            {
                return "0";
            }

            StringBuilder builder = new StringBuilder();
            int index = 0;

            foreach (KeyValuePair<PieceType, int> goal in colorGoals)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                int collected = Mathf.Min(GetCollectedCount(goal.Key), goal.Value);
                builder.Append(goal.Key);
                builder.Append(" ");
                builder.Append(collected);
                builder.Append("/");
                builder.Append(goal.Value);
                index++;
            }

            return builder.ToString();
        }

        private void RefreshHud()
        {
            uiManager?.UpdateHud(Score, MovesRemaining, TargetScore, CurrentLevelNumber, BuildObjectiveText());
        }
    }
}
