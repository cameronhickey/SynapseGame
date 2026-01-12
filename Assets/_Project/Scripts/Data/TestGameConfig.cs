using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cerebrum.Data
{
    [CreateAssetMenu(fileName = "TestGameConfig", menuName = "Cerebrum/Test Game Config")]
    public class TestGameConfig : ScriptableObject
    {
        [Header("Test Players")]
        public string[] playerNames = { "Ken", "Amy", "Alex" };

        [Header("Test Categories")]
        public List<TestCategory> categories = new List<TestCategory>();

        [Header("Audio Paths")]
        public string audioBasePath = "Audio/TestGame";

        public bool IsConfigured => categories.Count == 6 && categories.TrueForAll(c => c.clues.Count == 5);

        public Board CreateTestBoard()
        {
            Board board = new Board();

            foreach (var testCat in categories)
            {
                Category category = new Category(testCat.title);
                foreach (var testClue in testCat.clues)
                {
                    category.Clues.Add(new Clue(testClue.question, testClue.answer, testClue.value));
                }
                board.Categories.Add(category);
            }

            return board;
        }

        public string GetClueAudioPath(int categoryIndex, int clueIndex)
        {
            // Resources.Load doesn't need the extension
            return $"{audioBasePath}/Clues/cat{categoryIndex}_clue{clueIndex}";
        }

        public string GetAnswerAudioPath(int categoryIndex, int clueIndex)
        {
            return $"{audioBasePath}/Answers/cat{categoryIndex}_answer{clueIndex}";
        }

        public string GetCategoryAudioPath(int categoryIndex)
        {
            return $"{audioBasePath}/Categories/cat{categoryIndex}";
        }

        public string GetPlayerPhraseAudioPath(string phraseId, int playerIndex)
        {
            return $"{audioBasePath}/PlayerPhrases/{phraseId}_player{playerIndex}";
        }
    }

    [Serializable]
    public class TestCategory
    {
        public string title;
        public List<TestClue> clues = new List<TestClue>();
    }

    [Serializable]
    public class TestClue
    {
        public int value;
        public string question;
        public string answer;
    }
}
