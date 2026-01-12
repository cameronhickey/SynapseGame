using UnityEditor;
using UnityEngine;
using System.IO;

namespace Cerebrum.Editor
{
    public class CategoryDataCopier : MonoBehaviour
    {
        private const string SOURCE_FOLDER = "Assets/_Project/Data/Categories";
        private const string SOURCE_INDEX = "Assets/_Project/Data/category_index.txt";
        private const string DEST_FOLDER = "Assets/_Project/Resources/Categories";

        [MenuItem("Cerebrum/Copy Categories to Resources")]
        public static void CopyCategoriesToResources()
        {
            // Create destination folder if needed
            if (!Directory.Exists(DEST_FOLDER))
            {
                Directory.CreateDirectory(DEST_FOLDER);
                Debug.Log($"[CategoryDataCopier] Created folder: {DEST_FOLDER}");
            }

            // Copy index file (remove .txt extension for Resources.Load)
            if (File.Exists(SOURCE_INDEX))
            {
                string destIndex = Path.Combine(DEST_FOLDER, "category_index.txt");
                File.Copy(SOURCE_INDEX, destIndex, true);
                Debug.Log($"[CategoryDataCopier] Copied index file");
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
            Debug.Log($"[CategoryDataCopier] Copied {copied} category files to Resources");
            EditorUtility.DisplayDialog("Complete", $"Copied {copied} category files to Resources/Categories.\n\nYou can now build the app with category data included.", "OK");
        }

        [MenuItem("Cerebrum/Copy Categories to Resources", true)]
        public static bool ValidateCopyCategories()
        {
            return Directory.Exists(SOURCE_FOLDER) && File.Exists(SOURCE_INDEX);
        }
    }
}
