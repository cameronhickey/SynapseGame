using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Cerebrum.Data;
using Cerebrum.OpenAI;

namespace Cerebrum.Game
{
    public class ClueSelectionController : MonoBehaviour
    {
        public static ClueSelectionController Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float listenTimeoutSeconds = 8f;

        public event Action<int, int> OnClueSelected;
        public event Action OnSelectionCancelled;

        private Board currentBoard;
        private bool isListening;
        private Coroutine selectionCoroutine;

        private static readonly int[] ROW_VALUES = { 200, 400, 600, 800, 1000 };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private bool skipPlayerNameOnNextPrompt = false;

        public void SetBoard(Board board)
        {
            currentBoard = board;
            // Reset first selection flag when a new board is set (new game)
            isFirstSelection = true;
        }

        public void ResetFirstSelection()
        {
            isFirstSelection = true;
        }

        public void SetSkipPlayerName(bool skip)
        {
            skipPlayerNameOnNextPrompt = skip;
        }

        public void StartSelection()
        {
            if (currentBoard == null)
            {
                Debug.LogError("[ClueSelection] No board set");
                return;
            }

            if (isListening)
            {
                Debug.LogWarning("[ClueSelection] Already listening");
                return;
            }

            selectionCoroutine = StartCoroutine(SelectionFlowCoroutine());
        }

        public void CancelSelection()
        {
            if (selectionCoroutine != null)
            {
                StopCoroutine(selectionCoroutine);
                selectionCoroutine = null;
            }
            isListening = false;
            OnSelectionCancelled?.Invoke();
        }

        private bool isFirstSelection = true;

        private IEnumerator SelectionFlowCoroutine()
        {
            isListening = true;

            // Get current chooser name
            string chooserName = GameManager.Instance?.GetCurrentChooser()?.Name ?? "Player";
            bool shouldSkipName = skipPlayerNameOnNextPrompt;
            skipPlayerNameOnNextPrompt = false; // Reset after reading
            
            Debug.Log($"[ClueSelection] Asking {chooserName} to select a clue (first: {isFirstSelection}, skipName: {shouldSkipName})");

            // Play selection prompt
            bool promptComplete = false;
            if (PhrasePlayer.Instance != null)
            {
                if (isFirstSelection)
                {
                    // Use specific "first pick" category for game start
                    PhrasePlayer.Instance.PlaySelectCategoryFirst(chooserName, () => promptComplete = true);
                    isFirstSelection = false;
                }
                else if (shouldSkipName)
                {
                    // Skip player name - just say "your pick" or similar
                    PhrasePlayer.Instance.PlaySelectCategoryShort(() => promptComplete = true);
                }
                else
                {
                    // Use regular "select category" phrases for subsequent picks
                    PhrasePlayer.Instance.PlaySelectCategory(chooserName, () => promptComplete = true);
                }
            }
            else if (TTSService.Instance != null)
            {
                string prompt;
                if (isFirstSelection)
                {
                    prompt = $"{chooserName}, pick the first category.";
                    isFirstSelection = false;
                }
                else if (shouldSkipName)
                {
                    prompt = "Your pick.";
                }
                else
                {
                    prompt = $"{chooserName}, your pick.";
                }
                TTSService.Instance.Speak(prompt, () => promptComplete = true);
            }
            else
            {
                promptComplete = true;
                isFirstSelection = false;
            }

            while (!promptComplete)
            {
                yield return null;
            }

            // Listen for selection
            string transcript = null;
            bool transcriptionDone = false;

            STTService.Instance?.RecordAndTranscribe(
                (result) => { transcript = result; transcriptionDone = true; },
                (error) => { Debug.LogError($"[ClueSelection] STT error: {error}"); transcriptionDone = true; }
            );

            float startTime = Time.time;
            while (!transcriptionDone && Time.time - startTime < listenTimeoutSeconds)
            {
                yield return null;
            }

            isListening = false;

            if (string.IsNullOrEmpty(transcript))
            {
                Debug.Log("[ClueSelection] No response heard, cancelling");
                OnSelectionCancelled?.Invoke();
                yield break;
            }

            Debug.Log($"[ClueSelection] Heard: \"{transcript}\"");

            // Parse the selection
            var selection = ParseSelection(transcript);

            if (selection.HasValue)
            {
                int categoryIndex = selection.Value.categoryIndex;
                int rowIndex = selection.Value.rowIndex;

                Debug.Log($"[ClueSelection] Matched: {currentBoard.Categories[categoryIndex].Title} for ${ROW_VALUES[rowIndex]}");
                OnClueSelected?.Invoke(categoryIndex, rowIndex);
            }
            else
            {
                Debug.LogWarning($"[ClueSelection] Could not parse selection from: \"{transcript}\"");
                
                // Could retry or ask for clarification
                OnSelectionCancelled?.Invoke();
            }
        }

