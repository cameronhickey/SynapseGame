using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Game;
using Cerebrum.OpenAI;
using Cerebrum.Data;
using Cerebrum.Core;

namespace Cerebrum.UI
{
    public class GameWinnerUI : MonoBehaviour
    {
        public static GameWinnerUI Instance { get; private set; }
        
        private Canvas winnerCanvas;
        private GameObject overlayPanel;
        private TextMeshProUGUI winnerText;
        private TextMeshProUGUI scoreText;
        private Button playAgainButton;
        private List<GameObject> confettiParticles = new List<GameObject>();
        private bool isShowing;
        
        // Confetti colors
        private static readonly Color[] confettiColors = new Color[]
        {
            new Color(1f, 0.84f, 0f),      // Gold
            new Color(0.3f, 0.7f, 1f),     // Light Blue
            new Color(1f, 0.4f, 0.4f),     // Light Red
            new Color(0.4f, 1f, 0.4f),     // Light Green
            new Color(1f, 0.6f, 1f),       // Pink
            new Color(1f, 0.8f, 0.4f),     // Orange
            new Color(0.8f, 0.6f, 1f),     // Purple
        };
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        public void ShowWinner()
        {
            if (isShowing) return;
            if (GameManager.Instance == null || GameManager.Instance.Players == null) return;
            
            isShowing = true;
            
            // Find the winner
            Player winner = null;
            int highestScore = int.MinValue;
            bool isTie = false;
            
            foreach (var player in GameManager.Instance.Players)
            {
                if (player.Score > highestScore)
                {
                    highestScore = player.Score;
                    winner = player;
                    isTie = false;
                }
                else if (player.Score == highestScore)
                {
                    isTie = true;
                }
            }
            
            if (winner == null) return;
            
            CreateWinnerUI(winner, isTie);
            StartCoroutine(WinnerSequence(winner, isTie));
        }
        
        private void CreateWinnerUI(Player winner, bool isTie)
        {
            // Create canvas
            GameObject canvasObj = new GameObject("WinnerCanvas");
            winnerCanvas = canvasObj.AddComponent<Canvas>();
            winnerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            winnerCanvas.sortingOrder = 200;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Dark overlay
            overlayPanel = new GameObject("Overlay");
            overlayPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform overlayRect = overlayPanel.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            
            Image overlayImage = overlayPanel.AddComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.02f, 0.1f, 0f); // Start transparent
            
            // Winner text container
            GameObject textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(overlayPanel.transform, false);
            RectTransform textRect = textContainer.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.4f);
            textRect.anchorMax = new Vector2(0.9f, 0.75f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup layout = textContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            
            // "WINNER" header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(textContainer.transform, false);
            TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.text = isTie ? "IT'S A TIE!" : "WINNER!";
            headerText.fontSize = 72;
            headerText.color = new Color(1f, 0.84f, 0f); // Gold
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontStyle = FontStyles.Bold;
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(800, 100);
            
            // Winner name
            GameObject nameObj = new GameObject("WinnerName");
            nameObj.transform.SetParent(textContainer.transform, false);
            winnerText = nameObj.AddComponent<TextMeshProUGUI>();
            winnerText.text = isTie ? "Great Game!" : $"{winner.Name} Wins!";
            winnerText.fontSize = 96;
            winnerText.color = Color.white;
            winnerText.alignment = TextAlignmentOptions.Center;
            winnerText.fontStyle = FontStyles.Bold;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(1000, 130);
            
            // Add glow effect
            Outline glow = nameObj.AddComponent<Outline>();
            glow.effectColor = new Color(1f, 0.84f, 0f, 0.5f);
            glow.effectDistance = new Vector2(4, 4);
            
            // Score display
            GameObject scoreObj = new GameObject("Score");
            scoreObj.transform.SetParent(textContainer.transform, false);
            scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = $"Final Score: ${winner.Score:N0}";
            scoreText.fontSize = 48;
            scoreText.color = new Color(0.8f, 0.9f, 1f);
            scoreText.alignment = TextAlignmentOptions.Center;
            RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.sizeDelta = new Vector2(600, 70);
            
            // Play Again button
            GameObject buttonObj = new GameObject("PlayAgainButton");
            buttonObj.transform.SetParent(overlayPanel.transform, false);
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.35f, 0.15f);
            buttonRect.anchorMax = new Vector2(0.65f, 0.25f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            
            Outline buttonOutline = buttonObj.AddComponent<Outline>();
            buttonOutline.effectColor = new Color(0.4f, 0.7f, 1f, 0.8f);
            buttonOutline.effectDistance = new Vector2(3, 3);
            
            playAgainButton = buttonObj.AddComponent<Button>();
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            
            // Button text
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "PLAY AGAIN";
            buttonText.fontSize = 36;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;
            
            // Hide button initially
            buttonObj.SetActive(false);
            
            // Start transparent
            CanvasGroup canvasGroup = overlayPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }
        
