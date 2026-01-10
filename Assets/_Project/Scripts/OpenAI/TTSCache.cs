using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    public class TTSCache : MonoBehaviour
    {
        public static TTSCache Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxConcurrentRequests = 3;

        public bool IsCaching { get; private set; }
        public int TotalClues { get; private set; }
        public int CachedClues { get; private set; }
        public float Progress => TotalClues > 0 ? (float)CachedClues / TotalClues : 0f;

        public event Action<float> OnCacheProgress;
        public event Action OnCacheComplete;
        public event Action<Clue> OnClueReady;

        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, Clue> keyToClue = new Dictionary<string, Clue>();
        private List<CacheRequest> pendingRequests = new List<CacheRequest>();
        private int activeRequests = 0;

        private class CacheRequest
        {
            public string Key;
            public string Text;
            public Clue Clue;
        }

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

        public void PreCacheBoard(Board board, Action onComplete = null)
        {
            if (board == null || board.Categories == null)
            {
                Debug.LogWarning("[TTSCache] No board to cache");
                onComplete?.Invoke();
                return;
            }

            if (OpenAIClient.Instance == null || !OpenAIClient.Instance.IsConfigured)
            {
                Debug.LogWarning("[TTSCache] OpenAI not configured, skipping pre-cache");
                onComplete?.Invoke();
                return;
            }

            // Clear previous cache
            audioCache.Clear();
            keyToClue.Clear();
            pendingRequests.Clear();
            CachedClues = 0;

            // Collect all clues for caching
            List<CacheRequest> allRequests = new List<CacheRequest>();
            foreach (var category in board.Categories)
            {
                foreach (var clue in category.Clues)
                {
                    string key = GetCacheKey(clue);
                    if (!string.IsNullOrEmpty(clue.Question))
                    {
                        allRequests.Add(new CacheRequest
                        {
                            Key = key,
                            Text = clue.Question,
                            Clue = clue
                        });
                        keyToClue[key] = clue;
                    }
                }
            }

            // Shuffle the requests for random reveal order
            ShuffleList(allRequests);
            pendingRequests = allRequests;

            TotalClues = pendingRequests.Count;
            
            if (TotalClues == 0)
            {
                Debug.Log("[TTSCache] No clues to cache");
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[TTSCache] Starting pre-cache of {TotalClues} clues...");
            IsCaching = true;

            StartCoroutine(ProcessCacheQueue(onComplete));
        }

        private IEnumerator ProcessCacheQueue(Action onComplete)
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

            IsCaching = false;
            Debug.Log($"[TTSCache] Pre-cache complete! {CachedClues}/{TotalClues} clues cached");
            
            OnCacheComplete?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator FetchAudio(CacheRequest request)
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
                    Debug.LogWarning($"[TTSCache] Failed to cache: {error}");
                    isComplete = true;
                }
            );

            while (!isComplete)
            {
                yield return null;
            }

            if (clip != null)
            {
                audioCache[request.Key] = clip;
                
                // Notify that this clue is ready
                OnClueReady?.Invoke(request.Clue);
            }

            CachedClues++;
            activeRequests--;

            OnCacheProgress?.Invoke(Progress);

            if (CachedClues % 5 == 0 || CachedClues == TotalClues)
            {
                Debug.Log($"[TTSCache] Progress: {CachedClues}/{TotalClues} ({Progress * 100:F0}%)");
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public bool TryGetCachedAudio(Clue clue, out AudioClip clip)
        {
            string key = GetCacheKey(clue);
            return audioCache.TryGetValue(key, out clip);
        }

        public bool HasCachedAudio(Clue clue)
        {
            string key = GetCacheKey(clue);
            return audioCache.ContainsKey(key);
        }

        private string GetCacheKey(Clue clue)
        {
            // Use hash of question text as key
            return clue.Question.GetHashCode().ToString();
        }

        public void ClearCache()
        {
            audioCache.Clear();
            CachedClues = 0;
            TotalClues = 0;
        }
    }
}
