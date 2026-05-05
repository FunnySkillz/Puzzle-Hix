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
    /// Validates the menu flow and the active match-3 prototype scene.
    /// </summary>
    public class RuntimeSmokeTests
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string PuzzleBoardSceneName = "PuzzleBoard";

        [UnityTest]
        public IEnumerator MainMenuScene_LoadsWithController()
        {
            SceneManager.LoadScene(MainMenuSceneName);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuSceneName));
            Assert.That(Object.FindObjectOfType<MainMenuController>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator StartFlow_LoadsPuzzleBoardAndBuildsMatch3Board()
        {
            SceneManager.LoadScene(MainMenuSceneName);
            yield return null;

            MainMenuController mainMenuController = Object.FindObjectOfType<MainMenuController>();
            Assert.That(mainMenuController, Is.Not.Null);

            mainMenuController.OnStartGame();
            yield return WaitForScene(PuzzleBoardSceneName, 3f);
            yield return null;

            BoardManager boardManager = Object.FindObjectOfType<BoardManager>();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(PuzzleBoardSceneName));
            Assert.That(boardManager, Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<Piece>().Length, Is.EqualTo(64));
            Assert.That(GameObject.Find("ScoreText"), Is.Not.Null);
            Assert.That(GameObject.Find("MovesText"), Is.Not.Null);
            Assert.That(GameObject.Find("TargetText"), Is.Not.Null);
            Assert.That(Resources.FindObjectsOfTypeAll<Button>().Any(button => button.name == "RestartButton"), Is.True);
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
        }

        [UnityTest]
        public IEnumerator WinLossAndRestart_EndStatesWork()
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

            boardManager.StartNewGame();
            yield return null;
            Assert.That(boardManager.IsGameOver, Is.False);
            Assert.That(boardManager.CurrentScore, Is.EqualTo(0));
            Assert.That(boardManager.MovesRemaining, Is.EqualTo(25));

            boardManager.SetBoardForTesting(CreatePlayableBoard(), 1, 999999);
            yield return null;
            yield return PlayFirstValidSwap(boardManager);

            Assert.That(boardManager.IsGameOver, Is.True);
            Assert.That(boardManager.HasWon, Is.False);
            Assert.That(GameObject.Find("EndGameTitle").GetComponent<Text>().text, Is.EqualTo("Game Over"));

            boardManager.StartNewGame();
            yield return null;
            Assert.That(boardManager.IsGameOver, Is.False);
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
    }
}
