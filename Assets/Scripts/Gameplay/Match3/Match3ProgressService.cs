using PuzzleDungeon.Services;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    public sealed class Match3ProgressService
    {
        private readonly SaveService saveService;

        public Match3ProgressService(SaveService save)
        {
            saveService = save ?? new SaveService();
        }

        public int GetCurrentLevelIndex(int levelCount)
        {
            return Mathf.Clamp(saveService.GetCurrentLevelIndex(), 0, Mathf.Max(0, levelCount - 1));
        }

        public int GetUnlockedLevelIndex(int levelCount)
        {
            return Mathf.Clamp(saveService.GetUnlockedLevelIndex(), 0, Mathf.Max(0, levelCount - 1));
        }

        public PlayerProgressData LoadProgress(int levelCount)
        {
            int currentLevelIndex = GetCurrentLevelIndex(levelCount);
            int unlockedLevelIndex = GetUnlockedLevelIndex(levelCount);
            int playerXp = saveService.GetPlayerXp();
            int calculatedLevel = PlayerProgressData.CalculatePlayerLevel(playerXp);

            if (saveService.GetPlayerLevel() != calculatedLevel)
            {
                saveService.SetPlayerLevel(calculatedLevel);
            }

            return new PlayerProgressData(currentLevelIndex, unlockedLevelIndex, playerXp, calculatedLevel, saveService.GetTotalStars());
        }

        public bool CanPlayLevel(int levelIndex, int levelCount)
        {
            int clampedLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, levelCount - 1));
            return clampedLevelIndex <= GetUnlockedLevelIndex(levelCount);
        }

        public bool SelectLevel(int levelIndex, int levelCount)
        {
            if (!CanPlayLevel(levelIndex, levelCount))
            {
                return false;
            }

            SetCurrentLevelIndex(levelIndex, levelCount);
            return true;
        }

        public void SetCurrentLevelIndex(int levelIndex, int levelCount)
        {
            saveService.SetCurrentLevelIndex(Mathf.Clamp(levelIndex, 0, Mathf.Max(0, levelCount - 1)));
            saveService.Flush();
        }

        public void MarkLevelComplete(int completedLevelIndex, int levelCount, string levelId, int movesUsed)
        {
            int nextLevelIndex = Mathf.Clamp(completedLevelIndex + 1, 0, Mathf.Max(0, levelCount - 1));
            saveService.SetUnlockedLevelIndex(nextLevelIndex);
            saveService.SetCurrentLevelIndex(nextLevelIndex);
            saveService.SetBestMoveCount(levelId, movesUsed);
            int starDelta = saveService.SetLevelStars(levelId, 1);
            saveService.AddTotalStars(starDelta);
            saveService.Flush();
        }

        public LevelResult CompleteLevel(Match3LevelData level, int completedLevelIndex, int levelCount, int score, int movesRemaining, int movesUsed, int cascadeCount)
        {
            string levelId = level != null ? level.LevelId : $"match3_level_{completedLevelIndex + 1:00}";
            int previousStars = saveService.GetLevelStars(levelId);
            LevelResult result = LevelResult.Create(level, true, score, movesRemaining, movesUsed, cascadeCount, previousStars);
            int nextLevelIndex = Mathf.Clamp(completedLevelIndex + 1, 0, Mathf.Max(0, levelCount - 1));

            saveService.SetUnlockedLevelIndex(nextLevelIndex);
            saveService.SetCurrentLevelIndex(nextLevelIndex);
            saveService.SetBestMoveCount(levelId, movesUsed);
            int starDelta = saveService.SetLevelStars(levelId, result.Stars);
            saveService.AddTotalStars(starDelta);

            if (result.XpAwarded > 0)
            {
                int newXp = saveService.GetPlayerXp() + result.XpAwarded;
                saveService.SetPlayerXp(newXp);
                saveService.SetPlayerLevel(PlayerProgressData.CalculatePlayerLevel(newXp));
            }

            saveService.Flush();
            return result;
        }

        public LevelResult CreateLossResult(Match3LevelData level, int score, int movesRemaining, int movesUsed, int cascadeCount)
        {
            string levelId = level != null ? level.LevelId : "match3_level_01";
            return LevelResult.Create(level, false, score, movesRemaining, movesUsed, cascadeCount, saveService.GetLevelStars(levelId));
        }

        public LevelMapNodeState[] BuildLevelMap(Match3LevelCatalog catalog)
        {
            int levelCount = catalog != null ? catalog.LevelCount : 0;
            LevelMapNodeState[] nodes = new LevelMapNodeState[levelCount];
            int unlockedIndex = GetUnlockedLevelIndex(levelCount);
            int currentIndex = GetCurrentLevelIndex(levelCount);

            for (int i = 0; i < levelCount; i++)
            {
                Match3LevelData level = catalog.GetLevelByIndex(i);
                string levelId = level != null ? level.LevelId : $"match3_level_{i + 1:00}";
                int bestMoves = saveService.TryGetBestMoveCount(levelId, out int storedBestMoves) ? storedBestMoves : 0;
                nodes[i] = new LevelMapNodeState(i, level, i <= unlockedIndex, i == currentIndex, saveService.GetLevelStars(levelId), bestMoves);
            }

            return nodes;
        }

        public void ResetAll(Match3LevelCatalog catalog)
        {
            string[] levelIds = new string[catalog != null ? catalog.LevelCount : 0];

            for (int i = 0; i < levelIds.Length; i++)
            {
                Match3LevelData level = catalog.GetLevelByIndex(i);
                levelIds[i] = level != null ? level.LevelId : $"match3_level_{i + 1:00}";
            }

            saveService.ResetMatch3Progress(levelIds);
            saveService.Flush();
        }
    }
}
