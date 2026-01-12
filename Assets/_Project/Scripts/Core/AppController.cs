using UnityEngine;
using UnityEngine.InputSystem;

namespace Cerebrum.Core
{
    public class AppController : MonoBehaviour
    {
        public static AppController Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool startWindowed = true;
        [SerializeField] private int windowedWidth = 1280;
        [SerializeField] private int windowedHeight = 720;

        private bool isFullscreen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set initial window mode
            if (startWindowed)
            {
                SetWindowed();
            }

            isFullscreen = Screen.fullScreen;
            Debug.Log($"[AppController] Initialized. Fullscreen: {isFullscreen}. Press Escape to toggle fullscreen, Cmd+Q to quit.");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Escape - toggle fullscreen
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ToggleFullscreen();
            }

            // Cmd+Q (Mac) or Ctrl+Q - Quit
            bool cmdOrCtrl = keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed ||
                            keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            
            if (cmdOrCtrl && keyboard.qKey.wasPressedThisFrame)
            {
                QuitApp();
            }

            // Alt+F4 for Windows
            if ((keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed) && keyboard.f4Key.wasPressedThisFrame)
            {
                QuitApp();
            }

            // Cmd+F or F11 - toggle fullscreen
            if ((cmdOrCtrl && keyboard.fKey.wasPressedThisFrame) || keyboard.f11Key.wasPressedThisFrame)
            {
                ToggleFullscreen();
            }
        }

        public void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            
            if (isFullscreen)
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
                Debug.Log("[AppController] Switched to fullscreen");
            }
            else
            {
                SetWindowed();
            }
        }

        private void SetWindowed()
        {
            Screen.SetResolution(windowedWidth, windowedHeight, FullScreenMode.Windowed);
            Debug.Log($"[AppController] Switched to windowed mode ({windowedWidth}x{windowedHeight})");
        }

        public void QuitApp()
        {
            Debug.Log("[AppController] Quitting application...");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
