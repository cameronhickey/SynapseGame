using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Core;
using Cerebrum.Data;
using Cerebrum.Game;
using Cerebrum.OpenAI;

namespace Cerebrum.UI
{
    public class HomeView : MonoBehaviour
    {
        [Header("Player Setup")]
        [SerializeField] private string[] defaultNames = { "Player 1", "Player 2", "Player 3" };
        
        [Header("Loading")]
        #pragma warning disable CS0414
        [SerializeField] private float minimumLoadTime = 2f;
        #pragma warning restore CS0414

        private TMP_InputField player1Input;
        private TMP_InputField player2Input;
        private TMP_InputField player3Input;
        private Button startButton;
        private Slider loadingBar;
        private TextMeshProUGUI loadingText;
        private GameObject setupPanel;

        [SerializeField] private string player1Key = "Q";
        [SerializeField] private string player2Key = "G";
        [SerializeField] private string player3Key = "M";

        #pragma warning disable CS0414
        private bool isPreloadingComplete;
        private bool isLoadingPlayerAudio;
        #pragma warning restore CS0414

        private GameObject rightPanel;
        private GameObject backgroundImage;
        private TMP_InputField[] playerInputs;
        private Button testGameButton;
        private GameObject loadingScreen;
        private Slider loadingScreenBar;
        private TextMeshProUGUI loadingScreenText;
        #pragma warning disable CS0414
        private bool isTestGameMode;
        #pragma warning restore CS0414

        private void Start()
        {
            // Disable old PlayerSetupUI if it exists
            DisableOldUI();
            
            BuildUI();
            SetupUI();
            // Don't preload audio on startup - wait for user to click Start Game
            StartCoroutine(LoadCategoriesOnly());
            
            playerInputs = new TMP_InputField[] { player1Input, player2Input, player3Input };
        }

        private void DisableOldUI()
        {
            // Find and disable the old PlayerSetupUI
            var oldSetup = FindFirstObjectByType<PlayerSetupUI>();
            if (oldSetup != null)
            {
                Debug.Log("[HomeView] Disabling old PlayerSetupUI");
                oldSetup.gameObject.SetActive(false);
            }
            
            // Find and disable PreloadPromptUI
            var preloadPrompt = FindFirstObjectByType<PreloadPromptUI>();
            if (preloadPrompt != null)
            {
                Debug.Log("[HomeView] Disabling old PreloadPromptUI");
                preloadPrompt.gameObject.SetActive(false);
            }
            
            // Also find and destroy any objects named with old UI patterns
            var oldPanels = new string[] { "SetupPanel", "PlayerSetupPanel", "StartButton", "LoadingPanel", "PreloadPanel", "PreloadPrompt" };
            foreach (var panelName in oldPanels)
            {
                var obj = GameObject.Find(panelName);
                if (obj != null && obj.transform.parent != transform)
                {
                    Debug.Log($"[HomeView] Destroying old UI: {panelName}");
                    Destroy(obj);
                }
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleTabNavigation();
            }
        }

        private void HandleTabNavigation()
        {
            if (playerInputs == null) return;
            
            int currentIndex = -1;
            for (int i = 0; i < playerInputs.Length; i++)
            {
                if (playerInputs[i] != null && playerInputs[i].isFocused)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            int nextIndex;
            if (UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed)
            {
                nextIndex = currentIndex <= 0 ? playerInputs.Length - 1 : currentIndex - 1;
            }
            else
            {
                nextIndex = currentIndex >= playerInputs.Length - 1 ? 0 : currentIndex + 1;
            }
            
            if (playerInputs[nextIndex] != null)
            {
                playerInputs[nextIndex].Select();
                playerInputs[nextIndex].ActivateInputField();
            }
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[HomeView] No canvas found for UI generation");
                return;
            }

            CreateBackgroundImage(canvas);

            Color panelColor = new Color(0.05f, 0.05f, 0.15f, 0.88f);
            Color inputBgColor = new Color(0.1f, 0.1f, 0.25f, 0.95f);
            Color buttonColor = new Color(0.2f, 0.4f, 0.8f, 1f);

            // Left panel for player names
            setupPanel = new GameObject("PlayerSetupPanel");
            setupPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = setupPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.05f);
            panelRect.anchorMax = new Vector2(0.42f, 0.38f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImg = setupPanel.AddComponent<Image>();
            panelImg.color = panelColor;
            panelImg.sprite = CreateRoundedRectSprite(50);
            panelImg.type = Image.Type.Sliced;

            VerticalLayoutGroup layout = setupPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(20, 20, 20, 20);

            CreateText(setupPanel.transform, "Enter Player Names", 28, FontStyles.Bold, 400, 40);
            CreateSpacer(setupPanel.transform, 5);

            player1Input = CreatePlayerInputLarge(setupPanel.transform, defaultNames[0], player1Key, inputBgColor);
            player2Input = CreatePlayerInputLarge(setupPanel.transform, defaultNames[1], player2Key, inputBgColor);
            player3Input = CreatePlayerInputLarge(setupPanel.transform, defaultNames[2], player3Key, inputBgColor);

            // Right panel for loading/start
            rightPanel = new GameObject("StartPanel");
            rightPanel.transform.SetParent(canvas.transform, false);
            RectTransform rightRect = rightPanel.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.58f, 0.05f);
            rightRect.anchorMax = new Vector2(0.92f, 0.38f);
            rightRect.offsetMin = Vector2.zero;
            rightRect.offsetMax = Vector2.zero;
            Image rightImg = rightPanel.AddComponent<Image>();
            rightImg.color = panelColor;
            rightImg.sprite = CreateRoundedRectSprite(50);
            rightImg.type = Image.Type.Sliced;

