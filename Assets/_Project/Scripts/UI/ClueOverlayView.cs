using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;
using Cerebrum.Game;
using Cerebrum.OpenAI;

namespace Cerebrum.UI
{
    public class ClueOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI answerText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI transcriptText;
        [SerializeField] private Button revealAnswerButton;
        [SerializeField] private Button backToBoardButton;

        [Header("TTS Settings")]
        [SerializeField] private bool enableTTS = true;

        [Header("Answer Flow")]
        [SerializeField] private bool enableAnswerFlow = true;

        public System.Action OnClueCompleted;
        public System.Action OnQuestionReadComplete;

        private Clue currentClue;
        private string currentCategory;
        private bool isReadingQuestion;
        private bool someoneAnsweredCorrectly;
        private AnswerFlowController answerFlow;

        private void Awake()
        {
            if (revealAnswerButton != null)
            {
                revealAnswerButton.onClick.AddListener(OnRevealAnswerClicked);
            }

            if (backToBoardButton != null)
            {
                backToBoardButton.onClick.AddListener(OnBackToBoardClicked);
            }

            Hide();
        }

        private void Start()
        {
            answerFlow = FindFirstObjectByType<AnswerFlowController>();
            if (answerFlow == null && enableAnswerFlow)
            {
                GameObject flowObj = new GameObject("AnswerFlowController");
                answerFlow = flowObj.AddComponent<AnswerFlowController>();
            }

            if (answerFlow != null)
            {
                answerFlow.OnStateChanged += OnAnswerFlowStateChanged;
                answerFlow.OnPlayerBuzzed += OnPlayerBuzzed;
                answerFlow.OnTranscriptReady += OnTranscriptReady;
                answerFlow.OnJudgmentReady += OnJudgmentReady;
                answerFlow.OnFlowComplete += OnAnswerFlowComplete;
            }

            // Create question timer bar if it doesn't exist
            if (FindFirstObjectByType<QuestionTimerBar>() == null)
            {
                GameObject timerBarObj = new GameObject("QuestionTimerBar");
                timerBarObj.AddComponent<QuestionTimerBar>();
            }
        }

