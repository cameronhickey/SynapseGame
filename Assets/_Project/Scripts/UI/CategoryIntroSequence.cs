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
        #pragma warning disable CS0414
        [SerializeField] private float cardWidth = 900f; // Reserved for future use
        [SerializeField] private float cardHeight = 300f; // Reserved for future use
        #pragma warning restore CS0414

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        public event Action OnIntroComplete;

        private Canvas canvas;
        private GameObject introPanel;
        private GameObject categoryCard;
        private Image cardImage;
        private TextMeshProUGUI categoryText;
        #pragma warning disable CS0414
        private TextMeshProUGUI shadowText; // Kept for compatibility, no longer used
        #pragma warning restore CS0414
        private Image backgroundImage;
        private RectTransform panelRect;
        private RectTransform cardRect;
        private GameObject skipButton;
        private bool skipRequested;

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

            // Show each category (skip if requested)
            foreach (var category in board.Categories)
            {
                if (skipRequested) break;
                yield return ShowCategory(category.Title);
                if (!skipRequested)
                {
                    yield return new WaitForSeconds(delayBetweenCategories);
                }
            }

            // Play "Let's start!" outro (skip if requested)
            if (!skipRequested)
            {
                yield return PlayOutroPhrase();
            }

            // Clean up
            if (skipButton != null)
            {
                Destroy(skipButton);
                skipButton = null;
            }
            if (introPanel != null)
            {
                Destroy(introPanel);
            }

            isPlaying = false;
            skipRequested = false;
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

            // Transparent background - needed for fade-out logic but doesn't obscure game background
            backgroundImage = introPanel.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0f);

            // Create category card container - reduced size (75% width, 55% height)
            categoryCard = new GameObject("CategoryCard");
            categoryCard.transform.SetParent(introPanel.transform, false);

            cardRect = categoryCard.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            // Reduced card size by ~15% from previous (was 90%/70%, now 75%/55%)
            float screenWidth = canvas.GetComponent<RectTransform>().rect.width;
            float screenHeight = canvas.GetComponent<RectTransform>().rect.height;
            cardRect.sizeDelta = new Vector2(screenWidth * 0.75f, screenHeight * 0.55f);
            cardRect.anchoredPosition = new Vector2(screenWidth + cardRect.sizeDelta.x, 0); // Start off-screen right

            // Layer 1: Outer glow (soft cyan/orange gradient simulation)
            GameObject outerGlow = new GameObject("OuterGlow");
            outerGlow.transform.SetParent(categoryCard.transform, false);
            RectTransform outerGlowRect = outerGlow.AddComponent<RectTransform>();
            outerGlowRect.anchorMin = Vector2.zero;
            outerGlowRect.anchorMax = Vector2.one;
            outerGlowRect.offsetMin = new Vector2(-12, -12);
            outerGlowRect.offsetMax = new Vector2(12, 12);
            Image outerGlowImage = outerGlow.AddComponent<Image>();
            outerGlowImage.color = new Color(0.2f, 0.5f, 0.8f, 0.4f);
            Outline glow1 = outerGlow.AddComponent<Outline>();
            glow1.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.3f);
            glow1.effectDistance = new Vector2(8, 8);
            Outline glow2 = outerGlow.AddComponent<Outline>();
            glow2.effectColor = new Color(0.8f, 0.5f, 0.3f, 0.2f);
            glow2.effectDistance = new Vector2(6, -6);

            // Layer 2: Border frame (visible edge)
            GameObject borderFrame = new GameObject("BorderFrame");
            borderFrame.transform.SetParent(categoryCard.transform, false);
            RectTransform borderRect = borderFrame.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            cardImage = borderFrame.AddComponent<Image>();
            cardImage.color = new Color(0.4f, 0.55f, 0.75f, 0.9f);
            Outline borderInner = borderFrame.AddComponent<Outline>();
            borderInner.effectColor = new Color(0.6f, 0.75f, 0.95f, 0.8f);
            borderInner.effectDistance = new Vector2(2, 2);

            // Layer 3: Inner panel (dark background)
            GameObject innerPanel = new GameObject("InnerPanel");
            innerPanel.transform.SetParent(categoryCard.transform, false);
            RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(6, 6);
            innerRect.offsetMax = new Vector2(-6, -6);
            Image innerImage = innerPanel.AddComponent<Image>();
            innerImage.color = new Color(0.06f, 0.08f, 0.18f, 0.95f);

            // Layer 4: Main text (white, large, centered)
            GameObject textObj = new GameObject("CategoryText");
            textObj.transform.SetParent(categoryCard.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(60, 60);
            textRect.offsetMax = new Vector2(-60, -60);

            categoryText = textObj.AddComponent<TextMeshProUGUI>();
            categoryText.fontSize = 200;
            categoryText.fontStyle = FontStyles.Bold;
            categoryText.color = Color.white;
            categoryText.alignment = TextAlignmentOptions.Center;
            categoryText.textWrappingMode = TextWrappingModes.Normal;
            categoryText.enableAutoSizing = true;
            categoryText.fontSizeMin = 100;
            categoryText.fontSizeMax = 280;

            // Apply category header font with drop shadow
            FontManager.EnsureExists();
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyCategoryStyle(categoryText);
            }

            // Hide card initially
            categoryCard.SetActive(false);
            
            // Create skip button
            CreateSkipButton();
        }

        private IEnumerator PlayIntroPhrase()
        {
            if (skipRequested) yield break;
            
            // Audio only - no text displayed for intro phrase
            bool done = false;
            var phrasePlayer = FindFirstObjectByType<PhrasePlayer>();
            
            if (phrasePlayer != null)
            {
                phrasePlayer.PlayPhrase("categories_intro", () => done = true);
                
                float timeout = 5f;
                float elapsed = 0f;
                while (!skipRequested && !done && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else if (!skipRequested)
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (!skipRequested)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        private IEnumerator ShowCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) yield break;

            // Use canvas-based screen width for animation
            float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
            float actualCardWidth = cardRect.sizeDelta.x;

            // Set text
            categoryText.text = categoryName.ToUpper();

            // Show the card
            categoryCard.SetActive(true);

            // Start position (off-screen right)
            cardRect.anchoredPosition = new Vector2(canvasWidth + actualCardWidth, 0);

            // Slide in from right to center
            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                float x = Mathf.Lerp(canvasWidth + actualCardWidth, 0, easeT);
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
                Debug.LogWarning($"[CategoryIntroSequence] Category audio not cached: {categoryName}");
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
                float x = Mathf.Lerp(0, -canvasWidth - actualCardWidth, easeT);
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
            if (skipButton != null)
            {
                Destroy(skipButton);
                skipButton = null;
            }
            if (introPanel != null)
            {
                Destroy(introPanel);
            }
            isPlaying = false;
            skipRequested = false;
            OnIntroComplete?.Invoke();
        }
        
        private void OnSkipClicked()
        {
            Debug.Log("[CategoryIntroSequence] Skip requested");
            skipRequested = true;
            audioSource?.Stop();
            TTSService.Instance?.Stop();
        }
        
        private void CreateSkipButton()
        {
            if (canvas == null) return;
            
            // Skip button in bottom-right corner
            skipButton = new GameObject("SkipButton");
            skipButton.transform.SetParent(canvas.transform, false);
            
            // Ensure button is on top of everything
            skipButton.transform.SetAsLastSibling();
            
            RectTransform btnRect = skipButton.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 0f);
            btnRect.anchorMax = new Vector2(1f, 0f);
            btnRect.pivot = new Vector2(1f, 0f);
            btnRect.anchoredPosition = new Vector2(-30, 30);
            btnRect.sizeDelta = new Vector2(140, 50);
            
            // Add canvas to ensure it renders on top
            Canvas buttonCanvas = skipButton.AddComponent<Canvas>();
            buttonCanvas.overrideSorting = true;
            buttonCanvas.sortingOrder = 100;
            skipButton.AddComponent<GraphicRaycaster>();
            
            // Glow/border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(skipButton.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3, -3);
            borderRect.offsetMax = new Vector2(3, 3);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0.5f, 0.7f, 0.9f, 0.9f);
            
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(skipButton.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.raycastTarget = true;
            Color fillColor = new Color(0.15f, 0.25f, 0.4f, 0.85f);
            bgImg.color = fillColor;
            
            // Button component with stronger color transitions
            Button btn = skipButton.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.onClick.AddListener(OnSkipClicked);
            btn.transition = Selectable.Transition.ColorTint;
            
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.5f, 1f); // Brighter on hover
            colors.pressedColor = new Color(0.6f, 0.6f, 0.7f, 1f); // Darker on press
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            // Text
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(skipButton.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Skip >>";
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            
            // Add hover effect component
            SkipButtonHover hoverEffect = skipButton.AddComponent<SkipButtonHover>();
            hoverEffect.Initialize(btnRect, borderImg);
        }
    }
}
