using UnityEngine;

namespace L5RGame
{
    public class MenuPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private Player player;
        private object contextObj;
        private MenuPromptProperties properties;
        private bool completed = false;

        public MenuPrompt(Game game, Player player, object contextObj, MenuPromptProperties properties)
        {
            this.game = game;
            this.player = player;
            this.contextObj = contextObj;
            this.properties = properties;
        }

        public bool Execute()
        {
            // Execute menu prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
