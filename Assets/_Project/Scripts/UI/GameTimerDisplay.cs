using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cerebrum.UI
{
    public class GameTimerDisplay : MonoBehaviour
    {
        public static GameTimerDisplay Instance { get; private set; }

        [Header("Timer Settings")]
        [SerializeField] private float buzzTimerDuration = 10f;
        [SerializeField] private float responseTimerDuration = 5f;

        [Header("Main Timer Visual Settings")]
        [SerializeField] private float barHeight = 10f;
        [SerializeField] private Color barColor = new Color(0.2f, 0.7f, 1f, 0.9f);
        [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.2f, 0.9f);
        [SerializeField] private float warningThreshold = 0.25f;

        [Header("Response Timer Visual Settings")]
        [SerializeField] private float responseBarHeight = 20f;
        [SerializeField] private float responseBarGap = 8f;
        [SerializeField] private int segmentCount = 10; // 10 red rectangles
        [SerializeField] private float segmentGap = 8f;
        [SerializeField] private Color responseBarColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red color

        [Header("Audio")]
        [SerializeField] private AudioClip buzzerSound;

        private Canvas parentCanvas;
        
        // Main buzz timer
        private GameObject timerBarContainer;
        private Image backgroundImage;
        private Image fillImage;
        private RectTransform fillRect;
        private float buzzDuration;
        private float buzzRemainingTime;
        private bool isBuzzRunning;
        private System.Action onBuzzComplete;

        // Response timer (dashed segments)
        private GameObject responseBarContainer;
        private List<Image> responseSegments = new List<Image>();
        private float responseDuration;
        private float responseRemainingTime;
        private bool isResponseRunning;
        private System.Action onResponseComplete;

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
            CreateTimerBars();
        }

        private void CreateTimerBars()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
            }

            if (parentCanvas == null)
            {
                Debug.LogError("[GameTimerDisplay] No canvas found!");
                return;
            }

            CreateMainTimerBar();
            CreateResponseTimerBar();

            Debug.Log("[GameTimerDisplay] Timer bars created");
        }

        private void CreateMainTimerBar()
        {
            // Create container at bottom of screen
            timerBarContainer = new GameObject("BuzzTimerBar");
            timerBarContainer.transform.SetParent(parentCanvas.transform, false);

            RectTransform containerRect = timerBarContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, barHeight);

            // Background (dark)
            backgroundImage = timerBarContainer.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill bar
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(timerBarContainer.transform, false);

            fillImage = fillObj.AddComponent<Image>();
            fillImage.color = barColor;

            fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            timerBarContainer.SetActive(false);
        }

        private void CreateResponseTimerBar()
        {
            // Create container above main timer bar
            responseBarContainer = new GameObject("ResponseTimerBar");
            responseBarContainer.transform.SetParent(parentCanvas.transform, false);

            RectTransform containerRect = responseBarContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, barHeight + responseBarGap);
            containerRect.sizeDelta = new Vector2(0, responseBarHeight);

            // Create segment container with horizontal layout
            GameObject segmentHolder = new GameObject("Segments");
            segmentHolder.transform.SetParent(responseBarContainer.transform, false);
            
            RectTransform holderRect = segmentHolder.AddComponent<RectTransform>();
            holderRect.anchorMin = Vector2.zero;
            holderRect.anchorMax = Vector2.one;
            holderRect.offsetMin = Vector2.zero;
            holderRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = segmentHolder.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = segmentGap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Create segments
            responseSegments.Clear();
            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segObj = new GameObject($"Segment_{i}");
                segObj.transform.SetParent(segmentHolder.transform, false);

                Image segImage = segObj.AddComponent<Image>();
                segImage.color = responseBarColor;

                LayoutElement layoutElement = segObj.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1;

                responseSegments.Add(segImage);
            }

            responseBarContainer.SetActive(false);
        }

        private void Update()
        {
            UpdateBuzzTimer();
            UpdateResponseTimer();
        }

        private void UpdateBuzzTimer()
        {
            if (!isBuzzRunning) return;

            buzzRemainingTime -= Time.deltaTime;

            if (buzzRemainingTime <= 0)
            {
                buzzRemainingTime = 0;
                isBuzzRunning = false;
                
                if (fillRect != null)
                {
                    fillRect.anchorMax = new Vector2(1, 1);
                }

                timerBarContainer?.SetActive(false);
                onBuzzComplete?.Invoke();
                return;
            }

            // Update fill (grows from left to right as time passes)
            float elapsed = buzzDuration - buzzRemainingTime;
            float progress = elapsed / buzzDuration;
            
            if (fillRect != null)
            {
                fillRect.anchorMax = new Vector2(progress, 1);
            }

            // Warning color when time is low
            float timeRatio = buzzRemainingTime / buzzDuration;
            if (fillImage != null)
            {
                fillImage.color = timeRatio <= warningThreshold ? warningColor : barColor;
            }
        }

        private void UpdateResponseTimer()
        {
            if (!isResponseRunning) return;

            responseRemainingTime -= Time.deltaTime;

            if (responseRemainingTime <= 0)
            {
                responseRemainingTime = 0;
                isResponseRunning = false;
                
                // Hide all segments
                foreach (var seg in responseSegments)
                {
                    seg.gameObject.SetActive(false);
                }

                responseBarContainer?.SetActive(false);
                
                // Play buzzer sound before callback
                PlayBuzzerSound();
                
                onResponseComplete?.Invoke();
                return;
            }

            // Calculate which segments to show based on time remaining
            // With 10 segments and 5 seconds, every second removes 2 segments (one from each edge)
            // At 5s: all 10 visible (1,2,3,4,5,6,7,8,9,10)
            // At 4s: 8 visible (2,3,4,5,6,7,8,9) - removed 1 and 10
            // At 3s: 6 visible (3,4,5,6,7,8) - removed 2 and 9
            // At 2s: 4 visible (4,5,6,7) - removed 3 and 8
            // At 1s: 2 visible (5,6) - removed 4 and 7
            // At 0s: 0 visible - removed 5 and 6
            
            float secondsRemaining = responseRemainingTime;
            int pairsToHide = Mathf.FloorToInt((responseDuration - secondsRemaining)); // How many pairs have been removed
            
            for (int i = 0; i < segmentCount; i++)
            {
                // Segments are numbered 0-9 (representing positions 1-10)
                // Index 0 = leftmost (position 1), Index 9 = rightmost (position 10)
                // We hide from edges: first hide 0 and 9, then 1 and 8, etc.
                
                int distanceFromEdge;
                if (i < segmentCount / 2)
                {
                    // Left half: distance from left edge
                    distanceFromEdge = i;
                }
                else
                {
                    // Right half: distance from right edge
                    distanceFromEdge = segmentCount - 1 - i;
                }
                
                // Show segment if its distance from edge is >= pairsToHide
                bool shouldShow = distanceFromEdge >= pairsToHide;
                responseSegments[i].gameObject.SetActive(shouldShow);
            }
        }

        private void PlayBuzzerSound()
        {
            if (buzzerSound != null)
            {
                AudioSource.PlayClipAtPoint(buzzerSound, Camera.main?.transform.position ?? Vector3.zero);
            }
            else
            {
                // Generate a simple buzzer tone if no clip assigned
                StartCoroutine(PlayGeneratedBuzzer());
            }
        }

        private System.Collections.IEnumerator PlayGeneratedBuzzer()
        {
            // Create a temporary AudioSource for the buzzer
            GameObject buzzerObj = new GameObject("BuzzerSound");
            AudioSource source = buzzerObj.AddComponent<AudioSource>();
            
            // Generate a simple buzzer tone (200Hz for 0.5 seconds)
            int sampleRate = 44100;
            float duration = 0.5f;
            int sampleCount = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Buzzer", sampleCount, 1, sampleRate, false);
            
            float[] samples = new float[sampleCount];
            float frequency = 200f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Square wave for harsh buzzer sound
                samples[i] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * frequency * t)) * 0.3f;
                // Fade out at the end
                if (i > sampleCount * 0.7f)
                {
                    samples[i] *= 1f - ((float)(i - sampleCount * 0.7f) / (sampleCount * 0.3f));
                }
            }
            clip.SetData(samples, 0);
            
            source.clip = clip;
            source.Play();
            
            yield return new WaitForSeconds(duration + 0.1f);
            Destroy(buzzerObj);
        }

        public void StartBuzzTimer(System.Action onComplete = null)
        {
            StartBuzzTimer(buzzTimerDuration, onComplete);
        }

        public void StartBuzzTimer(float remainingTime, System.Action onComplete = null)
        {
            if (timerBarContainer == null)
            {
                CreateTimerBars();
            }

            buzzDuration = buzzTimerDuration;
            buzzRemainingTime = Mathf.Clamp(remainingTime, 0, buzzTimerDuration);
            onBuzzComplete = onComplete;
            isBuzzRunning = true;

            // Set fill to reflect remaining time
            float elapsed = buzzDuration - buzzRemainingTime;
            float progress = elapsed / buzzDuration;
            if (fillRect != null)
            {
                fillRect.anchorMax = new Vector2(progress, 1);
                float timeRatio = buzzRemainingTime / buzzDuration;
                fillImage.color = timeRatio <= warningThreshold ? warningColor : barColor;
            }

            timerBarContainer?.SetActive(true);
            Debug.Log($"[GameTimerDisplay] Buzz timer started: {buzzRemainingTime:F1}s remaining");
        }

        public void StopBuzzTimer()
        {
            isBuzzRunning = false;
            timerBarContainer?.SetActive(false);
        }

        public void StartResponseTimer(System.Action onComplete = null)
        {
            if (responseBarContainer == null)
            {
                CreateTimerBars();
            }

            responseDuration = responseTimerDuration;
            responseRemainingTime = responseTimerDuration;
            onResponseComplete = onComplete;
            isResponseRunning = true;

            // Show all segments
            foreach (var seg in responseSegments)
            {
                seg.gameObject.SetActive(true);
            }

            responseBarContainer?.SetActive(true);
            Debug.Log($"[GameTimerDisplay] Response timer started: {responseTimerDuration}s");
        }

        public void StopResponseTimer()
        {
            isResponseRunning = false;
            responseBarContainer?.SetActive(false);
        }

        public void StopAllTimers()
        {
            StopBuzzTimer();
            StopResponseTimer();
        }

        public float BuzzTimerDuration => buzzTimerDuration;
        public float ResponseTimerDuration => responseTimerDuration;
    }
}
