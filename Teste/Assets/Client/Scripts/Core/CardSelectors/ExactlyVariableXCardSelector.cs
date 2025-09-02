using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for exactly X number of cards where X is determined dynamically.
    /// The number of cards is calculated at selection time using a function.
    /// Complete C# port of the original JavaScript ExactlyVariableXCardSelector.
    /// </summary>
    [Serializable]
    public class ExactlyVariableXCardSelector : BaseCardSelector
    {
        [Header("Variable X Configuration")]
        public Func<AbilityContext, int> numCardsFunc;
        
        public ExactlyVariableXCardSelector(Func<AbilityContext, int> numCardsFunction, CardSelectorProperties properties) 
            : base(properties)
        {
            numCardsFunc = numCardsFunction ?? (context => 1);
            this.optional = false; // Exactly selectors are never optional
        }
        
        /// <summary>
        /// Get the current number of cards to select for given context
        /// </summary>
        private int GetCurrentNumCards(AbilityContext context)
        {
            if (numCardsFunc == null)
                return 1;
                
            try
            {
                return Math.Max(0, numCardsFunc(context));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ ExactlyVariableXCardSelector: Error calculating numCards: {e.Message}");
                return 1;
            }
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            if (context == null)
                return false;
                
            return (selectedCards?.Count ?? 0) > GetCurrentNumCards(context);
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            if (context == null)
                return activePromptTitle ?? "Select cards";
            
            int numCards = GetCurrentNumCards(context);
            
            // Match JavaScript logic exactly
            if (cardType.Count == 1)
            {
                string cardTypeName = cardType[0];
                return numCards == 1 
                    ? $"Choose a {cardTypeName}" 
                    : $"Choose {numCards} {cardTypeName}s";
            }
            
            return numCards == 1 
                ? "Select a card" 
                : $"Select {numCards} cards";
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            if (context == null)
                return (selectedCards?.Count ?? 0) > 0;
                
            return (selectedCards?.Count ?? 0) == GetCurrentNumCards(context);
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            if (context == null)
                return false;
            
            // Match JavaScript logic exactly - count all matching cards
            int numMatchingCards = context.game.GetAllCards().Count(card => 
                CanTarget(card, context, choosingPlayer));
            
            return numMatchingCards >= GetCurrentNumCards(context);
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            if (context == null)
                return (selectedCards?.Count ?? 0) > 0;
                
            return (selectedCards?.Count ?? 0) >= GetCurrentNumCards(context);
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            if (context == null)
                return false;
                
            return GetCurrentNumCards(context) == 1;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            if (selectedCards?.Contains(card) == true)
                return false; // Deselecting doesn't exceed limit
            
            // We need context to determine the limit, but this method doesn't provide it
            // This is a limitation of the base interface - we'll have to make a best guess
            // In practice, this should be called with HasReachedLimit which does have context
            return (selectedCards?.Count ?? 0) >= 1; // Conservative estimate
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            // For variable selectors, we need to check if it's expecting a single card
            // Since we don't have context here, we'll return the list and let the calling code handle it
            return selectedCards ?? new List<BaseCard>();
        }
        
        public override int GetMinimumRequired()
        {
            // Variable selectors need context to determine requirements
            // Return 0 as a safe default - actual validation happens in HasEnoughSelected
            return 0;
        }
        
        public override int GetMaximumAllowed()
        {
            // Variable selectors need context to determine maximum
            // Return a large number as default - actual validation happens in HasReachedLimit
            return int.MaxValue;
        }
        
        /// <summary>
        /// Context-aware format parameter method
        /// </summary>
        public object FormatSelectParam(List<BaseCard> selectedCards, AbilityContext context)
        {
            if (context != null && GetCurrentNumCards(context) == 1)
                return selectedCards?.FirstOrDefault();
            
            return selectedCards ?? new List<BaseCard>();
        }
        
        /// <summary>
        /// Context-aware minimum required method
        /// </summary>
        public int GetMinimumRequired(AbilityContext context)
        {
            return context != null ? GetCurrentNumCards(context) : 1;
        }
        
        /// <summary>
        /// Context-aware maximum allowed method
        /// </summary>
        public int GetMaximumAllowed(AbilityContext context)
        {
            return context != null ? GetCurrentNumCards(context) : 1;
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new
            {
                baseInfo,
                hasNumCardsFunc = numCardsFunc != null,
                // Note: We can't show currentNumCards without context
                selectorType = "ExactlyVariableX"
            };
        }
        
        /// <summary>
        /// Get debug info with context for better debugging
        /// </summary>
        public object GetDebugInfo(AbilityContext context)
        {
            var baseInfo = base.GetDebugInfo();
            int currentNum = context != null ? GetCurrentNumCards(context) : 0;
            
            return new
            {
                baseInfo,
                currentNumCards = currentNum,
                hasNumCardsFunc = numCardsFunc != null,
                selectorType = "ExactlyVariableX"
            };
        }
    }
}
