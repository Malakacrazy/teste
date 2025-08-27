using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Menu prompt for simple choice selection
    /// </summary>
    public partial class MenuPrompt : IGameStep
    {
        private Game game;
        private Player player;
        private object contextObj;
        private MenuPromptProperties properties;
        
        public MenuPrompt(Game gameInstance, Player promptPlayer, object context, MenuPromptProperties props)
        {
            game = gameInstance;
            player = promptPlayer;
            contextObj = context;
            properties = props;
        }
        
        public bool Execute()
        {
            return Continue();
        }
        
        public bool IsComplete()
        {
            return true; // Complete after one interaction
        }
        
        public bool Continue()
        {
            // Display menu prompt
            game.AddMessage("Waiting for {0} to make a choice...", player);
            return false; // Wait for player input
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (properties.onSelect != null)
            {
                bool result = properties.onSelect(player, command);
                if (result)
                {
                    // Prompt completed successfully
                    game.pipeline.Continue();
                }
            }
        }
        
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }
    }
}
