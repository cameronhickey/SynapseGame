using System.Collections.Generic;
using UnityEngine;

namespace Cerebrum.Data
{
    public class CategorySelector : MonoBehaviour
    {
        private static readonly int[] BOARD_VALUES = { 200, 400, 600, 800, 1000 };
        private const int CATEGORIES_PER_GAME = 6;

        public Board SelectRandomBoard()
        {
            if (JeopardyDataLoader.Instance == null)
            {
                Debug.LogError("[CategorySelector] JeopardyDataLoader not found!");
                return null;
            }

            List<Category> validCategories = JeopardyDataLoader.Instance.GetValidCategories();

            if (validCategories.Count < CATEGORIES_PER_GAME)
            {
                Debug.LogError($"[CategorySelector] Not enough valid categories! Have {validCategories.Count}, need {CATEGORIES_PER_GAME}");
                return null;
            }

            List<Category> shuffled = new List<Category>(validCategories);
            ShuffleList(shuffled);

            Board board = new Board();
            HashSet<string> usedTitles = new HashSet<string>();

            for (int i = 0; i < shuffled.Count && board.Categories.Count < CATEGORIES_PER_GAME; i++)
            {
                Category sourceCategory = shuffled[i];

                if (usedTitles.Contains(sourceCategory.Title))
                    continue;

                Category boardCategory = CreateBoardCategory(sourceCategory);

                if (boardCategory != null)
                {
                    board.Categories.Add(boardCategory);
                    usedTitles.Add(sourceCategory.Title);
                }
            }

            if (board.Categories.Count < CATEGORIES_PER_GAME)
            {
                Debug.LogError($"[CategorySelector] Could only select {board.Categories.Count} categories!");
                return null;
            }

            Debug.Log($"[CategorySelector] Created board with categories: {string.Join(", ", usedTitles)}");

            return board;
        }

        private Category CreateBoardCategory(Category source)
        {
            Category boardCategory = new Category(source.Title);

            Dictionary<int, List<Clue>> byValue = new Dictionary<int, List<Clue>>();
            foreach (int v in BOARD_VALUES)
            {
                byValue[v] = new List<Clue>();
            }

            foreach (var clue in source.Clues)
            {
                if (byValue.ContainsKey(clue.Value))
                {
                    byValue[clue.Value].Add(clue);
                }
            }

            foreach (int value in BOARD_VALUES)
            {
                List<Clue> cluesForValue = byValue[value];

                if (cluesForValue.Count == 0)
                {
                    return null;
                }

                int randomIndex = Random.Range(0, cluesForValue.Count);
                Clue selectedClue = cluesForValue[randomIndex];

                Clue boardClue = new Clue(
                    selectedClue.Question,
                    selectedClue.Answer,
                    value,
                    selectedClue.Round,
                    selectedClue.AirDate
                );

                boardCategory.Clues.Add(boardClue);
            }

            return boardCategory;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
