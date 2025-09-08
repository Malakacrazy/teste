using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GameWonPrompt : BaseStep
    {
        private Player winner;
        private Dictionary<string, bool> clickedButton;

        public GameWonPrompt(Game game, Player winner) : base(game)
        {
            this.winner = winner;
            clickedButton = new Dictionary<string, bool>();
        }

        public bool CompletionCondition(Player player)
        {
            return clickedButton.ContainsKey(player.Name) && clickedButton[player.Name];
        }

        public object ActivePrompt(Player player)
        {
            return new
            {
                promptTitle = "Game Won",
                menuTitle = winner.Name + " has won the game!",
                buttons = new[] { new { text = "Continue Playing" } }
            };
        }

        public object WaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent to choose to continue" };
        }

        public override bool OnMenuCommand(Player player, string command, string arg1, string arg2)
        {
            game.AddMessage("{0} wants to continue", player);
            clickedButton[player.Name] = true;
            return true;
        }
    }
}
