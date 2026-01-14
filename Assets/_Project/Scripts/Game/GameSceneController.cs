using UnityEngine;
using Cerebrum.Data;
using Cerebrum.OpenAI;
using Cerebrum.UI;

namespace Cerebrum.Game
{
    public class GameSceneController : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private bool useRealData = true;
        [SerializeField] private bool showPreloadPrompt = false; // Disabled - HomeView now handles preloading

        private PreloadPromptUI preloadPrompt;
        private Board pendingBoard;

        private void Start()
        {
            if (useRealData)
            {
                InitializeWithRealData();
            }
            else
            {
                InitializeWithPlaceholderData();
            }
        }

        private void InitializeWithRealData()
        {
            // First check if HomeView already preloaded a board
            Board board = GameManager.Instance?.CurrentBoard;
            
            if (board != null)
            {
                Debug.Log("[GameSceneController] Using preloaded board from HomeView (audio already cached)");
                // Pass false to skip re-caching since HomeView already cached
                InitializeBoardAndStart(board, false);
                return;
            }
            
            // Fallback: load a new board if none was preloaded
            if (OptimizedCategoryLoader.Instance == null || !OptimizedCategoryLoader.Instance.IsLoaded)
            {
                Debug.LogError("[GameSceneController] OptimizedCategoryLoader not ready! Run 'Cerebrum > Preprocess Categories' first.");
                Debug.LogError("[GameSceneController] Falling back to placeholder data.");
                InitializeWithPlaceholderData();
                return;
            }

            board = OptimizedCategoryLoader.Instance.LoadRandomBoard(6);

            if (board == null)
            {
                Debug.LogError("[GameSceneController] Failed to create board! Falling back to placeholder data.");
                InitializeWithPlaceholderData();
                return;
            }

            // Show preload prompt or initialize directly
            if (showPreloadPrompt && TTSCache.Instance != null)
            {
                pendingBoard = board;
                ShowPreloadPrompt();
            }
            else
            {
                InitializeBoardAndStart(board, false);
            }
        }

        private void ShowPreloadPrompt()
        {
            // Create the prompt UI dynamically
            preloadPrompt = CreatePreloadPromptUI();
            preloadPrompt.OnChoiceMade += OnPreloadChoiceMade;
            preloadPrompt.Show();
        }

        private void OnPreloadChoiceMade(bool shouldPreload)
        {
            if (preloadPrompt != null)
            {
                preloadPrompt.OnChoiceMade -= OnPreloadChoiceMade;
                Destroy(preloadPrompt.gameObject);
                preloadPrompt = null;
            }

            InitializeBoardAndStart(pendingBoard, shouldPreload);
            pendingBoard = null;
        }

        private void InitializeBoardAndStart(Board board, bool preloadAudio)
        {
            if (boardController != null)
            {
                // Always enable reveal animation (audio is preloaded before game starts)
                boardController.SetUseRevealAnimation(true);
                // Hide board elements initially - will show after category intro
                boardController.InitializeWithBoard(board, showImmediately: false);
            }

            // Pre-cache TTS audio for all clues if chosen
            if (preloadAudio && TTSCache.Instance != null)
            {
                Debug.Log("[GameSceneController] Starting TTS pre-cache...");
                TTSCache.Instance.PreCacheBoard(board, () =>
                {
                    Debug.Log("[GameSceneController] TTS pre-cache complete!");
                });
            }

            // Set up ClueSelectionController and start listening for first pick
            SetupClueSelection(board);

            Debug.Log($"[GameSceneController] Game initialized (preload: {preloadAudio})");
        }

        private void SetupClueSelection(Board board)
        {
            // Ensure ClueSelectionController exists
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
            // Wait a moment for initialization
            yield return new WaitForSeconds(0.3f);
            
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
            }
            
            // Now show the board elements after category intro completes
            if (boardController != null)
            {
                boardController.ShowBoardElements();
            }
            
            yield return new WaitForSeconds(0.3f);
            
