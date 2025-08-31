using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Places fate on a character
    /// </summary>
    [System.Serializable]
    public partial class PlaceFateAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to placing fate
        /// </summary>
        [System.Serializable]
        public class PlaceFateProperties : CardActionProperties
        {
            public int amount = 1;
            public object origin; // DrawCard, Player, or Ring
            
            public PlaceFateProperties() : base() { }
            
            public PlaceFateProperties(int amount) : base()
            {
                this.amount = amount;
            }
            
            public PlaceFateProperties(int amount, object origin) : base()
            {
                this.amount = amount;
                this.origin = origin;
            }
        }
        
        #region Constructors
        
        public PlaceFateAction() : base()
        {
            Initialize();
        }
        
        public PlaceFateAction(PlaceFateProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public PlaceFateAction(System.Func<AbilityContext, PlaceFateProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "placeFate";
            eventName = EventNames.OnMoveFate;
            effectMessage = "place {1} fate on {0}";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new PlaceFateProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is PlaceFateProperties fateProps)
                return fateProps;
                
            // Convert base properties to PlaceFateProperties
            return new PlaceFateProperties()
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
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("place {1} fate on {0}", new object[] { properties.target, properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is DrawCard card))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't place 0 fate
            if (properties.amount == 0)
                return false;
                
            // Must be in play area
            if (card.location != Locations.PlayArea)
                return false;
            
            if (!base.CanAffect(target, context, additionalProperties))
                return false;
                
            // Check origin if specified
            return CheckOrigin(properties.origin, context);
        }
        
        /// <summary>
        /// Check if the origin can provide the fate
        /// </summary>
        private bool CheckOrigin(object origin, AbilityContext context)
        {
            if (origin == null)
                return true;
                
            // Check if origin has fate
            if (GetFateFromSource(origin) == 0)
                return false;
                
            // Players and rings can always provide fate
            if (origin is Player || origin is Ring)
                return true;
                
            // Cards must allow removing fate
            if (origin is DrawCard card)
                return card.AllowGameAction("removeFate", context);
                
            return true;
        }
        
        /// <summary>
        /// Get fate amount from source
        /// </summary>
        private int GetFateFromSource(object source)
        {
            return source switch
            {
                Player player => player.fate,
                Ring ring => ring.fate,
                DrawCard card => card.fate,
                _ => 0
            };
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            gameEvent.AddProperty("fate", properties.amount);
            gameEvent.AddProperty("origin", properties.origin);
            gameEvent.AddProperty("recipient", target as DrawCard);
        }
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            return MoveFateEventCondition(gameEvent);
        }
        
        protected override bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var eventFate = gameEvent.GetProperty("fate", 0);
            var eventOrigin = gameEvent.GetProperty("origin");
            var eventRecipient = gameEvent.GetProperty("recipient") as DrawCard;
            
            return !gameEvent.IsCancelled() && 
                   gameEvent.name == eventName && 
                   eventFate == properties.amount && 
                   eventOrigin == properties.origin && 
                   eventRecipient == target;
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            MoveFateEventHandler(gameEvent);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Place specific amount of fate on target
        /// </summary>
        public static PlaceFateAction Amount(int amount, DrawCard target = null)
        {
            var action = new PlaceFateAction(new PlaceFateProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Place 1 fate on target
        /// </summary>
        public static PlaceFateAction One(DrawCard target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Place fate from specific origin
        /// </summary>
        public static PlaceFateAction FromOrigin(int amount, object origin, DrawCard target = null)
        {
            var action = new PlaceFateAction(new PlaceFateProperties(amount, origin));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Place fate from player pool
        /// </summary>
        public static PlaceFateAction FromPlayer(int amount = 1, Player player = null)
        {
            return new PlaceFateAction(context => new PlaceFateProperties(amount, player ?? context.player));
        }
        
        /// <summary>
        /// Place fate from ring
        /// </summary>
        public static PlaceFateAction FromRing(Ring ring, int amount = 1)
        {
            return new PlaceFateAction(new PlaceFateProperties(amount, ring));
        }
        
        #endregion
    }
}