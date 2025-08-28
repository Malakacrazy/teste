using System;

namespace L5RGame
{
    /// <summary>
    /// Conflict phase step
    /// </summary>
    public class ConflictPhase : BaseStepWithPipeline, IGameStep
    {
        public ConflictPhase(Game game) : base(game)
        {
        }

        protected override void InitializePipeline()
        {
            base.InitializePipeline();
            // Add conflict-specific steps here
        }

        bool IGameStep.IsComplete() => IsComplete;

        public override string GetDebugInfo()
        {
            return $"ConflictPhase - {GetCompletionPercentage():F0}% complete";
        }
        
        private float GetCompletionPercentage()
        {
            return PipelineProgress * 100f;
        }
    }
}
