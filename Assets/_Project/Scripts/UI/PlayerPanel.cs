using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;
using System.Collections;

namespace Cerebrum.UI
{
    public class PlayerPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image chooserIndicator;
        [SerializeField] private RectTransform buzzKeyContainer;

        private Outline borderOutline;
        private int playerIndex;
        private GameObject keyIndicator;
        private Coroutine buzzFlashCoroutine;
        private bool isBuzzedIn;
        
        // Colors for buzz-in highlight
        private static readonly Color navyBlue = new Color(0.05f, 0.1f, 0.2f, 1f);
        private Color originalNameColor = Color.white;
        private Color originalBgColor;
        
        // Default buzz keys for each player
        private static readonly string[] buzzKeys = { "Z", "G", "M" };
        
        // Player border colors (cyan, orange, magenta, green)
        private static readonly Color[] playerColors = new Color[]
        {
            new Color(0.3f, 0.8f, 1f, 0.9f),   // Cyan
            new Color(1f, 0.6f, 0.2f, 0.9f),   // Orange
            new Color(0.9f, 0.4f, 0.9f, 0.9f), // Magenta
            new Color(0.4f, 0.9f, 0.5f, 0.9f)  // Green
        };
        
        // Darker versions of player colors for key backgrounds
        private static readonly Color[] darkPlayerColors = new Color[]
        {
            new Color(0.05f, 0.25f, 0.35f, 1f),  // Dark Cyan
            new Color(0.35f, 0.18f, 0.05f, 1f),  // Dark Orange
            new Color(0.3f, 0.1f, 0.3f, 1f),     // Dark Magenta
            new Color(0.1f, 0.3f, 0.12f, 1f)     // Dark Green
        };

        private void Awake()
        {
            borderOutline = GetComponent<Outline>();
        }

        public void SetPlayer(Player player, bool isChooser, int index = -1)
        {
            if (index >= 0)
            {
                playerIndex = index;
                SetBorderColor(index);
                SetBuzzKey(index);
            }

            if (nameText != null)
            {
                nameText.text = player.Name.ToUpper();
            }

            UpdateScore(player.Score);
            SetChooserState(isChooser);
        }
        
        private void SetBuzzKey(int index)
        {
            if (index < 0 || index >= buzzKeys.Length) return;
            
            // Clean up old indicator
            if (keyIndicator != null)
            {
                Destroy(keyIndicator);
            }
            
            // Create keyboard key style indicator
            keyIndicator = CreateKeyIndicator(buzzKeys[index], index);
        }
        
        private GameObject CreateKeyIndicator(string key, int colorIndex)
        {
            Transform parent = buzzKeyContainer != null ? buzzKeyContainer : transform;
            Color darkColor = darkPlayerColors[colorIndex % darkPlayerColors.Length];
            Color lightColor = playerColors[colorIndex % playerColors.Length];
            
            // Main container
            GameObject container = new GameObject("KeyIndicator");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            
            // Position in top-right corner
            containerRect.anchorMin = new Vector2(1f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-8, -8);
            containerRect.sizeDelta = new Vector2(36, 36);
            
            // Outer bevel (light edge - top/left highlight)
            GameObject outerBevel = new GameObject("OuterBevel");
            outerBevel.transform.SetParent(container.transform, false);
            RectTransform outerRect = outerBevel.AddComponent<RectTransform>();
            outerRect.anchorMin = Vector2.zero;
            outerRect.anchorMax = Vector2.one;
            outerRect.offsetMin = Vector2.zero;
            outerRect.offsetMax = Vector2.zero;
            Image outerImg = outerBevel.AddComponent<Image>();
            outerImg.color = new Color(lightColor.r * 0.6f, lightColor.g * 0.6f, lightColor.b * 0.6f, 1f);
            
            // Inner shadow (bottom/right darker edge)
            GameObject innerShadow = new GameObject("InnerShadow");
            innerShadow.transform.SetParent(container.transform, false);
            RectTransform shadowRect = innerShadow.AddComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = new Vector2(2, 0);
            shadowRect.offsetMax = new Vector2(0, -2);
            Image shadowImg = innerShadow.AddComponent<Image>();
            shadowImg.color = new Color(darkColor.r * 0.4f, darkColor.g * 0.4f, darkColor.b * 0.4f, 1f);
            
            // Main key face
            GameObject keyFace = new GameObject("KeyFace");
            keyFace.transform.SetParent(container.transform, false);
            RectTransform faceRect = keyFace.AddComponent<RectTransform>();
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = new Vector2(2, 2);
            faceRect.offsetMax = new Vector2(-2, -2);
            Image faceImg = keyFace.AddComponent<Image>();
            faceImg.color = darkColor;
            
            // Top highlight (subtle gradient simulation)
            GameObject highlight = new GameObject("Highlight");
            highlight.transform.SetParent(container.transform, false);
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0, 0.6f);
            hlRect.anchorMax = new Vector2(1, 1);
            hlRect.offsetMin = new Vector2(3, 0);
            hlRect.offsetMax = new Vector2(-3, -3);
            Image hlImg = highlight.AddComponent<Image>();
            hlImg.color = new Color(1f, 1f, 1f, 0.15f);
            
