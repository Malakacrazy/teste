using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class PlayerOrderPrompt : UiPrompt
    {
        protected List<Player> players;

        public Player CurrentPlayer
        {
            get
            {
                LazyFetchPlayers();
                return players.Count > 0 ? players[0] : null;
            }
        }

        public PlayerOrderPrompt(Game game) : base(game)
        {
        }

        protected void LazyFetchPlayers()
        {
            if (players == null)
            {
                players = game.GetPlayersInFirstPlayerOrder().ToList();
            }
        }

        protected void SkipPlayers()
        {
            LazyFetchPlayers();
            players = players.Where(p => !SkipCondition(p)).ToList();
        }

        public virtual bool SkipCondition(Player player)
        {
            return false;
        }

        protected void CompletePlayer()
        {
            LazyFetchPlayers();
            if (players.Count > 0)
            {
                players.RemoveAt(0);
            }
        }

        public void SetPlayers(List<Player> players)
        {
            this.players = players;
        }

        public override bool IsComplete 
        { 
            get
            {
                LazyFetchPlayers();
                return players.Count == 0;
            }
        }

        public override bool ActiveCondition(Player player)
        {
            return player == CurrentPlayer;
        }

        public override bool Continue()
        {
            SkipPlayers();
            return base.Continue();
        }
    }
}
