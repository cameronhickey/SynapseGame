using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Cerebrum.Data;
using Cerebrum.OpenAI;
using UnityEngine.Networking;

namespace Cerebrum.Editor
{
    public class TestGameGenerator : EditorWindow
    {
        private TestGameConfig config;
        private bool isGenerating;
        private float progress;
        private string statusMessage = "";
        private int totalItems;
        private int completedItems;

        [MenuItem("Cerebrum/Test Game/Open Generator")]
        public static void ShowWindow()
        {
            GetWindow<TestGameGenerator>("Test Game Generator");
        }

        [MenuItem("Cerebrum/Test Game/Select Random Categories")]
        public static void SelectRandomCategories()
        {
            var config = LoadOrCreateConfig();
            if (config == null) return;

            SelectCategoriesForConfig(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[TestGameGenerator] Selected 6 random categories from last 500. Check TestGameConfig asset.");
        }

        [MenuItem("Cerebrum/Test Game/Generate All Audio")]
        public static void GenerateAllAudioMenu()
        {
            var window = GetWindow<TestGameGenerator>("Test Game Generator");
            window.StartGenerateAllAudio();
        }

        private static TestGameConfig LoadOrCreateConfig()
        {
            var config = Resources.Load<TestGameConfig>("TestGameConfig");
            
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TestGameConfig>();
                
                string path = "Assets/_Project/Resources/TestGameConfig.asset";
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"[TestGameGenerator] Created TestGameConfig at {path}");
            }

            return config;
        }

        private static void SelectCategoriesForConfig(TestGameConfig config)
        {
            // Load category files
            var categoryFiles = Resources.LoadAll<TextAsset>("Categories");
            if (categoryFiles.Length == 0)
            {
                Debug.LogError("[TestGameGenerator] No category files found in Resources/Categories");
                return;
            }

            // Get last 500 categories (or all if less than 500)
            int startIndex = Mathf.Max(0, categoryFiles.Length - 500);
            List<TextAsset> candidates = new List<TextAsset>();
            for (int i = startIndex; i < categoryFiles.Length; i++)
            {
                candidates.Add(categoryFiles[i]);
            }

            // Shuffle and pick 6
            System.Random rng = new System.Random();
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }

            config.categories.Clear();
            int[] values = { 200, 400, 600, 800, 1000 };

            for (int c = 0; c < 6 && c < candidates.Count; c++)
            {
                var catFile = candidates[c];
                var lines = catFile.text.Split('\n');
                
                if (lines.Length < 6) continue;

                TestCategory testCat = new TestCategory();
                testCat.title = lines[0].Trim();
                testCat.clues = new List<TestClue>();

                for (int i = 1; i <= 5 && i < lines.Length; i++)
                {
                    var parts = lines[i].Split('|');
                    if (parts.Length >= 3)
                    {
                        TestClue clue = new TestClue();
                        if (int.TryParse(parts[0], out int val))
                        {
                            clue.value = val;
                        }
                        else
                        {
                            clue.value = values[i - 1];
                        }
                        clue.question = parts[1].Replace("\\|", "|").Trim();
                        clue.answer = parts[2].Replace("\\|", "|").Trim();
                        testCat.clues.Add(clue);
                    }
                }

                if (testCat.clues.Count == 5)
                {
                    config.categories.Add(testCat);
                }
            }

            Debug.Log($"[TestGameGenerator] Selected {config.categories.Count} categories");
            foreach (var cat in config.categories)
            {
                Debug.Log($"  - {cat.title} ({cat.clues.Count} clues)");
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Test Game Audio Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            config = (TestGameConfig)EditorGUILayout.ObjectField("Config", config, typeof(TestGameConfig), false);

            if (config == null)
            {
                config = Resources.Load<TestGameConfig>("TestGameConfig");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Load/Create Config"))
            {
                config = LoadOrCreateConfig();
            }

            if (GUILayout.Button("Select Random Categories"))
            {
                if (config != null)
                {
                    SelectCategoriesForConfig(config);
                    EditorUtility.SetDirty(config);
                }
            }

            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(isGenerating || config == null || !config.IsConfigured);
            if (GUILayout.Button("Generate All Audio"))
            {
                StartGenerateAllAudio();
            }
            EditorGUI.EndDisabledGroup();

            if (config != null && !config.IsConfigured)
            {
                EditorGUILayout.HelpBox("Config not ready. Need 6 categories with 5 clues each.", MessageType.Warning);
            }

            GUILayout.Space(10);

            if (isGenerating)
            {
                EditorGUILayout.LabelField("Status:", statusMessage);
                var rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, progress, $"{completedItems}/{totalItems}");
            }
        }

        private List<AudioGenerationTask> pendingTasks = new List<AudioGenerationTask>();
        private int currentTaskIndex;
        private UnityWebRequest activeRequest;

