using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Cerebrum.UI
{
    /// <summary>
    /// Adds scale and glow effects to the skip button on hover and click.
    /// </summary>
    public class SkipButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform buttonRect;
        private Image borderImage;
        private Vector3 originalScale;
        private Color originalBorderColor;
        
        private const float HoverScale = 1.08f;
        private const float PressScale = 0.95f;
        private float targetScale = 1f;
        private float currentScale = 1f;
        private bool isHovering;
        private bool isPressed;

        public void Initialize(RectTransform rect, Image border)
        {
            buttonRect = rect;
            borderImage = border;
            if (buttonRect != null)
            {
                originalScale = buttonRect.localScale;
            }
            if (borderImage != null)
            {
                originalBorderColor = borderImage.color;
            }
        }

        private void Update()
        {
            // Smooth scale animation
            if (isPressed)
            {
                targetScale = PressScale;
            }
            else if (isHovering)
            {
                targetScale = HoverScale;
            }
            else
            {
                targetScale = 1f;
            }

            currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * 15f);
            
            if (buttonRect != null)
            {
                buttonRect.localScale = originalScale * currentScale;
            }

            // Update border glow based on hover state
            if (borderImage != null)
            {
                Color targetColor = isHovering 
                    ? new Color(0.7f, 0.9f, 1f, 1f)  // Brighter cyan on hover
                    : originalBorderColor;
                borderImage.color = Color.Lerp(borderImage.color, targetColor, Time.deltaTime * 10f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }
    }
}
