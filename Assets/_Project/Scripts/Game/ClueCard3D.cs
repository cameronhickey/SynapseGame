using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Cerebrum.Data;

namespace Cerebrum.Game
{
    /// <summary>
    /// A 3D clue card that can flip with real perspective animation.
    /// </summary>
    public class ClueCard3D : MonoBehaviour
    {
        public Clue Clue { get; private set; }
        public Category Category { get; private set; }
        public int Value { get; private set; }
        public bool IsUsed { get; private set; }
        
        public event Action<ClueCard3D> OnCardClicked;
        
        [Header("Animation")]
        [SerializeField] private float flipDuration = 0.6f;
        [SerializeField] private float liftDistance = 0.5f;
        [SerializeField] private float growScale = 2.5f;
        [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private GameObject frontFace;
        private GameObject backFace;
        private TextMeshPro valueText;
        private TextMeshPro clueText;
        private TextMeshPro answerText;
        private MeshRenderer frontRenderer;
        private MeshRenderer backRenderer;
        private BoxCollider cardCollider;
        
        private float cardWidth;
        private float cardHeight;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isAnimating;

        public void Initialize(Clue clue, Category category, int value, float width, float height, 
            Material frontMaterial, Material backMaterial)
        {
            Clue = clue;
            Category = category;
            Value = value;
            cardWidth = width;
            cardHeight = height;
            
            originalPosition = transform.localPosition;
            originalScale = Vector3.one;
            
            CreateCardGeometry(frontMaterial, backMaterial);
            CreateValueText();
        }

        private void CreateCardGeometry(Material frontMaterial, Material backMaterial)
        {
            float depth = 0.05f;
            
            // Front face (shows dollar value initially)
            frontFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontFace.name = "FrontFace";
            frontFace.transform.SetParent(transform);
            frontFace.transform.localPosition = Vector3.zero;
            frontFace.transform.localRotation = Quaternion.identity;
            frontFace.transform.localScale = new Vector3(cardWidth, cardHeight, depth);
            
            frontRenderer = frontFace.GetComponent<MeshRenderer>();
            frontRenderer.material = new Material(frontMaterial);
            frontRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            frontRenderer.receiveShadows = true;
            
            // Remove default collider, we'll add our own
            Destroy(frontFace.GetComponent<Collider>());
            
            // Back face (shows clue text when flipped)
            backFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backFace.name = "BackFace";
            backFace.transform.SetParent(transform);
            backFace.transform.localPosition = Vector3.zero;
            backFace.transform.localRotation = Quaternion.identity;
            backFace.transform.localScale = new Vector3(cardWidth, cardHeight, depth);
            
            backRenderer = backFace.GetComponent<MeshRenderer>();
            backRenderer.material = new Material(backMaterial);
            backRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            backRenderer.receiveShadows = true;
            backFace.SetActive(false);
            
            // Remove default collider
            Destroy(backFace.GetComponent<Collider>());
            
            // Add single collider for the card
            cardCollider = gameObject.AddComponent<BoxCollider>();
            cardCollider.size = new Vector3(cardWidth, cardHeight, depth);
        }

        private void CreateValueText()
        {
            // Value text on front - position slightly in front of the card face
            GameObject valueObj = new GameObject("ValueText");
            valueObj.transform.SetParent(transform); // Parent to card, not frontFace
            valueObj.transform.localPosition = new Vector3(0, 0, -0.03f); // Just in front of card
            valueObj.transform.localRotation = Quaternion.identity;
            
            valueText = valueObj.AddComponent<TextMeshPro>();
            valueText.text = "$" + Value;
            valueText.fontSize = 6;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = new Color(1f, 0.85f, 0.4f); // Gold
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Overflow;
            
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            
            // Clue text on back (hidden initially)
            GameObject clueObj = new GameObject("ClueText");
            clueObj.transform.SetParent(transform); // Parent to card
            clueObj.transform.localPosition = new Vector3(0, 0.05f, 0.03f); // Behind card (visible when flipped)
            clueObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
            
            clueText = clueObj.AddComponent<TextMeshPro>();
            clueText.text = Clue.Question;
            clueText.fontSize = 3;
            clueText.alignment = TextAlignmentOptions.Center;
            clueText.color = Color.white;
            clueText.textWrappingMode = TextWrappingModes.Normal;
            clueText.overflowMode = TextOverflowModes.Ellipsis;
            clueText.enableAutoSizing = true;
            clueText.fontSizeMin = 1.5f;
            clueText.fontSizeMax = 4f;
            
            RectTransform clueRect = clueText.GetComponent<RectTransform>();
            clueRect.sizeDelta = new Vector2(cardWidth * 0.9f, cardHeight * 0.6f);
            clueObj.SetActive(false); // Hidden until flipped
            
            // Answer text (shown after reveal)
            GameObject answerObj = new GameObject("AnswerText");
            answerObj.transform.SetParent(transform);
            answerObj.transform.localPosition = new Vector3(0, -0.15f, 0.03f);
            answerObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
            
            answerText = answerObj.AddComponent<TextMeshPro>();
            answerText.text = "";
            answerText.fontSize = 4;
            answerText.alignment = TextAlignmentOptions.Center;
            answerText.color = new Color(1f, 0.85f, 0.4f); // Gold
            answerText.fontStyle = FontStyles.Bold;
            
            RectTransform answerRect = answerText.GetComponent<RectTransform>();
            answerRect.sizeDelta = new Vector2(cardWidth * 0.9f, cardHeight * 0.3f);
            answerObj.SetActive(false); // Hidden until needed
        }

        private void OnMouseDown()
        {
            if (!isAnimating && !IsUsed)
            {
                OnCardClicked?.Invoke(this);
            }
        }

        private void OnMouseEnter()
        {
            if (!isAnimating && !IsUsed)
            {
                // Highlight effect
                if (frontRenderer != null)
                {
                    frontRenderer.material.SetColor("_EmissionColor", new Color(0.1f, 0.2f, 0.4f));
                    frontRenderer.material.EnableKeyword("_EMISSION");
                }
            }
        }

        private void OnMouseExit()
        {
            if (frontRenderer != null)
            {
                frontRenderer.material.DisableKeyword("_EMISSION");
            }
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
            if (cardCollider != null)
            {
                cardCollider.enabled = false;
            }
        }

        /// <summary>
        /// Animate the card spinning into view from off-screen
        /// </summary>
        public void SpinIntoView(float delay, float duration = 0.8f)
        {
            StartCoroutine(SpinIntoViewCoroutine(delay, duration));
        }

        private IEnumerator SpinIntoViewCoroutine(float delay, float duration)
        {
            // Start hidden and rotated
            Vector3 targetPos = transform.localPosition;
            Vector3 startPos = targetPos + new Vector3(0, 2f, -3f); // Start above and behind
            transform.localPosition = startPos;
            transform.localRotation = Quaternion.Euler(90, 0, 180); // Face away
            transform.localScale = Vector3.zero;
            
            // Hide initially
            if (frontFace != null) frontFace.SetActive(false);
            if (valueText != null) valueText.gameObject.SetActive(false);
            
            yield return new WaitForSeconds(delay);
            
            // Show card
            if (frontFace != null) frontFace.SetActive(true);
            if (valueText != null) valueText.gameObject.SetActive(true);
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                
                // Spin in (multiple rotations)
                float spinAngle = (1f - eased) * 720f; // 2 full rotations
                transform.localRotation = Quaternion.Euler(spinAngle * 0.3f, spinAngle, 0);
                
                // Move to position
                transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);
                
                // Scale up
                transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, eased);
                
