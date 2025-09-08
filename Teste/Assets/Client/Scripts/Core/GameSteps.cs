using System;
using System.Collections.Generic;

namespace L5RGame
{

    public class GameStepHonorBidPrompt : BaseStep, IGameStep
    {
        private string activePromptTitle;
        private System.Action<int> costHandler;
        private List<int> prohibitedBids;
        private Duel duel;

        public GameStepHonorBidPrompt(Game game) : base(game) { }
        public GameStepHonorBidPrompt(Game game, string title, System.Action<int> handler, List<int> prohibited, Duel associatedDuel = null) : base(game)
        {
            activePromptTitle = title;
            costHandler = handler;
            prohibitedBids = prohibited ?? new List<int>();
            duel = associatedDuel;
        }

        public override bool Continue()
        {
            return false; // Wait for player input
        }

        public override string GetDebugInfo()
        {
            return $"HonorBidPrompt - Title: {activePromptTitle} - Duel: {duel != null}";
        }
    }





    public class SetupPhase : BaseStepWithPipeline, IGameStep
    {
        public SetupPhase(Game game) : base(game) { }

        public override string GetDebugInfo()
        {
            return "SetupPhase - Game setup";
        }
    }

    public class SimultaneousEffectWindow : BaseStep, IGameStep
    {
        private List<EffectChoice> choices = new List<EffectChoice>();

        public SimultaneousEffectWindow(Game game) : base(game) { }

        public void AddChoice(EffectChoice choice)
        {
            choices.Add(choice);
        }

        public override bool Continue()
        {
            // Process all choices and complete
            foreach (var choice in choices)
            {
                choice.Execute();
            }
            return true;
        }

        public override string GetDebugInfo()
        {
            return $"SimultaneousEffectWindow - Choices: {choices.Count}";
        }
    }

}
