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

            EnsureTileLabel();
        }

        /// <summary>
        /// Applies placeholder visuals for tile state, selection state, and goal highlighting.
        /// </summary>
        public void SetVisual(
            Tile tile,
            bool isSelected,
            bool isGoalCell,
            Color emptyCellColor,
            Color occupiedCellColor,
            Color selectedCellColor,
            Color goalCellColor)
        {
            bool hasTile = tile != null;

            tileLabelText.text = hasTile ? tile.Id : string.Empty;

            Color cellColor = hasTile ? occupiedCellColor : emptyCellColor;

            if (isGoalCell)
            {
                cellColor = goalCellColor;
            }

            if (isSelected)
            {
                cellColor = selectedCellColor;
            }

            backgroundImage.color = cellColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            clickHandler?.Invoke(position);
        }

        private void EnsureTileLabel()
        {
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
            tileLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tileLabelText.fontSize = 24;
            tileLabelText.alignment = TextAnchor.MiddleCenter;
            tileLabelText.color = Color.white;
        }
    }
}
