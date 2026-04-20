using System.Collections.Generic;
using PuzzleDungeon.Core;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay
{
    /// <summary>
    /// Scene-facing controller for loading a level, rendering a placeholder board, and handling player move input.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameController : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelData levelData;

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

        private BoardState boardState;
        private GameRules gameRules;
        private Position? selectedPosition;

        private GridLayoutGroup boardGrid;
        private Text moveCountText;

        private void Awake()
        {
            if (levelData == null)
            {
                Debug.LogError("GameController requires a LevelData asset reference.");
                enabled = false;
                return;
            }

            // Level loading remains data-driven and independent from scene objects.
            boardState = levelLoader.Load(levelData);
            gameRules = new GameRules(levelData.MoveLimit, new Position(levelData.GoalX, levelData.GoalY), levelData.GoalTileId);
            gameRules.ResetMoveCounter();
            selectedPosition = null;

            EnsureEventSystemWithInputSystem();
            EnsureUiRoots();
            BuildBoardVisuals();
            RefreshBoardVisuals();
            UpdateMoveCounterText();
        }

        /// <summary>
        /// Receives cell click/tap events and routes move attempts through GameRules.
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

            Position currentSelection = selectedPosition.Value;

            if (currentSelection == clickedPosition)
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

            // Move execution is delegated to GameRules so UI code does not alter board state directly.
            MoveResult moveResult = gameRules.TryMove(boardState, currentSelection, clickedPosition);

            if (moveResult.IsSuccess)
            {
                selectedPosition = null;
            }

            RefreshBoardVisuals();
            UpdateMoveCounterText();
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

            if (moveCountText == null)
            {
                GameObject moveTextObject = new GameObject("MoveCountText", typeof(RectTransform), typeof(Text));
                moveTextObject.transform.SetParent(canvas.transform, false);

                RectTransform textRect = moveTextObject.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 1f);
                textRect.anchorMax = new Vector2(0.5f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.anchoredPosition = new Vector2(0f, -40f);
                textRect.sizeDelta = new Vector2(500f, 80f);

                moveCountText = moveTextObject.GetComponent<Text>();
                moveCountText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                moveCountText.fontSize = 36;
                moveCountText.alignment = TextAnchor.MiddleCenter;
                moveCountText.color = Color.white;
            }

            if (boardGrid == null)
            {
                GameObject boardObject = new GameObject("BoardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                boardObject.transform.SetParent(canvas.transform, false);

                RectTransform boardRect = boardObject.GetComponent<RectTransform>();
                boardRect.anchorMin = new Vector2(0.5f, 0.5f);
                boardRect.anchorMax = new Vector2(0.5f, 0.5f);
                boardRect.pivot = new Vector2(0.5f, 0.5f);
                boardRect.anchoredPosition = new Vector2(0f, -30f);

                boardGrid = boardObject.GetComponent<GridLayoutGroup>();
                boardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                boardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                boardGrid.childAlignment = TextAnchor.MiddleCenter;
                boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            }
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

            // Build from top row to bottom so visual grid aligns with board coordinates.
            for (int y = boardState.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < boardState.Width; x++)
                {
                    Position cellPosition = new Position(x, y);
                    GameObject cellObject = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(BoardCellView));
                    cellObject.transform.SetParent(boardGrid.transform, false);

                    BoardCellView cellView = cellObject.GetComponent<BoardCellView>();
                    cellView.Initialize(cellPosition, HandleCellInteraction);

                    boardCellViews[cellPosition] = cellView;
                }
            }
        }

        private void RefreshBoardVisuals()
        {
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

        private void UpdateMoveCounterText()
        {
            if (moveCountText == null)
            {
                return;
            }

            string stateSuffix = string.Empty;

            if (gameRules.HasWon(boardState))
            {
                stateSuffix = " - WIN";
            }
            else if (gameRules.HasLost(boardState))
            {
                stateSuffix = " - FAIL";
            }

            if (levelData.MoveLimit > 0)
            {
                moveCountText.text = $"Moves: {gameRules.MovesUsed}/{levelData.MoveLimit}{stateSuffix}";
            }
            else
            {
                moveCountText.text = $"Moves: {gameRules.MovesUsed}{stateSuffix}";
            }
        }
    }
}
