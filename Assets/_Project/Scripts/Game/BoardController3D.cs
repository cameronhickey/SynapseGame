using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;
using Cerebrum.UI;
using Cerebrum.OpenAI;

namespace Cerebrum.Game
{
    /// <summary>
    /// 3D version of the game board using proper 3D objects and camera for realistic animations.
    /// </summary>
    public class BoardController3D : MonoBehaviour
    {
        // Board layout settings reserved for future 3D card rendering
        #pragma warning disable CS0414
        [Header("Board Layout")]
        [SerializeField] private float cardWidth = 1.2f;
        [SerializeField] private float cardHeight = 0.8f;
        [SerializeField] private float cardSpacing = 0.1f;
        [SerializeField] private float headerHeight = 0.5f;
        #pragma warning restore CS0414
        
        [Header("Materials")]
        [SerializeField] private Material cardFrontMaterial;
        [SerializeField] private Material cardBackMaterial;
        [SerializeField] private Material headerMaterial;
        
        [Header("Colors")]
        [SerializeField] private Color cardColor = new Color(0.1f, 0.2f, 0.5f);
        [SerializeField] private Color cardHighlightColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color headerColor = new Color(0.15f, 0.15f, 0.3f);
        
        private Board currentBoard;
        private Camera mainCamera;
        private Canvas boardCanvas;
        private RectTransform boardContainer;
        private List<ClueCardUI3D> clueCards = new List<ClueCardUI3D>();
        private List<GameObject> categoryHeaders = new List<GameObject>();
        private Dictionary<Clue, ClueCardUI3D> clueToCardMap = new Dictionary<Clue, ClueCardUI3D>();
        
        // UI Colors
        private Color cardFrontColor = new Color(0.12f, 0.15f, 0.4f);
        private Color cardBackColor = new Color(0.85f, 0.75f, 0.45f);
        private Color headerBgColor = new Color(0.08f, 0.08f, 0.25f);
        
        // UI Canvas for overlay elements (scores, timer, etc.)
        private Canvas uiCanvas;
        private List<PlayerPanel> playerPanels = new List<PlayerPanel>();
        private ClueOverlayView clueOverlay;
        
        public IReadOnlyList<PlayerPanel> PlayerPanels => playerPanels;
        
        private static readonly int[] ROW_VALUES = { 200, 400, 600, 800, 1000 };
        private const int NUM_CATEGORIES = 6;
        private const int NUM_ROWS = 5;

