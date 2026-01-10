using System;
using System.Collections;
using UnityEngine;

namespace Cerebrum.OpenAI
{
    public class MicrophoneRecorder : MonoBehaviour
    {
        public static MicrophoneRecorder Instance { get; private set; }

        [Header("Recording Settings")]
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int maxRecordingSeconds = 10;

        [Header("Voice Activity Detection")]
        [SerializeField] private float silenceThreshold = 0.01f;
        [SerializeField] private float silenceDurationToStop = 1.5f;
        [SerializeField] private float minRecordingDuration = 0.5f;
        [SerializeField] private float maxWaitForSpeech = 5f;

        public bool IsRecording { get; private set; }
        public bool IsAutoRecording { get; private set; }
        public float CurrentVolume { get; private set; }
        
        public event Action OnRecordingStarted;
        public event Action<byte[]> OnRecordingStopped;
        public event Action OnSpeechDetected;
        public event Action OnSilenceDetected;

        private AudioClip recordingClip;
        private string microphoneDevice;
        private float recordingStartTime;
        private float lastSpeechTime;
        private bool hasSpeechStarted;
        private Coroutine autoRecordCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeMicrophone();
        }

        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                Debug.Log($"[MicrophoneRecorder] Using microphone: {microphoneDevice}");
            }
            else
            {
                Debug.LogWarning("[MicrophoneRecorder] No microphone detected!");
            }
        }

        public bool HasMicrophone => !string.IsNullOrEmpty(microphoneDevice);

        public void StartRecording()
        {
            if (IsRecording)
            {
                Debug.LogWarning("[MicrophoneRecorder] Already recording");
                return;
            }

            if (!HasMicrophone)
            {
                Debug.LogError("[MicrophoneRecorder] No microphone available");
                return;
            }

            recordingClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
            recordingStartTime = Time.realtimeSinceStartup;
            IsRecording = true;

            OnRecordingStarted?.Invoke();
            Debug.Log("[MicrophoneRecorder] Recording started");
        }

        public void StopRecording()
        {
            if (!IsRecording)
            {
                Debug.LogWarning("[MicrophoneRecorder] Not currently recording");
                return;
            }

            int position = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
            IsRecording = false;

            if (position <= 0)
            {
                Debug.LogWarning("[MicrophoneRecorder] No audio recorded");
                OnRecordingStopped?.Invoke(null);
                return;
            }

            byte[] wavData = ConvertToWav(recordingClip, position);
            
            float duration = Time.realtimeSinceStartup - recordingStartTime;
            Debug.Log($"[MicrophoneRecorder] Recording stopped. Duration: {duration:F2}s, Size: {wavData.Length} bytes");

            OnRecordingStopped?.Invoke(wavData);
        }

        public void CancelRecording()
        {
            if (!IsRecording) return;

            if (autoRecordCoroutine != null)
            {
                StopCoroutine(autoRecordCoroutine);
                autoRecordCoroutine = null;
            }

            Microphone.End(microphoneDevice);
            IsRecording = false;
            IsAutoRecording = false;
            Debug.Log("[MicrophoneRecorder] Recording cancelled");
        }

        public void StartAutoRecording(Action<byte[]> onComplete = null)
        {
            if (IsRecording)
            {
                Debug.LogWarning("[MicrophoneRecorder] Already recording");
                return;
            }

            if (!HasMicrophone)
            {
                Debug.LogError("[MicrophoneRecorder] No microphone available");
                onComplete?.Invoke(null);
                return;
            }

            IsAutoRecording = true;
            autoRecordCoroutine = StartCoroutine(AutoRecordCoroutine(onComplete));
        }

        private IEnumerator AutoRecordCoroutine(Action<byte[]> onComplete)
        {
            // Start the microphone
            recordingClip = Microphone.Start(microphoneDevice, true, maxRecordingSeconds, sampleRate);
            recordingStartTime = Time.realtimeSinceStartup;
            IsRecording = true;
            hasSpeechStarted = false;
            lastSpeechTime = Time.realtimeSinceStartup;

            OnRecordingStarted?.Invoke();
            Debug.Log("[MicrophoneRecorder] Auto-recording started, waiting for speech...");

            // Wait for microphone to initialize
            yield return new WaitUntil(() => Microphone.GetPosition(microphoneDevice) > 0);

            float waitStartTime = Time.realtimeSinceStartup;
            int sampleWindow = 128;
            float[] samples = new float[sampleWindow];

            while (IsRecording)
            {
                float elapsed = Time.realtimeSinceStartup - recordingStartTime;

                // Get current audio level
                int micPosition = Microphone.GetPosition(microphoneDevice);
                if (micPosition >= sampleWindow && recordingClip != null)
                {
                    recordingClip.GetData(samples, micPosition - sampleWindow);
                    
                    float sum = 0f;
                    for (int i = 0; i < sampleWindow; i++)
                    {
                        sum += Mathf.Abs(samples[i]);
                    }
                    CurrentVolume = sum / sampleWindow;
                }

                bool isSpeaking = CurrentVolume > silenceThreshold;

                if (isSpeaking)
                {
                    if (!hasSpeechStarted)
                    {
                        hasSpeechStarted = true;
                        OnSpeechDetected?.Invoke();
                        Debug.Log("[MicrophoneRecorder] Speech detected!");
                    }
                    lastSpeechTime = Time.realtimeSinceStartup;
                }

                // Check stop conditions
                if (hasSpeechStarted)
                {
                    float silenceDuration = Time.realtimeSinceStartup - lastSpeechTime;
                    float recordingDuration = Time.realtimeSinceStartup - recordingStartTime;

                    // Stop if silence detected after speaking for minimum duration
                    if (silenceDuration >= silenceDurationToStop && recordingDuration >= minRecordingDuration)
                    {
                        Debug.Log($"[MicrophoneRecorder] Silence detected after {recordingDuration:F2}s of recording");
                        OnSilenceDetected?.Invoke();
                        break;
                    }
                }
                else
                {
                    // Timeout waiting for speech
                    if (Time.realtimeSinceStartup - waitStartTime >= maxWaitForSpeech)
                    {
                        Debug.Log("[MicrophoneRecorder] Timeout waiting for speech");
                        break;
                    }
                }

                // Max recording time reached
                if (elapsed >= maxRecordingSeconds - 0.5f)
                {
                    Debug.Log("[MicrophoneRecorder] Max recording time reached");
                    break;
                }

                yield return null;
            }

            // Stop and process
            int position = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
            IsRecording = false;
            IsAutoRecording = false;
            autoRecordCoroutine = null;

            if (position <= 0 || !hasSpeechStarted)
            {
                Debug.LogWarning("[MicrophoneRecorder] No speech recorded");
                OnRecordingStopped?.Invoke(null);
                onComplete?.Invoke(null);
                yield break;
            }

            byte[] wavData = ConvertToWav(recordingClip, position);
            float duration = Time.realtimeSinceStartup - recordingStartTime;
            Debug.Log($"[MicrophoneRecorder] Auto-recording complete. Duration: {duration:F2}s, Size: {wavData.Length} bytes");

            OnRecordingStopped?.Invoke(wavData);
            onComplete?.Invoke(wavData);
        }

        private byte[] ConvertToWav(AudioClip clip, int sampleCount)
        {
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            int channels = clip.channels;
            int sampleRateValue = clip.frequency;

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                int byteRate = sampleRateValue * channels * 2;
                int blockAlign = channels * 2;
                int subChunk2Size = sampleCount * channels * 2;

                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + subChunk2Size);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt subchunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1Size (16 for PCM)
                writer.Write((short)1); // AudioFormat (1 = PCM)
                writer.Write((short)channels);
                writer.Write(sampleRateValue);
                writer.Write(byteRate);
                writer.Write((short)blockAlign);
                writer.Write((short)16); // BitsPerSample

                // data subchunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(subChunk2Size);

                // Convert float samples to 16-bit PCM
                foreach (float sample in samples)
                {
                    short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                    writer.Write(intSample);
                }

                return stream.ToArray();
            }
        }

        private void OnDestroy()
        {
            if (IsRecording)
            {
                CancelRecording();
            }
        }
    }
}
