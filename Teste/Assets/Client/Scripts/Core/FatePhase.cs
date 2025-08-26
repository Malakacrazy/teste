using UnityEngine;

namespace L5RGame
{
    public class FatePhase : MonoBehaviour, IGameStep
    {
        private Game game;
        private bool completed = false;

        public FatePhase(Game game)
        {
            this.game = game;
        }

        public bool Execute()
        {
            // Execute fate phase logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
