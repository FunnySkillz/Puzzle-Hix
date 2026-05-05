using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Tracks score, moves, target score, and terminal state for the match-3 prototype.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        private UIManager uiManager;

        public int Score { get; private set; }
        public int MovesRemaining { get; private set; }
        public int TargetScore { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool HasWon { get; private set; }

        public void Initialize(BoardConfig config, UIManager ui)
        {
            uiManager = ui;
            Score = 0;
            MovesRemaining = config.StartingMoves;
            TargetScore = config.TargetScore;
            IsGameOver = false;
            HasWon = false;
            uiManager.UpdateHud(Score, MovesRemaining, TargetScore);
            uiManager.HideEndGame();
        }

        public void InitializeForTest(int moves, int targetScore, UIManager ui)
        {
            uiManager = ui;
            Score = 0;
            MovesRemaining = Mathf.Max(1, moves);
            TargetScore = Mathf.Max(10, targetScore);
            IsGameOver = false;
            HasWon = false;
            uiManager.UpdateHud(Score, MovesRemaining, TargetScore);
            uiManager.HideEndGame();
        }

        public void ConsumeMove()
        {
            if (IsGameOver)
            {
                return;
            }

            MovesRemaining = Mathf.Max(0, MovesRemaining - 1);
            uiManager.UpdateHud(Score, MovesRemaining, TargetScore);
        }

        public void AddScore(int amount)
        {
            if (IsGameOver)
            {
                return;
            }

            Score += Mathf.Max(0, amount);
            uiManager.UpdateHud(Score, MovesRemaining, TargetScore);
        }

        public void EvaluateEndState()
        {
            if (IsGameOver)
            {
                return;
            }

            if (Score >= TargetScore)
            {
                IsGameOver = true;
                HasWon = true;
                uiManager.ShowEndGame(true);
                return;
            }

            if (MovesRemaining <= 0)
            {
                IsGameOver = true;
                HasWon = false;
                uiManager.ShowEndGame(false);
            }
        }
    }
}
