using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;
using Cerebrum.Game;
using Cerebrum.OpenAI;

namespace Cerebrum.UI
{
    /// <summary>
    /// Displays an animated introduction of all categories before the game starts.
    /// Shows each category as a blue rounded card sliding right-to-left with audio.
    /// </summary>
    public class CategoryIntroSequence : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float slideInDuration = 0.4f;
        [SerializeField] private float holdDuration = 0.3f;
        [SerializeField] private float slideOutDuration = 0.3f;
        [SerializeField] private float delayBetweenCategories = 0.1f;

        [Header("Card Style")]
        [SerializeField] private Color cardColor = new Color(0.1f, 0.3f, 0.7f, 1f); // Blue
        // cardCornerRadius reserved for future rounded sprite support
        [SerializeField] private float cardWidth = 900f;
        [SerializeField] private float cardHeight = 300f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        public event Action OnIntroComplete;

        private Canvas canvas;
        private GameObject introPanel;
        private GameObject categoryCard;
        private Image cardImage;
        private TextMeshProUGUI categoryText;
        private TextMeshProUGUI shadowText;
        private Image backgroundImage;
        private RectTransform panelRect;
        private RectTransform cardRect;

        private bool isPlaying;
        private int currentCategoryIndex;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void PlayIntro(Board board, Action onComplete = null)
        {
            if (board == null || board.Categories == null || board.Categories.Count == 0)
            {
                Debug.LogWarning("[CategoryIntroSequence] No categories to introduce");
                onComplete?.Invoke();
                OnIntroComplete?.Invoke();
                return;
            }

            if (isPlaying)
            {
                Debug.LogWarning("[CategoryIntroSequence] Already playing");
                return;
            }

            StartCoroutine(PlayIntroSequence(board, onComplete));
        }

        private IEnumerator PlayIntroSequence(Board board, Action onComplete)
        {
            isPlaying = true;
            currentCategoryIndex = 0;

            // Find or create canvas
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[CategoryIntroSequence] No canvas found");
                isPlaying = false;
                onComplete?.Invoke();
                yield break;
            }

            // Create intro UI
            CreateIntroUI();

            // Play "Our categories are..." intro phrase
            yield return PlayIntroPhrase();

            // Show each category
            foreach (var category in board.Categories)
            {
                yield return ShowCategory(category.Title);
                yield return new WaitForSeconds(delayBetweenCategories);
            }

            // Play "Let's start!" outro
            yield return PlayOutroPhrase();

            // Clean up
            if (introPanel != null)
            {
                Destroy(introPanel);
            }

