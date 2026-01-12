using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Cerebrum.Data;
using Debug = UnityEngine.Debug;

namespace Cerebrum.OpenAI
{
    public class PhraseTTSCache : MonoBehaviour
    {
        public static PhraseTTSCache Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxConcurrentRequests = 10;
        [SerializeField] private bool useLocalCache = true;
        [SerializeField] private string cacheFolder = "PhraseCache";

        [Header("Status")]
        [SerializeField] private int cachedCount;
        [SerializeField] private int totalPhrases;
        [SerializeField] private bool isPreCaching;

        public bool IsPreCaching => isPreCaching;
        public float Progress => totalPhrases > 0 ? (float)cachedCount / totalPhrases : 0f;
        public int CachedCount => cachedCount;
        public int TotalPhrases => totalPhrases;

        public event Action<float> OnProgress;
        public event Action OnComplete;

        private Dictionary<string, AudioClip> phraseCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> playerNameCache = new Dictionary<string, AudioClip>();
        private string localCachePath;
        private int activeRequests;
        private Queue<CacheRequest> pendingRequests = new Queue<CacheRequest>();

        private class CacheRequest
        {
            public string Id;
            public string Text;
            public bool IsPlayerName;
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

            localCachePath = Path.Combine(Application.persistentDataPath, cacheFolder);
            if (useLocalCache && !Directory.Exists(localCachePath))
            {
                Directory.CreateDirectory(localCachePath);
            }

            totalPhrases = GamePhrases.TotalCount;
        }

