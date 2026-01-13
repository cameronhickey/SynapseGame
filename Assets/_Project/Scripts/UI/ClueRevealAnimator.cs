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
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private float liftDuration = 0.15f;
        [SerializeField] private float flipDuration = 0.4f;
        [SerializeField] private float flyDuration = 0.35f;
        [SerializeField] private int flashCount = 2;

        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Colors")]
        [SerializeField] private Color cardTopColor = new Color(0.18f, 0.18f, 0.55f, 1f);
        [SerializeField] private Color cardBottomColor = new Color(0.06f, 0.06f, 0.35f, 1f);
        [SerializeField] private Color highlightColor = new Color(0.5f, 0.5f, 1f, 1f);
        [SerializeField] private Color edgeGlowColor = new Color(0.6f, 0.8f, 1f, 1f);
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.6f);

        [Header("Layout")]
        [SerializeField] private float targetTopMargin = 20f;
        [SerializeField] private float targetSideMargin = 20f;
        [SerializeField] private float targetBottomMargin = 120f;
        [SerializeField] private float shadowOffset = 12f;
        [SerializeField] private float liftScale = 1.15f;

        public event Action OnRevealComplete;

        private Canvas parentCanvas;
        private RectTransform canvasRect;
        private GameObject cardContainer;
        private RectTransform containerRect;
        private GameObject frontCard;
        private GameObject backCard;
        private RectTransform frontCardRect;
        private RectTransform backCardRect;
        private Image frontCardImage;
        private Image backCardImage;
        private TextMeshProUGUI frontText;
        private TextMeshProUGUI backText;
        private GameObject shadowCard;
        private RectTransform shadowCardRect;
        private Image shadowCardImage;
        private Material cardMaterial;
        private Material backMaterial;
        private bool isAnimating;
        private bool isShowing;
        
        // Store source position for reverse animation
        private Vector2 lastSourceCenter;
        private Vector2 lastSourceSize;
        private Vector2 lastTargetCenter;
        private Vector2 lastTargetSize;
        private float lastScaleRatio;

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
            var shadowGroup = shadowCard.AddComponent<CanvasGroup>();
            shadowGroup.alpha = 0.6f;
            shadowCard.SetActive(false);

            // Create card container (this rotates in 3D)
            cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(parentCanvas.transform, false);
            containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);

            // Load metallic shader (may not be available until imported)
            Shader metallicShader = Shader.Find("UI/MetallicCard");
            Debug.Log($"[ClueRevealAnimator] MetallicCard shader found: {metallicShader != null}");

            // Create BACK of card (shows dollar amount, visible first)
            backCard = new GameObject("CardBack");
            backCard.transform.SetParent(cardContainer.transform, false);
            backCardRect = backCard.AddComponent<RectTransform>();
            backCardRect.anchorMin = Vector2.zero;
            backCardRect.anchorMax = Vector2.one;
            backCardRect.offsetMin = Vector2.zero;
            backCardRect.offsetMax = Vector2.zero;
            backCardImage = backCard.AddComponent<Image>();
            
            if (metallicShader != null)
            {
                backMaterial = new Material(metallicShader);
                backMaterial.SetColor("_TopColor", cardTopColor);
                backMaterial.SetColor("_BottomColor", cardBottomColor);
                backMaterial.SetColor("_HighlightColor", highlightColor);
                backMaterial.SetFloat("_HighlightPos", 0.5f);
                backMaterial.SetFloat("_EdgeGlow", 0f);
                backCardImage.material = backMaterial;
            }
            else
            {
                // Fallback: solid blue color
                backCardImage.color = new Color(0.1f, 0.1f, 0.5f, 1f);
            }

            // Back text (dollar amount)
            GameObject backTextObj = new GameObject("BackText");
            backTextObj.transform.SetParent(backCard.transform, false);
            backText = backTextObj.AddComponent<TextMeshProUGUI>();
            backText.alignment = TextAlignmentOptions.Center;
            backText.fontSize = 72;
            backText.color = new Color(1f, 0.85f, 0.4f); // Gold color
            backText.fontStyle = FontStyles.Bold;
            RectTransform backTextRect = backTextObj.GetComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;

            // Create FRONT of card (shows clue text, hidden initially)
            frontCard = new GameObject("CardFront");
            frontCard.transform.SetParent(cardContainer.transform, false);
            frontCardRect = frontCard.AddComponent<RectTransform>();
            frontCardRect.anchorMin = Vector2.zero;
            frontCardRect.anchorMax = Vector2.one;
            frontCardRect.offsetMin = Vector2.zero;
            frontCardRect.offsetMax = Vector2.zero;
            frontCardImage = frontCard.AddComponent<Image>();
            
            if (metallicShader != null)
            {
                cardMaterial = new Material(metallicShader);
                cardMaterial.SetColor("_TopColor", cardTopColor);
                cardMaterial.SetColor("_BottomColor", cardBottomColor);
                cardMaterial.SetColor("_HighlightColor", highlightColor);
                cardMaterial.SetColor("_EdgeGlowColor", edgeGlowColor);
                cardMaterial.SetFloat("_HighlightPos", 0.5f);
                cardMaterial.SetFloat("_EdgeGlow", 0f);
                frontCardImage.material = cardMaterial;
            }
            else
            {
                // Fallback: solid blue color
                frontCardImage.color = new Color(0.1f, 0.1f, 0.5f, 1f);
            }

            // Front text (clue question)
            GameObject frontTextObj = new GameObject("FrontText");
            frontTextObj.transform.SetParent(frontCard.transform, false);
            frontText = frontTextObj.AddComponent<TextMeshProUGUI>();
            frontText.alignment = TextAlignmentOptions.Center;
            frontText.fontSize = 72;
            frontText.color = Color.white;
            frontText.textWrappingMode = TextWrappingModes.Normal;
            frontText.enableAutoSizing = true;
            frontText.fontSizeMin = 36;
            frontText.fontSizeMax = 96;

            RectTransform frontTextRect = frontTextObj.GetComponent<RectTransform>();
            frontTextRect.anchorMin = new Vector2(0.08f, 0.12f);
            frontTextRect.anchorMax = new Vector2(0.92f, 0.88f);
            frontTextRect.offsetMin = Vector2.zero;
            frontTextRect.offsetMax = Vector2.zero;

            FontManager.EnsureExists();
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyClueStyle(frontText);
            }

            cardContainer.SetActive(false);
            Debug.Log("[ClueRevealAnimator] Card created successfully");
        }

        public void RevealClue(RectTransform sourceButton, string clueText, Action onComplete = null)
        {
            RevealClue(sourceButton, clueText, 0, onComplete);
        }

        public void RevealClue(RectTransform sourceButton, string clueText, int dollarValue, Action onComplete = null)
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
            if (cardContainer == null)
            {
                CreateFlyingCard();
            }

            if (cardContainer == null)
            {
                Debug.LogError("[ClueRevealAnimator] Could not create card");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(RevealCoroutine(sourceButton, clueText, dollarValue, onComplete));
        }

        private IEnumerator RevealCoroutine(RectTransform sourceButton, string clueText, int dollarValue, Action onComplete)
        {
            isAnimating = true;
            isShowing = true;

            // Get source button info
            Image sourceImage = sourceButton.GetComponent<Image>();
            Color originalColor = sourceImage != null ? sourceImage.color : cardBottomColor;

            // Phase 1: Flash the source button
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

            // Calculate target position and size (this is the FINAL size)
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 targetSize = new Vector2(
                canvasSize.x - (targetSideMargin * 2),
                canvasSize.y - targetTopMargin - targetBottomMargin
            );
            Vector2 targetCenter = new Vector2(
                0,
                (targetBottomMargin - targetTopMargin) / 2f
            );

            // Calculate the scale factor to shrink from target to source size
            float scaleRatio = sourceSize.x / targetSize.x;
            float liftedScaleRatio = scaleRatio * liftScale;

            // Store for reverse animation
            lastSourceCenter = sourceCenter;
            lastSourceSize = sourceSize;
            lastTargetCenter = targetCenter;
            lastTargetSize = targetSize;
            lastScaleRatio = scaleRatio;

            // Set card to FINAL size immediately (text renders at this size)
            containerRect.sizeDelta = targetSize;
            containerRect.anchoredPosition = sourceCenter;
            containerRect.localEulerAngles = Vector3.zero;
            // Start scaled down to match source button size
            containerRect.localScale = new Vector3(scaleRatio, scaleRatio, 1f);

            // Set up card content (text will wrap at final size)
            backText.text = dollarValue > 0 ? $"${dollarValue}" : "";
            frontText.text = clueText;

            // Start with back card visible, front hidden
            backCard.SetActive(true);
            frontCard.SetActive(false);
            
            Debug.Log($"[ClueRevealAnimator] Starting animation: scaleRatio={scaleRatio}, targetSize={targetSize}");

            // Setup shadow at source size
            shadowCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowCardRect.pivot = new Vector2(0.5f, 0.5f);
            shadowCardRect.anchoredPosition = sourceCenter + new Vector2(shadowOffset * 0.5f, -shadowOffset * 0.5f);
            shadowCardRect.sizeDelta = sourceSize;

            // Show cards
            shadowCard.SetActive(true);
            cardContainer.SetActive(true);
            cardContainer.transform.SetAsLastSibling();

            // Phase 2: Lift off the board (scale up slightly)
            float elapsed = 0f;
            
            while (elapsed < liftDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / liftDuration);
                float curvedT = liftCurve.Evaluate(t);

                float currentScale = Mathf.Lerp(scaleRatio, liftedScaleRatio, curvedT);
                containerRect.localScale = new Vector3(currentScale, currentScale, 1f);

                // Shadow grows and moves as card lifts
                float shadowGrow = Mathf.Lerp(0.5f, 1.2f, curvedT);
                shadowCardRect.anchoredPosition = sourceCenter + new Vector2(shadowOffset * shadowGrow, -shadowOffset * shadowGrow);
                shadowCardRect.sizeDelta = Vector2.Lerp(sourceSize, sourceSize * liftScale, curvedT);
                
                var shadowGroup = shadowCard.GetComponent<CanvasGroup>();
                if (shadowGroup != null)
                    shadowGroup.alpha = Mathf.Lerp(0.3f, 0.6f, curvedT);

                yield return null;
            }

            // Phase 3: Flip animation (scale X for flip effect)
            elapsed = 0f;
            bool hasFlipped = false;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipDuration);
                float curvedT = flipCurve.Evaluate(t);

                // Scale X to simulate rotation (1 -> 0 -> 1), keep Y at lifted scale
                float rotationProgress = curvedT * 180f;
                float flipScaleX = Mathf.Abs(Mathf.Cos(rotationProgress * Mathf.Deg2Rad));
                
                // Add slight vertical bulge during flip for depth effect
                float flipScaleY = 1f + (1f - flipScaleX) * 0.08f;
                containerRect.localScale = new Vector3(
                    Mathf.Max(0.01f, flipScaleX) * liftedScaleRatio, 
                    flipScaleY * liftedScaleRatio, 
                    1f
                );

                // Swap cards at midpoint (90 degrees)
                if (rotationProgress >= 90f && !hasFlipped)
                {
                    hasFlipped = true;
                    backCard.SetActive(false);
                    frontCard.SetActive(true);
                }

                // Animate shimmer during flip
                if (cardMaterial != null)
                {
                    float highlightPos = Mathf.Lerp(-0.3f, 1.3f, t);
                    cardMaterial.SetFloat("_HighlightPos", highlightPos);
                    
                    float glowPulse = 1f - Mathf.Abs(t - 0.5f) * 2f;
                    cardMaterial.SetFloat("_EdgeGlow", 0.3f + glowPulse * 0.7f);
                }
                if (backMaterial != null)
                {
                    float highlightPos = Mathf.Lerp(-0.3f, 1.3f, t);
                    backMaterial.SetFloat("_HighlightPos", highlightPos);
                }

                yield return null;
            }

            // Ensure flip complete
            containerRect.localScale = new Vector3(liftedScaleRatio, liftedScaleRatio, 1f);
            backCard.SetActive(false);
            frontCard.SetActive(true);

            // Phase 4: Fly to center and scale up to full size
            elapsed = 0f;

            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float curvedT = flyCurve.Evaluate(t);

                // Interpolate position and scale (not size!)
                Vector2 currentCenter = Vector2.Lerp(sourceCenter, targetCenter, curvedT);
                float currentScale = Mathf.Lerp(liftedScaleRatio, 1f, curvedT);

                containerRect.anchoredPosition = currentCenter;
                containerRect.localScale = new Vector3(currentScale, currentScale, 1f);

                // Shadow follows
                float shadowScale = Mathf.Lerp(1.2f, 2f, curvedT);
                shadowCardRect.anchoredPosition = currentCenter + new Vector2(shadowOffset * shadowScale, -shadowOffset * shadowScale);
                shadowCardRect.sizeDelta = Vector2.Lerp(sourceSize * liftScale, targetSize, curvedT);

                // Continue shimmer during fly
                if (cardMaterial != null)
                {
                    float highlightPos = 1.3f + t * 0.5f;
                    cardMaterial.SetFloat("_HighlightPos", highlightPos);
                    cardMaterial.SetFloat("_EdgeGlow", Mathf.Lerp(0.5f, 0f, curvedT));
                }

                yield return null;
            }

            // Ensure final state - full size, scale 1
            containerRect.anchoredPosition = targetCenter;
            containerRect.localScale = Vector3.one;

            shadowCardRect.anchoredPosition = targetCenter + new Vector2(shadowOffset * 2f, -shadowOffset * 2f);
            shadowCardRect.sizeDelta = targetSize;

            if (cardMaterial != null)
            {
                cardMaterial.SetFloat("_EdgeGlow", 0f);
                cardMaterial.SetFloat("_HighlightPos", 2f);
            }

            isAnimating = false;
            OnRevealComplete?.Invoke();
            onComplete?.Invoke();
        }

        public void HideClue(Action onComplete = null)
        {
            if (cardContainer != null && cardContainer.activeSelf)
            {
                StartCoroutine(HideCoroutine(onComplete));
            }
            else
            {
                isShowing = false;
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Immediately show the answer text on the card (no animation, just swap text).
        /// </summary>
        public void ShowAnswerText(string answer)
        {
            if (frontText != null)
            {
                frontText.text = answer;
                frontText.color = new Color(1f, 0.85f, 0.4f); // Gold color for answer
            }
        }

        /// <summary>
        /// Dismiss the card - flip back and return to source position.
        /// Call this after ShowAnswerText when ready to close.
        /// </summary>
        public void DismissCard(Action onComplete = null)
        {
            if (cardContainer != null && cardContainer.activeSelf)
            {
                StartCoroutine(DismissCardCoroutine(onComplete));
            }
            else
            {
                isShowing = false;
                onComplete?.Invoke();
            }
        }

        private IEnumerator DismissCardCoroutine(Action onComplete)
        {
            isAnimating = true;
            
            // No delay - start animating immediately
            float liftedScaleRatio = lastScaleRatio * liftScale;

            // Phase 1: Shrink and fly back to source position
            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float curvedT = flyCurve.Evaluate(t);

                Vector2 currentCenter = Vector2.Lerp(lastTargetCenter, lastSourceCenter, curvedT);
                float currentScale = Mathf.Lerp(1f, liftedScaleRatio, curvedT);

                containerRect.anchoredPosition = currentCenter;
                containerRect.localScale = new Vector3(currentScale, currentScale, 1f);

                float shadowScale = Mathf.Lerp(2f, 1.2f, curvedT);
                shadowCardRect.anchoredPosition = currentCenter + new Vector2(shadowOffset * shadowScale, -shadowOffset * shadowScale);
                shadowCardRect.sizeDelta = Vector2.Lerp(lastTargetSize, lastSourceSize * liftScale, curvedT);

                yield return null;
            }

            // Phase 2: Flip back (front to back)
            elapsed = 0f;
            bool hasFlipped = false;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipDuration);
                float curvedT = flipCurve.Evaluate(t);

                float rotationProgress = curvedT * 180f;
                float flipScaleX = Mathf.Abs(Mathf.Cos(rotationProgress * Mathf.Deg2Rad));
                float flipScaleY = 1f + (1f - flipScaleX) * 0.08f;
                
                containerRect.localScale = new Vector3(
                    Mathf.Max(0.01f, flipScaleX) * liftedScaleRatio,
                    flipScaleY * liftedScaleRatio,
                    1f
                );

                if (rotationProgress >= 90f && !hasFlipped)
                {
                    hasFlipped = true;
                    frontCard.SetActive(false);
                    backCard.SetActive(true);
                    backText.text = "";
                }

                yield return null;
            }

            // Phase 3: Settle and fade out
            containerRect.localScale = new Vector3(liftedScaleRatio, liftedScaleRatio, 1f);
            
            elapsed = 0f;
            float settleDuration = liftDuration;
            
            CanvasGroup cardGroup = cardContainer.GetComponent<CanvasGroup>();
            if (cardGroup == null) cardGroup = cardContainer.AddComponent<CanvasGroup>();
            CanvasGroup shadowGroup = shadowCard.GetComponent<CanvasGroup>();
            if (shadowGroup == null) shadowGroup = shadowCard.AddComponent<CanvasGroup>();

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float curvedT = liftCurve.Evaluate(t);

                float currentScale = Mathf.Lerp(liftedScaleRatio, lastScaleRatio, curvedT);
                containerRect.localScale = new Vector3(currentScale, currentScale, 1f);

                float alpha = 1f - curvedT;
                cardGroup.alpha = alpha;
                shadowGroup.alpha = alpha * 0.6f;

                float shadowGrow = Mathf.Lerp(1.2f, 0.5f, curvedT);
                shadowCardRect.anchoredPosition = lastSourceCenter + new Vector2(shadowOffset * shadowGrow, -shadowOffset * shadowGrow);
                shadowCardRect.sizeDelta = Vector2.Lerp(lastSourceSize * liftScale, lastSourceSize, curvedT);

                yield return null;
            }

            // Reset and hide
            cardContainer.SetActive(false);
            shadowCard.SetActive(false);
            cardGroup.alpha = 1f;
            shadowGroup.alpha = 1f;
            frontText.color = Color.white;
            isShowing = false;
            isAnimating = false;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Show the answer on the card, then flip back and return to source position.
        /// </summary>
        public void ShowAnswerAndDismiss(string answer, Action onComplete = null)
        {
            if (cardContainer != null && cardContainer.activeSelf)
            {
                StartCoroutine(ShowAnswerAndDismissCoroutine(answer, onComplete));
            }
            else
            {
                isShowing = false;
                onComplete?.Invoke();
            }
        }

        private IEnumerator ShowAnswerAndDismissCoroutine(string answer, Action onComplete)
        {
            isAnimating = true;

            // Show answer on front card
            frontText.text = answer;
            frontText.color = new Color(1f, 0.85f, 0.4f); // Gold color for answer
            
            // Brief moment to see the answer appear, then animate out
            yield return new WaitForSeconds(0.3f);

            float liftedScaleRatio = lastScaleRatio * liftScale;

            // Phase 1: Shrink and fly back to source position
            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float curvedT = flyCurve.Evaluate(t);

                // Interpolate position and scale back to source
                Vector2 currentCenter = Vector2.Lerp(lastTargetCenter, lastSourceCenter, curvedT);
                float currentScale = Mathf.Lerp(1f, liftedScaleRatio, curvedT);

                containerRect.anchoredPosition = currentCenter;
                containerRect.localScale = new Vector3(currentScale, currentScale, 1f);

                // Shadow follows
                float shadowScale = Mathf.Lerp(2f, 1.2f, curvedT);
                shadowCardRect.anchoredPosition = currentCenter + new Vector2(shadowOffset * shadowScale, -shadowOffset * shadowScale);
                shadowCardRect.sizeDelta = Vector2.Lerp(lastTargetSize, lastSourceSize * liftScale, curvedT);

                yield return null;
            }

            // Phase 2: Flip back (front to back)
            elapsed = 0f;
            bool hasFlipped = false;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipDuration);
                float curvedT = flipCurve.Evaluate(t);

                // Scale X to simulate reverse rotation
                float rotationProgress = curvedT * 180f;
                float flipScaleX = Mathf.Abs(Mathf.Cos(rotationProgress * Mathf.Deg2Rad));
                float flipScaleY = 1f + (1f - flipScaleX) * 0.08f;
                
                containerRect.localScale = new Vector3(
                    Mathf.Max(0.01f, flipScaleX) * liftedScaleRatio,
                    flipScaleY * liftedScaleRatio,
                    1f
                );

                // Swap cards at midpoint (show back/empty)
                if (rotationProgress >= 90f && !hasFlipped)
                {
                    hasFlipped = true;
                    frontCard.SetActive(false);
                    backCard.SetActive(true);
                    backText.text = ""; // Empty back
                }

                yield return null;
            }

            // Phase 3: Settle back to source size and fade out
            containerRect.localScale = new Vector3(liftedScaleRatio, liftedScaleRatio, 1f);
            
            elapsed = 0f;
            float settleDuration = liftDuration;
            
            CanvasGroup cardGroup = cardContainer.GetComponent<CanvasGroup>();
            if (cardGroup == null)
            {
                cardGroup = cardContainer.AddComponent<CanvasGroup>();
            }
            CanvasGroup shadowGroup = shadowCard.GetComponent<CanvasGroup>();
            if (shadowGroup == null)
            {
                shadowGroup = shadowCard.AddComponent<CanvasGroup>();
            }

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float curvedT = liftCurve.Evaluate(t);

                float currentScale = Mathf.Lerp(liftedScaleRatio, lastScaleRatio, curvedT);
                containerRect.localScale = new Vector3(currentScale, currentScale, 1f);

                // Fade out
                float alpha = 1f - curvedT;
                cardGroup.alpha = alpha;
                shadowGroup.alpha = alpha * 0.6f;

                // Shadow settles
                float shadowGrow = Mathf.Lerp(1.2f, 0.5f, curvedT);
                shadowCardRect.anchoredPosition = lastSourceCenter + new Vector2(shadowOffset * shadowGrow, -shadowOffset * shadowGrow);
                shadowCardRect.sizeDelta = Vector2.Lerp(lastSourceSize * liftScale, lastSourceSize, curvedT);

                yield return null;
            }

            // Reset and hide
            cardContainer.SetActive(false);
            shadowCard.SetActive(false);
            cardGroup.alpha = 1f;
            shadowGroup.alpha = 1f;
            frontText.color = Color.white; // Reset text color
            isShowing = false;
            isAnimating = false;

            onComplete?.Invoke();
        }

        private IEnumerator HideCoroutine(Action onComplete)
        {
            float duration = 0.2f;
            
            CanvasGroup cardGroup = cardContainer.GetComponent<CanvasGroup>();
            if (cardGroup == null)
            {
                cardGroup = cardContainer.AddComponent<CanvasGroup>();
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

            cardContainer.SetActive(false);
            shadowCard.SetActive(false);
            cardGroup.alpha = 1f;
            shadowGroup.alpha = 1f;
            isShowing = false;

            onComplete?.Invoke();
        }

        public bool IsShowing => isShowing;
    }
}
