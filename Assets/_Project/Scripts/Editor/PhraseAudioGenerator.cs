#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Cerebrum.Data;
using Cerebrum.OpenAI;

namespace Cerebrum.Editor
{
    /// <summary>
    /// Editor tool to pre-generate TTS audio for all bundleable game phrases.
    /// Generated files are saved to Resources/Audio/Phrases/ for bundling with the app.
    /// Makes direct API calls so it works in Editor without Play mode.
    /// </summary>
    public class PhraseAudioGenerator : EditorWindow
    {
        private const string OUTPUT_PATH = "Assets/_Project/Resources/Audio/Phrases";

        private Vector2 scrollPosition;
        private bool isGenerating = false;
        private int currentIndex = 0;
        private int totalCount = 0;
        private string currentPhrase = "";
        private List<string> generatedPhrases = new List<string>();
        private List<string> failedPhrases = new List<string>();
        private List<string> skippedPhrases = new List<string>();

        private OpenAIConfig config;
        private UnityWebRequest activeRequest;

        [MenuItem("Cerebrum/Generate Phrase Audio")]
        public static void ShowWindow()
        {
            var window = GetWindow<PhraseAudioGenerator>("Phrase Audio Generator");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Phrase Audio Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Stats
            var bundleable = GamePhrases.GetBundleablePhrases();
            var runtime = GamePhrases.GetRuntimePhrases();

            EditorGUILayout.LabelField("Total Phrases:", GamePhrases.TotalCount.ToString());
            EditorGUILayout.LabelField("Bundleable (static):", bundleable.Count.ToString());
            EditorGUILayout.LabelField("Runtime-only (with names):", runtime.Count.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Output Path: {OUTPUT_PATH}");

            // Show current voice config
            var config = Resources.Load<OpenAIConfig>("OpenAIConfig");
            if (config != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Voice Settings (from OpenAIConfig):", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Voice: {config.Voice}");
                EditorGUILayout.LabelField($"  Model: {config.Model}");
                EditorGUILayout.LabelField($"  Speed: {config.TTSSpeed}");
            }
            else
            {
                EditorGUILayout.HelpBox("OpenAIConfig not found in Resources!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Check existing files
            int existingCount = CountExistingFiles();
            EditorGUILayout.LabelField($"Already Generated: {existingCount}/{bundleable.Count}");

            EditorGUILayout.Space();

            // Generation controls
            EditorGUI.BeginDisabledGroup(isGenerating);

            if (GUILayout.Button("Generate Missing Phrases", GUILayout.Height(30)))
            {
                StartGeneration(false);
            }

            if (GUILayout.Button("Regenerate All Phrases", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Regenerate All?",
                    "This will regenerate all phrase audio files, even existing ones. Continue?",
                    "Yes", "Cancel"))
                {
                    StartGeneration(true);
                }
            }

            EditorGUI.EndDisabledGroup();

            // Progress
            if (isGenerating)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Generating...", EditorStyles.boldLabel);
                
                float progress = totalCount > 0 ? (float)currentIndex / totalCount : 0;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), progress,
                    $"{currentIndex}/{totalCount} - {currentPhrase}");

                if (GUILayout.Button("Cancel"))
                {
                    isGenerating = false;
                }
            }

            // Results
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (generatedPhrases.Count > 0)
            {
                EditorGUILayout.LabelField($"Generated ({generatedPhrases.Count}):", EditorStyles.boldLabel);
                foreach (var phrase in generatedPhrases)
                {
                    EditorGUILayout.LabelField($"  ✓ {phrase}", EditorStyles.miniLabel);
                }
            }

            if (skippedPhrases.Count > 0)
            {
                EditorGUILayout.LabelField($"Skipped ({skippedPhrases.Count}):", EditorStyles.boldLabel);
                foreach (var phrase in skippedPhrases)
                {
                    EditorGUILayout.LabelField($"  - {phrase}", EditorStyles.miniLabel);
                }
            }

