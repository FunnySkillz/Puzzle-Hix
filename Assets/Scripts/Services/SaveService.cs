using UnityEngine;

namespace PuzzleDungeon.Services
{
    /// <summary>
    /// Persists basic local MVP progress data such as level progression and best moves.
    /// </summary>
    public class SaveService
    {
        private const string CurrentLevelKey = "PuzzleDungeon.CurrentLevelIndex";
        private const string UnlockedLevelKey = "PuzzleDungeon.UnlockedLevelIndex";
        private const string BestMovePrefix = "PuzzleDungeon.BestMove.";

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

        public bool TryGetBestMoveCount(string levelId, out int bestMoves)
        {
            bestMoves = 0;

            if (string.IsNullOrWhiteSpace(levelId))
            {
                return false;
            }

            string key = BuildBestMoveKey(levelId);

            if (!PlayerPrefs.HasKey(key))
            {
                return false;
            }

            int storedMoves = PlayerPrefs.GetInt(key, 0);

            if (storedMoves <= 0)
            {
                return false;
            }

            bestMoves = storedMoves;
            return true;
        }

        public void SetBestMoveCount(string levelId, int movesUsed)
        {
            if (string.IsNullOrWhiteSpace(levelId) || movesUsed <= 0)
            {
                return;
            }

            string key = BuildBestMoveKey(levelId);

            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, movesUsed);
                return;
            }

            int existingBest = PlayerPrefs.GetInt(key, movesUsed);

            if (movesUsed < existingBest)
            {
                PlayerPrefs.SetInt(key, movesUsed);
            }
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }

        private static string BuildBestMoveKey(string levelId)
        {
            return $"{BestMovePrefix}{levelId.Trim()}";
        }
    }
}
