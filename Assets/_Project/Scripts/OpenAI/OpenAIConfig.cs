using System;
using System.IO;
using UnityEngine;

namespace Cerebrum.OpenAI
{
    [CreateAssetMenu(fileName = "OpenAIConfig", menuName = "Cerebrum/OpenAI Config")]
    public class OpenAIConfig : ScriptableObject
    {
        private const string CONFIG_FILENAME = "openai_config.json";
        
        [Header("API Settings")]
        [Tooltip("OpenAI API Key - Loaded from openai_config.json at runtime")]
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

        private bool _configLoaded = false;

        public string GetTTSUrl() => BaseUrl + TTSEndpoint;
        public string GetSTTUrl() => BaseUrl + STTEndpoint;
        public string GetChatUrl() => BaseUrl + ChatEndpoint;

        public bool IsConfigured => !string.IsNullOrEmpty(GetApiKey());

        /// <summary>
        /// Gets the API key, loading from JSON config file if needed
        /// </summary>
        public string GetApiKey()
        {
            if (!_configLoaded)
            {
                LoadFromJsonConfig();
            }
            return ApiKey;
        }

        /// <summary>
        /// Load configuration from JSON file in StreamingAssets
        /// </summary>
        public void LoadFromJsonConfig()
        {
            _configLoaded = true;
            
            string configPath = GetConfigPath();
            
            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"[OpenAIConfig] Config file not found at: {configPath}");
                Debug.LogWarning($"[OpenAIConfig] Create {CONFIG_FILENAME} from the template file.");
                return;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                JsonConfigData data = JsonUtility.FromJson<JsonConfigData>(json);
                
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.apiKey))
                        ApiKey = data.apiKey;
                    if (!string.IsNullOrEmpty(data.ttsModel))
                        TTSModel = data.ttsModel;
                    if (!string.IsNullOrEmpty(data.ttsVoice))
                        TTSVoice = data.ttsVoice;
                    if (data.ttsSpeed > 0)
                        TTSSpeed = data.ttsSpeed;
                    if (!string.IsNullOrEmpty(data.sttModel))
                        STTModel = data.sttModel;
                    if (!string.IsNullOrEmpty(data.judgeModel))
                        JudgeModel = data.judgeModel;
                    if (!string.IsNullOrEmpty(data.baseUrl))
                        BaseUrl = data.baseUrl;
                    
                    Debug.Log("[OpenAIConfig] Loaded configuration from JSON file");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenAIConfig] Failed to load config: {e.Message}");
            }
        }

        private string GetConfigPath()
        {
#if UNITY_EDITOR
            // In editor, look in project root for easy editing
            return Path.Combine(Application.dataPath, "..", CONFIG_FILENAME);
#else
            // In built app, look in StreamingAssets
            return Path.Combine(Application.streamingAssetsPath, CONFIG_FILENAME);
#endif
        }

        [Serializable]
        private class JsonConfigData
        {
            public string apiKey;
            public string ttsModel;
            public string ttsVoice;
            public float ttsSpeed;
            public string sttModel;
            public string judgeModel;
            public string baseUrl;
        }
    }
}
