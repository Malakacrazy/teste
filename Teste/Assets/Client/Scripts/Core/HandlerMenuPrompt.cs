using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Handler menu prompt for more complex choices
    /// </summary>
    public partial class HandlerMenuPrompt : IGameStep
    {
        private Game game;
        private Player player;
        private HandlerMenuPromptProperties properties;
        
        public HandlerMenuPrompt(Game gameInstance, Player promptPlayer, HandlerMenuPromptProperties props)
        {
            game = gameInstance;
            player = promptPlayer;
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
            game.AddMessage("Waiting for {0} to select from available options...", player);
            return false;
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Find matching choice by command/arg
            var choice = properties.choices?.Find(c => c.ToString() == command);
            if (choice != null && properties.onSelect != null)
            {
                bool result = properties.onSelect(player, choice);
                if (result)
                {
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
