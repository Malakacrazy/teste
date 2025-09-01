using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player gains honor
    /// </summary>
    [System.Serializable]
    public partial class GainHonorAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to gaining honor
        /// </summary>
        [System.Serializable]
        public class GainHonorProperties : PlayerActionProperties
        {
            public int amount = 1;
            
            public GainHonorProperties() : base() { }
            
            public GainHonorProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public GainHonorAction() : base()
        {
            Initialize();
        }
        
        public GainHonorAction(GainHonorProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public GainHonorAction(System.Func<AbilityContext, GainHonorProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        public GainHonorAction(Player player, int amount) : base(new GainHonorProperties { amount = amount })
        {
            if (player != null)
                GetProperties(null).target.Add(player);
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "gainHonor";
            eventName = EventNames.OnModifyHonor;
            effectMessage = "gain {0} honor";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new GainHonorProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is GainHonorProperties honorProps)
                return honorProps;
                
            // Convert base properties to GainHonorProperties
            return new GainHonorProperties()
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
            // Default to the acting player for gaining honor
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("gain {0} honor", new object[] { properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't gain 0 honor (but can gain negative honor - that's losing honor)
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
                player.ModifyHonor(amount);
                LogExecution("{0} gained {1} honor", player.name, amount);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action for player to gain specific amount of honor
        /// </summary>
        public static GainHonorAction Amount(int amount, Player target = null)
        {
            var action = new GainHonorAction(new GainHonorProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for player to gain 1 honor
        /// </summary>
        public static GainHonorAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Create action for both players to gain honor
        /// </summary>
        public static GainHonorAction Both(int amount = 1)
        {
            var action = new GainHonorAction(new GainHonorProperties(amount));
            action.TargetBothPlayers();
            return action;
        }
        
        #endregion
    }
}