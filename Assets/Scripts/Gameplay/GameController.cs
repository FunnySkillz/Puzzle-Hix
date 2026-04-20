using System;
using PuzzleDungeon.Core;
using PuzzleDungeon.Services;

namespace PuzzleDungeon.Gameplay
{
    /// <summary>
    /// Coordinates level lifecycle flow between level data, board state, rules evaluation, and save persistence.
    /// </summary>
    public class GameController
    {
        private readonly SaveService saveService;
        private readonly GameRules gameRules;
        private readonly BoardState boardState;

        private LevelData activeLevel;

        public GameController(SaveService saveService, GameRules gameRules, BoardState boardState)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.gameRules = gameRules ?? throw new ArgumentNullException(nameof(gameRules));
            this.boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
        }

        public LevelData ActiveLevel => activeLevel;
        public BoardState BoardState => boardState;

        public void Initialize(LevelData levelData)
        {
            activeLevel = levelData ?? throw new ArgumentNullException(nameof(levelData));
            saveService.SetCurrentLevelIndex(activeLevel.LevelIndex);
        }

        public void StartLevel()
        {
            gameRules.ResetMoveCounter();
            boardState.ResetProgress();
        }

        public void ResetLevel()
        {
            gameRules.ResetMoveCounter();
            boardState.ResetProgress();
        }

        public void SaveProgress()
        {
            if (activeLevel != null)
            {
                saveService.SetCurrentLevelIndex(activeLevel.LevelIndex);
            }

            saveService.Flush();
        }

        public bool IsWinConditionMet()
        {
            return gameRules.HasWon(boardState);
        }

        public bool IsLossConditionMet()
        {
            return gameRules.HasLost(boardState);
        }

        public void CompleteLevel()
        {
            if (activeLevel == null)
            {
                throw new InvalidOperationException("GameController must be initialized with a LevelData before completion.");
            }

            boardState.MarkCompleted();
            saveService.SetUnlockedLevelIndex(activeLevel.LevelIndex + 1);
            saveService.Flush();
        }
    }
}
