using UnityEditor;
using UnityEngine;

namespace Cerebrum.Editor
{
    /// <summary>
    /// Ensures macOS build settings are configured correctly for microphone and speech recognition.
    /// </summary>
    [InitializeOnLoad]
    public static class MacOSBuildSettings
    {
        private const string MicrophoneDescription = "Cerebrum needs microphone access to hear your answers during the game.";
        
        static MacOSBuildSettings()
        {
            // Set microphone usage description if empty
            EnsureMicrophoneDescription();
        }

        [MenuItem("Cerebrum/Configure macOS Build Settings")]
        public static void ConfigureMacOSSettings()
        {
            EnsureMicrophoneDescription();
            Debug.Log("[MacOSBuildSettings] macOS build settings configured!");
        }

        private static void EnsureMicrophoneDescription()
        {
            // For macOS standalone, we need to set via SerializedObject on PlayerSettings
            var playerSettings = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings == null || playerSettings.Length == 0)
            {
                Debug.LogWarning("[MacOSBuildSettings] Could not load ProjectSettings");
                return;
            }

            SerializedObject serializedSettings = new SerializedObject(playerSettings[0]);
            
            // macOS microphone usage description property
            SerializedProperty micDescProp = serializedSettings.FindProperty("macOSMicrophoneUsageDescription");
            
            if (micDescProp != null && string.IsNullOrEmpty(micDescProp.stringValue))
            {
                micDescProp.stringValue = MicrophoneDescription;
                serializedSettings.ApplyModifiedProperties();
                Debug.Log("[MacOSBuildSettings] Set macOS Microphone Usage Description");
            }
            // If property not found or already set, no warning needed - user can configure manually
        }
    }
}
