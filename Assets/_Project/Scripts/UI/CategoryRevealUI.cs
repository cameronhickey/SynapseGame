using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;
using Cerebrum.OpenAI;

namespace Cerebrum.UI
{
    public class CategoryRevealUI : MonoBehaviour
    {
        public static CategoryRevealUI Instance { get; private set; }

        [Header("Animation Settings")]
        [SerializeField] private float slideInDuration = 0.4f;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float slideOutDuration = 0.3f;
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual Settings")]
        [SerializeField] private Color cardColor = new Color(0.06f, 0.06f, 0.4f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private float cardPadding = 60f;

        public event Action OnRevealComplete;

        private Canvas parentCanvas;
        private GameObject categoryCard;
        private RectTransform cardRect;
        private TextMeshProUGUI categoryText;
        private Image cardBackground;
        #pragma warning disable CS0414
        private bool isRevealing; // State tracking for animation
        #pragma warning restore CS0414

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
            }

            if (parentCanvas == null)
            {
                Debug.LogError("[CategoryRevealUI] No canvas found!");
                return;
            }

            // Create category card
            categoryCard = new GameObject("CategoryRevealCard");
            categoryCard.transform.SetParent(parentCanvas.transform, false);

            cardRect = categoryCard.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.1f, 0.2f);
            cardRect.anchorMax = new Vector2(0.9f, 0.8f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            cardBackground = categoryCard.AddComponent<Image>();
            cardBackground.color = cardColor;

            // Create text
            GameObject textObj = new GameObject("CategoryText");
            textObj.transform.SetParent(categoryCard.transform, false);

            categoryText = textObj.AddComponent<TextMeshProUGUI>();
            categoryText.alignment = TextAlignmentOptions.Center;
            categoryText.fontSize = 160;
            categoryText.fontStyle = FontStyles.Bold;
            categoryText.color = textColor;
            categoryText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            categoryText.enableAutoSizing = true;
            categoryText.fontSizeMin = 72;
            categoryText.fontSizeMax = 200;

            RectTransform textRect = categoryText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(cardPadding, cardPadding);
            textRect.offsetMax = new Vector2(-cardPadding, -cardPadding);

            // Apply Bebas Neue font with drop shadow
            FontManager.EnsureExists();
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyCategoryStyle(categoryText);
            }

            categoryCard.SetActive(false);
        }

        public void RevealCategories(Board board, Action onComplete = null)
        {
            if (board == null || board.Categories == null || board.Categories.Count == 0)
            {
                Debug.LogWarning("[CategoryRevealUI] No categories to reveal");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(RevealCategoriesCoroutine(board.Categories, onComplete));
        }

        private IEnumerator RevealCategoriesCoroutine(List<Category> categories, Action onComplete)
        {
            isRevealing = true;
            Debug.Log("[CategoryRevealUI] Starting category reveal");

            // First, say "Today's categories are..." (with timeout)
            bool introComplete = false;
            float introTimeout = 5f;
            float introElapsed = 0f;
            
            if (PhrasePlayer.Instance != null)
            {
                Debug.Log("[CategoryRevealUI] Playing intro phrase via PhrasePlayer");
                PhrasePlayer.Instance.PlayPhrase("todays_categories", () => introComplete = true);
            }
            else if (TTSService.Instance != null)
            {
                Debug.Log("[CategoryRevealUI] Playing intro phrase via TTSService");
                TTSService.Instance.Speak("Today's categories are...", () => introComplete = true);
            }
            else
            {
                Debug.Log("[CategoryRevealUI] No TTS available, skipping intro");
                introComplete = true;
            }

            // Wait with timeout
            while (!introComplete && introElapsed < introTimeout)
            {
                introElapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!introComplete)
            {
                Debug.LogWarning("[CategoryRevealUI] Intro phrase timed out, continuing...");
            }

            yield return new WaitForSeconds(0.3f);

            // Reveal each category
            for (int i = 0; i < categories.Count; i++)
            {
                Category category = categories[i];
                Debug.Log($"[CategoryRevealUI] Revealing category {i + 1}/{categories.Count}: {category.Title}");
                yield return StartCoroutine(RevealSingleCategory(category.Title, i == 0));
            }

            Debug.Log("[CategoryRevealUI] All categories revealed");
            isRevealing = false;
            onComplete?.Invoke();
            OnRevealComplete?.Invoke();
        }

        private IEnumerator RevealSingleCategory(string categoryName, bool isFirst)
        {
            categoryText.text = categoryName.ToUpper();
            categoryCard.SetActive(true);

            // Get screen width for animation
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;

            // Start position (off-screen right)
            Vector2 startPos = new Vector2(screenWidth, 0);
            Vector2 centerPos = Vector2.zero;
            Vector2 endPos = new Vector2(-screenWidth, 0);

            // Slide in from right
            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = slideCurve.Evaluate(elapsed / slideInDuration);
                cardRect.anchoredPosition = Vector2.Lerp(startPos, centerPos, t);
                yield return null;
            }
            cardRect.anchoredPosition = centerPos;

            // Speak the category name (with timeout)
            bool speechComplete = false;
            float speechTimeout = 5f;
            
            if (TTSService.Instance != null)
            {
                TTSService.Instance.Speak(categoryName, () => speechComplete = true);
            }
            else
            {
                speechComplete = true;
            }

            // Wait for speech (with timeout) and minimum display time
            float displayTimer = 0f;
            while (displayTimer < displayDuration || (!speechComplete && displayTimer < speechTimeout))
            {
                displayTimer += Time.deltaTime;
                yield return null;
            }

            // Slide out to left
            elapsed = 0f;
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = slideCurve.Evaluate(elapsed / slideOutDuration);
                cardRect.anchoredPosition = Vector2.Lerp(centerPos, endPos, t);
                yield return null;
            }

            categoryCard.SetActive(false);
            cardRect.anchoredPosition = Vector2.zero;
        }

        public void Hide()
        {
            if (categoryCard != null)
            {
                categoryCard.SetActive(false);
            }
            StopAllCoroutines();
            isRevealing = false;
        }
    }
}
