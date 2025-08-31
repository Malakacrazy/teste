using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player loses fate
    /// </summary>
    [System.Serializable]
    public partial class LoseFateAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to losing fate
        /// </summary>
        [System.Serializable]
        public class LoseFateProperties : PlayerActionProperties
        {
            public int amount = 1;
            
            public LoseFateProperties() : base() { }
            
            public LoseFateProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public LoseFateAction() : base()
        {
            Initialize();
        }
        
        public LoseFateAction(LoseFateProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public LoseFateAction(System.Func<AbilityContext, LoseFateProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "spendFate";
            eventName = EventNames.OnModifyFate;
            effectMessage = "make {0} lose {1} fate";
            costMessage = "spending {1} fate";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new LoseFateProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is LoseFateProperties fateProps)
                return fateProps;
                
            // Convert base properties to LoseFateProperties
            return new LoseFateProperties()
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
            // Default to the acting player for losing fate (like spending fate)
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("make {0} lose {1} fate", new object[] { properties.target, properties.amount });
        }
        
        public override (string message, object[] args) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("spending {1} fate", new object[] { properties.target, properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't lose 0 or negative fate
            if (properties.amount <= 0)
                return false;
                
            // Player must have fate to lose
            if (player.fate <= 0)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("amount", -properties.amount); // Negative because it's a loss
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var amount = gameEvent.GetProperty("amount", -1);
            
            if (player != null)
            {
                player.ModifyFate(amount);
                LogExecution("{0} lost {1} fate", player.name, -amount);
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action for player to lose specific amount of fate
        /// </summary>
        public static LoseFateAction Amount(int amount, Player target = null)
        {
            var action = new LoseFateAction(new LoseFateProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for player to lose 1 fate
        /// </summary>
        public static LoseFateAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Create action for opponent to lose fate
        /// </summary>
        public static LoseFateAction Opponent(int amount = 1)
        {
            var action = new LoseFateAction(new LoseFateProperties(amount));
            action.TargetOpponent();
            return action;
        }
        
        #endregion
    }
}