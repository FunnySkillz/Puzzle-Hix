using UnityEngine;

namespace PuzzleDungeon.Services
{
    /// <summary>
    /// Persists basic local MVP progress data such as current and unlocked level indices.
    /// </summary>
    public class SaveService
    {
        private const string CurrentLevelKey = "PuzzleDungeon.CurrentLevelIndex";
        private const string UnlockedLevelKey = "PuzzleDungeon.UnlockedLevelIndex";

        public int GetCurrentLevelIndex()
        {
            return PlayerPrefs.GetInt(CurrentLevelKey, 0);
        }

        public void SetCurrentLevelIndex(int levelIndex)
        {
            PlayerPrefs.SetInt(CurrentLevelKey, Mathf.Max(0, levelIndex));
        }

        public int GetUnlockedLevelIndex()
        {
            return PlayerPrefs.GetInt(UnlockedLevelKey, 0);
        }

        public void SetUnlockedLevelIndex(int levelIndex)
        {
            int sanitized = Mathf.Max(0, levelIndex);
            int existing = GetUnlockedLevelIndex();
            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(existing, sanitized));
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}
