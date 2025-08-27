using System;

namespace L5RGame
{
    /// <summary>
    /// Game won prompt for victory conditions
    /// </summary>
    public partial class GameWonPrompt : IGameStep
    {
        private Game game;
        private Player winner;
        
        public GameWonPrompt(Game gameInstance, Player winningPlayer) 
        {
            game = gameInstance;
            winner = winningPlayer;
        }
        
        public bool Execute()
        {
            return Continue();
        }
        
        public bool IsComplete()
        {
            return true; // Complete immediately after announcing winner
        }
        
        public bool Continue()
        {
            game.AddMessage("🎉 {0} has won the game! 🎉", winner);
            
            // Stop all clocks
            game.StopClocks();
            
            // Set final game state
            game.winner = winner;
            game.finishedAt = DateTime.Now;
            
            return true;
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }
    }
}
