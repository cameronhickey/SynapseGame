using UnityEngine;
using UnityEngine.UI;
using Cerebrum.Game;

namespace Cerebrum.UI
{
    /// <summary>
    /// Displays progress bars for question time (bottom of screen) and player response time (above player panel).
    /// </summary>
    public class QuestionTimerBar : MonoBehaviour
    {
        [Header("Question Timer Settings")]
        [SerializeField] private float barHeight = 8f;
        [SerializeField] private Color questionFullColor = new Color(0.2f, 0.6f, 1f, 0.9f); // Blue
        [SerializeField] private Color questionLowColor = new Color(1f, 0.3f, 0.2f, 0.9f);  // Red
        [SerializeField] private float lowThreshold = 0.3f; // When to start turning red

        [Header("Response Timer Settings")]
        [SerializeField] private float responseBarHeight = 6f;
        [SerializeField] private Color responseFullColor = new Color(0.2f, 0.9f, 0.4f, 0.9f); // Green
        [SerializeField] private Color responseLowColor = new Color(1f, 0.6f, 0.2f, 0.9f);   // Orange

        [Header("Buzzer Sound")]
        [SerializeField] private AudioClip buzzerSound;
        
        // Question timer bar (bottom of screen)
        private GameObject questionBarContainer;
        private Image questionFillImage;
        private RectTransform questionFillRect;
        private bool questionBarVisible;

        // Response timer bar (above player panel)
        private GameObject responseBarContainer;
        private RectTransform responseBarRect;
        private Image responseFillImage;
        private RectTransform responseFillRect;
        private bool responseBarVisible;
        private int currentBuzzedPlayerIndex = -1;

        private AudioSource audioSource;
        private Canvas canvas;

        private void Start()
        {
            CreateTimerBars();
            
            // Get or create audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Subscribe to timer events
            if (AnswerFlowController.Instance != null)
            {
                AnswerFlowController.Instance.OnQuestionTimerUpdate += OnQuestionTimerUpdate;
                AnswerFlowController.Instance.OnQuestionTimerExpired += OnQuestionTimerExpired;
                AnswerFlowController.Instance.OnResponseTimerUpdate += OnResponseTimerUpdate;
                AnswerFlowController.Instance.OnResponseTimerExpired += OnResponseTimerExpired;
                AnswerFlowController.Instance.OnPlayerBuzzed += OnPlayerBuzzed;
                AnswerFlowController.Instance.OnStateChanged += OnStateChanged;
            }
            
            HideQuestionBar();
            HideResponseBar();
        }

        private void OnDestroy()
        {
            if (AnswerFlowController.Instance != null)
            {
                AnswerFlowController.Instance.OnQuestionTimerUpdate -= OnQuestionTimerUpdate;
                AnswerFlowController.Instance.OnQuestionTimerExpired -= OnQuestionTimerExpired;
                AnswerFlowController.Instance.OnResponseTimerUpdate -= OnResponseTimerUpdate;
                AnswerFlowController.Instance.OnResponseTimerExpired -= OnResponseTimerExpired;
                AnswerFlowController.Instance.OnPlayerBuzzed -= OnPlayerBuzzed;
                AnswerFlowController.Instance.OnStateChanged -= OnStateChanged;
            }
        }

        private void CreateTimerBars()
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[QuestionTimerBar] No canvas found");
                return;
            }

            // Create question timer bar (bottom of screen)
            questionBarContainer = CreateQuestionBar();

