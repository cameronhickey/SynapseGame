using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace Cerebrum.Editor
{
    public class CategoryDataCopier : IPreprocessBuildWithReport
    {
        private const string SOURCE_FOLDER = "Assets/_Project/Data/Categories";
        private const string SOURCE_INDEX = "Assets/_Project/Data/category_index.txt";
        private const string STREAMING_ASSETS = "Assets/StreamingAssets";
        private const string DEST_FOLDER = "Assets/StreamingAssets/Categories";

        public int callbackOrder => 0;

        // Automatically copy categories before build
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[CategoryDataCopier] Pre-build: Copying categories to StreamingAssets...");
            CopyCategoriesToStreamingAssets();
        }

        [MenuItem("Cerebrum/Copy Categories to StreamingAssets")]
        public static void CopyCategoriesToStreamingAssets()
        {
            // Create StreamingAssets folder if needed
            if (!Directory.Exists(STREAMING_ASSETS))
            {
                Directory.CreateDirectory(STREAMING_ASSETS);
            }

            // Create destination folder if needed
            if (!Directory.Exists(DEST_FOLDER))
            {
                Directory.CreateDirectory(DEST_FOLDER);
                Debug.Log($"[CategoryDataCopier] Created folder: {DEST_FOLDER}");
            }

            // Copy index file to StreamingAssets root
            if (File.Exists(SOURCE_INDEX))
            {
                string destIndex = Path.Combine(STREAMING_ASSETS, "category_index.txt");
                File.Copy(SOURCE_INDEX, destIndex, true);
                Debug.Log($"[CategoryDataCopier] Copied index file to StreamingAssets");
            }
            else
            {
                Debug.LogError($"[CategoryDataCopier] Source index not found: {SOURCE_INDEX}");
                return;
            }

            // Copy all category files
            if (!Directory.Exists(SOURCE_FOLDER))
            {
                Debug.LogError($"[CategoryDataCopier] Source folder not found: {SOURCE_FOLDER}");
                return;
            }

            string[] files = Directory.GetFiles(SOURCE_FOLDER, "*.txt");
            int copied = 0;

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(DEST_FOLDER, fileName);
                File.Copy(file, destPath, true);
                copied++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[CategoryDataCopier] Copied {copied} category files to StreamingAssets");
        }

        [MenuItem("Cerebrum/Copy Categories to StreamingAssets", true)]
        public static bool ValidateCopyCategories()
        {
            return Directory.Exists(SOURCE_FOLDER) && File.Exists(SOURCE_INDEX);
        }
    }
}
