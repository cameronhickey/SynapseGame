using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Cerebrum.UI
{
    public class DebugConsole : MonoBehaviour
    {
        public static DebugConsole Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject consolePanel;
        [SerializeField] private TextMeshProUGUI logText;

        [Header("Settings")]
        [SerializeField] private int maxLines = 100;
        [SerializeField] private bool showOnStart = false; // Hidden by default, press ` to toggle

        private Queue<string> logLines = new Queue<string>();
        private bool isVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.logMessageReceived += HandleLog;
        }

        private void Start()
        {
            if (consolePanel == null)
            {
                CreateConsoleUI();
            }

            isVisible = showOnStart;
            consolePanel.SetActive(isVisible);
            
            Log("<color=cyan>[DebugConsole]</color> Initialized. Press ` (backtick) to toggle.");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Toggle console with backtick or F1 key
            if (keyboard.backquoteKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame)
            {
                ToggleConsole();
            }
        }

        private void CreateConsoleUI()
        {
            // Create canvas
            GameObject canvasObj = new GameObject("DebugCanvas");
            canvasObj.transform.SetParent(transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create panel - simple dark background at top
            consolePanel = new GameObject("ConsolePanel");
            consolePanel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = consolePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.7f);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = consolePanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.9f);

            // Create text directly on panel - simple approach
            GameObject textObj = new GameObject("LogText");
            textObj.transform.SetParent(consolePanel.transform, false);
            
            logText = textObj.AddComponent<TextMeshProUGUI>();
            logText.fontSize = 16;
            logText.color = Color.white;
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.overflowMode = TextOverflowModes.Truncate;
            logText.richText = true;
            
            // Get font
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (font != null)
            {
                logText.font = font;
            }
            
            logText.text = "=== Debug Console ===\nWaiting for logs...";

            RectTransform textRect = logText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error => "[ERR]",
                LogType.Exception => "[EXC]",
                LogType.Warning => "[WRN]",
                _ => ""
            };

            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string formattedLog = $"[{timestamp}]{prefix} {logString}";
            
            logLines.Enqueue(formattedLog);
            
            while (logLines.Count > maxLines)
            {
                logLines.Dequeue();
            }

            if (logText != null)
            {
                logText.text = string.Join("\n", logLines);
            }
        }

        public void ToggleConsole()
        {
            isVisible = !isVisible;
            consolePanel?.SetActive(isVisible);
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }
    }
}
