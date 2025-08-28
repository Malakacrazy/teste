using System;

namespace L5RGame
{
    public class FatePhase : GamePhase
    {
        public FatePhase(Game game) : base(game, GamePhases.Fate) { }

        public override string GetDebugInfo()
        {
            return "FatePhase - Fate actions";
        }
    }
}