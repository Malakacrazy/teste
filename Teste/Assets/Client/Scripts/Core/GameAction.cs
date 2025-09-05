using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Extensions;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game actions in L5R. Provides targeting, event creation, and execution framework.
    /// Game actions represent atomic operations that can affect players, cards, rings, or tokens.
    /// </summary>
    [System.Serializable]
    public abstract partial class GameAction : IGameAction
    {
        [Header("Game Action Configuration")]
        [SerializeField] protected string actionName = "";
        [SerializeField] protected string costMessage = "";
        [SerializeField] protected string effectMessage = "";
        [SerializeField] protected string eventName = EventNames.Unnamed;
        [SerializeField] protected List<string> targetTypes = new List<string>();
        
        // Properties
        protected GameActionProperties defaultProperties;
        protected System.Func<AbilityContext, GameActionProperties> propertyFactory;
        protected GameActionProperties staticProperties;
        
        // Delegates for customization
        protected System.Func<AbilityContext, List<object>> getDefaultTargetsFunc;
        
        /// <summary>
        /// Properties that define how this game action behaves
        /// </summary>
        [System.Serializable]
        public class GameActionProperties
        {
            public List<object> target = new List<object>();
            public bool cannotBeCancelled = false;
            public bool optional = false;
            public GameAction parentAction = null;
            
            // Additional properties storage for dynamic behavior
            private Dictionary<string, object> additionalProperties = new Dictionary<string, object>();
            
            public GameActionProperties()
            {
                target = new List<object>();
            }
            
            public GameActionProperties(List<object> targets, bool cannotBeCancelled = false, bool optional = false)
            {
                this.target = targets ?? new List<object>();
                this.cannotBeCancelled = cannotBeCancelled;
                this.optional = optional;
            }
            
            // Dictionary-like interface
            public bool ContainsKey(string key)
            {
                switch (key)
                {
                    case "target": return target != null;
                    case "cannotBeCancelled": return true;
                    case "optional": return true;
                    case "parentAction": return parentAction != null;
                    default: return additionalProperties.ContainsKey(key);
                }
            }
            
            public object this[string key]
            {
                get
                {
                    switch (key)
                    {
                        case "target": return target;
                        case "cannotBeCancelled": return cannotBeCancelled;
                        case "optional": return optional;
                        case "parentAction": return parentAction;
                        default: return additionalProperties.ContainsKey(key) ? additionalProperties[key] : null;
                    }
                }
                set
                {
                    switch (key)
                    {
                        case "target": target = value as List<object> ?? new List<object>(); break;
                        case "cannotBeCancelled": cannotBeCancelled = (bool)value; break;
                        case "optional": optional = (bool)value; break;
                        case "parentAction": parentAction = value as GameAction; break;
                        default: additionalProperties[key] = value; break;
                    }
                }
            }
        }
        
        #region Properties
        
        public string Name => actionName;
        public string Cost => costMessage;
        public string Effect => effectMessage;
        public string EventName => eventName;
        public List<string> TargetTypes => targetTypes;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Constructor with static properties
        /// </summary>
        protected GameAction(GameActionProperties properties = null)
        {
            Initialize();
            staticProperties = properties ?? new GameActionProperties();
        }
        
        /// <summary>
        /// Constructor with dynamic property factory
        /// </summary>
        protected GameAction(System.Func<AbilityContext, GameActionProperties> propertyFactory)
        {
            Initialize();
            this.propertyFactory = propertyFactory;
        }
        
        /// <summary>
        /// Initialize default values
        /// </summary>
        protected virtual void Initialize()
        {
            defaultProperties = new GameActionProperties
            {
                cannotBeCancelled = false,
                optional = false
            };
            
            getDefaultTargetsFunc = DefaultTargets;
            
            // Set up default values - can be overridden by subclasses
            actionName = GetType().Name.Replace("GameAction", "").Replace("Action", "");
            costMessage = "";
            effectMessage = "affects {0}";
            eventName = EventNames.Unnamed;
            targetTypes = new List<string>();
        }
        
        #endregion
        
        #region Target Management
        
        /// <summary>
        /// Get default targets for this action - override in subclasses
        /// </summary>
        protected virtual List<object> DefaultTargets(AbilityContext context)
        {
            return new List<object>();
        }
        
        /// <summary>
        /// Set custom default target function
        /// </summary>
        public void SetDefaultTarget(System.Func<AbilityContext, List<object>> targetFunc)
        {
            getDefaultTargetsFunc = targetFunc;
        }
        
        /// <summary>
        /// Set default target to a single object
        /// </summary>
        public void SetDefaultTarget(System.Func<AbilityContext, object> targetFunc)
        {
            getDefaultTargetsFunc = context => 
            {
                var target = targetFunc(context);
                return target != null ? new List<object> { target } : new List<object>();
            };
        }
        
        /// <summary>
        /// Get resolved properties for this action
        /// </summary>
        public virtual GameActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            // Start with default targets
            var properties = new GameActionProperties
            {
                target = getDefaultTargetsFunc(context)
            };
            
            // Apply default properties
            ApplyProperties(properties, defaultProperties);
            
            // Apply additional properties if provided
            if (additionalProperties != null)
            {
                ApplyProperties(properties, additionalProperties);
            }
            
            // Apply factory or static properties
            if (propertyFactory != null)
            {
                ApplyProperties(properties, propertyFactory(context));
            }
            else if (staticProperties != null)
            {
                ApplyProperties(properties, staticProperties);
            }
            
            // Ensure target is a list and filter out nulls
            if (properties.target == null)
                properties.target = new List<object>();
                
            properties.target = properties.target.Where(t => t != null).ToList();
            
            return properties;
        }
        
        /// <summary>
        /// Apply properties from source to target
        /// </summary>
        private void ApplyProperties(GameActionProperties target, GameActionProperties source)
        {
            if (source == null) return;
            
            if (source.target?.Count > 0)
                target.target = source.target;
                
            if (source.cannotBeCancelled)
                target.cannotBeCancelled = source.cannotBeCancelled;
                
            if (source.optional)
                target.optional = source.optional;
                
            if (source.parentAction != null)
                target.parentAction = source.parentAction;
        }
        
        #endregion
        
        #region Messaging
        
        /// <summary>
        /// Get cost message for this action
        /// </summary>
        public virtual (string message, object[] args) GetCostMessage(AbilityContext context)
        {
            return (costMessage, new object[0]);
        }
        
        /// <summary>
        /// Get effect message for this action
        /// </summary>
        public virtual (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return (effectMessage, new object[] { properties.target });
        }
        
        #endregion
        
        #region Targeting Validation
        
        /// <summary>
        /// Check if this action can affect a specific target
        /// </summary>
        public virtual bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (target == null) return false;
            
            var properties = GetProperties(context, additionalProperties);
            
            // Check if target type is supported
            string targetType = GetTargetType(target);
            if (!targetTypes.IsNullOrEmpty() && !targetTypes.Contains(targetType))
                return false;
            
            // Check for action loops
            if (context.gameActionsResolutionChain.Contains(this))
                return false;
            
            // Check restrictions based on stage and cancellation
            if (context.stage == Stages.Effect && properties.cannotBeCancelled)
                return true;
                
            // Check target-specific restrictions
            return CheckTargetRestrictions(target, context);
        }
        
        /// <summary>
        /// Check target-specific restrictions - override in subclasses
        /// </summary>
        protected virtual bool CheckTargetRestrictions(object target, AbilityContext context)
        {
            // Try to call CheckRestrictions if the target supports it
            if (target is BaseCard card)
                return card.CheckRestrictions(actionName, context);
            if (target is Player player)
                return player.CheckRestrictions(actionName, context);
            if (target is Ring ring)
                return ring.CheckRestrictions(actionName, context);
                
            return true;
        }
        
        /// <summary>
        /// Get target type string for an object
        /// </summary>
        protected virtual string GetTargetType(object target)
        {
            return target switch
            {
                BaseCard card => card.type,
                Player _ => "player",
                Ring _ => "ring",
                StatusToken _ => "token",
                _ => target?.GetType().Name.ToLower() ?? "unknown"
            };
        }
        
        /// <summary>
        /// Check if this action has at least one legal target
        /// </summary>
        public virtual bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.target.Any(target => CanAffect(target, context, additionalProperties));
        }
        
        /// <summary>
        /// Check if all targets are legal
        /// </summary>
        public virtual bool AllTargetsLegal(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.target.All(target => CanAffect(target, context, additionalProperties));
        }
        
        /// <summary>
        /// Check if this action is optional
        /// </summary>
        public virtual bool IsOptional(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.optional;
        }
        
        #endregion
        
        #region Event Creation and Management
        
        /// <summary>
        /// Add events to array for all legal targets
        /// </summary>
        public virtual void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            foreach (var target in properties.target.Where(t => CanAffect(t, context, additionalProperties)))
            {
                var gameEvent = GetEvent(target, context, additionalProperties);
                events.Add(gameEvent);
            }
        }
        
        /// <summary>
        /// Get event for a specific target
        /// </summary>
        public virtual GameEvent GetEvent(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var gameEvent = CreateEvent(target, context, additionalProperties);
            UpdateEvent(gameEvent, target, context, additionalProperties);
            return gameEvent;
        }
        
        /// <summary>
        /// Create base event - override in subclasses for specific event types
        /// </summary>
        protected virtual GameEvent CreateEvent(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var gameEvent = new GameEvent(EventNames.Unnamed, new Dictionary<string, object> 
            { 
                { "cannotBeCancelled", properties.cannotBeCancelled } 
            }, (evt) => { /* Default handler */ });
            
            gameEvent.SetCheckFullyResolvedFunc((evt) => 
                IsEventFullyResolved(evt, target, context, additionalProperties));
                
            return gameEvent;
        }
        
        /// <summary>
        /// Update event with action-specific properties
        /// </summary>
        protected virtual void UpdateEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            // gameEvent.name is read-only, use eventName property instead
            AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.ReplaceHandler((evt) => { EventHandler(evt, additionalProperties); });
            gameEvent.SetCondition((evt) => CheckEventCondition(evt, additionalProperties));
        }
        
        /// <summary>
        /// Add properties to event - override in subclasses
        /// </summary>
        protected virtual void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            gameEvent.context = context;
            gameEvent.AddProperty("target", target);
        }
        
        /// <summary>
        /// Event handler - override in subclasses
        /// </summary>
        protected virtual bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            // Default implementation - subclasses should override
            Debug.Log($"🎬 GameAction '{actionName}' event handler executed");
            return true;
        }
        
        /// <summary>
        /// Check event condition - override in subclasses
        /// </summary>
        protected virtual bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            return true;
        }
        
        /// <summary>
        /// Check if event is fully resolved - override in subclasses
        /// </summary>
        protected virtual bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return !gameEvent.IsCancelled() && gameEvent.name == eventName;
        }
        
        #endregion
        
        #region Execution
        
        /// <summary>
        /// Resolve this action for a specific target
        /// </summary>
        public virtual void Resolve(object target, AbilityContext context)
        {
            if (target != null)
            {
                SetDefaultTarget(ctx => target);
            }
            
            var events = new List<GameEvent>();
            AddEventsToArray(events, context);
            
            context.game.QueueSimpleStep(() => 
            {
                context.game.OpenEventWindow(events);
                return true;
            });
        }
        
        /// <summary>
        /// Get all events that would be created by this action
        /// </summary>
        public virtual List<GameEvent> GetEventArray(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var events = new List<GameEvent>();
            AddEventsToArray(events, context, additionalProperties);
            return events;
        }
        
        #endregion
        
        #region Specialized Helper Methods
        
        /// <summary>
        /// Helper for fate movement validation (commonly used in L5R)
        /// </summary>
        protected virtual bool MoveFateEventCondition(GameEvent gameEvent)
        {
            var origin = gameEvent.GetProperty("origin") as BaseCard;
            var recipient = gameEvent.GetProperty("recipient") as BaseCard;
            
            if (origin != null)
            {
                if (origin.fate == 0)
                    return false;
                    
                if (origin.IsCharacter() && !origin.AllowGameAction("removeFate", gameEvent.context))
                    return false;
            }
            
            if (recipient != null)
            {
                if (recipient.IsCharacter() && !recipient.AllowGameAction("placeFate", gameEvent.context))
                    return false;
            }
            
            return origin != null || recipient != null;
        }
        
        /// <summary>
        /// Helper for fate movement execution (commonly used in L5R)
        /// </summary>
        protected virtual void MoveFateEventHandler(GameEvent gameEvent)
        {
            var origin = gameEvent.GetProperty("origin") as BaseCard;
            var recipient = gameEvent.GetProperty("recipient") as BaseCard;
            int fate = (int)(gameEvent.GetProperty("fate") ?? 0);
            
            if (origin != null)
            {
                fate = Mathf.Min(fate, origin.fate);
                origin.ModifyFate(-fate);
                gameEvent.SetProperty("fate", fate);
            }
            
            if (recipient != null)
            {
                recipient.ModifyFate(fate);
            }
        }
        
        /// <summary>
        /// Check if targets are chosen by initiating player - override in subclasses
        /// </summary>
        public virtual bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return false;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action with static targets
        /// </summary>
        public static T CreateWithTargets<T>(params object[] targets) where T : GameAction, new()
        {
            var action = new T();
            action.SetDefaultTarget(context => targets.ToList());
            return action;
        }
        
        /// <summary>
        /// Create action with dynamic target selection
        /// </summary>
        public static T CreateWithTargetSelector<T>(System.Func<AbilityContext, List<object>> targetSelector) where T : GameAction, new()
        {
            var action = new T();
            action.SetDefaultTarget(targetSelector);
            return action;
        }
        
        /// <summary>
        /// Create action with properties
        /// </summary>
        public static T CreateWithProperties<T>(GameActionProperties properties) where T : GameAction, new()
        {
            return new T() { staticProperties = properties };
        }
        
        #endregion
        
        #region Abstract and Virtual Methods
        
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information about this action
        /// </summary>
        public virtual string GetDebugInfo(AbilityContext context)
        {
            var properties = GetProperties(context);
            return $"GameAction '{actionName}': " +
                   $"Targets: {properties.target.Count}, " +
                   $"Optional: {properties.optional}, " +
                   $"Cannot be cancelled: {properties.cannotBeCancelled}";
        }
        
        /// <summary>
        /// Log action execution
        /// </summary>
        protected virtual void LogExecution(string message, params object[] args)
        {
            Debug.Log($"🎬 {actionName}: {string.Format(message, args)}");
        }
        
        public override string ToString()
        {
            return $"GameAction[{actionName}]";
        }
        
        #endregion
    }
    
    // EventNames are defined in Constants.cs
}