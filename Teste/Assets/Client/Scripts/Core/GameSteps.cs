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





    // SetupPhase and SimultaneousEffectWindow are now defined in separate files in GameSteps directory

}
