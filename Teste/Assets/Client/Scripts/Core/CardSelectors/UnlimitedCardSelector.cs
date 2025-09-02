using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector that allows selection of any number of valid cards.
    /// No upper limit on selection count.
    /// Perfect C# port of the original JavaScript UnlimitedCardSelector.
    /// </summary>
    [Serializable]
    public class UnlimitedCardSelector : BaseCardSelector
    {
        public UnlimitedCardSelector(CardSelectorProperties properties) : base(properties)
        {
            this.numCards = 0; // 0 indicates unlimited
            this.optional = true; // Unlimited selectors are typically optional
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            // Match JavaScript - unlimited selectors never reach a limit
            return false;
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            return activePromptTitle ?? "Select any number of cards";
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            // Unlimited selectors always have "enough" targets since 0 is acceptable
            return true;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            // Unlimited selectors never exceed limits
            return false;
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            return selectedCards ?? new List<BaseCard>();
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return false; // Don't auto-fire, let player confirm selection
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            // Unlimited selectors are always satisfied (even with 0 cards)
            return true;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            // Unlimited selectors never exceed limits
            return false;
        }
        
        public override int GetMinimumRequired()
        {
            return 0; // No minimum for unlimited selectors
        }
        
        public override int GetMaximumAllowed()
        {
            return int.MaxValue; // Truly unlimited
        }
    }
}
