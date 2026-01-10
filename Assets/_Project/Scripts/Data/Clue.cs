using System;

namespace Cerebrum.Data
{
    [Serializable]
    public class Clue
    {
        public string Question;
        public string Answer;
        public int Value;
        public bool Used;
        public string AirDate;
        public int Round;

        public Clue(string question, string answer, int value, int round = 1, string airDate = "")
        {
            Question = question;
            Answer = answer;
            Value = value;
            Round = round;
            AirDate = airDate;
            Used = false;
        }
    }
}
