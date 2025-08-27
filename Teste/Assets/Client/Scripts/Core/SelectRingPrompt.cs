using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Ring selection prompt
    /// </summary>
    public partial class SelectRingPrompt : IGameStep
    {
        private Game game;
        private Player player;
        private SelectRingPromptProperties properties;
        private bool selectionComplete = false;
        
        public SelectRingPrompt(Game gameInstance, Player promptPlayer, SelectRingPromptProperties props)
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
            return selectionComplete;
        }
        
        public bool Continue()
        {
            game.AddMessage("Waiting for {0} to select a ring...", player);
            
            // Set selectable rings based on condition
            var selectableRings = new List<Ring>();
            foreach (var ring in game.GetRings())
            {
                if (properties.ringCondition?.Invoke(ring) ?? true)
                {
                    selectableRings.Add(ring);
                }
            }
            
            player.SetSelectableRings(selectableRings);
            return false;
        }
        
        public void OnRingClicked(Player clickingPlayer, Ring ring)
        {
            if (clickingPlayer != player) return;
            
            if (properties.ringCondition?.Invoke(ring) ?? true)
            {
                if (properties.onSelect != null)
                {
                    bool result = properties.onSelect(player, ring);
                    if (result)
                    {
                        selectionComplete = true;
                        player.ClearSelectableRings();
                        game.pipeline.Continue();
                    }
                }
            }
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (command == "pass" && properties.optional)
            {
                selectionComplete = true;
                player.ClearSelectableRings();
                game.pipeline.Continue();
            }
        }
        
        public void OnCardClicked(Player player, BaseCard card) { }
        public void Initialize() { }
        public void Cleanup() 
        {
            player?.ClearSelectableRings();
        }
    }
}