        private class AudioGenerationTask
        {
            public string text;
            public string filePath;
            public string type; // "category", "clue", "answer"
            public int catIndex;
            public int clueIndex;
        }

        private void StartGenerateAllAudio()
        {
            if (config == null || !config.IsConfigured)
            {
                Debug.LogError("[TestGameGenerator] Config not ready");
                return;
            }

            // Build task list
            pendingTasks.Clear();
            string basePath = $"Assets/_Project/Resources/{config.audioBasePath}";
            
            // Create directories
            Directory.CreateDirectory($"{basePath}/Clues");
            Directory.CreateDirectory($"{basePath}/Answers");
            Directory.CreateDirectory($"{basePath}/Categories");

            // Add category tasks
            for (int c = 0; c < config.categories.Count; c++)
            {
                pendingTasks.Add(new AudioGenerationTask
                {
                    text = config.categories[c].title,
                    filePath = $"{basePath}/Categories/cat{c}.wav",
                    type = "category",
                    catIndex = c
                });
            }

            // Add clue tasks
            for (int c = 0; c < config.categories.Count; c++)
            {
                for (int i = 0; i < config.categories[c].clues.Count; i++)
                {
                    pendingTasks.Add(new AudioGenerationTask
                    {
                        text = config.categories[c].clues[i].question,
                        filePath = $"{basePath}/Clues/cat{c}_clue{i}.wav",
                        type = "clue",
                        catIndex = c,
                        clueIndex = i
                    });
                }
            }

            // Add answer tasks
            for (int c = 0; c < config.categories.Count; c++)
            {
                for (int i = 0; i < config.categories[c].clues.Count; i++)
                {
                    pendingTasks.Add(new AudioGenerationTask
                    {
                        text = config.categories[c].clues[i].answer,
                        filePath = $"{basePath}/Answers/cat{c}_answer{i}.wav",
                        type = "answer",
                        catIndex = c,
                        clueIndex = i
                    });
                }
            }

            totalItems = pendingTasks.Count;
            completedItems = 0;
            currentTaskIndex = 0;
            isGenerating = true;
            progress = 0;
            statusMessage = "Starting...";

            EditorApplication.update += ProcessNextTask;
            Repaint();
        }

        private void ProcessNextTask()
        {
            if (!isGenerating)
            {
                EditorApplication.update -= ProcessNextTask;
                return;
            }

            // Check if we have an active request
            if (activeRequest != null)
            {
                if (!activeRequest.isDone)
                    return;

                // Process completed request
                if (activeRequest.result == UnityWebRequest.Result.Success)
                {
                    byte[] audioData = activeRequest.downloadHandler.data;
                    SaveAudioData(audioData, pendingTasks[currentTaskIndex - 1].filePath);
                }
                else
                {
                    Debug.LogWarning($"[TestGameGenerator] Failed: {activeRequest.error}");
                }

                activeRequest.Dispose();
                activeRequest = null;
            }

            // Check if done
            if (currentTaskIndex >= pendingTasks.Count)
            {
                isGenerating = false;
                statusMessage = "Complete!";
                progress = 1f;
                EditorApplication.update -= ProcessNextTask;
                AssetDatabase.Refresh();
                Debug.Log("[TestGameGenerator] All audio generated successfully!");
                Repaint();
                return;
            }

            // Start next task
            var task = pendingTasks[currentTaskIndex];
            statusMessage = $"Generating {task.type} audio... ({currentTaskIndex + 1}/{totalItems})";
            
            var openAIConfig = Resources.Load<OpenAIConfig>("OpenAIConfig");
            if (openAIConfig == null || !openAIConfig.IsConfigured)
            {
                Debug.LogError("[TestGameGenerator] OpenAI not configured");
                isGenerating = false;
                EditorApplication.update -= ProcessNextTask;
                return;
            }

            // Make TTS request
            string url = openAIConfig.GetTTSUrl();
            string json = JsonUtility.ToJson(new TTSRequest
            {
                model = openAIConfig.TTSModel,
                input = task.text,
                voice = openAIConfig.TTSVoice
            });

            activeRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            activeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            activeRequest.downloadHandler = new DownloadHandlerBuffer();
            activeRequest.SetRequestHeader("Content-Type", "application/json");
            activeRequest.SetRequestHeader("Authorization", $"Bearer {openAIConfig.ApiKey}");
            activeRequest.SendWebRequest();

            currentTaskIndex++;
            completedItems = currentTaskIndex;
            progress = (float)completedItems / totalItems;
            Repaint();
        }

        [Serializable]
        private class TTSRequest
        {
            public string model;
            public string input;
            public string voice;
        }

        private void SaveAudioData(byte[] mp3Data, string wavFilePath)
        {
            // Save as MP3 first, Unity will import it
            string mp3Path = wavFilePath.Replace(".wav", ".mp3");
            File.WriteAllBytes(mp3Path, mp3Data);
            Debug.Log($"[TestGameGenerator] Saved: {mp3Path}");
        }

    }
}
