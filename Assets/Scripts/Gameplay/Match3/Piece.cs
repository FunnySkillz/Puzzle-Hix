using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Runtime view and input endpoint for one match-3 piece.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class Piece : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler
    {
        private BoardManager owner;
        private RectTransform rectTransform;
        private Image image;
        private Text label;
        private Vector2 dragStartPosition;
        private Vector3 baseScale = Vector3.one;

        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public PieceType Type { get; private set; }
        public bool IsSelected { get; private set; }

        public void Initialize(BoardManager boardManager, int gridX, int gridY, PieceType pieceType, Sprite sprite, Color color)
        {
            owner = boardManager;
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();

            GridX = gridX;
            GridY = gridY;
            Type = pieceType;

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;

            EnsureLabel();
            label.text = ResolveLabel(pieceType);
            label.color = Color.white;

            SetSelected(false);
        }

        public void SetGridPosition(int gridX, int gridY)
        {
            GridX = gridX;
            GridY = gridY;
        }

        public void SetType(PieceType pieceType, Sprite sprite, Color color)
        {
            Type = pieceType;
            image.sprite = sprite;
            image.color = color;
            EnsureLabel();
            label.text = ResolveLabel(pieceType);
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            RectTransform rect = RectTransform;

            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
            }
        }

        public IEnumerator AnimateTo(Vector2 anchoredPosition, float duration)
        {
            RectTransform rect = RectTransform;

            if (rect == null)
            {
                yield break;
            }

            Vector2 start = rect.anchoredPosition;

            if (duration <= 0f)
            {
                rect.anchoredPosition = anchoredPosition;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(start, anchoredPosition, Smooth(t));
                yield return null;
            }

            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
            }
        }

        public IEnumerator AnimateClear(float duration)
        {
            RectTransform rect = RectTransform;

            if (rect == null)
            {
                yield break;
            }

            Vector3 startScale = rect.localScale;

            if (duration <= 0f)
            {
                rect.localScale = Vector3.zero;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.Lerp(startScale, Vector3.zero, Smooth(t));
                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = Vector3.zero;
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            RectTransform rect = RectTransform;

            if (rect != null)
            {
                rect.localScale = selected ? baseScale * 1.12f : baseScale;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.HandlePieceClicked(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragStartPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.HandlePieceDrag(this, eventData.position - dragStartPosition);
        }

        private RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }

                return rectTransform;
            }
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }

        private static string ResolveLabel(PieceType pieceType)
        {
            return pieceType.ToString().Substring(0, 1);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - (2f * value));
        }
    }
}
