using System;

namespace L5RGame
{
    public class DrawPhase : GamePhase
    {
        public DrawPhase(Game game) : base(game, GamePhases.Draw) { }

        public override string GetDebugInfo()
        {
            return "DrawPhase - Drawing cards";
        }
    }
}