using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    public class TestGameAudioLoader : MonoBehaviour
    {
        public static TestGameAudioLoader Instance { get; private set; }

        private TestGameConfig config;
        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
        private bool isLoaded;

        public bool IsLoaded => isLoaded;

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

        public void LoadTestGameAudio()
        {
            config = Resources.Load<TestGameConfig>("TestGameConfig");
            if (config == null)
            {
                Debug.LogError("[TestGameAudioLoader] TestGameConfig not found");
                return;
            }

            audioCache.Clear();

            // Load category audio
            for (int c = 0; c < config.categories.Count; c++)
            {
                string path = config.GetCategoryAudioPath(c);
                var clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                {
                    audioCache[$"category_{c}"] = clip;
                }
            }

            // Load clue and answer audio
            for (int c = 0; c < config.categories.Count; c++)
            {
                for (int i = 0; i < config.categories[c].clues.Count; i++)
                {
                    string cluePath = config.GetClueAudioPath(c, i);
                    var clueClip = Resources.Load<AudioClip>(cluePath);
                    if (clueClip != null)
                    {
                        audioCache[$"clue_{c}_{i}"] = clueClip;
                    }

                    string answerPath = config.GetAnswerAudioPath(c, i);
                    var answerClip = Resources.Load<AudioClip>(answerPath);
                    if (answerClip != null)
                    {
                        audioCache[$"answer_{c}_{i}"] = answerClip;
                    }
                }
            }

            // Load integrated name phrases for test players
            if (IntegratedPhraseCache.Instance == null)
            {
                var cacheObj = new GameObject("[IntegratedPhraseCache]");
                cacheObj.AddComponent<IntegratedPhraseCache>();
            }
            IntegratedPhraseCache.Instance.LoadTestGamePhrases(config.playerNames);

            // Load player name audio for buzz-in
            LoadPlayerNameAudio(config.playerNames);

            isLoaded = true;
            Debug.Log($"[TestGameAudioLoader] Loaded {audioCache.Count} audio clips for test game");
        }

        public TestGameConfig GetConfig()
        {
            if (config == null)
            {
                config = Resources.Load<TestGameConfig>("TestGameConfig");
            }
            return config;
        }

        public Board CreateTestBoard()
        {
            var cfg = GetConfig();
            return cfg?.CreateTestBoard();
        }

        public string[] GetTestPlayerNames()
        {
            var cfg = GetConfig();
            return cfg?.playerNames ?? new string[] { "Ken", "Amy", "Alex" };
        }

        public AudioClip GetCategoryAudio(int categoryIndex)
        {
            audioCache.TryGetValue($"category_{categoryIndex}", out var clip);
            return clip;
        }

        public AudioClip GetClueAudio(int categoryIndex, int clueIndex)
        {
            audioCache.TryGetValue($"clue_{categoryIndex}_{clueIndex}", out var clip);
            return clip;
        }

        public AudioClip GetClueAudio(Clue clue, Board board)
        {
            if (board == null || clue == null) return null;

            for (int c = 0; c < board.Categories.Count; c++)
            {
                var category = board.Categories[c];
                for (int i = 0; i < category.Clues.Count; i++)
                {
                    if (category.Clues[i] == clue)
                    {
                        return GetClueAudio(c, i);
                    }
                }
            }
            return null;
        }

        public AudioClip GetAnswerAudio(int categoryIndex, int clueIndex)
        {
            audioCache.TryGetValue($"answer_{categoryIndex}_{clueIndex}", out var clip);
            return clip;
        }

        public AudioClip GetAnswerAudio(Clue clue, Board board)
        {
            if (board == null || clue == null) return null;

            for (int c = 0; c < board.Categories.Count; c++)
            {
                var category = board.Categories[c];
                for (int i = 0; i < category.Clues.Count; i++)
                {
                    if (category.Clues[i] == clue)
                    {
                        return GetAnswerAudio(c, i);
                    }
                }
            }
            return null;
        }

        private void LoadPlayerNameAudio(string[] playerNames)
        {
            // Ensure PhraseTTSCache exists
            if (PhraseTTSCache.Instance == null)
            {
                var cacheObj = new GameObject("[PhraseTTSCache]");
                cacheObj.AddComponent<PhraseTTSCache>();
                DontDestroyOnLoad(cacheObj);
            }

            int loaded = 0;
            foreach (var playerName in playerNames)
            {
                string key = $"player_{playerName.ToLowerInvariant().Replace(" ", "_")}";
                string resourcePath = $"Audio/TestGame/PlayerNames/{key}";
                var clip = Resources.Load<AudioClip>(resourcePath);
                if (clip != null)
                {
                    // Add to PhraseTTSCache's player name cache
                    PhraseTTSCache.Instance.AddPlayerNameToCache(playerName, clip);
                    loaded++;
                }
            }
            Debug.Log($"[TestGameAudioLoader] Loaded {loaded} player name audio clips");
        }
    }
}
