using System;

namespace L5RGame
{
    public class MenuPrompt : BaseStep, IGameStep
    {
        public MenuPrompt(Game game) : base(game) { }
        public MenuPrompt(Game game, Player player, string title, string text) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "MenuPrompt - Waiting for menu selection";
        }
    }

    public class HonorBidPrompt : BaseStep, IGameStep
    {
        public HonorBidPrompt(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "HonorBidPrompt - Waiting for honor bid";
        }
    }

    public class GameWonPrompt : BaseStep, IGameStep
    {
        public GameWonPrompt(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "GameWonPrompt - Game ended";
        }
    }

    public class InitiateAbilityEventWindow : BaseStepWithPipeline, IGameStep
    {
        public InitiateAbilityEventWindow(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "InitiateAbilityEventWindow - Processing ability events";
        }
    }

    public class HandlerMenuPrompt : BaseStep, IGameStep
    {
        public HandlerMenuPrompt(Game game) : base(game) { }
        public HandlerMenuPrompt(Game game, Player player, object properties) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "HandlerMenuPrompt - Handler menu selection";
        }
    }

    public class SelectRingPrompt : BaseStep, IGameStep
    {
        public SelectRingPrompt(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "SelectRingPrompt - Ring selection";
        }
    }

    public class SetupPhase : BaseStepWithPipeline, IGameStep
    {
        public SetupPhase(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "SetupPhase - Game setup";
        }
    }

    public class SimultaneousEffectWindow : BaseStepWithPipeline, IGameStep
    {
        public SimultaneousEffectWindow(Game game) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "SimultaneousEffectWindow - Processing simultaneous effects";
        }
    }

    public class SelectCardPrompt : BaseStep, IGameStep
    {
        public SelectCardPrompt(Game game) : base(game) { }
        public SelectCardPrompt(Game game, Player player, object properties) : base(game) { }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            return "SelectCardPrompt - Card selection";
        }
    }
}
