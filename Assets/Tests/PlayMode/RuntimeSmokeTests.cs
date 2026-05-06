using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuzzleDungeon.Gameplay.Match3;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleDungeon.Tests.PlayMode
{
    /// <summary>
    /// Validates the menu flow and the active match-3 MVP prototype scene.
    /// </summary>
    public class RuntimeSmokeTests
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string LevelMapSceneName = "LevelMap";
        private const string PuzzleBoardSceneName = "PuzzleBoard";
        private const string CurrentLevelKey = "PuzzleDungeon.CurrentLevelIndex";
        private const string UnlockedLevelKey = "PuzzleDungeon.UnlockedLevelIndex";

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

        [UnityTest]
        public IEnumerator MainMenuScene_LoadsWithController()
        {
            SceneManager.LoadScene(MainMenuSceneName);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuSceneName));
            Assert.That(Object.FindObjectOfType<MainMenuController>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator StartFlow_LoadsLevelMapThenPuzzleBoardAndBuildsLevelOneBoard()
        {
            Screen.SetResolution(1080, 1920, false);
            SceneManager.LoadScene(MainMenuSceneName);
            yield return null;

            MainMenuController mainMenuController = Object.FindObjectOfType<MainMenuController>();
            Assert.That(mainMenuController, Is.Not.Null);

            mainMenuController.OnStartGame();
            yield return WaitForScene(LevelMapSceneName, 3f);
            yield return null;

            LevelMapController levelMapController = Object.FindObjectOfType<LevelMapController>();
            Assert.That(levelMapController, Is.Not.Null);
            Assert.That(GameObject.Find("PlayerLevelText"), Is.Not.Null);
            Assert.That(GameObject.Find("XpText"), Is.Not.Null);
            Assert.That(GameObject.Find("TotalStarsText"), Is.Not.Null);
            Assert.That(GameObject.Find("LevelNode_01").GetComponent<Button>().interactable, Is.True);
            Assert.That(GameObject.Find("LevelNode_02").GetComponent<Button>().interactable, Is.False);

            levelMapController.StartLevel(0);
            yield return WaitForScene(PuzzleBoardSceneName, 3f);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(PuzzleBoardSceneName));
            Assert.That(boardManager, Is.Not.Null);
            Assert.That(boardManager.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<Piece>().Length, Is.EqualTo(64));
            Assert.That(GameObject.Find("LevelText"), Is.Not.Null);
            Assert.That(GameObject.Find("ObjectiveText"), Is.Not.Null);
            Assert.That(GameObject.Find("MovesText"), Is.Not.Null);
            Assert.That(Resources.FindObjectsOfTypeAll<Button>().Any(button => button.name == "RetryButton"), Is.True);
            Assert.That(Resources.FindObjectsOfTypeAll<Button>().Any(button => button.name == "NextButton"), Is.True);
        }

        [UnityTest]
        public IEnumerator LevelMap_AllowsReplayOfCompletedLevelsAndBlocksLockedLevels()
        {
            SceneManager.LoadScene(LevelMapSceneName);
            yield return null;

            LevelMapController levelMapController = Object.FindObjectOfType<LevelMapController>();
            Assert.That(levelMapController.GetNodeState(0).IsUnlocked, Is.True);
            Assert.That(levelMapController.GetNodeState(1).IsUnlocked, Is.False);

            levelMapController.StartLevel(1);
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(LevelMapSceneName));

            PlayerPrefs.SetInt(UnlockedLevelKey, 1);
            PlayerPrefs.SetInt("PuzzleDungeon.Stars.match3_level_01", 2);
            PlayerPrefs.Save();
            levelMapController.RefreshMap();

            Assert.That(levelMapController.GetNodeState(0).IsCompleted, Is.True);
            Assert.That(levelMapController.GetNodeState(0).Stars, Is.EqualTo(2));
            Assert.That(levelMapController.GetNodeState(1).IsUnlocked, Is.True);

            levelMapController.StartLevel(0);
            yield return WaitForScene(PuzzleBoardSceneName, 3f);
        }

        [UnityTest]
        public IEnumerator PuzzleBoard_UsesThemeOrFallbackVisuals()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            Piece firstPiece = Object.FindObjectsOfType<Piece>().FirstOrDefault();
            Image firstPieceImage = firstPiece != null ? firstPiece.GetComponent<Image>() : null;

            Assert.That(GameObject.Find("CanvasBackground"), Is.Not.Null);
            Assert.That(GameObject.Find("Match3Cell_0_0"), Is.Not.Null);
            Assert.That(firstPiece, Is.Not.Null);
            Assert.That(firstPieceImage, Is.Not.Null);
            Assert.That(firstPieceImage.color.a, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator ClickValidSwap_ConsumesMoveScoresAndRefillsBoard()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 25, 999999);
            yield return null;

            Assert.That(boardManager.TryFindFirstValidSwap(out Vector2Int from, out Vector2Int to), Is.True);

            int startingMoves = boardManager.MovesRemaining;
            boardManager.HandlePieceClicked(boardManager.GetPieceAt(from.x, from.y));
            boardManager.HandlePieceClicked(boardManager.GetPieceAt(to.x, to.y));
            Assert.That(boardManager.IsInputBlocked, Is.True);

            yield return WaitUntilStable(boardManager);

            Assert.That(boardManager.MovesRemaining, Is.EqualTo(startingMoves - 1));
            Assert.That(boardManager.CurrentScore, Is.GreaterThan(0));
            AssertBoardIsFilledAndSynchronized(boardManager, 8, 8);
        }

        [UnityTest]
        public IEnumerator ClickInvalidSwap_ReturnsPiecesAndKeepsMoves()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 25, 999999);
            yield return null;

            Assert.That(boardManager.TryFindFirstInvalidAdjacentSwap(out Vector2Int from, out Vector2Int to), Is.True);

            Piece firstBefore = boardManager.GetPieceAt(from.x, from.y);
            Piece secondBefore = boardManager.GetPieceAt(to.x, to.y);
            int startingMoves = boardManager.MovesRemaining;
            int startingScore = boardManager.CurrentScore;

            boardManager.HandlePieceClicked(firstBefore);
            boardManager.HandlePieceClicked(secondBefore);
            yield return WaitUntilStable(boardManager);

            Assert.That(boardManager.MovesRemaining, Is.EqualTo(startingMoves));
            Assert.That(boardManager.CurrentScore, Is.EqualTo(startingScore));
            Assert.That(boardManager.GetPieceAt(from.x, from.y), Is.EqualTo(firstBefore));
            Assert.That(boardManager.GetPieceAt(to.x, to.y), Is.EqualTo(secondBefore));
        }

        [UnityTest]
        public IEnumerator MatchFour_CreatesReadableSpecialPiece()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreateMatchFourBoard(), 25, 999999);
            yield return null;

            boardManager.HandlePieceClicked(boardManager.GetPieceAt(1, 0));
            boardManager.HandlePieceClicked(boardManager.GetPieceAt(1, 1));
            yield return WaitUntilStable(boardManager);

            Assert.That(Object.FindObjectsOfType<Piece>().Any(piece => piece.SpecialPieceType != SpecialPieceType.None), Is.True);
            AssertBoardIsFilledAndSynchronized(boardManager, 5, 5);
        }

        [UnityTest]
        public IEnumerator DragSwap_UsesDominantCardinalDirection()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 25, 999999);
            yield return null;

            Assert.That(boardManager.TryFindFirstValidSwap(out Vector2Int from, out Vector2Int to), Is.True);

            int startingMoves = boardManager.MovesRemaining;
            Vector2 delta = new Vector2(to.x - from.x, to.y - from.y) * 80f;
            boardManager.HandlePieceDrag(boardManager.GetPieceAt(from.x, from.y), delta);
            yield return WaitUntilStable(boardManager);

            Assert.That(boardManager.MovesRemaining, Is.EqualTo(startingMoves - 1));
            Assert.That(boardManager.CurrentScore, Is.GreaterThan(0));
            AssertBoardIsFilledAndSynchronized(boardManager, 8, 8);
        }

        [UnityTest]
        public IEnumerator WinNextRetryAndResumeProgression_WorkFromPuzzleBoard()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 25, 10);
            yield return null;

            yield return PlayFirstValidSwap(boardManager);

            Assert.That(boardManager.IsGameOver, Is.True);
            Assert.That(boardManager.HasWon, Is.True);
            Assert.That(GameObject.Find("EndGameTitle").GetComponent<Text>().text, Is.EqualTo("Level Complete"));
            Assert.That(boardManager.LastLevelResult, Is.Not.Null);
            Assert.That(boardManager.LastLevelResult.Stars, Is.GreaterThanOrEqualTo(1));
            Assert.That(PlayerPrefs.GetInt("PuzzleDungeon.PlayerXp", 0), Is.GreaterThan(0));

            boardManager.GoToNextLevel();
            yield return null;

            Assert.That(boardManager.CurrentLevelNumber, Is.EqualTo(2));
            Assert.That(boardManager.IsGameOver, Is.False);
            Assert.That(boardManager.CurrentScore, Is.EqualTo(0));

            boardManager.RetryCurrentLevel();
            yield return null;

            Assert.That(boardManager.CurrentLevelNumber, Is.EqualTo(2));
            Assert.That(boardManager.CurrentScore, Is.EqualTo(0));

            boardManager.ReturnToMenu();
            yield return WaitForScene(LevelMapSceneName, 3f);
            yield return null;

            LevelMapController levelMapController = Object.FindObjectOfType<LevelMapController>();
            Assert.That(levelMapController.GetNodeState(0).IsCompleted, Is.True);
            Assert.That(levelMapController.GetNodeState(1).IsUnlocked, Is.True);

            levelMapController.StartLevel(1);
            yield return WaitForScene(PuzzleBoardSceneName, 3f);
            yield return null;

            BoardManager resumedBoard = Object.FindObjectOfType<BoardManager>();
            Assert.That(resumedBoard.CurrentLevelNumber, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RetryReplay_DoesNotDuplicateStarsOrXp()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 25, 10);
            yield return null;

            yield return PlayFirstValidSwap(boardManager);

            int starsAfterFirstWin = PlayerPrefs.GetInt("PuzzleDungeon.Stars.match3_level_01", 0);
            int xpAfterFirstWin = PlayerPrefs.GetInt("PuzzleDungeon.PlayerXp", 0);

            boardManager.RetryCurrentLevel();
            yield return null;
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 25, 10);
            yield return null;
            yield return PlayFirstValidSwap(boardManager);

            Assert.That(PlayerPrefs.GetInt("PuzzleDungeon.Stars.match3_level_01", 0), Is.EqualTo(starsAfterFirstWin));
            Assert.That(PlayerPrefs.GetInt("PuzzleDungeon.PlayerXp", 0), Is.EqualTo(xpAfterFirstWin));
            Assert.That(boardManager.LastLevelResult.XpAwarded, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator LossAndRetry_ResetLevelState()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            boardManager.SetBoardForTesting(CreatePlayableBoard(), 1, 999999);
            yield return null;

            yield return PlayFirstValidSwap(boardManager);

            Assert.That(boardManager.IsGameOver, Is.True);
            Assert.That(boardManager.HasWon, Is.False);
            Assert.That(GameObject.Find("EndGameTitle").GetComponent<Text>().text, Is.EqualTo("Game Over"));

            boardManager.RetryCurrentLevel();
            yield return null;

            Assert.That(boardManager.IsGameOver, Is.False);
            Assert.That(boardManager.CurrentScore, Is.EqualTo(0));
            Assert.That(boardManager.MovesRemaining, Is.GreaterThan(0));
            Assert.That(Object.FindObjectsOfType<Piece>().Length, Is.EqualTo(64));
        }

        private static IEnumerator PlayFirstValidSwap(BoardManager boardManager)
        {
            Assert.That(boardManager.TryFindFirstValidSwap(out Vector2Int from, out Vector2Int to), Is.True);
            boardManager.HandlePieceClicked(boardManager.GetPieceAt(from.x, from.y));
            boardManager.HandlePieceClicked(boardManager.GetPieceAt(to.x, to.y));

            float endTime = Time.realtimeSinceStartup + 5f;

            while (Time.realtimeSinceStartup < endTime)
            {
                if (boardManager.IsGameOver || !boardManager.IsInputBlocked)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Board did not finish resolving within the timeout.");
        }

        private static IEnumerator WaitUntilStable(BoardManager boardManager)
        {
            float endTime = Time.realtimeSinceStartup + 5f;

            while (Time.realtimeSinceStartup < endTime)
            {
                if (!boardManager.IsInputBlocked)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Board did not finish resolving within the timeout.");
        }

        private static IEnumerator WaitForScene(string expectedSceneName, float timeoutSeconds)
        {
            float endTime = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < endTime)
            {
                if (SceneManager.GetActiveScene().name == expectedSceneName)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Scene '{expectedSceneName}' was not loaded within the timeout.");
        }

        private static PieceType[,] CreatePlayableBoard()
        {
            return BoardManager.GenerateBoardTypes(8, 8, AllTypes(), 1234);
        }

        private static PieceType[,] CreateMatchFourBoard()
        {
            PieceType R = PieceType.Red;
            PieceType B = PieceType.Blue;
            PieceType G = PieceType.Green;
            PieceType Y = PieceType.Yellow;
            PieceType P = PieceType.Purple;
            PieceType[,] board = new PieceType[5, 5];

            SetRow(board, 0, new[] { R, B, R, R, G });
            SetRow(board, 1, new[] { B, R, G, Y, P });
            SetRow(board, 2, new[] { G, Y, P, B, R });
            SetRow(board, 3, new[] { Y, P, B, G, Y });
            SetRow(board, 4, new[] { P, B, Y, R, B });
            return board;
        }

        private static void SetRow(PieceType[,] board, int y, PieceType[] row)
        {
            for (int x = 0; x < row.Length; x++)
            {
                board[x, y] = row[x];
            }
        }

        private static PieceType[] AllTypes()
        {
            return new[]
            {
                PieceType.Red,
                PieceType.Blue,
                PieceType.Green,
                PieceType.Yellow,
                PieceType.Purple,
                PieceType.Orange
            };
        }

        private static void AssertBoardIsFilledAndSynchronized(BoardManager boardManager, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Piece piece = boardManager.GetPieceAt(x, y);
                    Assert.That(piece, Is.Not.Null, $"Expected a piece at {x},{y}.");
                    Assert.That(piece.GridX, Is.EqualTo(x));
                    Assert.That(piece.GridY, Is.EqualTo(y));
                }
            }
        }

        private static void ClearProgress()
        {
            PlayerPrefs.DeleteKey(CurrentLevelKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            PlayerPrefs.DeleteKey("PuzzleDungeon.BestMove.match3_level_01");
            PlayerPrefs.DeleteKey("PuzzleDungeon.Stars.match3_level_01");
            PlayerPrefs.DeleteKey("PuzzleDungeon.PlayerXp");
            PlayerPrefs.DeleteKey("PuzzleDungeon.PlayerLevel");
            PlayerPrefs.DeleteKey("PuzzleDungeon.TotalStars");
            PlayerPrefs.Save();
        }
    }
}