            // Key letter text
            GameObject textObj = new GameObject("KeyText");
            textObj.transform.SetParent(container.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = key;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return container;
        }

        public void SetPlayer(Player player, bool isChooser)
        {
            SetPlayer(player, isChooser, -1);
        }

        private void SetBorderColor(int index)
        {
            if (borderOutline != null)
            {
                Color borderColor = playerColors[index % playerColors.Length];
                borderOutline.effectColor = borderColor;
            }
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                string prefix = score < 0 ? "-$" : "$";
                scoreText.text = prefix + Mathf.Abs(score).ToString("N0");
            }
        }

        public void SetChooserState(bool isChooser)
        {
            if (chooserIndicator != null)
            {
                chooserIndicator.gameObject.SetActive(isChooser);
            }
            
            // Brighten border when chooser
            if (borderOutline != null)
            {
                Color baseColor = playerColors[playerIndex % playerColors.Length];
                borderOutline.effectColor = isChooser 
                    ? new Color(baseColor.r * 1.2f, baseColor.g * 1.2f, baseColor.b * 1.2f, 1f)
                    : baseColor;
            }
        }
        
        /// <summary>
        /// Trigger buzz-in highlight effect: flash panel bright, invert name to navy blue
        /// </summary>
        public void SetBuzzedIn(bool buzzedIn)
        {
            Debug.Log($"[PlayerPanel] SetBuzzedIn({buzzedIn}) on {gameObject.name}, bgImage={backgroundImage != null}, nameText={nameText != null}");
            
            isBuzzedIn = buzzedIn;
            
            if (buzzFlashCoroutine != null)
            {
                StopCoroutine(buzzFlashCoroutine);
                buzzFlashCoroutine = null;
            }
            
            if (buzzedIn)
            {
                // Store original colors
                if (nameText != null) originalNameColor = nameText.color;
                if (backgroundImage != null) originalBgColor = backgroundImage.color;
                
                Debug.Log($"[PlayerPanel] Starting flash coroutine, originalBgColor={originalBgColor}");
                buzzFlashCoroutine = StartCoroutine(BuzzFlashCoroutine());
            }
            else
            {
                // Reset to normal
                ResetBuzzHighlight();
            }
        }
        
        private IEnumerator BuzzFlashCoroutine()
        {
            Color playerColor = playerColors[playerIndex % playerColors.Length];
            Color brightColor = new Color(
                Mathf.Min(1f, playerColor.r * 2f),
                Mathf.Min(1f, playerColor.g * 2f),
                Mathf.Min(1f, playerColor.b * 2f),
                1f
            );
            
            // Initial flash - set to bright color immediately
            if (backgroundImage != null)
            {
                backgroundImage.color = brightColor;
            }
            if (nameText != null)
            {
                nameText.color = navyBlue;
            }
            if (borderOutline != null)
            {
                borderOutline.effectColor = Color.white;
                borderOutline.effectDistance = new Vector2(5, 5);
            }
            
            // Flash animation - pulse a few times
            float flashDuration = 0.15f;
            int flashCount = 3;
            
            for (int i = 0; i < flashCount && isBuzzedIn; i++)
            {
                // Fade to slightly dimmer
                float elapsed = 0f;
                while (elapsed < flashDuration && isBuzzedIn)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / flashDuration;
                    
                    if (backgroundImage != null)
                    {
                        backgroundImage.color = Color.Lerp(brightColor, playerColor, t * 0.3f);
                    }
                    yield return null;
                }
                
                // Flash back to bright
                elapsed = 0f;
                while (elapsed < flashDuration && isBuzzedIn)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / flashDuration;
                    
                    if (backgroundImage != null)
                    {
                        backgroundImage.color = Color.Lerp(playerColor * 0.7f + brightColor * 0.3f, brightColor, t);
                    }
                    yield return null;
                }
            }
            
            // Hold at bright state while buzzed in
            while (isBuzzedIn)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.color = brightColor;
                }
                yield return null;
            }
        }
        
        private void ResetBuzzHighlight()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = originalBgColor != default ? originalBgColor : new Color(0.05f, 0.08f, 0.15f, 0.9f);
            }
            if (nameText != null)
            {
                nameText.color = originalNameColor != default ? originalNameColor : Color.white;
            }
            if (borderOutline != null)
            {
                Color baseColor = playerColors[playerIndex % playerColors.Length];
                borderOutline.effectColor = baseColor;
                borderOutline.effectDistance = new Vector2(3, 3);
            }
        }
        
        /// <summary>
        /// Clear buzz state (call when clue flow ends)
        /// </summary>
        public void ClearBuzzState()
        {
            SetBuzzedIn(false);
        }
    }
}
