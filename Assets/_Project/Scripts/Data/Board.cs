using System;
using System.Collections.Generic;

namespace Cerebrum.Data
{
    [Serializable]
    public class Board
    {
        public List<Category> Categories;
        public static readonly int[] RowValues = { 200, 400, 600, 800, 1000 };

        public Board()
        {
            Categories = new List<Category>(6);
        }

        public Clue GetClue(int categoryIndex, int rowIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= Categories.Count)
                return null;

            var category = Categories[categoryIndex];
            int targetValue = RowValues[rowIndex];

            foreach (var clue in category.Clues)
            {
                if (clue.Value == targetValue && !clue.Used)
                    return clue;
            }

            return null;
        }

        public bool AllCluesUsed()
        {
            foreach (var category in Categories)
            {
                foreach (var clue in category.Clues)
                {
                    if (!clue.Used)
                        return false;
                }
            }
            return true;
        }
    }
}
