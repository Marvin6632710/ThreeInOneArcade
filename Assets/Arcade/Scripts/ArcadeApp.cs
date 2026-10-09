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
        private Text pauseGameTitle;
        private Image hudAccent;
        private Font font;
        private Sprite roundedSprite;
        private static readonly Color Ink = new Color32(12, 19, 31, 255);
        private static readonly Color Surface = new Color32(24, 35, 51, 255);
        private static readonly Color Accent = new Color32(172, 239, 193, 255);
        private static readonly Color Muted = new Color32(162, 180, 199, 255);
        private static readonly Color[] GameAccents = {
            new Color32(172, 239, 193, 255), new Color32(203, 188, 255, 255), new Color32(153, 212, 255, 255)
        };

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
                hudAccent.color = GameAccents[CurrentGame];
                pauseGameTitle.text = (CurrentGame + 1).ToString("00") + "  /  " + GameCatalog.Titles[CurrentGame].ToUpperInvariant();
                pauseGameTitle.color = GameAccents[CurrentGame];
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
            int direction = keyboard.downArrowKey.wasPressedThisFrame || (IsMainMenu && keyboard.rightArrowKey.wasPressedThisFrame) ? 1
                : keyboard.upArrowKey.wasPressedThisFrame || (IsMainMenu && keyboard.leftArrowKey.wasPressedThisFrame) ? -1 : 0;
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
            var backdrop = new GameObject("Arcade grid", typeof(RectTransform), typeof(ArcadeBackdrop));
            backdrop.transform.SetParent(MainMenuRoot.transform, false);
            var backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero; backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
            backdrop.GetComponent<ArcadeBackdrop>().raycastTarget = false;
            RoundedBox("Brand badge", MainMenuRoot.transform, new Rect(56, 32, 44, 44), Accent);
            Label("Monogram", MainMenuRoot.transform, "A/", new Rect(56, 32, 44, 44), 23, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label("Edition", MainMenuRoot.transform, "THE CLASSROOM COLLECTION", new Rect(116, 36, 420, 36), 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label("Index", MainMenuRoot.transform, "CSX4515  /  FINAL 2026", new Rect(880, 36, 344, 36), 12, Muted, TextAnchor.MiddleRight);
            Label("Title", MainMenuRoot.transform, GameCatalog.Title, new Rect(53, 104, 900, 78), 68, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label("Subtitle", MainMenuRoot.transform, "Three challenges. One place to play.", new Rect(59, 183, 780, 30), 20, Muted);
            Label("Selection label", MainMenuRoot.transform, "CHOOSE YOUR GAME", new Rect(59, 232, 190, 26), 12, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Box("Section rule", MainMenuRoot.transform, new Rect(242, 245, 982, 1), new Color32(46, 63, 82, 255));
            for (int i = 0; i < 3; i++) {
                int game = i;
                GameButtons[i] = GameCard(i, MainMenuRoot.transform, new Rect(56 + i * 396, 272, 376, 332), () => PlayGame(game));
            }
            Box("Footer rule", MainMenuRoot.transform, new Rect(56, 626, 1168, 1), new Color32(46, 63, 82, 255));
            Label("Author caption", MainMenuRoot.transform, "CREATED BY", new Rect(59, 645, 420, 18), 10, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label("Author", MainMenuRoot.transform, GameCatalog.Author + "  /  " + GameCatalog.StudentId, new Rect(59, 666, 450, 24), 16, Color.white);
            Label("Menu help", MainMenuRoot.transform, "ARROW KEYS  navigate   ·   ENTER  select   ·   MOUSE  click", new Rect(492, 650, 550, 36), 12, Muted, TextAnchor.MiddleCenter);
            ExitButton = ActionButton("Exit", MainMenuRoot.transform, "Exit", "", "×", new Rect(1080, 646, 144, 44), ExitGame, false);

            hudRoot = Panel("Game HUD", parent, Color.clear, false);
            RoundedBox("HUD shade", hudRoot.transform, new Rect(24, 20, 750, 68), new Color32(12, 19, 31, 235));
            hudAccent = Box("Game accent", hudRoot.transform, new Rect(24, 34, 3, 40), Accent);
            hudTitle = Label("Game title", hudRoot.transform, "", new Rect(44, 28, 700, 26), 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            hudControls = Label("Controls", hudRoot.transform, "", new Rect(44, 56, 700, 22), 14, Muted);
            ActionButton("Pause", hudRoot.transform, "Pause  [ESC]", "", "", new Rect(1098, 24, 158, 48), PauseGame, false);

            gameOverRoot = Panel("Game Over Hint", parent, Color.clear, false);
            RoundedBox("Game over backdrop", gameOverRoot.transform, new Rect(300, 622, 680, 64), new Color32(12, 19, 31, 240));
            Label("Game over message", gameOverRoot.transform, "GAME OVER  ·  Press ESC, then Restart to play again", new Rect(316, 630, 648, 48), 19, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            PauseRoot = Panel("In-Game Menu", parent, new Color32(5, 10, 19, 168));
            RoundedBox("Panel shadow", PauseRoot.transform, new Rect(330, 104, 620, 532), new Color32(0, 0, 0, 75));
            RoundedBox("Panel border", PauseRoot.transform, new Rect(330, 92, 620, 532), new Color32(58, 76, 97, 255));
            RoundedBox("Pause panel", PauseRoot.transform, new Rect(331, 93, 618, 530), new Color32(17, 27, 43, 255));
            pauseGameTitle = Label("Pause game title", PauseRoot.transform, "", new Rect(382, 119, 516, 24), 12, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label("Paused", PauseRoot.transform, "PAUSED", new Rect(378, 148, 445, 70), 60, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            RoundedBox("Pause symbol left", PauseRoot.transform, new Rect(853, 169, 11, 32), Accent);
            RoundedBox("Pause symbol right", PauseRoot.transform, new Rect(873, 169, 11, 32), Accent);
            Label("Pause description", PauseRoot.transform, "Your run is on hold. Jump back in when you're ready.", new Rect(382, 220, 516, 30), 15, Muted);
            RoundedBox("Paused status", PauseRoot.transform, new Rect(382, 264, 516, 38), new Color32(28, 42, 59, 255));
            Label("Paused status text", PauseRoot.transform, "GAMEPLAY + AUDIO PAUSED", new Rect(396, 268, 488, 30), 11, Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            ResumeButton = ActionButton("Resume", PauseRoot.transform, "Resume", "CONTINUE THIS RUN", "→", new Rect(382, 324, 516, 66), ResumeGame, true);
            RestartButton = ActionButton("Restart", PauseRoot.transform, "Restart", "START THIS GAME FRESH", "↻", new Rect(382, 404, 516, 64), RestartGame, false);
            BackButton = ActionButton("Back to Main Menu", PauseRoot.transform, "Back to Main Menu", "CHOOSE ANOTHER GAME", "←", new Rect(382, 482, 516, 64), BackToMainMenu, false);
            Label("Pause help", PauseRoot.transform, "ESC  resume    ·    ↑ / ↓  navigate    ·    ENTER  select", new Rect(350, 572, 580, 30), 12, Muted, TextAnchor.MiddleCenter);

            loadingRoot = Panel("Loading", parent, Ink);
            Label("Loading brand", loadingRoot.transform, GameCatalog.Title, new Rect(240, 280, 800, 80), 54, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Box("Loading accent", loadingRoot.transform, new Rect(600, 388, 80, 3), Accent);
            Label("Loading label", loadingRoot.transform, "LOADING YOUR NEXT PLAY…", new Rect(240, 412, 800, 42), 14, Muted, TextAnchor.MiddleCenter);
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

        private Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 64;
            const float radius = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Arcade rounded panel", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                var point = new Vector2(x + .5f, y + .5f);
                var nearest = new Vector2(Mathf.Clamp(point.x, radius, size - radius), Mathf.Clamp(point.y, radius, size - radius));
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius + .5f - Vector2.Distance(point, nearest)));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        private Image RoundedBox(string name, Transform parent, Rect bounds, Color color)
        {
            var image = Box(name, parent, bounds, color);
            image.sprite = RoundedSprite(); image.type = Image.Type.Sliced;
            return image;
        }

        private void TopLeft(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
        }

        private Image LocalBox(string name, Transform parent, Rect bounds, Color color, bool rounded = false)
        {
            var image = rounded ? RoundedBox(name, parent, bounds, color) : Box(name, parent, bounds, color);
            TopLeft(image.rectTransform, bounds);
            return image;
        }

        private Button ButtonShell(string name, Transform parent, Rect bounds, Color accent, bool primary)
        {
            RoundedBox(name + " shadow", parent, new Rect(bounds.x, bounds.y + 6, bounds.width, bounds.height), new Color32(0, 0, 0, 45));
            var border = RoundedBox(name, parent, bounds, new Color32(49, 65, 85, 255));
            border.raycastTarget = true;
            var fill = LocalBox("Surface", border.transform, new Rect(1, 1, bounds.width - 2, bounds.height - 2), primary ? accent : Surface, true);
            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border; button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var feedback = border.gameObject.AddComponent<ArcadeMenuButton>();
            feedback.Border = border; feedback.Background = fill; feedback.BaseColor = Surface; feedback.Accent = accent; feedback.Primary = primary;
            return button;
        }

        private Button GameCard(int index, Transform parent, Rect bounds, UnityEngine.Events.UnityAction action)
        {
            var accent = GameAccents[index];
            var button = ButtonShell("Game " + (index + 1), parent, bounds, accent, false);
            button.onClick.AddListener(action);
            var artFrame = LocalBox("Artwork mask", button.transform, new Rect(10, 10, bounds.width - 20, 180), Ink, true);
            artFrame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var artwork = new GameObject("Classroom artwork", typeof(RectTransform), typeof(RawImage));
            var artRect = Position(artwork, artFrame.transform, new Rect(0, 0, bounds.width - 20, 180));
            TopLeft(artRect, new Rect(0, 0, bounds.width - 20, 180));
            var raw = artwork.GetComponent<RawImage>();
            raw.texture = Resources.Load<Texture2D>("MenuArt/" + new[] { "Dogs", "Balloon", "Arena" }[index]); raw.raycastTarget = false;
            LocalBox("Index badge", button.transform, new Rect(24, 24, 40, 30), new Color32(12, 19, 31, 235), true);
            LocalLabel("Number", button.transform, (index + 1).ToString("00"), new Rect(24, 24, 40, 30), 13, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            string[] categories = { "REFLEX", "FLIGHT", "ARENA" };
            LocalLabel("Category", button.transform, categories[index], new Rect(224, 24, 128, 30), 12, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            LocalLabel("Label", button.transform, GameCatalog.Titles[index], new Rect(24, 204, 332, 34), 25, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            string[] descriptions = { "Send the dogs. Catch the falling balls.", "Collect cash. Keep clear of the bombs.", "Push your rivals out. Own the arena." };
            LocalLabel("Description", button.transform, descriptions[index], new Rect(24, 243, 332, 24), 14, Muted);
            LocalLabel("Challenge", button.transform, "CHALLENGE " + (index + 2), new Rect(24, 286, 180, 30), 11, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            var play = LocalBox("Play pill", button.transform, new Rect(220, 282, 132, 34), accent, true);
            button.GetComponent<ArcadeMenuButton>().Action = play;
            LocalLabel("Play label", button.transform, "PLAY GAME  →", new Rect(220, 282, 132, 34), 11, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
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

        private Button ActionButton(string name, Transform parent, string title, string subtitle, string symbol, Rect bounds, UnityEngine.Events.UnityAction action, bool primary)
        {
            var button = ButtonShell(name, parent, bounds, Accent, primary);
            var go = button.gameObject;
            button.onClick.AddListener(action);
            Color titleColor = primary ? Ink : Color.white;
            if (subtitle.Length == 0) LocalLabel("Label", go.transform, title, new Rect(14, 2, bounds.width - 28, bounds.height - 4), 18, titleColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            else {
                LocalLabel("Label", go.transform, title, new Rect(22, 7, bounds.width - 96, 28), 22, titleColor, TextAnchor.MiddleLeft, FontStyle.Bold);
                LocalLabel("Description", go.transform, subtitle, new Rect(23, 36, bounds.width - 98, 18), 10, primary ? new Color32(37, 73, 53, 255) : Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                LocalLabel("Symbol", go.transform, symbol, new Rect(bounds.width - 58, 3, 36, bounds.height - 6), 25, primary ? Ink : Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            return button;
        }

        private Text LocalLabel(string name, Transform parent, string value, Rect bounds, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var text = Label(name, parent, value, bounds, size, color, alignment, style);
            TopLeft(text.rectTransform, bounds);
            return text;
        }
    }
}
