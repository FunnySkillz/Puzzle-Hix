using System.Linq;
using NUnit.Framework;
using PuzzleDungeon.Gameplay.Match3;
using PuzzleDungeon.Services;
using UnityEngine;

namespace PuzzleDungeon.Tests.EditMode.Match3
{
    public class Match3MvpTests
    {
        private const string CurrentLevelKey = "PuzzleDungeon.CurrentLevelIndex";
        private const string UnlockedLevelKey = "PuzzleDungeon.UnlockedLevelIndex";
        private const string BestMoveKey = "PuzzleDungeon.BestMove.match3_level_01";
        private const string StarKey = "PuzzleDungeon.Stars.match3_level_01";
        private const string PlayerXpKey = "PuzzleDungeon.PlayerXp";
        private const string PlayerLevelKey = "PuzzleDungeon.PlayerLevel";
        private const string TotalStarsKey = "PuzzleDungeon.TotalStars";

        [SetUp]
        public void SetUp()
        {
            ClearProgress();
        }

        [TearDown]
        public void TearDown()
        {
            ClearProgress();
        }

        private static void ClearProgress()
        {
            PlayerPrefs.DeleteKey(CurrentLevelKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            PlayerPrefs.DeleteKey(BestMoveKey);
            PlayerPrefs.DeleteKey(StarKey);
            PlayerPrefs.DeleteKey(PlayerXpKey);
            PlayerPrefs.DeleteKey(PlayerLevelKey);
            PlayerPrefs.DeleteKey(TotalStarsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void DefaultCatalog_LoadsTwentyOrderedValidLevels()
        {
            Match3LevelCatalog catalog = Match3LevelCatalog.LoadDefault();

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.LevelCount, Is.EqualTo(20));

            for (int i = 0; i < catalog.LevelCount; i++)
            {
                Match3LevelData level = catalog.GetLevelByIndex(i);
                Assert.That(level, Is.Not.Null);
                Assert.That(level.LevelNumber, Is.EqualTo(i + 1));
                Assert.That(level.Width, Is.InRange(3, 10));
                Assert.That(level.Height, Is.InRange(3, 10));
                Assert.That(level.Moves, Is.GreaterThan(0));
                Assert.That(level.AvailablePieceTypeCount, Is.InRange(3, 6));
            }
        }

        [Test]
        public void DefaultCatalog_ContainsRequiredObjectiveBands()
        {
            Match3LevelCatalog catalog = Match3LevelCatalog.LoadDefault();

            Assert.That(catalog.GetLevelByIndex(0).ObjectiveType, Is.EqualTo(ObjectiveType.Score));
            Assert.That(catalog.GetLevelByIndex(4).ObjectiveType, Is.EqualTo(ObjectiveType.Score));
            Assert.That(catalog.GetLevelByIndex(5).ObjectiveType, Is.EqualTo(ObjectiveType.CollectColor));
            Assert.That(catalog.GetLevelByIndex(9).ObjectiveType, Is.EqualTo(ObjectiveType.CollectColor));
            Assert.That(catalog.GetLevelByIndex(10).ObjectiveType, Is.EqualTo(ObjectiveType.ClearPieces));
            Assert.That(catalog.GetLevelByIndex(14).ObjectiveType, Is.EqualTo(ObjectiveType.ClearPieces));
            Assert.That(catalog.GetLevelByIndex(15).ObjectiveType, Is.EqualTo(ObjectiveType.ScoreAndCollect));
            Assert.That(catalog.GetLevelByIndex(19).ObjectiveType, Is.EqualTo(ObjectiveType.ScoreAndCollect));
        }

        [Test]
        public void ObjectiveEvaluation_WorksForScoreCollectClearAndMixedGoals()
        {
            GameObject gameObject = new GameObject("GameManagerTest", typeof(GameManager));
            GameManager gameManager = gameObject.GetComponent<GameManager>();
            BoardConfig config = ScriptableObject.CreateInstance<BoardConfig>();

            Match3LevelData scoreLevel = CreateLevel(ObjectiveType.Score, 30, 100, null, 0);
            gameManager.Initialize(scoreLevel, config, null);
            gameManager.RecordClearedPieces(Enumerable.Repeat(PieceType.Red, 10), 100);
            Assert.That(gameManager.IsObjectiveComplete(), Is.True);

            Match3LevelData collectLevel = CreateLevel(ObjectiveType.CollectColor, 30, 1000, new[] { new ColorGoal(PieceType.Blue, 3) }, 0);
            gameManager.Initialize(collectLevel, config, null);
            gameManager.RecordClearedPieces(new[] { PieceType.Blue, PieceType.Red, PieceType.Blue, PieceType.Blue }, 40);
            Assert.That(gameManager.IsObjectiveComplete(), Is.True);

            Match3LevelData clearLevel = CreateLevel(ObjectiveType.ClearPieces, 30, 1000, null, 5);
            gameManager.Initialize(clearLevel, config, null);
            gameManager.RecordClearedPieces(Enumerable.Repeat(PieceType.Green, 5), 50);
            Assert.That(gameManager.IsObjectiveComplete(), Is.True);

            Match3LevelData mixedLevel = CreateLevel(ObjectiveType.ScoreAndCollect, 30, 80, new[] { new ColorGoal(PieceType.Yellow, 2) }, 0);
            gameManager.Initialize(mixedLevel, config, null);
            gameManager.RecordClearedPieces(new[] { PieceType.Yellow, PieceType.Yellow, PieceType.Purple }, 90);
            Assert.That(gameManager.IsObjectiveComplete(), Is.True);

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SpecialPieceDetection_CoversMatchFourMatchFiveTAndL()
        {
            Assert.That(FindSpecial(CreateHorizontalMatchBoard(4), new Vector2Int(1, 1)), Is.EqualTo(SpecialPieceType.LineHorizontal));
            Assert.That(FindSpecial(CreateHorizontalMatchBoard(5), new Vector2Int(2, 1)), Is.EqualTo(SpecialPieceType.ColorClear));
            Assert.That(FindSpecial(CreateTShapeBoard(), new Vector2Int(2, 2)), Is.EqualTo(SpecialPieceType.Bomb));
            Assert.That(FindSpecial(CreateLShapeBoard(), new Vector2Int(1, 1)), Is.EqualTo(SpecialPieceType.Bomb));
        }

        [Test]
        public void ProgressService_UnlocksNextLevelWithoutSkipping()
        {
            SaveService saveService = new SaveService();
            Match3ProgressService progressService = new Match3ProgressService(saveService);

            progressService.MarkLevelComplete(0, 20, "match3_level_01", 7);

            Assert.That(saveService.GetUnlockedLevelIndex(), Is.EqualTo(1));
            Assert.That(saveService.GetCurrentLevelIndex(), Is.EqualTo(1));
            Assert.That(saveService.TryGetBestMoveCount("match3_level_01", out int bestMoves), Is.True);
            Assert.That(bestMoves, Is.EqualTo(7));

            progressService.MarkLevelComplete(0, 20, "match3_level_01", 10);

            Assert.That(saveService.GetUnlockedLevelIndex(), Is.EqualTo(1));
            Assert.That(saveService.TryGetBestMoveCount("match3_level_01", out bestMoves), Is.True);
            Assert.That(bestMoves, Is.EqualTo(7));
        }

        [Test]
        public void LevelResult_CalculatesOneTwoAndThreeStarWins()
        {
            Match3LevelData level = CreateLevel(ObjectiveType.Score, 20, 1000, null, 0);

            Assert.That(LevelResult.CalculateStars(level, false, 2000, 20), Is.EqualTo(StarRating.None));
            Assert.That(LevelResult.CalculateStars(level, true, 1000, 0), Is.EqualTo(StarRating.One));
            Assert.That(LevelResult.CalculateStars(level, true, 1250, 0), Is.EqualTo(StarRating.Two));
            Assert.That(LevelResult.CalculateStars(level, true, 1500, 0), Is.EqualTo(StarRating.Three));
            Assert.That(LevelResult.CalculateStars(level, true, 1000, 7), Is.EqualTo(StarRating.Three));
        }

        [Test]
        public void ProgressService_StarsXpAndPlayerLevel_DoNotRegressOrDuplicate()
        {
            SaveService saveService = new SaveService();
            Match3ProgressService progressService = new Match3ProgressService(saveService);
            Match3LevelData level = CreateLevel(ObjectiveType.Score, 20, 1000, null, 0);

            LevelResult firstResult = progressService.CompleteLevel(level, 0, 20, 1500, 8, 12, 2);
            int xpAfterFirstWin = saveService.GetPlayerXp();

            LevelResult replayResult = progressService.CompleteLevel(level, 0, 20, 1500, 8, 12, 2);

            Assert.That(firstResult.Stars, Is.EqualTo(3));
            Assert.That(firstResult.XpAwarded, Is.GreaterThan(0));
            Assert.That(saveService.GetLevelStars(level.LevelId), Is.EqualTo(3));
            Assert.That(saveService.GetTotalStars(), Is.EqualTo(3));
            Assert.That(saveService.GetPlayerXp(), Is.EqualTo(xpAfterFirstWin));
            Assert.That(saveService.GetPlayerLevel(), Is.EqualTo(PlayerProgressData.CalculatePlayerLevel(xpAfterFirstWin)));
            Assert.That(replayResult.XpAwarded, Is.EqualTo(0));
        }

        [Test]
        public void ProgressService_BuildsLevelMapNodeStates()
        {
            Match3LevelCatalog catalog = Match3LevelCatalog.LoadDefault();
            SaveService saveService = new SaveService();
            Match3ProgressService progressService = new Match3ProgressService(saveService);

            saveService.SetUnlockedLevelIndex(1);
            saveService.SetCurrentLevelIndex(1);
            saveService.SetLevelStars("match3_level_01", 2);

            LevelMapNodeState[] nodes = progressService.BuildLevelMap(catalog);

            Assert.That(nodes[0].IsUnlocked, Is.True);
            Assert.That(nodes[0].IsCompleted, Is.True);
            Assert.That(nodes[0].Stars, Is.EqualTo(2));
            Assert.That(nodes[1].IsUnlocked, Is.True);
            Assert.That(nodes[1].IsCurrent, Is.True);
            Assert.That(nodes[2].IsUnlocked, Is.False);
        }

        [Test]
        public void AudioService_WithMissingClips_DoesNotThrow()
        {
            GameObject gameObject = new GameObject("AudioServiceTest", typeof(AudioService));
            AudioService audioService = gameObject.GetComponent<AudioService>();

            Assert.DoesNotThrow(() =>
            {
                audioService.Play(AudioCue.ButtonClick);
                audioService.Play(AudioCue.Swap);
                audioService.Play(AudioCue.MatchClear);
                audioService.Play(AudioCue.Win);
            });

            Object.DestroyImmediate(gameObject);
        }

        private static Match3LevelData CreateLevel(ObjectiveType objectiveType, int moves, int targetScore, ColorGoal[] goals, int clearTarget)
        {
            Match3LevelData level = ScriptableObject.CreateInstance<Match3LevelData>();
            level.ConfigureForTesting(1, 8, 8, moves, targetScore, 6, objectiveType, goals, clearTarget);
            return level;
        }

        private static SpecialPieceType FindSpecial(PieceType?[,] board, Vector2Int position)
        {
            return BoardManager.DetermineSpecialPieceType(BoardManager.FindMatches(board), position);
        }

        private static PieceType?[,] CreateHorizontalMatchBoard(int length)
        {
            PieceType?[,] board = new PieceType?[6, 4];

            for (int x = 0; x < length; x++)
            {
                board[x, 1] = PieceType.Red;
            }

            return board;
        }

        private static PieceType?[,] CreateTShapeBoard()
        {
            PieceType?[,] board = new PieceType?[5, 5];
            board[1, 2] = PieceType.Blue;
            board[2, 2] = PieceType.Blue;
            board[3, 2] = PieceType.Blue;
            board[2, 1] = PieceType.Blue;
            board[2, 3] = PieceType.Blue;
            return board;
        }

        private static PieceType?[,] CreateLShapeBoard()
        {
            PieceType?[,] board = new PieceType?[5, 5];
            board[1, 1] = PieceType.Green;
            board[2, 1] = PieceType.Green;
            board[3, 1] = PieceType.Green;
            board[1, 2] = PieceType.Green;
            board[1, 3] = PieceType.Green;
            return board;
        }
    }
}
