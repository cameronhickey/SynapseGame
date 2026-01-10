using UnityEngine;
using Cerebrum.Data;
using Cerebrum.Game;
using Cerebrum.OpenAI;

namespace Cerebrum.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            CreatePersistentManagers();
        }

        private static void CreatePersistentManagers()
        {
            if (GameManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("[GameManager]");
                gameManagerObj.AddComponent<GameManager>();
                DontDestroyOnLoad(gameManagerObj);
            }

            if (OptimizedCategoryLoader.Instance == null)
            {
                GameObject loaderObj = new GameObject("[OptimizedCategoryLoader]");
                loaderObj.AddComponent<OptimizedCategoryLoader>();
                DontDestroyOnLoad(loaderObj);
            }

            if (OpenAIClient.Instance == null)
            {
                GameObject openAIObj = new GameObject("[OpenAIClient]");
                openAIObj.AddComponent<OpenAIClient>();
                DontDestroyOnLoad(openAIObj);
            }

            if (TTSService.Instance == null)
            {
                GameObject ttsObj = new GameObject("[TTSService]");
                ttsObj.AddComponent<TTSService>();
                DontDestroyOnLoad(ttsObj);
            }

            if (TTSCache.Instance == null)
            {
                GameObject cacheObj = new GameObject("[TTSCache]");
                cacheObj.AddComponent<TTSCache>();
                DontDestroyOnLoad(cacheObj);
            }

            if (MicrophoneRecorder.Instance == null)
            {
                GameObject micObj = new GameObject("[MicrophoneRecorder]");
                micObj.AddComponent<MicrophoneRecorder>();
                DontDestroyOnLoad(micObj);
            }

            if (STTService.Instance == null)
            {
                GameObject sttObj = new GameObject("[STTService]");
                sttObj.AddComponent<STTService>();
                DontDestroyOnLoad(sttObj);
            }

            if (AnswerJudge.Instance == null)
            {
                GameObject judgeObj = new GameObject("[AnswerJudge]");
                judgeObj.AddComponent<AnswerJudge>();
                DontDestroyOnLoad(judgeObj);
            }
        }
    }
}
