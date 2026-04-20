using PuzzleDungeon.Core;
using PuzzleDungeon.Gameplay;
using PuzzleDungeon.Services;
using UnityEngine;

namespace PuzzleDungeon.Infrastructure
{
    /// <summary>
    /// Acts as the scene composition root that wires core MVP services and starts the initial level flow.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData initialLevel;

        private GameController gameController;

        private void Awake()
        {
            if (initialLevel == null)
            {
                Debug.LogWarning("GameBootstrap has no initial LevelData assigned.");
                return;
            }

            SaveService saveService = new SaveService();
            GameRules gameRules = new GameRules();
            BoardState boardState = new BoardState(initialLevel.BoardWidth, initialLevel.BoardHeight);

            gameController = new GameController(saveService, gameRules, boardState);
            gameController.Initialize(initialLevel);
            gameController.StartLevel();
        }
    }
}
