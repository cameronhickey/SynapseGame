using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Cerebrum.Data;
using Cerebrum.OpenAI;

namespace Cerebrum.Game
{
    public enum AnswerFlowState
    {
        WaitingForQuestionRead,
        WaitingForBuzz,
        Recording,
        Transcribing,
        Judging,
        ShowingResult,
        Complete
    }

    public class AnswerFlowController : MonoBehaviour
    {
        public static AnswerFlowController Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float answerTimeSeconds = 5f;
        [SerializeField] private float earlyBuzzLockoutMs = 750f;
        [SerializeField] private float resultDisplaySeconds = 2f;

        [Header("Input - Buzz Keys")]
        [SerializeField] private Key player1BuzzKey = Key.Z;
        [SerializeField] private Key player2BuzzKey = Key.G;
        [SerializeField] private Key player3BuzzKey = Key.M;

        public AnswerFlowState CurrentState { get; private set; }
        public int CurrentBuzzerPlayerIndex { get; private set; } = -1;
        public string LastTranscript { get; private set; }
        public JudgeResult LastJudgeResult { get; private set; }

        public event Action<AnswerFlowState> OnStateChanged;
        public event Action<int> OnPlayerBuzzed;
        public event Action<string> OnTranscriptReady;
        public event Action<JudgeResult> OnJudgmentReady;
        public event Action OnFlowComplete;

        private Clue currentClue;
        private bool[] playerLockedOut;
        private float answerTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartFlow(Clue clue)
        {
            currentClue = clue;
            CurrentBuzzerPlayerIndex = -1;
            LastTranscript = "";
            LastJudgeResult = null;

            int playerCount = GameManager.Instance?.Players.Count ?? 3;
            playerLockedOut = new bool[playerCount];

            SetState(AnswerFlowState.WaitingForQuestionRead);
        }

        public void OnQuestionReadComplete()
        {
            if (CurrentState == AnswerFlowState.WaitingForQuestionRead)
            {
                SetState(AnswerFlowState.WaitingForBuzz);
            }
        }

        public void EndFlow()
        {
            StopAllCoroutines();
            
            if (MicrophoneRecorder.Instance != null && MicrophoneRecorder.Instance.IsRecording)
            {
                MicrophoneRecorder.Instance.CancelRecording();
            }

            SetState(AnswerFlowState.Complete);
            OnFlowComplete?.Invoke();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Per-player buzz keys
            if (keyboard[player1BuzzKey].wasPressedThisFrame)
            {
                TryBuzz(0);
            }
            else if (keyboard[player2BuzzKey].wasPressedThisFrame)
            {
                TryBuzz(1);
            }
            else if (keyboard[player3BuzzKey].wasPressedThisFrame)
            {
                TryBuzz(2);
            }
        }

        public void TryBuzz(int playerIndex)
        {
            if (CurrentState == AnswerFlowState.WaitingForQuestionRead)
            {
                // Early buzz - lock out player
                HandleEarlyBuzz(playerIndex);
                return;
            }

            if (CurrentState != AnswerFlowState.WaitingForBuzz)
            {
                return;
            }

            if (playerIndex < 0 || playerIndex >= playerLockedOut.Length)
            {
                return;
            }

            if (playerLockedOut[playerIndex])
            {
                Debug.Log($"[AnswerFlow] Player {playerIndex} is locked out");
                return;
            }

            // Valid buzz!
            CurrentBuzzerPlayerIndex = playerIndex;
            string playerName = GameManager.Instance?.Players[playerIndex].Name ?? $"Player {playerIndex + 1}";
            
            Debug.Log($"[AnswerFlow] {playerName} buzzed in!");
            OnPlayerBuzzed?.Invoke(playerIndex);

            // Announce buzz, then auto-start recording
            if (TTSService.Instance != null)
            {
                TTSService.Instance.SpeakBuzzIn(playerName, OnBuzzAnnouncementComplete);
            }
            else
            {
                // No TTS, start recording immediately
                StartAutoRecording();
            }
        }

        private void OnBuzzAnnouncementComplete()
        {
            if (CurrentState == AnswerFlowState.WaitingForBuzz && CurrentBuzzerPlayerIndex >= 0)
            {
                StartAutoRecording();
            }
        }

        private void HandleEarlyBuzz(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerLockedOut.Length) return;
            if (playerLockedOut[playerIndex]) return;

            string playerName = GameManager.Instance?.Players[playerIndex].Name ?? $"Player {playerIndex + 1}";
            Debug.Log($"[AnswerFlow] {playerName} buzzed too early! Locked out for {earlyBuzzLockoutMs}ms");

            playerLockedOut[playerIndex] = true;
            StartCoroutine(UnlockPlayerAfterDelay(playerIndex, earlyBuzzLockoutMs / 1000f));

            // Play a short feedback sound or phrase
            // For now just log it
        }

        private IEnumerator UnlockPlayerAfterDelay(int playerIndex, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (playerIndex >= 0 && playerIndex < playerLockedOut.Length)
            {
                playerLockedOut[playerIndex] = false;
                Debug.Log($"[AnswerFlow] Player {playerIndex} unlocked");
            }
        }

