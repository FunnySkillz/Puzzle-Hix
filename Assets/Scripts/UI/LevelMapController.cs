using PuzzleDungeon.Gameplay.Match3;
using PuzzleDungeon.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleDungeon.UI
{
    [DisallowMultipleComponent]
    public class LevelMapController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string PuzzleBoardSceneName = "PuzzleBoard";

        [SerializeField] private PrototypeTheme theme;
        [SerializeField] private Match3LevelCatalog levelCatalog;

        private Canvas canvas;
        private RectTransform nodeRoot;
        private Text playerLevelText;
        private Text xpText;
        private Text totalStarsText;
        private Match3ProgressService progressService;
        private LevelMapNodeState[] nodeStates = new LevelMapNodeState[0];

        public LevelMapNodeState[] NodeStates => nodeStates;

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

        private Match3LevelCatalog ActiveCatalog
        {
            get
            {
                if (levelCatalog == null)
                {
                    levelCatalog = Match3LevelCatalog.LoadDefault();
                }

                return levelCatalog;
            }
        }

        private void Awake()
        {
            progressService = new Match3ProgressService(new SaveService());
            EnsureEventSystemWithInputSystem();
            EnsureCanvas();
            EnsureHeader();
            RefreshMap();
        }

        public void StartLevel(int levelIndex)
        {
            Match3LevelCatalog catalog = ActiveCatalog;
            int levelCount = catalog != null ? catalog.LevelCount : 0;

            if (!progressService.SelectLevel(levelIndex, levelCount))
            {
                AudioService.PlayGlobal(AudioCue.InvalidSwap);
                return;
            }

            AudioService.PlayGlobal(AudioCue.ButtonClick);
            SceneManager.LoadScene(PuzzleBoardSceneName);
        }

        public void BackToMenu()
        {
            AudioService.PlayGlobal(AudioCue.ButtonClick);
            SceneManager.LoadScene(MainMenuSceneName);
        }

        public void ResetProgressForDebug()
        {
            progressService.ResetAll(ActiveCatalog);
            AudioService.PlayGlobal(AudioCue.ButtonClick);
            RefreshMap();
        }

        public LevelMapNodeState GetNodeState(int levelIndex)
        {
            if (nodeStates == null || levelIndex < 0 || levelIndex >= nodeStates.Length)
            {
                return null;
            }

            return nodeStates[levelIndex];
        }

        public void RefreshMap()
        {
            Match3LevelCatalog catalog = ActiveCatalog;

            if (catalog == null)
            {
                return;
            }

            PlayerProgressData progress = progressService.LoadProgress(catalog.LevelCount);
            nodeStates = progressService.BuildLevelMap(catalog);
            UpdateHeader(progress);
            RebuildNodes();
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
                GameObject canvasObject = new GameObject("LevelMapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
                image.color = ActiveTheme != null ? ActiveTheme.CanvasBackgroundColor : new Color(0.08f, 0.10f, 0.13f, 1f);
                image.raycastTarget = false;
            }
        }

        private void EnsureHeader()
        {
            CreateText("LevelMapTitle", "PuzzleDungeon", new Vector2(0f, -40f), new Vector2(900f, 70f), 44, TextAnchor.MiddleCenter);
            playerLevelText = CreateText("PlayerLevelText", "", new Vector2(-330f, -118f), new Vector2(260f, 42f), 25, TextAnchor.MiddleLeft);
            xpText = CreateText("XpText", "", new Vector2(0f, -118f), new Vector2(360f, 42f), 25, TextAnchor.MiddleCenter);
            totalStarsText = CreateText("TotalStarsText", "", new Vector2(330f, -118f), new Vector2(260f, 42f), 25, TextAnchor.MiddleRight);

            CreateButton("BackButton", "Menu", new Vector2(-320f, -820f), new Vector2(210f, 62f), BackToMenu);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CreateButton("ResetProgressButton", "Reset", new Vector2(320f, -820f), new Vector2(210f, 62f), ResetProgressForDebug);
#endif
        }

        private void UpdateHeader(PlayerProgressData progress)
        {
            if (playerLevelText != null)
            {
                playerLevelText.text = $"Player Lv {progress.PlayerLevel}";
            }

            if (xpText != null)
            {
                xpText.text = $"XP {progress.XpIntoCurrentLevel}/{progress.XpForNextLevel}";
            }

            if (totalStarsText != null)
            {
                totalStarsText.text = $"Stars {progress.TotalStars}";
            }
        }

        private void RebuildNodes()
        {
            if (nodeRoot != null)
            {
                Destroy(nodeRoot.gameObject);
            }

            GameObject rootObject = new GameObject("LevelMapContent", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
            nodeRoot = rootObject.GetComponent<RectTransform>();
            nodeRoot.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRoot.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRoot.pivot = new Vector2(0.5f, 0.5f);
            nodeRoot.anchoredPosition = new Vector2(0f, -80f);
            nodeRoot.sizeDelta = new Vector2(920f, 1120f);

            for (int i = 0; i < nodeStates.Length; i++)
            {
                CreateNode(nodeStates[i]);
            }
        }

        private void CreateNode(LevelMapNodeState state)
        {
            int row = state.LevelIndex / 5;
            int column = state.LevelIndex % 5;
            float x = -360f + (column * 180f);
            float y = 420f - (row * 250f);
            Button button = CreateButton($"LevelNode_{state.LevelNumber:00}", BuildNodeText(state), new Vector2(x, y), new Vector2(132f, 132f), () => StartLevel(state.LevelIndex));
            button.interactable = state.IsUnlocked;

            Image image = button.GetComponent<Image>();

            if (!state.IsUnlocked)
            {
                image.color = new Color(0.25f, 0.27f, 0.30f, 1f);
            }
            else if (state.IsCurrent)
            {
                image.color = new Color(0.96f, 0.72f, 0.24f, 1f);
            }
            else if (state.IsCompleted)
            {
                image.color = new Color(0.24f, 0.58f, 0.42f, 1f);
            }
            else
            {
                image.color = ActiveTheme != null ? ActiveTheme.ButtonColor : new Color(0.20f, 0.40f, 0.62f, 1f);
            }
        }

        private static string BuildNodeText(LevelMapNodeState state)
        {
            if (!state.IsUnlocked)
            {
                return $"{state.LevelNumber}\nLOCK";
            }

            string stars = new string('*', state.Stars) + new string('-', 3 - state.Stars);
            return $"{state.LevelNumber}\n{stars}";
        }

        private Text CreateText(string name, string content, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
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
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = ActiveTheme != null ? ActiveTheme.TextColor : Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onPressed)
        {
            Transform parent = nodeRoot != null && name.StartsWith("LevelNode_") ? nodeRoot : canvas.transform;
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = ActiveTheme != null ? ActiveTheme.ButtonSprite : null;
            image.color = ActiveTheme != null ? ActiveTheme.ButtonColor : new Color(0.20f, 0.40f, 0.62f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onPressed);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text labelText = labelObject.GetComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = name.StartsWith("LevelNode_") ? 24 : 26;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = ActiveTheme != null ? ActiveTheme.TextColor : Color.white;
            labelText.raycastTarget = false;
            return button;
        }
    }
}
