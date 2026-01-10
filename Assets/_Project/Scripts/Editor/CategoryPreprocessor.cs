using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Cerebrum.Editor
{
    public class CategoryPreprocessor : UnityEditor.Editor
    {
        private const string SOURCE_FOLDER = "Assets/Clues";
        private const string OUTPUT_FOLDER = "Assets/_Project/Data/Categories";
        private const string INDEX_FILE = "Assets/_Project/Data/category_index.txt";

        [MenuItem("Cerebrum/Preprocess Categories")]
        public static void PreprocessCategories()
        {
            // Ensure output directory exists
            if (!Directory.Exists(OUTPUT_FOLDER))
            {
                Directory.CreateDirectory(OUTPUT_FOLDER);
            }

            // Clear existing category files
            string[] existingFiles = Directory.GetFiles(OUTPUT_FOLDER, "*.txt");
            foreach (string file in existingFiles)
            {
                File.Delete(file);
            }

            Dictionary<string, List<ClueData>> allCategories = new Dictionary<string, List<ClueData>>();

            // Find all TSV files
            string[] tsvFiles = Directory.GetFiles(SOURCE_FOLDER, "*.tsv");
            Debug.Log($"[CategoryPreprocessor] Found {tsvFiles.Length} TSV files");

            int totalClues = 0;

            foreach (string tsvFile in tsvFiles)
            {
                string[] lines = File.ReadAllLines(tsvFile);
                
                // Skip header
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    ClueData clue = ParseClue(line);
                    if (clue == null || string.IsNullOrWhiteSpace(clue.Category)) continue;

                    string categoryKey = NormalizeCategoryName(clue.Category);
                    
                    if (!allCategories.ContainsKey(categoryKey))
                    {
                        allCategories[categoryKey] = new List<ClueData>();
                    }
                    
                    allCategories[categoryKey].Add(clue);
                    totalClues++;
                }
            }

            Debug.Log($"[CategoryPreprocessor] Parsed {totalClues} clues into {allCategories.Count} unique categories");

            // Filter to valid categories (at least 5 clues with distinct values)
            List<string> validCategories = new List<string>();
            int[] requiredValues = { 200, 400, 600, 800, 1000 };

            foreach (var kvp in allCategories)
            {
                if (HasCluesForAllValues(kvp.Value, requiredValues))
                {
                    validCategories.Add(kvp.Key);
                }
            }

            Debug.Log($"[CategoryPreprocessor] {validCategories.Count} categories have clues for all 5 values");

            // Write each valid category to its own file
            StringBuilder indexBuilder = new StringBuilder();
            indexBuilder.AppendLine(validCategories.Count.ToString());

            for (int i = 0; i < validCategories.Count; i++)
            {
                string categoryKey = validCategories[i];
                List<ClueData> clues = allCategories[categoryKey];
                
                string fileName = $"{i:D5}.txt";
                string filePath = Path.Combine(OUTPUT_FOLDER, fileName);

                WriteCategoryFile(filePath, clues[0].Category, clues, requiredValues);
                
                // Index: number|original_category_name
                indexBuilder.AppendLine($"{i}|{clues[0].Category}");

                if (i % 1000 == 0)
                {
                    EditorUtility.DisplayProgressBar("Processing Categories", 
                        $"Writing category {i}/{validCategories.Count}", 
                        (float)i / validCategories.Count);
                }
            }

            EditorUtility.ClearProgressBar();

            // Write index file
            File.WriteAllText(INDEX_FILE, indexBuilder.ToString());

            AssetDatabase.Refresh();

            Debug.Log($"[CategoryPreprocessor] Complete! Wrote {validCategories.Count} category files to {OUTPUT_FOLDER}");
            Debug.Log($"[CategoryPreprocessor] Index file written to {INDEX_FILE}");
        }

        private static ClueData ParseClue(string line)
        {
            // TSV format: round, clue_value, daily_double_value, category, comments, answer, question, air_date, notes
            string[] parts = line.Split('\t');
            
            if (parts.Length < 7) return null;

            ClueData clue = new ClueData();
            
            // Parse value
            string valueStr = parts[1].Trim();
            if (valueStr.StartsWith("$")) valueStr = valueStr.Substring(1);
            valueStr = valueStr.Replace(",", "");
            
            if (!int.TryParse(valueStr, out clue.Value))
            {
                // Try to infer from round
                string round = parts[0].Trim().ToLower();
                if (round.Contains("double"))
                {
                    // Double Jeopardy values are 2x
                    return null; // Skip for simplicity
                }
            }

            clue.Category = parts[3].Trim();
            clue.Answer = parts[5].Trim();   // "answer" in TSV is actually the clue text
            clue.Question = parts[6].Trim(); // "question" is the correct response
            
            if (parts.Length > 7)
            {
                clue.AirDate = parts[7].Trim();
            }

            return clue;
        }

        private static string NormalizeCategoryName(string name)
        {
            return name.ToLowerInvariant().Trim();
        }

        private static bool HasCluesForAllValues(List<ClueData> clues, int[] requiredValues)
        {
            HashSet<int> foundValues = new HashSet<int>();
            
            foreach (var clue in clues)
            {
                foreach (int val in requiredValues)
                {
                    if (clue.Value == val)
                    {
                        foundValues.Add(val);
                    }
                }
            }

            return foundValues.Count >= requiredValues.Length;
        }

        private static void WriteCategoryFile(string path, string categoryName, List<ClueData> clues, int[] requiredValues)
        {
            StringBuilder sb = new StringBuilder();
            
            // First line: category name
            sb.AppendLine(categoryName);
            
            // Next 5 lines: one clue per value (200, 400, 600, 800, 1000)
            foreach (int value in requiredValues)
            {
                ClueData clue = null;
                foreach (var c in clues)
                {
                    if (c.Value == value)
                    {
                        clue = c;
                        break;
                    }
                }

                if (clue != null)
                {
                    // Format: value|answer|question
                    // Escape pipes in content
                    string answer = clue.Answer.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
                    string question = clue.Question.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
                    sb.AppendLine($"{value}|{answer}|{question}");
                }
                else
                {
                    sb.AppendLine($"{value}||");
                }
            }

            File.WriteAllText(path, sb.ToString());
        }

        private class ClueData
        {
            public string Category;
            public int Value;
            public string Answer;   // The clue text shown to players
            public string Question; // The correct response
            public string AirDate;
        }
    }
}
