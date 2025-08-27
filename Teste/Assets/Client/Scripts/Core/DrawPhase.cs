using System;

namespace L5RGame
{
    public class DrawPhase : BaseStepWithPipeline, IGameStep
    {
        public DrawPhase(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "DrawPhase - Drawing cards";
        }
    }
}