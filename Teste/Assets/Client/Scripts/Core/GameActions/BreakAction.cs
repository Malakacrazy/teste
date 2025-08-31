using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Breaks a province card
    /// </summary>
    [System.Serializable]
    public partial class BreakAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to breaking provinces
        /// </summary>
        [System.Serializable]
        public class BreakProperties : CardActionProperties
        {
            public BreakProperties() : base() { }
        }
        
        #region Constructors
        
        public BreakAction() : base()
        {
            Initialize();
        }
        
        public BreakAction(BreakProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public BreakAction(System.Func<AbilityContext, BreakProperties> factory) : base(context => factory(context))
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "break";
            eventName = EventNames.OnBreakProvince;
            costMessage = "breaking {0}";
            effectMessage = "break {0}";
            targetTypes = new List<string> { CardTypes.Province };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new BreakProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is BreakProperties breakProps)
                return breakProps;
                
            // Convert base properties to BreakProperties
            return new BreakProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            // Only provinces can be broken
            if (!card.IsProvince())
                return false;
                
            // Already broken provinces cannot be broken again
            if (card.IsBroken())
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            // Add current conflict information
            if (context.game.currentConflict != null)
            {
                gameEvent.AddProperty("conflict", context.game.currentConflict);
            }
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var province = gameEvent.GetProperty("target") as ProvinceCard;
            
            if (province != null)
            {
                province.BreakProvince();
                LogExecution("Broke province {0}", province.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to break specific province
        /// </summary>
        public static BreakAction Province(ProvinceCard province)
        {
            var action = new BreakAction();
            action.SetDefaultTarget(context => province);
            return action;
        }
        
        /// <summary>
        /// Create action to break attacked province
        /// </summary>
        public static BreakAction AttackedProvince()
        {
            var action = new BreakAction();
            action.SetDefaultTarget(context => context.game.currentConflict?.attackedProvince);
            return action;
        }
        
        #endregion
    }
}