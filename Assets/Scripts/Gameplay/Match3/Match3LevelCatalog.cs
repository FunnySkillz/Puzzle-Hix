using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    [CreateAssetMenu(fileName = "Match3LevelCatalog", menuName = "PuzzleDungeon/Match 3/Level Catalog")]
    public class Match3LevelCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "Match3LevelCatalog";

        [SerializeField] private Match3LevelData[] levels = new Match3LevelData[0];

        public IReadOnlyList<Match3LevelData> Levels => levels ?? new Match3LevelData[0];
        public int LevelCount => Levels.Count;

        public Match3LevelData GetLevelByIndex(int index)
        {
            if (LevelCount == 0)
            {
                return null;
            }

            int clampedIndex = Mathf.Clamp(index, 0, LevelCount - 1);
            return levels[clampedIndex];
        }

        public static Match3LevelCatalog LoadDefault()
        {
            return Resources.Load<Match3LevelCatalog>(DefaultResourcePath);
        }

        public void ConfigureForTesting(Match3LevelData[] orderedLevels)
        {
            levels = orderedLevels ?? new Match3LevelData[0];
        }
    }
}
