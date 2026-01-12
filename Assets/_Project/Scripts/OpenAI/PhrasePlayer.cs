using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    public class PhrasePlayer : MonoBehaviour
    {
        public static PhrasePlayer Instance { get; private set; }

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float pauseBetweenClips = 0.15f;

        public bool IsPlaying => audioSource != null && audioSource.isPlaying;

        public event Action OnPlaybackStarted;
        public event Action OnPlaybackCompleted;

        private Coroutine currentPlayback;
        private Queue<AudioClip> clipQueue = new Queue<AudioClip>();

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

        public void PlayPhrase(string phraseId, Action onComplete = null)
        {
            AudioClip clip = null;

            // 1. Check bundled phrases first (instant, no API call)
            if (BundledPhraseLoader.Instance != null && BundledPhraseLoader.Instance.TryGetPhrase(phraseId, out clip))
            {
                PlayClip(clip, onComplete);
                return;
            }

            // 2. Fall back to runtime-cached phrases
            if (PhraseTTSCache.Instance != null && PhraseTTSCache.Instance.TryGetPhrase(phraseId, out clip))
            {
                PlayClip(clip, onComplete);
                return;
            }

            Debug.LogWarning($"[PhrasePlayer] Phrase not available: {phraseId}");
            onComplete?.Invoke();
        }

        public void PlayRandomPhrase(GamePhrases.PhraseCategory category, Action onComplete = null)
        {
            AudioClip clip = null;

            // 1. Try bundled phrases first
            if (BundledPhraseLoader.Instance != null)
            {
                clip = BundledPhraseLoader.Instance.GetRandomPhrase(category);
            }

            // 2. Fall back to runtime-cached phrases
            if (clip == null && PhraseTTSCache.Instance != null)
            {
                clip = PhraseTTSCache.Instance.GetRandomPhrase(category);
            }

            if (clip == null)
            {
                Debug.LogWarning($"[PhrasePlayer] No phrases available for category: {category}");
                onComplete?.Invoke();
                return;
            }

            PlayClip(clip, onComplete);
        }

        public void PlayPhraseWithName(string phraseId, string playerName, Action onComplete = null)
        {
            var phrase = GamePhrases.GetById(phraseId);
            if (phrase == null)
            {
                Debug.LogWarning($"[PhrasePlayer] Unknown phrase: {phraseId}");
                onComplete?.Invoke();
                return;
            }

            PlayPhraseWithName(phrase, playerName, onComplete);
        }

        public void PlayPhraseWithName(GamePhrases.Phrase phrase, string playerName, Action onComplete = null)
        {
            if (phrase == null)
            {
                onComplete?.Invoke();
                return;
            }

            List<AudioClip> clips = new List<AudioClip>();

            // Get player name clip
            AudioClip nameClip = null;
            if (!string.IsNullOrEmpty(playerName))
            {
                PhraseTTSCache.Instance?.TryGetPlayerName(playerName, out nameClip);
            }

            // Get phrase clip - check bundled first, then runtime cache
            AudioClip phraseClip = null;
            if (BundledPhraseLoader.Instance != null)
            {
                BundledPhraseLoader.Instance.TryGetPhrase(phrase.Id, out phraseClip);
            }
            if (phraseClip == null)
            {
                PhraseTTSCache.Instance?.TryGetPhrase(phrase.Id, out phraseClip);
            }

            // Build sequence based on phrase settings
            if (phrase.NamePrefix && nameClip != null)
            {
                clips.Add(nameClip);
            }

            if (phraseClip != null)
            {
                clips.Add(phraseClip);
            }

            if (phrase.NameSuffix && nameClip != null)
            {
                clips.Add(nameClip);
            }

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[PhrasePlayer] No clips available for phrase: {phrase.Id}");
                onComplete?.Invoke();
                return;
            }

            PlayClipSequence(clips, onComplete);
        }

        public void PlayRandomPhraseWithName(GamePhrases.PhraseCategory category, string playerName, Action onComplete = null)
        {
            var phrase = PhraseTTSCache.Instance?.GetRandomPhraseData(category);
            if (phrase == null)
            {
                Debug.LogWarning($"[PhrasePlayer] No phrases for category: {category}");
                onComplete?.Invoke();
                return;
            }

            PlayPhraseWithName(phrase, playerName, onComplete);
        }

        public void PlayCorrect(string playerName = null, Action onComplete = null)
        {
            PlayRandomPhraseWithName(GamePhrases.PhraseCategory.Correct, playerName, onComplete);
        }

        public void PlayIncorrect(Action onComplete = null)
        {
            PlayRandomPhrase(GamePhrases.PhraseCategory.Incorrect, onComplete);
        }

        public void PlayBuzzIn(string playerName, Action onComplete = null)
        {
            PlayRandomPhraseWithName(GamePhrases.PhraseCategory.BuzzIn, playerName, onComplete);
        }

        public void PlaySelectCategory(string playerName, Action onComplete = null)
        {
            PlayRandomPhraseWithName(GamePhrases.PhraseCategory.SelectCategory, playerName, onComplete);
        }

        public void PlaySelectCategoryFirst(string playerName, Action onComplete = null)
        {
            PlayRandomPhraseWithName(GamePhrases.PhraseCategory.SelectCategoryFirst, playerName, onComplete);
        }

        public void PlaySelectCategoryShort(Action onComplete = null)
        {
            // Short prompt without player name - used after correct answer to avoid repeating name
            if (TTSService.Instance != null)
            {
                TTSService.Instance.Speak("Your pick.", onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public void PlayTimeout(Action onComplete = null)
        {
            PlayRandomPhrase(GamePhrases.PhraseCategory.Timeout, onComplete);
        }

        public void PlayAnyoneElse(Action onComplete = null)
        {
            PlayRandomPhrase(GamePhrases.PhraseCategory.AnyoneElse, onComplete);
        }

        public void PlayRevealAnswer(string answer, Action onComplete = null)
        {
            // Play reveal phrase, then the answer
            // First get a random reveal phrase
            var phraseData = PhraseTTSCache.Instance?.GetRandomPhraseData(GamePhrases.PhraseCategory.RevealAnswer);
            
            if (phraseData != null && PhraseTTSCache.Instance.TryGetPhrase(phraseData.Id, out AudioClip phraseClip))
            {
                // Play phrase, then speak answer via TTS
                PlayClip(phraseClip, () =>
                {
                    // After phrase, speak the answer
                    TTSService.Instance?.Speak(answer, onComplete);
                });
            }
            else
            {
                // No cached phrase, just speak via TTSService
                TTSService.Instance?.SpeakRevealAnswer(answer);
                onComplete?.Invoke();
            }
        }

        public void PlayPlayerName(string playerName, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                onComplete?.Invoke();
                return;
            }

            if (PhraseTTSCache.Instance != null && PhraseTTSCache.Instance.TryGetPlayerName(playerName, out AudioClip clip))
            {
                PlayClip(clip, onComplete);
            }
            else
            {
                Debug.LogWarning($"[PhrasePlayer] Player name not cached: {playerName}");
                onComplete?.Invoke();
            }
        }

        private void PlayClip(AudioClip clip, Action onComplete = null)
        {
            if (currentPlayback != null)
            {
                StopCoroutine(currentPlayback);
            }

            currentPlayback = StartCoroutine(PlayClipCoroutine(clip, onComplete));
        }

        private void PlayClipSequence(List<AudioClip> clips, Action onComplete = null)
        {
            if (currentPlayback != null)
            {
                StopCoroutine(currentPlayback);
            }

            currentPlayback = StartCoroutine(PlaySequenceCoroutine(clips, onComplete));
        }

        private IEnumerator PlayClipCoroutine(AudioClip clip, Action onComplete)
        {
            // Stop TTSService if it's playing to prevent overlap
            if (TTSService.Instance != null && TTSService.Instance.IsSpeaking)
            {
                TTSService.Instance.Stop();
            }

            OnPlaybackStarted?.Invoke();

            audioSource.clip = clip;
            audioSource.Play();

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            OnPlaybackCompleted?.Invoke();
            onComplete?.Invoke();
            currentPlayback = null;
        }

        private IEnumerator PlaySequenceCoroutine(List<AudioClip> clips, Action onComplete)
        {
            // Stop TTSService if it's playing to prevent overlap
            if (TTSService.Instance != null && TTSService.Instance.IsSpeaking)
            {
                TTSService.Instance.Stop();
            }

            OnPlaybackStarted?.Invoke();

            for (int i = 0; i < clips.Count; i++)
            {
                audioSource.clip = clips[i];
                audioSource.Play();

                while (audioSource.isPlaying)
                {
                    yield return null;
                }

                // Pause between clips (except after last one)
                if (i < clips.Count - 1 && pauseBetweenClips > 0)
                {
                    yield return new WaitForSeconds(pauseBetweenClips);
                }
            }

            OnPlaybackCompleted?.Invoke();
            onComplete?.Invoke();
            currentPlayback = null;
        }

        public void Stop()
        {
            if (currentPlayback != null)
            {
                StopCoroutine(currentPlayback);
                currentPlayback = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            OnPlaybackCompleted?.Invoke();
        }
    }
}