                yield return null;
            }
            
            // Ensure final state
            transform.localPosition = targetPos;
            transform.localRotation = Quaternion.identity;
            transform.localScale = originalScale;
        }

        public void FlipAndReveal(Action onComplete = null)
        {
            if (isAnimating) return;
            StartCoroutine(FlipAndRevealCoroutine(onComplete));
        }

        private IEnumerator FlipAndRevealCoroutine(Action onComplete)
        {
            isAnimating = true;
            
            Vector3 startPos = transform.localPosition;
            Vector3 liftedPos = startPos + new Vector3(0, 0, -liftDistance);
            Vector3 centerPos = new Vector3(0, 0, -liftDistance * 2);
            
            // Phase 1: Lift card slightly
            float elapsed = 0f;
            float liftDuration = 0.2f;
            while (elapsed < liftDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / liftDuration;
                transform.localPosition = Vector3.Lerp(startPos, liftedPos, t);
                yield return null;
            }
            
            // Phase 2: Move to center and grow while flipping
            elapsed = 0f;
            float moveAndFlipDuration = flipDuration;
            bool hasSwapped = false;
            
            while (elapsed < moveAndFlipDuration)
            {
                elapsed += Time.deltaTime;
                float t = flipCurve.Evaluate(elapsed / moveAndFlipDuration);
                
                // Move to center
                transform.localPosition = Vector3.Lerp(liftedPos, centerPos, t);
                
                // Scale up
                float scale = Mathf.Lerp(1f, growScale, t);
                transform.localScale = originalScale * scale;
                
                // Rotate (flip)
                float angle = t * 180f;
                transform.localRotation = Quaternion.Euler(0, angle, 0);
                
                // Swap faces at 90 degrees
                if (angle >= 90f && !hasSwapped)
                {
                    hasSwapped = true;
                    frontFace.SetActive(false);
                    backFace.SetActive(true);
                    // Show back text elements
                    if (valueText != null) valueText.gameObject.SetActive(false);
                    if (clueText != null) clueText.gameObject.SetActive(true);
                }
                
                yield return null;
            }
            
            // Ensure final state
            transform.localPosition = centerPos;
            transform.localScale = originalScale * growScale;
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            frontFace.SetActive(false);
            backFace.SetActive(true);
            
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
            
            Vector3 startPos = transform.localPosition;
            Vector3 startScale = transform.localScale;
            Quaternion startRot = transform.localRotation;
            
            // Phase 1: Flip back
            float elapsed = 0f;
            bool hasSwapped = false;
            
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = flipCurve.Evaluate(elapsed / flipDuration);
                
                // Rotate back
                float angle = 180f + t * 180f;
                transform.localRotation = Quaternion.Euler(0, angle, 0);
                
                // Swap faces back at 270 degrees (90 from current)
                if (angle >= 270f && !hasSwapped)
                {
                    hasSwapped = true;
                    backFace.SetActive(false);
                    frontFace.SetActive(true);
                    
                    // Change front to show empty/used state
                    if (valueText != null)
                    {
                        valueText.text = "";
                    }
                    if (frontRenderer != null)
                    {
                        frontRenderer.material.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);
                    }
                }
                
                yield return null;
            }
            
            // Phase 2: Move back and shrink
            elapsed = 0f;
            float returnDuration = 0.3f;
            Vector3 currentPos = transform.localPosition;
            
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnDuration;
                
                transform.localPosition = Vector3.Lerp(currentPos, originalPosition, t);
                transform.localScale = Vector3.Lerp(startScale, originalScale, t);
                
                yield return null;
            }
            
            // Final state
            transform.localPosition = originalPosition;
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
            
            isAnimating = false;
            
            onComplete?.Invoke();
        }
    }
}
