using System;
using System.Collections.Generic;

namespace Cerebrum.Data
{
    [Serializable]
    public class Category
    {
        public string Title;
        public List<Clue> Clues;

        public Category(string title)
        {
            Title = title;
            Clues = new List<Clue>();
        }

        public bool HasMinimumClues(int required = 5)
        {
            return Clues.Count >= required;
        }

        public bool HasCluesForAllValues(int[] values)
        {
            foreach (int value in values)
            {
                bool found = false;
                foreach (var clue in Clues)
                {
                    if (clue.Value == value)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
