using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Cerebrum.OpenAI
{
    public class OpenAIClient : MonoBehaviour
    {
        public static OpenAIClient Instance { get; private set; }

        [SerializeField] private OpenAIConfig config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadConfig();
        }

        private void LoadConfig()
        {
            if (config == null)
            {
                config = Resources.Load<OpenAIConfig>("OpenAIConfig");
                Debug.Log($"[OpenAIClient] Loaded config from Resources: {(config != null ? "SUCCESS" : "FAILED")}");
            }

            if (config == null)
            {
                Debug.LogWarning("[OpenAIClient] OpenAIConfig not found in Resources. Create one via Assets > Create > Cerebrum > OpenAI Config");
            }
            else if (!config.IsConfigured)
            {
                Debug.LogWarning("[OpenAIClient] OpenAI API Key not configured. TTS/STT will not work.");
            }
            else
            {
                string apiKey = config.GetApiKey();
                string maskedKey = apiKey.Length > 10 
                    ? apiKey.Substring(0, 7) + "..." + apiKey.Substring(apiKey.Length - 4)
                    : "***";
                Debug.Log($"[OpenAIClient] Config loaded. API Key: {maskedKey}, Model: {config.TTSModel}, Voice: {config.TTSVoice}");
            }
        }

        public OpenAIConfig Config => config;

        public bool IsConfigured => config != null && config.IsConfigured;

        public void PostTTS(string text, Action<AudioClip> onSuccess, Action<string> onError)
        {
            if (!IsConfigured)
            {
                onError?.Invoke("OpenAI not configured");
                return;
            }

            StartCoroutine(PostTTSCoroutine(text, onSuccess, onError));
        }

        private IEnumerator PostTTSCoroutine(string text, Action<AudioClip> onSuccess, Action<string> onError)
        {
            var requestBody = new TTSRequest
            {
                model = config.TTSModel,
                input = text,
                voice = config.TTSVoice,
                speed = config.TTSSpeed
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            string url = config.GetTTSUrl();
            Debug.Log($"[OpenAIClient] TTS Request to: {url}, Key length: {config.GetApiKey()?.Length ?? 0}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + config.GetApiKey());

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string responseBody = request.downloadHandler?.text ?? "no body";
                    Debug.LogError($"[OpenAIClient] TTS Error: {request.error}, Response: {responseBody}");
                    onError?.Invoke(request.error);
                    yield break;
                }

                byte[] audioData = request.downloadHandler.data;
                
                StartCoroutine(ConvertMP3ToAudioClip(audioData, onSuccess, onError));
            }
        }

        private IEnumerator ConvertMP3ToAudioClip(byte[] mp3Data, Action<AudioClip> onSuccess, Action<string> onError)
        {
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "tts_audio_" + DateTime.Now.Ticks + ".mp3");
            
            try
            {
                System.IO.File.WriteAllBytes(tempPath, mp3Data);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Failed to write temp file: {e.Message}");
                yield break;
            }

            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
            {
                yield return audioRequest.SendWebRequest();

                if (audioRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[OpenAIClient] Audio load error: {audioRequest.error}");
                    onError?.Invoke(audioRequest.error);
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    onSuccess?.Invoke(clip);
                }
            }

            try
            {
                System.IO.File.Delete(tempPath);
            }
            catch { }
        }

        public void PostSTT(byte[] audioData, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsConfigured)
            {
                onError?.Invoke("OpenAI not configured");
                return;
            }

            StartCoroutine(PostSTTCoroutine(audioData, onSuccess, onError));
        }

        private IEnumerator PostSTTCoroutine(byte[] audioData, Action<string> onSuccess, Action<string> onError)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");
            form.AddField("model", config.STTModel);

            using (UnityWebRequest request = UnityWebRequest.Post(config.GetSTTUrl(), form))
            {
                request.SetRequestHeader("Authorization", "Bearer " + config.GetApiKey());

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[OpenAIClient] STT Error: {request.error}");
                    onError?.Invoke(request.error);
                    yield break;
                }

                var response = JsonUtility.FromJson<STTResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response.text);
            }
        }

        public void PostChat(string systemPrompt, string userMessage, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsConfigured)
            {
                onError?.Invoke("OpenAI not configured");
                return;
            }

            StartCoroutine(PostChatCoroutine(systemPrompt, userMessage, onSuccess, onError));
        }

        private IEnumerator PostChatCoroutine(string systemPrompt, string userMessage, Action<string> onSuccess, Action<string> onError)
        {
            var requestBody = new ChatRequest
            {
                model = config.JudgeModel,
                messages = new ChatMessage[]
                {
                    new ChatMessage { role = "system", content = systemPrompt },
                    new ChatMessage { role = "user", content = userMessage }
                }
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(config.GetChatUrl(), "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + config.GetApiKey());

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[OpenAIClient] Chat Error: {request.error}");
                    onError?.Invoke(request.error);
                    yield break;
                }

                var response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                if (response.choices != null && response.choices.Length > 0)
                {
                    onSuccess?.Invoke(response.choices[0].message.content);
                }
                else
                {
                    onError?.Invoke("No response from model");
                }
            }
        }

        [Serializable]
        private class TTSRequest
        {
            public string model;
            public string input;
            public string voice;
            public float speed;
        }

        [Serializable]
        private class STTResponse
        {
            public string text;
        }

        [Serializable]
        private class ChatRequest
        {
            public string model;
            public ChatMessage[] messages;
        }

        [Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatResponse
        {
            public ChatChoice[] choices;
        }

        [Serializable]
        private class ChatChoice
        {
            public ChatMessage message;
        }
    }
}
