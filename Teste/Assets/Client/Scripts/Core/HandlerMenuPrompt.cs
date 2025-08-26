using UnityEngine;

namespace L5RGame
{
    public class HandlerMenuPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private Player player;
        private HandlerMenuPromptProperties properties;
        private bool completed = false;

        public HandlerMenuPrompt(Game game, Player player, HandlerMenuPromptProperties properties)
        {
            this.game = game;
            this.player = player;
            this.properties = properties;
        }

        public bool Execute()
        {
            // Execute handler menu prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