            if (failedPhrases.Count > 0)
            {
                EditorGUILayout.LabelField($"Failed ({failedPhrases.Count}):", EditorStyles.boldLabel);
                foreach (var phrase in failedPhrases)
                {
                    EditorGUILayout.LabelField($"  ✗ {phrase}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            // Phrase list preview
            EditorGUILayout.Space();
            if (GUILayout.Button("Show All Bundleable Phrases"))
            {
                ShowPhraseList();
            }
        }

        private int CountExistingFiles()
        {
            if (!Directory.Exists(OUTPUT_PATH)) return 0;

            int count = 0;
            var bundleable = GamePhrases.GetBundleablePhrases();
            foreach (var phrase in bundleable)
            {
                string filePath = Path.Combine(OUTPUT_PATH, $"{phrase.Id}.mp3");
                if (File.Exists(filePath)) count++;
            }
            return count;
        }

        private void StartGeneration(bool regenerateAll)
        {
            generatedPhrases.Clear();
            failedPhrases.Clear();
            skippedPhrases.Clear();

            // Load config
            config = Resources.Load<OpenAIConfig>("OpenAIConfig");
            if (config == null || !config.IsConfigured)
            {
                EditorUtility.DisplayDialog("Error", "OpenAIConfig not found or API key not set!", "OK");
                return;
            }

            // Ensure output directory exists
            if (!Directory.Exists(OUTPUT_PATH))
            {
                Directory.CreateDirectory(OUTPUT_PATH);
            }

            isGenerating = true;
            currentIndex = 0;

            var bundleable = GamePhrases.GetBundleablePhrases();
            totalCount = bundleable.Count;

            EditorApplication.update += GenerationUpdate;

            // Store generation state
            generationQueue = new Queue<GamePhrases.Phrase>(bundleable);
            regenerateExisting = regenerateAll;
        }

        private Queue<GamePhrases.Phrase> generationQueue;
        private bool regenerateExisting;
        private bool waitingForResponse;

        private void GenerationUpdate()
        {
            if (!isGenerating)
            {
                EditorApplication.update -= GenerationUpdate;
                if (activeRequest != null)
                {
                    activeRequest.Dispose();
                    activeRequest = null;
                }
                return;
            }

            // Check if we're waiting for an active request
            if (waitingForResponse && activeRequest != null)
            {
                if (!activeRequest.isDone) return;

                // Request completed
                if (activeRequest.result == UnityWebRequest.Result.Success)
                {
                    // Save MP3 directly
                    try
                    {
                        File.WriteAllBytes(currentFilePath, activeRequest.downloadHandler.data);
                        generatedPhrases.Add(currentPhraseId);
                        Debug.Log($"[PhraseAudioGenerator] Saved: {currentFilePath}");
                    }
                    catch (Exception e)
                    {
                        failedPhrases.Add($"{currentPhraseId} (save error: {e.Message})");
                    }
                }
                else
                {
                    failedPhrases.Add($"{currentPhraseId} ({activeRequest.error})");
                }

                activeRequest.Dispose();
                activeRequest = null;
                waitingForResponse = false;
                Repaint();
                return;
            }

            if (generationQueue.Count == 0)
            {
                // Done
                isGenerating = false;
                EditorApplication.update -= GenerationUpdate;
                AssetDatabase.Refresh();
                Debug.Log($"[PhraseAudioGenerator] Complete! Generated: {generatedPhrases.Count}, Skipped: {skippedPhrases.Count}, Failed: {failedPhrases.Count}");
                Repaint();
                return;
            }

            var phrase = generationQueue.Dequeue();
            currentIndex++;
            currentPhrase = phrase.Id;

            string filePath = Path.Combine(OUTPUT_PATH, $"{phrase.Id}.mp3");

            // Skip if exists and not regenerating
            if (!regenerateExisting && File.Exists(filePath))
            {
                skippedPhrases.Add(phrase.Id);
                Repaint();
                return;
            }

            // Generate via TTS API
            waitingForResponse = true;
            GeneratePhraseAudio(phrase, filePath);
        }

        private void GeneratePhraseAudio(GamePhrases.Phrase phrase, string filePath)
        {
            // Build request body
            var requestBody = new TTSRequestBody
            {
                model = config.TTSModelString,
                input = phrase.Text,
                voice = config.TTSVoiceString,
                speed = config.TTSSpeed,
                response_format = "mp3"
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            string url = config.BaseUrl + config.TTSEndpoint;
            activeRequest = new UnityWebRequest(url, "POST");
            activeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            activeRequest.downloadHandler = new DownloadHandlerBuffer();
            activeRequest.SetRequestHeader("Content-Type", "application/json");
            activeRequest.SetRequestHeader("Authorization", "Bearer " + config.ApiKey);

            activeRequest.SendWebRequest();

            // Poll for completion in GenerationUpdate
            currentPhraseId = phrase.Id;
            currentFilePath = filePath;
        }

        private string currentPhraseId;
        private string currentFilePath;

        [Serializable]
        private class TTSRequestBody
        {
            public string model;
            public string input;
            public string voice;
            public float speed;
            public string response_format;
        }

        private void ShowPhraseList()
        {
            var bundleable = GamePhrases.GetBundleablePhrases();
            Debug.Log($"=== Bundleable Phrases ({bundleable.Count}) ===");
            foreach (var phrase in bundleable)
            {
                Debug.Log($"  [{phrase.Id}] \"{phrase.Text}\"");
            }

            var runtime = GamePhrases.GetRuntimePhrases();
            Debug.Log($"=== Runtime Phrases ({runtime.Count}) ===");
            foreach (var phrase in runtime)
            {
                string prefix = phrase.NamePrefix ? "[Name] → " : "";
                string suffix = phrase.NameSuffix ? " → [Name]" : "";
                Debug.Log($"  [{phrase.Id}] {prefix}\"{phrase.Text}\"{suffix}");
            }
        }
    }
}
#endif
