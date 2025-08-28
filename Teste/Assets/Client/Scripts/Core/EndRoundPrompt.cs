using System;

namespace L5RGame
{
    /// <summary>
    /// Prompt for ending the round
    /// </summary>
    public class EndRoundPrompt : BaseStep, IGameStep
    {
        public EndRoundPrompt(Game game) : base(game)
        {
        }

        public override bool Continue()
        {
            // Simple implementation - just complete
            return true;
        }

        bool IGameStep.IsComplete() => IsComplete;

        public override string GetDebugInfo()
        {
            return "EndRoundPrompt - Waiting for round end";
        }
    }
}
