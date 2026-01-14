using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cerebrum.Data;

namespace Cerebrum.UI
{
    public class PlayerPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image chooserIndicator;

        private Outline borderOutline;
        private int playerIndex;
        
        // Player border colors (cyan, orange, magenta, green)
        private static readonly Color[] playerColors = new Color[]
        {
            new Color(0.3f, 0.8f, 1f, 0.9f),   // Cyan
            new Color(1f, 0.6f, 0.2f, 0.9f),   // Orange
            new Color(0.9f, 0.4f, 0.9f, 0.9f), // Magenta
            new Color(0.4f, 0.9f, 0.5f, 0.9f)  // Green
        };

        private void Awake()
        {
            borderOutline = GetComponent<Outline>();
        }

        public void SetPlayer(Player player, bool isChooser, int index = -1)
        {
            if (index >= 0)
            {
                playerIndex = index;
                SetBorderColor(index);
            }

            if (nameText != null)
            {
                nameText.text = player.Name.ToUpper();
            }

            UpdateScore(player.Score);
            SetChooserState(isChooser);
        }

        public void SetPlayer(Player player, bool isChooser)
        {
            SetPlayer(player, isChooser, -1);
        }

        private void SetBorderColor(int index)
        {
            if (borderOutline != null)
            {
                Color borderColor = playerColors[index % playerColors.Length];
                borderOutline.effectColor = borderColor;
            }
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                string prefix = score < 0 ? "-$" : "$";
                scoreText.text = prefix + Mathf.Abs(score).ToString("N0");
            }
        }

        public void SetChooserState(bool isChooser)
        {
            if (chooserIndicator != null)
            {
                chooserIndicator.gameObject.SetActive(isChooser);
            }
            
            // Brighten border when chooser
            if (borderOutline != null)
            {
                Color baseColor = playerColors[playerIndex % playerColors.Length];
                borderOutline.effectColor = isChooser 
                    ? new Color(baseColor.r * 1.2f, baseColor.g * 1.2f, baseColor.b * 1.2f, 1f)
                    : baseColor;
            }
        }
    }
}
