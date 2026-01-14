using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cerebrum.Core
{
    public static class SceneLoader
    {
        public const string HOME_SCENE = "Home";
        public const string GAME_SCENE = "Game";
        public const string GAME_3D_SCENE = "Game3D";

        public static void LoadHome()
        {
            SceneManager.LoadScene(HOME_SCENE);
        }

        public static void LoadGame()
        {
            SceneManager.LoadScene(GAME_SCENE);
        }

        public static void LoadGame3D()
        {
            SceneManager.LoadScene(GAME_3D_SCENE);
        }

        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
