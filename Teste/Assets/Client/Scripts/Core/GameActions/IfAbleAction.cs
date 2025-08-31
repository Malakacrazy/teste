using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Executes an action if able, otherwise executes an alternative action
    /// </summary>
    [System.Serializable]
    public partial class IfAbleAction : GameAction
    {
        /// <summary>
        /// Properties specific to if able actions
        /// </summary>
        [System.Serializable]
        public class IfAbleActionProperties : GameActionProperties
        {
            public GameAction ifAbleAction;
            public GameAction otherwiseAction;
            
            public IfAbleActionProperties() : base() { }
            
            public IfAbleActionProperties(GameAction ifAbleAction, GameAction otherwiseAction = null) : base()
            {
                this.ifAbleAction = ifAbleAction;
                this.otherwiseAction = otherwiseAction;
            }
        }
        
        #region Constructors
        
        public IfAbleAction() : base()
        {
            Initialize();
        }
        
        public IfAbleAction(IfAbleActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public IfAbleAction(System.Func<AbilityContext, IfAbleActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "ifAble";
            eventName = EventNames.OnIfAbleAction;
            effectMessage = "execute action if able";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new IfAbleActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is IfAbleActionProperties ifAbleProps)
            {
                // Set default targets for both actions
                ifAbleProps.ifAbleAction?.SetDefaultTarget(ctx => ifAbleProps.target);
                ifAbleProps.otherwiseAction?.SetDefaultTarget(ctx => ifAbleProps.target);
                return ifAbleProps;
            }
                
            // Convert base properties to IfAbleActionProperties
            return new IfAbleActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.ifAbleAction?.HasLegalTarget(context, additionalProperties) == true)
            {
                return properties.ifAbleAction.GetEffectMessage(context, additionalProperties);
            }
            else if (properties.otherwiseAction != null)
            {
                return properties.otherwiseAction.GetEffectMessage(context, additionalProperties);
            }
            
            return ("no legal targets", new object[0]);
        }
        
        #endregion
        
        #region Targeting
        
        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            return (properties.ifAbleAction?.HasLegalTarget(context, additionalProperties) == true) ||
                   (properties.otherwiseAction?.HasLegalTarget(context, additionalProperties) == true);
        }
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            return (properties.ifAbleAction?.CanAffect(target, context, additionalProperties) == true) ||
                   (properties.otherwiseAction?.CanAffect(target, context, additionalProperties) == true);
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            return (properties.ifAbleAction?.HasTargetsChosenByInitiatingPlayer(context, additionalProperties) == true) ||
                   (properties.otherwiseAction?.HasTargetsChosenByInitiatingPlayer(context, additionalProperties) == true);
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            // Try to execute the primary action if it has legal targets
            if (properties.ifAbleAction?.HasLegalTarget(context, additionalProperties) == true)
            {
                properties.ifAbleAction.AddEventsToArray(events, context, additionalProperties);
            }
            // Otherwise, execute the alternative action if it exists and has legal targets
            else if (properties.otherwiseAction?.HasLegalTarget(context, additionalProperties) == true)
            {
                properties.otherwiseAction.AddEventsToArray(events, context, additionalProperties);
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Execute action if able, otherwise do nothing
        /// </summary>
        public static IfAbleAction Execute(GameAction action)
        {
            return new IfAbleAction(new IfAbleActionProperties(action, null));
        }
        
        /// <summary>
        /// Execute primary action if able, otherwise execute alternative action
        /// </summary>
        public static IfAbleAction ExecuteOrElse(GameAction primaryAction, GameAction alternativeAction)
        {
            return new IfAbleAction(new IfAbleActionProperties(primaryAction, alternativeAction));
        }
        
        /// <summary>
        /// Try to bow character, otherwise do nothing
        /// </summary>
        public static IfAbleAction TryBow(BaseCard character)
        {
            var bowAction = GameActions.Bow();
            bowAction.SetDefaultTarget(context => character);
            return Execute(bowAction);
        }
        
        /// <summary>
        /// Try to ready character, otherwise do nothing
        /// </summary>
        public static IfAbleAction TryReady(BaseCard character)
        {
            var readyAction = GameActions.Ready();
            readyAction.SetDefaultTarget(context => character);
            return Execute(readyAction);
        }
        
        /// <summary>
        /// Try to honor character, otherwise dishonor if possible
        /// </summary>
        public static IfAbleAction TryHonorElseDishonor(BaseCard character)
        {
            var honorAction = GameActions.Honor();
            honorAction.SetDefaultTarget(context => character);
            
            var dishonorAction = GameActions.Dishonor();
            dishonorAction.SetDefaultTarget(context => character);
            
            return ExecuteOrElse(honorAction, dishonorAction);
        }
        
        /// <summary>
        /// Try to place fate, otherwise remove fate
        /// </summary>
        public static IfAbleAction TryPlaceFateElseRemove(BaseCard character, int amount = 1)
        {
            var placeAction = GameActions.PlaceFate();
            placeAction.SetDefaultTarget(context => character);
            
            var removeAction = GameActions.RemoveFate();
            removeAction.SetDefaultTarget(context => character);
            
            return ExecuteOrElse(placeAction, removeAction);
        }
        
        #endregion
    }
}