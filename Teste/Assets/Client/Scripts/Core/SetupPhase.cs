using UnityEngine;

namespace L5RGame
{
    public class SetupPhase : MonoBehaviour, IGameStep
    {
        private Game game;
        private bool completed = false;

        public SetupPhase(Game game)
        {
            this.game = game;
        }

        public bool Execute()
        {
            // Execute setup phase logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
