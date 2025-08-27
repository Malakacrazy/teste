using System;

namespace L5RGame
{
    /// <summary>
    /// Action window step
    /// </summary>
    public class ActionWindow : BaseStepWithPipeline, IGameStep
    {
        private Player currentPlayer;

        public ActionWindow(Game game, Player player = null) : base(game)
        {
            currentPlayer = player;
        }

        protected override void InitializePipeline()
        {
            base.InitializePipeline();
            // Add action processing steps here
        }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            var playerInfo = currentPlayer != null ? $" ({currentPlayer.name})" : "";
            return $"ActionWindow{playerInfo} - Waiting for actions";
        }
    }
}
