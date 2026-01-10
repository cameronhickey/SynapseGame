using UnityEngine;
using UnityEngine.UI;
using Cerebrum.Core;

namespace Cerebrum.UI
{
    public class HomeView : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;

        private void Start()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }
        }

        private void OnStartGameClicked()
        {
            Debug.Log("[HomeView] Starting game...");
            SceneLoader.LoadGame();
        }

        private void OnDestroy()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameClicked);
            }
        }
    }
}
