using UnityEngine;

namespace L5RGame
{
    public class EndRoundPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private bool completed = false;

        public EndRoundPrompt(Game game)
        {
            this.game = game;
        }

        public bool Execute()
        {
            // Execute end round prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
