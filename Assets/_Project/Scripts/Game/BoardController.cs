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

        private Board currentBoard;
        private List<ClueButton> clueButtons = new List<ClueButton>();
        private List<PlayerPanel> playerPanels = new List<PlayerPanel>();
        private List<TextMeshProUGUI> categoryHeaders = new List<TextMeshProUGUI>();
        private Dictionary<Clue, ClueButton> clueToButtonMap = new Dictionary<Clue, ClueButton>();

        private static readonly int[] ROW_VALUES = { 200, 400, 600, 800, 1000 };
        private const int NUM_CATEGORIES = 6;
        private const int NUM_ROWS = 5;

        private void Awake()
        {
            LoadPrefabsIfNeeded();
        }

        private void Start()
        {
            if (clueOverlay != null)
            {
                clueOverlay.OnClueCompleted += OnClueCompleted;
            }

            // Subscribe to TTS cache events for reveal animation
            if (useRevealAnimation && TTSCache.Instance != null)
            {
                TTSCache.Instance.OnClueReady += OnClueAudioReady;
            }
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

        public void InitializeWithBoard(Board board)
        {
            currentBoard = board;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetBoard(currentBoard);
                GameManager.Instance.SetState(Core.GameState.BoardMode);
            }

            BuildUI();
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
            BuildCategoryHeaders();
            BuildClueGrid();
            BuildPlayerPanels();
        }

        private void ClearUI()
        {
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

            for (int c = 0; c < NUM_CATEGORIES; c++)
            {
                GameObject headerObj = Instantiate(categoryHeaderPrefab, categoryHeaderContainer);
                TextMeshProUGUI headerText = headerObj.GetComponentInChildren<TextMeshProUGUI>();

                if (headerText != null)
                {
                    headerText.text = currentBoard.Categories[c].Title;
                }

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

                        // Start hidden if using reveal animation
                        bool startHidden = useRevealAnimation && TTSCache.Instance != null;
                        clueButton.Initialize(c, r, ROW_VALUES[r], startHidden);
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

        private void OnClueAudioReady(Clue clue)
        {
            if (clue != null && clueToButtonMap.TryGetValue(clue, out ClueButton button))
            {
                button.RevealWithAnimation();
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
                    panel.SetPlayer(GameManager.Instance.Players[i], isChooser);
                    playerPanels.Add(panel);
                }
            }
        }

        private void OnClueSelected(int categoryIndex, int rowIndex)
        {
            if (currentBoard == null) return;

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

            if (clueOverlay != null)
            {
                clueOverlay.Show(clue, category.Title);
            }
        }

        private void OnClueCompleted()
        {
            RefreshBoard();
            UpdatePlayerPanels();

            if (GameManager.Instance != null && GameManager.Instance.IsRoundComplete())
            {
                Debug.Log("[BoardController] Round complete!");
            }
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

            if (TTSCache.Instance != null)
            {
                TTSCache.Instance.OnClueReady -= OnClueAudioReady;
            }

            foreach (var button in clueButtons)
            {
                if (button != null)
                {
                    button.OnClueSelected -= OnClueSelected;
                }
            }
        }
    }
}
