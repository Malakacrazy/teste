using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a card-based effect applied to one or more targets.
    /// Perfect C# port of the original JavaScript Effect class.
    /// 
    /// Manages effect application, target validation, condition checking,
    /// and automatic cleanup when conditions change.
    /// </summary>
    [Serializable]
    public class Effect
    {
        #region Fields
        
        [Header("Effect Configuration")]
        public Game game;
        public BaseCard source;
        public string duration;
        public string location;
        public bool canChangeZoneOnce;
        
        [Header("Effect State")]
        public List<object> targets = new List<object>();
        public AbilityContext context;
        public object effect;
        public Dictionary<string, object> until = new Dictionary<string, object>();
        public string type = "effect";
        
        [Header("Targeting Functions")]
        [SerializeField] private bool hasMatchFunction;
        [SerializeField] private bool hasConditionFunction;
        [SerializeField] private bool hasSingleMatchTarget;
        
        // Function delegates (not serialized)
        private Func<object, AbilityContext, bool> matchFunction;
        private Func<AbilityContext, bool> conditionFunction;
        private object singleMatchTarget;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create a new Effect instance
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceCard">Source card of the effect</param>
        /// <param name="properties">Effect properties</param>
        /// <param name="effectImplementation">Effect implementation</param>
        public Effect(Game gameInstance, BaseCard sourceCard, EffectProperties properties, object effectImplementation)
        {
            game = gameInstance ?? throw new ArgumentNullException(nameof(gameInstance));
            source = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
            effect = effectImplementation ?? throw new ArgumentNullException(nameof(effectImplementation));
            
            // Set up properties
            SetupProperties(properties);
            
            // Initialize context and effect
            RefreshContext();
            
            // Ensure effect has required properties
            if (effect is IEffectImplementation effectImpl)
            {
                effectImpl.duration = duration;
                effectImpl.isConditional = hasConditionFunction;
            }
            else
            {
                // Wrap the effect if it doesn't implement the interface
                effect = new EffectWrapper(effect, duration, hasConditionFunction);
            }
        }

        /// <summary>
        /// Create a simple effect with effect type, value and source (compatibility constructor)
        /// </summary>
        /// <param name="effectType">Type of effect</param>
        /// <param name="value">Effect value</param>
        /// <param name="source">Effect source</param>
        public Effect(string effectType, object value, EffectSource source)
        {
            if (source?.game == null)
            {
                Debug.LogError($"Cannot create effect {effectType}: source game is null");
                return;
            }

            game = source.game;
            this.source = source;
            type = effectType;
            duration = "persistent";
            location = "play area";
            
            // Create a simple effect implementation
            effect = new SimpleEffectImplementation(effectType, value);
            
            // Initialize context
            RefreshContext();
            
            targets = new List<object>();
        }

        /// <summary>
        /// Create a conditional effect with effect type, value function and source (compatibility constructor)
        /// </summary>
        /// <param name="effectType">Type of effect</param>
        /// <param name="valueFunc">Function to calculate effect value</param>
        /// <param name="source">Effect source</param>
        public Effect(string effectType, System.Func<GameObject, object> valueFunc, EffectSource source)
        {
            if (source?.game == null)
            {
                Debug.LogError($"Cannot create effect {effectType}: source game is null");
                return;
            }

            game = source.game;
            this.source = source;
            type = effectType;
            duration = "persistent";
            location = "play area";
            
            // Create a conditional effect implementation
            effect = new ConditionalEffectImplementation(effectType, valueFunc);
            
            // Initialize context
            RefreshContext();
            
            targets = new List<object>();
        }
        
        /// <summary>
        /// Setup effect properties from the configuration
        /// </summary>
        private void SetupProperties(EffectProperties properties)
        {
            // Set match function or target
            if (properties.MatchCondition != null)
            {
                matchFunction = properties.MatchCondition;
                hasMatchFunction = true;
            }
            else if (properties.MatchTarget != null)
            {
                singleMatchTarget = properties.MatchTarget;
                hasSingleMatchTarget = true;
            }
            else
            {
                matchFunction = (target, context) => true;
                hasMatchFunction = true;
            }
            
            // Set condition function
            if (properties.ActiveCondition != null)
            {
                conditionFunction = properties.ActiveCondition;
                hasConditionFunction = true;
            }
            else
            {
                conditionFunction = context => true;
                hasConditionFunction = false;
            }
            
            // Set other properties
            duration = properties.Duration.ToString();
            location = properties.SourceLocation.ToString();
            canChangeZoneOnce = properties.CanChangeZoneOnce;
            until = properties.Until ?? new Dictionary<string, object>();
        }
        
        #endregion
        
        #region Context Management
        
        /// <summary>
        /// Refresh the effect context
        /// </summary>
        public void RefreshContext()
        {
            context = game.GetFrameworkContext(source.controller);
            context.source = source;
            
            // Set context on effect if it supports it
            if (effect is IEffectImplementation effectImpl)
            {
                effectImpl.SetContext(context);
            }
        }
        
        #endregion
        
        #region Target Validation
        
        /// <summary>
        /// Check if a target is valid for this effect
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <returns>True if target is valid</returns>
        public virtual bool IsValidTarget(object target)
        {
            // Override in derived classes for specific validation
            return true;
        }
        
        /// <summary>
        /// Get default target for this effect
        /// </summary>
        /// <param name="contextOverride">Context to use (optional)</param>
        /// <returns>Default target or null</returns>
        public virtual object GetDefaultTarget(AbilityContext contextOverride = null)
        {
            // Override in derived classes to provide default targets
            return null;
        }
        
        /// <summary>
        /// Get all possible targets for this effect
        /// </summary>
        /// <returns>List of possible targets</returns>
        public virtual List<object> GetTargets()
        {
            // Override in derived classes to provide target lists
            return new List<object>();
        }
        
        #endregion
        
        #region Target Management
        
        /// <summary>
        /// Add a target to this effect
        /// </summary>
        /// <param name="target">Target to add</param>
        public void AddTarget(object target)
        {
            if (target == null) return;
            
            targets.Add(target);
            
            // Apply effect if it supports it
            if (effect is IEffectImplementation effectImpl)
            {
                effectImpl.Apply(target);
            }
            
            Debug.Log($"Effect from {source.name} applied to {GetTargetName(target)}");
        }
        
        /// <summary>
        /// Remove a single target from this effect
        /// </summary>
        /// <param name="target">Target to remove</param>
        public void RemoveTarget(object target)
        {
            if (target == null) return;
            
            RemoveTargets(new List<object> { target });
        }
        
        /// <summary>
        /// Remove multiple targets from this effect
        /// </summary>
        /// <param name="targetsToRemove">Targets to remove</param>
        public void RemoveTargets(List<object> targetsToRemove)
        {
            if (targetsToRemove == null || targetsToRemove.Count == 0) return;
            
            foreach (var target in targetsToRemove)
            {
                // Unapply effect if it supports it
                if (effect is IEffectImplementation effectImpl)
                {
                    effectImpl.Unapply(target);
                }
                Debug.Log($"Effect from {source.name} removed from {GetTargetName(target)}");
            }
            
            targets = targets.Where(t => !targetsToRemove.Contains(t)).ToList();
        }
        
        /// <summary>
        /// Check if this effect has a specific target
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if effect has this target</returns>
        public bool HasTarget(object target)
        {
            return targets.Contains(target);
        }
        
        /// <summary>
        /// Cancel this effect and remove all targets
        /// </summary>
        public void Cancel()
        {
            foreach (var target in targets)
            {
                // Unapply effect if it supports it
                if (effect is IEffectImplementation effectImpl)
                {
                    effectImpl.Unapply(target);
                }
            }
            
            targets.Clear();
            Debug.Log($"Effect from {source.name} cancelled - all targets removed");
        }
        
        #endregion
        
        #region Effect State
        
        /// <summary>
        /// Check if this effect is currently active
        /// </summary>
        /// <returns>True if effect is active</returns>
        public bool IsEffectActive()
        {
            if (duration != Durations.Persistent)
            {
                return true;
            }
            
            // Check if source has persistent effect that includes this effect
            bool effectOnSource = source.persistentEffects.Any(persistentEffect => 
                persistentEffect.reference != null && persistentEffect.reference.Contains(this));
            
            return !source.facedown && effectOnSource;
        }
        
        #endregion
        
        #region Condition Checking
        
        /// <summary>
        /// Check effect conditions and update targets accordingly
        /// Perfect port of the JavaScript condition checking logic
        /// </summary>
        /// <param name="stateChanged">Whether state has already changed</param>
        /// <returns>True if state changed during this check</returns>
        public bool CheckCondition(bool stateChanged = false)
        {
            // Check if effect should be active
            if (!conditionFunction(context) || !IsEffectActive())
            {
                stateChanged = targets.Count > 0 || stateChanged;
                Cancel();
                return stateChanged;
            }
            
            // Handle function-based matching
            if (hasMatchFunction)
            {
                // Get invalid targets
                var invalidTargets = targets.Where(target => 
                    !matchFunction(target, context) || !IsValidTarget(target)).ToList();
                
                // Remove invalid targets
                if (invalidTargets.Count > 0)
                {
                    RemoveTargets(invalidTargets);
                    stateChanged = true;
                }
                
                // Recalculate effect for remaining valid targets
                foreach (var target in targets)
                {
                    if (effect is IEffectImplementation effectImpl && effectImpl.Recalculate(target))
                        stateChanged = true;
                }
                
                // Check for new targets
                var allPossibleTargets = GetTargets();
                var newTargets = allPossibleTargets.Where(target => 
                    !targets.Contains(target) && 
                    matchFunction(target, context) && 
                    IsValidTarget(target)).ToList();
                
                // Apply effect to new targets
                foreach (var newTarget in newTargets)
                {
                    AddTarget(newTarget);
                }
                
                return stateChanged || newTargets.Count > 0;
            }
            
            // Handle single target matching
            if (hasSingleMatchTarget)
            {
                if (targets.Contains(singleMatchTarget))
                {
                    if (!IsValidTarget(singleMatchTarget))
                    {
                        Cancel();
                        return true;
                    }
                    
                    bool recalculated = false;
                    if (effect is IEffectImplementation effectImpl)
                    {
                        recalculated = effectImpl.Recalculate(singleMatchTarget);
                    }
                    return recalculated || stateChanged;
                }
                else if (!targets.Contains(singleMatchTarget) && IsValidTarget(singleMatchTarget))
                {
                    AddTarget(singleMatchTarget);
                    return true;
                }
            }
            
            return stateChanged;
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information about this effect
        /// </summary>
        /// <returns>Debug information object</returns>
        public object GetDebugInfo()
        {
            var targetNames = targets.Select(target => GetTargetName(target)).ToList();
            
            return new
            {
                source = source?.name ?? "Unknown",
                targets = string.Join(", ", targetNames),
                targetCount = targets.Count,
                active = IsEffectActive(),
                condition = conditionFunction?.Invoke(context) ?? true,
                effect = (effect is IEffectImplementation effectImpl) ? effectImpl.GetDebugInfo() : effect?.GetType().Name,
                duration = duration,
                location = location,
                hasMatchFunction = hasMatchFunction,
                hasSingleTarget = hasSingleMatchTarget,
                canChangeZone = canChangeZoneOnce
            };
        }
        
        /// <summary>
        /// Get name of a target for debugging
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Target name</returns>
        private string GetTargetName(object target)
        {
            if (target == null) return "null";
            
            if (target is BaseCard card)
                return card.name;
            if (target is Player player)
                return player.name;
            if (target is Ring ring)
                return ring.GetElement();
            if (target is Conflict conflict)
                return $"Conflict({conflict.conflictType})";
            
            return target.GetType().Name;
        }
        
        /// <summary>
        /// Get string representation of this effect
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var targetCount = targets?.Count ?? 0;
            var sourceName = source?.name ?? "Unknown";
            var effectName = effect?.GetType().Name ?? "Unknown";
            
            return $"Effect[{effectName}] from {sourceName} affecting {targetCount} target(s)";
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// Cleanup when effect is destroyed
        /// </summary>
        public void OnDestroy()
        {
            Cancel(); // Remove all targets
            effect = null;
            matchFunction = null;
            conditionFunction = null;
            singleMatchTarget = null;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Properties for effect configuration
    /// </summary>
    [Serializable]
    public class EffectProperties
    {
        // Basic Properties
        public string Duration = "persistent";
        public string SourceLocation = "play area";
        public string TargetController = "self";
        public string TargetLocation = "play area";
        public bool CanChangeZoneOnce = false;
        
        // Missing properties for compilation
        public string duration = "persistent";
        public string location = "play area";
        public object effect;
        public object condition;
        public object match;
        public string targetController = "self";
        public Dictionary<string, object> until;
        public bool multipleTrigger = false;
        public Dictionary<string, object> when;
        
        // Delegate functions
        public Func<object, AbilityContext, bool> MatchCondition;
        public Func<AbilityContext, bool> ActiveCondition;
        public Func<object, bool> TargetValidation;
        public object MatchTarget;
        public Dictionary<string, object> Until;
    }

    /// <summary>
    /// Interface for effect implementations
    /// </summary>
    public interface IEffectImplementation
    {
        string duration { get; set; }
        bool isConditional { get; set; }
        
        void SetContext(AbilityContext context);
        void Apply(object target);
        void Unapply(object target);
        bool Recalculate(object target);
        object GetDebugInfo();
    }

    /// <summary>
    /// Wrapper for effect objects that don't implement IEffectImplementation
    /// </summary>
    public class EffectWrapper : IEffectImplementation
    {
        public object wrappedEffect;
        public string duration { get; set; }
        public bool isConditional { get; set; }

        public EffectWrapper(object effect, string effectDuration, bool conditional)
        {
            wrappedEffect = effect;
            duration = effectDuration;
            isConditional = conditional;
        }

        public void SetContext(AbilityContext context)
        {
            // Try to set context on wrapped effect if it has SetContext method
            var setContextMethod = wrappedEffect?.GetType().GetMethod("SetContext");
            setContextMethod?.Invoke(wrappedEffect, new object[] { context });
        }

        public void Apply(object target)
        {
            // Try to apply effect on wrapped effect if it has Apply method
            var applyMethod = wrappedEffect?.GetType().GetMethod("Apply");
            applyMethod?.Invoke(wrappedEffect, new object[] { target });
        }

        public void Unapply(object target)
        {
            // Try to unapply effect on wrapped effect if it has Unapply method
            var unapplyMethod = wrappedEffect?.GetType().GetMethod("Unapply");
            unapplyMethod?.Invoke(wrappedEffect, new object[] { target });
        }

        public bool Recalculate(object target)
        {
            // Try to recalculate effect on wrapped effect if it has Recalculate method
            var recalculateMethod = wrappedEffect?.GetType().GetMethod("Recalculate");
            if (recalculateMethod != null)
            {
                var result = recalculateMethod.Invoke(wrappedEffect, new object[] { target });
                return result is bool ? (bool)result : false;
            }
            return false;
        }

        public object GetDebugInfo()
        {
            // Try to get debug info from wrapped effect if it has GetDebugInfo method
            var getDebugInfoMethod = wrappedEffect?.GetType().GetMethod("GetDebugInfo");
            if (getDebugInfoMethod != null)
            {
                return getDebugInfoMethod.Invoke(wrappedEffect, null);
            }
            return wrappedEffect?.GetType().Name ?? "UnknownEffect";
        }
    }

    /// <summary>
    /// Constants for effect durations
    /// </summary>
    public static class Durations
    {
        public const string Persistent = "persistent";
        public const string UntilEndOfConflict = "untilEndOfConflict";
        public const string UntilEndOfPhase = "untilEndOfPhase";
        public const string UntilEndOfRound = "untilEndOfRound";
        public const string UntilEndOfDuel = "untilEndOfDuel";
        public const string UntilPassPriority = "untilPassPriority";
        public const string UntilOpponentPassPriority = "untilOpponentPassPriority";
        public const string UntilNextPassPriority = "untilNextPassPriority";
        public const string Custom = "custom";
    }

    /// <summary>
    /// Simple effect implementation for basic effects
    /// </summary>
    public class SimpleEffectImplementation : IEffectImplementation
    {
        public string effectType;
        public object value;
        public string duration { get; set; } = "persistent";
        public bool isConditional { get; set; } = false;

        public SimpleEffectImplementation(string type, object val)
        {
            effectType = type;
            value = val;
        }

        public void SetContext(AbilityContext context) { }
        
        public void Apply(object target)
        {
            // Apply the effect to the target
            if (target is GameObject go)
            {
                var effectComponent = go.GetComponent<EffectContainer>();
                if (effectComponent != null)
                {
                    effectComponent.AddEffect(effectType, value);
                }
            }
        }

        public void Unapply(object target)
        {
            // Remove the effect from the target
            if (target is GameObject go)
            {
                var effectComponent = go.GetComponent<EffectContainer>();
                if (effectComponent != null)
                {
                    effectComponent.RemoveEffect(effectType);
                }
            }
        }

        public bool Recalculate(object target) => false;

        public object GetDebugInfo() => new { type = effectType, value = value };
    }

    /// <summary>
    /// Conditional effect implementation for dynamic effects
    /// </summary>
    public class ConditionalEffectImplementation : IEffectImplementation
    {
        public string effectType;
        public System.Func<GameObject, object> valueFunc;
        public string duration { get; set; } = "persistent";
        public bool isConditional { get; set; } = true;

        public ConditionalEffectImplementation(string type, System.Func<GameObject, object> func)
        {
            effectType = type;
            valueFunc = func;
        }

        public void SetContext(AbilityContext context) { }
        
        public void Apply(object target)
        {
            // Apply the conditional effect to the target
            if (target is GameObject go)
            {
                var currentValue = valueFunc(go);
                var effectComponent = go.GetComponent<EffectContainer>();
                if (effectComponent != null)
                {
                    effectComponent.AddEffect(effectType, currentValue);
                }
            }
        }

        public void Unapply(object target)
        {
            // Remove the effect from the target
            if (target is GameObject go)
            {
                var effectComponent = go.GetComponent<EffectContainer>();
                if (effectComponent != null)
                {
                    effectComponent.RemoveEffect(effectType);
                }
            }
        }

        public bool Recalculate(object target)
        {
            if (target is GameObject go)
            {
                var newValue = valueFunc(go);
                var effectComponent = go.GetComponent<EffectContainer>();
                if (effectComponent != null)
                {
                    effectComponent.UpdateEffect(effectType, newValue);
                    return true;
                }
            }
            return false;
        }

        public object GetDebugInfo() => new { type = effectType, hasValueFunc = valueFunc != null };
    }

    /// <summary>
    /// Component to hold effects on GameObjects
    /// </summary>
    public class EffectContainer : MonoBehaviour
    {
        private Dictionary<string, object> effects = new Dictionary<string, object>();

        public void AddEffect(string effectType, object value)
        {
            effects[effectType] = value;
        }

        public void RemoveEffect(string effectType)
        {
            effects.Remove(effectType);
        }

        public void UpdateEffect(string effectType, object newValue)
        {
            if (effects.ContainsKey(effectType))
            {
                effects[effectType] = newValue;
            }
        }

        public object GetEffect(string effectType)
        {
            return effects.ContainsKey(effectType) ? effects[effectType] : null;
        }

        public bool HasEffect(string effectType)
        {
            return effects.ContainsKey(effectType);
        }
    }
}
