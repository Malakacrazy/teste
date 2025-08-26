using UnityEngine;

namespace L5RGame
{
    public class ConflictFlow : MonoBehaviour, IGameStep
    {
        private Game game;
        private Conflict conflict;
        private bool canPass;
        private bool completed = false;

        public ConflictFlow(Game game, Conflict conflict, bool canPass)
        {
            this.game = game;
            this.conflict = conflict;
            this.canPass = canPass;
        }

        public bool Execute()
        {
            // Execute conflict flow logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
