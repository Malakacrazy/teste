using UnityEngine;

namespace L5RGame
{
    public class ConflictPhase : MonoBehaviour, IGameStep
    {
        private Game game;
        private bool completed = false;

        public ConflictPhase(Game game)
        {
            this.game = game;
        }

        public bool Execute()
        {
            // Execute conflict phase logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
