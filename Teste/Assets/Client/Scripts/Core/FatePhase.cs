using System;

namespace L5RGame
{
    public class FatePhase : BaseStepWithPipeline, IGameStep
    {
        public FatePhase(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "FatePhase - Fate actions";
        }
    }
}