        public void PreCacheAllPhrases(Action onComplete = null)
        {
            if (isPreCaching)
            {
                Debug.LogWarning("[PhraseTTSCache] Already pre-caching");
                return;
            }

            var timer = Stopwatch.StartNew();
            Debug.Log($"<color=yellow>[PhraseTTSCache]</color> Starting pre-cache of {GamePhrases.TotalCount} phrases...");

            isPreCaching = true;
            cachedCount = 0;
            pendingRequests.Clear();

            // Queue all phrases
            foreach (var phrase in GamePhrases.AllPhrases)
            {
                // Check local cache first
                if (useLocalCache && TryLoadFromDisk(phrase.Id, out AudioClip cached))
                {
                    phraseCache[phrase.Id] = cached;
                    cachedCount++;
                    continue;
                }

                pendingRequests.Enqueue(new CacheRequest
                {
                    Id = phrase.Id,
                    Text = phrase.Text,
                    IsPlayerName = false
                });
            }

            int loadedFromDisk = cachedCount;
            if (loadedFromDisk > 0)
            {
                Debug.Log($"<color=cyan>[PhraseTTSCache]</color> Loaded {loadedFromDisk} phrases from disk cache");
            }

            if (pendingRequests.Count == 0)
            {
                timer.Stop();
                Debug.Log($"<color=green>[PhraseTTSCache]</color> All {cachedCount} phrases loaded from cache in {timer.ElapsedMilliseconds}ms");
                isPreCaching = false;
                OnComplete?.Invoke();
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(ProcessCacheQueue(() =>
            {
                timer.Stop();
                Debug.Log($"<color=green>[PhraseTTSCache]</color> Pre-cached {cachedCount} phrases in {timer.ElapsedMilliseconds}ms");
                isPreCaching = false;
                OnComplete?.Invoke();
                onComplete?.Invoke();
            }));
        }

        public void CachePlayerNames(List<string> playerNames, Action onComplete = null)
        {
            if (playerNames == null || playerNames.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            var timer = Stopwatch.StartNew();
            Debug.Log($"<color=yellow>[PhraseTTSCache]</color> Caching {playerNames.Count} player names...");

            int completed = 0;
            int total = playerNames.Count;

            foreach (var name in playerNames)
            {
                if (string.IsNullOrEmpty(name)) continue;

                string nameKey = GetPlayerNameKey(name);

                // Check if already cached
                if (playerNameCache.ContainsKey(nameKey))
                {
                    completed++;
                    if (completed >= total)
                    {
                        timer.Stop();
                        Debug.Log($"<color=green>[PhraseTTSCache]</color> Player names ready in {timer.ElapsedMilliseconds}ms");
                        onComplete?.Invoke();
                    }
                    continue;
                }

                // Generate via TTS
                OpenAIClient.Instance?.PostTTS(name, (clip) =>
                {
                    if (clip != null)
                    {
                        playerNameCache[nameKey] = clip;
                        Debug.Log($"<color=cyan>[PhraseTTSCache]</color> Cached player name: \"{name}\"");
                    }

                    completed++;
                    if (completed >= total)
                    {
                        timer.Stop();
                        Debug.Log($"<color=green>[PhraseTTSCache]</color> Player names cached in {timer.ElapsedMilliseconds}ms");
                        onComplete?.Invoke();
                    }
                }, (error) =>
                {
                    Debug.LogWarning($"[PhraseTTSCache] TTS error for name: {error}");
                    completed++;
                    if (completed >= total)
                    {
                        timer.Stop();
                        onComplete?.Invoke();
                    }
                });
            }
        }

        private IEnumerator ProcessCacheQueue(Action onComplete)
        {
            while (pendingRequests.Count > 0 || activeRequests > 0)
            {
                // Start new requests up to max concurrent
                while (activeRequests < maxConcurrentRequests && pendingRequests.Count > 0)
                {
                    var request = pendingRequests.Dequeue();
                    StartCoroutine(ProcessRequest(request));
                }

                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator ProcessRequest(CacheRequest request)
        {
            activeRequests++;

            bool done = false;
            AudioClip result = null;

            OpenAIClient.Instance?.PostTTS(request.Text, (clip) =>
            {
                result = clip;
                done = true;
            }, (error) =>
            {
                Debug.LogWarning($"[PhraseTTSCache] TTS error: {error}");
                done = true;
            });

            while (!done)
            {
                yield return null;
            }

            if (result != null)
            {
                if (request.IsPlayerName)
                {
                    playerNameCache[request.Id] = result;
                }
                else
                {
                    phraseCache[request.Id] = result;

                    // Save to disk cache
                    if (useLocalCache)
                    {
                        SaveToDisk(request.Id, result);
                    }
                }

                cachedCount++;
                OnProgress?.Invoke(Progress);
            }

            activeRequests--;
        }

        public bool TryGetPhrase(string phraseId, out AudioClip clip)
        {
            return phraseCache.TryGetValue(phraseId, out clip);
        }

        public bool TryGetPlayerName(string playerName, out AudioClip clip)
        {
            string key = GetPlayerNameKey(playerName);
            return playerNameCache.TryGetValue(key, out clip);
        }

        public void PreloadPlayerName(string playerName, Action onComplete = null)
        {
            string key = GetPlayerNameKey(playerName);
            
            // Already cached?
            if (playerNameCache.ContainsKey(key))
            {
                onComplete?.Invoke();
                return;
            }

            // Try to load from disk
            if (useLocalCache && TryLoadFromDisk(key, out AudioClip cached))
            {
                playerNameCache[key] = cached;
                onComplete?.Invoke();
                return;
            }

            // Generate via TTS
            if (OpenAIClient.Instance != null)
            {
                OpenAIClient.Instance.PostTTS(playerName, (clip) =>
                {
                    if (clip != null)
                    {
                        playerNameCache[key] = clip;
                        
                        // Save to disk cache
                        if (useLocalCache)
                        {
                            SaveToDisk(key, clip);
                        }
                    }
                    onComplete?.Invoke();
                }, (error) =>
                {
                    Debug.LogWarning($"[PhraseTTSCache] TTS error: {error}");
                    onComplete?.Invoke();
                });
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public AudioClip GetRandomPhrase(GamePhrases.PhraseCategory category)
        {
            var phrases = GamePhrases.GetByCategory(category);
            if (phrases.Count == 0) return null;

            var phrase = phrases[UnityEngine.Random.Range(0, phrases.Count)];
            TryGetPhrase(phrase.Id, out AudioClip clip);
            return clip;
        }

        public GamePhrases.Phrase GetRandomPhraseData(GamePhrases.PhraseCategory category)
        {
            var phrases = GamePhrases.GetByCategory(category);
            if (phrases.Count == 0) return null;
            return phrases[UnityEngine.Random.Range(0, phrases.Count)];
        }

        private string GetPlayerNameKey(string name)
        {
            return $"player_{name.ToLowerInvariant().Replace(" ", "_")}";
        }

        private bool TryLoadFromDisk(string phraseId, out AudioClip clip)
        {
            clip = null;
            string path = Path.Combine(localCachePath, $"{phraseId}.wav");

            if (!File.Exists(path)) return false;

            try
            {
                byte[] wavData = File.ReadAllBytes(path);
                clip = WavToAudioClip(wavData, phraseId);
                return clip != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhraseTTSCache] Failed to load {phraseId}: {e.Message}");
                return false;
            }
        }

        private void SaveToDisk(string phraseId, AudioClip clip)
        {
            try
            {
                string path = Path.Combine(localCachePath, $"{phraseId}.wav");
                byte[] wavData = AudioClipToWav(clip);
                File.WriteAllBytes(path, wavData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhraseTTSCache] Failed to save {phraseId}: {e.Message}");
            }
        }

        private AudioClip WavToAudioClip(byte[] wavData, string name)
        {
            if (wavData.Length < 44) return null;

            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int bitsPerSample = BitConverter.ToInt16(wavData, 34);

            int dataStart = 44;
            int dataSize = wavData.Length - 44;
            int sampleCount = dataSize / (bitsPerSample / 8);

            float[] samples = new float[sampleCount];

            if (bitsPerSample == 16)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(wavData, dataStart + i * 2);
                    samples[i] = sample / 32768f;
                }
            }

            AudioClip clip = AudioClip.Create(name, sampleCount / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private byte[] AudioClipToWav(AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int sampleCount = samples.Length;
            int byteCount = sampleCount * 2;

            byte[] wav = new byte[44 + byteCount];

            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
            BitConverter.GetBytes(36 + byteCount).CopyTo(wav, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);

            // fmt chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
            BitConverter.GetBytes(16).CopyTo(wav, 16);
            BitConverter.GetBytes((short)1).CopyTo(wav, 20);
            BitConverter.GetBytes((short)clip.channels).CopyTo(wav, 22);
            BitConverter.GetBytes(clip.frequency).CopyTo(wav, 24);
            BitConverter.GetBytes(clip.frequency * clip.channels * 2).CopyTo(wav, 28);
            BitConverter.GetBytes((short)(clip.channels * 2)).CopyTo(wav, 32);
            BitConverter.GetBytes((short)16).CopyTo(wav, 34);

            // data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
            BitConverter.GetBytes(byteCount).CopyTo(wav, 40);

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(samples[i] * 32767f);
                BitConverter.GetBytes(sample).CopyTo(wav, 44 + i * 2);
            }

            return wav;
        }

        public void ClearCache()
        {
            phraseCache.Clear();
            playerNameCache.Clear();
            cachedCount = 0;
        }
    }
}
