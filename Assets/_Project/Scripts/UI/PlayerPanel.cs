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

        private Color normalColor = new Color(0.1f, 0.1f, 0.2f);
        private Color chooserColor = new Color(0.2f, 0.15f, 0.4f);

        public void SetPlayer(Player player, bool isChooser)
        {
            if (nameText != null)
            {
                nameText.text = player.Name;
            }

            UpdateScore(player.Score);
            SetChooserState(isChooser);
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                string prefix = score < 0 ? "-$" : "$";
                scoreText.text = prefix + Mathf.Abs(score).ToString();
            }
        }

        public void SetChooserState(bool isChooser)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isChooser ? chooserColor : normalColor;
            }

            if (chooserIndicator != null)
            {
                chooserIndicator.gameObject.SetActive(isChooser);
            }
        }
    }
}
