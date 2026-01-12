using System.Collections.Generic;
using UnityEngine;
using Cerebrum.Core;
using Cerebrum.Data;

namespace Cerebrum.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Players")]
        public List<Player> Players;
        public int CurrentChooserIndex;

        [Header("Game State")]
        public GameState CurrentState;
        public Board CurrentBoard;
        public Clue ActiveClue;
        public int ActiveCategoryIndex;
        public int ActiveRowIndex;
        public bool IsTestMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePlayers();
        }

        private void InitializePlayers()
        {
            Players = new List<Player>
            {
                new Player("Cameron"),
                new Player("Calder"),
                new Player("Lauren")
            };
            CurrentChooserIndex = 0;
        }

        public Player GetCurrentChooser()
        {
            return Players[CurrentChooserIndex];
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        public void SetActiveClue(Clue clue, int categoryIndex, int rowIndex)
        {
            ActiveClue = clue;
            ActiveCategoryIndex = categoryIndex;
            ActiveRowIndex = rowIndex;
            SetState(GameState.ClueMode);
        }

        public void ClearActiveClue()
        {
            if (ActiveClue != null)
            {
                ActiveClue.Used = true;
            }
            ActiveClue = null;
            SetState(GameState.BoardMode);
        }

        public void RotateChooser()
        {
            CurrentChooserIndex = (CurrentChooserIndex + 1) % Players.Count;
            Debug.Log($"[GameManager] New chooser: {GetCurrentChooser().Name}");
        }

        public void AwardPoints(int playerIndex, int points)
        {
            if (playerIndex >= 0 && playerIndex < Players.Count)
            {
                Players[playerIndex].AddScore(points);
                CurrentChooserIndex = playerIndex;
            }
        }

        public void DeductPoints(int playerIndex, int points)
        {
            if (playerIndex >= 0 && playerIndex < Players.Count)
            {
                Players[playerIndex].SubtractScore(points);
            }
        }

        public void SetBoard(Board board)
        {
            CurrentBoard = board;
        }

        public void SetCurrentBoard(Board board)
        {
            CurrentBoard = board;
        }

        public void SetPlayerNames(List<string> names)
        {
            if (names == null || names.Count == 0) return;
            
            Players.Clear();
            foreach (string name in names)
            {
                Players.Add(new Player(name));
            }
            CurrentChooserIndex = 0;
            Debug.Log($"[GameManager] Players set: {string.Join(", ", names)}");
        }

        public bool IsRoundComplete()
        {
            return CurrentBoard != null && CurrentBoard.AllCluesUsed();
        }
    }
}
