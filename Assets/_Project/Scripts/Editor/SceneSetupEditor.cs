#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

namespace Cerebrum.Editor
{
    public class SceneSetupEditor : EditorWindow
    {
        [MenuItem("Cerebrum/Setup Scenes")]
        public static void SetupScenes()
        {
            CreateHomeScene();
            CreateGameScene();
            CreatePrefabs();
            UpdateBuildSettings();
            
            Debug.Log("[SceneSetup] All scenes and prefabs created successfully!");
        }

        [MenuItem("Cerebrum/Create Home Scene Only")]
        public static void CreateHomeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0, 0, -10);

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "CEREBRUM";
            titleText.fontSize = 120;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.84f, 0f);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.sizeDelta = new Vector2(800, 150);
            titleRect.anchoredPosition = Vector2.zero;

            // Subtitle
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "A board.fun Trivia Game";
            subtitleText.fontSize = 36;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = Color.white;
            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.55f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.55f);
            subtitleRect.sizeDelta = new Vector2(600, 50);
            subtitleRect.anchoredPosition = Vector2.zero;

            // Start Button
            GameObject buttonObj = new GameObject("StartGameButton");
            buttonObj.transform.SetParent(canvasObj.transform, false);
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.06f, 0.06f, 0.4f);
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.1f, 0.1f, 0.5f);
            colors.pressedColor = new Color(0.04f, 0.04f, 0.3f);
            button.colors = colors;
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.35f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.35f);
            buttonRect.sizeDelta = new Vector2(300, 80);
            buttonRect.anchoredPosition = Vector2.zero;

            // Button Text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "START GAME";
            buttonText.fontSize = 36;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
            buttonTextRect.anchoredPosition = Vector2.zero;

            // HomeView Script
            GameObject homeViewObj = new GameObject("HomeView");
            var homeView = homeViewObj.AddComponent<UI.HomeView>();
            
            // Use SerializedObject to set the button reference
            SerializedObject serializedHomeView = new SerializedObject(homeView);
            serializedHomeView.FindProperty("startGameButton").objectReferenceValue = button;
            serializedHomeView.ApplyModifiedProperties();

            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Save scene
            string scenePath = "Assets/_Project/Scenes/Home.unity";
            EnsureDirectoryExists(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log("[SceneSetup] Home scene created at: " + scenePath);
        }

        [MenuItem("Cerebrum/Create Game Scene Only")]
        public static void CreateGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.08f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0, 0, -10);

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Main Layout
            GameObject mainLayoutObj = new GameObject("MainLayout");
            mainLayoutObj.transform.SetParent(canvasObj.transform, false);
            RectTransform mainLayoutRect = mainLayoutObj.AddComponent<RectTransform>();
            mainLayoutRect.anchorMin = Vector2.zero;
            mainLayoutRect.anchorMax = Vector2.one;
            mainLayoutRect.sizeDelta = Vector2.zero;
            mainLayoutRect.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup mainLayout = mainLayoutObj.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(20, 20, 20, 20);
            mainLayout.spacing = 10;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            // Category Headers Container
            GameObject categoryHeadersObj = new GameObject("CategoryHeaders");
            categoryHeadersObj.transform.SetParent(mainLayoutObj.transform, false);
            RectTransform categoryHeadersRect = categoryHeadersObj.AddComponent<RectTransform>();
            LayoutElement categoryHeadersLayout = categoryHeadersObj.AddComponent<LayoutElement>();
            categoryHeadersLayout.preferredHeight = 80;
            categoryHeadersLayout.flexibleHeight = 0;
            HorizontalLayoutGroup categoryHeadersGroup = categoryHeadersObj.AddComponent<HorizontalLayoutGroup>();
            categoryHeadersGroup.spacing = 10;
            categoryHeadersGroup.childControlWidth = true;
            categoryHeadersGroup.childControlHeight = true;
            categoryHeadersGroup.childForceExpandWidth = true;
            categoryHeadersGroup.childForceExpandHeight = true;

            // Clue Grid Container
            GameObject gridObj = new GameObject("ClueGrid");
            gridObj.transform.SetParent(mainLayoutObj.transform, false);
            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            LayoutElement gridLayout = gridObj.AddComponent<LayoutElement>();
            gridLayout.flexibleHeight = 1;
            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.cellSize = new Vector2(280, 120);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;

            // Player Panels Container
            GameObject playerPanelsObj = new GameObject("PlayerPanels");
            playerPanelsObj.transform.SetParent(mainLayoutObj.transform, false);
            RectTransform playerPanelsRect = playerPanelsObj.AddComponent<RectTransform>();
            LayoutElement playerPanelsLayout = playerPanelsObj.AddComponent<LayoutElement>();
            playerPanelsLayout.preferredHeight = 120;
            playerPanelsLayout.flexibleHeight = 0;
            HorizontalLayoutGroup playerPanelsGroup = playerPanelsObj.AddComponent<HorizontalLayoutGroup>();
            playerPanelsGroup.spacing = 20;
            playerPanelsGroup.childControlWidth = true;
            playerPanelsGroup.childControlHeight = true;
            playerPanelsGroup.childForceExpandWidth = true;
            playerPanelsGroup.childForceExpandHeight = true;
            playerPanelsGroup.padding = new RectOffset(100, 100, 10, 10);

            // Clue Overlay
            GameObject overlayObj = CreateClueOverlay(canvasObj.transform);

            // Board Controller
            GameObject boardControllerObj = new GameObject("BoardController");
            var boardController = boardControllerObj.AddComponent<Game.BoardController>();

            // Game Scene Controller
            GameObject gameSceneControllerObj = new GameObject("GameSceneController");
            var gameSceneController = gameSceneControllerObj.AddComponent<Game.GameSceneController>();

            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Wire up references using SerializedObject
            SerializedObject serializedBoardController = new SerializedObject(boardController);
            serializedBoardController.FindProperty("categoryHeaderContainer").objectReferenceValue = categoryHeadersObj.transform;
            serializedBoardController.FindProperty("gridContainer").objectReferenceValue = gridObj.transform;
            serializedBoardController.FindProperty("playerPanelContainer").objectReferenceValue = playerPanelsObj.transform;
            serializedBoardController.FindProperty("clueOverlay").objectReferenceValue = overlayObj.GetComponent<UI.ClueOverlayView>();
            serializedBoardController.ApplyModifiedProperties();

            SerializedObject serializedGameSceneController = new SerializedObject(gameSceneController);
            serializedGameSceneController.FindProperty("boardController").objectReferenceValue = boardController;
            serializedGameSceneController.ApplyModifiedProperties();

            // Save scene
            string scenePath = "Assets/_Project/Scenes/Game.unity";
            EnsureDirectoryExists(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log("[SceneSetup] Game scene created at: " + scenePath);
        }

        private static GameObject CreateClueOverlay(Transform parent)
        {
            // Overlay Panel (hidden by default)
            GameObject overlayObj = new GameObject("ClueOverlay");
            overlayObj.transform.SetParent(parent, false);
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var clueOverlayView = overlayObj.AddComponent<UI.ClueOverlayView>();

            // Background Panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(overlayObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.06f, 0.4f);
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.sizeDelta = Vector2.zero;

            // Category Text
            GameObject categoryTextObj = new GameObject("CategoryText");
            categoryTextObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI categoryText = categoryTextObj.AddComponent<TextMeshProUGUI>();
            categoryText.text = "CATEGORY";
            categoryText.fontSize = 36;
            categoryText.alignment = TextAlignmentOptions.Center;
            categoryText.color = Color.white;
            RectTransform categoryRect = categoryTextObj.GetComponent<RectTransform>();
            categoryRect.anchorMin = new Vector2(0, 0.85f);
            categoryRect.anchorMax = new Vector2(1, 0.95f);
            categoryRect.sizeDelta = Vector2.zero;

            // Value Text
            GameObject valueTextObj = new GameObject("ValueText");
            valueTextObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI valueText = valueTextObj.AddComponent<TextMeshProUGUI>();
            valueText.text = "$200";
            valueText.fontSize = 48;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = new Color(1f, 0.84f, 0f);
            RectTransform valueRect = valueTextObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0.75f);
            valueRect.anchorMax = new Vector2(1, 0.85f);
            valueRect.sizeDelta = Vector2.zero;

            // Question Text
            GameObject questionTextObj = new GameObject("QuestionText");
            questionTextObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI questionText = questionTextObj.AddComponent<TextMeshProUGUI>();
            questionText.text = "This is the clue question...";
            questionText.fontSize = 42;
            questionText.alignment = TextAlignmentOptions.Center;
            questionText.color = Color.white;
            RectTransform questionRect = questionTextObj.GetComponent<RectTransform>();
            questionRect.anchorMin = new Vector2(0.05f, 0.35f);
            questionRect.anchorMax = new Vector2(0.95f, 0.75f);
            questionRect.sizeDelta = Vector2.zero;

            // Answer Text
            GameObject answerTextObj = new GameObject("AnswerText");
            answerTextObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI answerText = answerTextObj.AddComponent<TextMeshProUGUI>();
            answerText.text = "";
            answerText.fontSize = 36;
            answerText.alignment = TextAlignmentOptions.Center;
            answerText.color = new Color(0.5f, 1f, 0.5f);
            RectTransform answerRect = answerTextObj.GetComponent<RectTransform>();
            answerRect.anchorMin = new Vector2(0.05f, 0.25f);
            answerRect.anchorMax = new Vector2(0.95f, 0.35f);
            answerRect.sizeDelta = Vector2.zero;

            // Status Text (for TTS loading indicator)
            GameObject statusTextObj = new GameObject("StatusText");
            statusTextObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 24;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = new Color(0.8f, 0.8f, 0.8f);
            statusText.fontStyle = FontStyles.Italic;
            RectTransform statusRect = statusTextObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.18f);
            statusRect.anchorMax = new Vector2(0.95f, 0.25f);
            statusRect.sizeDelta = Vector2.zero;

            // Transcript Text (shows what player said)
            GameObject transcriptTextObj = new GameObject("TranscriptText");
            transcriptTextObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI transcriptText = transcriptTextObj.AddComponent<TextMeshProUGUI>();
            transcriptText.text = "";
            transcriptText.fontSize = 28;
            transcriptText.alignment = TextAlignmentOptions.Center;
            transcriptText.color = new Color(1f, 1f, 0.7f);
            RectTransform transcriptRect = transcriptTextObj.GetComponent<RectTransform>();
            transcriptRect.anchorMin = new Vector2(0.05f, 0.12f);
            transcriptRect.anchorMax = new Vector2(0.95f, 0.18f);
            transcriptRect.sizeDelta = Vector2.zero;

            // Reveal Answer Button
            GameObject revealBtnObj = CreateButton(panelObj.transform, "RevealAnswerButton", "REVEAL ANSWER", 
                new Vector2(0.25f, 0.02f), new Vector2(0.45f, 0.12f));
            Button revealBtn = revealBtnObj.GetComponent<Button>();

            // Back to Board Button
            GameObject backBtnObj = CreateButton(panelObj.transform, "BackToBoardButton", "BACK TO BOARD", 
                new Vector2(0.55f, 0.02f), new Vector2(0.75f, 0.12f));
            Button backBtn = backBtnObj.GetComponent<Button>();

            // Wire up ClueOverlayView references
            SerializedObject serializedOverlay = new SerializedObject(clueOverlayView);
            serializedOverlay.FindProperty("overlayPanel").objectReferenceValue = panelObj;
            serializedOverlay.FindProperty("categoryText").objectReferenceValue = categoryText;
            serializedOverlay.FindProperty("valueText").objectReferenceValue = valueText;
            serializedOverlay.FindProperty("questionText").objectReferenceValue = questionText;
            serializedOverlay.FindProperty("answerText").objectReferenceValue = answerText;
            serializedOverlay.FindProperty("statusText").objectReferenceValue = statusText;
            serializedOverlay.FindProperty("transcriptText").objectReferenceValue = transcriptText;
            serializedOverlay.FindProperty("revealAnswerButton").objectReferenceValue = revealBtn;
            serializedOverlay.FindProperty("backToBoardButton").objectReferenceValue = backBtn;
            serializedOverlay.ApplyModifiedProperties();

            return overlayObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.1f, 0.1f, 0.25f);
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.15f, 0.15f, 0.35f);
            colors.pressedColor = new Color(0.08f, 0.08f, 0.2f);
            button.colors = colors;
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 24;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return buttonObj;
        }

        [MenuItem("Cerebrum/Create Prefabs Only")]
        public static void CreatePrefabs()
        {
            EnsureDirectoryExists("Assets/_Project/Resources/Prefabs/dummy.txt");

            // Category Header Prefab
            CreateCategoryHeaderPrefab();

            // Clue Button Prefab
            CreateClueButtonPrefab();

            // Player Panel Prefab
            CreatePlayerPanelPrefab();

            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] All prefabs created!");
        }

        private static void CreateCategoryHeaderPrefab()
        {
            GameObject headerObj = new GameObject("CategoryHeader");
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();

            // Layer 1: Outer glow (soft cyan/orange gradient simulation)
            GameObject outerGlow = new GameObject("OuterGlow");
            outerGlow.transform.SetParent(headerObj.transform, false);
            RectTransform outerGlowRect = outerGlow.AddComponent<RectTransform>();
            outerGlowRect.anchorMin = Vector2.zero;
            outerGlowRect.anchorMax = Vector2.one;
            outerGlowRect.offsetMin = new Vector2(-6, -6);
            outerGlowRect.offsetMax = new Vector2(6, 6);
            Image outerGlowImage = outerGlow.AddComponent<Image>();
            outerGlowImage.color = new Color(0.2f, 0.5f, 0.8f, 0.4f);
            Outline glow1 = outerGlow.AddComponent<Outline>();
            glow1.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.3f);
            glow1.effectDistance = new Vector2(4, 4);
            Outline glow2 = outerGlow.AddComponent<Outline>();
            glow2.effectColor = new Color(0.8f, 0.5f, 0.3f, 0.2f);
            glow2.effectDistance = new Vector2(3, -3);

            // Layer 2: Border frame (visible edge)
            GameObject borderFrame = new GameObject("BorderFrame");
            borderFrame.transform.SetParent(headerObj.transform, false);
            RectTransform borderRect = borderFrame.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            Image borderImage = borderFrame.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.55f, 0.75f, 0.9f);
            Outline borderInner = borderFrame.AddComponent<Outline>();
            borderInner.effectColor = new Color(0.6f, 0.75f, 0.95f, 0.8f);
            borderInner.effectDistance = new Vector2(1, 1);

            // Layer 3: Inner panel (dark background)
            GameObject innerPanel = new GameObject("InnerPanel");
            innerPanel.transform.SetParent(headerObj.transform, false);
            RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3, 3);
            innerRect.offsetMax = new Vector2(-3, -3);
            Image innerImage = innerPanel.AddComponent<Image>();
            innerImage.color = new Color(0.06f, 0.08f, 0.18f, 0.95f);

            // Layer 4: Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CATEGORY";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 20;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 8);
            textRect.offsetMax = new Vector2(-8, -8);

            string prefabPath = "Assets/_Project/Resources/Prefabs/CategoryHeader.prefab";
            PrefabUtility.SaveAsPrefabAsset(headerObj, prefabPath);
            Object.DestroyImmediate(headerObj);
        }

        private static void CreateClueButtonPrefab()
        {
            GameObject buttonObj = new GameObject("ClueButton");
            
            // Semi-transparent dark blue background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.08f, 0.12f, 0.28f, 0.9f);
            
            // Add glowing border
            Outline borderOutline = buttonObj.AddComponent<Outline>();
            borderOutline.effectColor = new Color(0.5f, 0.7f, 1f, 0.7f);
            borderOutline.effectDistance = new Vector2(2, 2);
            
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.9f, 1f);
            colors.pressedColor = new Color(0.6f, 0.7f, 0.9f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.4f);
            button.colors = colors;

            GameObject textObj = new GameObject("ValueText");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "$200";
            text.fontSize = 42;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var clueButton = buttonObj.AddComponent<UI.ClueButton>();

            // Wire up references
            SerializedObject serialized = new SerializedObject(clueButton);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("valueText").objectReferenceValue = text;
            serialized.FindProperty("backgroundImage").objectReferenceValue = buttonImage;
            serialized.ApplyModifiedProperties();

            string prefabPath = "Assets/_Project/Resources/Prefabs/ClueButton.prefab";
            PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
            Object.DestroyImmediate(buttonObj);
        }

        private static void CreatePlayerPanelPrefab()
        {
            GameObject panelObj = new GameObject("PlayerPanel");
            
            // Semi-transparent dark background
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);
            
            // Add glowing border (will be recolored per player)
            Outline borderOutline = panelObj.AddComponent<Outline>();
            borderOutline.effectColor = new Color(0.3f, 0.8f, 1f, 0.9f); // Cyan default
            borderOutline.effectDistance = new Vector2(3, 3);

            // Name text (top half)
            GameObject textObj = new GameObject("NameText");
            textObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI nameText = textObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "NAME";
            nameText.fontSize = 28;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.fontStyle = FontStyles.Bold;
            RectTransform nameRect = textObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-10, -8);

            // Score text (bottom half)
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "$0";
            scoreText.fontSize = 32;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = new Color(1f, 0.9f, 0.5f); // Gold tint
            scoreText.fontStyle = FontStyles.Bold;
            RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 0);
            scoreRect.anchorMax = new Vector2(1f, 0.5f);
            scoreRect.offsetMin = new Vector2(10, 8);
            scoreRect.offsetMax = new Vector2(-10, 0);

            // Chooser Indicator (thin line at top when active)
            GameObject indicatorObj = new GameObject("ChooserIndicator");
            indicatorObj.transform.SetParent(panelObj.transform, false);
            Image indicatorImage = indicatorObj.AddComponent<Image>();
            indicatorImage.color = new Color(1f, 0.7f, 0.2f); // Orange glow
            RectTransform indicatorRect = indicatorObj.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0, 1f);
            indicatorRect.anchorMax = new Vector2(1f, 1f);
            indicatorRect.offsetMin = new Vector2(5, -8);
            indicatorRect.offsetMax = new Vector2(-5, -3);
            indicatorObj.SetActive(false);

            var playerPanel = panelObj.AddComponent<UI.PlayerPanel>();

            // Wire up references
            SerializedObject serialized = new SerializedObject(playerPanel);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("scoreText").objectReferenceValue = scoreText;
            serialized.FindProperty("backgroundImage").objectReferenceValue = panelImage;
            serialized.FindProperty("chooserIndicator").objectReferenceValue = indicatorImage;
            serialized.ApplyModifiedProperties();

            string prefabPath = "Assets/_Project/Resources/Prefabs/PlayerPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelObj, prefabPath);
            Object.DestroyImmediate(panelObj);
        }

        [MenuItem("Cerebrum/Update Build Settings")]
        public static void UpdateBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Home.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[SceneSetup] Build settings updated with Home and Game scenes");
        }

        [MenuItem("Cerebrum/Create OpenAI Config")]
        public static void CreateOpenAIConfig()
        {
            string resourcesPath = "Assets/_Project/Resources";
            EnsureDirectoryExists(resourcesPath + "/dummy.txt");

            string configPath = resourcesPath + "/OpenAIConfig.asset";
            
            if (System.IO.File.Exists(configPath))
            {
                Debug.Log("[SceneSetup] OpenAIConfig already exists at: " + configPath);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<OpenAI.OpenAIConfig>(configPath);
                return;
            }

            var config = ScriptableObject.CreateInstance<OpenAI.OpenAIConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            Debug.Log("[SceneSetup] OpenAIConfig created at: " + configPath);
            Debug.Log("[SceneSetup] IMPORTANT: Set your OpenAI API key in the Inspector!");
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
#endif
