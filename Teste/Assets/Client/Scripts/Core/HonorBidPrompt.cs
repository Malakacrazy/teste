using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Honor bid prompt for the draw phase
    /// </summary>
    public partial class HonorBidPrompt : IGameStep
    {
        private Game game;
        private string title;
        private Action<int> costHandler;
        private List<int> prohibitedBids;
        private Duel duel;
        private Dictionary<Player, int> bids = new Dictionary<Player, int>();
        
        public HonorBidPrompt(Game gameInstance, string activePromptTitle, Action<int> handler, List<int> prohibited, Duel duelContext = null)
        {
            game = gameInstance;
            title = activePromptTitle;
            costHandler = handler;
            prohibitedBids = prohibited ?? new List<int>();
            duel = duelContext;
        }
        
        public bool Execute()
        {
            return Continue();
        }
        
        public bool IsComplete()
        {
            // Complete when all players have bid
            foreach (var player in game.GetPlayers())
            {
                if (!bids.ContainsKey(player))
                {
                    return false;
                }
            }
            return true;
        }
        
        public bool Continue()
        {
            game.AddMessage(title);
            
            // Check if all bids received
            if (IsComplete())
            {
                ResolveBids();
                return true;
            }
            
            return false; // Wait for more bids
        }
        
        private void ResolveBids()
        {
            // Reveal honor bids
            game.AddMessage("Honor bids revealed:");
            foreach (var kvp in bids)
            {
                kvp.Key.SetShowBid(kvp.Value);
            }
            
            // Execute cost handler if provided
            costHandler?.Invoke(0);
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (command == "bid" && int.TryParse(arg, out int bidAmount))
            {
                if (!prohibitedBids.Contains(bidAmount) && bidAmount >= 0 && bidAmount <= 5)
                {
                    bids[player] = bidAmount;
                    game.AddMessage("{0} has made their honor bid", player);
                }
            }
        }
        
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }
    }
}
