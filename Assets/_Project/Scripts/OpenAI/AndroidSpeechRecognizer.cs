using System;
using UnityEngine;

namespace Cerebrum.OpenAI
{
    public class AndroidSpeechRecognizer : MonoBehaviour
    {
        public static AndroidSpeechRecognizer Instance { get; private set; }

        public bool IsAvailable { get; private set; }
        public bool IsListening { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action OnReady;
        public event Action OnSpeechStart;
        public event Action OnSpeechEnd;
        public event Action<string> OnPartialResultReceived;
        public event Action<string> OnFinalResultReceived;
        public event Action<string> OnErrorReceived;

        private Action<string> currentCallback;
        private Action<string> currentErrorCallback;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject speechPlugin;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // This GameObject must be named "AndroidSpeechBridge" to receive messages from Java
            gameObject.name = "AndroidSpeechBridge";

            Initialize();
        }

        private void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.cerebrum.speech.SpeechRecognizerPlugin"))
                {
                    speechPlugin = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
                    IsAvailable = speechPlugin.Call<bool>("isAvailable");
                    Debug.Log($"[AndroidSpeech] Initialized, available: {IsAvailable}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidSpeech] Failed to initialize: {e.Message}");
                IsAvailable = false;
            }
#else
            Debug.Log("[AndroidSpeech] Not available in Editor, will use fallback");
            IsAvailable = false;
#endif
        }

        public void StartListening(Action<string> onResult = null, Action<string> onError = null)
        {
            if (!IsAvailable || IsListening)
            {
                onError?.Invoke("Speech recognition not available or already listening");
                return;
            }

            currentCallback = onResult;
            currentErrorCallback = onError;
            IsListening = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            speechPlugin?.Call("startListening");
#endif
            Debug.Log("[AndroidSpeech] Started listening");
        }

        public void StopListening()
        {
            if (!IsListening) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            speechPlugin?.Call("stopListening");
#endif
            IsListening = false;
            Debug.Log("[AndroidSpeech] Stopped listening");
        }

        // Called from Java plugin
        public void OnInitialized(string available)
        {
            IsInitialized = true;
            IsAvailable = available == "true";
            Debug.Log($"[AndroidSpeech] OnInitialized: {available}");
        }

        // Called from Java plugin
        public void OnListeningStarted(string unused)
        {
            Debug.Log("[AndroidSpeech] OnListeningStarted");
        }

        // Called from Java plugin
        public void OnReadyForSpeech(string unused)
        {
            Debug.Log("[AndroidSpeech] OnReadyForSpeech");
            OnReady?.Invoke();
        }

        // Called from Java plugin
        public void OnBeginningOfSpeech(string unused)
        {
            Debug.Log("[AndroidSpeech] OnBeginningOfSpeech");
            OnSpeechStart?.Invoke();
        }

        // Called from Java plugin
        public void OnEndOfSpeech(string unused)
        {
            Debug.Log("[AndroidSpeech] OnEndOfSpeech");
            OnSpeechEnd?.Invoke();
        }

        // Called from Java plugin
        public void OnPartialResult(string partial)
        {
            Debug.Log($"[AndroidSpeech] Partial: {partial}");
            OnPartialResultReceived?.Invoke(partial);
        }

        // Called from Java plugin
        public void OnResult(string result)
        {
            Debug.Log($"[AndroidSpeech] Final result: {result}");
            IsListening = false;
            
            OnFinalResultReceived?.Invoke(result);
            currentCallback?.Invoke(result);
            
            currentCallback = null;
            currentErrorCallback = null;
        }

        // Called from Java plugin
        public void OnError(string error)
        {
            Debug.LogWarning($"[AndroidSpeech] Error: {error}");
            IsListening = false;
            
            OnErrorReceived?.Invoke(error);
            currentErrorCallback?.Invoke(error);
            
            currentCallback = null;
            currentErrorCallback = null;
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            speechPlugin?.Call("destroy");
            speechPlugin?.Dispose();
#endif
        }
    }
}
