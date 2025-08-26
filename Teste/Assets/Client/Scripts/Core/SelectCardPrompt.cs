using UnityEngine;

namespace L5RGame
{
    public class SelectCardPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private Player player;
        private SelectCardPromptProperties properties;
        private bool completed = false;

        public SelectCardPrompt(Game game, Player player, SelectCardPromptProperties properties)
        {
            this.game = game;
            this.player = player;
            this.properties = properties;
        }

        public bool Execute()
        {
            // Execute select card prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
