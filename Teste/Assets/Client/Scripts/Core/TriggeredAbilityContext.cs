using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Specialized ability context for triggered abilities that includes event information.
    /// Extends AbilityContext with event-specific data and cancellation capabilities.
    /// </summary>
    public class TriggeredAbilityContext : AbilityContext
    {
        [Header("Triggered Ability Context")]
        public GameEvent triggerEvent;
        public List<GameEvent> triggerEvents = new List<GameEvent>();
        public bool eventCancelled = false;

        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public TriggeredAbilityContext() : base()
        {
            triggerEvents = new List<GameEvent>();
        }

        /// <summary>
        /// Constructor with triggered ability context properties
        /// </summary>
        /// <param name="properties">Triggered context properties</param>
        public TriggeredAbilityContext(TriggeredAbilityContextProperties properties) : base()
        {
            Initialize(properties);
        }

        /// <summary>
        /// Constructor with base properties and event
        /// </summary>
        /// <param name="baseProperties">Base ability context properties</param>
        /// <param name="gameEvent">Triggering event</param>
        public TriggeredAbilityContext(AbilityContextProperties baseProperties, GameEvent gameEvent) : base(baseProperties)
        {
            triggerEvent = gameEvent;
            eventObj = gameEvent;
            eventObject = gameEvent;
            
            if (gameEvent != null)
            {
                triggerEvents = new List<GameEvent> { gameEvent };
            }
        }

        /// <summary>
        /// Initialize the triggered ability context
        /// </summary>
        /// <param name="properties">Triggered context properties</param>
        public virtual void Initialize(TriggeredAbilityContextProperties properties)
        {
            if (properties == null)
                return;

            // Initialize base context
            base.Initialize(properties.BaseProperties);

            // Set triggered-specific properties
            triggerEvent = properties.Event;
            eventObj = properties.Event;
            eventObject = properties.Event;
            
            if (properties.Event != null)
            {
                triggerEvents = new List<GameEvent> { properties.Event };
            }

            if (properties.Events != null && properties.Events.Count > 0)
            {
                triggerEvents = new List<GameEvent>(properties.Events);
                // Set the first event as the primary trigger if not already set
                if (triggerEvent == null && triggerEvents.Count > 0)
                {
                    triggerEvent = triggerEvents[0];
                    eventObj = triggerEvent;
                    eventObject = triggerEvent;
                }
            }
        }

        /// <summary>
        /// Creates a copy of this triggered ability context with optional new properties
        /// </summary>
        /// <param name="newProps">Properties to override in the copy</param>
        /// <returns>New triggered ability context with modified properties</returns>
        public new TriggeredAbilityContext Copy(Dictionary<string, object> newProps = null)
        {
            var copyGO = new GameObject("TriggeredAbilityContext_Copy");
            var copy = copyGO.AddComponent<TriggeredAbilityContext>();
            
            // Copy base properties
            var baseProps = GetProps();
            copy.Initialize(baseProps);
            
            // Copy triggered-specific properties
            copy.triggerEvent = triggerEvent;
            copy.triggerEvents = triggerEvents != null ? new List<GameEvent>(triggerEvents) : new List<GameEvent>();
            copy.eventCancelled = eventCancelled;
            
            // Apply new properties if provided
            if (newProps != null)
            {
                ApplyNewProperties(copy, newProps);
            }
            
            return copy;
        }

        /// <summary>
        /// Apply new properties to a copied context
        /// </summary>
        /// <param name="context">Context to modify</param>
        /// <param name="newProps">Properties to apply</param>
        private void ApplyNewProperties(TriggeredAbilityContext context, Dictionary<string, object> newProps)
        {
            foreach (var kvp in newProps)
            {
                switch (kvp.Key.ToLower())
                {
                    case "event":
                        if (kvp.Value is GameEvent gameEvent)
                        {
                            context.triggerEvent = gameEvent;
                            context.eventObj = gameEvent;
                            context.eventObject = gameEvent;
                        }
                        break;
                    case "events":
                        if (kvp.Value is List<GameEvent> events)
                        {
                            context.triggerEvents = new List<GameEvent>(events);
                        }
                        break;
                    case "game":
                        context.game = kvp.Value as Game;
                        break;
                    case "source":
                        context.source = kvp.Value;
                        break;
                    case "player":
                        context.player = kvp.Value as Player;
                        break;
                    case "ability":
                        context.ability = kvp.Value as BaseAbility;
                        break;
                    case "stage":
                        context.stage = kvp.Value as string;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the properties that define this triggered context
        /// </summary>
        /// <returns>Properties object for creating copies</returns>
        public new TriggeredAbilityContextProperties GetProps()
        {
            var baseProps = base.GetProps();
            return new TriggeredAbilityContextProperties
            {
                BaseProperties = baseProps,
                Event = triggerEvent,
                Events = triggerEvents != null ? new List<GameEvent>(triggerEvents) : null
            };
        }

        /// <summary>
        /// Cancels the triggering event
        /// </summary>
        public virtual void Cancel()
        {
            if (triggerEvent != null && !triggerEvent.cancelled)
            {
                triggerEvent.Cancel();
                eventCancelled = true;
                
                Debug.Log($"⚡ Event {triggerEvent.name} cancelled by triggered ability");
            }
        }

        /// <summary>
        /// Cancels all triggering events
        /// </summary>
        public virtual void CancelAllEvents()
        {
            if (triggerEvents != null)
            {
                foreach (var evt in triggerEvents)
                {
                    if (evt != null && !evt.cancelled)
                    {
                        evt.Cancel();
                    }
                }
                eventCancelled = true;
            }
        }

        /// <summary>
        /// Gets the primary triggering event
        /// </summary>
        /// <returns>Primary triggering event</returns>
        public virtual GameEvent GetTriggerEvent()
        {
            return triggerEvent;
        }

        /// <summary>
        /// Gets all triggering events
        /// </summary>
        /// <returns>List of all triggering events</returns>
        public virtual List<GameEvent> GetTriggerEvents()
        {
            return triggerEvents ?? new List<GameEvent>();
        }

        /// <summary>
        /// Checks if the triggering event was cancelled
        /// </summary>
        /// <returns>True if event was cancelled</returns>
        public virtual bool IsEventCancelled()
        {
            return eventCancelled || (triggerEvent?.cancelled ?? false);
        }

        /// <summary>
        /// Gets event data from the triggering event
        /// </summary>
        /// <param name="key">Data key to retrieve</param>
        /// <returns>Event data value or null</returns>
        public virtual object GetEventData(string key)
        {
            if (triggerEvent?.data != null && triggerEvent.data.ContainsKey(key))
            {
                return triggerEvent.data[key];
            }
            return null;
        }

        /// <summary>
        /// Gets typed event data from the triggering event
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="key">Data key to retrieve</param>
        /// <returns>Typed event data or default</returns>
        public virtual T GetEventData<T>(string key)
        {
            var data = GetEventData(key);
            if (data is T typedData)
                return typedData;
            return default(T);
        }

        /// <summary>
        /// Checks if the triggering event has specific data
        /// </summary>
        /// <param name="key">Data key to check</param>
        /// <returns>True if event has the data</returns>
        public virtual bool HasEventData(string key)
        {
            return triggerEvent?.data?.ContainsKey(key) ?? false;
        }

        /// <summary>
        /// Gets the event name of the triggering event
        /// </summary>
        /// <returns>Event name</returns>
        public virtual string GetEventName()
        {
            return triggerEvent?.name ?? "Unknown Event";
        }

        /// <summary>
        /// Checks if this context was triggered by a specific event type
        /// </summary>
        /// <param name="eventName">Event name to check</param>
        /// <returns>True if triggered by the specified event type</returns>
        public virtual bool IsTriggeredBy(string eventName)
        {
            return GetEventName().Equals(eventName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if this context was triggered by any of the specified event types
        /// </summary>
        /// <param name="eventNames">Event names to check</param>
        /// <returns>True if triggered by any of the specified event types</returns>
        public virtual bool IsTriggeredByAny(params string[] eventNames)
        {
            var currentEventName = GetEventName();
            return eventNames.Any(name => currentEventName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the source of the triggering event
        /// </summary>
        /// <returns>Event source or null</returns>
        public virtual object GetEventSource()
        {
            return GetEventData("source");
        }

        /// <summary>
        /// Gets the target of the triggering event
        /// </summary>
        /// <returns>Event target or null</returns>
        public virtual object GetEventTarget()
        {
            return GetEventData("target");
        }

        /// <summary>
        /// Gets the player associated with the triggering event
        /// </summary>
        /// <returns>Event player or null</returns>
        public virtual Player GetEventPlayer()
        {
            return GetEventData<Player>("player");
        }

        /// <summary>
        /// Gets the card associated with the triggering event
        /// </summary>
        /// <returns>Event card or null</returns>
        public virtual BaseCard GetEventCard()
        {
            return GetEventData<BaseCard>("card");
        }

        /// <summary>
        /// Adds additional triggering event
        /// </summary>
        /// <param name="additionalEvent">Additional event that triggered this ability</param>
        public virtual void AddTriggerEvent(GameEvent additionalEvent)
        {
            if (additionalEvent != null)
            {
                if (triggerEvents == null)
                    triggerEvents = new List<GameEvent>();
                    
                if (!triggerEvents.Contains(additionalEvent))
                {
                    triggerEvents.Add(additionalEvent);
                }
            }
        }

        /// <summary>
        /// Creates a framework triggered context for system effects
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="contextPlayer">Player for the context</param>
        /// <param name="triggeringEvent">Event that triggered the ability</param>
        /// <returns>Framework triggered ability context</returns>
        public static TriggeredAbilityContext CreateFrameworkTriggeredContext(Game gameInstance, Player contextPlayer, GameEvent triggeringEvent)
        {
            var contextGO = new GameObject("FrameworkTriggeredContext");
            var context = contextGO.AddComponent<TriggeredAbilityContext>();
            
            var properties = new TriggeredAbilityContextProperties
            {
                BaseProperties = new AbilityContextProperties
                {
                    game = gameInstance,
                    player = contextPlayer,
                    source = new EffectSource(),
                    ability = new BaseAbility(),
                    stage = Stages.Effect
                },
                Event = triggeringEvent
            };
            
            context.Initialize(properties);
            return context;
        }

        /// <summary>
        /// Creates a triggered context for ability execution
        /// </summary>
        /// <param name="ability">Triggered ability</param>
        /// <param name="contextPlayer">Player executing the ability</param>
        /// <param name="triggeringEvent">Event that triggered the ability</param>
        /// <returns>Triggered ability context</returns>
        public static TriggeredAbilityContext CreateTriggeredContext(BaseAbility ability, Player contextPlayer, GameEvent triggeringEvent)
        {
            var contextGO = new GameObject("TriggeredAbilityContext");
            var context = contextGO.AddComponent<TriggeredAbilityContext>();
            
            var properties = new TriggeredAbilityContextProperties
            {
                BaseProperties = new AbilityContextProperties
                {
                    game = ability.game,
                    source = ability.source ?? ability.card,
                    player = contextPlayer,
                    ability = ability,
                    stage = Stages.PreTarget
                },
                Event = triggeringEvent
            };
            
            context.Initialize(properties);
            return context;
        }

        /// <summary>
        /// String representation of the triggered ability context
        /// </summary>
        /// <returns>String describing the context</returns>
        public override string ToString()
        {
            var eventName = GetEventName();
            var sourceName = source?.GetType().Name ?? "Unknown";
            var playerName = player?.name ?? "Unknown";
            
            return $"TriggeredAbilityContext[{stage}] - {sourceName} by {playerName} on {eventName}";
        }

        /// <summary>
        /// Debug method to log triggered context information
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public new void DebugLogContext()
        {
            base.DebugLogContext();
            Debug.Log($"   Trigger Event: {GetEventName()}");
            Debug.Log($"   Event Cancelled: {IsEventCancelled()}");
            Debug.Log($"   Trigger Events Count: {triggerEvents?.Count ?? 0}");
        }

        // Property aliases for compatibility
        public GameEvent Event => triggerEvent;
        public List<GameEvent> Events => triggerEvents;
        public bool EventCancelled => eventCancelled;
    }

    /// <summary>
    /// Properties for creating triggered ability contexts
    /// </summary>
    [Serializable]
    public class TriggeredAbilityContextProperties
    {
        public AbilityContextProperties BaseProperties;
        public GameEvent Event;
        public List<GameEvent> Events;

        public TriggeredAbilityContextProperties()
        {
            Events = new List<GameEvent>();
        }
    }

    /// <summary>
    /// Extension methods for triggered ability context functionality
    /// </summary>
    public static class TriggeredAbilityContextExtensions
    {
        /// <summary>
        /// Creates a new triggered context from a base context and event
        /// </summary>
        /// <param name="baseContext">Base ability context</param>
        /// <param name="triggerEvent">Triggering event</param>
        /// <returns>New triggered ability context</returns>
        public static TriggeredAbilityContext ToTriggeredContext(this AbilityContext baseContext, GameEvent triggerEvent)
        {
            var contextGO = new GameObject("TriggeredAbilityContext_Converted");
            var triggeredContext = contextGO.AddComponent<TriggeredAbilityContext>();
            
            var properties = new TriggeredAbilityContextProperties
            {
                BaseProperties = baseContext.GetProps(),
                Event = triggerEvent
            };
            
            triggeredContext.Initialize(properties);
            return triggeredContext;
        }

        /// <summary>
        /// Checks if a context is a triggered ability context
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if it's a triggered ability context</returns>
        public static bool IsTriggeredContext(this AbilityContext context)
        {
            return context is TriggeredAbilityContext;
        }

        /// <summary>
        /// Safely casts a context to triggered ability context
        /// </summary>
        /// <param name="context">Context to cast</param>
        /// <returns>Triggered ability context or null</returns>
        public static TriggeredAbilityContext AsTriggeredContext(this AbilityContext context)
        {
            return context as TriggeredAbilityContext;
        }

        /// <summary>
        /// Gets the event from a context (works with both regular and triggered contexts)
        /// </summary>
        /// <param name="context">Context to get event from</param>
        /// <returns>Game event or null</returns>
        public static GameEvent GetContextEvent(this AbilityContext context)
        {
            if (context is TriggeredAbilityContext triggeredContext)
            {
                return triggeredContext.GetTriggerEvent();
            }
            
            return context.GetEvent();
        }

        /// <summary>
        /// Checks if a context can cancel its triggering event
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if context can cancel events</returns>
        public static bool CanCancelEvent(this AbilityContext context)
        {
            if (context is TriggeredAbilityContext triggeredContext)
            {
                return triggeredContext.triggerEvent != null && !triggeredContext.IsEventCancelled();
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to cancel the triggering event if possible
        /// </summary>
        /// <param name="context">Context to cancel event for</param>
        /// <returns>True if event was cancelled</returns>
        public static bool TryCancelEvent(this AbilityContext context)
        {
            if (context is TriggeredAbilityContext triggeredContext && triggeredContext.CanCancelEvent())
            {
                triggeredContext.Cancel();
                return true;
            }
            
            return false;
        }
    }
}