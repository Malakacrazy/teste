using UnityEngine;

namespace L5RGame
{
    public class DynastyPhase : MonoBehaviour, IGameStep
    {
        private Game game;
        private bool completed = false;

        public DynastyPhase(Game game)
        {
            this.game = game;
        }

        public bool Execute()
        {
            // Execute dynasty phase logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
