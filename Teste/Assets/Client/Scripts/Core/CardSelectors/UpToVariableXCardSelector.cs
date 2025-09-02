using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for up to X number of cards where X is determined dynamically.
    /// The maximum number of cards is calculated at selection time using a function.
    /// Perfect C# port of the original JavaScript UpToVariableXCardSelector.
    /// </summary>
    [Serializable]
    public class UpToVariableXCardSelector : BaseCardSelector
    {
        [Header("Variable X Configuration")]
        public Func<AbilityContext, int> numCardsFunc;
        
        public UpToVariableXCardSelector(Func<AbilityContext, int> numCardsFunction, CardSelectorProperties properties) 
            : base(properties)
        {
            numCardsFunc = numCardsFunction ?? (context => 1);
            this.optional = true; // Up-to selectors are optional by nature
        }
        
        /// <summary>
        /// Get the current maximum number of cards that can be selected
        /// </summary>
        private int GetCurrentMaxCards(AbilityContext context)
        {
            if (numCardsFunc == null || context == null)
                return 1;
                
            try
            {
                return Math.Max(0, numCardsFunc(context));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ UpToVariableXCardSelector: Error calculating maxCards: {e.Message}");
                return 1;
            }
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            if (context == null)
                return activePromptTitle ?? "Select characters";
            
            int currentMax = GetCurrentMaxCards(context);
            
            // Match JavaScript logic exactly
            return currentMax == 1 
                ? "Select up to one character" 
                : $"Select up to {currentMax} characters";
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            if (context == null)
                return false;
            
            int maxCards = GetCurrentMaxCards(context);
            return (selectedCards?.Count ?? 0) >= maxCards;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            if (context == null)
                return false;
            
            int maxCards = GetCurrentMaxCards(context);
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
            
            // Without context, we can't determine the actual limit
            // This is a limitation of the interface, but we'll be conservative
            return false; // Let the context-aware methods handle the real validation
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            return selectedCards ?? new List<BaseCard>();
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            if (context == null)
                return false;
            
            // Only auto-fire for single card up-to selectors
            int maxCards = GetCurrentMaxCards(context);
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
            // Without context, we can't determine the max
            return int.MaxValue;
        }
        
        /// <summary>
        /// Context-aware maximum allowed method
        /// </summary>
        public int GetMaximumAllowed(AbilityContext context)
        {
            return context != null ? GetCurrentMaxCards(context) : 1;
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new
            {
                baseInfo,
                hasNumCardsFunc = numCardsFunc != null,
                selectorType = "UpToVariableX"
            };
        }
    }
}
