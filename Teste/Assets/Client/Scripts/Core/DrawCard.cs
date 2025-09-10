using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player draws cards from their deck
    /// </summary>
    [System.Serializable]
    public partial class DrawAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to drawing cards
        /// </summary>
        [System.Serializable]
        public class DrawProperties : PlayerActionProperties
        {
            public int amount = 1;
            
            public DrawProperties() : base() { }
            
            public DrawProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public DrawAction() : base()
        {
            Initialize();
        }
        
        public DrawAction(DrawProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DrawAction(System.Func<AbilityContext, DrawProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "draw";
            eventName = EventNames.OnCardsDrawn;
            effectMessage = "draw {1} cards";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new DrawProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is DrawProperties drawProps)
                return drawProps;
                
            // Convert base properties to DrawProperties
            return new DrawProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                amount = 1
            };
        }
        
        #endregion
        
        #region Default Targets
        
        protected override List<object> DefaultTargets(AbilityContext context)
        {
            // Default to the acting player for drawing cards
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var cardText = properties.amount == 1 ? "card" : "cards";
            return ($"draw {properties.amount} {cardText}", new object[0]);
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't draw 0 cards
            if (properties.amount == 0)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("amount", properties.amount);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var amount = gameEvent.GetProperty("amount", 1);
            
            if (player != null)
            {
                player.DrawCardsToHand(amount);
                LogExecution("{0} drew {1} cards", player.name, amount);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action for player to draw specific amount of cards
        /// </summary>
        public static DrawAction Amount(int amount, Player target = null)
        {
            var action = new DrawAction(new DrawProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for player to draw 1 card
        /// </summary>
        public static DrawAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Create action for player to draw 2 cards
        /// </summary>
        public static DrawAction Two(Player target = null)
        {
            return Amount(2, target);
        }
        
        /// <summary>
        /// Create action for player to draw 3 cards
        /// </summary>
        public static DrawAction Three(Player target = null)
        {
            return Amount(3, target);
        }
        
        /// <summary>
        /// Create action for opponent to draw cards
        /// </summary>
        public static DrawAction Opponent(int amount = 1)
        {
            var action = new DrawAction(new DrawProperties(amount));
            action.TargetOpponent();
            return action;
        }
        
        /// <summary>
        /// Create action for both players to draw cards
        /// </summary>
        public static DrawAction Both(int amount = 1)
        {
            var action = new DrawAction(new DrawProperties(amount));
            action.TargetBothPlayers();
            return action;
        }
        
        /// <summary>
        /// Create action to draw cards equal to bid
        /// </summary>
        public static DrawAction EqualToBid(Player target = null)
        {
            var action = new DrawAction(context => new DrawProperties(context.player.honorBid));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        #endregion
    }
}