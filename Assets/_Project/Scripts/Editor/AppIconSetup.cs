using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Cerebrum.Editor
{
    public static class AppIconSetup
    {
        private const string ICON_PATH = "Assets/Images/brain_square_1024.png";

        [MenuItem("Cerebrum/Set App Icon")]
        public static void SetAppIcon()
        {
            // Load the icon texture
            Texture2D sourceIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
            
            if (sourceIcon == null)
            {
                Debug.LogError($"[AppIconSetup] Icon not found at: {ICON_PATH}");
                return;
            }

            // Ensure the texture is readable
            string assetPath = AssetDatabase.GetAssetPath(sourceIcon);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.isReadable = true;
                importer.SaveAndReimport();
                
                // Reload after reimport
                sourceIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
            }

            // Get required icon sizes for Standalone
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
            Texture2D[] icons = new Texture2D[sizes.Length];
            
            Debug.Log($"[AppIconSetup] Creating {sizes.Length} icon sizes for Standalone");
            
            for (int i = 0; i < sizes.Length; i++)
            {
                icons[i] = ResizeTexture(sourceIcon, sizes[i], sizes[i]);
                Debug.Log($"[AppIconSetup] Created {sizes[i]}x{sizes[i]} icon");
            }
            
            // Set for Standalone (includes macOS)
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
            
            Debug.Log("[AppIconSetup] App icon set successfully!");
            EditorUtility.DisplayDialog("Success", $"App icon has been set with {sizes.Length} sizes", "OK");
        }

        private static Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            
            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }

        [MenuItem("Cerebrum/Set App Icon", true)]
        public static bool ValidateSetAppIcon()
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH) != null;
        }
    }
}
