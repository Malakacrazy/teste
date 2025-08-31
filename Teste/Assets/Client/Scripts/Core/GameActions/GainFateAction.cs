using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player gains fate
    /// </summary>
    [System.Serializable]
    public partial class GainFateAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to gaining fate
        /// </summary>
        [System.Serializable]
        public class GainFateProperties : PlayerActionProperties
        {
            public int amount = 1;
            
            public GainFateProperties() : base() { }
            
            public GainFateProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public GainFateAction() : base()
        {
            Initialize();
        }
        
        public GainFateAction(GainFateProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public GainFateAction(System.Func<AbilityContext, GainFateProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "gainFate";
            eventName = EventNames.OnModifyFate;
            effectMessage = "gain {0} fate";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new GainFateProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is GainFateProperties fateProps)
                return fateProps;
                
            // Convert base properties to GainFateProperties
            return new GainFateProperties()
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
            // Default to the acting player for gaining fate
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("gain {0} fate", new object[] { properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't gain 0 or negative fate
            if (properties.amount <= 0)
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
                player.ModifyFate(amount);
                LogExecution("{0} gained {1} fate", player.name, amount);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action for player to gain specific amount of fate
        /// </summary>
        public static GainFateAction Amount(int amount, Player target = null)
        {
            var action = new GainFateAction(new GainFateProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for player to gain 1 fate
        /// </summary>
        public static GainFateAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Create action for both players to gain fate
        /// </summary>
        public static GainFateAction Both(int amount = 1)
        {
            var action = new GainFateAction(new GainFateProperties(amount));
            action.TargetBothPlayers();
            return action;
        }
        
        #endregion
    }
}