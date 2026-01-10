using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Cerebrum.UI
{
    public class PreloadPromptUI : MonoBehaviour
    {
        private GameObject promptPanel;
        private Button preloadButton;
        private Button skipButton;

        public event Action<bool> OnChoiceMade;

        public void Setup(GameObject panel, Button preload, Button skip)
        {
            promptPanel = panel;
            preloadButton = preload;
            skipButton = skip;

            if (preloadButton != null)
            {
                preloadButton.onClick.AddListener(() => MakeChoice(true));
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(() => MakeChoice(false));
            }
        }

        public void Show()
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(true);
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void MakeChoice(bool preload)
        {
            Hide();
            OnChoiceMade?.Invoke(preload);
        }

        private void OnDestroy()
        {
            if (preloadButton != null)
            {
                preloadButton.onClick.RemoveAllListeners();
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
            }
        }
    }
}
