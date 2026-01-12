using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Data;

namespace Cerebrum.OpenAI
{
    /// <summary>
    /// Loads pre-generated phrase audio files bundled with the app.
    /// These are static phrases that don't require runtime TTS generation.
    /// Audio files should be placed in Resources/Audio/Phrases/ with filenames matching phrase IDs.
    /// </summary>
    public class BundledPhraseLoader : MonoBehaviour
    {
        public static BundledPhraseLoader Instance { get; private set; }

        private const string PHRASE_RESOURCE_PATH = "Audio/Phrases"; // Unity finds this in _Project/Resources/Audio/Phrases

        private Dictionary<string, AudioClip> loadedPhrases = new Dictionary<string, AudioClip>();
        private bool isLoaded = false;

        public bool IsLoaded => isLoaded;
        public int LoadedCount => loadedPhrases.Count;

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

        private void Start()
        {
            LoadAllBundledPhrases();
        }

        /// <summary>
        /// Loads all bundled phrase audio clips from Resources.
        /// </summary>
        public void LoadAllBundledPhrases()
        {
            if (isLoaded) return;

            var bundleablePhrases = GamePhrases.GetBundleablePhrases();
            int loaded = 0;
            int missing = 0;

            foreach (var phrase in bundleablePhrases)
            {
                // Unity Resources.Load works without extension - Unity imports MP3 as AudioClip
                string resourcePath = $"{PHRASE_RESOURCE_PATH}/{phrase.Id}";
                AudioClip clip = Resources.Load<AudioClip>(resourcePath);

                if (clip != null)
                {
                    loadedPhrases[phrase.Id] = clip;
                    loaded++;
                }
                else
                {
                    missing++;
                }
            }

            isLoaded = true;
            Debug.Log($"[BundledPhraseLoader] Loaded {loaded}/{bundleablePhrases.Count} bundled phrases ({missing} missing)");
        }

        /// <summary>
        /// Tries to get a bundled phrase audio clip.
        /// </summary>
        public bool TryGetPhrase(string phraseId, out AudioClip clip)
        {
            return loadedPhrases.TryGetValue(phraseId, out clip);
        }

        /// <summary>
        /// Checks if a phrase is available as a bundled clip.
        /// </summary>
        public bool HasPhrase(string phraseId)
        {
            return loadedPhrases.ContainsKey(phraseId);
        }

        /// <summary>
        /// Gets a bundled phrase clip, or null if not available.
        /// </summary>
        public AudioClip GetPhrase(string phraseId)
        {
            loadedPhrases.TryGetValue(phraseId, out AudioClip clip);
            return clip;
        }

        /// <summary>
        /// Gets a random bundled phrase from a category.
        /// Returns null if no bundled phrases exist for that category.
        /// </summary>
        public AudioClip GetRandomPhrase(GamePhrases.PhraseCategory category)
        {
            var categoryPhrases = GamePhrases.GetByCategory(category);
            var bundledInCategory = new List<AudioClip>();

            foreach (var phrase in categoryPhrases)
            {
                if (phrase.IsBundleable && loadedPhrases.TryGetValue(phrase.Id, out AudioClip clip))
                {
                    bundledInCategory.Add(clip);
                }
            }

            if (bundledInCategory.Count == 0) return null;

            return bundledInCategory[Random.Range(0, bundledInCategory.Count)];
        }

        /// <summary>
        /// Returns list of all phrase IDs that are bundled and loaded.
        /// </summary>
        public List<string> GetLoadedPhraseIds()
        {
            return new List<string>(loadedPhrases.Keys);
        }

        /// <summary>
        /// Returns list of bundleable phrase IDs that are NOT yet bundled (missing files).
        /// Useful for identifying what still needs to be generated.
        /// </summary>
        public List<string> GetMissingPhraseIds()
        {
            var missing = new List<string>();
            var bundleablePhrases = GamePhrases.GetBundleablePhrases();

            foreach (var phrase in bundleablePhrases)
            {
                if (!loadedPhrases.ContainsKey(phrase.Id))
                {
                    missing.Add(phrase.Id);
                }
            }

            return missing;
        }
    }
}
