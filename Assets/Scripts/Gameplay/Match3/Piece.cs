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
        private Outline outline;
        private Shadow shadow;
        private Vector2 dragStartPosition;
        private Vector3 baseScale = Vector3.one;

        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public PieceType Type { get; private set; }
        public SpecialPieceType SpecialPieceType { get; private set; }
        public bool IsSelected { get; private set; }

        public void Initialize(BoardManager boardManager, int gridX, int gridY, PieceType pieceType, SpecialPieceType specialType, Sprite sprite, Color color)
        {
            owner = boardManager;
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();

            GridX = gridX;
            GridY = gridY;
            Type = pieceType;
            SpecialPieceType = specialType;

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;

            EnsureVisualEffects(color);
            EnsureLabel();
            label.text = ResolveLabel(pieceType, specialType);
            label.color = Color.white;

            SetSelected(false);
        }

        public void SetGridPosition(int gridX, int gridY)
        {
            GridX = gridX;
            GridY = gridY;
        }

        public void SetType(PieceType pieceType, SpecialPieceType specialType, Sprite sprite, Color color)
        {
            Type = pieceType;
            SpecialPieceType = specialType;
            image.sprite = sprite;
            image.color = color;
            EnsureVisualEffects(color);
            EnsureLabel();
            label.text = ResolveLabel(pieceType, specialType);
        }

        public void SetSpecialPieceType(SpecialPieceType specialType)
        {
            SpecialPieceType = specialType;
            EnsureLabel();
            label.text = ResolveLabel(Type, SpecialPieceType);
            RectTransform rect = RectTransform;

            if (rect != null)
            {
                rect.localScale = SpecialPieceType == SpecialPieceType.None ? baseScale : baseScale * 1.08f;
            }
        }

        public IEnumerator AnimateSpecialCreated(float duration)
        {
            RectTransform rect = RectTransform;

            if (rect == null)
            {
                yield break;
            }

            Vector3 startScale = baseScale;
            Vector3 peakScale = baseScale * 1.26f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                rect.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = SpecialPieceType == SpecialPieceType.None ? baseScale : baseScale * 1.08f;
            }
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

        public IEnumerator AnimateClear(float duration, float popScale)
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
                if (t < 0.35f)
                {
                    rect.localScale = Vector3.Lerp(startScale, startScale * popScale, Smooth(t / 0.35f));
                }
                else
                {
                    rect.localScale = Vector3.Lerp(startScale * popScale, Vector3.zero, Smooth((t - 0.35f) / 0.65f));
                }

                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = Vector3.zero;
            }
        }

        public IEnumerator AnimateInvalidBounce(Vector2 offset, float duration)
        {
            RectTransform rect = RectTransform;

            if (rect == null)
            {
                yield break;
            }

            Vector2 start = rect.anchoredPosition;
            Vector2 peak = start + offset;
            float halfDuration = Mathf.Max(0.01f, duration * 0.5f);

            yield return AnimateTo(peak, halfDuration);
            yield return AnimateTo(start, halfDuration);
        }

        public IEnumerator AnimateHintPulse(float duration)
        {
            RectTransform rect = RectTransform;

            if (rect == null)
            {
                yield break;
            }

            Vector3 startScale = rect.localScale;
            Vector3 peakScale = startScale * 1.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rect == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                rect.localScale = Vector3.Lerp(startScale, peakScale, wave);
                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = IsSelected ? baseScale * 1.12f : startScale;
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

            if (outline != null)
            {
                outline.effectColor = selected ? Color.white : new Color(0f, 0f, 0f, 0.42f);
                outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
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

        private void EnsureVisualEffects(Color color)
        {
            if (outline == null)
            {
                outline = GetComponent<Outline>();

                if (outline == null)
                {
                    outline = gameObject.AddComponent<Outline>();
                }
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.42f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (shadow == null)
            {
                shadow = GetComponent<Shadow>();

                if (shadow == null)
                {
                    shadow = gameObject.AddComponent<Shadow>();
                }
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(4f, -5f);
        }

        private static string ResolveLabel(PieceType pieceType, SpecialPieceType specialType)
        {
            string typeLabel = pieceType.ToString().Substring(0, 1);

            switch (specialType)
            {
                case SpecialPieceType.LineHorizontal:
                    return $"{typeLabel}-";
                case SpecialPieceType.LineVertical:
                    return $"{typeLabel}|";
                case SpecialPieceType.Bomb:
                    return $"{typeLabel}*";
                case SpecialPieceType.ColorClear:
                    return $"{typeLabel}@";
                default:
                    return typeLabel;
            }
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - (2f * value));
        }
    }
}
