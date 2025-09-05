using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Simple discard action for cards
    /// </summary>
    [System.Serializable]
    public class DiscardAction : GameAction
    {
        public Player targetPlayer;
        public BaseCard targetCard;
        public string actionType = "discard";
        
        public DiscardAction(Player player, BaseCard card)
        {
            targetPlayer = player;
            targetCard = card;
        }
        
        public override void Execute(AbilityContext context)
        {
            if (targetCard != null && targetPlayer != null)
            {
                targetPlayer.DiscardCardFromHand(targetCard);
                Debug.Log($"Discarded {targetCard.name} from {targetPlayer.name}'s hand");
            }
        }
    }
}