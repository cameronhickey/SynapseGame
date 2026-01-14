#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Cerebrum.Game;
using Cerebrum.UI;

namespace Cerebrum.Editor
{
    public class Scene3DSetupEditor : EditorWindow
    {
        [MenuItem("Cerebrum/Create Game3D Scene")]
        public static void CreateGame3DScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Create GameManager if it doesn't exist
            CreateGameManager();
            
            // Create BoardController3D
            CreateBoardController3D();
            
            // Create AnswerFlowController
            CreateAnswerFlowController();
            
            // Create QuestionTimerBar
            CreateQuestionTimerBar();
            
            // Create required services
            CreateServices();
            
            // Save the scene
            string scenePath = "Assets/_Project/Scenes/Game3D.unity";
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(scenePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            // Add to build settings if not already there
            AddSceneToBuildSettings(scenePath);
            
            Debug.Log($"[Scene3DSetup] Game3D scene created at {scenePath}");
            EditorUtility.DisplayDialog("Scene Created", 
                $"Game3D scene has been created at:\n{scenePath}\n\nIt has been added to Build Settings.", 
                "OK");
        }

        private static void CreateGameManager()
        {
            if (FindFirstObjectByType<GameManager>() != null) return;
            
            GameObject gmObj = new GameObject("[GameManager]");
            gmObj.AddComponent<GameManager>();
            
            // Make it persist across scenes
            // Note: DontDestroyOnLoad will be called in GameManager's Awake
        }

        private static void CreateBoardController3D()
        {
            GameObject boardObj = new GameObject("BoardController3D");
            boardObj.AddComponent<BoardController3D>();
        }

        private static void CreateAnswerFlowController()
        {
            if (FindFirstObjectByType<AnswerFlowController>() != null) return;
            
            GameObject flowObj = new GameObject("AnswerFlowController");
            flowObj.AddComponent<AnswerFlowController>();
        }

        private static void CreateQuestionTimerBar()
        {
            if (FindFirstObjectByType<QuestionTimerBar>() != null) return;
            
            GameObject timerObj = new GameObject("QuestionTimerBar");
            timerObj.AddComponent<QuestionTimerBar>();
        }

        private static void CreateServices()
        {
            // TTSService
            if (FindFirstObjectByType<OpenAI.TTSService>() == null)
            {
                GameObject ttsObj = new GameObject("[TTSService]");
                ttsObj.AddComponent<OpenAI.TTSService>();
            }
            
            // STTService
            if (FindFirstObjectByType<OpenAI.STTService>() == null)
            {
                GameObject sttObj = new GameObject("[STTService]");
                sttObj.AddComponent<OpenAI.STTService>();
            }
            
            // MicrophoneRecorder
            if (FindFirstObjectByType<OpenAI.MicrophoneRecorder>() == null)
            {
                GameObject micObj = new GameObject("[MicrophoneRecorder]");
                micObj.AddComponent<OpenAI.MicrophoneRecorder>();
            }
            
            // AnswerJudge
            if (FindFirstObjectByType<OpenAI.AnswerJudge>() == null)
            {
                GameObject judgeObj = new GameObject("[AnswerJudge]");
                judgeObj.AddComponent<OpenAI.AnswerJudge>();
            }
            
            // PhrasePlayer
            if (FindFirstObjectByType<OpenAI.PhrasePlayer>() == null)
            {
                GameObject phraseObj = new GameObject("[PhrasePlayer]");
                phraseObj.AddComponent<OpenAI.PhrasePlayer>();
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            
            // Check if scene is already in build settings
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    return; // Already added
                }
            }
            
            // Add new scene
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            
            Debug.Log($"[Scene3DSetup] Added {scenePath} to Build Settings");
        }

        [MenuItem("Cerebrum/Setup Existing Scene as 3D Game")]
        public static void SetupExistingSceneAs3D()
        {
            // For converting an existing scene to use 3D board
            if (!EditorUtility.DisplayDialog("Setup 3D Game",
                "This will add 3D game components to the current scene. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }
            
            CreateGameManager();
            CreateBoardController3D();
            CreateAnswerFlowController();
            CreateQuestionTimerBar();
            CreateServices();
            
            Debug.Log("[Scene3DSetup] Added 3D game components to current scene");
        }
    }
}
#endif
