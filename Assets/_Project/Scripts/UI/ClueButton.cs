using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Cerebrum.Data;
using Cerebrum.OpenAI;

namespace Cerebrum.UI
{
    public class ClueButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image backgroundImage;

        [Header("Reveal Animation")]
        [SerializeField] private float revealDuration = 0.3f;
        [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public int CategoryIndex { get; private set; }
        public int RowIndex { get; private set; }
        public int Value { get; private set; }
        public bool IsUsed { get; private set; }
        public bool IsRevealed { get; private set; }

        private Color activeColor = new Color(0.06f, 0.06f, 0.4f); // Jeopardy blue
        private Color usedColor = new Color(0.1f, 0.1f, 0.15f);
        private Color hiddenColor = new Color(0.03f, 0.03f, 0.2f);

        private Clue associatedClue;
        private static AudioSource sharedAudioSource;
        private static AudioClip revealSound;

        public System.Action<int, int> OnClueSelected;

        public void Initialize(int categoryIndex, int rowIndex, int value, bool startHidden = false)
        {
            CategoryIndex = categoryIndex;
            RowIndex = rowIndex;
            Value = value;
            IsUsed = false;
            IsRevealed = !startHidden;

            if (valueText != null)
            {
                valueText.text = $"${value}";
                
                if (startHidden)
                {
                    valueText.gameObject.SetActive(false);
                }
            }

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
                button.interactable = !startHidden;
            }

            if (backgroundImage != null && startHidden)
            {
                backgroundImage.color = hiddenColor;
            }
            else
            {
                UpdateVisual();
            }
        }

        public void SetAssociatedClue(Clue clue)
        {
            associatedClue = clue;
        }

        public Clue GetAssociatedClue()
        {
            return associatedClue;
        }

        public void RevealWithAnimation()
        {
            if (IsRevealed) return;
            
            IsRevealed = true;
            StartCoroutine(RevealAnimationCoroutine());
        }

        private IEnumerator RevealAnimationCoroutine()
        {
            // Play reveal sound
            PlayRevealSound();

            // Animate the value text popping in
            if (valueText != null)
            {
                valueText.gameObject.SetActive(true);
                Transform textTransform = valueText.transform;
                Vector3 originalScale = textTransform.localScale;
                
                float elapsed = 0f;
                while (elapsed < revealDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = revealCurve.Evaluate(elapsed / revealDuration);
                    
                    // Scale from 0 to full size with overshoot
                    float scale = t * 1.1f;
                    if (t > 0.7f)
                    {
                        scale = Mathf.Lerp(1.1f, 1f, (t - 0.7f) / 0.3f);
                    }
                    
                    textTransform.localScale = originalScale * scale;
                    yield return null;
                }

                textTransform.localScale = originalScale;
            }

            // Update background color
            if (backgroundImage != null)
            {
                backgroundImage.color = activeColor;
            }

            // Enable button
            if (button != null)
            {
                button.interactable = true;
            }
        }

        private void PlayRevealSound()
        {
            if (sharedAudioSource == null)
            {
                GameObject audioObj = new GameObject("ClueRevealAudio");
                sharedAudioSource = audioObj.AddComponent<AudioSource>();
                sharedAudioSource.playOnAwake = false;
                
                // Generate a simple "bomp" sound programmatically
                revealSound = GenerateBompSound();
            }

            if (revealSound != null)
            {
                sharedAudioSource.PlayOneShot(revealSound, 0.5f);
            }
        }

        private static AudioClip GenerateBompSound()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = (int)(sampleRate * duration);
            
            AudioClip clip = AudioClip.Create("Bomp", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 15f); // Quick decay
                
                // Low frequency "bomp" with slight pitch drop
                float freq = 220f * (1f - t * 0.3f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t * duration);
                
                samples[i] = wave * envelope * 0.8f;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        private void OnButtonClicked()
        {
            if (!IsUsed)
            {
                OnClueSelected?.Invoke(CategoryIndex, RowIndex);
            }
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = IsUsed ? usedColor : activeColor;
            }

            if (valueText != null)
            {
                valueText.gameObject.SetActive(!IsUsed);
            }

            if (button != null)
            {
                button.interactable = !IsUsed;
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }
    }
}