        public (int categoryIndex, int rowIndex)? ParseSelection(string transcript)
        {
            if (currentBoard == null || string.IsNullOrEmpty(transcript))
                return null;

            string input = transcript.ToLowerInvariant();

            // Extract dollar value
            int? value = ExtractValue(input);
            if (!value.HasValue)
            {
                Debug.Log("[ClueSelection] No value found in transcript");
                return null;
            }

            // Find matching row index
            int rowIndex = Array.IndexOf(ROW_VALUES, value.Value);
            if (rowIndex < 0)
            {
                Debug.Log($"[ClueSelection] Value ${value.Value} not in valid values");
                return null;
            }

            // Match category
            int? categoryIndex = MatchCategory(input);
            if (!categoryIndex.HasValue)
            {
                Debug.Log("[ClueSelection] No category matched");
                return null;
            }

            // Check if clue is available
            var category = currentBoard.Categories[categoryIndex.Value];
            bool clueAvailable = false;
            foreach (var clue in category.Clues)
            {
                if (clue.Value == value.Value && !clue.Used)
                {
                    clueAvailable = true;
                    break;
                }
            }

            if (!clueAvailable)
            {
                Debug.Log($"[ClueSelection] Clue already used: {category.Title} for ${value.Value}");
                return null;
            }

            return (categoryIndex.Value, rowIndex);
        }

        private int? ExtractValue(string input)
        {
            // Match patterns like "for 400", "for $400", "400", "$400", "four hundred"
            var patterns = new[]
            {
                @"\$?(\d{3,4})",                    // $400, 400, $1000, 1000
                @"for\s+\$?(\d{3,4})",              // for 400, for $400
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(input, pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
                {
                    return value;
                }
            }

            // Word-to-number mapping
            var wordValues = new Dictionary<string, int>
            {
                { "two hundred", 200 },
                { "four hundred", 400 },
                { "six hundred", 600 },
                { "eight hundred", 800 },
                { "one thousand", 1000 },
                { "a thousand", 1000 },
                { "thousand", 1000 },
            };

            foreach (var kvp in wordValues)
            {
                if (input.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private int? MatchCategory(string input)
        {
            if (currentBoard == null) return null;

            int bestMatch = -1;
            int bestScore = 0;

            for (int i = 0; i < currentBoard.Categories.Count; i++)
            {
                string categoryTitle = currentBoard.Categories[i].Title.ToLowerInvariant();
                int score = CalculateMatchScore(input, categoryTitle);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = i;
                }
            }

            // Require minimum match threshold
            if (bestScore >= 3)
            {
                return bestMatch;
            }

            return null;
        }

        private int CalculateMatchScore(string input, string categoryTitle)
        {
            int score = 0;

            // Exact match (highest priority)
            if (input.Contains(categoryTitle))
            {
                return 100;
            }

            // Split category into words and check each
            string[] categoryWords = categoryTitle.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in categoryWords)
            {
                if (word.Length < 3) continue;

                if (input.Contains(word))
                {
                    score += word.Length;
                }
                else
                {
                    // Check for partial match (at least 4 chars)
                    for (int len = Math.Min(word.Length, input.Length); len >= 4; len--)
                    {
                        string prefix = word.Substring(0, len);
                        if (input.Contains(prefix))
                        {
                            score += len;
                            break;
                        }
                    }
                }
            }

            return score;
        }
    }
}