        private IEnumerator AnswerTimerCoroutine()
        {
            answerTimer = answerTimeSeconds;

            while (answerTimer > 0 && CurrentState == AnswerFlowState.WaitingForBuzz)
            {
                answerTimer -= Time.deltaTime;
                yield return null;
            }

            // Time ran out without recording
            if (CurrentState == AnswerFlowState.WaitingForBuzz && CurrentBuzzerPlayerIndex >= 0)
            {
                Debug.Log("[AnswerFlow] Answer time expired");
                HandleIncorrectAnswer();
            }
        }

        public void StartRecording()
        {
            if (CurrentBuzzerPlayerIndex < 0) return;
            if (MicrophoneRecorder.Instance == null || !MicrophoneRecorder.Instance.HasMicrophone)
            {
                Debug.LogWarning("[AnswerFlow] No microphone available");
                return;
            }

            SetState(AnswerFlowState.Recording);
            MicrophoneRecorder.Instance.StartRecording();
        }

        private void StartAutoRecording()
        {
            if (CurrentBuzzerPlayerIndex < 0) return;
            if (MicrophoneRecorder.Instance == null || !MicrophoneRecorder.Instance.HasMicrophone)
            {
                Debug.LogWarning("[AnswerFlow] No microphone available");
                HandleIncorrectAnswer();
                return;
            }

            SetState(AnswerFlowState.Recording);
            Debug.Log("[AnswerFlow] Starting auto-recording...");

            MicrophoneRecorder.Instance.StartAutoRecording((audioData) =>
            {
                if (audioData != null && audioData.Length > 0)
                {
                    OnAutoRecordingComplete(audioData);
                }
                else
                {
                    Debug.LogWarning("[AnswerFlow] No audio captured");
                    HandleIncorrectAnswer();
                }
            });
        }

        private void OnAutoRecordingComplete(byte[] audioData)
        {
            if (CurrentState != AnswerFlowState.Recording) return;

            SetState(AnswerFlowState.Transcribing);
            STTService.Instance?.Transcribe(audioData, OnTranscriptionSuccess, OnTranscriptionError);
        }

        public void StopRecordingAndProcess()
        {
            if (CurrentState != AnswerFlowState.Recording) return;
            if (MicrophoneRecorder.Instance == null) return;

            SetState(AnswerFlowState.Transcribing);

            MicrophoneRecorder.Instance.OnRecordingStopped += OnRecordingComplete;
            MicrophoneRecorder.Instance.StopRecording();
        }

        private void OnRecordingComplete(byte[] audioData)
        {
            MicrophoneRecorder.Instance.OnRecordingStopped -= OnRecordingComplete;

            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogWarning("[AnswerFlow] No audio recorded");
                HandleIncorrectAnswer();
                return;
            }

            // Transcribe
            STTService.Instance?.Transcribe(audioData, OnTranscriptionSuccess, OnTranscriptionError);
        }

        private void OnTranscriptionSuccess(string transcript)
        {
            LastTranscript = transcript;
            OnTranscriptReady?.Invoke(transcript);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                HandleIncorrectAnswer();
                return;
            }

            // Judge the answer
            SetState(AnswerFlowState.Judging);
            AnswerJudge.Instance?.Judge(transcript, currentClue.Answer, OnJudgmentComplete);
        }

        private void OnTranscriptionError(string error)
        {
            Debug.LogError($"[AnswerFlow] Transcription error: {error}");
            HandleIncorrectAnswer();
        }

        private void OnJudgmentComplete(JudgeResult result)
        {
            LastJudgeResult = result;
            OnJudgmentReady?.Invoke(result);

            SetState(AnswerFlowState.ShowingResult);

            if (result.IsCorrect)
            {
                HandleCorrectAnswer();
            }
            else
            {
                HandleIncorrectAnswer();
            }
        }

        private void HandleCorrectAnswer()
        {
            if (GameManager.Instance != null && CurrentBuzzerPlayerIndex >= 0)
            {
                GameManager.Instance.AwardPoints(CurrentBuzzerPlayerIndex, currentClue.Value);
                string playerName = GameManager.Instance.Players[CurrentBuzzerPlayerIndex].Name;
                
                TTSService.Instance?.SpeakCorrect(playerName);
            }

            StartCoroutine(CompleteFlowAfterDelay());
        }

        private void HandleIncorrectAnswer()
        {
            if (GameManager.Instance != null && CurrentBuzzerPlayerIndex >= 0)
            {
                GameManager.Instance.DeductPoints(CurrentBuzzerPlayerIndex, currentClue.Value);
                playerLockedOut[CurrentBuzzerPlayerIndex] = true;
                
                TTSService.Instance?.SpeakIncorrect();
            }

            // Check if any players can still buzz
            bool anyPlayersRemaining = false;
            for (int i = 0; i < playerLockedOut.Length; i++)
            {
                if (!playerLockedOut[i])
                {
                    anyPlayersRemaining = true;
                    break;
                }
            }

            if (anyPlayersRemaining)
            {
                // Reset for next buzz attempt
                CurrentBuzzerPlayerIndex = -1;
                SetState(AnswerFlowState.WaitingForBuzz);
            }
            else
            {
                // No one got it right - reveal answer
                TTSService.Instance?.SpeakRevealAnswer(currentClue.Answer);
                StartCoroutine(CompleteFlowAfterDelay());
            }
        }

        private IEnumerator CompleteFlowAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplaySeconds);
            EndFlow();
        }

        private void SetState(AnswerFlowState newState)
        {
            CurrentState = newState;
            Debug.Log($"[AnswerFlow] State: {newState}");
            OnStateChanged?.Invoke(newState);
        }
    }
}
