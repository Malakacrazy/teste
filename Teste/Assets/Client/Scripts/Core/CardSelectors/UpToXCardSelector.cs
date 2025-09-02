using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for up to X number of cards.
    /// Players can select anywhere from 0 to X cards.
    /// Perfect C# port of the original JavaScript UpToXCardSelector.
    /// </summary>
    [Serializable]
    public class UpToXCardSelector : BaseCardSelector
    {
        [Header("Up To X Configuration")]
        public int maxCards;
        
        public UpToXCardSelector(int maxNumCards, CardSelectorProperties properties) : base(properties)
        {
            maxCards = maxNumCards;
            this.numCards = maxNumCards;
            this.optional = true; // Up-to selectors are optional by nature
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            // Match JavaScript logic exactly
            return maxCards == 1 
                ? "Select a character" 
                : $"Select {maxCards} characters";
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) >= maxCards;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) > maxCards;
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            // Up-to selectors always have enough targets since 0 is acceptable
            return true;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            if (selectedCards?.Contains(card) == true)
                return false; // Deselecting doesn't exceed limit
            
            return (selectedCards?.Count ?? 0) >= maxCards;
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            if (maxCards == 1)
                return selectedCards?.FirstOrDefault();
            
            return selectedCards ?? new List<BaseCard>();
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            // Only auto-fire for single card up-to selectors
            return maxCards == 1;
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            // Up-to selectors are satisfied with any number from 0 to max
            return true;
        }
        
        public override int GetMinimumRequired()
        {
            return 0; // Up-to selectors have no minimum
        }
        
        public override int GetMaximumAllowed()
        {
            return maxCards;
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new
            {
                baseInfo,
                maxCards
            };
        }
    }
}
