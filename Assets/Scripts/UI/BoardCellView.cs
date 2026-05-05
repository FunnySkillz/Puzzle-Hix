using System;
using PuzzleDungeon.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleDungeon.UI
{
    /// <summary>
    /// Lightweight UI view for one board cell that forwards click/tap input to the scene controller.
    /// </summary>
    public class BoardCellView : MonoBehaviour, IPointerClickHandler
    {
        private Position position;
        private Action<Position> clickHandler;

        private Image backgroundImage;
        private Image goalIconImage;
        private Image tileImage;
        private Text tileLabelText;

        /// <summary>
        /// Configures the board position and click callback for this cell view.
        /// </summary>
        public void Initialize(Position position, Action<Position> onClicked)
        {
            this.position = position;
            clickHandler = onClicked;

            backgroundImage = GetComponent<Image>();

            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }

            backgroundImage.raycastTarget = true;
            EnsureChildVisuals();
        }

        /// <summary>
        /// Applies prototype visuals for tile state, selection state, invalid feedback, and goal highlighting.
        /// </summary>
        public void SetVisual(
            Tile tile,
            bool isSelected,
            bool isGoalCell,
            bool isInvalidCell,
            PrototypeTheme theme,
            Color emptyCellColor,
            Color occupiedCellColor,
            Color selectedCellColor,
            Color goalCellColor,
            Color invalidCellColor)
        {
            bool hasTile = tile != null;
            Color cellColor = emptyCellColor;
            Sprite cellSprite = theme != null ? theme.EmptyCellSprite : null;

            if (hasTile)
            {
                cellColor = occupiedCellColor;
            }

            if (isGoalCell)
            {
                cellColor = theme != null ? theme.GoalCellColor : goalCellColor;
                cellSprite = theme != null && theme.GoalCellSprite != null ? theme.GoalCellSprite : cellSprite;
            }

            if (isSelected)
            {
                cellColor = theme != null ? theme.SelectedCellColor : selectedCellColor;
                cellSprite = theme != null && theme.SelectedCellSprite != null ? theme.SelectedCellSprite : cellSprite;
            }

            if (isInvalidCell)
            {
                cellColor = theme != null ? theme.InvalidCellColor : invalidCellColor;
                cellSprite = theme != null && theme.InvalidCellSprite != null ? theme.InvalidCellSprite : cellSprite;
            }

            backgroundImage.sprite = cellSprite;
            backgroundImage.color = cellColor;

            bool showTileSprite = hasTile && theme != null && theme.TileSprite != null;
            tileImage.enabled = showTileSprite;
            tileImage.sprite = showTileSprite ? theme.TileSprite : null;
            tileImage.color = showTileSprite ? theme.GetTileColor(tile) : Color.clear;

            bool showGoalIcon = isGoalCell && !hasTile && theme != null && theme.GoalIconSprite != null;
            goalIconImage.enabled = showGoalIcon;
            goalIconImage.sprite = showGoalIcon ? theme.GoalIconSprite : null;
            goalIconImage.color = showGoalIcon ? new Color(1f, 1f, 1f, 0.78f) : Color.clear;

            tileLabelText.text = hasTile ? ResolveTileLabel(tile) : string.Empty;
            tileLabelText.color = theme != null ? theme.TileLabelColor : Color.white;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            clickHandler?.Invoke(position);
        }

        private static string ResolveTileLabel(Tile tile)
        {
            if (tile == null || string.IsNullOrEmpty(tile.Id))
            {
                return string.Empty;
            }

            return tile.Id == "goal" ? "G" : tile.Id.ToUpperInvariant();
        }

        private void EnsureChildVisuals()
        {
            if (goalIconImage == null)
            {
                goalIconImage = CreateChildImage("GoalIcon", new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f));
            }

            if (tileImage == null)
            {
                tileImage = CreateChildImage("TileSprite", new Vector2(0.11f, 0.11f), new Vector2(0.89f, 0.89f));
            }

            if (tileLabelText != null)
            {
                return;
            }

            GameObject textObject = new GameObject("TileLabel", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            tileLabelText = textObject.GetComponent<Text>();
            tileLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tileLabelText.fontSize = 24;
            tileLabelText.fontStyle = FontStyle.Bold;
            tileLabelText.alignment = TextAnchor.MiddleCenter;
            tileLabelText.color = Color.white;
            tileLabelText.raycastTarget = false;
        }

        private Image CreateChildImage(string childName, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject imageObject = new GameObject(childName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(transform, false);

            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = anchorMin;
            imageRect.anchorMax = anchorMax;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }
    }
}
