using System;
using System.Collections;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    public class TTSService : MonoBehaviour
    {
        public static TTSService Instance { get; private set; }

        [SerializeField] private AudioSource audioSource;

        public bool IsSpeaking => audioSource != null && audioSource.isPlaying;
        public event Action OnSpeechStarted;
        public event Action OnSpeechCompleted;

        private Coroutine currentSpeechCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void Speak(string text, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke();
                return;
            }

            if (OpenAIClient.Instance == null || !OpenAIClient.Instance.IsConfigured)
            {
                Debug.LogWarning("[TTSService] OpenAI not configured. Skipping TTS.");
                onComplete?.Invoke();
                return;
            }

            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
            }

            currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(text, onComplete));
        }

        private IEnumerator SpeakCoroutine(string text, Action onComplete)
        {
            Debug.Log($"[TTSService] Speaking: {text}");

            bool isLoading = true;
            AudioClip clip = null;
            string error = null;

            OpenAIClient.Instance.PostTTS(text,
                (audioClip) => { clip = audioClip; isLoading = false; },
                (err) => { error = err; isLoading = false; }
            );

            while (isLoading)
            {
                yield return null;
            }

            if (clip == null)
            {
                Debug.LogError($"[TTSService] Failed to get audio: {error}");
                onComplete?.Invoke();
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();
            OnSpeechStarted?.Invoke();

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            OnSpeechCompleted?.Invoke();
            onComplete?.Invoke();
            currentSpeechCoroutine = null;
        }

        public void Stop()
        {
            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
                currentSpeechCoroutine = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            OnSpeechCompleted?.Invoke();
        }

        public void SpeakQuestion(string question, Action onComplete = null)
        {
            Speak(question, onComplete);
        }

        public void SpeakClue(Clue clue, Action onComplete = null)
        {
            if (clue == null || string.IsNullOrEmpty(clue.Question))
            {
                onComplete?.Invoke();
                return;
            }

            // Try to use cached audio first
            if (TTSCache.Instance != null && TTSCache.Instance.TryGetCachedAudio(clue, out AudioClip cachedClip))
            {
                Debug.Log("[TTSService] Playing cached audio");
                PlayClip(cachedClip, onComplete);
                return;
            }

            // Fall back to live TTS
            Debug.Log("[TTSService] No cached audio, fetching live...");
            Speak(clue.Question, onComplete);
        }

        private void PlayClip(AudioClip clip, Action onComplete = null)
        {
            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
            }

            currentSpeechCoroutine = StartCoroutine(PlayClipCoroutine(clip, onComplete));
        }

        private IEnumerator PlayClipCoroutine(AudioClip clip, Action onComplete)
        {
            audioSource.clip = clip;
            audioSource.Play();
            OnSpeechStarted?.Invoke();

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            OnSpeechCompleted?.Invoke();
            onComplete?.Invoke();
            currentSpeechCoroutine = null;
        }

        public void SpeakCorrect(string playerName = null)
        {
            string phrase = string.IsNullOrEmpty(playerName) 
                ? "That's correct!" 
                : $"That's correct, {playerName}!";
            Speak(phrase);
        }

        public void SpeakIncorrect()
        {
            Speak("Sorry, that's not quite right.");
        }

        public void SpeakNextChooser(string playerName)
        {
            Speak($"What category next, {playerName}?");
        }

        public void SpeakRevealAnswer(string answer)
        {
            Speak($"The correct response was: {answer}");
        }

        public void SpeakBuzzIn(string playerName, Action onComplete = null)
        {
            Speak(playerName, onComplete);
        }
    }
}
