using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for actions that target cards
    /// </summary>
    [System.Serializable]
    public partial class CardGameAction : GameAction
    {
        /// <summary>
        /// Properties for card-targeted actions
        /// </summary>
        [System.Serializable]
        public class CardActionProperties : GameActionProperties
        {
            public CardActionProperties() : base() { }
            
            public CardActionProperties(List<object> targets) : base(targets) { }
        }
        
        #region Constructors
        
        protected CardGameAction() : base()
        {
            Initialize();
        }
        
        protected CardGameAction(CardActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        protected CardGameAction(System.Func<AbilityContext, CardActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Holding, 
                CardTypes.Event, 
                CardTypes.Stronghold, 
                CardTypes.Province, 
                CardTypes.Role, 
                "ring" 
            };
        }
        
        #endregion
        
        #region Default Targets
        
        protected override List<object> DefaultTargets(AbilityContext context)
        {
            // Default to source card
            return context.source != null ? 
                new List<object> { context.source } : 
                new List<object>();
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("target") as BaseCard;
            return card != null && CanAffect(card, gameEvent.context, additionalProperties);
        }
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return target is BaseCard || target is Ring ? 
                base.CanAffect(target, context, additionalProperties) : false;
        }
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("card", target as BaseCard);
        }
        
        protected override bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var eventCard = gameEvent.GetProperty("card") as BaseCard;
            return eventCard == target && base.IsEventFullyResolved(gameEvent, target, context, additionalProperties);
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new CardActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is CardActionProperties cardProps)
                return cardProps;
                
            // Convert base properties to CardActionProperties
            return new CardActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if province needs refilling after card leaves
        /// </summary>
        protected virtual void CheckForRefillProvince(BaseCard card, GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            if (!card.IsInProvince() || card.location == Locations.StrongholdProvince)
                return;
                
            var isReplacementEffect = additionalProperties?.GetType().GetProperties()
                .Any(p => p.Name == "replacementEffect") == true;
            var refillContext = isReplacementEffect ? gameEvent.context : gameEvent.context;
            
            refillContext.RefillProvince(card.controller, card.location);
        }
        
        #endregion
    }
}