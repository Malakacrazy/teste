using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Removes fate from a character
    /// </summary>
    [System.Serializable]
    public partial class RemoveFateAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to removing fate
        /// </summary>
        [System.Serializable]
        public class RemoveFateProperties : CardActionProperties
        {
            public int amount = 1;
            public object recipient; // DrawCard, Player, or Ring that receives the fate
            
            public RemoveFateProperties() : base() { }
            
            public RemoveFateProperties(int amount) : base()
            {
                this.amount = amount;
            }
            
            public RemoveFateProperties(int amount, object recipient) : base()
            {
                this.amount = amount;
                this.recipient = recipient;
            }
        }
        
        #region Constructors
        
        public RemoveFateAction() : base()
        {
            Initialize();
        }
        
        public RemoveFateAction(RemoveFateProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public RemoveFateAction(System.Func<AbilityContext, RemoveFateProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "removeFate";
            eventName = EventNames.OnMoveFate;
            costMessage = "removing {1} fate from {0}";
            effectMessage = "remove {1} fate from {0}";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new RemoveFateProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is RemoveFateProperties fateProps)
                return fateProps;
                
            // Convert base properties to RemoveFateProperties
            return new RemoveFateProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                amount = 1
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("removing {1} fate from {0}", new object[] { properties.target, properties.amount });
        }
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("remove {1} fate from {0}", new object[] { properties.target, properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't remove 0 fate
            if (properties.amount == 0)
                return false;
                
            // Must be in play area
            if (card.location != Locations.PlayArea)
                return false;
                
            // Must have fate to remove
            if (card.fate == 0)
                return false;
            
            if (!base.CanAffect(target, context, additionalProperties))
                return false;
                
            // Check recipient if specified
            return CheckRecipient(properties.recipient, context);
        }
        
        /// <summary>
        /// Check if the recipient can receive the fate
        /// </summary>
        private bool CheckRecipient(object recipient, AbilityContext context)
        {
            if (recipient == null)
                return true;
                
            // Players and rings can always receive fate
            if (recipient is Player || recipient is Ring)
                return true;
                
            // Cards must allow placing fate
            if (recipient is DrawCard card)
                return card.AllowGameAction("placeFate", context);
                
            return true;
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            gameEvent.AddProperty("fate", properties.amount);
            gameEvent.AddProperty("recipient", properties.recipient);
            gameEvent.AddProperty("origin", target as DrawCard);
        }
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            return MoveFateEventCondition(gameEvent);
        }
        
        protected override bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var eventFate = gameEvent.GetProperty("fate", 0);
            var eventRecipient = gameEvent.GetProperty("recipient");
            var eventOrigin = gameEvent.GetProperty("origin") as DrawCard;
            
            return !gameEvent.IsCancelled() && 
                   gameEvent.name == eventName && 
                   eventFate == properties.amount && 
                   eventOrigin == target && 
                   eventRecipient == properties.recipient;
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            MoveFateEventHandler(gameEvent);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Remove specific amount of fate from target
        /// </summary>
        public static RemoveFateAction Amount(int amount, BaseCard target = null)
        {
            var action = new RemoveFateAction(new RemoveFateProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Remove 1 fate from target
        /// </summary>
        public static RemoveFateAction One(BaseCard target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Remove all fate from target
        /// </summary>
        public static RemoveFateAction All(BaseCard target = null)
        {
            var action = new RemoveFateAction(context => 
            {
                var card = target ?? context.source as BaseCard;
                return new RemoveFateProperties(card?.fate ?? 0);
            });
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Remove fate and give to specific recipient
        /// </summary>
        public static RemoveFateAction ToRecipient(int amount, object recipient, BaseCard target = null)
        {
            var action = new RemoveFateAction(new RemoveFateProperties(amount, recipient));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Remove fate and give to player
        /// </summary>
        public static RemoveFateAction ToPlayer(int amount = 1, Player player = null)
        {
            return new RemoveFateAction(context => new RemoveFateProperties(amount, player ?? context.player));
        }
        
        /// <summary>
        /// Remove fate and give to ring
        /// </summary>
        public static RemoveFateAction ToRing(Ring ring, int amount = 1)
        {
            return new RemoveFateAction(new RemoveFateProperties(amount, ring));
        }
        
        #endregion
    }
}