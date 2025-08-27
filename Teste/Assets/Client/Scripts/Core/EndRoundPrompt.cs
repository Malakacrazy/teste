using System;

namespace L5RGame
{
    /// <summary>
    /// End round prompt for cleanup
    /// </summary>
    public partial class EndRoundPrompt : IGameStep
    {
        private Game game;
        private Player player;
        
        public EndRoundPrompt(Game gameInstance, Player promptPlayer = null) 
        {
            game = gameInstance;
            player = promptPlayer;
        }
        
        public bool Execute()
        {
            return Continue();
        }
        
        public bool IsComplete()
        {
            return true; // Complete immediately after cleanup
        }
        
        public bool Continue()
        {
            game.AddMessage("End of round - performing cleanup...");
            
            // Reset player states
            foreach (var gamePlayer in game.GetPlayers())
            {
                gamePlayer.passedDynasty = false;
                gamePlayer.limitedPlayed = 0;
                gamePlayer.conflictOpportunities.Reset();
            }
            
            // Reset rings
            foreach (var ring in game.GetRings())
            {
                ring.ResetRing();
            }
            
            game.roundNumber++;
            return true;
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }
    }
}
