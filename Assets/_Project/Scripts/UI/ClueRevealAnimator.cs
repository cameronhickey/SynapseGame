using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cerebrum.UI
{
    public class ClueRevealAnimator : MonoBehaviour
    {
        [Header("Animation Timing")]
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float flyDuration = 0.5f;
        [SerializeField] private int flashCount = 2;

        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Colors")]
        [SerializeField] private Color cardFrontColor = new Color(0.06f, 0.06f, 0.4f, 1f);
        [SerializeField] private Color cardBackColor = new Color(0.15f, 0.15f, 0.5f, 1f);
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("Layout")]
        [SerializeField] private float targetTopMargin = 20f;
        [SerializeField] private float targetSideMargin = 20f;
        [SerializeField] private float targetBottomMargin = 120f; // Space for player scores
        [SerializeField] private float shadowOffset = 15f;

        public event Action OnRevealComplete;

        private Canvas parentCanvas;
        private RectTransform canvasRect;
        private GameObject flyingCard;
        private GameObject shadowCard;
        private RectTransform flyingCardRect;
        private RectTransform shadowCardRect;
        private Image flyingCardImage;
        private Image shadowCardImage;
        private TextMeshProUGUI cardText;
        private bool isAnimating;
        private bool isShowing;

        private void Awake()
        {
            FindCanvas();
        }

        private void FindCanvas()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
            }

            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }
        }

        private void CreateFlyingCard()
        {
            if (parentCanvas == null)
            {
                FindCanvas();
                if (parentCanvas == null)
                {
                    Debug.LogError("[ClueRevealAnimator] No canvas found");
                    return;
                }
            }

            // Create shadow card (behind main card)
            shadowCard = new GameObject("ClueShadow");
            shadowCard.transform.SetParent(parentCanvas.transform, false);
            shadowCardRect = shadowCard.AddComponent<RectTransform>();
            shadowCardImage = shadowCard.AddComponent<Image>();
            shadowCardImage.color = shadowColor;
            shadowCard.SetActive(false);

            // Create flying card
            flyingCard = new GameObject("FlyingClueCard");
            flyingCard.transform.SetParent(parentCanvas.transform, false);
            flyingCardRect = flyingCard.AddComponent<RectTransform>();
            flyingCardImage = flyingCard.AddComponent<Image>();
            flyingCardImage.color = cardBackColor;

            // Add text to card
            GameObject textObj = new GameObject("ClueText");
            textObj.transform.SetParent(flyingCard.transform, false);
            cardText = textObj.AddComponent<TextMeshProUGUI>();
            cardText.alignment = TextAlignmentOptions.Center;
            cardText.fontSize = 72;
            cardText.color = Color.white;
            cardText.textWrappingMode = TextWrappingModes.Normal;
            cardText.enableAutoSizing = true;
            cardText.fontSizeMin = 36;
            cardText.fontSizeMax = 96;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.12f, 0.15f);
            textRect.anchorMax = new Vector2(0.88f, 0.85f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Apply Lora font with drop shadow for clue text
            FontManager.EnsureExists();
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyClueStyle(cardText);
            }

            flyingCard.SetActive(false);
        }

        public void RevealClue(RectTransform sourceButton, string clueText, Action onComplete = null)
        {
            Debug.Log($"[ClueRevealAnimator] RevealClue called, source={sourceButton != null}");

            if (isAnimating)
            {
                Debug.LogWarning("[ClueRevealAnimator] Already animating, skipping");
                return;
            }

            if (sourceButton == null)
            {
                Debug.LogError("[ClueRevealAnimator] Source button is null");
                onComplete?.Invoke();
                return;
            }

            // Create card if needed
            if (flyingCard == null)
            {
                CreateFlyingCard();
            }

            if (flyingCard == null)
            {
                Debug.LogError("[ClueRevealAnimator] Could not create flying card");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(RevealCoroutine(sourceButton, clueText, onComplete));
        }

        private IEnumerator RevealCoroutine(RectTransform sourceButton, string clueText, Action onComplete)
        {
            isAnimating = true;
            isShowing = true;

            // Get source button info
            Image sourceImage = sourceButton.GetComponent<Image>();
            Color originalColor = sourceImage != null ? sourceImage.color : cardBackColor;

            // Phase 1: Flash the source button
            Debug.Log("[ClueRevealAnimator] Phase 1: Flash");
            if (sourceImage != null)
            {
                for (int i = 0; i < flashCount; i++)
                {
                    sourceImage.color = flashColor;
                    yield return new WaitForSeconds(flashDuration / (flashCount * 2));
                    sourceImage.color = originalColor;
                    yield return new WaitForSeconds(flashDuration / (flashCount * 2));
                }
            }

            // Get source position and size in canvas space
            Vector3[] sourceCorners = new Vector3[4];
            sourceButton.GetWorldCorners(sourceCorners);
            
            Vector2 sourceMin = parentCanvas.transform.InverseTransformPoint(sourceCorners[0]);
            Vector2 sourceMax = parentCanvas.transform.InverseTransformPoint(sourceCorners[2]);
            Vector2 sourceCenter = (sourceMin + sourceMax) / 2f;
            Vector2 sourceSize = sourceMax - sourceMin;

            // Calculate target position and size (fill screen except player area)
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 targetSize = new Vector2(
                canvasSize.x - (targetSideMargin * 2),
                canvasSize.y - targetTopMargin - targetBottomMargin
            );
            Vector2 targetCenter = new Vector2(
                0,
                (targetBottomMargin - targetTopMargin) / 2f
            );

            // Setup flying card at source position
            flyingCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            flyingCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            flyingCardRect.pivot = new Vector2(0.5f, 0.5f);
            flyingCardRect.anchoredPosition = sourceCenter;
            flyingCardRect.sizeDelta = sourceSize;
            flyingCardRect.localRotation = Quaternion.identity;
            flyingCardImage.color = cardBackColor;
            cardText.text = ""; // Hide text during back side

            // Setup shadow
            shadowCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowCardRect.pivot = new Vector2(0.5f, 0.5f);
            shadowCardRect.anchoredPosition = sourceCenter + new Vector2(shadowOffset, -shadowOffset);
            shadowCardRect.sizeDelta = sourceSize;

            // Show cards
            shadowCard.SetActive(true);
            flyingCard.SetActive(true);
            flyingCard.transform.SetAsLastSibling();

            // Phase 2: Fly and flip simultaneously
            Debug.Log("[ClueRevealAnimator] Phase 2: Fly and Flip");
            float elapsed = 0f;
            bool hasFlipped = false;

            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float curvedT = flyCurve.Evaluate(t);

                // Interpolate position and size
                Vector2 currentCenter = Vector2.Lerp(sourceCenter, targetCenter, curvedT);
                Vector2 currentSize = Vector2.Lerp(sourceSize, targetSize, curvedT);

                flyingCardRect.anchoredPosition = currentCenter;
                flyingCardRect.sizeDelta = currentSize;

                // Shadow follows with offset (grows with card)
                float shadowScale = Mathf.Lerp(1f, 1.5f, curvedT);
                shadowCardRect.anchoredPosition = currentCenter + new Vector2(shadowOffset * shadowScale, -shadowOffset * shadowScale);
                shadowCardRect.sizeDelta = currentSize;
                shadowCardImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.3f, 0.5f, curvedT));

                // 3D flip rotation (rotate around Y axis)
                float flipProgress = t;
                float rotationY = flipProgress * 180f;
                
                // Apply perspective-like scale for 3D effect
                float perspectiveScale = Mathf.Abs(Mathf.Cos(rotationY * Mathf.Deg2Rad));
                flyingCardRect.localScale = new Vector3(perspectiveScale, 1f, 1f);

                // Switch from back to front at 90 degrees
                if (rotationY >= 90f && !hasFlipped)
                {
                    hasFlipped = true;
                    flyingCardImage.color = cardFrontColor;
                    cardText.text = clueText;
                }

                yield return null;
            }

            // Ensure final state
            flyingCardRect.anchoredPosition = targetCenter;
            flyingCardRect.sizeDelta = targetSize;
            flyingCardRect.localScale = Vector3.one;
            flyingCardImage.color = cardFrontColor;
            cardText.text = clueText;

            shadowCardRect.anchoredPosition = targetCenter + new Vector2(shadowOffset * 1.5f, -shadowOffset * 1.5f);
            shadowCardRect.sizeDelta = targetSize;

            isAnimating = false;
            Debug.Log("[ClueRevealAnimator] Animation complete");
            OnRevealComplete?.Invoke();
            onComplete?.Invoke();
        }

        public void HideClue(Action onComplete = null)
        {
            if (flyingCard != null && flyingCard.activeSelf)
            {
                StartCoroutine(HideCoroutine(onComplete));
            }
            else
            {
                isShowing = false;
                onComplete?.Invoke();
            }
        }

        private IEnumerator HideCoroutine(Action onComplete)
        {
            // Quick fade out
            float duration = 0.2f;
            
            CanvasGroup cardGroup = flyingCard.GetComponent<CanvasGroup>();
            if (cardGroup == null)
            {
                cardGroup = flyingCard.AddComponent<CanvasGroup>();
            }
            
            CanvasGroup shadowGroup = shadowCard.GetComponent<CanvasGroup>();
            if (shadowGroup == null)
            {
                shadowGroup = shadowCard.AddComponent<CanvasGroup>();
            }

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float alpha = 1f - (t / duration);
                cardGroup.alpha = alpha;
                shadowGroup.alpha = alpha;
                yield return null;
            }

            flyingCard.SetActive(false);
            shadowCard.SetActive(false);
            cardGroup.alpha = 1f;
            shadowGroup.alpha = 1f;
            isShowing = false;

            onComplete?.Invoke();
        }

        public bool IsShowing => isShowing;
    }
}
