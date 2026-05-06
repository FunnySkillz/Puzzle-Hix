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

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(CurrentLevelKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            PlayerPrefs.DeleteKey(BestMoveKey);
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
