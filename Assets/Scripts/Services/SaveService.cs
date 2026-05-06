using System.Collections.Generic;
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
        private const string StarPrefix = "PuzzleDungeon.Stars.";
        private const string PlayerXpKey = "PuzzleDungeon.PlayerXp";
        private const string PlayerLevelKey = "PuzzleDungeon.PlayerLevel";
        private const string TotalStarsKey = "PuzzleDungeon.TotalStars";

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

        public int GetHighestUnlockedLevelIndex()
        {
            return GetUnlockedLevelIndex();
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

        public int GetLevelStars(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return 0;
            }

            return Mathf.Clamp(PlayerPrefs.GetInt(BuildStarKey(levelId), 0), 0, 3);
        }

        public int SetLevelStars(string levelId, int stars)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return 0;
            }

            int existing = GetLevelStars(levelId);
            int sanitized = Mathf.Clamp(stars, 0, 3);
            int updated = Mathf.Max(existing, sanitized);
            PlayerPrefs.SetInt(BuildStarKey(levelId), updated);
            return updated - existing;
        }

        public int GetPlayerXp()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(PlayerXpKey, 0));
        }

        public void SetPlayerXp(int xp)
        {
            PlayerPrefs.SetInt(PlayerXpKey, Mathf.Max(0, xp));
        }

        public int GetPlayerLevel()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(PlayerLevelKey, 1));
        }

        public void SetPlayerLevel(int playerLevel)
        {
            PlayerPrefs.SetInt(PlayerLevelKey, Mathf.Max(1, playerLevel));
        }

        public int GetTotalStars()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(TotalStarsKey, 0));
        }

        public void AddTotalStars(int starDelta)
        {
            if (starDelta <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(TotalStarsKey, GetTotalStars() + starDelta);
        }

        public void ResetMatch3Progress(IEnumerable<string> levelIds)
        {
            PlayerPrefs.DeleteKey(CurrentLevelKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            PlayerPrefs.DeleteKey(PlayerXpKey);
            PlayerPrefs.DeleteKey(PlayerLevelKey);
            PlayerPrefs.DeleteKey(TotalStarsKey);

            if (levelIds != null)
            {
                foreach (string levelId in levelIds)
                {
                    if (string.IsNullOrWhiteSpace(levelId))
                    {
                        continue;
                    }

                    PlayerPrefs.DeleteKey(BuildBestMoveKey(levelId));
                    PlayerPrefs.DeleteKey(BuildStarKey(levelId));
                }
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

        private static string BuildStarKey(string levelId)
        {
            return $"{StarPrefix}{levelId.Trim()}";
        }
    }
}
