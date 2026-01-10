using System;
using UnityEngine;

namespace Cerebrum.OpenAI
{
    [Serializable]
    public class JudgeResult
    {
        public bool IsCorrect;
        public string Rationale;
        public string AcceptedAnswer;

        public static JudgeResult Correct(string rationale = "", string acceptedAnswer = "")
        {
            return new JudgeResult
            {
                IsCorrect = true,
                Rationale = rationale,
                AcceptedAnswer = acceptedAnswer
            };
        }

        public static JudgeResult Incorrect(string rationale = "")
        {
            return new JudgeResult
            {
                IsCorrect = false,
                Rationale = rationale,
                AcceptedAnswer = ""
            };
        }
    }

    public class AnswerJudge : MonoBehaviour
    {
        public static AnswerJudge Instance { get; private set; }

        public bool IsJudging { get; private set; }
        public event Action<JudgeResult> OnJudgmentComplete;

        private const string JUDGE_SYSTEM_PROMPT = @"You are a Jeopardy answer judge. Compare the player's response to the correct answer.

Rules:
- Accept responses that are substantially correct, even with minor spelling errors or alternate phrasings
- Accept common nicknames or shortened versions of names
- Accept singular/plural variations
- The player doesn't need to phrase as a question (ignore 'What is' or 'Who is' prefixes)
- Be lenient but fair - the core answer must be correct

Respond with ONLY a JSON object in this exact format:
{""correct"": true/false, ""rationale"": ""brief explanation"", ""accepted_answer"": ""the answer you accepted (if correct)""}";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Judge(string playerResponse, string correctAnswer, Action<JudgeResult> onComplete = null)
        {
            if (string.IsNullOrEmpty(playerResponse))
            {
                var result = JudgeResult.Incorrect("No response given");
                onComplete?.Invoke(result);
                OnJudgmentComplete?.Invoke(result);
                return;
            }

            if (OpenAIClient.Instance == null || !OpenAIClient.Instance.IsConfigured)
            {
                var result = FallbackJudge(playerResponse, correctAnswer);
                onComplete?.Invoke(result);
                OnJudgmentComplete?.Invoke(result);
                return;
            }

            IsJudging = true;
            string userMessage = $"Correct answer: \"{correctAnswer}\"\nPlayer's response: \"{playerResponse}\"";

            Debug.Log($"[AnswerJudge] Judging: \"{playerResponse}\" vs \"{correctAnswer}\"");

            OpenAIClient.Instance.PostChat(JUDGE_SYSTEM_PROMPT, userMessage,
                (response) =>
                {
                    IsJudging = false;
                    var result = ParseJudgeResponse(response, correctAnswer);
                    Debug.Log($"[AnswerJudge] Result: {(result.IsCorrect ? "CORRECT" : "INCORRECT")} - {result.Rationale}");
                    onComplete?.Invoke(result);
                    OnJudgmentComplete?.Invoke(result);
                },
                (error) =>
                {
                    IsJudging = false;
                    Debug.LogError($"[AnswerJudge] API Error: {error}. Using fallback.");
                    var result = FallbackJudge(playerResponse, correctAnswer);
                    onComplete?.Invoke(result);
                    OnJudgmentComplete?.Invoke(result);
                }
            );
        }

        private JudgeResult ParseJudgeResponse(string response, string correctAnswer)
        {
            try
            {
                // Clean up response - sometimes the model wraps in markdown code blocks
                response = response.Trim();
                if (response.StartsWith("```"))
                {
                    int start = response.IndexOf('{');
                    int end = response.LastIndexOf('}');
                    if (start >= 0 && end > start)
                    {
                        response = response.Substring(start, end - start + 1);
                    }
                }

                var parsed = JsonUtility.FromJson<JudgeResponseJson>(response);
                
                return new JudgeResult
                {
                    IsCorrect = parsed.correct,
                    Rationale = parsed.rationale ?? "",
                    AcceptedAnswer = parsed.accepted_answer ?? ""
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AnswerJudge] Failed to parse response: {e.Message}. Response: {response}");
                return JudgeResult.Incorrect("Could not parse judge response");
            }
        }

        private JudgeResult FallbackJudge(string playerResponse, string correctAnswer)
        {
            // Simple fallback: case-insensitive contains check
            string normalizedPlayer = NormalizeAnswer(playerResponse);
            string normalizedCorrect = NormalizeAnswer(correctAnswer);

            bool isCorrect = normalizedPlayer.Contains(normalizedCorrect) || 
                            normalizedCorrect.Contains(normalizedPlayer);

            if (isCorrect)
            {
                return JudgeResult.Correct("Fallback judge: answers match", playerResponse);
            }
            else
            {
                return JudgeResult.Incorrect("Fallback judge: answers don't match");
            }
        }

        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrEmpty(answer)) return "";
            
            // Remove common prefixes
            answer = answer.ToLowerInvariant().Trim();
            string[] prefixes = { "what is ", "what are ", "who is ", "who are ", "where is ", "a ", "an ", "the " };
            
            foreach (var prefix in prefixes)
            {
                if (answer.StartsWith(prefix))
                {
                    answer = answer.Substring(prefix.Length);
                    break;
                }
            }

            // Remove punctuation
            answer = System.Text.RegularExpressions.Regex.Replace(answer, @"[^\w\s]", "");
            
            return answer.Trim();
        }

        [Serializable]
        private class JudgeResponseJson
        {
            public bool correct;
            public string rationale;
            public string accepted_answer;
        }
    }
}