            // Create response timer bar (will be positioned above player panel when needed)
            responseBarContainer = CreateResponseBar();
        }

        private GameObject CreateQuestionBar()
        {
            GameObject container = new GameObject("QuestionTimerBar");
            container.transform.SetParent(canvas.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, barHeight);

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(container.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(container.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = questionFullColor;

            questionFillRect = fillRect;
            questionFillImage = fillImage;

            container.transform.SetAsLastSibling();
            return container;
        }

        private GameObject CreateResponseBar()
        {
            GameObject container = new GameObject("ResponseTimerBar");
            container.transform.SetParent(canvas.transform, false);
            
            responseBarRect = container.AddComponent<RectTransform>();
            // Will be positioned dynamically above player panel
            responseBarRect.sizeDelta = new Vector2(200, responseBarHeight);

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(container.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(container.transform, false);
            
            responseFillRect = fill.AddComponent<RectTransform>();
            responseFillRect.anchorMin = new Vector2(0, 0);
            responseFillRect.anchorMax = new Vector2(1, 1);
            responseFillRect.pivot = new Vector2(0, 0.5f);
            responseFillRect.anchoredPosition = Vector2.zero;
            responseFillRect.sizeDelta = Vector2.zero;
            
            responseFillImage = fill.AddComponent<Image>();
            responseFillImage.color = responseFullColor;

            container.transform.SetAsLastSibling();
            container.SetActive(false);
            return container;
        }

        private void OnPlayerBuzzed(int playerIndex)
        {
            currentBuzzedPlayerIndex = playerIndex;
            PositionResponseBarAbovePlayer(playerIndex);
        }

        private void PositionResponseBarAbovePlayer(int playerIndex)
        {
            if (responseBarRect == null) return;

            // Find the BoardController to get player panels
            var boardController = FindFirstObjectByType<BoardController>();
            if (boardController == null || boardController.PlayerPanels == null) return;
            if (playerIndex < 0 || playerIndex >= boardController.PlayerPanels.Count) return;

            var playerPanel = boardController.PlayerPanels[playerIndex];
            if (playerPanel == null) return;

            RectTransform panelRect = playerPanel.GetComponent<RectTransform>();
            if (panelRect == null) return;

            // Get the world corners of the player panel
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);
            
            // Convert to canvas space
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            
            // Position the bar above the panel (corners[1] is top-left, corners[2] is top-right)
            Vector3 topCenter = (corners[1] + corners[2]) / 2f;
            
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, topCenter),
                canvas.worldCamera, 
                out localPoint);

            // Set position and size to match player panel width
            float panelWidth = Vector3.Distance(corners[1], corners[2]);
            responseBarRect.anchorMin = new Vector2(0.5f, 0.5f);
            responseBarRect.anchorMax = new Vector2(0.5f, 0.5f);
            responseBarRect.pivot = new Vector2(0.5f, 0);
            responseBarRect.anchoredPosition = localPoint + new Vector2(0, 4); // 4px gap above panel
            responseBarRect.sizeDelta = new Vector2(panelWidth, responseBarHeight);
        }

        private void OnQuestionTimerUpdate(float progress)
        {
            if (!questionBarVisible && progress > 0f && progress < 1f)
            {
                ShowQuestionBar();
            }

            if (questionFillRect != null)
            {
                questionFillRect.anchorMax = new Vector2(progress, 1);
            }

            if (questionFillImage != null)
            {
                if (progress <= lowThreshold)
                {
                    float t = progress / lowThreshold;
                    questionFillImage.color = Color.Lerp(questionLowColor, questionFullColor, t);
                }
                else
                {
                    questionFillImage.color = questionFullColor;
                }
            }
        }

        private void OnQuestionTimerExpired()
        {
            Debug.Log("[QuestionTimerBar] Question timer expired - playing buzzer");
            PlayBuzzer();
        }

        private void OnResponseTimerUpdate(float progress)
        {
            if (progress <= 0f)
            {
                HideResponseBar();
                return;
            }

            if (!responseBarVisible)
            {
                ShowResponseBar();
            }

            if (responseFillRect != null)
            {
                responseFillRect.anchorMax = new Vector2(progress, 1);
            }

            if (responseFillImage != null)
            {
                if (progress <= lowThreshold)
                {
                    float t = progress / lowThreshold;
                    responseFillImage.color = Color.Lerp(responseLowColor, responseFullColor, t);
                }
                else
                {
                    responseFillImage.color = responseFullColor;
                }
            }
        }

        private void OnResponseTimerExpired()
        {
            Debug.Log("[QuestionTimerBar] Response timer expired - playing buzzer");
            PlayBuzzer();
            HideResponseBar();
        }

        private void PlayBuzzer()
        {
            if (audioSource == null) return;

            if (buzzerSound != null)
            {
                audioSource.PlayOneShot(buzzerSound);
                return;
            }

            // Try to load from resources
            var clip = Resources.Load<AudioClip>("Audio/Buzzer");
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
                return;
            }

            // Generate a simple buzzer sound programmatically
            clip = GenerateBuzzerSound();
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private AudioClip GenerateBuzzerSound()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int sampleCount = (int)(sampleRate * duration);
            
            AudioClip clip = AudioClip.Create("Buzzer", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];
            
            // Generate a harsh buzzer tone (mix of frequencies)
            float baseFreq = 220f; // A3
            float freq2 = 280f;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration); // Fade out
                envelope = Mathf.Pow(envelope, 0.5f); // Slower fade
                
                // Mix two frequencies for harsh sound
                float wave1 = Mathf.Sin(2 * Mathf.PI * baseFreq * t);
                float wave2 = Mathf.Sin(2 * Mathf.PI * freq2 * t);
                float wave3 = Mathf.Sin(2 * Mathf.PI * baseFreq * 2 * t) * 0.3f; // Harmonic
                
                samples[i] = (wave1 * 0.5f + wave2 * 0.3f + wave3) * envelope * 0.6f;
            }
            
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnStateChanged(AnswerFlowState state)
        {
            switch (state)
            {
                case AnswerFlowState.WaitingForBuzz:
                    // Keep question bar visible, hide response bar
                    HideResponseBar();
                    break;
                case AnswerFlowState.Recording:
                    // Keep question bar visible (paused), response bar shows above player
                    break;
                case AnswerFlowState.Complete:
                    HideQuestionBar();
                    HideResponseBar();
                    currentBuzzedPlayerIndex = -1;
                    break;
            }
        }

        private void ShowQuestionBar()
        {
            questionBarVisible = true;
            if (questionBarContainer != null)
            {
                questionBarContainer.SetActive(true);
                questionBarContainer.transform.SetAsLastSibling();
            }
        }

        private void HideQuestionBar()
        {
            questionBarVisible = false;
            if (questionBarContainer != null)
            {
                questionBarContainer.SetActive(false);
            }
        }

        private void ShowResponseBar()
        {
            responseBarVisible = true;
            if (responseBarContainer != null)
            {
                responseBarContainer.SetActive(true);
                responseBarContainer.transform.SetAsLastSibling();
            }
        }

        private void HideResponseBar()
        {
            responseBarVisible = false;
            if (responseBarContainer != null)
            {
                responseBarContainer.SetActive(false);
            }
        }
    }
}
