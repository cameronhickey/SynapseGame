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
        }

        public void Show(Clue clue, string categoryTitle)
        {
            currentClue = clue;
            currentCategory = categoryTitle;
            isReadingQuestion = false;

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
                GameManager.Instance.RotateChooser();
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
                SetStatus("CORRECT!");
                if (answerText != null)
                {
                    answerText.color = new Color(0.5f, 1f, 0.5f);
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

            // Show the correct answer
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
            // Auto-close after flow completes
            OnBackToBoardClicked();
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
