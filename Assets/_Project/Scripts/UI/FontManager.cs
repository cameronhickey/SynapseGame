using UnityEngine;
using TMPro;

namespace Cerebrum.UI
{
    public class FontManager : MonoBehaviour
    {
        public static FontManager Instance { get; private set; }

        [Header("Font Assets")]
        [SerializeField] private TMP_FontAsset categoryFont; // Bebas Neue
        [SerializeField] private TMP_FontAsset clueFont; // Lora

        [Header("Font Paths (fallback)")]
        [SerializeField] private string categoryFontPath = "Fonts/BebasNeue SDF";
        [SerializeField] private string clueFontPath = "Fonts/Lora SDF";

        [Header("Drop Shadow Settings")]
        [SerializeField] private Color shadowColor = Color.black;
        [SerializeField] private Vector2 shadowOffset = new Vector2(2f, -2f);
        [SerializeField] private float shadowDilate = 0.1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFonts();
        }

        private void LoadFonts()
        {
            if (categoryFont == null)
            {
                categoryFont = Resources.Load<TMP_FontAsset>(categoryFontPath);
                if (categoryFont == null)
                {
                    Debug.LogWarning($"[FontManager] Could not load category font from: {categoryFontPath}");
                }
            }

            if (clueFont == null)
            {
                clueFont = Resources.Load<TMP_FontAsset>(clueFontPath);
                if (clueFont == null)
                {
                    Debug.LogWarning($"[FontManager] Could not load clue font from: {clueFontPath}");
                }
            }
        }

        public TMP_FontAsset GetCategoryFont()
        {
            return categoryFont;
        }

        public TMP_FontAsset GetClueFont()
        {
            return clueFont;
        }

        public void ApplyCategoryStyle(TextMeshProUGUI text)
        {
            if (text == null) return;

            if (categoryFont != null)
            {
                text.font = categoryFont;
            }

            ApplyDropShadow(text);
        }

        public void ApplyClueStyle(TextMeshProUGUI text)
        {
            if (text == null) return;

            if (clueFont != null)
            {
                text.font = clueFont;
            }

            ApplyDropShadow(text);
        }

        public void ApplyDropShadow(TextMeshProUGUI text)
        {
            if (text == null) return;

            // Enable shadow via material properties
            text.fontMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontMaterial.SetColor("_UnderlayColor", shadowColor);
            text.fontMaterial.SetFloat("_UnderlayOffsetX", shadowOffset.x);
            text.fontMaterial.SetFloat("_UnderlayOffsetY", shadowOffset.y);
            text.fontMaterial.SetFloat("_UnderlayDilate", shadowDilate);
            text.fontMaterial.SetFloat("_UnderlaySoftness", 0f); // Sharp shadow
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("FontManager");
                obj.AddComponent<FontManager>();
            }
        }
    }
}