            isPlaying = false;
            onComplete?.Invoke();
            OnIntroComplete?.Invoke();
        }

        private void CreateIntroUI()
        {
            // Create overlay panel
            introPanel = new GameObject("CategoryIntroPanel");
            introPanel.transform.SetParent(canvas.transform, false);

            panelRect = introPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Dark semi-transparent background
            backgroundImage = introPanel.AddComponent<Image>();
            backgroundImage.color = new Color(0.02f, 0.02f, 0.08f, 0.95f);

            // Create category card (blue rounded rectangle)
            categoryCard = new GameObject("CategoryCard");
            categoryCard.transform.SetParent(introPanel.transform, false);

            cardRect = categoryCard.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            cardRect.anchoredPosition = new Vector2(Screen.width + cardWidth, 0); // Start off-screen right

            // Card background image
            cardImage = categoryCard.AddComponent<Image>();
            cardImage.color = cardColor;
            
            // Try to use rounded sprite if available, otherwise solid color
            var roundedSprite = Resources.Load<Sprite>("UI/RoundedRect");
            if (roundedSprite != null)
            {
                cardImage.sprite = roundedSprite;
                cardImage.type = Image.Type.Sliced;
            }

            // Shadow text (black, offset down-right)
            GameObject shadowObj = new GameObject("ShadowText");
            shadowObj.transform.SetParent(categoryCard.transform, false);

            RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = new Vector2(4, -4); // Offset for shadow effect
            shadowRect.offsetMax = new Vector2(4, -4);

            shadowText = shadowObj.AddComponent<TextMeshProUGUI>();
            shadowText.fontSize = 64;
            shadowText.fontStyle = FontStyles.Bold;
            shadowText.color = Color.black;
            shadowText.alignment = TextAlignmentOptions.Center;
            shadowText.textWrappingMode = TextWrappingModes.Normal;

            // Apply category header font
            if (FontManager.Instance != null)
            {
                shadowText.font = FontManager.Instance.GetCategoryFont();
            }

            // Main text (white, on top of shadow)
            GameObject textObj = new GameObject("CategoryText");
            textObj.transform.SetParent(categoryCard.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            categoryText = textObj.AddComponent<TextMeshProUGUI>();
            categoryText.fontSize = 64;
            categoryText.fontStyle = FontStyles.Bold;
            categoryText.color = Color.white;
            categoryText.alignment = TextAlignmentOptions.Center;
            categoryText.textWrappingMode = TextWrappingModes.Normal;

            // Apply category header font
            if (FontManager.Instance != null)
            {
                categoryText.font = FontManager.Instance.GetCategoryFont();
            }

            // Hide card initially
            categoryCard.SetActive(false);
        }

        private IEnumerator PlayIntroPhrase()
        {
            // Audio only - no text displayed for intro phrase
            bool done = false;
            var phrasePlayer = FindFirstObjectByType<PhrasePlayer>();
            
            if (phrasePlayer != null)
            {
                phrasePlayer.PlayPhrase("categories_intro", () => done = true);
                
                float timeout = 5f;
                float elapsed = 0f;
                while (!done && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator ShowCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) yield break;

            float screenWidth = Screen.width;

            // Set text on both main and shadow
            categoryText.text = categoryName.ToUpper();
            shadowText.text = categoryName.ToUpper();

            // Show the card
            categoryCard.SetActive(true);

            // Start position (off-screen right)
            cardRect.anchoredPosition = new Vector2(screenWidth + cardWidth, 0);

            // Slide in from right to center
            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                float x = Mathf.Lerp(screenWidth + cardWidth, 0, easeT);
                cardRect.anchoredPosition = new Vector2(x, 0);
                yield return null;
            }
            cardRect.anchoredPosition = Vector2.zero;

            // Play category audio
            AudioClip clip = null;
            
            // In test mode, use TestGameAudioLoader
            if (GameManager.Instance != null && GameManager.Instance.IsTestMode)
            {
                clip = TestGameAudioLoader.Instance?.GetCategoryAudio(currentCategoryIndex);
            }
            // For live games, use UnifiedTTSLoader
            else if (UnifiedTTSLoader.Instance != null)
            {
                UnifiedTTSLoader.Instance.TryGetCategoryAudio(categoryName, out clip);
            }

            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                
                // Wait for audio to finish
                while (audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else if (TTSService.Instance != null)
            {
                // Fallback: generate on the fly
                bool done = false;
                TTSService.Instance.Speak(categoryName, () => done = true);
                while (!done)
                {
                    yield return null;
                }
            }
            else
            {
                // No audio available, just hold
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(holdDuration);
            currentCategoryIndex++;

            // Slide out to left
            elapsed = 0f;
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideOutDuration;
                float easeT = t * t; // Ease in quadratic
                float x = Mathf.Lerp(0, -screenWidth - cardWidth, easeT);
                cardRect.anchoredPosition = new Vector2(x, 0);
                yield return null;
            }

            // Hide card after sliding out
            categoryCard.SetActive(false);
        }

        private IEnumerator PlayOutroPhrase()
        {
            // Audio only - no text displayed for outro phrase
            bool done = false;
            var phrasePlayer = FindFirstObjectByType<PhrasePlayer>();
            
            if (phrasePlayer != null)
            {
                phrasePlayer.PlayPhrase("lets_play", () => done = true);
                
                float timeout = 5f;
                float elapsed = 0f;
                while (!done && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitForSeconds(0.3f);

            // Fade out background
            float fadeDuration = 0.3f;
            float fadeElapsed = 0f;
            Color startColor = backgroundImage.color;

            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float t = fadeElapsed / fadeDuration;
                backgroundImage.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                yield return null;
            }
        }

        public void Skip()
        {
            StopAllCoroutines();
            if (introPanel != null)
            {
                Destroy(introPanel);
            }
            isPlaying = false;
            OnIntroComplete?.Invoke();
        }
    }
}