        private IEnumerator WinnerSequence(Player winner, bool isTie)
        {
            // Fade in overlay
            CanvasGroup canvasGroup = overlayPanel.GetComponent<CanvasGroup>();
            Image overlayImage = overlayPanel.GetComponent<Image>();
            
            float fadeTime = 1f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                canvasGroup.alpha = t;
                overlayImage.color = new Color(0.02f, 0.02f, 0.1f, t * 0.9f);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            
            // Start confetti
            StartCoroutine(SpawnConfetti());
            
            // Play winner announcement
            yield return new WaitForSeconds(0.5f);
            
            if (PhrasePlayer.Instance != null && !isTie)
            {
                bool announcementComplete = false;
                
                // Try to play the integrated winner phrase
                var winnerPhrase = GamePhrases.IntegratedNamePhrases.Find(p => p.Id == "winner_int_1");
                if (winnerPhrase != null)
                {
                    PhrasePlayer.Instance.PlayPhraseWithName(winnerPhrase, winner.Name, () => announcementComplete = true);
                }
                else
                {
                    // Fallback to generic
                    PhrasePlayer.Instance.PlayPhrase("congratulations", () => announcementComplete = true);
                }
                
                // Wait for announcement
                float timeout = 5f;
                while (!announcementComplete && timeout > 0)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }
            
            yield return new WaitForSeconds(1f);
            
            // Show play again button
            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(true);
            }
        }
        
        private IEnumerator SpawnConfetti()
        {
            float duration = 8f;
            float elapsed = 0f;
            float spawnInterval = 0.05f;
            float nextSpawn = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                
                if (elapsed >= nextSpawn)
                {
                    SpawnConfettiPiece();
                    nextSpawn = elapsed + spawnInterval;
                }
                
                yield return null;
            }
        }
        
        private void SpawnConfettiPiece()
        {
            if (winnerCanvas == null) return;
            
            GameObject confetti = new GameObject("Confetti");
            confetti.transform.SetParent(winnerCanvas.transform, false);
            
            RectTransform rect = confetti.AddComponent<RectTransform>();
            
            // Random starting position at top
            float startX = UnityEngine.Random.Range(-Screen.width * 0.5f, Screen.width * 0.5f);
            rect.anchoredPosition = new Vector2(startX, Screen.height * 0.6f);
            
            // Random size
            float size = UnityEngine.Random.Range(10f, 25f);
            rect.sizeDelta = new Vector2(size, size * UnityEngine.Random.Range(0.5f, 1.5f));
            
            // Random color
            Image img = confetti.AddComponent<Image>();
            img.color = confettiColors[UnityEngine.Random.Range(0, confettiColors.Length)];
            
            // Random rotation
            rect.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
            
            confettiParticles.Add(confetti);
            StartCoroutine(AnimateConfetti(confetti, rect));
        }
        
        private IEnumerator AnimateConfetti(GameObject confetti, RectTransform rect)
        {
            float fallSpeed = UnityEngine.Random.Range(200f, 400f);
            float swaySpeed = UnityEngine.Random.Range(2f, 5f);
            float swayAmount = UnityEngine.Random.Range(50f, 150f);
            float rotationSpeed = UnityEngine.Random.Range(100f, 300f);
            float startX = rect.anchoredPosition.x;
            float elapsed = 0f;
            
            while (confetti != null && rect.anchoredPosition.y > -Screen.height * 0.6f)
            {
                elapsed += Time.deltaTime;
                
                float newY = rect.anchoredPosition.y - fallSpeed * Time.deltaTime;
                float newX = startX + Mathf.Sin(elapsed * swaySpeed) * swayAmount;
                
                rect.anchoredPosition = new Vector2(newX, newY);
                rect.Rotate(0, 0, rotationSpeed * Time.deltaTime);
                
                yield return null;
            }
            
            if (confetti != null)
            {
                confettiParticles.Remove(confetti);
                Destroy(confetti);
            }
        }
        
        private void OnPlayAgainClicked()
        {
            Debug.Log("[GameWinnerUI] Play Again clicked");
            
            // Clean up
            CleanUp();
            
            // Reset game state
            if (GameManager.Instance != null)
            {
                foreach (var player in GameManager.Instance.Players)
                {
                    player.Score = 0;
                }
                GameManager.Instance.CurrentChooserIndex = 0;
                GameManager.Instance.CurrentBoard = null;
            }
            
            // Return to home and start new game
            SceneLoader.LoadHome();
        }
        
        private void CleanUp()
        {
            isShowing = false;
            
            // Stop all confetti coroutines
            StopAllCoroutines();
            
            // Destroy confetti
            foreach (var confetti in confettiParticles)
            {
                if (confetti != null) Destroy(confetti);
            }
            confettiParticles.Clear();
            
            // Destroy canvas
            if (winnerCanvas != null)
            {
                Destroy(winnerCanvas.gameObject);
            }
        }
        
        private void OnDestroy()
        {
            CleanUp();
            if (Instance == this) Instance = null;
        }
    }
}
