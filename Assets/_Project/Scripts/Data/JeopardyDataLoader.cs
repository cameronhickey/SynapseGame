using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cerebrum.Data
{
    public class JeopardyDataLoader : MonoBehaviour
    {
        public static JeopardyDataLoader Instance { get; private set; }

        [Header("Data Path")]
        [SerializeField] private string cluesFolder = "Clues";

        [Header("Stats")]
        public int TotalSeasonsLoaded;
        public int TotalCategoriesAvailable;
        public int TotalCluesAvailable;

        private Dictionary<string, Category> allCategories = new Dictionary<string, Category>();
        private List<Category> validCategories = new List<Category>();

        private static readonly int[] ROUND1_VALUES = { 100, 200, 300, 400, 500 };
        private static readonly int[] ROUND2_VALUES = { 200, 400, 600, 800, 1000 };
        private static readonly int[] BOARD_VALUES = { 200, 400, 600, 800, 1000 };

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

        public void LoadAllData()
        {
            allCategories.Clear();
            validCategories.Clear();
            TotalSeasonsLoaded = 0;
            TotalCluesAvailable = 0;

            string dataPath = Path.Combine(Application.dataPath, cluesFolder);

            if (!Directory.Exists(dataPath))
            {
                Debug.LogError($"[JeopardyDataLoader] Clues folder not found at: {dataPath}");
                return;
            }

            string[] tsvFiles = Directory.GetFiles(dataPath, "*.tsv");

            foreach (string filePath in tsvFiles)
            {
                LoadTsvFile(filePath);
                TotalSeasonsLoaded++;
            }

            BuildValidCategories();

            TotalCategoriesAvailable = validCategories.Count;

            Debug.Log($"[JeopardyDataLoader] Loaded {TotalSeasonsLoaded} seasons, {TotalCategoriesAvailable} valid categories, {TotalCluesAvailable} total clues");

            if (TotalCategoriesAvailable < 6)
            {
                Debug.LogError($"[JeopardyDataLoader] FATAL: Only {TotalCategoriesAvailable} valid categories found. Need at least 6!");
            }
        }

        private void LoadTsvFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                if (lines.Length < 2)
                {
                    Debug.LogWarning($"[JeopardyDataLoader] Empty or header-only file: {filePath}");
                    return;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    ParseLine(lines[i]);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JeopardyDataLoader] Error loading {filePath}: {e.Message}");
            }
        }

        private void ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            string[] columns = ParseTsvLine(line);

            if (columns.Length < 7) return;

            try
            {
                int round = ParseInt(columns[0], 1);
                int clueValue = ParseInt(columns[1], 0);
                string categoryName = columns[3].Trim();
                string answer = columns[5].Trim();
                string question = columns[6].Trim();
                string airDate = columns.Length > 7 ? columns[7].Trim() : "";

                if (string.IsNullOrEmpty(categoryName) || string.IsNullOrEmpty(answer))
                    return;

                if (clueValue == 0)
                    return;

                int normalizedValue = NormalizeValue(clueValue, round);
                if (normalizedValue == 0) return;

                if (!allCategories.ContainsKey(categoryName))
                {
                    allCategories[categoryName] = new Category(categoryName);
                }

                Clue clue = new Clue(answer, question, normalizedValue, round, airDate);
                allCategories[categoryName].Clues.Add(clue);
                TotalCluesAvailable++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JeopardyDataLoader] Error parsing line: {e.Message}");
            }
        }

        private string[] ParseTsvLine(string line)
        {
            List<string> columns = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == '\t' && !inQuotes)
                {
                    columns.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            columns.Add(current);
            return columns.ToArray();
        }

        private int ParseInt(string value, int defaultValue)
        {
            if (int.TryParse(value.Trim(), out int result))
                return result;
            return defaultValue;
        }

        private int NormalizeValue(int originalValue, int round)
        {
            if (round == 1)
            {
                for (int i = 0; i < ROUND1_VALUES.Length; i++)
                {
                    if (originalValue == ROUND1_VALUES[i])
                        return BOARD_VALUES[i];
                }
            }

            for (int i = 0; i < ROUND2_VALUES.Length; i++)
            {
                if (originalValue == ROUND2_VALUES[i])
                    return BOARD_VALUES[i];
            }

            return 0;
        }

        private void BuildValidCategories()
        {
            validCategories.Clear();

            foreach (var kvp in allCategories)
            {
                Category category = kvp.Value;

                Dictionary<int, List<Clue>> byValue = new Dictionary<int, List<Clue>>();
                foreach (int v in BOARD_VALUES)
                {
                    byValue[v] = new List<Clue>();
                }

                foreach (var clue in category.Clues)
                {
                    if (byValue.ContainsKey(clue.Value))
                    {
                        byValue[clue.Value].Add(clue);
                    }
                }

                bool hasAllValues = true;
                foreach (int v in BOARD_VALUES)
                {
                    if (byValue[v].Count == 0)
                    {
                        hasAllValues = false;
                        break;
                    }
                }

                if (hasAllValues)
                {
                    validCategories.Add(category);
                }
            }
        }

        public List<Category> GetValidCategories()
        {
            return validCategories;
        }

        public Category GetCategoryByName(string name)
        {
            if (allCategories.ContainsKey(name))
                return allCategories[name];
            return null;
        }
    }
}
