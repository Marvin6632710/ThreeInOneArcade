using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Arcade
{
    /// <summary>
    /// Shared menu shell. Original class scenes remain independent; only this
    /// controller and its UI survive a scene change.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class ArcadeApp : MonoBehaviour
    {
        public static ArcadeApp Instance { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsTransitioning { get; private set; }
        public bool IsMainMenu => SceneManager.GetActiveScene().path == GameCatalog.MenuScene;
        public int CurrentGame { get; private set; } = -1;
        public GameObject MainMenuRoot { get; private set; }
        public GameObject PauseRoot { get; private set; }
        public Button[] GameButtons { get; } = new Button[3];
        public Button ExitButton { get; private set; }
        public Button ResumeButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button BackButton { get; private set; }

        private readonly List<Behaviour> pausedBehaviours = new List<Behaviour>();
        private GameObject hudRoot;
        private GameObject gameOverRoot;
        private GameObject loadingRoot;
        private Text hudTitle;
        private Text hudControls;
        private Font font;
        private static readonly Color Ink = new Color32(12, 19, 31, 255);
        private static readonly Color Surface = new Color32(24, 35, 51, 255);
        private static readonly Color Accent = new Color32(202, 244, 103, 255);
        private static readonly Color Muted = new Color32(162, 180, 199, 255);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null) new GameObject("Arcade App").AddComponent<ArcadeApp>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildInterface();
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (Instance != this) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Physics.gravity = Vector3.down * 9.81f;
            Instance = null;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A balloon run changes global gravity. Never leak it into the arena.
            Physics.gravity = Vector3.down * 9.81f;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            pausedBehaviours.Clear();
            IsPaused = false;
            IsTransitioning = false;
            CurrentGame = System.Array.IndexOf(GameCatalog.Scenes, scene.path);
            MainMenuRoot.SetActive(IsMainMenu);
            PauseRoot.SetActive(false);
            hudRoot.SetActive(!IsMainMenu && CurrentGame >= 0);
            loadingRoot.SetActive(false);
            gameOverRoot.SetActive(false);
            if (CurrentGame >= 0) {
                hudTitle.text = GameCatalog.Titles[CurrentGame].ToUpperInvariant();
                hudControls.text = GameCatalog.Controls[CurrentGame];
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Select(IsMainMenu ? GameButtons[0] : null);
        }

        private void Update()
        {
            if (!IsTransitioning && (IsMainMenu || IsPaused)) UpdateMenuSelection();
            if (!IsTransitioning && !IsMainMenu && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
                if (IsPaused) ResumeGame(); else PauseGame();
            }
            if (CurrentGame == 1 && !IsMainMenu && !IsTransitioning) {
                var balloon = FindFirstObjectByType<Games.Balloon.PlayerControllerX>();
                gameOverRoot.SetActive(balloon != null && balloon.gameOver && !IsPaused);
            }
        }

        private void UpdateMenuSelection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || EventSystem.current == null) return;
            int direction = keyboard.downArrowKey.wasPressedThisFrame ? 1 : keyboard.upArrowKey.wasPressedThisFrame ? -1 : 0;
            if (direction == 0) return;
            Button[] buttons = IsMainMenu ? new[] { GameButtons[0], GameButtons[1], GameButtons[2], ExitButton } : new[] { ResumeButton, RestartButton, BackButton };
            int selected = System.Array.FindIndex(buttons, b => b.gameObject == EventSystem.current.currentSelectedGameObject);
            int next = selected < 0 ? 0 : (selected + direction + buttons.Length) % buttons.Length;
            Select(buttons[next]);
        }

        public void PauseGame()
        {
            if (IsMainMenu || IsPaused || IsTransitioning) return;
            IsPaused = true;
            SuspendGameplay();
            Time.timeScale = 0f;
            AudioListener.pause = true;
            PauseRoot.SetActive(true);
            gameOverRoot.SetActive(false);
            Select(ResumeButton);
        }

        public void ResumeGame()
        {
            if (!IsPaused || IsTransitioning) return;
            foreach (var behaviour in pausedBehaviours) if (behaviour != null) behaviour.enabled = true;
            pausedBehaviours.Clear();
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            PauseRoot.SetActive(false);
            Select(null);
        }

        private void SuspendGameplay()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) {
                if (behaviour.enabled && behaviour.GetType().Namespace != null && behaviour.GetType().Namespace.StartsWith("Arcade.Games.")) {
                    pausedBehaviours.Add(behaviour);
                    behaviour.enabled = false;
                }
            }
        }

        public void PlayGame(int index)
        {
            if (index < 0 || index >= GameCatalog.Scenes.Length) return;
            Load(GameCatalog.Scenes[index]);
        }
        public void RestartGame() { if (CurrentGame >= 0) Load(GameCatalog.Scenes[CurrentGame]); }
        public void BackToMainMenu() { Load(GameCatalog.MenuScene); }

        private void Load(string scene)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            SuspendGameplay();
            Time.timeScale = 0f;
            AudioListener.pause = true;
            loadingRoot.SetActive(true);
            Select(null);
            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }

        public void ExitGame()
        {
            Debug.Log("ARCADE_EXIT_REQUESTED");
            Time.timeScale = 1f;
            AudioListener.pause = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Select(Selectable target)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (target != null) target.Select();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Arcade UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("Arcade Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            // Read discrete arrow presses ourselves, including a press/release
            // within one frame. Prevent the default move action double-stepping.
            events.GetComponent<InputSystemUIInputModule>().move = null;
            var parent = canvasObject.transform;

            MainMenuRoot = Panel("Main Menu", parent, Ink);
            Box("Top rule", MainMenuRoot.transform, new Rect(64, 50, 1152, 2), new Color32(61, 79, 98, 255));
            Label("Edition", MainMenuRoot.transform, "CLASSROOM COLLECTION  /  2026", new Rect(64, 62, 600, 24), 14, Muted);
            Label("Index", MainMenuRoot.transform, "03 GAMES · 01 ARCADE", new Rect(910, 62, 306, 24), 14, Muted, TextAnchor.MiddleRight);
            Label("Title", MainMenuRoot.transform, GameCatalog.Title, new Rect(180, 111, 920, 90), 72, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label("Subtitle", MainMenuRoot.transform, "PICK YOUR NEXT PLAY.", new Rect(180, 203, 920, 28), 18, Accent, TextAnchor.MiddleCenter);
            for (int i = 0; i < 3; i++) {
                int game = i;
                GameButtons[i] = Card("Game " + (i + 1), MainMenuRoot.transform, GameCatalog.Titles[i], GameCatalog.Descriptions[i], (i + 1).ToString("00"), new Rect(350, 268 + i * 88, 580, 72), () => PlayGame(game), false);
            }
            ExitButton = Card("Exit", MainMenuRoot.transform, "Exit", "CLOSE THE ARCADE", "×", new Rect(350, 544, 580, 64), ExitGame, false);
            Label("Menu help", MainMenuRoot.transform, "MOUSE  select     ·     ↑ / ↓  navigate     ·     ENTER  play", new Rect(64, 662, 760, 28), 14, Muted);
            Label("Author", MainMenuRoot.transform, "By " + GameCatalog.Author + "  ·  " + GameCatalog.StudentId, new Rect(700, 662, 516, 28), 15, Color.white, TextAnchor.MiddleRight);

            hudRoot = Panel("Game HUD", parent, Color.clear, false);
            Box("HUD shade", hudRoot.transform, new Rect(0, 0, 1280, 76), new Color32(12, 19, 31, 220));
            hudTitle = Label("Game title", hudRoot.transform, "", new Rect(26, 9, 650, 28), 20, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            hudControls = Label("Controls", hudRoot.transform, "", new Rect(26, 38, 1050, 24), 15, Color.white);
            Card("Pause", hudRoot.transform, "Pause  [ESC]", "", "", new Rect(1080, 15, 174, 44), PauseGame, true);

            gameOverRoot = Panel("Game Over Hint", parent, Color.clear, false);
            Box("Game over backdrop", gameOverRoot.transform, new Rect(300, 604, 680, 70), new Color32(12, 19, 31, 235));
            Label("Game over message", gameOverRoot.transform, "GAME OVER  ·  Press ESC, then Restart to play again", new Rect(312, 613, 656, 50), 21, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            PauseRoot = Panel("In-Game Menu", parent, new Color32(7, 13, 23, 235));
            Box("Pause accent", PauseRoot.transform, new Rect(603, 116, 74, 5), Accent);
            Label("Paused", PauseRoot.transform, "PAUSED", new Rect(240, 149, 800, 95), 70, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label("Pause description", PauseRoot.transform, "TAKE A BREATHER. YOUR GAME IS RIGHT HERE.", new Rect(210, 242, 860, 30), 15, Muted, TextAnchor.MiddleCenter);
            ResumeButton = Card("Resume", PauseRoot.transform, "Resume", "CONTINUE THIS RUN", "↗", new Rect(380, 308, 520, 72), ResumeGame, true);
            RestartButton = Card("Restart", PauseRoot.transform, "Restart", "START THIS GAME FRESH", "↻", new Rect(380, 396, 520, 72), RestartGame, false);
            BackButton = Card("Back to Main Menu", PauseRoot.transform, "Back to Main Menu", "CHOOSE ANOTHER GAME", "←", new Rect(380, 484, 520, 72), BackToMainMenu, false);
            Label("Pause help", PauseRoot.transform, "ESC  resume     ·     ↑ / ↓  navigate     ·     ENTER  select", new Rect(240, 622, 800, 30), 15, Muted, TextAnchor.MiddleCenter);

            loadingRoot = Panel("Loading", parent, Ink);
            Label("Loading label", loadingRoot.transform, "LOADING YOUR NEXT PLAY…", new Rect(240, 300, 800, 100), 30, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            MainMenuRoot.SetActive(false); PauseRoot.SetActive(false); hudRoot.SetActive(false); gameOverRoot.SetActive(false); loadingRoot.SetActive(false);
        }

        private GameObject Panel(string name, Transform parent, Color color, bool block = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = block;
            return go;
        }

        private RectTransform Position(GameObject go, Transform parent, Rect bounds)
        {
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x - 640, 360 - bounds.y);
            rect.sizeDelta = bounds.size;
            return rect;
        }

        private Image Box(string name, Transform parent, Rect bounds, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Position(go, parent, bounds);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        private Text Label(string name, Transform parent, string value, Rect bounds, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            Position(go, parent, bounds);
            var text = go.GetComponent<Text>(); text.font = font; text.text = value;
            text.fontSize = size; text.color = color; text.alignment = alignment; text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Button Card(string name, Transform parent, string title, string subtitle, string number, Rect bounds, UnityEngine.Events.UnityAction action, bool primary)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Position(go, parent, bounds);
            var image = go.GetComponent<Image>(); image.color = primary ? Accent : Surface;
            var button = go.GetComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color32(222, 239, 196, 255);
            colors.selectedColor = new Color32(216, 233, 193, 255); colors.pressedColor = new Color32(153, 185, 125, 255);
            colors.fadeDuration = .08f; button.colors = colors;
            button.onClick.AddListener(action);
            Color titleColor = primary ? Ink : Color.white;
            if (subtitle.Length == 0) LocalLabel("Label", go.transform, title, new Rect(14, 2, bounds.width - 28, bounds.height - 4), 18, titleColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            else {
                LocalLabel("Number", go.transform, number, new Rect(20, 3, 46, bounds.height - 6), 25, primary ? Ink : Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                LocalLabel("Label", go.transform, title, new Rect(84, 8, bounds.width - 104, 29), 24, titleColor, TextAnchor.MiddleLeft, FontStyle.Bold);
                LocalLabel("Description", go.transform, subtitle, new Rect(84, 36, bounds.width - 104, 23), 12, primary ? new Color32(55, 74, 37, 255) : Muted);
            }
            return button;
        }

        private Text LocalLabel(string name, Transform parent, string value, Rect bounds, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var text = Label(name, parent, value, bounds, size, color, alignment, style);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0, 1);
            text.rectTransform.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            return text;
        }
    }
}
