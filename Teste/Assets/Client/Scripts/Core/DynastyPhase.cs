using System;

namespace L5RGame
{
    public class DynastyPhase : BaseStepWithPipeline, IGameStep
    {
        public DynastyPhase(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;

        public override string GetDebugInfo()
        {
            return "DynastyPhase - Dynasty actions";
        }
    }
}
