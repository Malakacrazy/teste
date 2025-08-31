using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Executes a custom handler function as a game action
    /// </summary>
    [System.Serializable]
    public partial class HandlerAction : GameAction
    {
        /// <summary>
        /// Properties specific to handler actions
        /// </summary>
        [System.Serializable]
        public class HandlerProperties : GameActionProperties
        {
            [System.NonSerialized]
            public System.Action<AbilityContext> handler;
            public bool hasTargetsChosenByInitiatingPlayer = false;
            
            public HandlerProperties() : base() 
            {
                handler = (context) => { }; // Default empty handler
            }
            
            public HandlerProperties(System.Action<AbilityContext> handler, bool hasTargetsChosenByInitiatingPlayer = false) : base()
            {
                this.handler = handler ?? ((context) => { });
                this.hasTargetsChosenByInitiatingPlayer = hasTargetsChosenByInitiatingPlayer;
            }
        }
        
        #region Constructors
        
        public HandlerAction() : base()
        {
            Initialize();
        }
        
        public HandlerAction(HandlerProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public HandlerAction(System.Func<AbilityContext, HandlerProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        public HandlerAction(System.Action<AbilityContext> handler, bool hasTargetsChosenByInitiatingPlayer = false) : base()
        {
            Initialize();
            staticProperties = new HandlerProperties(handler, hasTargetsChosenByInitiatingPlayer);
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "handler";
            eventName = EventNames.OnHandlerAction;
            effectMessage = "execute custom effect";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new HandlerProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is HandlerProperties handlerProps)
                return handlerProps;
                
            // Convert base properties to HandlerProperties
            return new HandlerProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Targeting
        
        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            // Handler actions always have a legal target (themselves)
            return true;
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.hasTargetsChosenByInitiatingPlayer;
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            events.Add(GetEvent(null, context, additionalProperties));
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(gameEvent.context, additionalProperties);
            
            try
            {
                properties.handler?.Invoke(gameEvent.context);
                LogExecution("Executed custom handler");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing handler action: {ex.Message}");
                return false;
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create handler action with custom function
        /// </summary>
        public static HandlerAction Create(System.Action<AbilityContext> handler, bool hasTargetsChosenByInitiatingPlayer = false)
        {
            return new HandlerAction(handler, hasTargetsChosenByInitiatingPlayer);
        }
        
        /// <summary>
        /// Create handler action that logs a message
        /// </summary>
        public static HandlerAction LogMessage(string message)
        {
            return Create(context => context.game.AddMessage(message));
        }
        
        /// <summary>
        /// Create handler action that logs a formatted message
        /// </summary>
        public static HandlerAction LogMessage(string format, params object[] args)
        {
            return Create(context => context.game.AddMessage(format, args));
        }
        
        /// <summary>
        /// Create handler action that executes if player is attacking
        /// </summary>
        public static HandlerAction IfAttacking(System.Action<AbilityContext> handler)
        {
            return Create(context =>
            {
                if (context.player.IsAttackingPlayer())
                    handler(context);
            });
        }
        
        /// <summary>
        /// Create handler action that executes if player is defending
        /// </summary>
        public static HandlerAction IfDefending(System.Action<AbilityContext> handler)
        {
            return Create(context =>
            {
                if (context.player.IsDefendingPlayer())
                    handler(context);
            });
        }
        
        /// <summary>
        /// Create handler action that executes during specific conflict type
        /// </summary>
        public static HandlerAction IfConflictType(string conflictType, System.Action<AbilityContext> handler)
        {
            return Create(context =>
            {
                if (context.game.currentConflict?.conflictType == conflictType)
                    handler(context);
            });
        }
        
        /// <summary>
        /// Create handler action that modifies a card's stats temporarily
        /// </summary>
        public static HandlerAction ModifyStats(BaseCard card, int militaryBonus = 0, int politicalBonus = 0)
        {
            return Create(context =>
            {
                if (militaryBonus != 0)
                    card.AddStatModifier("military", militaryBonus);
                if (politicalBonus != 0)
                    card.AddStatModifier("political", politicalBonus);
            });
        }
        
        /// <summary>
        /// Create handler action that triggers when specific condition is met
        /// </summary>
        public static HandlerAction OnCondition(System.Func<AbilityContext, bool> condition, System.Action<AbilityContext> handler)
        {
            return Create(context =>
            {
                if (condition(context))
                    handler(context);
            });
        }
        
        /// <summary>
        /// Create handler action that does nothing (placeholder)
        /// </summary>
        public static HandlerAction DoNothing()
        {
            return Create(context => { });
        }
        
        /// <summary>
        /// Create handler action that adds a delayed effect
        /// </summary>
        public static HandlerAction AddDelayedEffect(GameAction delayedAction, string triggerEvent)
        {
            return Create(context =>
            {
                // Add delayed effect logic here
                // This would integrate with the effect engine
                LogExecution("Added delayed effect for {0}", triggerEvent);
            });
        }
        
        #endregion
    }
}