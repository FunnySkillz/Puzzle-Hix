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
            saveService.Flush();
        }
    }
}
