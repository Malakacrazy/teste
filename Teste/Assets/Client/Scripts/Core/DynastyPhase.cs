using System;

namespace L5RGame
{
    public class DynastyPhase : GamePhase
    {
        public DynastyPhase(Game game) : base(game, GamePhases.Dynasty) { }

        public override string GetDebugInfo()
        {
            return "DynastyPhase - Dynasty actions";
        }
    }
}
