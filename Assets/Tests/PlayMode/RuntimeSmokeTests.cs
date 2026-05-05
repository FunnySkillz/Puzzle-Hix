using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PuzzleDungeon.Core;
using PuzzleDungeon.Gameplay;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleDungeon.Tests.PlayMode
{
    /// <summary>
    /// Validates scene startup and a minimal gameplay interaction path in runtime context.
    /// </summary>
    public class RuntimeSmokeTests
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string PuzzleBoardSceneName = "PuzzleBoard";

        private const string CurrentLevelKey = "PuzzleDungeon.CurrentLevelIndex";
        private const string UnlockedLevelKey = "PuzzleDungeon.UnlockedLevelIndex";
        private const string BestMoveLevelTestKey = "PuzzleDungeon.BestMove.level_test";
        private const string BestMoveLevelTest2Key = "PuzzleDungeon.BestMove.level_test_2";
        private const string BestMoveLevelPrototype3Key = "PuzzleDungeon.BestMove.level_prototype_3";
        private const string BestMoveLevelPrototype4Key = "PuzzleDungeon.BestMove.level_prototype_4";
        private const string BestMoveLevelPrototype5Key = "PuzzleDungeon.BestMove.level_prototype_5";

        [SetUp]
        public void SetUp()
        {
            ClearProgressKeys();
        }

        [TearDown]
        public void TearDown()
        {
            ClearProgressKeys();
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
        public IEnumerator StartFlow_LoadsPuzzleBoardAndBuildsBoardUi()
        {
            SceneManager.LoadScene(MainMenuSceneName);
            yield return null;

            MainMenuController mainMenuController = Object.FindObjectOfType<MainMenuController>();
            Assert.That(mainMenuController, Is.Not.Null);

            mainMenuController.OnStartGame();
            yield return WaitForScene(PuzzleBoardSceneName, 3f);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(PuzzleBoardSceneName));
            Assert.That(Object.FindObjectOfType<GameController>(), Is.Not.Null);
            Assert.That(GameObject.Find("MoveCountText"), Is.Not.Null);
            Assert.That(GameObject.Find("StatusText"), Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<BoardCellView>().Length, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator PuzzleBoard_LoadsPrototypeThemeVisuals()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            GameObject backgroundObject = GameObject.Find("CanvasBackground");
            GameObject firstCellObject = GameObject.Find("Cell_0_0");

            Assert.That(backgroundObject, Is.Not.Null);
            Assert.That(firstCellObject, Is.Not.Null);
            Assert.That(firstCellObject.GetComponent<Image>().sprite, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator GameplayInteraction_ValidThenInvalid_KeepsBoardResponsive()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            GameController gameController = Object.FindObjectOfType<GameController>();
            Assert.That(gameController, Is.Not.Null);

            Text moveCounter = GameObject.Find("MoveCountText")?.GetComponent<Text>();
            Assert.That(moveCounter, Is.Not.Null);
            Assert.That(moveCounter.text, Does.Contain("0"));

            // Level_Test: move tile "a" from (1,0) to empty (1,1) should be valid.
            LogAssert.Expect(LogType.Log, new Regex(@"^Valid move \(1, 0\) -> \(1, 1\)\. Moves used: 1\..*$"));
            gameController.HandleCellInteraction(new Position(1, 0));
            gameController.HandleCellInteraction(new Position(1, 1));
            yield return null;

            Assert.That(moveCounter.text, Does.StartWith("Moves: 1/"));

            // Attempt adjacent occupied destination to verify invalid move handling and stable move count.
            LogAssert.Expect(LogType.Warning, new Regex(@"^Invalid move \(1, 1\) -> \(2, 1\): .*occupied\.$"));
            gameController.HandleCellInteraction(new Position(1, 1));
            gameController.HandleCellInteraction(new Position(2, 1));
            yield return null;

            Assert.That(moveCounter.text, Does.StartWith("Moves: 1/"));
        }

        [UnityTest]
        public IEnumerator GameplayInteraction_DiagonalAndLongDistanceMoves_FailWithoutMoveCountChange()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            GameController gameController = Object.FindObjectOfType<GameController>();
            Text moveCounter = GameObject.Find("MoveCountText")?.GetComponent<Text>();

            Assert.That(gameController, Is.Not.Null);
            Assert.That(moveCounter, Is.Not.Null);

            LogAssert.Expect(LogType.Warning, new Regex(@"^Invalid move \(0, 0\) -> \(1, 1\): .*one square.*$"));
            gameController.HandleCellInteraction(new Position(0, 0));
            gameController.HandleCellInteraction(new Position(1, 1));
            yield return null;

            Assert.That(moveCounter.text, Does.StartWith("Moves: 0/"));

            LogAssert.Expect(LogType.Warning, new Regex(@"^Invalid move \(0, 0\) -> \(0, 2\): .*one square.*$"));
            gameController.HandleCellInteraction(new Position(0, 2));
            yield return null;

            Assert.That(moveCounter.text, Does.StartWith("Moves: 0/"));
        }

        [UnityTest]
        public IEnumerator WinRetryNextAndMenuFlow_WorkFromPuzzleBoard()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            GameController gameController = Object.FindObjectOfType<GameController>();
            Assert.That(gameController, Is.Not.Null);

            SolveFirstLevel(gameController);
            yield return null;

            GameObject winPopup = GameObject.Find("WinPopup");
            Assert.That(winPopup, Is.Not.Null);
            Assert.That(winPopup.activeSelf, Is.True);

            gameController.OnNextLevelPressed();
            yield return null;

            Text levelTitle = GameObject.Find("LevelTitleText")?.GetComponent<Text>();
            Text moveCounter = GameObject.Find("MoveCountText")?.GetComponent<Text>();
            Assert.That(levelTitle, Is.Not.Null);
            Assert.That(moveCounter, Is.Not.Null);
            Assert.That(levelTitle.text, Is.EqualTo("Level 2"));
            Assert.That(moveCounter.text, Does.StartWith("Moves: 0/"));

            gameController.OnRetryPressed();
            yield return null;

            Assert.That(moveCounter.text, Does.StartWith("Moves: 0/"));

            gameController.OnMenuPressed();
            yield return WaitForScene(MainMenuSceneName, 3f);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenuSceneName));
        }

        [UnityTest]
        public IEnumerator FailPopupAndRetry_WorkWhenMoveLimitIsReached()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            GameController gameController = Object.FindObjectOfType<GameController>();
            Text moveCounter = GameObject.Find("MoveCountText")?.GetComponent<Text>();
            Assert.That(gameController, Is.Not.Null);
            Assert.That(moveCounter, Is.Not.Null);

            for (int i = 0; i < 4; i++)
            {
                gameController.HandleCellInteraction(new Position(1, 0));
                gameController.HandleCellInteraction(new Position(1, 1));
                yield return null;

                gameController.HandleCellInteraction(new Position(1, 1));
                gameController.HandleCellInteraction(new Position(1, 0));
                yield return null;
            }

            GameObject failPopup = GameObject.Find("FailPopup");
            Assert.That(failPopup, Is.Not.Null);
            Assert.That(failPopup.activeSelf, Is.True);
            Assert.That(moveCounter.text, Does.StartWith("Moves: 8/"));

            gameController.OnRetryPressed();
            yield return null;

            Assert.That(failPopup.activeSelf, Is.False);
            Assert.That(moveCounter.text, Does.StartWith("Moves: 0/"));
        }

        [UnityTest]
        public IEnumerator SavedProgress_ResumesUnlockedCurrentLevel()
        {
            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            GameController gameController = Object.FindObjectOfType<GameController>();
            Assert.That(gameController, Is.Not.Null);

            SolveFirstLevel(gameController);
            yield return null;

            gameController.OnNextLevelPressed();
            yield return null;

            SceneManager.LoadScene(PuzzleBoardSceneName);
            yield return null;

            Text levelTitle = GameObject.Find("LevelTitleText")?.GetComponent<Text>();
            Assert.That(levelTitle, Is.Not.Null);
            Assert.That(levelTitle.text, Is.EqualTo("Level 2"));
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
        }

        private static void SolveFirstLevel(GameController gameController)
        {
            gameController.HandleCellInteraction(new Position(0, 0));
            gameController.HandleCellInteraction(new Position(0, 1));
            gameController.HandleCellInteraction(new Position(0, 1));
            gameController.HandleCellInteraction(new Position(0, 2));
            gameController.HandleCellInteraction(new Position(0, 2));
            gameController.HandleCellInteraction(new Position(0, 3));
            gameController.HandleCellInteraction(new Position(0, 3));
            gameController.HandleCellInteraction(new Position(1, 3));
            gameController.HandleCellInteraction(new Position(1, 3));
            gameController.HandleCellInteraction(new Position(2, 3));
            gameController.HandleCellInteraction(new Position(2, 3));
            gameController.HandleCellInteraction(new Position(3, 3));
        }

        private static void ClearProgressKeys()
        {
            PlayerPrefs.DeleteKey(CurrentLevelKey);
            PlayerPrefs.DeleteKey(UnlockedLevelKey);
            PlayerPrefs.DeleteKey(BestMoveLevelTestKey);
            PlayerPrefs.DeleteKey(BestMoveLevelTest2Key);
            PlayerPrefs.DeleteKey(BestMoveLevelPrototype3Key);
            PlayerPrefs.DeleteKey(BestMoveLevelPrototype4Key);
            PlayerPrefs.DeleteKey(BestMoveLevelPrototype5Key);
            PlayerPrefs.Save();
        }
    }
}
