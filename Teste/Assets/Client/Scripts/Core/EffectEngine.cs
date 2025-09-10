using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a game effect with timing, targets, and conditions
    /// </summary>
    [System.Serializable]
    public class GameEffect
    {
        [Header("Effect Identity")]
        public string id;
        public string duration;
        public EffectSource source;
        public List<GameObject> targets = new List<GameObject>();
        public GameObject match;

        [Header("Effect Properties")]
        public object effect;
        public object condition;
        public AbilityContext context;
        public Dictionary<string, object> until;
        public bool multipleTrigger = false;
        public bool canChangeZoneOnce = false;

        [Header("State")]
        public bool isActive = true;
        public DateTime appliedAt;

        /// <summary>
        /// Initialize the game effect
        /// </summary>
        /// <param name="effectSource">Source of the effect</param>
        /// <param name="effectDuration">Duration type</param>
        /// <param name="effectTargets">Target objects</param>
        /// <param name="effectProperties">Effect properties</param>
        public void Initialize(EffectSource effectSource, string effectDuration, 
                             List<GameObject> effectTargets, object effectProperties)
        {
            id = Guid.NewGuid().ToString();
            source = effectSource;
            duration = effectDuration;
            targets = effectTargets ?? new List<GameObject>();
            effect = effectProperties;
            appliedAt = DateTime.Now;
            isActive = true;
        }

        /// <summary>
        /// Check if this effect's condition is met and update targets
        /// </summary>
        /// <param name="stateChanged">Whether game state has changed</param>
        /// <returns>True if state changed as a result</returns>
        public bool CheckCondition(bool stateChanged)
        {
            try
            {
                // If effect has a condition function, evaluate it
                if (condition is System.Func<AbilityContext, bool> conditionFunc)
                {
                    bool conditionMet = conditionFunc(context);
                    
                    if (isActive != conditionMet)
                    {
                        isActive = conditionMet;
                        return true;
                    }
                }

                // Check if targets are still valid
                var validTargets = targets.Where(target => target != null && IsValidTarget(target)).ToList();
                if (validTargets.Count != targets.Count)
                {
                    targets = validTargets;
                    return true;
                }

                return stateChanged;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error checking effect condition: {e.Message}");
                return stateChanged;
            }
        }

        /// <summary>
        /// Check if a target is still valid for this effect
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <returns>True if target is valid</returns>
        private bool IsValidTarget(GameObject target)
        {
            // Basic validation - target exists and meets match criteria
            if (target == null) return false;
            
            // If there's a match object, target must match it
            if (match != null && target != match) return false;
            
            return true;
        }

        /// <summary>
        /// Cancel this effect and clean up
        /// </summary>
        public void Cancel()
        {
            isActive = false;
            
            // Remove effect from targets
            foreach (var target in targets.ToList())
            {
                if (target != null)
                {
                    // Remove effect from target if supported
                    if (target is IEffectTarget effectTarget)
                    {
                        effectTarget.RemoveEffect(this);
                    }
                }
            }
            
            targets.Clear();
        }

        /// <summary>
        /// Get debug information about this effect
        /// </summary>
        /// <returns>Debug info object</returns>
        public object GetDebugInfo()
        {
            return new
            {
                id = id,
                source = source?.name ?? "Unknown",
                duration = duration,
                targetCount = targets.Count,
                isActive = isActive,
                appliedAt = appliedAt.ToString("HH:mm:ss"),
                effectType = effect?.GetType().Name ?? "Unknown"
            };
        }
    }

    /// <summary>
    /// Represents a delayed effect that triggers on specific events
    /// </summary>
    public class DelayedEffect
    {
        public string type = EffectNames.DelayedEffect;
        public System.Func<AbilityContext, bool> condition;
        public Dictionary<string, System.Func<GameEvent, AbilityContext, bool>> when;
        public IGameAction gameAction;
        public string message;
        public object messageArgs;
        public bool multipleTrigger = false;

        public object GetValue()
        {
            return this;
        }
    }

    /// <summary>
    /// Manages all effects in the game including timing and event handling
    /// </summary>
    public class EffectEngine : MonoBehaviour
    {
        [Header("Effect Engine")]
        [SerializeField] public List<GameEffect> effects = new List<GameEffect>();
        [SerializeField] private List<CustomDurationEvent> customDurationEvents = new List<CustomDurationEvent>();
        [SerializeField] private bool newEffect = false;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool enableEffectTracing = false;

        /// <summary>
        /// Reference to the game instance
        /// </summary>
        public Game game { get; private set; }

        /// <summary>
        /// Event system for handling game events
        /// </summary>
        public EventRegistrar events { get; private set; }

        /// <summary>
        /// Initialize the effect engine
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        public void Initialize(Game gameInstance)
        {
            game = gameInstance;
            events = new EventRegistrar(gameInstance, this);
            
            // Register for game events that clean up effects
            RegisterGameEvents();
            
            effects.Clear();
            customDurationEvents.Clear();
            newEffect = false;

            if (enableDebugLogging)
                Debug.Log("🔮 EffectEngine initialized");
        }

        /// <summary>
        /// Register for standard game events
        /// </summary>
        private void RegisterGameEvents()
        {
            events.Register(new List<string>
            {
                EventNames.OnConflictFinished,
                EventNames.OnPhaseEnded,
                EventNames.OnRoundEnded,
                EventNames.OnDuelFinished,
                EventNames.OnPassActionPhasePriority
            });
        }

        /// <summary>
        /// Add a new effect to the engine
        /// </summary>
        /// <param name="effect">Effect to add</param>
        /// <returns>The added effect</returns>
        public GameEffect Add(GameEffect effect)
        {
            if (effect == null)
            {
                Debug.LogWarning("⚠️ Attempted to add null effect");
                return null;
            }

            effects.Add(effect);
            
            // Register custom duration events if needed
            if (effect.duration == Durations.Custom)
            {
                RegisterCustomDurationEvents(effect);
            }
            
            newEffect = true;
            
            if (enableEffectTracing)
                Debug.Log($"🔮 Effect added: {effect.GetDebugInfo()}");
            
            return effect;
        }

        /// <summary>
        /// Check for and trigger delayed effects based on game events
        /// </summary>
        /// <param name="gameEvents">Events that occurred</param>
        public void CheckDelayedEffects(List<GameEvent> gameEvents)
        {
            var effectsToTrigger = new List<GameEffect>();
            var effectsToRemove = new List<GameEffect>();

            // Find delayed effects that should trigger
            foreach (var effect in effects.Where(e => IsDelayedEffect(e)))
            {
                var delayedEffect = GetDelayedEffectProperties(effect);
                if (delayedEffect == null) continue;

                bool shouldTrigger = false;

                // Check condition-based triggers
                if (delayedEffect.condition != null)
                {
                    if (delayedEffect.condition(effect.context))
                    {
                        shouldTrigger = true;
                    }
                }
                // Check event-based triggers
                else if (delayedEffect.when != null)
                {
                    var triggeringEvents = gameEvents.Where(gameEvent => 
                        delayedEffect.when.ContainsKey(gameEvent.name)).ToList();

                    if (triggeringEvents.Count > 0)
                    {
                        // Remove effect if it's not multiple trigger and not persistent
                        if (!delayedEffect.multipleTrigger && effect.duration != Durations.Persistent)
                        {
                            effectsToRemove.Add(effect);
                        }

                        // Check if any event actually triggers the effect
                        if (triggeringEvents.Any(gameEvent => 
                            delayedEffect.when[gameEvent.name](gameEvent, effect.context)))
                        {
                            shouldTrigger = true;
                        }
                    }
                }

                if (shouldTrigger)
                {
                    effectsToTrigger.Add(effect);
                }
            }

            // Remove triggered single-use effects
            if (effectsToRemove.Count > 0)
            {
                UnapplyAndRemove(effect => effectsToRemove.Contains(effect));
            }

            // Trigger effects
            if (effectsToTrigger.Count > 0)
            {
                TriggerDelayedEffects(effectsToTrigger);
            }
        }

        /// <summary>
        /// Check if an effect is a delayed effect
        /// </summary>
        /// <param name="effect">Effect to check</param>
        /// <returns>True if delayed effect</returns>
        private bool IsDelayedEffect(GameEffect effect)
        {
            if (effect.effect is DelayedEffect) return true;
            
            // Check by type name for dynamic effects
            var effectType = effect.effect?.GetType().Name;
            return effectType == "DelayedEffect" || 
                   (effect.effect is IDelayedEffect);
        }

        /// <summary>
        /// Get delayed effect properties from a game effect
        /// </summary>
        /// <param name="effect">Game effect</param>
        /// <returns>Delayed effect properties</returns>
        private DelayedEffect GetDelayedEffectProperties(GameEffect effect)
        {
            if (effect.effect is DelayedEffect delayedEffect)
                return delayedEffect;

            if (effect.effect is IDelayedEffect dynamicDelayed)
                return dynamicDelayed.GetDelayedEffect();

            return null;
        }

        /// <summary>
        /// Trigger a list of delayed effects
        /// </summary>
        /// <param name="effectsToTrigger">Effects to trigger</param>
        private void TriggerDelayedEffects(List<GameEffect> effectsToTrigger)
        {
            var choices = effectsToTrigger.Select(effect =>
            {
                var delayedEffect = GetDelayedEffectProperties(effect);
                var context = effect.context;
                var targets = effect.targets;

                string title = $"{(context.source as EffectSource)?.name ?? "Unknown"}'s effect";
                if (targets.Count == 1)
                {
                    title += $" on {targets[0].objectName}";
                }

                return new EffectChoice
                {
                    Title = title,
                    Handler = () =>
                    {
                        // Set default targets
                        delayedEffect.gameAction.SetDefaultTarget(context => targets);

                        // Show message if applicable
                        if (!string.IsNullOrEmpty(delayedEffect.message) && 
                            delayedEffect.gameAction.HasLegalTarget(context))
                        {
                            var messageArgs = GetMessageArgs(delayedEffect.messageArgs, context);
                            game.AddMessage(delayedEffect.message, messageArgs);
                        }

                        // Execute the game action
                        var actionEvents = new List<GameEvent>();
                        delayedEffect.gameAction.AddEventsToArray(actionEvents, context);
                        
                        game.QueueSimpleStep(() => {
                            game.OpenThenEventWindow(actionEvents);
                            return true;
                        });
                        
                        game.QueueSimpleStep(() => {
                            context.Refill();
                            return true;
                        });
                    }
                };
            }).ToList();

            game.OpenSimultaneousEffectWindow(choices);
        }

        /// <summary>
        /// Get message arguments for delayed effect messages
        /// </summary>
        /// <param name="messageArgs">Raw message arguments</param>
        /// <param name="context">Ability context</param>
        /// <returns>Resolved message arguments</returns>
        private object[] GetMessageArgs(object messageArgs, AbilityContext context)
        {
            if (messageArgs == null) return new object[0];
            
            if (messageArgs is System.Func<AbilityContext, object[]> argsFunc)
            {
                return argsFunc(context);
            }
            
            if (messageArgs is object[] argsArray)
            {
                return argsArray;
            }
            
            return new object[] { messageArgs };
        }

        /// <summary>
        /// Remove lasting effects from a specific card
        /// </summary>
        /// <param name="card">Card to remove effects from</param>
        public void RemoveLastingEffects(EffectSource card)
        {
            // Remove non-persistent effects that match the card
            UnapplyAndRemove(effect =>
                effect.match == card && 
                effect.duration != Durations.Persistent && 
                !effect.canChangeZoneOnce);

            // Update canChangeZoneOnce flag for remaining effects
            foreach (var effect in effects.Where(e => e.match == card && e.canChangeZoneOnce))
            {
                effect.canChangeZoneOnce = false;
            }

            if (enableEffectTracing)
                Debug.Log($"🔮 Removed lasting effects from {card.name}");
        }

        /// <summary>
        /// Check all effects for condition changes and update game state
        /// </summary>
        /// <param name="prevStateChanged">Whether state changed previously</param>
        /// <param name="loops">Current loop count (prevents infinite loops)</param>
        /// <returns>True if game state changed</returns>
        public bool CheckEffects(bool prevStateChanged = false, int loops = 0)
        {
            // Prevent infinite loops and unnecessary checks
            if (!prevStateChanged && !newEffect) return false;
            
            bool stateChanged = false;
            newEffect = false;

            // Check each effect's condition and update targets
            stateChanged = effects.Aggregate(stateChanged, (current, effect) => effect.CheckCondition(current));

            // Prevent infinite recursion
            if (loops >= 10)
            {
                Debug.LogError("❌ EffectEngine.CheckEffects looped 10 times - possible infinite loop!");
                return stateChanged;
            }

            // Recurse if state changed
            if (stateChanged)
            {
                return CheckEffects(stateChanged, loops + 1);
            }

            return stateChanged;
        }

        /// <summary>
        /// Handle conflict finished event
        /// </summary>
        public void OnConflictFinished()
        {
            newEffect = UnapplyAndRemove(effect => effect.duration == Durations.UntilEndOfConflict);
            
            if (enableEffectTracing)
                Debug.Log("🔮 Conflict finished - removed UntilEndOfConflict effects");
        }

        /// <summary>
        /// Handle duel finished event
        /// </summary>
        public void OnDuelFinished()
        {
            newEffect = UnapplyAndRemove(effect => effect.duration == Durations.UntilEndOfDuel);
            
            if (enableEffectTracing)
                Debug.Log("🔮 Duel finished - removed UntilEndOfDuel effects");
        }

        /// <summary>
        /// Handle phase ended event
        /// </summary>
        public void OnPhaseEnded()
        {
            newEffect = UnapplyAndRemove(effect => effect.duration == Durations.UntilEndOfPhase);
            
            if (enableEffectTracing)
                Debug.Log("🔮 Phase ended - removed UntilEndOfPhase effects");
        }

        /// <summary>
        /// Handle round ended event
        /// </summary>
        public void OnRoundEnded()
        {
            newEffect = UnapplyAndRemove(effect => effect.duration == Durations.UntilEndOfRound);
            
            if (enableEffectTracing)
                Debug.Log("🔮 Round ended - removed UntilEndOfRound effects");
        }

        /// <summary>
        /// Handle pass priority event
        /// </summary>
        public void OnPassActionPhasePriority()
        {
            newEffect = UnapplyAndRemove(effect => effect.duration == Durations.UntilPassPriority);

            // Update priority-based durations
            foreach (var effect in effects)
            {
                if (effect.duration == Durations.UntilOpponentPassPriority)
                {
                    effect.duration = Durations.UntilPassPriority;
                }
                else if (effect.duration == Durations.UntilNextPassPriority)
                {
                    effect.duration = Durations.UntilOpponentPassPriority;
                }
            }
            
            if (enableEffectTracing)
                Debug.Log("🔮 Pass priority - updated priority-based effects");
        }

        /// <summary>
        /// Register custom duration events for an effect
        /// </summary>
        /// <param name="effect">Effect with custom duration</param>
        private void RegisterCustomDurationEvents(GameEffect effect)
        {
            if (effect.until == null) return;

            var handler = CreateCustomDurationHandler(effect);

            foreach (var eventName in effect.until.Keys)
            {
                var customEvent = new CustomDurationEvent
                {
                    name = eventName,
                    handler = handler,
                    effect = effect
                };

                customDurationEvents.Add(customEvent);
                game.on(eventName, handler);
            }

            if (enableEffectTracing)
                Debug.Log($"🔮 Registered custom duration events for effect: {effect.id}");
        }

        /// <summary>
        /// Unregister custom duration events for an effect
        /// </summary>
        /// <param name="effect">Effect to unregister events for</param>
        private void UnregisterCustomDurationEvents(GameEffect effect)
        {
            var eventsForEffect = customDurationEvents.Where(e => e.effect == effect).ToList();

            foreach (var customEvent in eventsForEffect)
            {
                game.removeListener(customEvent.name, customEvent.handler);
                customDurationEvents.Remove(customEvent);
            }

            if (enableEffectTracing && eventsForEffect.Count > 0)
                Debug.Log($"🔮 Unregistered {eventsForEffect.Count} custom duration events");
        }

        /// <summary>
        /// Create a handler for custom duration events
        /// </summary>
        /// <param name="customDurationEffect">Effect with custom duration</param>
        /// <returns>Event handler function</returns>
        private System.Action<GameEvent> CreateCustomDurationHandler(GameEffect customDurationEffect)
        {
            return (gameEvent) =>
            {
                try
                {
                    if (customDurationEffect.until.TryGetValue(gameEvent.name, out object listener))
                    {
                        bool shouldEnd = false;

                        if (listener is System.Func<GameEvent, bool> boolFunc)
                        {
                            shouldEnd = boolFunc(gameEvent);
                        }
                        else if (listener is System.Func<GameEvent, AbilityContext, bool> contextFunc)
                        {
                            shouldEnd = contextFunc(gameEvent, customDurationEffect.context);
                        }

                        if (shouldEnd)
                        {
                            customDurationEffect.Cancel();
                            UnregisterCustomDurationEvents(customDurationEffect);
                            effects.Remove(customDurationEffect);

                            if (enableEffectTracing)
                                Debug.Log($"🔮 Custom duration effect ended: {customDurationEffect.id}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Error in custom duration handler: {e.Message}");
                }
            };
        }

        /// <summary>
        /// Remove effects matching a condition
        /// </summary>
        /// <param name="match">Condition to match for removal</param>
        /// <returns>True if any effects were removed</returns>
        public bool UnapplyAndRemove(System.Func<GameEffect, bool> match)
        {
            var matchingEffects = effects.Where(match).ToList();

            foreach (var effect in matchingEffects)
            {
                effect.Cancel();
                
                if (effect.duration == Durations.Custom)
                {
                    UnregisterCustomDurationEvents(effect);
                }
            }

            effects.RemoveAll(effect => matchingEffects.Contains(effect));

            if (enableEffectTracing && matchingEffects.Count > 0)
                Debug.Log($"🔮 Removed {matchingEffects.Count} effects");

            return matchingEffects.Count > 0;
        }

        /// <summary>
        /// Get debug information about all effects
        /// </summary>
        /// <returns>List of effect debug info</returns>
        public List<object> GetDebugInfo()
        {
            return effects.Select(effect => effect.GetDebugInfo()).ToList();
        }

        /// <summary>
        /// Get count of active effects
        /// </summary>
        /// <returns>Number of active effects</returns>
        public int GetActiveEffectCount()
        {
            return effects.Count(e => e.isActive);
        }

        /// <summary>
        /// Get effects by duration
        /// </summary>
        /// <param name="duration">Duration to filter by</param>
        /// <returns>List of effects with matching duration</returns>
        public List<GameEffect> GetEffectsByDuration(string duration)
        {
            return effects.Where(e => e.duration == duration).ToList();
        }

        /// <summary>
        /// Create a delayed effect that triggers on specific conditions
        /// </summary>
        /// <param name="trigger">Trigger object that determines when effect fires</param>
        /// <param name="gameAction">Action to execute when triggered</param>
        /// <returns>Created delayed effect</returns>
        public DelayedEffect CreateDelayedEffect(object trigger, IGameAction gameAction)
        {
            var delayedEffect = new DelayedEffect
            {
                type = EffectNames.DelayedEffect,
                gameAction = gameAction,
                multipleTrigger = false
            };

            // Convert trigger to condition function
            if (trigger is ConflictFinishedTrigger)
            {
                delayedEffect.when = new Dictionary<string, System.Func<GameEvent, AbilityContext, bool>>
                {
                    { EventNames.OnConflictFinished, (gameEvent, context) => true }
                };
            }
            // Add other trigger types as needed

            return delayedEffect;
        }

        /// <summary>
        /// Clear all effects (for cleanup)
        /// </summary>
        public void ClearAllEffects()
        {
            // Cancel all effects
            foreach (var effect in effects.ToList())
            {
                effect.Cancel();
                if (effect.duration == Durations.Custom)
                {
                    UnregisterCustomDurationEvents(effect);
                }
            }

            effects.Clear();
            customDurationEvents.Clear();
            newEffect = false;

            if (enableDebugLogging)
                Debug.Log("🔮 All effects cleared");
        }

        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        private void OnDestroy()
        {
            ClearAllEffects();
            Debug.Log("🔮 EffectEngine destroyed");
        }

        /// <summary>
        /// Create a take control effect
        /// </summary>
        /// <param name="target">Target to take control of</param>
        /// <param name="controller">New controller</param>
        /// <returns>Take control effect</returns>
        public static object TakeControl(object target, Player controller)
        {
            return new
            {
                type = "takeControl",
                target = target,
                controller = controller,
                originalController = target is BaseCard card ? card.controller : null
            };
        }
    }

    /// <summary>
    /// Represents a custom duration event registration
    /// </summary>
    [System.Serializable]
    public class CustomDurationEvent
    {
        public string name;
        public System.Action<GameEvent> handler;
        public GameEffect effect;
    }

    /// <summary>
    /// Interface for delayed effects
    /// </summary>
    public interface IDelayedEffect
    {
        DelayedEffect GetDelayedEffect();
    }

    /// <summary>
    /// Interface for game actions
    /// </summary>
    public interface IGameAction
    {
        void SetDefaultTarget(System.Func<AbilityContext, object> targetFunc);
        void SetDefaultTarget(System.Func<AbilityContext, List<object>> targetFunc);
        bool HasLegalTarget(AbilityContext context, GameAction.GameActionProperties additionalProperties = null);
        void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameAction.GameActionProperties additionalProperties = null);
    }

    /// <summary>
    /// Represents an effect choice for simultaneous resolution
    /// </summary>
    public class SimpleEffectChoice
    {
        public string title;
        public System.Action handler;
    }
    

    /// <summary>
    /// Simple event registrar for the effect engine
    /// </summary>
    public class EventRegistrar
    {
        private Game game;
        private EffectEngine engine;

        public EventRegistrar(Game gameInstance, EffectEngine effectEngine)
        {
            game = gameInstance;
            engine = effectEngine;
        }

        public void Register(List<string> eventNames)
        {
            // Register event handlers with the game
            foreach (var eventName in eventNames)
            {
                switch (eventName)
                {
                    case EventNames.OnConflictFinished:
                        game.on(eventName, (e) => engine.OnConflictFinished());
                        break;
                    case EventNames.OnPhaseEnded:
                        game.on(eventName, (e) => engine.OnPhaseEnded());
                        break;
                    case EventNames.OnRoundEnded:
                        game.on(eventName, (e) => engine.OnRoundEnded());
                        break;
                    case EventNames.OnDuelFinished:
                        game.on(eventName, (e) => engine.OnDuelFinished());
                        break;
                    case EventNames.OnPassActionPhasePriority:
                        game.on(eventName, (e) => engine.OnPassActionPhasePriority());
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Additional effect names for the effect engine
    /// </summary>
    public static partial class EffectNames
    {
        public const string DelayedEffect = "delayedEffect";
    }


    /// <summary>
    /// Trigger that fires when a conflict finishes
    /// </summary>
    public class ConflictFinishedTrigger
    {
        public string EventName => EventNames.OnConflictFinished;
        
        public ConflictFinishedTrigger() { }
    }

    /// <summary>
    /// Extension methods for effect engine
    /// </summary>
    public static class EffectEngineExtensions
    {
        /// <summary>
        /// Add a simple effect to the engine
        /// </summary>
        /// <param name="engine">Effect engine</param>
        /// <param name="source">Effect source</param>
        /// <param name="duration">Effect duration</param>
        /// <param name="targets">Effect targets</param>
        /// <param name="effectProperties">Effect properties</param>
        /// <returns>Created effect</returns>
        public static GameEffect AddSimpleEffect(this EffectEngine engine, EffectSource source, 
                                                string duration, List<GameObject> targets, object effectProperties)
        {
            var effect = new GameEffect();
            effect.Initialize(source, duration, targets, effectProperties);
            return engine.Add(effect);
        }

        /// <summary>
        /// Remove all effects from a specific source
        /// </summary>
        /// <param name="engine">Effect engine</param>
        /// <param name="source">Source to remove effects from</param>
        public static void RemoveEffectsFromSource(this EffectEngine engine, EffectSource source)
        {
            engine.UnapplyAndRemove(effect => effect.source == source);
        }

        /// <summary>
        /// Check if engine has any effects from a source
        /// </summary>
        /// <param name="engine">Effect engine</param>
        /// <param name="source">Source to check for</param>
        /// <returns>True if has effects from source</returns>
        public static bool HasEffectsFromSource(this EffectEngine engine, EffectSource source)
        {
            return engine.GetDebugInfo().Any(); // Would need actual effects list access
        }

        /// <summary>
        /// Creates a take control effect for a target
        /// </summary>
        /// <param name="target">Target to take control of</param>
        /// <param name="controller">New controller</param>
        /// <returns>Take control effect</returns>
        public static object TakeControl(object target, Player controller)
        {
            return new
            {
                type = "takeControl",
                target = target,
                controller = controller,
                action = new System.Func<AbilityContext, bool>((context) =>
                {
                    if (target is BaseCard card && controller != null)
                    {
                        var previousController = card.controller;
                        card.controller = controller;
                        return true;
                    }
                    return false;
                })
            };
        }
    }

    /// <summary>
    /// Static methods for EffectEngine
    /// </summary>
    public static class EffectEngineStatic
    {
        /// <summary>
        /// Creates a take control effect for a target
        /// </summary>
        /// <param name="target">Target to take control of</param>
        /// <param name="controller">New controller</param>
        /// <returns>Take control effect</returns>
        public static object TakeControl(object target, Player controller)
        {
            return new
            {
                type = "takeControl",
                target = target,
                controller = controller,
                Apply = new System.Action<object>((t) =>
                {
                    if (t is BaseCard card && controller != null)
                    {
                        card.controller = controller;
                    }
                }),
                Unapply = new System.Action<object>((t) =>
                {
                    if (t is BaseCard card)
                    {
                        card.controller = card.owner; // Restore original controller
                    }
                })
            };
        }

        /// <summary>
        /// Creates a take control effect with a condition
        /// </summary>
        /// <param name="target">Target to take control of</param>
        /// <param name="controller">New controller</param>
        /// <param name="condition">Condition for the effect</param>
        /// <returns>Take control effect</returns>
        public static object TakeControl(object target, Player controller, System.Func<AbilityContext, bool> condition)
        {
            return new
            {
                type = "takeControl",
                target = target,
                controller = controller,
                condition = condition,
                Apply = new System.Action<object>((t) =>
                {
                    if (t is BaseCard card && controller != null)
                    {
                        card.controller = controller;
                    }
                }),
                Unapply = new System.Action<object>((t) =>
                {
                    if (t is BaseCard card)
                    {
                        card.controller = card.owner; // Restore original controller
                    }
                })
            };
        }
    }

    /// <summary>
    /// Interface for objects that can have effects applied to them
    /// </summary>
    public interface IEffectTarget
    {
        void RemoveEffect(object effect);
        void ApplyEffect(object effect);
    }

    /// <summary>
    /// Extension methods for GameObjects to handle effects
    /// </summary>
    public static class GameObjectEffectExtensions
    {
        /// <summary>
        /// Remove an effect from a GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="effect">Effect to remove</param>
        public static void RemoveEffect(this GameObject gameObject, object effect)
        {
            // Try to get IEffectTarget component
            var effectTarget = gameObject.GetComponent<IEffectTarget>();
            if (effectTarget != null)
            {
                effectTarget.RemoveEffect(effect);
                return;
            }

            // Try to get BaseCard component
            var baseCard = gameObject.GetComponent<BaseCard>();
            if (baseCard != null)
            {
                // Handle BaseCard effect removal
                // This would integrate with the card's effect system
                return;
            }

            // Default handling for other GameObjects
            Debug.Log($"RemoveEffect called on {gameObject.name} but no effect handling found");
        }

        /// <summary>
        /// Apply an effect to a GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="effect">Effect to apply</param>
        public static void ApplyEffect(this GameObject gameObject, object effect)
        {
            // Try to get IEffectTarget component
            var effectTarget = gameObject.GetComponent<IEffectTarget>();
            if (effectTarget != null)
            {
                effectTarget.ApplyEffect(effect);
                return;
            }

            // Try to get BaseCard component
            var baseCard = gameObject.GetComponent<BaseCard>();
            if (baseCard != null)
            {
                // Handle BaseCard effect application
                // This would integrate with the card's effect system
                return;
            }

            // Default handling for other GameObjects
            Debug.Log($"ApplyEffect called on {gameObject.name} but no effect handling found");
        }

        /// <summary>
        /// Add an effect to a GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="effect">Effect to add</param>
        public static void AddEffect(this UnityEngine.GameObject gameObject, object effect)
        {
            // Get or add EffectContainer component
            var effectContainer = gameObject.GetComponent<EffectContainer>();
            if (effectContainer == null)
            {
                effectContainer = gameObject.AddComponent<EffectContainer>();
            }

            // Try to determine effect type and value from the effect object
            if (effect is Effect eff)
            {
                effectContainer.AddEffect(eff.type, eff);
            }
            else
            {
                effectContainer.AddEffect(effect.GetType().Name, effect);
            }
        }
    }
    
}