            ClueSelectionController.Instance?.StartSelection();
        }

        private void OnVoiceClueSelected(int categoryIndex, int rowIndex)
        {
            Debug.Log($"[GameSceneController] Voice selected: category {categoryIndex}, row {rowIndex}");
            
            // Trigger the clue selection on the board
            if (boardController != null)
            {
                boardController.SelectClueByIndex(categoryIndex, rowIndex);
            }
        }

        private void OnVoiceSelectionCancelled()
        {
            Debug.Log("[GameSceneController] Voice selection cancelled or timed out");
            // Could retry or let user click manually
        }

        private void OnDestroy()
        {
            if (ClueSelectionController.Instance != null)
            {
                ClueSelectionController.Instance.OnClueSelected -= OnVoiceClueSelected;
                ClueSelectionController.Instance.OnSelectionCancelled -= OnVoiceSelectionCancelled;
            }
        }

        private PreloadPromptUI CreatePreloadPromptUI()
        {
            // Create canvas for prompt
            GameObject canvasObj = new GameObject("PreloadPromptCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create panel background
            GameObject panelObj = new GameObject("PromptPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image panelBg = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelBg.color = new Color(0, 0, 0, 0.9f);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Create content container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(panelObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(500, 250);

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(contentObj.transform, false);
            TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = "Audio Preload";
            titleText.fontSize = 36;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.7f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Create description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(contentObj.transform, false);
            TMPro.TextMeshProUGUI descText = descObj.AddComponent<TMPro.TextMeshProUGUI>();
            descText.text = "Pre-cache audio for all clues?\nThis provides instant playback but takes time to load.";
            descText.fontSize = 20;
            descText.alignment = TMPro.TextAlignmentOptions.Center;
            descText.color = new Color(0.8f, 0.8f, 0.8f);
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.35f);
            descRect.anchorMax = new Vector2(1, 0.7f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            // Create buttons container
            GameObject buttonsObj = new GameObject("Buttons");
            buttonsObj.transform.SetParent(contentObj.transform, false);
            RectTransform buttonsRect = buttonsObj.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0, 0);
            buttonsRect.anchorMax = new Vector2(1, 0.35f);
            buttonsRect.offsetMin = Vector2.zero;
            buttonsRect.offsetMax = Vector2.zero;

            // Create Preload button
            GameObject preloadBtnObj = CreateButton(buttonsObj.transform, "Preload Audio", new Vector2(-120, 0));
            UnityEngine.UI.Button preloadBtn = preloadBtnObj.GetComponent<UnityEngine.UI.Button>();

            // Create Skip button
            GameObject skipBtnObj = CreateButton(buttonsObj.transform, "Skip", new Vector2(120, 0));
            UnityEngine.UI.Button skipBtn = skipBtnObj.GetComponent<UnityEngine.UI.Button>();

            // Add PreloadPromptUI component and set it up
            PreloadPromptUI prompt = canvasObj.AddComponent<PreloadPromptUI>();
            prompt.Setup(panelObj, preloadBtn, skipBtn);

            return prompt;
        }

        private GameObject CreateButton(Transform parent, string text, Vector2 position)
        {
            GameObject btnObj = new GameObject(text + "Button");
            btnObj.transform.SetParent(parent, false);
            
            UnityEngine.UI.Image btnImage = btnObj.AddComponent<UnityEngine.UI.Image>();
            btnImage.color = new Color(0.2f, 0.2f, 0.6f);
            
            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = btnImage;
            
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(200, 50);
            btnRect.anchoredPosition = position;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TMPro.TextMeshProUGUI btnText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 24;
            btnText.alignment = TMPro.TextAlignmentOptions.Center;
            btnText.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btnObj;
        }

        private void InitializeWithPlaceholderData()
        {
            if (boardController != null)
            {
                boardController.InitializeWithPlaceholderData();
            }

            Debug.Log("[GameSceneController] Game initialized with placeholder data");
        }
    }
}
