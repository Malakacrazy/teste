using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player loses honor
    /// </summary>
    [System.Serializable]
    public partial class LoseHonorAction : PlayerAction
    {
        public Player player
        {
            get { return GetProperties(null).target?.Cast<Player>()?.FirstOrDefault(); }
            set { 
                var props = GetProperties(null);
                props.target.Clear();
                if (value != null) props.target.Add(value);
            }
        }
        
        public int amount
        {
            get { return (GetProperties(null) as LoseHonorProperties)?.amount ?? 0; }
            set { 
                var props = GetProperties(null) as LoseHonorProperties;
                if (props != null) props.amount = value;
            }
        }
        /// <summary>
        /// Properties specific to losing honor
        /// </summary>
        [System.Serializable]
        public class LoseHonorProperties : PlayerActionProperties
        {
            public int amount = 1;
            public bool dueToUnopposed = false;
            
            public LoseHonorProperties() : base() { }
            
            public LoseHonorProperties(int amount) : base()
            {
                this.amount = amount;
            }
            
            public LoseHonorProperties(int amount, bool dueToUnopposed) : base()
            {
                this.amount = amount;
                this.dueToUnopposed = dueToUnopposed;
            }
        }
        
        #region Constructors
        
        public LoseHonorAction() : base()
        {
            Initialize();
        }
        
        public LoseHonorAction(LoseHonorProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public LoseHonorAction(System.Func<AbilityContext, LoseHonorProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "loseHonor";
            eventName = EventNames.OnModifyHonor;
            effectMessage = "make {0} lose {1} honor";
            costMessage = "losing {1} honor";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new LoseHonorProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is LoseHonorProperties honorProps)
                return honorProps;
                
            // Convert base properties to LoseHonorProperties
            return new LoseHonorProperties()
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
            // Default to the acting player for losing honor
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("losing {1} honor", new object[] { properties.target, properties.amount });
        }
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("make {0} lose {1} honor", new object[] { properties.target, properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Can't lose 0 honor
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
            gameEvent.AddProperty("amount", -properties.amount); // Negative because it's a loss
            gameEvent.AddProperty("dueToUnopposed", properties.dueToUnopposed);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var amount = gameEvent.GetProperty("amount", -1);
            
            if (player != null)
            {
                player.ModifyHonor(amount);
                LogExecution("{0} lost {1} honor", player.name, -amount);
                return true;
            }
            return false;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action for player to lose specific amount of honor
        /// </summary>
        public static LoseHonorAction Amount(int amount, Player target = null)
        {
            var action = new LoseHonorAction(new LoseHonorProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for player to lose 1 honor
        /// </summary>
        public static LoseHonorAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Create action for player to lose honor due to unopposed conflict
        /// </summary>
        public static LoseHonorAction Unopposed(int amount = 1, Player target = null)
        {
            var action = new LoseHonorAction(new LoseHonorProperties(amount, true));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action for opponent to lose honor
        /// </summary>
        public static LoseHonorAction Opponent(int amount = 1)
        {
            var action = new LoseHonorAction(new LoseHonorProperties(amount));
            action.TargetOpponent();
            return action;
        }
        
        /// <summary>
        /// Create action for both players to lose honor
        /// </summary>
        public static LoseHonorAction Both(int amount = 1)
        {
            var action = new LoseHonorAction(new LoseHonorProperties(amount));
            action.TargetBothPlayers();
            return action;
        }
        
        #endregion
    }
}