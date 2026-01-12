// This file requires the Whisper Unity package: https://github.com/Macoron/whisper.unity
// Uncomment the #define below if you have the package installed
// #define WHISPER_UNITY_INSTALLED

#if WHISPER_UNITY_INSTALLED
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Whisper;
using Debug = UnityEngine.Debug;

namespace Cerebrum.OpenAI
{
    public class WhisperLocalSTT : MonoBehaviour
    {
        public static WhisperLocalSTT Instance { get; private set; }

        [Header("Model Settings")]
        [SerializeField] private string modelFileName = "ggml-base.bin";
        [SerializeField] private string language = "en";
        
        [Header("Status")]
        [SerializeField] private bool isInitialized;
        [SerializeField] private bool isProcessing;

        public bool IsAvailable => isInitialized && whisper != null;
        public bool IsProcessing => isProcessing;

        public event Action<string> OnTranscriptionComplete;
        public event Action<string> OnTranscriptionError;

        private WhisperWrapper whisper;
        private WhisperParams whisperParams;
        private Stopwatch processTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeWhisper();
        }

        private async void InitializeWhisper()
        {
            try
            {
                var loadTimer = Stopwatch.StartNew();
                string modelPath = Path.Combine(Application.streamingAssetsPath, "Whisper", modelFileName);
                
                Debug.Log($"<color=yellow>[WhisperLocal]</color> Loading model: {modelFileName}");

                // Check if model exists
                if (!File.Exists(modelPath))
                {
                    Debug.LogError($"[WhisperLocal] Model file not found: {modelPath}");
                    return;
                }

                // Initialize Whisper
                whisper = await WhisperWrapper.InitFromFileAsync(modelPath);
                
                if (whisper == null)
                {
                    Debug.LogError("[WhisperLocal] Failed to initialize Whisper");
                    return;
                }

                // Create and configure params
                whisperParams = WhisperParams.GetDefaultParams();
                whisperParams.Language = language;
                whisperParams.Translate = false;
                whisperParams.NoContext = true;
                whisperParams.SingleSegment = true;

                loadTimer.Stop();
                isInitialized = true;
                Debug.Log($"<color=green>[WhisperLocal]</color> Model loaded in {loadTimer.ElapsedMilliseconds}ms (language: {language})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WhisperLocal] Initialization error: {e.Message}");
            }
        }

        public void TranscribeAudioClip(AudioClip clip, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsAvailable)
            {
                onError?.Invoke("Whisper not initialized");
                return;
            }

            if (isProcessing)
            {
                onError?.Invoke("Already processing");
                return;
            }

            StartCoroutine(TranscribeCoroutine(clip, onSuccess, onError));
        }

        private System.Collections.IEnumerator TranscribeCoroutine(AudioClip clip, Action<string> onSuccess, Action<string> onError)
        {
            isProcessing = true;
            processTimer = Stopwatch.StartNew();
            Debug.Log($"<color=cyan>[WhisperLocal]</color> Transcribing {clip.length:F1}s of audio...");

            string result = null;
            string error = null;
            bool done = false;

            // Run transcription async
            TranscribeAsync(clip, 
                (text) => { result = text; done = true; },
                (err) => { error = err; done = true; }
            );

            // Wait for completion
            while (!done)
            {
                yield return null;
            }

            processTimer.Stop();
            isProcessing = false;

            if (error != null)
            {
                Debug.LogError($"[WhisperLocal] Error after {processTimer.ElapsedMilliseconds}ms: {error}");
                onError?.Invoke(error);
                OnTranscriptionError?.Invoke(error);
            }
            else
            {
                Debug.Log($"<color=green>[WhisperLocal]</color> Transcribed in {processTimer.ElapsedMilliseconds}ms | Result: \"{result}\"");
                onSuccess?.Invoke(result ?? "");
                OnTranscriptionComplete?.Invoke(result ?? "");
            }
        }

        private async void TranscribeAsync(AudioClip clip, Action<string> onSuccess, Action<string> onError)
        {
            try
            {
                var result = await whisper.GetTextAsync(clip, whisperParams);
                
                if (result == null)
                {
                    onError?.Invoke("Transcription returned null");
                    return;
                }

                string text = result.Result?.Trim() ?? "";
                onSuccess?.Invoke(text);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        public void StartMicrophoneTranscription(Action<string> onSuccess, Action<string> onError)
        {
            if (!IsAvailable)
            {
                onError?.Invoke("Whisper not initialized");
                return;
            }

            // Use the existing MicrophoneRecorder to capture audio
            if (MicrophoneRecorder.Instance == null || !MicrophoneRecorder.Instance.HasMicrophone)
            {
                onError?.Invoke("No microphone available");
                return;
            }

            isProcessing = true;
            Debug.Log("[WhisperLocal] Starting microphone recording for transcription...");

            MicrophoneRecorder.Instance.StartAutoRecording((audioData) =>
            {
                if (audioData != null && audioData.Length > 0)
                {
                    // Convert WAV bytes to AudioClip
                    AudioClip clip = WavToAudioClip(audioData);
                    if (clip != null)
                    {
                        StartCoroutine(TranscribeCoroutine(clip, onSuccess, onError));
                    }
                    else
                    {
                        isProcessing = false;
                        onError?.Invoke("Failed to convert audio");
                    }
                }
                else
                {
                    isProcessing = false;
                    onError?.Invoke("No audio captured");
                }
            });
        }

        public void StopMicrophoneTranscription()
        {
            if (MicrophoneRecorder.Instance != null && MicrophoneRecorder.Instance.IsRecording)
            {
                MicrophoneRecorder.Instance.StopRecording();
            }
        }

        private AudioClip WavToAudioClip(byte[] wavData)
        {
            try
            {
                // Parse WAV header
                if (wavData.Length < 44) return null;

                int channels = BitConverter.ToInt16(wavData, 22);
                int sampleRate = BitConverter.ToInt32(wavData, 24);
                int bitsPerSample = BitConverter.ToInt16(wavData, 34);

                // Find data chunk
                int dataStart = 44;
                int dataSize = wavData.Length - 44;

                // Convert bytes to float samples
                int sampleCount = dataSize / (bitsPerSample / 8);
                float[] samples = new float[sampleCount];

                if (bitsPerSample == 16)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short sample = BitConverter.ToInt16(wavData, dataStart + i * 2);
                        samples[i] = sample / 32768f;
                    }
                }
                else if (bitsPerSample == 32)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        samples[i] = BitConverter.ToSingle(wavData, dataStart + i * 4);
                    }
                }

                AudioClip clip = AudioClip.Create("Recording", sampleCount / channels, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WhisperLocal] WAV conversion error: {e.Message}");
                return null;
            }
        }
    }
}
#endif
