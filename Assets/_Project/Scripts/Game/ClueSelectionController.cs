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
        [SerializeField] private int maxRetries = 9;  // 10 total attempts

        public event Action<int, int> OnClueSelected;
        public event Action OnSelectionCancelled;

        private Board currentBoard;
        private bool isListening;
        private Coroutine selectionCoroutine;
        private int lastSelectedCategoryIndex = -1;  // For "same category" support

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
            
            // Cancel any ongoing microphone recording
            if (MicrophoneRecorder.Instance != null && MicrophoneRecorder.Instance.IsRecording)
            {
                MicrophoneRecorder.Instance.CancelRecording();
            }
            
            isListening = false;
            // Don't invoke OnSelectionCancelled here - it's expected when a clue is selected
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

            // Retry loop for listening
            int attempts = 0;
            while (attempts <= maxRetries)
            {
                attempts++;
                
                // Listen for selection (auto-stops after detecting silence)
                string transcript = null;
                bool transcriptionDone = false;

                Debug.Log($"[ClueSelection] Starting voice recording (attempt {attempts}/{maxRetries + 1})...");
                STTService.Instance?.AutoRecordAndTranscribe(
                    (result) => { 
                        Debug.Log($"[ClueSelection] Transcription result: {result}");
                        transcript = result; 
                        transcriptionDone = true; 
                    },
                    (error) => { 
                        Debug.LogError($"[ClueSelection] STT error: {error}"); 
                        transcriptionDone = true; 
                    }
                );

                float startTime = Time.time;
                while (!transcriptionDone && Time.time - startTime < listenTimeoutSeconds)
                {
                    yield return null;
                }

                // If still recording after timeout, cancel it
                if (!transcriptionDone && MicrophoneRecorder.Instance != null && MicrophoneRecorder.Instance.IsRecording)
                {
                    Debug.Log("[ClueSelection] Timeout - cancelling recording");
                    MicrophoneRecorder.Instance.CancelRecording();
                }

                if (string.IsNullOrEmpty(transcript))
                {
                    Debug.Log("[ClueSelection] No response heard");
                    if (attempts <= maxRetries)
                    {
                        // Prompt for retry
                        yield return PlayRetryPrompt("I didn't catch that. Please try again.");
                    }
                    continue;
                }

                Debug.Log($"[ClueSelection] Heard: \"{transcript}\"");

                // Try regex-based parsing first (fast, on-device)
                var selection = ParseSelection(transcript);

                // If regex failed, try LLM fallback
                if (!selection.HasValue)
                {
                    Debug.Log("[ClueSelection] Regex parsing failed, trying LLM fallback...");
                    yield return TryLLMParsing(transcript, (result) => selection = result);
                }

                if (selection.HasValue)
                {
                    int categoryIndex = selection.Value.categoryIndex;
                    int rowIndex = selection.Value.rowIndex;

                    Debug.Log($"[ClueSelection] Matched: {currentBoard.Categories[categoryIndex].Title} for ${ROW_VALUES[rowIndex]}");
                    lastSelectedCategoryIndex = categoryIndex;  // Remember for "same category"
                    isListening = false;
                    OnClueSelected?.Invoke(categoryIndex, rowIndex);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[ClueSelection] Could not parse selection from: \"{transcript}\"");
                    
                    if (attempts <= maxRetries)
                    {
                        // Prompt for retry with hint
                        yield return PlayRetryPrompt("I couldn't understand. Please say a category and dollar amount.");
                    }
                }
            }

            // All retries exhausted
            isListening = false;
            Debug.Log("[ClueSelection] Max retries reached, cancelling");
            OnSelectionCancelled?.Invoke();
        }

        private IEnumerator PlayRetryPrompt(string message)
        {
            bool done = false;
            if (TTSService.Instance != null)
            {
                TTSService.Instance.Speak(message, () => done = true);
                while (!done) yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator TryLLMParsing(string transcript, Action<(int categoryIndex, int rowIndex)?> onResult)
        {
            if (currentBoard == null || OpenAIClient.Instance == null)
            {
                onResult(null);
                yield break;
            }

            // Build category list for the prompt
            var categoryList = new List<string>();
            for (int i = 0; i < currentBoard.Categories.Count; i++)
            {
                categoryList.Add($"{i + 1}. {currentBoard.Categories[i].Title}");
            }

            // Build available values list
            var availableValues = string.Join(", ", ROW_VALUES);

            string systemPrompt = $@"You parse spoken trivia category selections. The input is from speech recognition and may have transcription errors.

Categories:
{string.Join("\n", categoryList)}

Valid dollar amounts: {availableValues}

IMPORTANT: Use fuzzy matching! The spoken word may be transcribed incorrectly:
- ""Comments"" might mean ""Comedies""
- ""Signs"" might mean ""Science""  
- ""Hipster"" might mean ""History""
- Single digits like ""4"" mean ""400"", ""6"" means ""600"", etc.

If ""same"", ""again"", or ""more"" is mentioned, use category index {lastSelectedCategoryIndex + 1}.

Find the BEST matching category even if not exact. Return JSON:
{{""category_index"": <1-indexed>, ""value"": <dollar amount>}}

Only return {{""error"": ""cannot parse""}} if truly impossible to determine.";

            string userMessage = transcript;

            bool done = false;
            string response = null;

            OpenAIClient.Instance.PostChat(systemPrompt, userMessage,
                (result) => { response = result; done = true; },
                (error) => { Debug.LogError($"[ClueSelection] LLM error: {error}"); done = true; }
            );

            float timeout = 5f;
            float elapsed = 0f;
            while (!done && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (string.IsNullOrEmpty(response))
            {
                Debug.Log("[ClueSelection] LLM returned no response");
                onResult(null);
                yield break;
            }

            Debug.Log($"[ClueSelection] LLM response: {response}");

            // Parse JSON response
            try
            {
                // Simple JSON parsing (avoid external dependencies)
                if (response.Contains("error"))
                {
                    onResult(null);
                    yield break;
                }

                var categoryMatch = Regex.Match(response, @"""category_index""\s*:\s*(\d+)");
                var valueMatch = Regex.Match(response, @"""value""\s*:\s*(\d+)");

                if (categoryMatch.Success && valueMatch.Success)
                {
                    int categoryIndex = int.Parse(categoryMatch.Groups[1].Value) - 1; // Convert to 0-indexed
                    int value = int.Parse(valueMatch.Groups[1].Value);
                    int rowIndex = Array.IndexOf(ROW_VALUES, value);

                    if (categoryIndex >= 0 && categoryIndex < currentBoard.Categories.Count && rowIndex >= 0)
                    {
                        // Verify clue is available
                        var category = currentBoard.Categories[categoryIndex];
                        bool available = false;
                        foreach (var clue in category.Clues)
                        {
                            if (clue.Value == value && !clue.Used)
                            {
                                available = true;
                                break;
                            }
                        }

                        if (available)
                        {
                            Debug.Log($"[ClueSelection] LLM parsed: category {categoryIndex}, value {value}");
                            onResult((categoryIndex, rowIndex));
                            yield break;
                        }
                        else
                        {
                            Debug.Log($"[ClueSelection] LLM selection not available: {category.Title} for ${value}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClueSelection] Error parsing LLM response: {e.Message}");
            }

            onResult(null);
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
            // Match patterns like "for 400", "for $400", "400", "$400"
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

            // Single digit shorthand: "2" -> 200, "4" -> 400, etc.
            var singleDigitMatch = Regex.Match(input, @"\bfor\s+(\d)\b|\b(\d)\s*hundred|\b(\d)\b");
            if (singleDigitMatch.Success)
            {
                string digit = singleDigitMatch.Groups[1].Value;
                if (string.IsNullOrEmpty(digit)) digit = singleDigitMatch.Groups[2].Value;
                if (string.IsNullOrEmpty(digit)) digit = singleDigitMatch.Groups[3].Value;
                
                if (int.TryParse(digit, out int d))
                {
                    // Map single digits to values
                    if (d >= 2 && d <= 10)
                    {
                        int mapped = d * 100;
                        if (Array.IndexOf(ROW_VALUES, mapped) >= 0)
                            return mapped;
                    }
                }
            }

            // Word-to-number mapping
            var wordValues = new Dictionary<string, int>
            {
                { "two hundred", 200 }, { "two", 200 },
                { "four hundred", 400 }, { "four", 400 },
                { "six hundred", 600 }, { "six", 600 },
                { "eight hundred", 800 }, { "eight", 800 },
                { "one thousand", 1000 }, { "ten", 1000 },
                { "a thousand", 1000 }, { "thousand", 1000 },
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

            // Check for "same category" / "same" / "again" references
            if (lastSelectedCategoryIndex >= 0 && lastSelectedCategoryIndex < currentBoard.Categories.Count)
            {
                if (input.Contains("same") || input.Contains("again") || input.Contains("more"))
                {
                    Debug.Log($"[ClueSelection] Using same category: {currentBoard.Categories[lastSelectedCategoryIndex].Title}");
                    return lastSelectedCategoryIndex;
                }
            }

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

            // Require minimum match threshold (lowered from 3 to 2 for single-word matches)
            if (bestScore >= 2)
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
