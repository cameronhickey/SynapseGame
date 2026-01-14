using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;

namespace Cerebrum.Game
{
    /// <summary>
    /// A UI-based clue card that uses RectTransform 3D rotation for flip animations.
    /// Works with URP 2D Renderer since it uses UI components.
    /// </summary>
    public class ClueCardUI3D : MonoBehaviour
    {
        public Clue Clue { get; private set; }
        public Category Category { get; private set; }
        public int Value { get; private set; }
        public bool IsUsed { get; private set; }
        
        public event Action<ClueCardUI3D> OnCardClicked;
        
        [Header("Animation")]
        [SerializeField] private float flipDuration = 0.6f;
        [SerializeField] private float growScale = 2.5f;
        
        private RectTransform rectTransform;
        private Image frontImage;
        private Image backImage;
        private TextMeshProUGUI valueText;
        private TextMeshProUGUI clueText;
        private TextMeshProUGUI answerText;
        private Button cardButton;
        private CanvasGroup canvasGroup;
        
        private Vector2 originalPosition;
        private Vector3 originalScale;
        private bool isAnimating;

        public void Initialize(Clue clue, Category category, int value, float width, float height,
            Color frontColor, Color backColor)
        {
            Clue = clue;
            Category = category;
            Value = value;
            
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();
            
            rectTransform.sizeDelta = new Vector2(width, height);
            originalPosition = rectTransform.anchoredPosition;
            originalScale = Vector3.one;
            
            CreateCardUI(frontColor, backColor, width, height);
        }

        private void CreateCardUI(Color frontColor, Color backColor, float width, float height)
        {
            // Add canvas group for fade effects
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Front side
            GameObject frontObj = new GameObject("Front");
            frontObj.transform.SetParent(transform, false);
            RectTransform frontRect = frontObj.AddComponent<RectTransform>();
            frontRect.anchorMin = Vector2.zero;
            frontRect.anchorMax = Vector2.one;
            frontRect.offsetMin = Vector2.zero;
            frontRect.offsetMax = Vector2.zero;
            
            frontImage = frontObj.AddComponent<Image>();
            frontImage.color = frontColor;
            
            // Add rounded corners effect via child image with gradient
            GameObject gradientObj = new GameObject("Gradient");
            gradientObj.transform.SetParent(frontObj.transform, false);
            RectTransform gradRect = gradientObj.AddComponent<RectTransform>();
            gradRect.anchorMin = Vector2.zero;
            gradRect.anchorMax = Vector2.one;
            gradRect.offsetMin = new Vector2(2, 2);
            gradRect.offsetMax = new Vector2(-2, -2);
            Image gradImage = gradientObj.AddComponent<Image>();
            gradImage.color = new Color(frontColor.r + 0.1f, frontColor.g + 0.1f, frontColor.b + 0.15f);
            
            // Value text
            GameObject valueObj = new GameObject("ValueText");
            valueObj.transform.SetParent(frontObj.transform, false);
            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            
            valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = "$" + Value;
            valueText.fontSize = 48;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = new Color(1f, 0.85f, 0.4f); // Gold
            valueText.fontStyle = FontStyles.Bold;
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 24;
            valueText.fontSizeMax = 72;
            
            // Back side (hidden initially)
            GameObject backObj = new GameObject("Back");
            backObj.transform.SetParent(transform, false);
            RectTransform backRect = backObj.AddComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            backRect.localRotation = Quaternion.Euler(0, 180, 0); // Pre-flip so it shows correctly
            
            backImage = backObj.AddComponent<Image>();
            backImage.color = backColor;
            backObj.SetActive(false);
            
            // Clue text on back
            GameObject clueObj = new GameObject("ClueText");
            clueObj.transform.SetParent(backObj.transform, false);
            RectTransform clueRect = clueObj.AddComponent<RectTransform>();
            clueRect.anchorMin = new Vector2(0.05f, 0.3f);
            clueRect.anchorMax = new Vector2(0.95f, 0.95f);
            clueRect.offsetMin = Vector2.zero;
            clueRect.offsetMax = Vector2.zero;
            
            clueText = clueObj.AddComponent<TextMeshProUGUI>();
            clueText.text = Clue.Question;
            clueText.fontSize = 24;
            clueText.alignment = TextAlignmentOptions.Center;
            clueText.color = Color.white;
            clueText.enableAutoSizing = true;
            clueText.fontSizeMin = 14;
            clueText.fontSizeMax = 36;
            
            // Answer text on back (hidden initially)
            GameObject answerObj = new GameObject("AnswerText");
            answerObj.transform.SetParent(backObj.transform, false);
            RectTransform answerRect = answerObj.AddComponent<RectTransform>();
            answerRect.anchorMin = new Vector2(0.05f, 0.05f);
            answerRect.anchorMax = new Vector2(0.95f, 0.3f);
            answerRect.offsetMin = Vector2.zero;
            answerRect.offsetMax = Vector2.zero;
            
            answerText = answerObj.AddComponent<TextMeshProUGUI>();
            answerText.text = "";
            answerText.fontSize = 28;
            answerText.alignment = TextAlignmentOptions.Center;
            answerText.color = new Color(1f, 0.85f, 0.4f); // Gold
            answerText.fontStyle = FontStyles.Bold;
            
            // Button for click handling
            cardButton = gameObject.AddComponent<Button>();
            cardButton.targetGraphic = frontImage;
            cardButton.onClick.AddListener(OnClick);
            
            // Hover color change
            ColorBlock colors = cardButton.colors;
            colors.highlightedColor = new Color(frontColor.r + 0.15f, frontColor.g + 0.15f, frontColor.b + 0.2f);
            colors.pressedColor = new Color(frontColor.r - 0.1f, frontColor.g - 0.1f, frontColor.b - 0.1f);
            cardButton.colors = colors;
        }

