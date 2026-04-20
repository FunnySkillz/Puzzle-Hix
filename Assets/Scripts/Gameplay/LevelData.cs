using UnityEngine;

namespace PuzzleDungeon.Gameplay
{
    /// <summary>
    /// Defines authorable level configuration data used to initialize an MVP board at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "PuzzleDungeon/Level Data")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private string levelId = "level_001";
        [SerializeField] private int levelIndex;
        [SerializeField] private int boardWidth = 6;
        [SerializeField] private int boardHeight = 6;
        [SerializeField] private int targetScore = 100;
        [SerializeField] private int moveLimit = 20;

        public string LevelId => levelId;
        public int LevelIndex => levelIndex;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public int TargetScore => targetScore;
        public int MoveLimit => moveLimit;
    }
}
