using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Gameplay
{
    /// <summary>
    /// Defines authorable level configuration data used to initialize an MVP board at runtime.
    /// To create a new level asset in Unity:
    /// 1) In the Project window, right-click and choose Create > PuzzleDungeon > Level Data.
    /// 2) Set board width, board height, and move limit.
    /// 3) Add entries to Initial Tile Layout using tile id plus x/y coordinates.
    /// 4) Assign the created asset to the level reference used by your bootstrap object.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "PuzzleDungeon/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Serializable]
        public struct TilePlacement
        {
            [SerializeField] private string tileId;
            [SerializeField] private int x;
            [SerializeField] private int y;

            public string TileId => tileId;
            public int X => x;
            public int Y => y;
        }

        [SerializeField] private string levelId = "level_001";
        [SerializeField] private int levelIndex;
        [Header("Board")]
        [SerializeField] private int boardWidth = 6;
        [SerializeField] private int boardHeight = 6;
        [SerializeField] private int moveLimit = 20;
        [Header("Goal")]
        [SerializeField] private string goalTileId = "goal";
        [SerializeField] private int goalX;
        [SerializeField] private int goalY;
        [Header("Initial Tile Layout")]
        [SerializeField] private TilePlacement[] initialTileLayout = Array.Empty<TilePlacement>();

        public string LevelId => levelId;
        public int LevelIndex => levelIndex;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public int MoveLimit => moveLimit;
        public string GoalTileId => goalTileId;
        public int GoalX => goalX;
        public int GoalY => goalY;
        public IReadOnlyList<TilePlacement> InitialTileLayout => initialTileLayout;
    }
}