        private void Start()
        {
            SetupCamera();
            CreateBoardCanvas();
            CreateUICanvas();
            
            if (GameManager.Instance != null && GameManager.Instance.CurrentBoard != null)
            {
                LoadBoard(GameManager.Instance.CurrentBoard);
                
                // Set up ClueSelectionController (reuses existing 2D game logic)
                SetupClueSelection(GameManager.Instance.CurrentBoard);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from ClueSelectionController events
            if (ClueSelectionController.Instance != null)
            {
                ClueSelectionController.Instance.OnClueSelected -= OnVoiceClueSelected;
                ClueSelectionController.Instance.OnSelectionCancelled -= OnVoiceSelectionCancelled;
            }
        }

        private void SetupClueSelection(Board board)
        {
            // Ensure ClueSelectionController exists (reuses existing component)
            if (ClueSelectionController.Instance == null)
            {
                GameObject selectionObj = new GameObject("[ClueSelectionController]");
                selectionObj.AddComponent<ClueSelectionController>();
            }

            // Set the board and subscribe to events
            ClueSelectionController.Instance.SetBoard(board);
            ClueSelectionController.Instance.OnClueSelected += OnVoiceClueSelected;
            ClueSelectionController.Instance.OnSelectionCancelled += OnVoiceSelectionCancelled;

            // Start the first selection prompt after a short delay
            StartCoroutine(StartFirstSelectionAfterDelay());
        }

        private System.Collections.IEnumerator StartFirstSelectionAfterDelay()
        {
            // Wait a moment for the board to render
            yield return new WaitForSeconds(0.5f);
            
            // Play category introduction sequence
            var board = GameManager.Instance?.CurrentBoard;
            if (board != null)
            {
                var introSequence = FindFirstObjectByType<UI.CategoryIntroSequence>();
                if (introSequence == null)
                {
                    var introObj = new GameObject("[CategoryIntroSequence]");
                    introSequence = introObj.AddComponent<UI.CategoryIntroSequence>();
                }
                
                bool introComplete = false;
                introSequence.PlayIntro(board, () => introComplete = true);
                
                while (!introComplete)
                {
                    yield return null;
                }
                
                yield return new WaitForSeconds(0.3f);
            }
            
            ClueSelectionController.Instance?.StartSelection();
        }

        private void OnVoiceClueSelected(int categoryIndex, int rowIndex)
        {
            Debug.Log($"[BoardController3D] Voice selected: category {categoryIndex}, row {rowIndex}");
            
            // Find and click the corresponding card
            SelectClueByIndex(categoryIndex, rowIndex);
        }

        private void OnVoiceSelectionCancelled()
        {
            Debug.Log("[BoardController3D] Voice selection cancelled or timed out");
            // User can click manually or voice will retry
        }

        public void SelectClueByIndex(int categoryIndex, int rowIndex)
        {
            if (currentBoard == null) return;
            if (categoryIndex < 0 || categoryIndex >= currentBoard.Categories.Count) return;
            
            var category = currentBoard.Categories[categoryIndex];
            if (rowIndex < 0 || rowIndex >= category.Clues.Count) return;
            
            Clue clue = category.Clues[rowIndex];
            if (clueToCardMap.TryGetValue(clue, out ClueCardUI3D card))
            {
                if (!card.IsUsed)
                {
                    OnClueCardClicked(card);
                }
            }
        }

        private void SetupCamera()
        {
            // Find or create main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            
            // Configure for 3D rendering (uses renderer index 1 if available)
            var renderer3DSetup = mainCamera.GetComponent<CameraRenderer3DSetup>();
            if (renderer3DSetup == null)
            {
                renderer3DSetup = mainCamera.gameObject.AddComponent<CameraRenderer3DSetup>();
            }
            
            mainCamera.backgroundColor = new Color(0.04f, 0.04f, 0.08f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateBoardCanvas()
        {
            // Create a Screen Space canvas for the game board
            GameObject canvasObj = new GameObject("BoardCanvas");
            boardCanvas = canvasObj.AddComponent<Canvas>();
            boardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            boardCanvas.sortingOrder = 10;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create board container
            GameObject containerObj = new GameObject("BoardContainer");
            containerObj.transform.SetParent(canvasObj.transform, false);
            boardContainer = containerObj.AddComponent<RectTransform>();
            boardContainer.anchorMin = new Vector2(0.5f, 0.55f);
            boardContainer.anchorMax = new Vector2(0.5f, 0.55f);
            boardContainer.anchoredPosition = Vector2.zero;
        }

        private void CreateUICanvas()
        {
            // Create screen-space overlay canvas for UI elements
            GameObject canvasObj = new GameObject("UI Canvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100;
            
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create player panels container at bottom
            CreatePlayerPanels();
            
            // Create clue overlay (hidden by default)
            CreateClueOverlay();
        }

        private void CreatePlayerPanels()
        {
            if (GameManager.Instance == null) return;
            
            GameObject panelContainer = new GameObject("PlayerPanels");
            panelContainer.transform.SetParent(uiCanvas.transform, false);
            
            RectTransform containerRect = panelContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, 120);
            
            HorizontalLayoutGroup layout = panelContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(100, 100, 10, 10);
            
            // Load player panel prefab
            GameObject prefab = Resources.Load<GameObject>("Prefabs/PlayerPanel");
            
            for (int i = 0; i < GameManager.Instance.Players.Count; i++)
            {
                GameObject panelObj;
                if (prefab != null)
                {
                    panelObj = Instantiate(prefab, panelContainer.transform);
                }
                else
                {
                    panelObj = CreatePlayerPanelFallback(panelContainer.transform);
                }
                
                PlayerPanel panel = panelObj.GetComponent<PlayerPanel>();
                if (panel != null)
                {
                    bool isChooser = (i == GameManager.Instance.CurrentChooserIndex);
                    panel.SetPlayer(GameManager.Instance.Players[i], isChooser);
                    playerPanels.Add(panel);
                }
            }
        }

        private GameObject CreatePlayerPanelFallback(Transform parent)
        {
            GameObject panel = new GameObject("PlayerPanel");
            panel.transform.SetParent(parent, false);
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.2f);
            
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            
            // Name text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Player";
            nameText.fontSize = 28;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            
            // Score text
            GameObject scoreObj = new GameObject("Score");
            scoreObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "$0";
            scoreText.fontSize = 36;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = Color.white;
            
            return panel;
        }

        private void CreateClueOverlay()
        {
            // Try to find existing ClueOverlayView or create new one
            clueOverlay = FindFirstObjectByType<ClueOverlayView>();
            if (clueOverlay == null)
            {
                GameObject overlayPrefab = Resources.Load<GameObject>("Prefabs/ClueOverlay");
                if (overlayPrefab != null)
                {
                    GameObject overlayObj = Instantiate(overlayPrefab, uiCanvas.transform);
                    clueOverlay = overlayObj.GetComponent<ClueOverlayView>();
                }
            }
        }

        public void LoadBoard(Board board)
        {
            currentBoard = board;
            ClearBoard();
            BuildBoard();
        }

        private void ClearBoard()
        {
            foreach (var card in clueCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            clueCards.Clear();
            clueToCardMap.Clear();
            
            foreach (var header in categoryHeaders)
            {
                if (header != null)
                    Destroy(header);
            }
            categoryHeaders.Clear();
            
            if (boardContainer != null)
            {
                Destroy(boardContainer);
            }
        }

        private void BuildBoard()
        {
            if (currentBoard == null) return;
            
            // UI-based layout (pixels)
            float uiCardWidth = 140f;
            float uiCardHeight = 80f;
            float uiCardSpacing = 8f;
            float uiHeaderHeight = 50f;
            
            float totalWidth = NUM_CATEGORIES * (uiCardWidth + uiCardSpacing) - uiCardSpacing;
            float totalHeight = uiHeaderHeight + uiCardSpacing + NUM_ROWS * (uiCardHeight + uiCardSpacing) - uiCardSpacing;
            
            float startX = -totalWidth / 2f + uiCardWidth / 2f;
            float startY = totalHeight / 2f - uiHeaderHeight / 2f;
            
            // Build category headers
            for (int c = 0; c < NUM_CATEGORIES && c < currentBoard.Categories.Count; c++)
            {
                float x = startX + c * (uiCardWidth + uiCardSpacing);
                float y = startY;
                
                GameObject header = CreateCategoryHeader(currentBoard.Categories[c].Title, x, y, uiCardWidth, uiHeaderHeight);
                categoryHeaders.Add(header);
            }
            
            // Build clue cards with staggered intro animation
            for (int c = 0; c < NUM_CATEGORIES && c < currentBoard.Categories.Count; c++)
            {
                var category = currentBoard.Categories[c];
                
                for (int r = 0; r < NUM_ROWS && r < category.Clues.Count; r++)
                {
                    float x = startX + c * (uiCardWidth + uiCardSpacing);
                    float y = startY - uiHeaderHeight - uiCardSpacing - r * (uiCardHeight + uiCardSpacing) - uiCardHeight / 2f;
                    
                    Clue clue = category.Clues[r];
                    ClueCardUI3D card = CreateClueCard(clue, category, x, y, ROW_VALUES[r], uiCardWidth, uiCardHeight);
                    clueCards.Add(card);
                    clueToCardMap[clue] = card;
                    
                    // Stagger the spin-in animation
                    float delay = c * 0.06f + r * 0.03f;
                    card.SpinIntoView(delay, 0.5f);
                }
            }
        }

        private GameObject CreateCategoryHeader(string title, float x, float y, float width, float height)
        {
            GameObject header = new GameObject("Header_" + title);
            header.transform.SetParent(boardContainer, false);
            
            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            
            // Background
            Image bg = header.AddComponent<Image>();
            bg.color = headerBgColor;
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(header.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2);
            textRect.offsetMax = new Vector2(-4, -2);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = title.ToUpper();
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8;
            tmp.fontSizeMax = 16;
            
            return header;
        }

        private ClueCardUI3D CreateClueCard(Clue clue, Category category, float x, float y, int value, float width, float height)
        {
            GameObject cardObj = new GameObject("Card_" + value);
            cardObj.transform.SetParent(boardContainer, false);
            
            RectTransform rect = cardObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            
            ClueCardUI3D card = cardObj.AddComponent<ClueCardUI3D>();
            card.Initialize(clue, category, value, width, height, cardFrontColor, cardBackColor);
            card.OnCardClicked += OnClueCardClicked;
            
            return card;
        }

        private void OnClueCardClicked(ClueCardUI3D card)
        {
            if (card.IsUsed) return;
            
            Debug.Log($"[BoardController3D] Card clicked: ${card.Value} - {card.Clue.Question.Substring(0, Math.Min(30, card.Clue.Question.Length))}...");
            
            // Mark card as used
            card.MarkAsUsed();
            
            // Animate card flip and reveal
            card.FlipAndReveal(() =>
            {
                // After animation, start the answer flow
                if (clueOverlay != null)
                {
                    clueOverlay.StartFlowOnly(card.Clue, card.Category.Title);
                }
            });
        }

        private void OnClueCompleted(Clue clue)
        {
            Debug.Log("[BoardController3D] Clue completed");
            UpdatePlayerPanels();
            
            if (GameManager.Instance != null && GameManager.Instance.IsRoundComplete())
            {
                Debug.Log("[BoardController3D] Round complete!");
            }
        }

        public void UpdatePlayerPanels()
        {
            if (GameManager.Instance == null) return;
            
            for (int i = 0; i < playerPanels.Count && i < GameManager.Instance.Players.Count; i++)
            {
                bool isChooser = (i == GameManager.Instance.CurrentChooserIndex);
                playerPanels[i].SetPlayer(GameManager.Instance.Players[i], isChooser);
            }
        }

        public void RefreshBoard()
        {
            // Cards are marked as used when clicked, no additional refresh needed
        }
    }
}
