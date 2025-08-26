using UnityEngine;

namespace L5RGame
{
    public class SelectRingPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private Player player;
        private SelectRingPromptProperties properties;
        private bool completed = false;

        public SelectRingPrompt(Game game, Player player, SelectRingPromptProperties properties)
        {
            this.game = game;
            this.player = player;
            this.properties = properties;
        }

        public bool Execute()
        {
            // Execute select ring prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
