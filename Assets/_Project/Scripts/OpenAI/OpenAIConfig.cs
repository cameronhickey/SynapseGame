using UnityEngine;

namespace Cerebrum.OpenAI
{
    [CreateAssetMenu(fileName = "OpenAIConfig", menuName = "Cerebrum/OpenAI Config")]
    public class OpenAIConfig : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("OpenAI API Key - FOR LOCAL DEV ONLY. Replace with proxy before shipping.")]
        public string ApiKey = "";

        [Header("TTS Settings")]
        [Tooltip("TTS model to use (tts-1 or tts-1-hd)")]
        public string TTSModel = "tts-1";

        [Tooltip("Voice to use for TTS (alloy, echo, fable, onyx, nova, shimmer)")]
        public string TTSVoice = "nova";

        [Tooltip("Speed of speech (0.25 to 4.0)")]
        [Range(0.25f, 4.0f)]
        public float TTSSpeed = 1.0f;

        [Header("STT Settings")]
        [Tooltip("Speech-to-text model")]
        public string STTModel = "whisper-1";

        [Header("Judge Settings")]
        [Tooltip("Model for answer judging")]
        public string JudgeModel = "gpt-4o-mini";

        [Header("Endpoints")]
        public string BaseUrl = "https://api.openai.com/v1";
        public string TTSEndpoint = "/audio/speech";
        public string STTEndpoint = "/audio/transcriptions";
        public string ChatEndpoint = "/chat/completions";

        public string GetTTSUrl() => BaseUrl + TTSEndpoint;
        public string GetSTTUrl() => BaseUrl + STTEndpoint;
        public string GetChatUrl() => BaseUrl + ChatEndpoint;

        public bool IsConfigured => !string.IsNullOrEmpty(ApiKey);
    }
}
