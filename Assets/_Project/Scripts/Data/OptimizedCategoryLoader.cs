using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Cerebrum.Data
{
    public class OptimizedCategoryLoader : MonoBehaviour
    {
        public static OptimizedCategoryLoader Instance { get; private set; }

        // Editor paths (used in Unity Editor)
        private const string EDITOR_CATEGORIES_FOLDER = "Assets/_Project/Data/Categories";
        private const string EDITOR_INDEX_FILE = "Assets/_Project/Data/category_index.txt";
        
        // StreamingAssets paths (used in built apps)
        private const string STREAMING_CATEGORIES_FOLDER = "Categories";
        private const string STREAMING_INDEX_FILE = "category_index.txt";

        public int TotalCategoryCount { get; private set; }
        public bool IsLoaded { get; private set; }
        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(LoadIndexAsync());
        }

        private IEnumerator LoadIndexAsync()
        {
            IsLoading = true;
            
#if UNITY_EDITOR
            // In editor, use direct file access
            if (!File.Exists(EDITOR_INDEX_FILE))
            {
                Debug.LogError($"[OptimizedCategoryLoader] Index file not found: {EDITOR_INDEX_FILE}");
                Debug.LogError("[OptimizedCategoryLoader] Run 'Cerebrum > Preprocess Categories' in the editor first!");
                IsLoading = false;
                yield break;
            }

            string[] lines = File.ReadAllLines(EDITOR_INDEX_FILE);
            if (lines.Length > 0 && int.TryParse(lines[0], out int count))
            {
                TotalCategoryCount = count;
                IsLoaded = true;
                Debug.Log($"[OptimizedCategoryLoader] Index loaded: {TotalCategoryCount} categories available");
            }
            yield return null; // Required for IEnumerator
#else
            // In built app, use StreamingAssets
            // On macOS/Windows, files can be read directly from StreamingAssets
            string indexPath = Path.Combine(Application.streamingAssetsPath, STREAMING_INDEX_FILE);
            Debug.Log($"[OptimizedCategoryLoader] Loading index from: {indexPath}");
            
            try
            {
                if (File.Exists(indexPath))
                {
                    string[] lines = File.ReadAllLines(indexPath);
                    if (lines.Length > 0 && int.TryParse(lines[0], out int count))
                    {
                        TotalCategoryCount = count;
                        IsLoaded = true;
                        Debug.Log($"[OptimizedCategoryLoader] Index loaded from StreamingAssets: {TotalCategoryCount} categories available");
                    }
                }
                else
                {
                    Debug.LogError($"[OptimizedCategoryLoader] Index file not found at: {indexPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OptimizedCategoryLoader] Failed to load index: {e.Message}");
            }
            yield return null; // Required for IEnumerator
#endif
            IsLoading = false;
        }

        public Board LoadRandomBoard(int categoryCount = 6)
        {
            if (!IsLoaded || TotalCategoryCount < categoryCount)
            {
                Debug.LogError($"[OptimizedCategoryLoader] Not enough categories. Need {categoryCount}, have {TotalCategoryCount}");
                return null;
            }

            // Pick random category indices
            HashSet<int> selectedIndices = new HashSet<int>();
            System.Random rng = new System.Random();

            while (selectedIndices.Count < categoryCount)
            {
                int index = rng.Next(0, TotalCategoryCount);
                selectedIndices.Add(index);
            }

            // Load only those category files
            List<Category> categories = new List<Category>();
            
            foreach (int index in selectedIndices)
            {
                Category cat = LoadCategoryFile(index);
                if (cat != null)
                {
                    categories.Add(cat);
                }
            }

            if (categories.Count < categoryCount)
            {
                Debug.LogWarning($"[OptimizedCategoryLoader] Only loaded {categories.Count}/{categoryCount} categories");
            }

            Debug.Log($"[OptimizedCategoryLoader] Loaded {categories.Count} random categories");

            Board board = new Board();
            board.Categories = categories;
            return board;
        }

        private Category LoadCategoryFile(int index)
        {
            string fileName = $"{index:D5}.txt";
            
#if UNITY_EDITOR
            string filePath = Path.Combine(EDITOR_CATEGORIES_FOLDER, fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[OptimizedCategoryLoader] Category file not found: {filePath}");
                return null;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                return ParseCategoryLines(lines, filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[OptimizedCategoryLoader] Error loading {filePath}: {e.Message}");
                return null;
            }
#else
            // In built app, use synchronous file read from StreamingAssets
            string filePath = Path.Combine(Application.streamingAssetsPath, STREAMING_CATEGORIES_FOLDER, fileName);
            
            try
            {
                // On macOS, StreamingAssets files can be read directly
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);
                    return ParseCategoryLines(lines, filePath);
                }
                else
                {
                    Debug.LogWarning($"[OptimizedCategoryLoader] Category file not found: {filePath}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OptimizedCategoryLoader] Error loading {filePath}: {e.Message}");
                return null;
            }
#endif
        }

        private Category ParseCategoryLines(string[] lines, string filePath)
        {
            if (lines.Length < 6)
            {
                Debug.LogWarning($"[OptimizedCategoryLoader] Invalid category file: {filePath}");
                return null;
            }

            string categoryName = SanitizeText(lines[0]);
            Category category = new Category(categoryName);

            // Lines 1-5 contain clues for values 200, 400, 600, 800, 1000
            for (int i = 1; i < lines.Length && i <= 5; i++)
            {
                Clue clue = ParseClueLine(lines[i]);
                if (clue != null)
                {
                    category.Clues.Add(clue);
                }
            }

            return category;
        }

        private Clue ParseClueLine(string line)
        {
            // Format: value|answer|question
            string[] parts = line.Split('|');
            
            if (parts.Length < 3) return null;

            if (!int.TryParse(parts[0], out int value)) return null;

            string answer = SanitizeText(parts[1].Replace("\\|", "|"));
            string question = SanitizeText(parts[2].Replace("\\|", "|"));

            if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(question))
            {
                return null;
            }

            return new Clue(answer, question, value);
        }

        private string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Remove escape characters that shouldn't be displayed
            text = text.Replace("\\\"", "\"");  // \" -> "
            text = text.Replace("\\'", "'");    // \' -> '
            text = text.Replace("\\\\", "\\");  // \\ -> \
            text = text.Replace("\\n", " ");    // \n -> space
            text = text.Replace("\\r", "");     // \r -> remove
            text = text.Replace("\\t", " ");    // \t -> space
            
            return text.Trim();
        }
    }
}
