using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for exactly one card.
    /// Most common selector type for single target abilities.
    /// Perfect C# port of the original JavaScript SingleCardSelector.
    /// </summary>
    [Serializable]
    public class SingleCardSelector : BaseCardSelector
    {
        public SingleCardSelector(CardSelectorProperties properties) : base(properties)
        {
            this.numCards = 1;
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            // Match JavaScript logic exactly
            if (cardType.Count == 1)
            {
                if (cardType[0] == CardTypes.Attachment)
                {
                    return "Choose an attachment";
                }
                return $"Choose a {cardType[0]}";
            }
            
            return "Choose a card";
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return true;
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) >= numCards;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) > numCards;
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            // Match JavaScript logic exactly - return first card or the array
            var firstCard = selectedCards?.FirstOrDefault();
            return firstCard ?? (object)selectedCards;
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            var validTargets = GetAllLegalTargets(context, choosingPlayer);
            return validTargets.Count >= 1 || optional;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            if (selectedCards?.Contains(card) == true)
                return false; // Deselecting doesn't exceed limit
            
            return (selectedCards?.Count ?? 0) >= 1;
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) == 1 || (optional && (selectedCards?.Count ?? 0) == 0);
        }
        
        public override int GetMinimumRequired()
        {
            return optional ? 0 : 1;
        }
        
        public override int GetMaximumAllowed()
        {
            return 1;
        }
    }
}
