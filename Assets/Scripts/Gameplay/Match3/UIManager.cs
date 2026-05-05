using System;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Creates and updates the lightweight UI used by the match-3 prototype.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        private PrototypeTheme theme;
        private BoardManager boardManager;
        private Canvas canvas;
        private Text scoreText;
        private Text movesText;
        private Text targetText;
        private Text statusText;
        private GameObject endGamePanel;
        private Text endGameTitleText;

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

        public void UpdateHud(int score, int moves, int target)
        {
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
            if (endGamePanel == null)
            {
                return;
            }

            endGamePanel.SetActive(true);
            endGameTitleText.text = won ? "Level Complete" : "Game Over";
            SetStatus(won ? "Target score reached." : "No moves left.");
        }

        public void HideEndGame()
        {
            if (endGamePanel != null)
            {
                endGamePanel.SetActive(false);
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
            if (scoreText == null)
            {
                scoreText = CreateTopText("ScoreText", new Vector2(-300f, -26f), new Vector2(260f, 54f), 28);
            }

            if (movesText == null)
            {
                movesText = CreateTopText("MovesText", new Vector2(0f, -26f), new Vector2(260f, 54f), 28);
            }

            if (targetText == null)
            {
                targetText = CreateTopText("TargetText", new Vector2(300f, -26f), new Vector2(260f, 54f), 28);
            }

            if (statusText == null)
            {
                statusText = CreateTopText("StatusText", new Vector2(0f, -88f), new Vector2(840f, 54f), 24);
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
            rect.sizeDelta = new Vector2(760f, 320f);

            Image image = panelObject.GetComponent<Image>();
            image.sprite = theme != null ? theme.PanelSprite : null;
            image.color = theme != null ? theme.PanelColor : new Color(0f, 0f, 0f, 0.86f);

            endGameTitleText = CreateCenteredText(panelObject.transform, "EndGameTitle", "Level Complete", new Vector2(0f, 66f), new Vector2(680f, 72f), 44);
            endGameTitleText.fontStyle = FontStyle.Bold;

            CreateButton(panelObject.transform, "RestartButton", "Restart", new Vector2(-100f, -86f), theme != null ? theme.RetryIconSprite : null, () => boardManager.StartNewGame());
            CreateButton(panelObject.transform, "MenuButton", "Menu", new Vector2(110f, -86f), theme != null ? theme.MenuIconSprite : null, () => SceneManager.LoadScene(MainMenuSceneName));

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

        private void CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Sprite iconSprite, Action onPressed)
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
            button.onClick.AddListener(() => onPressed?.Invoke());

            if (iconSprite != null)
            {
                CreateIcon(buttonObject.transform, iconSprite);
            }

            Text text = CreateCenteredText(buttonObject.transform, "Label", label, iconSprite != null ? new Vector2(24f, 0f) : Vector2.zero, new Vector2(130f, 46f), 24);
            text.fontStyle = FontStyle.Bold;
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
    }
}