        public void Show(Clue clue, string categoryTitle)
        {
            currentClue = clue;
            currentCategory = categoryTitle;
            isReadingQuestion = false;

            // Hide the ClueRevealAnimator card when overlay shows
            var clueAnimator = FindFirstObjectByType<ClueRevealAnimator>();
            if (clueAnimator != null)
            {
                clueAnimator.HideClue();
            }

            if (categoryText != null)
            {
                categoryText.text = categoryTitle;
            }

            if (valueText != null)
            {
                valueText.text = $"${clue.Value}";
            }

            if (questionText != null)
            {
                questionText.text = clue.Question;
                
                // Apply Lora font styling for clue text
                FontManager.EnsureExists();
                if (FontManager.Instance != null)
                {
                    FontManager.Instance.ApplyClueStyle(questionText);
                }
                
                // Enable auto-sizing for larger text
                questionText.enableAutoSizing = true;
                questionText.fontSizeMin = 36;
                questionText.fontSizeMax = 72;
            }

            if (answerText != null)
            {
                answerText.text = "";
                answerText.gameObject.SetActive(false);
            }

            if (revealAnswerButton != null)
            {
                revealAnswerButton.gameObject.SetActive(true);
            }

            SetStatus("");

            if (overlayPanel != null)
            {
                overlayPanel.SetActive(true);
                
                // Ensure overlay covers full screen
                RectTransform rt = overlayPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            Debug.Log($"[ClueOverlay] Showing clue: {categoryTitle} for ${clue.Value}");

            // Start answer flow
            if (enableAnswerFlow && answerFlow != null)
            {
                answerFlow.StartFlow(clue);
            }

            // Clear transcript
            if (transcriptText != null)
            {
                transcriptText.text = "";
                transcriptText.gameObject.SetActive(false);
            }

            if (enableTTS)
            {
                SpeakQuestion();
            }
            else if (enableAnswerFlow && answerFlow != null)
            {
                // If no TTS, immediately mark question as read
                answerFlow.OnQuestionReadComplete();
            }
        }

        private void SpeakQuestion()
        {
            if (TTSService.Instance == null || currentClue == null)
            {
                OnQuestionReadComplete?.Invoke();
                if (enableAnswerFlow && answerFlow != null)
                {
                    answerFlow.OnQuestionReadComplete();
                }
                return;
            }

            isReadingQuestion = true;
            SetStatus("Reading question...");

            // Use SpeakClue to leverage cached audio
            TTSService.Instance.SpeakClue(currentClue, () =>
            {
                isReadingQuestion = false;
                SetStatus("Press SPACE to buzz in!");
                OnQuestionReadComplete?.Invoke();
                
                if (enableAnswerFlow && answerFlow != null)
                {
                    answerFlow.OnQuestionReadComplete();
                }
                
                Debug.Log("[ClueOverlay] Question reading complete");
            });
        }

        private void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
                statusText.gameObject.SetActive(!string.IsNullOrEmpty(status));
            }
        }

        public bool IsReadingQuestion => isReadingQuestion;

        /// <summary>
        /// Start the answer flow (audio, buzzing, etc.) without showing the overlay UI.
        /// Used when the ClueRevealAnimator is handling the visual display.
        /// </summary>
        public void StartFlowOnly(Clue clue, string categoryTitle)
        {
            currentClue = clue;
            currentCategory = categoryTitle;
            isReadingQuestion = false;
            someoneAnsweredCorrectly = false;

            Debug.Log($"[ClueOverlay] Starting flow only (no UI): {categoryTitle} for ${clue.Value}");

            // Start answer flow
            if (enableAnswerFlow && answerFlow != null)
            {
                answerFlow.StartFlow(clue);
            }

            // Start TTS to read the question
            if (enableTTS && TTSService.Instance != null)
            {
                SpeakQuestion();
            }
            else if (enableAnswerFlow && answerFlow != null)
            {
                // If no TTS, immediately mark question as read
                answerFlow.OnQuestionReadComplete();
            }
        }

        public void Hide()
        {
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(false);
            }
        }

        private void OnRevealAnswerClicked()
        {
            if (currentClue != null && answerText != null)
            {
                answerText.text = currentClue.Answer;
                answerText.gameObject.SetActive(true);
            }

            if (revealAnswerButton != null)
            {
                revealAnswerButton.gameObject.SetActive(false);
            }
        }

        private void OnBackToBoardClicked()
        {
            if (currentClue != null)
            {
                currentClue.Used = true;
            }

            Hide();
            OnClueCompleted?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearActiveClue();
                // Control stays with correct answerer (set by AwardPoints) or current chooser if no one got it
            }
        }

        private void OnAnswerFlowStateChanged(AnswerFlowState state)
        {
            switch (state)
            {
                case AnswerFlowState.WaitingForQuestionRead:
                    SetStatus("Reading question...");
                    break;
                case AnswerFlowState.WaitingForBuzz:
                    SetStatus("Press SPACE to buzz in!");
                    break;
                case AnswerFlowState.Recording:
                    SetStatus("Listening... speak your answer");
                    break;
                case AnswerFlowState.Transcribing:
                    SetStatus("Processing...");
                    break;
                case AnswerFlowState.Judging:
                    SetStatus("Checking answer...");
                    break;
                case AnswerFlowState.ShowingResult:
                    // Status will be set by judgment result
                    break;
            }
        }

        private void OnPlayerBuzzed(int playerIndex)
        {
            if (GameManager.Instance != null && playerIndex >= 0 && playerIndex < GameManager.Instance.Players.Count)
            {
                string playerName = GameManager.Instance.Players[playerIndex].Name;
                SetStatus($"{playerName} buzzed!");
            }
        }

        private void OnTranscriptReady(string transcript)
        {
            if (transcriptText != null)
            {
                transcriptText.text = $"\"{transcript}\"";
                transcriptText.gameObject.SetActive(true);
            }
        }

        private void OnJudgmentReady(JudgeResult result)
        {
            if (result.IsCorrect)
            {
                someoneAnsweredCorrectly = true;
                SetStatus("CORRECT!");
                if (answerText != null)
                {
                    answerText.color = new Color(0.5f, 1f, 0.5f);
                }
                
                // Immediately show answer on the animated card
                var clueAnimator = FindFirstObjectByType<ClueRevealAnimator>();
                if (clueAnimator != null && clueAnimator.IsShowing && currentClue != null)
                {
                    clueAnimator.ShowAnswerText(currentClue.Answer);
                }
            }
            else
            {
                SetStatus("Incorrect");
                if (answerText != null)
                {
                    answerText.color = new Color(1f, 0.5f, 0.5f);
                }
            }

            // Show the correct answer on overlay (for fallback)
            if (answerText != null && currentClue != null)
            {
                answerText.text = $"Correct: {currentClue.Answer}";
                answerText.gameObject.SetActive(true);
            }

            if (revealAnswerButton != null)
            {
                revealAnswerButton.gameObject.SetActive(false);
            }
        }

        private void OnAnswerFlowComplete()
        {
            Debug.Log($"[ClueOverlay] Answer flow complete, someoneCorrect={someoneAnsweredCorrectly}");
            
            // Find the animator to show answer and animate back
            var clueAnimator = FindFirstObjectByType<ClueRevealAnimator>();
            if (clueAnimator != null && clueAnimator.IsShowing && currentClue != null)
            {
                if (someoneAnsweredCorrectly)
                {
                    // Someone got it right - answer already shown in OnJudgmentReady
                    // Just dismiss the card now
                    clueAnimator.DismissCard(() =>
                    {
                        OnBackToBoardClicked();
                    });
                }
                else
                {
                    // Nobody got it - show answer immediately, then speak while visible
                    clueAnimator.ShowAnswerText(currentClue.Answer);
                    
                    // Use PhrasePlayer for pre-recorded intro, then cached answer audio
                    var phrasePlayer = FindFirstObjectByType<PhrasePlayer>();
                    if (phrasePlayer != null)
                    {
                        // Play intro phrase ("The correct answer was...")
                        phrasePlayer.PlayRevealAnswerIntro(() =>
                        {
                            // Then play cached answer audio
                            if (TTSService.Instance != null)
                            {
                                TTSService.Instance.SpeakAnswer(currentClue, () =>
                                {
                                    clueAnimator.DismissCard(() =>
                                    {
                                        OnBackToBoardClicked();
                                    });
                                });
                            }
                            else
                            {
                                clueAnimator.DismissCard(() =>
                                {
                                    OnBackToBoardClicked();
                                });
                            }
                        });
                    }
                    else if (enableTTS && TTSService.Instance != null)
                    {
                        // Fallback: use SpeakAnswer which checks for cached audio
                        TTSService.Instance.SpeakAnswer(currentClue, () =>
                        {
                            clueAnimator.DismissCard(() =>
                            {
                                OnBackToBoardClicked();
                            });
                        });
                    }
                    else
                    {
                        // No audio, just dismiss
                        clueAnimator.DismissCard(() =>
                        {
                            OnBackToBoardClicked();
                        });
                    }
                }
            }
            else
            {
                // Fallback: just close normally
                OnBackToBoardClicked();
            }
        }

        private void OnDestroy()
        {
            if (revealAnswerButton != null)
            {
                revealAnswerButton.onClick.RemoveListener(OnRevealAnswerClicked);
            }

            if (backToBoardButton != null)
            {
                backToBoardButton.onClick.RemoveListener(OnBackToBoardClicked);
            }

            if (answerFlow != null)
            {
                answerFlow.OnStateChanged -= OnAnswerFlowStateChanged;
                answerFlow.OnPlayerBuzzed -= OnPlayerBuzzed;
                answerFlow.OnTranscriptReady -= OnTranscriptReady;
                answerFlow.OnJudgmentReady -= OnJudgmentReady;
                answerFlow.OnFlowComplete -= OnAnswerFlowComplete;
            }
        }
    }
}
