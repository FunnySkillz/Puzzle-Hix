using System;
using System.Collections;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Creates and updates the lightweight UI used by the match-3 MVP prototype.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        private PrototypeTheme theme;
        private BoardManager boardManager;
        private Canvas canvas;
        private Text levelText;
        private Text scoreText;
        private Text movesText;
        private Text targetText;
        private Text objectiveText;
        private Text statusText;
        private GameObject endGamePanel;
        private GameObject nextButtonObject;
        private Text endGameTitleText;
        private Text endGameStarsText;
        private Text endGameSummaryText;

        public Transform CanvasTransform => canvas != null ? canvas.transform : null;

        public void Initialize(BoardManager board, PrototypeTheme prototypeTheme)
        {
            boardManager = board;
            theme = prototypeTheme;
            EnsureEventSystemWithInputSystem();
            EnsureCanvas();
            EnsureHud();
            HideEndGame();
        }

        public void UpdateHud(int score, int moves, int target, int levelNumber, string objectiveSummary)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {levelNumber}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            if (movesText != null)
            {
                movesText.text = $"Moves: {moves}";
            }

            if (targetText != null)
            {
                targetText.text = $"Target: {target}";
            }

            if (objectiveText != null)
            {
                objectiveText.text = objectiveSummary;
            }
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void ShowEndGame(bool won)
        {
            ShowEndGame(won, null);
        }

        public void ShowEndGame(bool won, LevelResult result)
        {
            if (endGamePanel == null)
            {
                return;
            }

            endGamePanel.SetActive(true);
            endGamePanel.transform.localScale = Vector3.zero;
            endGameTitleText.text = won ? "Level Complete" : "Game Over";
            UpdateEndGameSummary(won, result);

            if (nextButtonObject != null)
            {
                nextButtonObject.SetActive(won && boardManager != null && boardManager.HasNextLevel);
            }

            SetStatus(won ? "Objective complete." : "No moves left.");
            StartCoroutine(ScalePanelIn(endGamePanel.transform));
        }

        public void HideEndGame()
        {
            if (endGamePanel != null)
            {
                endGamePanel.SetActive(false);
            }
        }

        public void ShowFloatingFeedback(string message, Vector2 anchoredPosition, Color color, float duration = 0.75f)
        {
            StartCoroutine(FloatingFeedbackRoutine(message, anchoredPosition, color, duration));
        }

        public void ShowPieceBurst(Vector2 anchoredPosition, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = (Mathf.PI * 2f * i) / 6f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                StartCoroutine(BurstDotRoutine(anchoredPosition, direction, color));
            }
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

        private void EnsureCanvas()
        {
            canvas = FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Match3Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GameObject.Find("CanvasBackground") == null)
            {
                GameObject backgroundObject = new GameObject("CanvasBackground", typeof(RectTransform), typeof(Image));
                backgroundObject.transform.SetParent(canvas.transform, false);
                backgroundObject.transform.SetAsFirstSibling();

                RectTransform rect = backgroundObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = backgroundObject.GetComponent<Image>();
                image.color = theme != null ? theme.CanvasBackgroundColor : new Color(0.09f, 0.10f, 0.12f, 1f);
                image.raycastTarget = false;
            }
        }

        private void EnsureHud()
        {
            if (levelText == null)
            {
                levelText = CreateTopText("LevelText", new Vector2(-405f, -24f), new Vector2(190f, 50f), 26);
            }

            if (scoreText == null)
            {
                scoreText = CreateTopText("ScoreText", new Vector2(-145f, -24f), new Vector2(230f, 50f), 26);
            }

            if (movesText == null)
            {
                movesText = CreateTopText("MovesText", new Vector2(125f, -24f), new Vector2(230f, 50f), 26);
            }

            if (targetText == null)
            {
                targetText = CreateTopText("TargetText", new Vector2(390f, -24f), new Vector2(230f, 50f), 26);
            }

            if (objectiveText == null)
            {
                objectiveText = CreateTopText("ObjectiveText", new Vector2(0f, -78f), new Vector2(920f, 48f), 23);
            }

            if (statusText == null)
            {
                statusText = CreateTopText("StatusText", new Vector2(0f, -124f), new Vector2(920f, 46f), 22);
                statusText.text = "Swap adjacent pieces to match 3 or more.";
            }

            if (endGamePanel == null)
            {
                endGamePanel = CreateEndGamePanel();
            }
        }

        private Text CreateTopText(string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = theme != null ? theme.TextColor : Color.white;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateEndGamePanel()
        {
            GameObject panelObject = new GameObject("EndGamePanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(820f, 340f);

            Image image = panelObject.GetComponent<Image>();
            image.sprite = theme != null ? theme.PanelSprite : null;
            image.color = theme != null ? theme.PanelColor : new Color(0f, 0f, 0f, 0.86f);

            endGameTitleText = CreateCenteredText(panelObject.transform, "EndGameTitle", "Level Complete", new Vector2(0f, 118f), new Vector2(720f, 64f), 42);
            endGameTitleText.fontStyle = FontStyle.Bold;
            endGameStarsText = CreateCenteredText(panelObject.transform, "EndGameStars", "", new Vector2(0f, 50f), new Vector2(720f, 48f), 34);
            endGameStarsText.fontStyle = FontStyle.Bold;
            endGameSummaryText = CreateCenteredText(panelObject.transform, "EndGameSummary", "", new Vector2(0f, -10f), new Vector2(720f, 82f), 24);

            CreateButton(panelObject.transform, "RetryButton", "Retry", new Vector2(-225f, -130f), theme != null ? theme.RetryIconSprite : null, () => boardManager.RetryCurrentLevel());
            nextButtonObject = CreateButton(panelObject.transform, "NextButton", "Next", new Vector2(0f, -130f), theme != null ? theme.NextIconSprite : null, () => boardManager.GoToNextLevel());
            CreateButton(panelObject.transform, "MenuButton", "Map", new Vector2(225f, -130f), theme != null ? theme.MenuIconSprite : null, () => boardManager.ReturnToMenu());

            panelObject.SetActive(false);
            return panelObject;
        }

        private Text CreateCenteredText(Transform parent, string name, string content, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = theme != null ? theme.TextColor : Color.white;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Sprite iconSprite, Action onPressed)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(190f, 62f);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = theme != null ? theme.ButtonSprite : null;
            image.color = theme != null ? theme.ButtonColor : new Color(0.20f, 0.40f, 0.62f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                boardManager?.PlayButtonClick();
                onPressed?.Invoke();
            });

            if (iconSprite != null)
            {
                CreateIcon(buttonObject.transform, iconSprite);
            }

            Text text = CreateCenteredText(buttonObject.transform, "Label", label, iconSprite != null ? new Vector2(24f, 0f) : Vector2.zero, new Vector2(130f, 46f), 24);
            text.fontStyle = FontStyle.Bold;
            return buttonObject;
        }

        private static void CreateIcon(Transform parent, Sprite iconSprite)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-58f, 0f);
            rect.sizeDelta = new Vector2(34f, 34f);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = iconSprite;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private IEnumerator FloatingFeedbackRoutine(string message, Vector2 anchoredPosition, Color color, float duration)
        {
            Text text = CreateCenteredText(canvas.transform, "FloatingFeedback", message, anchoredPosition, new Vector2(260f, 56f), 30);
            text.fontStyle = FontStyle.Bold;
            text.color = color;

            RectTransform rect = text.GetComponent<RectTransform>();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = anchoredPosition + new Vector2(0f, Mathf.Lerp(0f, 80f, t));
                Color faded = color;
                faded.a = 1f - t;
                text.color = faded;
                yield return null;
            }

            Destroy(text.gameObject);
        }

        private void UpdateEndGameSummary(bool won, LevelResult result)
        {
            if (endGameStarsText != null)
            {
                endGameStarsText.text = won && result != null ? BuildStarText(result.Stars) : "---";
                endGameStarsText.color = won ? new Color(1f, 0.84f, 0.28f, 1f) : Color.white;
            }

            if (endGameSummaryText == null)
            {
                return;
            }

            if (won && result != null)
            {
                endGameSummaryText.text = $"Score {result.Score}  |  Moves left {result.MovesRemaining}\nXP +{result.XpAwarded}  |  Best stars {result.Stars}/3";
            }
            else
            {
                endGameSummaryText.text = "Objective not complete.\nRetry the level or return to the map.";
            }
        }

        private static string BuildStarText(int stars)
        {
            int clampedStars = Mathf.Clamp(stars, 0, 3);
            return new string('*', clampedStars) + new string('-', 3 - clampedStars);
        }

        private static IEnumerator ScalePanelIn(Transform panelTransform)
        {
            if (panelTransform == null)
            {
                yield break;
            }

            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (panelTransform == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                panelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t * t * (3f - (2f * t)));
                yield return null;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one;
            }
        }

        private IEnumerator BurstDotRoutine(Vector2 anchoredPosition, Vector2 direction, Color color)
        {
            GameObject dotObject = new GameObject("MatchBurstDot", typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = dotObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(12f, 12f);

            Image image = dotObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = anchoredPosition + (direction * Mathf.Lerp(0f, 56f, t));
                Color faded = color;
                faded.a = 1f - t;
                image.color = faded;
                yield return null;
            }

            Destroy(dotObject);
        }
    }
}
