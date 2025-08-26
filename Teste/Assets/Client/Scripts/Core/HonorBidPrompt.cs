using System.Collections.Generic;
using System;
using UnityEngine;

namespace L5RGame
{
    public class HonorBidPrompt : MonoBehaviour, IGameStep
    {
        private Game game;
        private string activePromptTitle;
        private Action<int> costHandler;
        private List<int> prohibitedBids;
        private Duel duel;
        private bool completed = false;

        public HonorBidPrompt(Game game, string activePromptTitle, Action<int> costHandler, List<int> prohibitedBids, Duel duel = null)
        {
            this.game = game;
            this.activePromptTitle = activePromptTitle;
            this.costHandler = costHandler;
            this.prohibitedBids = prohibitedBids ?? new List<int>();
            this.duel = duel;
        }

        public bool Execute()
        {
            // Execute honor bid prompt logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
