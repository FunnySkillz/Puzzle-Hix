using NUnit.Framework;
using PuzzleDungeon.Services;
using UnityEngine;

namespace PuzzleDungeon.Tests.EditMode
{
    /// <summary>
    /// Verifies local progression persistence behavior for SaveService.
    /// </summary>
    public class SaveServiceTests
    {
        private const string CurrentLevelKey = "PuzzleDungeon.CurrentLevelIndex";
        private const string UnlockedLevelKey = "PuzzleDungeon.UnlockedLevelIndex";
        private const string BestMovePrefix = "PuzzleDungeon.BestMove.";

        private readonly string[] bestMoveLevelIds =
        {
            "level_test",
            "level_best_move",
            "level_round_trip",
            "level_invalid"
        };

        [SetUp]
        public void SetUp()
        {
            ClearSaveData();
        }

        [TearDown]
        public void TearDown()
        {
            ClearSaveData();
        }

        [Test]
        public void SetUnlockedLevelIndex_DoesNotDecreaseWhenLowerValueIsSaved()
        {
            SaveService saveService = new SaveService();

            saveService.SetUnlockedLevelIndex(3);
            saveService.SetUnlockedLevelIndex(1);

            Assert.That(saveService.GetUnlockedLevelIndex(), Is.EqualTo(3));
        }

        [Test]
        public void SetBestMoveCount_OnlyUpdatesWhenNewValueIsLower()
        {
            const string levelId = "level_best_move";
            SaveService saveService = new SaveService();

            saveService.SetBestMoveCount(levelId, 12);
            bool hasBestAfterFirstSave = saveService.TryGetBestMoveCount(levelId, out int firstBest);

            saveService.SetBestMoveCount(levelId, 15);
            bool hasBestAfterHigherSave = saveService.TryGetBestMoveCount(levelId, out int secondBest);

            saveService.SetBestMoveCount(levelId, 9);
            bool hasBestAfterLowerSave = saveService.TryGetBestMoveCount(levelId, out int finalBest);

            Assert.That(hasBestAfterFirstSave, Is.True);
            Assert.That(firstBest, Is.EqualTo(12));
            Assert.That(hasBestAfterHigherSave, Is.True);
            Assert.That(secondBest, Is.EqualTo(12));
            Assert.That(hasBestAfterLowerSave, Is.True);
            Assert.That(finalBest, Is.EqualTo(9));
        }

        [Test]
        public void TryGetBestMoveCount_ReturnsFalse_WhenEntryDoesNotExist()
        {
            SaveService saveService = new SaveService();

            bool hasBestMoves = saveService.TryGetBestMoveCount("level_missing", out int bestMoves);

            Assert.That(hasBestMoves, Is.False);
            Assert.That(bestMoves, Is.EqualTo(0));
        }

        [Test]
        public void SetBestMoveCount_IgnoresInvalidInput()
        {
            SaveService saveService = new SaveService();

            saveService.SetBestMoveCount("level_invalid", 0);
            saveService.SetBestMoveCount("level_invalid", -1);
            saveService.SetBestMoveCount(string.Empty, 4);
            saveService.SetBestMoveCount("   ", 4);

            bool hasBestMoves = saveService.TryGetBestMoveCount("level_invalid", out _);

            Assert.That(hasBestMoves, Is.False);
        }

        [Test]
        public void CurrentAndUnlockedIndices_RoundTripSuccessfully()
        {
            SaveService saveService = new SaveService();

            saveService.SetCurrentLevelIndex(2);
            saveService.SetUnlockedLevelIndex(4);

            Assert.That(saveService.GetCurrentLevelIndex(), Is.EqualTo(2));
            Assert.That(saveService.GetUnlockedLevelIndex(), Is.EqualTo(4));
        }

        private void ClearSaveData()
        {
            PlayerPrefs.DeleteKey(CurrentLevelKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);

            for (int i = 0; i < bestMoveLevelIds.Length; i++)
            {
                PlayerPrefs.DeleteKey($"{BestMovePrefix}{bestMoveLevelIds[i]}");
            }

            PlayerPrefs.Save();
        }
    }
}
