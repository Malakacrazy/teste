using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class HonorBidPrompt : MonoBehaviour, IGameStep
    {
        public HonorBidPrompt(Game game, string activePromptTitle, System.Action<int> costHandler, List<int> prohibitedBids, Duel duel = null) { }
    }
}