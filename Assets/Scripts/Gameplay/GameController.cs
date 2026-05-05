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
        private const float InvalidFeedbackSeconds = 0.28f;

        [Header("Level Progression")]
        [SerializeField] private LevelData[] levels = Array.Empty<LevelData>();

        [Header("Board View")]
        [SerializeField] private Vector2 cellSize = new Vector2(96f, 96f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(6f, 6f);

        [Header("Theme")]
        [SerializeField] private PrototypeTheme theme;

        [Header("Fallback Colors")]
        [SerializeField] private Color emptyCellColor = new Color(0.18f, 0.20f, 0.24f, 1f);
        [SerializeField] private Color occupiedCellColor = new Color(0.28f, 0.55f, 0.78f, 1f);
        [SerializeField] private Color selectedCellColor = new Color(0.95f, 0.76f, 0.25f, 1f);
        [SerializeField] private Color goalCellColor = new Color(0.30f, 0.70f, 0.40f, 1f);
        [SerializeField] private Color invalidCellColor = new Color(0.90f, 0.18f, 0.16f, 1f);

        private readonly Dictionary<Position, BoardCellView> boardCellViews = new Dictionary<Position, BoardCellView>();
        private readonly LevelLoader levelLoader = new LevelLoader();

        private SaveService saveService;
        private BoardState boardState;
        private GameRules gameRules;
        private Position? selectedPosition;
        private Position? invalidFeedbackPosition;
        private int currentLevelIndex;

        private GridLayoutGroup boardGrid;
        private Image canvasBackgroundImage;
        private Text moveCountText;
        private Text levelTitleText;
        private Text statusText;
        private GameObject winPopupPanel;
        private GameObject failPopupPanel;

        private PrototypeTheme ActiveTheme
        {
            get
            {
                if (theme == null)
                {
                    theme = PrototypeTheme.LoadDefault();
                }

                return theme;
            }
        }

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

            LoadLevel(ResolveStartupLevelIndex(), true);
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
                LogInteractionBlockedReason();
                UpdateMoveCounterText();
                UpdateTerminalPopups();
                return;
            }

            if (!selectedPosition.HasValue)
            {
                if (!boardState.IsCellEmpty(clickedPosition))
                {
                    selectedPosition = clickedPosition;
                    invalidFeedbackPosition = null;
                    SetStatus("Choose an adjacent empty square.");
                    RefreshBoardVisuals();
                    return;
                }

                ShowInvalidSelection(clickedPosition, "Pick a tile first.");
                return;
            }

            Position source = selectedPosition.Value;

            if (source == clickedPosition)
            {
                selectedPosition = null;
                invalidFeedbackPosition = null;
                SetStatus("Selection cleared.");
                RefreshBoardVisuals();
                return;
            }

            MoveResult result = gameRules.TryMove(boardState, source, clickedPosition);

            if (!result.IsSuccess)
            {
                ShowInvalidMove(source, clickedPosition, result);
                UpdateMoveCounterText();
                UpdateTerminalPopups();
                return;
            }

            selectedPosition = null;
            invalidFeedbackPosition = null;
            LogSuccessfulMove(source, clickedPosition, result);

            if (result.IsWin)
            {
                saveService.SetBestMoveCount(levels[currentLevelIndex].LevelId, result.MovesUsed);
                saveService.SetUnlockedLevelIndex(currentLevelIndex + 1);
                saveService.Flush();
                SetStatus("Level complete!");
            }
            else if (result.IsFail)
            {
                SetStatus("No moves left.");
            }
            else
            {
                SetStatus("Nice slide.");
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
            invalidFeedbackPosition = null;
            CancelInvoke(nameof(ClearInvalidFeedback));
            HideTerminalPopups();

            if (persistCurrentLevel)
            {
                saveService.SetCurrentLevelIndex(currentLevelIndex);
                saveService.Flush();
            }

            BuildBoardVisuals();
            RefreshBoardVisuals();
            UpdateLevelTitleText();
            UpdateMoveCounterText();
            SetStatus("Slide the G tile to the target.");
            UpdateTerminalPopups();
        }

        private int ResolveStartupLevelIndex()
        {
            int highestPlayableIndex = levels.Length - 1;
            int savedCurrentLevelIndex = saveService.GetCurrentLevelIndex();
            int savedUnlockedLevelIndex = saveService.GetUnlockedLevelIndex();
            int clampedCurrentLevel = Mathf.Clamp(savedCurrentLevelIndex, 0, highestPlayableIndex);
            int clampedUnlockedLevel = Mathf.Clamp(savedUnlockedLevelIndex, 0, highestPlayableIndex);
            return Mathf.Min(clampedCurrentLevel, clampedUnlockedLevel);
        }

        private void ShowInvalidSelection(Position clickedPosition, string message)
        {
            invalidFeedbackPosition = clickedPosition;
            SetStatus(message);
            RefreshBoardVisuals();
            CancelInvoke(nameof(ClearInvalidFeedback));
            Invoke(nameof(ClearInvalidFeedback), InvalidFeedbackSeconds);
        }

        private void ShowInvalidMove(Position source, Position destination, MoveResult result)
        {
            LogInvalidMoveAttempt(source, destination, result);
            invalidFeedbackPosition = destination;
            SetStatus(result != null ? result.Message : "That slide is blocked.");
            RefreshBoardVisuals();
            CancelInvoke(nameof(ClearInvalidFeedback));
            Invoke(nameof(ClearInvalidFeedback), InvalidFeedbackSeconds);
        }

        private void ClearInvalidFeedback()
        {
            invalidFeedbackPosition = null;
            RefreshBoardVisuals();
        }

        private void LogInteractionBlockedReason()
        {
            if (boardState == null || gameRules == null)
            {
                return;
            }

            if (gameRules.HasWon(boardState))
            {
                Debug.LogWarning("Interaction ignored: level is already completed.");
                SetStatus("Level already complete.");
                return;
            }

            if (gameRules.HasLost(boardState))
            {
                Debug.LogWarning("Interaction ignored: move limit reached.");
                SetStatus("No moves left.");
                return;
            }

            Debug.LogWarning("Interaction ignored: moves are currently blocked by rule state.");
            SetStatus("Move blocked.");
        }

        private static void LogInvalidMoveAttempt(Position source, Position destination, MoveResult result)
        {
            if (result == null)
            {
                Debug.LogWarning($"Invalid move {source} -> {destination}: unknown failure.");
                return;
            }

            Debug.LogWarning($"Invalid move {source} -> {destination}: {result.Message}");
        }

        private static void LogSuccessfulMove(Position source, Position destination, MoveResult result)
        {
            if (result == null)
            {
                Debug.Log($"Move {source} -> {destination} applied.");
                return;
            }

            Debug.Log($"Valid move {source} -> {destination}. Moves used: {result.MovesUsed}. Win={result.IsWin}, Fail={result.IsFail}");
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

            EnsureCanvasBackground(canvas.transform);

            if (levelTitleText == null)
            {
                levelTitleText = CreateText(canvas.transform, "LevelTitleText", new Vector2(0f, -24f), new Vector2(620f, 56f), 30);
            }

            if (moveCountText == null)
            {
                moveCountText = CreateText(canvas.transform, "MoveCountText", new Vector2(0f, -72f), new Vector2(620f, 64f), 34);
            }

            if (statusText == null)
            {
                statusText = CreateText(canvas.transform, "StatusText", new Vector2(0f, -142f), new Vector2(760f, 64f), 24);
            }

            if (boardGrid == null)
            {
                GameObject boardObject = new GameObject("BoardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                boardObject.transform.SetParent(canvas.transform, false);

                RectTransform boardRect = boardObject.GetComponent<RectTransform>();
                boardRect.anchorMin = new Vector2(0.5f, 0.5f);
                boardRect.anchorMax = new Vector2(0.5f, 0.5f);
                boardRect.pivot = new Vector2(0.5f, 0.5f);
                boardRect.anchoredPosition = new Vector2(0f, -70f);

                boardGrid = boardObject.GetComponent<GridLayoutGroup>();
                boardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                boardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                boardGrid.childAlignment = TextAnchor.MiddleCenter;
                boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            }

            if (winPopupPanel == null)
            {
                winPopupPanel = CreatePopupPanel(canvas.transform, "WinPopup", "You Win!", true);
                CreatePopupButton(winPopupPanel.transform, "RetryButton", "Retry", new Vector2(-190f, -82f), ActiveTheme != null ? ActiveTheme.RetryIconSprite : null, OnRetryPressed);
                CreatePopupButton(winPopupPanel.transform, "NextButton", "Next", new Vector2(0f, -82f), ActiveTheme != null ? ActiveTheme.NextIconSprite : null, OnNextLevelPressed);
                CreatePopupButton(winPopupPanel.transform, "MenuButton", "Menu", new Vector2(190f, -82f), ActiveTheme != null ? ActiveTheme.MenuIconSprite : null, OnMenuPressed);
            }

            if (failPopupPanel == null)
            {
                failPopupPanel = CreatePopupPanel(canvas.transform, "FailPopup", "Level Failed", true);
                CreatePopupButton(failPopupPanel.transform, "RetryButton", "Retry", new Vector2(-100f, -82f), ActiveTheme != null ? ActiveTheme.RetryIconSprite : null, OnRetryPressed);
                CreatePopupButton(failPopupPanel.transform, "MenuButton", "Menu", new Vector2(100f, -82f), ActiveTheme != null ? ActiveTheme.MenuIconSprite : null, OnMenuPressed);
            }
        }

        private void EnsureCanvasBackground(Transform canvasTransform)
        {
            if (canvasBackgroundImage != null)
            {
                return;
            }

            GameObject backgroundObject = new GameObject("CanvasBackground", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvasTransform, false);
            backgroundObject.transform.SetAsFirstSibling();

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            canvasBackgroundImage = backgroundObject.GetComponent<Image>();
            canvasBackgroundImage.raycastTarget = false;
            canvasBackgroundImage.color = ActiveTheme != null ? ActiveTheme.CanvasBackgroundColor : new Color(0.09f, 0.10f, 0.12f, 1f);
        }

        private void BuildBoardVisuals()
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
            if (boardState == null || currentLevelIndex < 0 || currentLevelIndex >= levels.Length)
            {
                return;
            }

            LevelData levelData = levels[currentLevelIndex];

            foreach (KeyValuePair<Position, BoardCellView> pair in boardCellViews)
            {
                Position position = pair.Key;
                BoardCellView view = pair.Value;

                Tile tile = boardState.GetTile(position);
                bool isSelected = selectedPosition.HasValue && selectedPosition.Value == position;
                bool isGoalCell = position.X == levelData.GoalX && position.Y == levelData.GoalY;
                bool isInvalidCell = invalidFeedbackPosition.HasValue && invalidFeedbackPosition.Value == position;

                view.SetVisual(tile, isSelected, isGoalCell, isInvalidCell, ActiveTheme, emptyCellColor, occupiedCellColor, selectedCellColor, goalCellColor, invalidCellColor);
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

        private void SetStatus(string message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
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

        private void HideTerminalPopups()
        {
            if (winPopupPanel != null)
            {
                winPopupPanel.SetActive(false);
            }

            if (failPopupPanel != null)
            {
                failPopupPanel.SetActive(false);
            }
        }

        private Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ActiveTheme != null ? ActiveTheme.TextColor : Color.white;
            return text;
        }

        private GameObject CreatePopupPanel(Transform parent, string name, string title, bool startHidden)
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
            panelImage.sprite = ActiveTheme != null ? ActiveTheme.PanelSprite : null;
            panelImage.color = ActiveTheme != null ? ActiveTheme.PanelColor : new Color(0f, 0f, 0f, 0.82f);

            Text titleText = CreateCenteredChildText(panelObject.transform, "Title", title, new Vector2(0f, 64f), new Vector2(680f, 72f), 44);
            titleText.fontStyle = FontStyle.Bold;

            panelObject.SetActive(!startHidden);
            return panelObject;
        }

        private Text CreateCenteredChildText(Transform parent, string name, string content, Vector2 anchoredPosition, Vector2 size, int fontSize)
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ActiveTheme != null ? ActiveTheme.TextColor : Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void CreatePopupButton(Transform parent, string name, string label, Vector2 anchoredPosition, Sprite iconSprite, Action onPressed)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = new Vector2(170f, 58f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = ActiveTheme != null ? ActiveTheme.ButtonSprite : null;
            buttonImage.color = ActiveTheme != null ? ActiveTheme.ButtonColor : new Color(0.20f, 0.40f, 0.62f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onPressed?.Invoke());

            if (iconSprite != null)
            {
                CreateButtonIcon(buttonObject.transform, iconSprite, new Vector2(-54f, 0f));
                Text labelText = CreateCenteredChildText(buttonObject.transform, "Label", label, new Vector2(18f, 0f), new Vector2(112f, 46f), 22);
                labelText.fontStyle = FontStyle.Bold;
            }
            else
            {
                Text labelText = CreateCenteredChildText(buttonObject.transform, "Label", label, Vector2.zero, new Vector2(150f, 46f), 24);
                labelText.fontStyle = FontStyle.Bold;
            }
        }

        private static void CreateButtonIcon(Transform parent, Sprite iconSprite, Vector2 anchoredPosition)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = anchoredPosition;
            iconRect.sizeDelta = new Vector2(32f, 32f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
        }
    }
}
