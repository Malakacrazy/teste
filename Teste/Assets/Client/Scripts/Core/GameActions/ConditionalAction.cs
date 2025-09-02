using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Executes one of two actions based on a condition
    /// </summary>
    [System.Serializable]
    public partial class ConditionalAction : GameAction
    {
        /// <summary>
        /// Properties specific to conditional actions
        /// </summary>
        [System.Serializable]
        public class ConditionalActionProperties : GameAction.GameActionProperties
        {
            public System.Func<AbilityContext, ConditionalActionProperties, bool> conditionFunction;
            public bool conditionValue;
            public bool useFunction = false;
            public GameAction trueGameAction;
            public GameAction falseGameAction;
            
            public ConditionalActionProperties() : base() { }
            
            public ConditionalActionProperties(bool condition, GameAction trueAction, GameAction falseAction) : base()
            {
                this.conditionValue = condition;
                this.useFunction = false;
                this.trueGameAction = trueAction;
                this.falseGameAction = falseAction;
            }
            
            public ConditionalActionProperties(System.Func<AbilityContext, ConditionalActionProperties, bool> condition, 
                GameAction trueAction, GameAction falseAction) : base()
            {
                this.conditionFunction = condition;
                this.useFunction = true;
                this.trueGameAction = trueAction;
                this.falseGameAction = falseAction;
            }
        }
        
        #region Constructors
        
        public ConditionalAction() : base()
        {
            Initialize();
        }
        
        public ConditionalAction(ConditionalActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ConditionalAction(System.Func<AbilityContext, ConditionalActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "conditional";
            eventName = EventNames.OnConditionalAction;
            effectMessage = "conditionally execute action";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new ConditionalActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is ConditionalActionProperties condProps)
            {
                // Set default targets for both actions
                condProps.trueGameAction?.SetDefaultTarget(ctx => condProps.target);
                condProps.falseGameAction?.SetDefaultTarget(ctx => condProps.target);
                return condProps;
            }
                
            // Convert base properties to ConditionalActionProperties
            return new ConditionalActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Action Selection
        
        /// <summary>
        /// Get the action to execute based on the condition
        /// </summary>
        public GameAction GetGameAction(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            bool condition;
            if (properties.useFunction && properties.conditionFunction != null)
            {
                condition = properties.conditionFunction(context, properties);
            }
            else
            {
                condition = properties.conditionValue;
            }
            
            return condition ? properties.trueGameAction : properties.falseGameAction;
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var selectedAction = GetGameAction(context, additionalProperties);
            return selectedAction?.GetEffectMessage(context, additionalProperties) ?? ("no effect", new object[0]);
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var selectedAction = GetGameAction(context, additionalProperties);
            return selectedAction?.CanAffect(target, context, additionalProperties) ?? false;
        }
        
        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var selectedAction = GetGameAction(context, additionalProperties);
            return selectedAction?.HasLegalTarget(context, additionalProperties) ?? false;
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var selectedAction = GetGameAction(context, additionalProperties);
            return selectedAction?.HasTargetsChosenByInitiatingPlayer(context, additionalProperties) ?? false;
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var selectedAction = GetGameAction(context, additionalProperties);
            selectedAction?.AddEventsToArray(events, context, additionalProperties);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create conditional action with boolean condition
        /// </summary>
        public static ConditionalAction Create(bool condition, GameAction trueAction, GameAction falseAction = null)
        {
            return new ConditionalAction(new ConditionalActionProperties(condition, trueAction, falseAction));
        }
        
        /// <summary>
        /// Create conditional action with function condition
        /// </summary>
        public static ConditionalAction Create(System.Func<AbilityContext, ConditionalActionProperties, bool> condition, 
            GameAction trueAction, GameAction falseAction = null)
        {
            return new ConditionalAction(new ConditionalActionProperties(condition, trueAction, falseAction));
        }
        
        /// <summary>
        /// Execute action if condition is true, otherwise do nothing
        /// </summary>
        public static ConditionalAction If(bool condition, GameAction action)
        {
            return Create(condition, action, null);
        }
        
        /// <summary>
        /// Execute action if condition function returns true, otherwise do nothing
        /// </summary>
        public static ConditionalAction If(System.Func<AbilityContext, ConditionalActionProperties, bool> condition, GameAction action)
        {
            return Create(condition, action, null);
        }
        
        /// <summary>
        /// Execute action if player has enough fate
        /// </summary>
        public static ConditionalAction IfPlayerHasFate(int amount, GameAction action)
        {
            return Create((context, props) => context.player.fate >= amount, action);
        }
        
        /// <summary>
        /// Execute action if it's a specific conflict type
        /// </summary>
        public static ConditionalAction IfConflictType(string conflictType, GameAction action)
        {
            return Create((context, props) => 
                context.game.currentConflict?.conflictType == conflictType, action);
        }
        
        /// <summary>
        /// Execute action if player is attacking
        /// </summary>
        public static ConditionalAction IfAttacking(GameAction action)
        {
            return Create((context, props) => context.player.IsAttackingPlayer(), action);
        }
        
        /// <summary>
        /// Execute action if player is defending
        /// </summary>
        public static ConditionalAction IfDefending(GameAction action)
        {
            return Create((context, props) => context.player.IsDefendingPlayer(), action);
        }
        
        #endregion
    }
}