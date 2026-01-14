using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    /// <summary>
    /// Unified TTS loading system that collects all audio needs upfront
    /// and processes them concurrently with unified progress tracking.
    /// </summary>
    public class UnifiedTTSLoader : MonoBehaviour
    {
        public static UnifiedTTSLoader Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxConcurrentRequests = 10;

        public event Action<int, int> OnProgress; // (completed, total)
        public event Action OnComplete;

        public bool IsLoading { get; private set; }
        public int TotalRequests { get; private set; }
        public int CompletedRequests { get; private set; }

        // Caches
        private Dictionary<string, AudioClip> clueCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> answerCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> categoryCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> playerNameCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> phraseCache = new Dictionary<string, AudioClip>();

        private class TTSRequest
        {
            public string Key;
            public string Text;
            public CacheType Type;
            public string OriginalName; // For player names, store the original name for PhraseTTSCache
        }

        private enum CacheType
        {
            Clue,
            Answer,
            Category,
            PlayerName,
            Phrase
        }

        private List<TTSRequest> pendingRequests = new List<TTSRequest>();
        private int activeRequests = 0;

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
        /// Load all TTS audio for a game: categories, clues, answers, player names, and phrases
        /// </summary>
        public void LoadAllForGame(Board board, List<string> playerNames, Action onComplete = null)
        {
            if (board == null)
            {
                Debug.LogWarning("[UnifiedTTSLoader] No board provided");
                onComplete?.Invoke();
                return;
            }

            if (OpenAIClient.Instance == null || !OpenAIClient.Instance.IsConfigured)
            {
                Debug.LogWarning("[UnifiedTTSLoader] OpenAI not configured, skipping TTS");
                onComplete?.Invoke();
                return;
            }

            // Clear previous caches
            ClearCaches();
            pendingRequests.Clear();
            CompletedRequests = 0;

            // Collect all TTS requests
            CollectCategoryRequests(board);
            CollectClueRequests(board);
            CollectAnswerRequests(board);
            CollectPlayerNameRequests(playerNames);
            CollectPhraseRequests(playerNames);

            TotalRequests = pendingRequests.Count;

            if (TotalRequests == 0)
            {
                Debug.Log("[UnifiedTTSLoader] No TTS requests to process");
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[UnifiedTTSLoader] Starting unified TTS load: {TotalRequests} total requests");
            Debug.Log($"  - Categories: {board.Categories.Count}");
            Debug.Log($"  - Clues: {board.Categories.Count * 5}");
            Debug.Log($"  - Answers: {board.Categories.Count * 5}");
            Debug.Log($"  - Player names: {playerNames.Count}");
            Debug.Log($"  - Personalized phrases: {GamePhrases.IntegratedNamePhrases.Count * playerNames.Count}");

            IsLoading = true;
            StartCoroutine(ProcessAllRequests(onComplete));
        }

        private void CollectCategoryRequests(Board board)
        {
            foreach (var category in board.Categories)
            {
                if (!string.IsNullOrEmpty(category.Title))
                {
                    string key = GetCategoryKey(category.Title);
                    if (!categoryCache.ContainsKey(key))
                    {
                        pendingRequests.Add(new TTSRequest
                        {
                            Key = key,
                            Text = category.Title,
                            Type = CacheType.Category
                        });
                    }
                }
            }
        }

        private void CollectClueRequests(Board board)
        {
            foreach (var category in board.Categories)
            {
                foreach (var clue in category.Clues)
                {
                    if (!string.IsNullOrEmpty(clue.Question))
                    {
                        string key = GetClueKey(clue);
                        if (!clueCache.ContainsKey(key))
                        {
                            pendingRequests.Add(new TTSRequest
                            {
                                Key = key,
                                Text = clue.Question,
                                Type = CacheType.Clue
                            });
                        }
                    }
                }
            }
        }

        private void CollectAnswerRequests(Board board)
        {
            foreach (var category in board.Categories)
            {
                foreach (var clue in category.Clues)
                {
                    if (!string.IsNullOrEmpty(clue.Answer))
                    {
                        string key = GetAnswerKey(clue);
                        if (!answerCache.ContainsKey(key))
                        {
                            pendingRequests.Add(new TTSRequest
                            {
                                Key = key,
                                Text = clue.Answer,
                                Type = CacheType.Answer
                            });
                        }
                    }
                }
            }
        }

        private void CollectPlayerNameRequests(List<string> playerNames)
        {
            foreach (var name in playerNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    string key = GetPlayerNameKey(name);
                    if (!playerNameCache.ContainsKey(key))
                    {
                        pendingRequests.Add(new TTSRequest
                        {
                            Key = key,
                            Text = name,
                            Type = CacheType.PlayerName,
                            OriginalName = name
                        });
                    }
                }
            }
        }

        private void CollectPhraseRequests(List<string> playerNames)
        {
            foreach (var phrase in GamePhrases.IntegratedNamePhrases)
            {
                foreach (var playerName in playerNames)
                {
                    string key = GetPhraseKey(phrase.Id, playerName);
                    if (!phraseCache.ContainsKey(key))
                    {
                        string text = phrase.GetTextWithName(playerName);
                        pendingRequests.Add(new TTSRequest
                        {
                            Key = key,
                            Text = text,
                            Type = CacheType.Phrase
                        });
                    }
                }
            }
        }

        private IEnumerator ProcessAllRequests(Action onComplete)
        {
            int requestIndex = 0;

            while (requestIndex < pendingRequests.Count || activeRequests > 0)
            {
                // Start new requests up to max concurrent
                while (activeRequests < maxConcurrentRequests && requestIndex < pendingRequests.Count)
                {
                    var request = pendingRequests[requestIndex];
                    requestIndex++;
                    activeRequests++;
                    StartCoroutine(FetchAudio(request));
                }

                yield return null;
            }

            IsLoading = false;
            Debug.Log($"[UnifiedTTSLoader] Complete! {CompletedRequests}/{TotalRequests} loaded");

            OnComplete?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator FetchAudio(TTSRequest request)
        {
            bool isComplete = false;
            AudioClip clip = null;

            OpenAIClient.Instance.PostTTS(request.Text,
                (audioClip) =>
                {
                    clip = audioClip;
                    isComplete = true;
                },
                (error) =>
                {
                    Debug.LogWarning($"[UnifiedTTSLoader] Failed to load {request.Type} '{request.Key}': {error}");
                    isComplete = true;
                }
            );

            while (!isComplete)
            {
                yield return null;
            }

            if (clip != null)
            {
                StoreInCache(request, clip);
            }

            CompletedRequests++;
            activeRequests--;

            OnProgress?.Invoke(CompletedRequests, TotalRequests);
        }

        private void StoreInCache(TTSRequest request, AudioClip clip)
        {
            switch (request.Type)
            {
                case CacheType.Clue:
                    clueCache[request.Key] = clip;
                    break;
                case CacheType.Answer:
                    answerCache[request.Key] = clip;
                    break;
                case CacheType.Category:
                    categoryCache[request.Key] = clip;
                    break;
                case CacheType.PlayerName:
                    playerNameCache[request.Key] = clip;
                    // Also add to PhraseTTSCache so all code paths work consistently
                    if (!string.IsNullOrEmpty(request.OriginalName) && PhraseTTSCache.Instance != null)
                    {
                        PhraseTTSCache.Instance.AddPlayerNameToCache(request.OriginalName, clip);
                    }
                    break;
                case CacheType.Phrase:
                    phraseCache[request.Key] = clip;
                    break;
            }
        }

        // ===== Cache Key Generation =====
        
        private string GetClueKey(Clue clue)
        {
            return $"clue_{clue.Question.GetHashCode()}";
        }

        private string GetAnswerKey(Clue clue)
        {
            return $"answer_{clue.Answer.GetHashCode()}";
        }

        private string GetCategoryKey(string categoryName)
        {
            return $"category_{categoryName.ToLowerInvariant().Replace(" ", "_")}";
        }

        private string GetPlayerNameKey(string name)
        {
            return $"name_{name.ToLowerInvariant().Replace(" ", "_")}";
        }

        private string GetPhraseKey(string phraseId, string playerName)
        {
            return $"{phraseId}_{playerName.ToLowerInvariant().Replace(" ", "_")}";
        }

        // ===== Public Cache Access =====

        public bool TryGetClueAudio(Clue clue, out AudioClip clip)
        {
            return clueCache.TryGetValue(GetClueKey(clue), out clip);
        }

        public bool TryGetAnswerAudio(Clue clue, out AudioClip clip)
        {
            return answerCache.TryGetValue(GetAnswerKey(clue), out clip);
        }

        public bool TryGetCategoryAudio(string categoryName, out AudioClip clip)
        {
            return categoryCache.TryGetValue(GetCategoryKey(categoryName), out clip);
        }

        public bool TryGetPlayerNameAudio(string name, out AudioClip clip)
        {
            return playerNameCache.TryGetValue(GetPlayerNameKey(name), out clip);
        }

        public bool TryGetPhraseAudio(string phraseId, string playerName, out AudioClip clip)
        {
            return phraseCache.TryGetValue(GetPhraseKey(phraseId, playerName), out clip);
        }

        public AudioClip GetRandomPhraseAudio(GamePhrases.PhraseCategory category, string playerName)
        {
            var phrases = GamePhrases.GetIntegratedPhrasesByCategory(category);
            if (phrases.Count == 0) return null;

            // Shuffle and find first cached
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
                if (TryGetPhraseAudio(phrase.Id, playerName, out var clip))
                {
                    return clip;
                }
            }

            return null;
        }

        public void ClearCaches()
        {
            clueCache.Clear();
            answerCache.Clear();
            categoryCache.Clear();
            playerNameCache.Clear();
            phraseCache.Clear();
            CompletedRequests = 0;
            TotalRequests = 0;
        }
    }
}
