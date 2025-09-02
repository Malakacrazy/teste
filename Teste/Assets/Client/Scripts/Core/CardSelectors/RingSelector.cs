using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for ring targets instead of card targets.
    /// Perfect C# port of the original JavaScript RingSelector.
    /// </summary>
    [Serializable]
    public class RingSelector : BaseCardSelector
    {
        [Header("Ring Selection Configuration")]
        public Func<Ring, AbilityContext, bool> ringCondition;
        public GameAction gameAction;
        
        public RingSelector(CardSelectorProperties properties) : base(properties)
        {
            ringCondition = properties.ringCondition;
            gameAction = properties.gameAction;
        }
        
        /// <summary>
        /// Constructor with explicit ring condition
        /// </summary>
        public RingSelector(CardSelectorProperties properties, Func<Ring, AbilityContext, bool> condition, GameAction action = null)
            : base(properties)
        {
            ringCondition = condition ?? ((ring, context) => true);
            gameAction = action;
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            if (context?.game?.rings == null || ringCondition == null)
                return false;
            
            // Match JavaScript logic with underscore _.any()
            return context.game.rings.Values.Any(ring => ringCondition(ring, context));
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            return activePromptTitle ?? "Choose a ring";
        }
        
        /// <summary>
        /// Get all valid ring targets
        /// </summary>
        public List<Ring> GetValidRingTargets(AbilityContext context)
        {
            if (context?.game?.rings == null || ringCondition == null)
                return new List<Ring>();
            
            return context.game.rings.Values.Where(ring => ringCondition(ring, context)).ToList();
        }
        
        // Override base card methods to handle ring-specific logic
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            // Ring selectors typically select one ring
            return (selectedCards?.Count ?? 0) >= 1;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            // Ring selectors are typically single selection
            return (selectedCards?.Count ?? 0) >= 1;
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            // For ring selectors, we might want to return ring objects instead of cards
            return selectedCards?.FirstOrDefault();
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return true; // Usually auto-fire on ring selection
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) >= 1 || optional;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) > 1;
        }
        
        /// <summary>
        /// Format ring selection parameter
        /// </summary>
        public Ring FormatRingSelectParam(List<Ring> selectedRings)
        {
            return selectedRings?.FirstOrDefault();
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new
            {
                baseInfo,
                hasRingCondition = ringCondition != null,
                hasGameAction = gameAction != null,
                selectorType = "Ring"
            };
        }
    }
}
