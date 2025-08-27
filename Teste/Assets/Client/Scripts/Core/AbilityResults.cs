using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Helper class for various game utilities
    /// </summary>
    public static class GameHelper
    {
        public static Player GetLocalPlayer(Game game)
        {
            var players = game.GetPlayers();
            return players.Count > 0 ? players[0] : null;
        }
    }
}
