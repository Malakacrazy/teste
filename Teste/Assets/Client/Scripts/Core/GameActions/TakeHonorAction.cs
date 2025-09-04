using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player takes honor from another player
    /// </summary>
    [System.Serializable]
    public partial class TakeHonorAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to taking honor
        /// </summary>
        [System.Serializable]
        public class TakeHonorProperties : PlayerActionProperties
        {
            public int amount = 1;
            public Player source;
            
            public TakeHonorProperties() : base() { }
            
            public TakeHonorProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public TakeHonorAction() : base()
        {
            Initialize();
        }
        
        public TakeHonorAction(TakeHonorProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TakeHonorAction(System.Func<AbilityContext, TakeHonorProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        public TakeHonorAction(Player player, Player target, int amount) : base(new TakeHonorProperties { amount = amount, source = target })
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
            actionName = "takeHonor";
            eventName = EventNames.OnModifyHonor;
            effectMessage = "take {0} honor";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new TakeHonorProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is TakeHonorProperties honorProps)
                return honorProps;
                
            // Convert base properties to TakeHonorProperties
            return new TakeHonorProperties()
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
            // Default to the acting player for taking honor
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("take {0} honor", new object[] { properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't take 0 honor
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
            gameEvent.AddProperty("source", properties.source);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var source = gameEvent.GetProperty("source") as Player;
            var amount = gameEvent.GetProperty("amount", 1);
            
            if (player != null && source != null)
            {
                source.ModifyHonor(-amount);
                player.ModifyHonor(amount);
                LogExecution("{0} took {1} honor from {2}", player.name, amount, source.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action for player to take specific amount of honor from another player
        /// </summary>
        public static TakeHonorAction Amount(int amount, Player target = null, Player source = null)
        {
            var action = new TakeHonorAction(new TakeHonorProperties(amount) { source = source });
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for player to take 1 honor from another player
        /// </summary>
        public static TakeHonorAction One(Player target = null, Player source = null)
        {
            return Amount(1, target, source);
        }
        
        #endregion
    }
}