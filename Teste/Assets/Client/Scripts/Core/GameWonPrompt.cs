using UnityEngine;

namespace L5RGame
{
    public class GameWonPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private Player winner;
        private bool completed = false;

        public GameWonPrompt(Game game, Player winner)
        {
            this.game = game;
            this.winner = winner;
        }

        public bool Execute()
        {
            // Execute game won prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
