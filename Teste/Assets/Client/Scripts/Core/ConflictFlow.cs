using System;

namespace L5RGame
{
    public class ConflictFlow : BaseStepWithPipeline, IGameStep
    {
        public ConflictFlow(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "ConflictFlow - Conflict resolution";
        }
    }
}