using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cerebrum.Core
{
    public static class SceneLoader
    {
        public const string HOME_SCENE = "Home";
        public const string GAME_SCENE = "Game";

        public static void LoadHome()
        {
            SceneManager.LoadScene(HOME_SCENE);
        }

        public static void LoadGame()
        {
            SceneManager.LoadScene(GAME_SCENE);
        }

        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
