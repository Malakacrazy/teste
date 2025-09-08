using UnityEngine;

namespace L5RGame
{
    public class EndRoundPrompt : BaseStep
    {
        private Player currentPlayer;
        
        public Player CurrentPlayer => currentPlayer ?? game.GetActivePlayer();
        
        public EndRoundPrompt(Game game) : base(game, "End Round Prompt")
        {
        }

        public object GetActivePrompt(Player player)
        {
            return new
            {
                menuTitle = "",
                buttons = new[] { new { text = "End Round" } }
            };
        }

        public object GetWaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent to end the round" };
        }

        public override bool OnMenuCommand(Player player, string command, string arg1, string arg2)
        {
            if (player != CurrentPlayer)
            {
                return false;
            }

            CompletePlayer();
            return true;
        }
        
        private void CompletePlayer()
        {
            isComplete = true;
        }
        
        public override bool Execute()
        {
            return true;
        }
    }
}
