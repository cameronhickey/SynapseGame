using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Diagnostics;
using UnityEngine;

namespace Cerebrum.Editor
{
    public class MacOSPostBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // Path to entitlements file (relative to Assets folder)
        private const string EntitlementsPath = "Assets/Plugins/macOS/Cerebrum.entitlements";

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX)
                return;

            string appPath = report.summary.outputPath;
            
            UpdateInfoPlist(appPath);
            CodeSignApp(appPath);
        }

        private void UpdateInfoPlist(string appPath)
        {
            string plistPath = Path.Combine(appPath, "Contents", "Info.plist");

            if (!File.Exists(plistPath))
            {
                UnityEngine.Debug.LogWarning($"[MacOSPostBuild] Info.plist not found at: {plistPath}");
                return;
            }

            string plist = File.ReadAllText(plistPath);
            bool modified = false;

            // Find the LAST </dict> which is the root dict closing tag
            // Insert new keys before the final </dict></plist>
            string endMarker = "</dict>\n</plist>";
            
            // Add Speech Recognition permission at root level
            if (!plist.Contains("NSSpeechRecognitionUsageDescription"))
            {
                string newEntry = "\t<key>NSSpeechRecognitionUsageDescription</key>\n" +
                    "\t<string>Cerebrum uses speech recognition for voice-controlled gameplay and answering questions.</string>\n";
                plist = plist.Replace(endMarker, newEntry + endMarker);
                modified = true;
                UnityEngine.Debug.Log("[MacOSPostBuild] Added NSSpeechRecognitionUsageDescription");
            }

            // Add Microphone permission at root level
            if (!plist.Contains("NSMicrophoneUsageDescription"))
            {
                string newEntry = "\t<key>NSMicrophoneUsageDescription</key>\n" +
                    "\t<string>Cerebrum needs microphone access to hear your answers during the game.</string>\n";
                plist = plist.Replace(endMarker, newEntry + endMarker);
                modified = true;
                UnityEngine.Debug.Log("[MacOSPostBuild] Added NSMicrophoneUsageDescription");
            }

            if (modified)
            {
                File.WriteAllText(plistPath, plist);
                UnityEngine.Debug.Log($"[MacOSPostBuild] Successfully updated Info.plist at: {plistPath}");
            }
            else
            {
                UnityEngine.Debug.Log("[MacOSPostBuild] Info.plist already contains required permissions");
            }
        }

        private void CodeSignApp(string appPath)
        {
            // Get absolute path to entitlements file
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string entitlementsFullPath = Path.Combine(projectPath, EntitlementsPath);

            if (!File.Exists(entitlementsFullPath))
            {
                UnityEngine.Debug.LogWarning($"[MacOSPostBuild] Entitlements file not found at: {entitlementsFullPath}");
                UnityEngine.Debug.LogWarning("[MacOSPostBuild] Skipping code signing. Run manually: codesign --force --deep --sign - --options runtime \"" + appPath + "\"");
                return;
            }

            UnityEngine.Debug.Log("[MacOSPostBuild] Code signing app with hardened runtime...");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/codesign",
                    Arguments = $"--force --deep --sign - --entitlements \"{entitlementsFullPath}\" --options runtime \"{appPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log("[MacOSPostBuild] Code signing successful!");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[MacOSPostBuild] Code signing failed: {error}");
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[MacOSPostBuild] Code signing error: {e.Message}");
            }
        }
    }
}
