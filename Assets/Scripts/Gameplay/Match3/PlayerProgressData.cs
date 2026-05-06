using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    public sealed class PlayerProgressData
    {
        public const int XpPerLevel = 250;

        public PlayerProgressData(int currentLevelIndex, int highestUnlockedLevelIndex, int playerXp, int playerLevel, int totalStars)
        {
            CurrentLevelIndex = Mathf.Max(0, currentLevelIndex);
            HighestUnlockedLevelIndex = Mathf.Max(0, highestUnlockedLevelIndex);
            PlayerXp = Mathf.Max(0, playerXp);
            PlayerLevel = Mathf.Max(1, playerLevel);
            TotalStars = Mathf.Max(0, totalStars);
        }

        public int CurrentLevelIndex { get; }
        public int HighestUnlockedLevelIndex { get; }
        public int PlayerXp { get; }
        public int PlayerLevel { get; }
        public int TotalStars { get; }
        public int XpIntoCurrentLevel => PlayerXp % XpPerLevel;
        public int XpForNextLevel => XpPerLevel;

        public static int CalculatePlayerLevel(int playerXp)
        {
            return Mathf.Max(1, (Mathf.Max(0, playerXp) / XpPerLevel) + 1);
        }
    }
}
