using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Effect that suppresses other effects based on a predicate condition.
    /// Perfect C# port of the original JavaScript SuppressEffect class.
    /// Extends EffectValue to manage a list of suppressed effects.
    /// </summary>
    [Serializable]
    public class SuppressEffect : EffectValue
    {
        [Header("Suppress Effect Configuration")]
        [SerializeField] private bool hasPredicateFunction;
        
        // Non-serialized predicate function
        private Func<IEffectImplementation, bool> predicate;
        
        /// <summary>
        /// Constructor with predicate function
        /// </summary>
        /// <param name="predicateFunction">Function to determine which effects to suppress</param>
        public SuppressEffect(Func<IEffectImplementation, bool> predicateFunction) 
            : base(new List<IEffectImplementation>())
        {
            predicate = predicateFunction;
            hasPredicateFunction = predicate != null;
        }
        
        /// <summary>
        /// Recalculate which effects should be suppressed
        /// Perfect port of the JavaScript recalculate method
        /// </summary>
        /// <returns>True if the suppressed effects list changed</returns>
        public virtual bool Recalculate()
        {
            if (predicate == null)
            {
                return false;
            }
            
            var oldValue = new List<IEffectImplementation>((List<IEffectImplementation>)value);
            
            // Get all effects from the effect engine that match the predicate
            var suppressedEffects = new List<IEffectImplementation>();
            
            if (Context?.game?.effectEngine?.effects != null)
            {
                suppressedEffects = Context.game.effectEngine.effects
                    .Where(effect => effect?.effect != null && effect.effect is IEffectImplementation && predicate(effect.effect as IEffectImplementation))
                    .Select(effect => effect.effect as IEffectImplementation)
                    .ToList();
            }
            
            // Update the value
            SetValue(suppressedEffects);
            
            // Check if the list changed
            bool lengthChanged = oldValue.Count != suppressedEffects.Count;
            bool contentChanged = oldValue.Any(element => !suppressedEffects.Contains(element));
            
            bool hasChanged = lengthChanged || contentChanged;
            
            if (hasChanged)
            {
                Debug.Log($"SuppressEffect recalculated: {oldValue.Count} -> {suppressedEffects.Count} suppressed effects");
            }
            
            return hasChanged;
        }
        
        /// <summary>
        /// Get all currently suppressed effects
        /// </summary>
        /// <returns>List of suppressed effect implementations</returns>
        public List<IEffectImplementation> GetSuppressedEffects()
        {
            return new List<IEffectImplementation>((List<IEffectImplementation>)value);
        }
        
        /// <summary>
        /// Check if a specific effect is suppressed by this suppress effect
        /// </summary>
        /// <param name="effect">Effect to check</param>
        /// <returns>True if the effect is suppressed</returns>
        public bool IsSuppressed(IEffectImplementation effect)
        {
            return ((List<IEffectImplementation>)value).Contains(effect);
        }
        
        /// <summary>
        /// Get count of suppressed effects
        /// </summary>
        /// <returns>Number of suppressed effects</returns>
        public int GetSuppressedCount()
        {
            return ((List<IEffectImplementation>)value)?.Count ?? 0;
        }
        
        /// <summary>
        /// Update the predicate function (useful for dynamic suppression rules)
        /// </summary>
        /// <param name="newPredicate">New predicate function</param>
        public void UpdatePredicate(Func<IEffectImplementation, bool> newPredicate)
        {
            predicate = newPredicate;
            hasPredicateFunction = predicate != null;
            
            // Recalculate with new predicate
            Recalculate();
        }
        
        /// <summary>
        /// Get debug information about this suppress effect
        /// </summary>
        /// <returns>Debug information object</returns>
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            var suppressedEffectNames = ((List<IEffectImplementation>)value)?.Select(effect => effect.GetType().Name).ToList() ?? new List<string>();
            
            return new
            {
                baseInfo,
                suppressedCount = GetSuppressedCount(),
                suppressedEffects = suppressedEffectNames,
                hasPredicate = hasPredicateFunction,
                effectType = "SuppressEffect"
            };
        }
        
        /// <summary>
        /// String representation of this suppress effect
        /// </summary>
        /// <returns>String description</returns>
        public override string ToString()
        {
            var count = GetSuppressedCount();
            var sourceName = (Context?.source as BaseCard)?.name ?? "Unknown";
            return $"SuppressEffect from {sourceName}: suppressing {count} effect(s)";
        }
    }
}
