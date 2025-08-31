using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Cancels an event or ability, optionally replacing it with another action
    /// </summary>
    [System.Serializable]
    public partial class CancelAction : GameAction
    {
        /// <summary>
        /// Properties specific to cancel actions
        /// </summary>
        [System.Serializable]
        public class CancelActionProperties : GameActionProperties
        {
            public GameAction replacementGameAction;
            
            public CancelActionProperties() : base() { }
            
            public CancelActionProperties(GameAction replacementAction) : base()
            {
                this.replacementGameAction = replacementAction;
            }
        }
        
        #region Constructors
        
        public CancelAction() : base()
        {
            Initialize();
        }
        
        public CancelAction(CancelActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public CancelAction(System.Func<AbilityContext, CancelActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "cancel";
            eventName = EventNames.OnCancel;
            effectMessage = "cancel the effects of {0}";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new CancelActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is CancelActionProperties cancelProps)
            {
                // Set default target for replacement action if needed
                if (cancelProps.replacementGameAction != null)
                {
                    cancelProps.replacementGameAction.SetDefaultTarget(ctx => cancelProps.target);
                }
                return cancelProps;
            }
                
            // Convert base properties to CancelActionProperties
            return new CancelActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Default Targets
        
        protected override List<object> DefaultTargets(AbilityContext context)
        {
            // For triggered ability contexts, default to the event card
            if (context is TriggeredAbilityContext triggeredContext && triggeredContext.eventObject?.card != null)
            {
                return new List<object> { triggeredContext.eventObject.card };
            }
            
            return new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.replacementGameAction != null && context is TriggeredAbilityContext triggeredContext)
            {
                return ("{1} {0} instead of {2}", new object[] 
                { 
                    context.target, 
                    properties.replacementGameAction.Name,
                    triggeredContext.eventObject?.card
                });
            }
            
            return ("cancel the effects of {0}", new object[] { properties.target });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(context is TriggeredAbilityContext triggeredContext))
                return false;
                
            var eventObj = triggeredContext.eventObject;
            if (eventObj == null || eventObj.IsCancelled())
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Check if event can be cancelled
            if (eventObj.CannotBeCancelled())
                return false;
                
            // Check if replacement action has legal targets
            if (properties.replacementGameAction != null)
            {
                return properties.replacementGameAction.HasLegalTarget(context, additionalProperties);
            }
            
            return true;
        }
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(context is TriggeredAbilityContext triggeredContext))
                return false;
                
            var eventObj = triggeredContext.eventObject;
            if (eventObj == null || eventObj.CannotBeCancelled())
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Check replacement action if present
            if (properties.replacementGameAction != null)
            {
                return properties.replacementGameAction.CanAffect(target, context, additionalProperties);
            }
            
            return true;
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            return properties.replacementGameAction?.HasTargetsChosenByInitiatingPlayer(context, additionalProperties) ?? false;
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var gameEvent = CreateEvent(null, context, additionalProperties);
            AddPropertiesToEvent(gameEvent, null, context, additionalProperties);
            gameEvent.SetHandler(eventInstance => EventHandler(eventInstance, additionalProperties));
            events.Add(gameEvent);
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var context = gameEvent.context;
            var properties = GetProperties(context, additionalProperties);
            
            // Handle replacement action if present
            if (properties.replacementGameAction != null && context is TriggeredAbilityContext triggeredContext)
            {
                var events = new List<GameEvent>();
                var eventWindow = triggeredContext.eventObject?.window;
                
                // Create replacement events
                var replacementProperties = new Dictionary<string, object> { { "replacementEffect", true } };
                if (additionalProperties != null)
                {
                    foreach (var kvp in replacementProperties)
                    {
                        // Merge additional properties
                    }
                }
                
                properties.replacementGameAction.AddEventsToArray(events, context, additionalProperties);
                
                context.game.QueueSimpleStep(() =>
                {
                    // Set replacement event if not a sacrifice and single event
                    if (!triggeredContext.eventObject.IsSacrifice() && events.Count == 1)
                    {
                        triggeredContext.eventObject.SetReplacementEvent(events[0]);
                    }
                    
                    // Add all replacement events to the window
                    foreach (var newEvent in events)
                    {
                        eventWindow?.AddEvent(newEvent);
                    }
                    
                    return true;
                });
            }
            
            // Cancel the original event
            if (context is TriggeredAbilityContext trigContext)
            {
                trigContext.Cancel();
                LogExecution("Cancelled event for {0}", trigContext.eventObject?.card?.name ?? "unknown");
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to cancel without replacement
        /// </summary>
        public static CancelAction Event()
        {
            return new CancelAction();
        }
        
        /// <summary>
        /// Create action to cancel and replace with another action
        /// </summary>
        public static CancelAction Replace(GameAction replacementAction)
        {
            return new CancelAction(new CancelActionProperties(replacementAction));
        }
        
        /// <summary>
        /// Create action to cancel specific card's effect
        /// </summary>
        public static CancelAction Card(BaseCard card)
        {
            var action = new CancelAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        #endregion
    }
}