            VerticalLayoutGroup rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 15;
            rightLayout.childAlignment = TextAnchor.UpperCenter;
            rightLayout.childControlWidth = false;
            rightLayout.childControlHeight = false;
            rightLayout.padding = new RectOffset(20, 20, 25, 20);

            loadingBar = CreateSlider(rightPanel.transform, buttonColor);
            loadingText = CreateText(rightPanel.transform, "Loading...", 22, FontStyles.Normal, 350, 40);

            CreateSpacer(rightPanel.transform, 10);
            startButton = CreateButtonLarge(rightPanel.transform, "START GAME", buttonColor);
            startButton.interactable = false;
            
            CreateSpacer(rightPanel.transform, 10);
            Color testButtonColor = new Color(0.4f, 0.6f, 0.3f, 1f);  // Green tint for test
            testGameButton = CreateButtonLarge(rightPanel.transform, "TEST GAME", testButtonColor);
            testGameButton.interactable = false;
            
            // Create loading screen (hidden initially)
            CreateLoadingScreen(canvas);
        }

        private void CreateBackgroundImage(Canvas canvas)
        {
            backgroundImage = new GameObject("BackgroundImage");
            backgroundImage.transform.SetParent(canvas.transform, false);
            backgroundImage.transform.SetAsFirstSibling();

            RectTransform bgRect = backgroundImage.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = backgroundImage.AddComponent<Image>();
            Sprite bgSprite = Resources.Load<Sprite>("Images/Cerebrum_Home_Screen");
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.preserveAspect = false;
            }
            else
            {
                bgImage.color = new Color(0.05f, 0.05f, 0.2f, 1f);
                Debug.LogWarning("[HomeView] Background image not found at Resources/Images/Cerebrum_Home_Screen");
            }
        }

        private Sprite CreateRoundedRectSprite(int cornerRadius)
        {
            int size = cornerRadius * 3;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            
            Color white = Color.white;
            Color clear = new Color(1, 1, 1, 0);
            
            int r = cornerRadius;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0, dy = 0;
                    
                    if (x < r && y < r) { dx = r - x; dy = r - y; }
                    else if (x >= size - r && y < r) { dx = x - (size - r - 1); dy = r - y; }
                    else if (x < r && y >= size - r) { dx = r - x; dy = y - (size - r - 1); }
                    else if (x >= size - r && y >= size - r) { dx = x - (size - r - 1); dy = y - (size - r - 1); }
                    
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist > r)
                        tex.SetPixel(x, y, clear);
                    else if (dist > r - 1)
                        tex.SetPixel(x, y, new Color(1, 1, 1, r - dist));
                    else
                        tex.SetPixel(x, y, white);
                }
            }
            
            tex.Apply();
            
            int border = cornerRadius;
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            
            return sprite;
        }

        private TMP_InputField CreatePlayerInputLarge(Transform parent, string defaultName, string key, Color bgColor)
        {
            GameObject row = new GameObject($"Row_{defaultName}");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(450, 70);
            HorizontalLayoutGroup hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 15;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = false;
            hl.childControlHeight = false;
            LayoutElement rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredWidth = 450;
            rowLE.preferredHeight = 70;

            GameObject inputObj = new GameObject("Input");
            inputObj.transform.SetParent(row.transform, false);
            inputObj.AddComponent<RectTransform>().sizeDelta = new Vector2(350, 65);
            inputObj.AddComponent<Image>().color = bgColor;
            TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
            inputObj.AddComponent<LayoutElement>().preferredWidth = 350;

            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(15, 8);
            taRect.offsetMax = new Vector2(-15, -8);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 36;
            inputText.color = Color.white;

            input.textViewport = taRect;
            input.textComponent = inputText;
            input.text = defaultName;

            GameObject keyObj = new GameObject("Key");
            keyObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI keyTmp = keyObj.AddComponent<TextMeshProUGUI>();
            keyTmp.text = $"({key})";
            keyTmp.fontSize = 32;
            keyTmp.color = new Color(0.7f, 0.7f, 0.7f);
            keyObj.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 65);
            keyObj.AddComponent<LayoutElement>().preferredWidth = 70;

            return input;
        }

        private Button CreateButtonLarge(Transform parent, string text, Color color)
        {
            GameObject obj = new GameObject("StartButton");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(300, 70);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            obj.AddComponent<LayoutElement>().preferredWidth = 300;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, FontStyles style, float width, float height)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            obj.AddComponent<LayoutElement>().preferredHeight = height;
            return tmp;
        }

        private void CreateSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(10, height);
            spacer.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private Slider CreateSlider(Transform parent, Color fillColor)
        {
            GameObject obj = new GameObject("LoadingBar");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(350, 30);
            Slider slider = obj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            obj.AddComponent<LayoutElement>().preferredWidth = 350;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(obj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);

            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(obj.transform, false);
            RectTransform faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.offsetMin = Vector2.zero;
            faRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRect = fill.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = fillColor;

            slider.fillRect = fRect;
            return slider;
        }

        private void CreateLoadingScreen(Canvas canvas)
        {
            loadingScreen = new GameObject("LoadingScreen");
            loadingScreen.transform.SetParent(canvas.transform, false);
            
            RectTransform screenRect = loadingScreen.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;
            
            // Dark background
            Image bg = loadingScreen.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.1f, 0.95f);
            
            // Center panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(loadingScreen.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.4f);
            panelRect.anchorMax = new Vector2(0.7f, 0.6f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 20;
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth = false;
            panelLayout.childControlHeight = false;
            
            // Title
            loadingScreenText = CreateText(panel.transform, "Loading Game...", 36, FontStyles.Bold, 500, 50);
            
            // Progress bar
            Color barColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            loadingScreenBar = CreateSlider(panel.transform, barColor);
            loadingScreenBar.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);
            
            loadingScreen.SetActive(false);
        }

        private void SetupUI()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartGameClicked);
            }
            
            if (testGameButton != null)
            {
                testGameButton.onClick.AddListener(OnTestGameClicked);
            }
        }

        private IEnumerator LoadCategoriesOnly()
        {
            if (loadingText != null) loadingText.text = "Loading categories...";
            if (loadingBar != null) loadingBar.value = 0.2f;
            yield return null;

            if (OptimizedCategoryLoader.Instance != null)
            {
                while (!OptimizedCategoryLoader.Instance.IsLoaded)
                {
                    yield return null;
                }
            }

            if (loadingBar != null) loadingBar.value = 1f;
            if (loadingText != null) loadingText.text = "Ready - Choose Start or Test Game";
            
            if (startButton != null) startButton.interactable = true;
            
            // Check if test game is available
            var testConfig = Resources.Load<TestGameConfig>("TestGameConfig");
            if (testGameButton != null)
            {
                testGameButton.interactable = (testConfig != null && testConfig.IsConfigured);
                if (!testGameButton.interactable && loadingText != null)
                {
                    loadingText.text = "Ready (Test Game not configured)";
                }
            }

            isPreloadingComplete = true;
        }

        private void OnTestGameClicked()
        {
            Debug.Log("[HomeView] Test Game clicked");
            isTestGameMode = true;
            
            // Use test player names
            var testConfig = Resources.Load<TestGameConfig>("TestGameConfig");
            if (testConfig != null)
            {
                if (player1Input != null) player1Input.text = testConfig.playerNames[0];
                if (player2Input != null) player2Input.text = testConfig.playerNames[1];
                if (player3Input != null) player3Input.text = testConfig.playerNames[2];
            }
            
            StartCoroutine(StartTestGame());
        }

        private IEnumerator StartTestGame()
        {
            // Show loading screen
            if (setupPanel != null) setupPanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            if (loadingScreen != null) loadingScreen.SetActive(true);
            
            if (loadingScreenText != null) loadingScreenText.text = "Loading Test Game...";
            if (loadingScreenBar != null) loadingScreenBar.value = 0.2f;
            yield return null;
            
            // Ensure TestGameAudioLoader exists
            if (TestGameAudioLoader.Instance == null)
            {
                GameObject loaderObj = new GameObject("TestGameAudioLoader");
                loaderObj.AddComponent<TestGameAudioLoader>();
            }
            
            // Load test audio
            if (loadingScreenText != null) loadingScreenText.text = "Loading audio...";
            if (loadingScreenBar != null) loadingScreenBar.value = 0.5f;
            TestGameAudioLoader.Instance.LoadTestGameAudio();
            yield return null;
            
            // Create test board
            Board testBoard = TestGameAudioLoader.Instance.CreateTestBoard();
            if (testBoard != null && GameManager.Instance != null)
            {
                GameManager.Instance.SetCurrentBoard(testBoard);
                GameManager.Instance.SetPlayerNames(new List<string>(TestGameAudioLoader.Instance.GetTestPlayerNames()));
                GameManager.Instance.IsTestMode = true;
            }
            
            if (loadingScreenBar != null) loadingScreenBar.value = 1f;
            if (loadingScreenText != null) loadingScreenText.text = "Starting...";
            yield return new WaitForSeconds(0.3f);
            
            // Hide background and go to game
            if (backgroundImage != null) Destroy(backgroundImage);
            if (loadingScreen != null) Destroy(loadingScreen);
            
            SceneLoader.LoadGame();
        }

        private void OnStartGameClicked()
        {
            List<string> playerNames = GetPlayerNames();
            
            Debug.Log($"[HomeView] Starting game with players: {string.Join(", ", playerNames)}");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerNames(playerNames);
                GameManager.Instance.IsTestMode = false;
            }

            StartCoroutine(StartRealGame(playerNames));
        }

        private IEnumerator StartRealGame(List<string> playerNames)
        {
            // Show loading screen
            if (setupPanel != null) setupPanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            if (loadingScreen != null) loadingScreen.SetActive(true);
            
            if (loadingScreenText != null) loadingScreenText.text = "Selecting categories...";
            if (loadingScreenBar != null) loadingScreenBar.value = 0.1f;
            yield return null;
            
            // Load random board
            Board board = OptimizedCategoryLoader.Instance?.LoadRandomBoard(6);
            if (board != null && GameManager.Instance != null)
            {
                GameManager.Instance.SetCurrentBoard(board);
            }
            
            if (loadingScreenBar != null) loadingScreenBar.value = 0.2f;
            if (loadingScreenText != null) loadingScreenText.text = "Generating audio...";
            yield return null;
            
            // Generate TTS audio for all clues and answers
            if (TTSCache.Instance != null && board != null)
            {
                bool cacheComplete = false;
                TTSCache.Instance.OnCacheProgress += (progress) =>
                {
                    if (loadingScreenBar != null) loadingScreenBar.value = 0.2f + progress * 0.6f;
                };
                TTSCache.Instance.OnCacheComplete += () => cacheComplete = true;
                TTSCache.Instance.PreCacheBoard(board);

                while (!cacheComplete)
                {
                    yield return null;
                }
            }
            
            if (loadingScreenBar != null) loadingScreenBar.value = 0.85f;
            if (loadingScreenText != null) loadingScreenText.text = "Loading player audio...";
            
            // Cache player name phrases
            if (PhraseTTSCache.Instance != null)
            {
                bool phraseComplete = false;
                PhraseTTSCache.Instance.CachePlayerNames(playerNames, () => phraseComplete = true);

                float timeout = 15f;
                float elapsed = 0f;
                while (!phraseComplete && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            
            if (loadingScreenBar != null) loadingScreenBar.value = 1f;
            if (loadingScreenText != null) loadingScreenText.text = "Starting game...";
            yield return new WaitForSeconds(0.3f);
            
            // Clean up and start game
            if (backgroundImage != null) Destroy(backgroundImage);
            if (loadingScreen != null) Destroy(loadingScreen);
            
            SceneLoader.LoadGame();
        }

        private List<string> GetPlayerNames()
        {
            return new List<string>
            {
                string.IsNullOrWhiteSpace(player1Input?.text) ? defaultNames[0] : player1Input.text.Trim(),
                string.IsNullOrWhiteSpace(player2Input?.text) ? defaultNames[1] : player2Input.text.Trim(),
                string.IsNullOrWhiteSpace(player3Input?.text) ? defaultNames[2] : player3Input.text.Trim()
            };
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartGameClicked);
            }
            if (testGameButton != null)
            {
                testGameButton.onClick.RemoveListener(OnTestGameClicked);
            }
        }
    }
}
