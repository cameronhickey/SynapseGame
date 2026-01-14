using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Cerebrum.UI
{
    /// <summary>
    /// Handles focus behavior for cosmic-themed input fields:
    /// - Changes border from cyan to orange when focused
    /// </summary>
    public class CosmicInputField : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image borderImage;
        [SerializeField] private Image backgroundImage;
        
        private TMP_InputField inputField;
        
        // Colors
        private Color cyanBorderColor = new Color(0.55f, 0.8f, 1f, 0.9f);
        private Color cyanBgColor = new Color(0.075f, 0.2f, 0.36f, 0.85f);
        private Color orangeBorderColor = new Color(1f, 0.7f, 0.3f, 0.95f);
        private Color orangeBgColor = new Color(0.4f, 0.25f, 0.1f, 0.85f);

        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            if (inputField != null)
            {
                inputField.onSelect.AddListener(OnInputSelect);
                inputField.onDeselect.AddListener(OnInputDeselect);
            }
        }

        public void Initialize(Image border, Image background, string defaultValue)
        {
            borderImage = border;
            backgroundImage = background;
            
            // Set initial colors
            if (borderImage != null) borderImage.color = cyanBorderColor;
            if (backgroundImage != null) backgroundImage.color = cyanBgColor;
        }

        public void SetColors(Color cyanBorder, Color cyanBg, Color orangeBorder, Color orangeBg)
        {
            cyanBorderColor = cyanBorder;
            cyanBgColor = cyanBg;
            orangeBorderColor = orangeBorder;
            orangeBgColor = orangeBg;
            
            // Apply current state
            if (inputField == null || !inputField.isFocused)
            {
                if (borderImage != null) borderImage.color = cyanBorderColor;
                if (backgroundImage != null) backgroundImage.color = cyanBgColor;
            }
        }

        private void OnInputSelect(string value)
        {
            // Switch to orange highlight when focused
            if (borderImage != null) borderImage.color = orangeBorderColor;
            if (backgroundImage != null) backgroundImage.color = orangeBgColor;
        }

        private void OnInputDeselect(string value)
        {
            // Switch back to cyan when unfocused
            if (borderImage != null) borderImage.color = cyanBorderColor;
            if (backgroundImage != null) backgroundImage.color = cyanBgColor;
        }

        public void OnSelect(BaseEventData eventData)
        {
            OnInputSelect(inputField?.text ?? "");
        }

        public void OnDeselect(BaseEventData eventData)
        {
            OnInputDeselect(inputField?.text ?? "");
        }

        private void OnDestroy()
        {
            if (inputField != null)
            {
                inputField.onSelect.RemoveListener(OnInputSelect);
                inputField.onDeselect.RemoveListener(OnInputDeselect);
            }
        }
    }
}
