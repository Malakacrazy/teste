using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Bows a character, attachment, or stronghold
    /// </summary>
    [System.Serializable]
    public partial class BowAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to bowing
        /// </summary>
        [System.Serializable]
        public class BowActionProperties : CardActionProperties
        {
            public BowActionProperties() : base() { }
        }
        
        #region Constructors
        
        public BowAction() : base()
        {
            Initialize();
        }
        
        public BowAction(BowActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public BowAction(System.Func<AbilityContext, BowActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "bow";
            eventName = EventNames.OnCardBowed;
            costMessage = "bowing {0}";
            effectMessage = "bow {0}";
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Stronghold 
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new BowActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is BowActionProperties bowProps)
                return bowProps;
                
            // Convert base properties to BowActionProperties
            return new BowActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            // Must be in play area (or stronghold for stronghold cards)
            if (card.location != Locations.PlayArea && card.type != CardTypes.Stronghold)
                return false;
                
            // Cannot bow already bowed cards
            if (card.bowed)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            
            if (card != null)
            {
                card.Bow();
                LogExecution("Bowed {0}", card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to bow specific card
        /// </summary>
        public static BowAction Card(BaseCard card)
        {
            var action = new BowAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to bow source card
        /// </summary>
        public static BowAction Self()
        {
            var action = new BowAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        #endregion
    }
}