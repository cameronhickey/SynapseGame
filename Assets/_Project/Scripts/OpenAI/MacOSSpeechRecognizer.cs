using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Cerebrum.OpenAI
{
    public class MacOSSpeechRecognizer : MonoBehaviour
    {
        public static MacOSSpeechRecognizer Instance { get; private set; }

        public bool IsAvailable { get; private set; }
        public bool IsListening { get; private set; }
        public bool IsAuthorized { get; private set; }

        private Action<string> currentOnSuccess;
        private Action<string> currentOnError;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("MacOSSpeechPlugin")]
        private static extern void MacOSSpeech_Initialize();

        [DllImport("MacOSSpeechPlugin")]
        private static extern bool MacOSSpeech_IsAvailable();

        [DllImport("MacOSSpeechPlugin")]
        private static extern bool MacOSSpeech_IsListening();

        [DllImport("MacOSSpeechPlugin")]
        private static extern void MacOSSpeech_RequestAuthorization(AuthorizationCallback callback);

        [DllImport("MacOSSpeechPlugin")]
        private static extern int MacOSSpeech_GetAuthorizationStatus();

        [DllImport("MacOSSpeechPlugin")]
        private static extern void MacOSSpeech_StartListening(TranscriptionCallback onTranscription, ErrorCallback onError);

        [DllImport("MacOSSpeechPlugin")]
        private static extern void MacOSSpeech_StopListening();

        [DllImport("MacOSSpeechPlugin")]
        private static extern void MacOSSpeech_Cleanup();

        private delegate void TranscriptionCallback(string text);
        private delegate void ErrorCallback(string error);
        private delegate void AuthorizationCallback(int status);

        private static TranscriptionCallback transcriptionCallback;
        private static ErrorCallback errorCallback;
        private static AuthorizationCallback authCallback;
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

            Initialize();
        }

        private void Initialize()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            try
            {
                MacOSSpeech_Initialize();
                IsAvailable = MacOSSpeech_IsAvailable();
                
                // Check authorization status
                // 0=NotDetermined, 1=Denied, 2=Restricted, 3=Authorized
                int status = MacOSSpeech_GetAuthorizationStatus();
                IsAuthorized = (status == 3);
                
                string statusName = status switch {
                    0 => "NotDetermined",
                    1 => "Denied",
                    2 => "Restricted", 
                    3 => "Authorized",
                    _ => "Unknown"
                };
                
                Debug.Log($"[MacOSSpeech] Initialized. Available: {IsAvailable}, Status: {statusName} ({status})");
                
                if (status == 0 && IsAvailable) // Only request if NotDetermined
                {
                    Debug.Log("[MacOSSpeech] Requesting authorization...");
                    RequestAuthorization();
                }
                else if (status == 1)
                {
                    Debug.LogWarning("[MacOSSpeech] Permission denied. Enable in System Settings > Privacy & Security > Speech Recognition");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MacOSSpeech] Failed to initialize: {e.Message}");
                IsAvailable = false;
            }
#else
            IsAvailable = false;
            Debug.Log("[MacOSSpeech] Not available (not macOS standalone build)");
#endif
        }

        public void RequestAuthorization()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            authCallback = OnAuthorizationResult;
            MacOSSpeech_RequestAuthorization(authCallback);
#endif
        }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(AuthorizationCallback))]
        private static void OnAuthorizationResult(int status)
        {
            if (Instance != null)
            {
                Instance.IsAuthorized = (status == 3);
                Debug.Log($"[MacOSSpeech] Authorization status: {status} (Authorized: {Instance.IsAuthorized})");
            }
        }
#endif

        public void StartListening(Action<string> onSuccess, Action<string> onError)
        {
            if (!IsAvailable)
            {
                onError?.Invoke("macOS Speech Recognition not available");
                return;
            }

            if (!IsAuthorized)
            {
                onError?.Invoke("macOS Speech Recognition not authorized");
                return;
            }

            if (IsListening)
            {
                onError?.Invoke("Already listening");
                return;
            }

            currentOnSuccess = onSuccess;
            currentOnError = onError;
            IsListening = true;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            transcriptionCallback = OnTranscriptionReceived;
            errorCallback = OnErrorReceived;
            MacOSSpeech_StartListening(transcriptionCallback, errorCallback);
#else
            IsListening = false;
            onError?.Invoke("Not supported on this platform");
#endif
        }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(TranscriptionCallback))]
        private static void OnTranscriptionReceived(string text)
        {
            if (Instance != null)
            {
                Instance.IsListening = false;
                Instance.currentOnSuccess?.Invoke(text);
                Instance.currentOnSuccess = null;
                Instance.currentOnError = null;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void OnErrorReceived(string error)
        {
            if (Instance != null)
            {
                Instance.IsListening = false;
                Instance.currentOnError?.Invoke(error);
                Instance.currentOnSuccess = null;
                Instance.currentOnError = null;
            }
        }
#endif

        public void StopListening()
        {
            if (!IsListening) return;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            MacOSSpeech_StopListening();
#endif
            IsListening = false;
        }

        private void OnDestroy()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            MacOSSpeech_Cleanup();
#endif
        }
    }
}
