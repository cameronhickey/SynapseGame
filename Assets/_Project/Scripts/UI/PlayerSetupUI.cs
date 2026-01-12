using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cerebrum.UI
{
    public class PlayerSetupUI : MonoBehaviour
    {
        public static PlayerSetupUI Instance { get; private set; }

        [Header("Player Input Fields")]
        [SerializeField] private TMP_InputField player1Input;
        [SerializeField] private TMP_InputField player2Input;
        [SerializeField] private TMP_InputField player3Input;

        [Header("Buzz Key Labels")]
        [SerializeField] private TextMeshProUGUI player1KeyLabel;
        [SerializeField] private TextMeshProUGUI player2KeyLabel;
        [SerializeField] private TextMeshProUGUI player3KeyLabel;

        [Header("UI Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("Default Names")]
        [SerializeField] private string[] defaultNames = { "Player 1", "Player 2", "Player 3" };

        [Header("Buzz Keys")]
        [SerializeField] private string player1Key = "Z";
        [SerializeField] private string player2Key = "G";
        [SerializeField] private string player3Key = "M";

        public event Action<List<string>> OnPlayersConfirmed;
        public bool IsSetupComplete { get; private set; }

        #pragma warning disable CS0414
        private bool isPreloadingComplete;
        private bool isPlayerNamesReady;
        #pragma warning restore CS0414
        private float preloadProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetupUI();
            ShowSetup();
        }

        private void SetupUI()
        {
            // Set default names
            if (player1Input != null) player1Input.text = defaultNames.Length > 0 ? defaultNames[0] : "Player 1";
            if (player2Input != null) player2Input.text = defaultNames.Length > 1 ? defaultNames[1] : "Player 2";
            if (player3Input != null) player3Input.text = defaultNames.Length > 2 ? defaultNames[2] : "Player 3";

            // Set key labels
            if (player1KeyLabel != null) player1KeyLabel.text = $"({player1Key})";
            if (player2KeyLabel != null) player2KeyLabel.text = $"({player2Key})";
            if (player3KeyLabel != null) player3KeyLabel.text = $"({player3Key})";

            // Setup button
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }

            // Hide loading bar initially but show it will fill as preload progresses
            if (loadingBar != null)
            {
                loadingBar.value = 0;
                loadingBar.gameObject.SetActive(true);
            }

            if (loadingText != null)
            {
                loadingText.text = "Loading game data...";
            }
        }

        public void ShowSetup()
        {
            if (setupPanel != null)
            {
                setupPanel.SetActive(true);
            }
            IsSetupComplete = false;
        }

        public void HideSetup()
        {
            if (setupPanel != null)
            {
                setupPanel.SetActive(false);
            }
        }

        public void UpdatePreloadProgress(float progress, string message = null)
        {
            preloadProgress = progress;
            
            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (loadingText != null && !string.IsNullOrEmpty(message))
            {
                loadingText.text = message;
            }

            if (progress >= 1f)
            {
                isPreloadingComplete = true;
                if (loadingText != null)
                {
                    loadingText.text = "Ready to play!";
                }
            }

            UpdateStartButtonState();
        }

        public void SetPreloadComplete()
        {
            isPreloadingComplete = true;
            UpdatePreloadProgress(1f, "Ready to play!");
        }

        private void UpdateStartButtonState()
        {
            // Button is always enabled - we'll wait for preload when clicked if needed
            if (startButton != null)
            {
                startButton.interactable = true;
            }
        }

        private void OnStartClicked()
        {
            // Collect player names
            List<string> playerNames = new List<string>
            {
                string.IsNullOrWhiteSpace(player1Input?.text) ? defaultNames[0] : player1Input.text.Trim(),
                string.IsNullOrWhiteSpace(player2Input?.text) ? defaultNames[1] : player2Input.text.Trim(),
                string.IsNullOrWhiteSpace(player3Input?.text) ? defaultNames[2] : player3Input.text.Trim()
            };

            Debug.Log($"[PlayerSetupUI] Players: {string.Join(", ", playerNames)}");

            IsSetupComplete = true;
            OnPlayersConfirmed?.Invoke(playerNames);
        }

        public List<string> GetPlayerNames()
        {
            return new List<string>
            {
                string.IsNullOrWhiteSpace(player1Input?.text) ? defaultNames[0] : player1Input.text.Trim(),
                string.IsNullOrWhiteSpace(player2Input?.text) ? defaultNames[1] : player2Input.text.Trim(),
                string.IsNullOrWhiteSpace(player3Input?.text) ? defaultNames[2] : player3Input.text.Trim()
            };
        }

        public string GetBuzzKeyForPlayer(int playerIndex)
        {
            return playerIndex switch
            {
                0 => player1Key,
                1 => player2Key,
                2 => player3Key,
                _ => ""
            };
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
        }
    }
}
