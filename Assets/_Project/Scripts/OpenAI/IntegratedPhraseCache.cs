using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    /// <summary>
    /// Caches integrated name phrases (phrases with player names baked in).
    /// These are generated at runtime for real games, or pre-generated for test games.
    /// </summary>
    public class IntegratedPhraseCache : MonoBehaviour
    {
        public static IntegratedPhraseCache Instance { get; private set; }

        // Cache key format: "phraseId_playerName" e.g., "correct_int_1_ken"
        private Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public event Action<float> OnGenerationProgress;
        public event Action OnGenerationComplete;

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

        /// <summary>
        /// Get cache key for a phrase + player combination
        /// </summary>
        public static string GetCacheKey(string phraseId, string playerName)
        {
            return $"{phraseId}_{playerName.ToLowerInvariant().Replace(" ", "_")}";
        }

        /// <summary>
        /// Try to get a cached integrated phrase
        /// </summary>
        public bool TryGetPhrase(string phraseId, string playerName, out AudioClip clip)
        {
            string key = GetCacheKey(phraseId, playerName);
            return cache.TryGetValue(key, out clip);
        }

        /// <summary>
        /// Get a random integrated phrase for a category and player
        /// </summary>
        public AudioClip GetRandomIntegratedPhrase(GamePhrases.PhraseCategory category, string playerName)
        {
            var phrases = GamePhrases.GetIntegratedPhrasesByCategory(category);
            if (phrases.Count == 0) return null;

            // Try each phrase randomly until we find one that's cached
            var shuffled = new List<GamePhrases.Phrase>(phrases);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            foreach (var phrase in shuffled)
            {
                if (TryGetPhrase(phrase.Id, playerName, out var clip))
                {
                    return clip;
                }
            }

            return null;
        }

        /// <summary>
        /// Add a clip to the cache
        /// </summary>
        public void AddToCache(string phraseId, string playerName, AudioClip clip)
        {
            string key = GetCacheKey(phraseId, playerName);
            cache[key] = clip;
        }

        /// <summary>
        /// Load pre-generated integrated phrases from Resources (for test game)
        /// </summary>
        public void LoadTestGamePhrases(string[] playerNames)
        {
            int loaded = 0;
            foreach (var phrase in GamePhrases.IntegratedNamePhrases)
            {
                foreach (var playerName in playerNames)
                {
                    string key = GetCacheKey(phrase.Id, playerName);
                    string resourcePath = $"Audio/TestGame/IntegratedPhrases/{key}";
                    var clip = Resources.Load<AudioClip>(resourcePath);
                    if (clip != null)
                    {
                        cache[key] = clip;
                        loaded++;
                    }
                }
            }
            Debug.Log($"[IntegratedPhraseCache] Loaded {loaded} test game integrated phrases");
        }

        /// <summary>
        /// Generate integrated phrases for given player names via TTS
        /// </summary>
        public void GenerateForPlayers(List<string> playerNames, Action onComplete = null)
        {
            StartCoroutine(GeneratePhrasesCoroutine(playerNames, onComplete));
        }

        private IEnumerator GeneratePhrasesCoroutine(List<string> playerNames, Action onComplete)
        {
            var phrases = GamePhrases.IntegratedNamePhrases;
            int total = phrases.Count * playerNames.Count;
            int completed = 0;

            foreach (var phrase in phrases)
            {
                foreach (var playerName in playerNames)
                {
                    string key = GetCacheKey(phrase.Id, playerName);
                    
                    // Skip if already cached
                    if (cache.ContainsKey(key))
                    {
                        completed++;
                        continue;
                    }

                    string textWithName = phrase.GetTextWithName(playerName);
                    bool done = false;
                    AudioClip resultClip = null;

                    OpenAIClient client = FindFirstObjectByType<OpenAIClient>();
                    if (client == null)
                    {
                        var go = new GameObject("TempOpenAIClient");
                        client = go.AddComponent<OpenAIClient>();
                    }

                    client.PostTTS(textWithName,
                        (clip) => { resultClip = clip; done = true; },
                        (error) => { Debug.LogWarning($"[IntegratedPhraseCache] Failed to generate {key}: {error}"); done = true; }
                    );

                    float timeout = 15f;
                    float elapsed = 0f;
                    while (!done && elapsed < timeout)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (resultClip != null)
                    {
                        cache[key] = resultClip;
                    }

                    completed++;
                    OnGenerationProgress?.Invoke((float)completed / total);

                    // Small delay between requests
                    yield return new WaitForSeconds(0.1f);
                }
            }

            Debug.Log($"[IntegratedPhraseCache] Generated {cache.Count} integrated phrases");
            OnGenerationComplete?.Invoke();
            onComplete?.Invoke();
        }

        /// <summary>
        /// Check if integrated phrases are available for a player
        /// </summary>
        public bool HasPhrasesForPlayer(string playerName)
        {
            foreach (var phrase in GamePhrases.IntegratedNamePhrases)
            {
                string key = GetCacheKey(phrase.Id, playerName);
                if (cache.ContainsKey(key))
                    return true;
            }
            return false;
        }

        public int CachedCount => cache.Count;
    }
}
