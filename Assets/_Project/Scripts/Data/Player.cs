using System;

namespace Cerebrum.Data
{
    [Serializable]
    public class Player
    {
        public string Name;
        public int Score;

        public Player(string name)
        {
            Name = name;
            Score = 0;
        }

        public void AddScore(int points)
        {
            Score += points;
        }

        public void SubtractScore(int points)
        {
            Score -= points;
        }
    }
}
