using System;
using UnityEngine;

namespace Cerebrum.OpenAI
{
    public class STTService : MonoBehaviour
    {
        public static STTService Instance { get; private set; }

        public bool IsTranscribing { get; private set; }
        public event Action<string> OnTranscriptionComplete;
        public event Action<string> OnTranscriptionError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Transcribe(byte[] audioData, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (audioData == null || audioData.Length == 0)
            {
                string error = "No audio data to transcribe";
                Debug.LogWarning($"[STTService] {error}");
                onError?.Invoke(error);
                OnTranscriptionError?.Invoke(error);
                return;
            }

            if (OpenAIClient.Instance == null || !OpenAIClient.Instance.IsConfigured)
            {
                string error = "OpenAI not configured";
                Debug.LogWarning($"[STTService] {error}");
                onError?.Invoke(error);
                OnTranscriptionError?.Invoke(error);
                return;
            }

            IsTranscribing = true;
            Debug.Log($"[STTService] Transcribing {audioData.Length} bytes of audio...");

            OpenAIClient.Instance.PostSTT(audioData,
                (transcript) =>
                {
                    IsTranscribing = false;
                    Debug.Log($"[STTService] Transcription: \"{transcript}\"");
                    onSuccess?.Invoke(transcript);
                    OnTranscriptionComplete?.Invoke(transcript);
                },
                (error) =>
                {
                    IsTranscribing = false;
                    Debug.LogError($"[STTService] Error: {error}");
                    onError?.Invoke(error);
                    OnTranscriptionError?.Invoke(error);
                }
            );
        }

        public void RecordAndTranscribe(Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (MicrophoneRecorder.Instance == null)
            {
                onError?.Invoke("MicrophoneRecorder not available");
                return;
            }

            if (!MicrophoneRecorder.Instance.HasMicrophone)
            {
                onError?.Invoke("No microphone available");
                return;
            }

            Action<byte[]> onRecordingStopped = null;
            onRecordingStopped = (audioData) =>
            {
                MicrophoneRecorder.Instance.OnRecordingStopped -= onRecordingStopped;
                
                if (audioData != null)
                {
                    Transcribe(audioData, onSuccess, onError);
                }
                else
                {
                    onError?.Invoke("No audio recorded");
                }
            };

            MicrophoneRecorder.Instance.OnRecordingStopped += onRecordingStopped;
            MicrophoneRecorder.Instance.StartRecording();
        }

        public void StopRecordingAndTranscribe()
        {
            if (MicrophoneRecorder.Instance != null && MicrophoneRecorder.Instance.IsRecording)
            {
                MicrophoneRecorder.Instance.StopRecording();
            }
        }
    }
}
