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
        private Button test3DGameButton;
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

            // Cosmic theme colors
            Color panelColor = new Color(0.02f, 0.02f, 0.08f, 0.75f);
            Color cyanInputColor = new Color(0.15f, 0.4f, 0.6f, 0.9f);  // Cyan/teal for inputs
            Color blueButtonColor = new Color(0.2f, 0.45f, 0.75f, 1f);  // Blue for START
            Color orangeButtonColor = new Color(0.85f, 0.5f, 0.15f, 1f); // Orange/gold for TEST

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

            player1Input = CreatePlayerInputLarge(setupPanel.transform, defaultNames[0], player1Key, cyanInputColor);
            player2Input = CreatePlayerInputLarge(setupPanel.transform, defaultNames[1], player2Key, cyanInputColor);
            player3Input = CreatePlayerInputLarge(setupPanel.transform, defaultNames[2], player3Key, cyanInputColor);

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

            loadingBar = CreateSlider(rightPanel.transform, blueButtonColor);
            loadingText = CreateText(rightPanel.transform, "Loading...", 22, FontStyles.Normal, 350, 40);

            CreateSpacer(rightPanel.transform, 10);
            startButton = CreateButtonLarge(rightPanel.transform, "START GAME", blueButtonColor);
            startButton.interactable = false;
            
            CreateSpacer(rightPanel.transform, 10);
            testGameButton = CreateButtonLarge(rightPanel.transform, "TEST GAME", orangeButtonColor);
            testGameButton.interactable = false;
            
            // 3D button hidden for now - uncomment when 3D mode is ready
            // CreateSpacer(rightPanel.transform, 10);
            // Color test3DButtonColor = new Color(0.3f, 0.5f, 0.7f, 1f);
            // test3DGameButton = CreateButtonLarge(rightPanel.transform, "TEST GAME (3D)", test3DButtonColor);
            // test3DGameButton.interactable = false;
            
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
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(350, 60);
            inputObj.AddComponent<LayoutElement>().preferredWidth = 350;
            
            // Outer glow/border for cosmic effect
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(inputObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3, -3);
            borderRect.offsetMax = new Vector2(3, 3);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = CreateRoundedRectSprite(18);
            borderImg.type = Image.Type.Sliced;
            Color glowColor = new Color(
                Mathf.Min(1f, bgColor.r + 0.4f),
                Mathf.Min(1f, bgColor.g + 0.4f),
                Mathf.Min(1f, bgColor.b + 0.5f),
                0.9f
            );
            borderImg.color = glowColor;
            
            // Main background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(inputObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = CreateRoundedRectSprite(15);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(bgColor.r * 0.5f, bgColor.g * 0.5f, bgColor.b * 0.6f, 0.85f);
            
            // Text area with mask for proper caret rendering
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(15, 5);
            taRect.offsetMax = new Vector2(-15, -5);
            textArea.AddComponent<RectMask2D>();

            // Text component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 32;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Center;
            inputText.raycastTarget = false;

            // Placeholder (for showing default when empty)
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = defaultName;
            placeholder.fontSize = 32;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.raycastTarget = false;

            // Add input field AFTER creating all children
            TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
            input.targetGraphic = bgImg;  // Required for interaction/caret
            input.textViewport = taRect;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.text = "";  // Start empty so placeholder shows
            input.onFocusSelectAll = false;
            input.richText = false;
            input.resetOnDeActivation = false;
            
            // Configure cursor - dark blue for orange background  
            input.caretWidth = 4;
            input.customCaretColor = true;
            input.caretColor = new Color(0.1f, 0.15f, 0.4f);
            input.caretBlinkRate = 0.5f;
            input.selectionColor = new Color(0.1f, 0.2f, 0.5f, 0.5f);
            
            // Add cosmic input behavior (clear on focus, color change)
            CosmicInputField cosmicInput = inputObj.AddComponent<CosmicInputField>();
            cosmicInput.Initialize(borderImg, bgImg, defaultName);
            cosmicInput.SetColors(
                new Color(0.55f, 0.8f, 1f, 0.9f),      // cyan border
                new Color(0.075f, 0.2f, 0.36f, 0.85f), // cyan bg
                new Color(1f, 0.7f, 0.3f, 0.95f),      // orange border (focused)
                new Color(0.4f, 0.25f, 0.1f, 0.85f)    // orange bg (focused)
            );

            GameObject keyObj = new GameObject("Key");
            keyObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI keyTmp = keyObj.AddComponent<TextMeshProUGUI>();
            keyTmp.text = $"({key})";
            keyTmp.fontSize = 28;
            keyTmp.color = new Color(0.6f, 0.75f, 0.85f);
            keyObj.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 60);
            keyObj.AddComponent<LayoutElement>().preferredWidth = 70;

            return input;
        }

        private Button CreateButtonLarge(Transform parent, string text, Color color)
        {
            return CreateCosmicButton(parent, text, color, 340, 65);
        }
        
        private Button CreateCosmicButton(Transform parent, string text, Color fillColor, float width, float height)
        {
            GameObject obj = new GameObject($"Button_{text}");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            // Outer glow/border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(obj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3, -3);
            borderRect.offsetMax = new Vector2(3, 3);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = CreateRoundedRectSprite(18);
            borderImg.type = Image.Type.Sliced;
            // Glow color - brighter version of fill
            Color glowColor = new Color(
                Mathf.Min(1f, fillColor.r + 0.3f),
                Mathf.Min(1f, fillColor.g + 0.3f),
                Mathf.Min(1f, fillColor.b + 0.4f),
                0.9f
            );
            borderImg.color = glowColor;
            
            // Main button background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(obj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = CreateRoundedRectSprite(15);
            bgImg.type = Image.Type.Sliced;
            // Semi-transparent fill with gradient effect
            Color bgColor = new Color(fillColor.r * 0.6f, fillColor.g * 0.6f, fillColor.b * 0.7f, 0.85f);
            bgImg.color = bgColor;
            
            // Inner highlight (top gradient simulation)
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(obj.transform, false);
            RectTransform hlRect = highlightObj.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0.05f, 0.5f);
            hlRect.anchorMax = new Vector2(0.95f, 0.95f);
            hlRect.offsetMin = Vector2.zero;
            hlRect.offsetMax = Vector2.zero;
            Image hlImg = highlightObj.AddComponent<Image>();
            hlImg.sprite = CreateRoundedRectSprite(10);
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(1f, 1f, 1f, 0.08f);
            
            // Button component
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            
            // Button colors for hover/press states
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(
                Mathf.Min(1f, bgColor.r + 0.15f),
                Mathf.Min(1f, bgColor.g + 0.15f),
                Mathf.Min(1f, bgColor.b + 0.2f),
                0.95f
            );
            colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 0.95f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.25f, 0.6f);
            btn.colors = colors;
            
            obj.AddComponent<LayoutElement>().preferredWidth = width;

            // Text
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 20;
            tmp.fontSizeMax = 36;

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
            
            if (test3DGameButton != null)
            {
                test3DGameButton.onClick.AddListener(OnTest3DGameClicked);
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
            bool testConfigured = (testConfig != null && testConfig.IsConfigured);
            
            if (testGameButton != null)
            {
                testGameButton.interactable = testConfigured;
            }
            
            if (test3DGameButton != null)
            {
                test3DGameButton.interactable = testConfigured;
            }
            
            if (!testConfigured && loadingText != null)
            {
                loadingText.text = "Ready (Test Game not configured)";
            }

            isPreloadingComplete = true;
        }

        private void OnTestGameClicked()
        {
            Debug.Log("[HomeView] Test Game (2D) clicked");
            isTestGameMode = true;
            
            // Use test player names
            var testConfig = Resources.Load<TestGameConfig>("TestGameConfig");
            if (testConfig != null)
            {
                if (player1Input != null) player1Input.text = testConfig.playerNames[0];
                if (player2Input != null) player2Input.text = testConfig.playerNames[1];
                if (player3Input != null) player3Input.text = testConfig.playerNames[2];
            }
            
            StartCoroutine(StartTestGame(false));
        }

        private void OnTest3DGameClicked()
        {
            Debug.Log("[HomeView] Test Game (3D) clicked");
            isTestGameMode = true;
            
            // Use test player names
            var testConfig = Resources.Load<TestGameConfig>("TestGameConfig");
            if (testConfig != null)
            {
                if (player1Input != null) player1Input.text = testConfig.playerNames[0];
                if (player2Input != null) player2Input.text = testConfig.playerNames[1];
                if (player3Input != null) player3Input.text = testConfig.playerNames[2];
            }
            
            StartCoroutine(StartTestGame(true));
        }

        private IEnumerator StartTestGame(bool use3D = false)
        {
            // Show loading screen
            if (setupPanel != null) setupPanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            if (loadingScreen != null) loadingScreen.SetActive(true);
            
            string modeText = use3D ? "3D" : "2D";
            if (loadingScreenText != null) loadingScreenText.text = $"Loading {modeText} Test Game...";
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
            
            if (use3D)
            {
                SceneLoader.LoadGame3D();
            }
            else
            {
                SceneLoader.LoadGame();
            }
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
            
            if (loadingScreenBar != null) loadingScreenBar.value = 0.15f;
            if (loadingScreenText != null) loadingScreenText.text = "Preparing audio generation...";
            yield return null;
            
            // Create unified TTS loader if needed
            if (UnifiedTTSLoader.Instance == null)
            {
                var loaderObj = new GameObject("[UnifiedTTSLoader]");
                loaderObj.AddComponent<UnifiedTTSLoader>();
                DontDestroyOnLoad(loaderObj);
            }
            
            // Generate ALL TTS audio in one unified pass (categories, clues, answers, names, phrases)
            bool loadComplete = false;
            Action<int, int> progressHandler = (completed, total) =>
            {
                float progress = total > 0 ? (float)completed / total : 0f;
                if (loadingScreenBar != null) loadingScreenBar.value = 0.15f + progress * 0.8f;
                if (loadingScreenText != null)
                {
                    loadingScreenText.text = $"Generating audio... {completed}/{total}";
                }
            };
            Action completeHandler = () => loadComplete = true;
            
            UnifiedTTSLoader.Instance.OnProgress += progressHandler;
            UnifiedTTSLoader.Instance.OnComplete += completeHandler;
            UnifiedTTSLoader.Instance.LoadAllForGame(board, playerNames);
            
            float timeout = 300f; // 5 minute timeout for all TTS
            float elapsed = 0f;
            while (!loadComplete && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Unsubscribe
            UnifiedTTSLoader.Instance.OnProgress -= progressHandler;
            UnifiedTTSLoader.Instance.OnComplete -= completeHandler;
            
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
            if (test3DGameButton != null)
            {
                test3DGameButton.onClick.RemoveListener(OnTest3DGameClicked);
            }
        }
    }
}