        private void OnClick()
        {
            if (!isAnimating && !IsUsed)
            {
                OnCardClicked?.Invoke(this);
            }
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
            if (cardButton != null)
            {
                cardButton.interactable = false;
            }
        }

        public void SpinIntoView(float delay, float duration = 0.8f)
        {
            StartCoroutine(SpinIntoViewCoroutine(delay, duration));
        }

        private IEnumerator SpinIntoViewCoroutine(float delay, float duration)
        {
            // Start hidden
            Vector2 targetPos = rectTransform.anchoredPosition;
            Vector2 startPos = targetPos + new Vector2(0, 300);
            rectTransform.anchoredPosition = startPos;
            rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            rectTransform.localScale = Vector3.zero;
            canvasGroup.alpha = 0;
            
            yield return new WaitForSeconds(delay);
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
                float spinAngle = (1f - eased) * 360f;
                rectTransform.localRotation = Quaternion.Euler(0, 0, spinAngle);
                rectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, eased);
                canvasGroup.alpha = eased;
                
                yield return null;
            }
            
            rectTransform.anchoredPosition = targetPos;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = originalScale;
            canvasGroup.alpha = 1;
        }

        public void FlipAndReveal(Action onComplete = null)
        {
            if (isAnimating) return;
            StartCoroutine(FlipAndRevealCoroutine(onComplete));
        }

        private IEnumerator FlipAndRevealCoroutine(Action onComplete)
        {
            isAnimating = true;
            
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 centerPos = Vector2.zero; // Move to center
            
            // Phase 1: Move to center and grow
            float elapsed = 0f;
            float moveDuration = 0.3f;
            
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, centerPos, t);
                float scale = Mathf.Lerp(1f, growScale, t);
                rectTransform.localScale = originalScale * scale;
                
                yield return null;
            }
            
            // Phase 2: Flip with Y rotation
            elapsed = 0f;
            bool hasSwapped = false;
            
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                
                float angle = t * 180f;
                rectTransform.localRotation = Quaternion.Euler(0, angle, 0);
                
                // Swap at 90 degrees
                if (angle >= 90f && !hasSwapped)
                {
                    hasSwapped = true;
                    frontImage.transform.parent.gameObject.SetActive(false);
                    backImage.transform.parent.gameObject.SetActive(true);
                }
                
                yield return null;
            }
            
            rectTransform.localRotation = Quaternion.Euler(0, 180, 0);
            isAnimating = false;
            onComplete?.Invoke();
        }

        public void ShowAnswer(string answer)
        {
            if (answerText != null)
            {
                answerText.text = answer;
            }
        }

        public void DismissCard(Action onComplete = null)
        {
            if (isAnimating) return;
            StartCoroutine(DismissCardCoroutine(onComplete));
        }

        private IEnumerator DismissCardCoroutine(Action onComplete)
        {
            isAnimating = true;
            
            // Flip back
            float elapsed = 0f;
            bool hasSwapped = false;
            
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                
                float angle = 180f + t * 180f;
                rectTransform.localRotation = Quaternion.Euler(0, angle, 0);
                
                if (angle >= 270f && !hasSwapped)
                {
                    hasSwapped = true;
                    backImage.transform.parent.gameObject.SetActive(false);
                    frontImage.transform.parent.gameObject.SetActive(true);
                    valueText.text = ""; // Clear value
                    frontImage.color = new Color(0.1f, 0.1f, 0.15f, 0.5f); // Dim
                }
                
                yield return null;
            }
            
            // Move back and shrink
            elapsed = 0f;
            float returnDuration = 0.3f;
            Vector2 currentPos = rectTransform.anchoredPosition;
            Vector3 currentScale = rectTransform.localScale;
            
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnDuration;
                
                rectTransform.anchoredPosition = Vector2.Lerp(currentPos, originalPosition, t);
                rectTransform.localScale = Vector3.Lerp(currentScale, originalScale, t);
                
                yield return null;
            }
            
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localScale = originalScale;
            rectTransform.localRotation = Quaternion.identity;
            isAnimating = false;
            
            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(OnClick);
            }
        }
    }
}
