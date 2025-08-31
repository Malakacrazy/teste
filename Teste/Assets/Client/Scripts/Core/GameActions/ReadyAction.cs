using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Readies a character, attachment, or stronghold
    /// </summary>
    [System.Serializable]
    public partial class ReadyAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to readying
        /// </summary>
        [System.Serializable]
        public class ReadyProperties : CardActionProperties
        {
            public ReadyProperties() : base() { }
        }
        
        #region Constructors
        
        public ReadyAction() : base()
        {
            Initialize();
        }
        
        public ReadyAction(ReadyProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ReadyAction(System.Func<AbilityContext, ReadyProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "ready";
            eventName = EventNames.OnCardReadied;
            costMessage = "readying {0}";
            effectMessage = "ready {0}";
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
        public new ReadyProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is ReadyProperties readyProps)
                return readyProps;
                
            // Convert base properties to ReadyProperties
            return new ReadyProperties()
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
                
            // Cannot ready already ready cards
            if (!card.bowed)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            
            if (card != null)
            {
                card.Ready();
                LogExecution("Readied {0}", card.name);
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to ready specific card
        /// </summary>
        public static ReadyAction Card(BaseCard card)
        {
            var action = new ReadyAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to ready source card
        /// </summary>
        public static ReadyAction Self()
        {
            var action = new ReadyAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        /// <summary>
        /// Create action to ready all bowed characters
        /// </summary>
        public static ReadyAction AllBowedCharacters(Player player = null)
        {
            var action = new ReadyAction();
            action.SetDefaultTarget(context =>
            {
                var targetPlayer = player ?? context.player;
                return targetPlayer.cardsInPlay.Where(c => c.type == CardTypes.Character && c.bowed).ToList();
            });
            return action;
        }
        
        #endregion
    }
}