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
    public class BoardController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform categoryHeaderContainer;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Transform playerPanelContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject categoryHeaderPrefab;
        [SerializeField] private GameObject clueButtonPrefab;
        [SerializeField] private GameObject playerPanelPrefab;

        [Header("Prefab Paths (fallback)")]
        [SerializeField] private string categoryHeaderPrefabPath = "Prefabs/CategoryHeader";
        [SerializeField] private string clueButtonPrefabPath = "Prefabs/ClueButton";
        [SerializeField] private string playerPanelPrefabPath = "Prefabs/PlayerPanel";

        [Header("Overlay")]
        [SerializeField] private ClueOverlayView clueOverlay;

        [Header("Reveal Animation")]
        [SerializeField] private bool useRevealAnimation = true;
        private ClueRevealAnimator clueRevealAnimator;

        private GameObject backgroundImage;
        private Board currentBoard;
        private List<ClueButton> clueButtons = new List<ClueButton>();
        private List<PlayerPanel> playerPanels = new List<PlayerPanel>();
        private List<TextMeshProUGUI> categoryHeaders = new List<TextMeshProUGUI>();
        private Dictionary<Clue, ClueButton> clueToButtonMap = new Dictionary<Clue, ClueButton>();

        public IReadOnlyList<PlayerPanel> PlayerPanels => playerPanels;

        private static readonly int[] ROW_VALUES = { 200, 400, 600, 800, 1000 };
        private const int NUM_CATEGORIES = 6;
        private const int NUM_ROWS = 5;

        private void Awake()
        {
            LoadPrefabsIfNeeded();
            
            // Force reveal animation on (scene may have old serialized value)
            useRevealAnimation = true;
        }

        private void Start()
        {
            if (clueOverlay != null)
            {
                clueOverlay.OnClueCompleted += OnClueCompleted;
            }

            // Find or create ClueRevealAnimator
            if (useRevealAnimation)
            {
                clueRevealAnimator = FindFirstObjectByType<ClueRevealAnimator>();
                if (clueRevealAnimator == null)
                {
                    GameObject animatorObj = new GameObject("ClueRevealAnimator");
                    clueRevealAnimator = animatorObj.AddComponent<ClueRevealAnimator>();
                }
            }
            
            // Subscribe to buzz events (may need to wait for AnswerFlowController)
            StartCoroutine(SubscribeToBuzzEvents());
        }
        
        private System.Collections.IEnumerator SubscribeToBuzzEvents()
        {
            // Wait until AnswerFlowController is available
            while (AnswerFlowController.Instance == null)
            {
                yield return null;
            }
            
            Debug.Log("[BoardController] Subscribing to buzz events");
            AnswerFlowController.Instance.OnPlayerBuzzed += OnPlayerBuzzed;
            AnswerFlowController.Instance.OnFlowComplete += OnAnswerFlowComplete;
            AnswerFlowController.Instance.OnResponseTimerExpired += OnResponseTimerExpired;
        }

        private void LoadPrefabsIfNeeded()
        {
            if (categoryHeaderPrefab == null)
            {
                categoryHeaderPrefab = Resources.Load<GameObject>(categoryHeaderPrefabPath);
            }
            if (clueButtonPrefab == null)
            {
                clueButtonPrefab = Resources.Load<GameObject>(clueButtonPrefabPath);
            }
            if (playerPanelPrefab == null)
            {
                playerPanelPrefab = Resources.Load<GameObject>(playerPanelPrefabPath);
            }
        }

        public void SetUseRevealAnimation(bool useAnimation)
        {
            useRevealAnimation = useAnimation;
        }

        public void InitializeWithPlaceholderData()
        {
            currentBoard = CreatePlaceholderBoard();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetBoard(currentBoard);
                GameManager.Instance.SetState(Core.GameState.BoardMode);
            }

            BuildUI();
        }

        public void InitializeWithBoard(Board board, bool showImmediately = true)
        {
            currentBoard = board;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetBoard(currentBoard);
                GameManager.Instance.SetState(Core.GameState.BoardMode);
            }

            BuildUI();
            
            // Optionally hide board elements until ShowBoardElements is called
            if (!showImmediately)
            {
                SetBoardElementsVisible(false);
            }
        }

        public void SetBoardElementsVisible(bool visible)
        {
            // Toggle visibility of board containers
            if (categoryHeaderContainer != null)
                categoryHeaderContainer.gameObject.SetActive(visible);
            if (gridContainer != null)
                gridContainer.gameObject.SetActive(visible);
            if (playerPanelContainer != null)
                playerPanelContainer.gameObject.SetActive(visible);
        }

        public void ShowBoardElements()
        {
            SetBoardElementsVisible(true);
        }

        private Board CreatePlaceholderBoard()
        {
            Board board = new Board();

            string[] placeholderCategories = {
                "SCIENCE", "HISTORY", "GEOGRAPHY",
                "MOVIES", "LITERATURE", "SPORTS"
            };

            for (int c = 0; c < NUM_CATEGORIES; c++)
            {
                Category category = new Category(placeholderCategories[c]);

                for (int r = 0; r < NUM_ROWS; r++)
                {
                    Clue clue = new Clue(
                        $"This is a placeholder question for {placeholderCategories[c]} worth ${ROW_VALUES[r]}",
                        $"Placeholder answer for {placeholderCategories[c]}",
                        ROW_VALUES[r]
                    );
                    category.Clues.Add(clue);
                }

                board.Categories.Add(category);
            }

            return board;
        }

        private void BuildUI()
        {
            ClearUI();
            AdjustLayoutForTitle();
            CreateBackgroundImage();
            BuildCategoryHeaders();
            BuildClueGrid();
            BuildPlayerPanels();
        }

        private void AdjustLayoutForTitle()
        {
            // Find the MainLayout and adjust top padding to reveal title
            if (categoryHeaderContainer != null)
            {
                Transform mainLayout = categoryHeaderContainer.parent;
                if (mainLayout != null)
                {
                    VerticalLayoutGroup vlg = mainLayout.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null)
                    {
                        // Set top padding to 150 to reveal the game title in background
                        vlg.padding = new RectOffset(20, 20, 150, 20);
                    }
                }
            }
        }

        private void CreateBackgroundImage()
        {
            // Create a separate canvas for the background with lower sort order
            // This ensures it renders behind all other UI elements
            backgroundImage = new GameObject("GameBackgroundCanvas");
            
            // Add Canvas component with sort order -1 (renders before main canvas at 0)
            Canvas bgCanvas = backgroundImage.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            bgCanvas.sortingOrder = -1;
            
            // Add CanvasScaler to match screen
            var scaler = backgroundImage.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Create the actual background image as child
            GameObject imageObj = new GameObject("BackgroundImage");
            imageObj.transform.SetParent(backgroundImage.transform, false);
            
            RectTransform bgRect = imageObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = imageObj.AddComponent<Image>();
            Sprite bgSprite = Resources.Load<Sprite>("Images/Cerebrum_Game_Background");
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.preserveAspect = false;
            }
            else
            {
                bgImage.color = new Color(0.02f, 0.02f, 0.08f, 1f);
                Debug.LogWarning("[BoardController] Background image not found at Resources/Images/Cerebrum_Game_Background");
            }
            
            bgImage.raycastTarget = false;
            Debug.Log("[BoardController] Background canvas created with sortingOrder -1");
        }

        private void ClearUI()
        {
            // Clean up background
            if (backgroundImage != null)
            {
                Destroy(backgroundImage);
                backgroundImage = null;
            }

            foreach (var header in categoryHeaders)
            {
                if (header != null)
                    Destroy(header.gameObject);
            }
            categoryHeaders.Clear();

            foreach (var button in clueButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            clueButtons.Clear();

            foreach (var panel in playerPanels)
            {
                if (panel != null)
                    Destroy(panel.gameObject);
            }
            playerPanels.Clear();
        }

        private void BuildCategoryHeaders()
        {
            if (categoryHeaderContainer == null || categoryHeaderPrefab == null)
            {
                Debug.LogWarning("[BoardController] Category header container or prefab not set");
                return;
            }

            // Configure HorizontalLayoutGroup to respect LayoutElement sizes
            HorizontalLayoutGroup hlg = categoryHeaderContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childForceExpandWidth = false;  // Don't force expand - use LayoutElement
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = false;  // Don't control - let LayoutElement/RectTransform handle it
                hlg.childControlHeight = false;
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }
            
            // Ensure container RectTransform is tall enough
            RectTransform containerRect = categoryHeaderContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, 120);
            }

            for (int c = 0; c < NUM_CATEGORIES; c++)
            {
                GameObject headerObj = Instantiate(categoryHeaderPrefab, categoryHeaderContainer);
                TextMeshProUGUI headerText = headerObj.GetComponentInChildren<TextMeshProUGUI>();

                if (headerText != null)
                {
                    headerText.text = currentBoard.Categories[c].Title;
                    
                    // Apply Bebas Neue font with drop shadow
                    UI.FontManager.EnsureExists();
                    if (UI.FontManager.Instance != null)
                    {
                        UI.FontManager.Instance.ApplyCategoryStyle(headerText);
                    }
                    
                    // Enable auto-sizing for category headers
                    headerText.enableAutoSizing = true;
                    headerText.fontSizeMin = 14;
                    headerText.fontSizeMax = 32;
                    headerText.alignment = TextAlignmentOptions.Center;
                    
                    // Add margin/padding so text doesn't touch edges
                    headerText.margin = new Vector4(10, 8, 10, 8);  // left, top, right, bottom
                }

                // Set size directly on RectTransform
                RectTransform headerRect = headerObj.GetComponent<RectTransform>();
                if (headerRect != null)
                {
                    headerRect.sizeDelta = new Vector2(280, 120);  // Match grid cell size
                }

                // Add LayoutElement to enforce size in layout group
                LayoutElement layoutElement = headerObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = headerObj.AddComponent<LayoutElement>();
                }
                layoutElement.preferredWidth = 280;
                layoutElement.preferredHeight = 120;
                layoutElement.minHeight = 120;  // Force minimum height
                layoutElement.flexibleWidth = 0;
                layoutElement.flexibleHeight = 0;

                categoryHeaders.Add(headerText);
            }
        }

        private void BuildClueGrid()
        {
            if (gridContainer == null || clueButtonPrefab == null)
            {
                Debug.LogWarning("[BoardController] Grid container or clue button prefab not set");
                return;
            }

            clueToButtonMap.Clear();

            for (int r = 0; r < NUM_ROWS; r++)
            {
                for (int c = 0; c < NUM_CATEGORIES; c++)
                {
                    GameObject buttonObj = Instantiate(clueButtonPrefab, gridContainer);
                    ClueButton clueButton = buttonObj.GetComponent<ClueButton>();

                    if (clueButton != null)
                    {
                        // Find the associated clue
                        Clue associatedClue = null;
                        if (currentBoard != null && c < currentBoard.Categories.Count)
                        {
                            foreach (var clue in currentBoard.Categories[c].Clues)
                            {
                                if (clue.Value == ROW_VALUES[r])
                                {
                                    associatedClue = clue;
                                    break;
                                }
                            }
                        }

                        clueButton.Initialize(c, r, ROW_VALUES[r], false);
                        clueButton.SetAssociatedClue(associatedClue);
                        clueButton.OnClueSelected += OnClueSelected;
                        clueButtons.Add(clueButton);

                        // Map clue to button for reveal callbacks
                        if (associatedClue != null)
                        {
                            clueToButtonMap[associatedClue] = clueButton;
                        }
                    }
                }
            }
        }


        private void BuildPlayerPanels()
        {
            if (playerPanelContainer == null || playerPanelPrefab == null || GameManager.Instance == null)
            {
                Debug.LogWarning("[BoardController] Player panel container, prefab, or GameManager not set");
                return;
            }

            for (int i = 0; i < GameManager.Instance.Players.Count; i++)
            {
                GameObject panelObj = Instantiate(playerPanelPrefab, playerPanelContainer);
                PlayerPanel panel = panelObj.GetComponent<PlayerPanel>();

                if (panel != null)
                {
                    bool isChooser = (i == GameManager.Instance.CurrentChooserIndex);
                    panel.SetPlayer(GameManager.Instance.Players[i], isChooser, i);
                    playerPanels.Add(panel);
                    Debug.Log($"[BoardController] Created player panel {i}: {GameManager.Instance.Players[i].Name}");
                }
            }
            Debug.Log($"[BoardController] Built {playerPanels.Count} player panels");
        }

        /// <summary>
        /// Public method to select a clue by index (for voice selection)
        /// </summary>
        public void SelectClueByIndex(int categoryIndex, int rowIndex)
        {
            OnClueSelected(categoryIndex, rowIndex);
        }

        private void OnClueSelected(int categoryIndex, int rowIndex)
        {
            if (currentBoard == null) return;

            // Cancel any ongoing voice selection to free the microphone for answer recording
            ClueSelectionController.Instance?.CancelSelection();

            Category category = currentBoard.Categories[categoryIndex];
            Clue clue = null;

            foreach (var c in category.Clues)
            {
                if (c.Value == ROW_VALUES[rowIndex] && !c.Used)
                {
                    clue = c;
                    break;
                }
            }

            if (clue == null)
            {
                Debug.LogWarning($"[BoardController] No clue found for category {categoryIndex}, row {rowIndex}");
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetActiveClue(clue, categoryIndex, rowIndex);
            }

            // Find the button that was clicked for animation source
            ClueButton sourceButton = null;
            bool foundInMap = clueToButtonMap.TryGetValue(clue, out sourceButton);
            
            Debug.Log($"[BoardController] OnClueSelected: useRevealAnimation={useRevealAnimation}, animator={clueRevealAnimator != null}, foundInMap={foundInMap}, sourceButton={sourceButton != null}");
            
            if (foundInMap && sourceButton != null)
            {
                // Use reveal animation if enabled
                if (useRevealAnimation && clueRevealAnimator != null)
                {
                    RectTransform buttonRect = sourceButton.GetComponent<RectTransform>();
                    Debug.Log($"[BoardController] Starting reveal animation for clue: {clue.Question.Substring(0, Math.Min(30, clue.Question.Length))}...");
                    
                    // Hide the source button immediately so it doesn't show when card animates back
                    sourceButton.MarkAsUsed();
                    
                    clueRevealAnimator.RevealClue(buttonRect, clue.Question, clue.Value, () =>
                    {
                        // Animation complete - card stays visible as the clue display
                        Debug.Log("[BoardController] Reveal animation complete, starting answer flow");
                        
                        // Start the answer flow (audio, buzzing) without showing the old overlay UI
                        if (clueOverlay != null)
                        {
                            clueOverlay.StartFlowOnly(clue, category.Title);
                        }
                    });
                    return;
                }
            }

            // Fallback: show overlay directly without animation
            Debug.Log("[BoardController] Fallback: showing overlay without animation");
            if (clueOverlay != null)
            {
                clueOverlay.Show(clue, category.Title);
            }
        }

        private void OnClueCompleted()
        {
            ClearAllBuzzStates();
            RefreshBoard();
            UpdatePlayerPanels();

            if (GameManager.Instance != null && GameManager.Instance.IsRoundComplete())
            {
                Debug.Log("[BoardController] Round complete! Starting winner sequence...");
                StartCoroutine(ShowWinnerAfterDelay());
            }
            else
            {
                // Start next clue selection after a short delay
                StartCoroutine(StartNextSelectionAfterDelay());
            }
        }
        
        private System.Collections.IEnumerator ShowWinnerAfterDelay()
        {
            yield return new WaitForSeconds(1.5f);
            
            // Create GameWinnerUI if it doesn't exist
            if (GameWinnerUI.Instance == null)
            {
                GameObject winnerObj = new GameObject("GameWinnerUI");
                winnerObj.AddComponent<GameWinnerUI>();
            }
            
            GameWinnerUI.Instance?.ShowWinner();
        }

        private System.Collections.IEnumerator StartNextSelectionAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            ClueSelectionController.Instance?.StartSelection();
        }

        public void RefreshBoard()
        {
            foreach (var button in clueButtons)
            {
                if (button == null) continue;

                Category category = currentBoard.Categories[button.CategoryIndex];
                foreach (var clue in category.Clues)
                {
                    if (clue.Value == ROW_VALUES[button.RowIndex] && clue.Used)
                    {
                        button.MarkAsUsed();
                        break;
                    }
                }
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

        private void OnDestroy()
        {
            if (clueOverlay != null)
            {
                clueOverlay.OnClueCompleted -= OnClueCompleted;
            }

            foreach (var button in clueButtons)
            {
                if (button != null)
                {
                    button.OnClueSelected -= OnClueSelected;
                }
            }
            
            if (AnswerFlowController.Instance != null)
            {
                AnswerFlowController.Instance.OnPlayerBuzzed -= OnPlayerBuzzed;
                AnswerFlowController.Instance.OnFlowComplete -= OnAnswerFlowComplete;
                AnswerFlowController.Instance.OnResponseTimerExpired -= OnResponseTimerExpired;
            }
        }
        
        private void OnResponseTimerExpired()
        {
            // Clear buzz highlight when response timer expires (player ran out of time to answer)
            ClearAllBuzzStates();
        }
        
        private void OnPlayerBuzzed(int playerIndex)
        {
            Debug.Log($"[BoardController] OnPlayerBuzzed called: playerIndex={playerIndex}, playerPanels.Count={playerPanels.Count}");
            
            // Clear any previous buzz states first
            ClearAllBuzzStates();
            
            // Highlight the buzzed player
            if (playerIndex >= 0 && playerIndex < playerPanels.Count)
            {
                Debug.Log($"[BoardController] Setting buzz highlight on panel {playerIndex}");
                playerPanels[playerIndex].SetBuzzedIn(true);
            }
            else
            {
                Debug.LogWarning($"[BoardController] Cannot highlight player {playerIndex} - panels not ready or index out of range");
            }
        }
        
        private void OnAnswerFlowComplete()
        {
            ClearAllBuzzStates();
        }
        
        private void ClearAllBuzzStates()
        {
            foreach (var panel in playerPanels)
            {
                if (panel != null)
                {
                    panel.ClearBuzzState();
                }
            }
        }
    }
}
