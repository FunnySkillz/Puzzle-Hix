using System;
using System.Collections.Generic;
using PuzzleDungeon.Core;
using PuzzleDungeon.Services;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay
{
    /// <summary>
    /// Scene-facing gameplay controller that loads levels, handles board interaction, and manages retry/win/fail flow.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [Header("Level Progression")]
        [SerializeField] private LevelData[] levels = Array.Empty<LevelData>();

        [Header("Board View")]
        [SerializeField] private Vector2 cellSize = new Vector2(96f, 96f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(6f, 6f);

        [Header("Colors")]
        [SerializeField] private Color emptyCellColor = new Color(0.18f, 0.20f, 0.24f, 1f);
        [SerializeField] private Color occupiedCellColor = new Color(0.28f, 0.55f, 0.78f, 1f);
        [SerializeField] private Color selectedCellColor = new Color(0.95f, 0.76f, 0.25f, 1f);
        [SerializeField] private Color goalCellColor = new Color(0.30f, 0.70f, 0.40f, 1f);

        private readonly Dictionary<Position, BoardCellView> boardCellViews = new Dictionary<Position, BoardCellView>();
        private readonly LevelLoader levelLoader = new LevelLoader();

        private SaveService saveService;
        private BoardState boardState;
        private GameRules gameRules;
        private Position? selectedPosition;
        private int currentLevelIndex;

        private GridLayoutGroup boardGrid;
        private Text moveCountText;
        private Text levelTitleText;
        private GameObject winPopupPanel;
        private GameObject failPopupPanel;

        private void Awake()
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("GameController requires at least one LevelData in the levels list.");
                enabled = false;
                return;
            }

            saveService = new SaveService();

            EnsureEventSystemWithInputSystem();
            EnsureUiRoots();

            int savedIndex = saveService.GetCurrentLevelIndex();
            LoadLevel(Mathf.Clamp(savedIndex, 0, levels.Length - 1), true);
        }

        /// <summary>
        /// Handles click/tap on a board cell and attempts to move through GameRules only.
        /// </summary>
        public void HandleCellInteraction(Position clickedPosition)
        {
            if (boardState == null || gameRules == null)
            {
                return;
            }

            if (!gameRules.CanApplyMove(boardState))
            {
                UpdateMoveCounterText();
                UpdateTerminalPopups();
                return;
            }

            if (!selectedPosition.HasValue)
            {
                if (!boardState.IsCellEmpty(clickedPosition))
                {
                    selectedPosition = clickedPosition;
                    RefreshBoardVisuals();
                }

                return;
            }

            Position source = selectedPosition.Value;

            if (source == clickedPosition)
            {
                selectedPosition = null;
                RefreshBoardVisuals();
                return;
            }

            if (!boardState.IsCellEmpty(clickedPosition))
            {
                selectedPosition = clickedPosition;
                RefreshBoardVisuals();
                return;
            }

            // Route all move execution through GameRules to preserve gameplay authority in core logic.
            MoveResult result = gameRules.TryMove(boardState, source, clickedPosition);

            if (result.IsSuccess)
            {
                selectedPosition = null;
            }

            if (result.IsWin)
            {
                saveService.SetUnlockedLevelIndex(currentLevelIndex + 1);
                saveService.Flush();
            }

            RefreshBoardVisuals();
            UpdateMoveCounterText();
            UpdateTerminalPopups();
        }

        /// <summary>
        /// Retries the currently active level from its initial data.
        /// </summary>
        public void OnRetryPressed()
        {
            LoadLevel(currentLevelIndex, true);
        }

        /// <summary>
        /// Advances to the next level, or returns to main menu when no next level exists.
        /// </summary>
        public void OnNextLevelPressed()
        {
            int nextLevelIndex = currentLevelIndex + 1;

            if (nextLevelIndex >= levels.Length)
            {
                OnMenuPressed();
                return;
            }

            LoadLevel(nextLevelIndex, true);
        }

        /// <summary>
        /// Returns to the main menu scene.
        /// </summary>
        public void OnMenuPressed()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void LoadLevel(int levelIndex, bool persistCurrentLevel)
        {
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
            LevelData levelData = levels[currentLevelIndex];

            if (levelData == null)
            {
                Debug.LogError($"GameController has a missing LevelData reference at index {currentLevelIndex}.");
                enabled = false;
                return;
            }

            boardState = levelLoader.Load(levelData);
            gameRules = new GameRules(levelData.MoveLimit, new Position(levelData.GoalX, levelData.GoalY), levelData.GoalTileId);
            gameRules.ResetMoveCounter();
            selectedPosition = null;

            if (persistCurrentLevel)
            {
                saveService.SetCurrentLevelIndex(currentLevelIndex);
                saveService.Flush();
            }

            BuildBoardVisuals(levelData);
            RefreshBoardVisuals();
            UpdateLevelTitleText();
            UpdateMoveCounterText();
            UpdateTerminalPopups();
        }

        private void EnsureEventSystemWithInputSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            StandaloneInputModule legacyStandalone = eventSystem.GetComponent<StandaloneInputModule>();

            if (legacyStandalone != null)
            {
                Destroy(legacyStandalone);
            }
        }

        private void EnsureUiRoots()
        {
            Canvas canvas = FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("PuzzleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (levelTitleText == null)
            {
                levelTitleText = CreateText(canvas.transform, "LevelTitleText", new Vector2(0f, -12f), new Vector2(500f, 56f), 28);
            }

            if (moveCountText == null)
            {
                moveCountText = CreateText(canvas.transform, "MoveCountText", new Vector2(0f, -56f), new Vector2(500f, 64f), 34);
            }

            if (boardGrid == null)
            {
                GameObject boardObject = new GameObject("BoardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                boardObject.transform.SetParent(canvas.transform, false);

                RectTransform boardRect = boardObject.GetComponent<RectTransform>();
                boardRect.anchorMin = new Vector2(0.5f, 0.5f);
                boardRect.anchorMax = new Vector2(0.5f, 0.5f);
                boardRect.pivot = new Vector2(0.5f, 0.5f);
                boardRect.anchoredPosition = new Vector2(0f, -40f);

                boardGrid = boardObject.GetComponent<GridLayoutGroup>();
                boardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                boardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                boardGrid.childAlignment = TextAnchor.MiddleCenter;
                boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            }

            if (winPopupPanel == null)
            {
                winPopupPanel = CreatePopupPanel(canvas.transform, "WinPopup", "You Win!", true);
                CreatePopupButton(winPopupPanel.transform, "RetryButton", "Retry", new Vector2(-180f, -80f), OnRetryPressed);
                CreatePopupButton(winPopupPanel.transform, "NextButton", "Next Level", new Vector2(0f, -80f), OnNextLevelPressed);
                CreatePopupButton(winPopupPanel.transform, "MenuButton", "Menu", new Vector2(180f, -80f), OnMenuPressed);
            }

            if (failPopupPanel == null)
            {
                failPopupPanel = CreatePopupPanel(canvas.transform, "FailPopup", "Level Failed", true);
                CreatePopupButton(failPopupPanel.transform, "RetryButton", "Retry", new Vector2(-90f, -80f), OnRetryPressed);
                CreatePopupButton(failPopupPanel.transform, "MenuButton", "Menu", new Vector2(90f, -80f), OnMenuPressed);
            }
        }

        private void BuildBoardVisuals(LevelData levelData)
        {
            boardCellViews.Clear();

            for (int i = boardGrid.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(boardGrid.transform.GetChild(i).gameObject);
            }

            boardGrid.cellSize = cellSize;
            boardGrid.spacing = cellSpacing;
            boardGrid.constraintCount = boardState.Width;

            RectTransform gridRect = boardGrid.GetComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(
                (cellSize.x * boardState.Width) + (cellSpacing.x * (boardState.Width - 1)),
                (cellSize.y * boardState.Height) + (cellSpacing.y * (boardState.Height - 1)));

            // Build top-to-bottom so layout visually matches board coordinates.
            for (int y = boardState.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < boardState.Width; x++)
                {
                    Position position = new Position(x, y);
                    GameObject cellObject = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(BoardCellView));
                    cellObject.transform.SetParent(boardGrid.transform, false);

                    BoardCellView cellView = cellObject.GetComponent<BoardCellView>();
                    cellView.Initialize(position, HandleCellInteraction);
                    boardCellViews[position] = cellView;
                }
            }
        }

        private void RefreshBoardVisuals()
        {
            LevelData levelData = levels[currentLevelIndex];

            foreach (KeyValuePair<Position, BoardCellView> pair in boardCellViews)
            {
                Position position = pair.Key;
                BoardCellView view = pair.Value;

                Tile tile = boardState.GetTile(position);
                bool isSelected = selectedPosition.HasValue && selectedPosition.Value == position;
                bool isGoalCell = position.X == levelData.GoalX && position.Y == levelData.GoalY;

                view.SetVisual(tile, isSelected, isGoalCell, emptyCellColor, occupiedCellColor, selectedCellColor, goalCellColor);
            }
        }

        private void UpdateLevelTitleText()
        {
            if (levelTitleText == null)
            {
                return;
            }

            levelTitleText.text = $"Level {currentLevelIndex + 1}";
        }

        private void UpdateMoveCounterText()
        {
            if (moveCountText == null || gameRules == null)
            {
                return;
            }

            LevelData levelData = levels[currentLevelIndex];

            if (levelData.MoveLimit > 0)
            {
                moveCountText.text = $"Moves: {gameRules.MovesUsed}/{levelData.MoveLimit}";
            }
            else
            {
                moveCountText.text = $"Moves: {gameRules.MovesUsed}";
            }
        }

        private void UpdateTerminalPopups()
        {
            if (winPopupPanel == null || failPopupPanel == null || gameRules == null || boardState == null)
            {
                return;
            }

            bool hasWon = gameRules.HasWon(boardState);
            bool hasLost = !hasWon && gameRules.HasLost(boardState);

            winPopupPanel.SetActive(hasWon);
            failPopupPanel.SetActive(hasLost);
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static GameObject CreatePopupPanel(Transform parent, string name, string title, bool startHidden)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, 320f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.82f);

            Text titleText = CreateCenteredChildText(panelObject.transform, "Title", title, new Vector2(0f, 64f), new Vector2(680f, 72f), 44);
            titleText.fontStyle = FontStyle.Bold;

            panelObject.SetActive(!startHidden);
            return panelObject;
        }

        private static Text CreateCenteredChildText(Transform parent, string name, string content, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static void CreatePopupButton(Transform parent, string name, string label, Vector2 anchoredPosition, Action onPressed)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = new Vector2(160f, 56f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.20f, 0.40f, 0.62f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onPressed?.Invoke());

            Text labelText = CreateCenteredChildText(buttonObject.transform, "Label", label, Vector2.zero, new Vector2(150f, 46f), 24);
            labelText.fontStyle = FontStyle.Bold;
        }
    }
}
