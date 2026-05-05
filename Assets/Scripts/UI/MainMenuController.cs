using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleDungeon.UI
{
    /// <summary>
    /// Creates a minimal main menu UI and routes Start/Quit actions.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuController : MonoBehaviour
    {
        private const string GameplaySceneName = "PuzzleBoard";

        [SerializeField] private PrototypeTheme theme;

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
            EnsureEventSystemWithInputSystem();
            EnsureMenuUi();
        }

        /// <summary>
        /// Loads the playable puzzle board scene.
        /// </summary>
        public void OnStartGame()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }

        /// <summary>
        /// Quits the application. In editor, this stops play mode.
        /// </summary>
        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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

        private void EnsureMenuUi()
        {
            Canvas canvas = FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            CreateBackground(canvas.transform);
            CreateTitle(canvas.transform, "PuzzleDungeon");
            CreateButton(canvas.transform, "StartButton", "Start Game", new Vector2(0f, -70f), ActiveTheme != null ? ActiveTheme.PlayIconSprite : null, OnStartGame);
            CreateButton(canvas.transform, "QuitButton", "Quit", new Vector2(0f, -162f), ActiveTheme != null ? ActiveTheme.MenuIconSprite : null, OnQuitPressed);
        }

        private void CreateBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("CanvasBackground", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = backgroundObject.GetComponent<Image>();
            image.color = ActiveTheme != null ? ActiveTheme.CanvasBackgroundColor : new Color(0.09f, 0.10f, 0.12f, 1f);
            image.raycastTarget = false;
        }

        private void CreateTitle(Transform parent, string title)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObject.transform.SetParent(parent, false);

            RectTransform rect = titleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 140f);
            rect.sizeDelta = new Vector2(900f, 120f);

            Text text = titleObject.GetComponent<Text>();
            text.text = title;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ActiveTheme != null ? ActiveTheme.TextColor : Color.white;
        }

        private void CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Sprite iconSprite, UnityEngine.Events.UnityAction onPressed)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = new Vector2(330f, 74f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = ActiveTheme != null ? ActiveTheme.ButtonSprite : null;
            buttonImage.color = ActiveTheme != null ? ActiveTheme.ButtonColor : new Color(0.20f, 0.40f, 0.62f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onPressed);

            if (iconSprite != null)
            {
                CreateIcon(buttonObject.transform, iconSprite);
            }

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = iconSprite != null ? new Vector2(54f, 0f) : Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text labelText = labelObject.GetComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 30;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = ActiveTheme != null ? ActiveTheme.TextColor : Color.white;
            labelText.raycastTarget = false;
        }

        private static void CreateIcon(Transform parent, Sprite iconSprite)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(54f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
        }
    }
}
