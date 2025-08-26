using UnityEngine;

namespace L5RGame
{
    public class DrawPhase : MonoBehaviour, IGameStep
    {
        private Game game;
        private bool completed = false;

        public DrawPhase(Game game)
        {
            this.game = game;
        }

        public bool Execute()
        {
            // Execute draw phase logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
