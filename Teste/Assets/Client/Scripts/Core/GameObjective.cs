using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game objects in the L5R system.
    /// Provides core functionality like effects, restrictions, and targeting.
    /// </summary>
    public class GameObject : EffectSource
    {
        [Header("GameObject Identity")]
        public string objectName;
        public string id;
        public string printedType = "";
        public bool facedown = false;
        public string uuid;

        [Header("Effects")]
        [SerializeField] private List<Effect> effects = new List<Effect>();

        /// <summary>
        /// Reference to the game instance
        /// </summary>
        public Game game { get; protected set; }

        /// <summary>
        /// Current type considering effects
        /// </summary>
        public virtual string Type => GetObjectType();

        /// <summary>
        /// Initialize the GameObject with game reference and name
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="objectName">Name of the object</param>
        public virtual void Initialize(Game gameInstance, string objectName)
        {
            game = gameInstance;
            this.objectName = objectName;
            id = objectName;
            uuid = System.Guid.NewGuid().ToString();
            effects = new List<Effect>();

            Debug.Log($"🎮 GameObject initialized: {objectName}");
        }

        /// <summary>
        /// Add an effect to this object
        /// </summary>
        /// <param name="effect">Effect to add</param>
        public void AddEffect(Effect effect)
        {
            if (effect != null && !effects.Contains(effect))
            {
                effects.Add(effect);
            }
        }

        /// <summary>
        /// Remove an effect from this object
        /// </summary>
        /// <param name="effect">Effect to remove</param>
        public void RemoveEffect(Effect effect)
        {
            effects.Remove(effect);
        }

        /// <summary>
        /// Get all effects of a specific type, with their values resolved
        /// </summary>
        /// <param name="effectType">Type of effect to find</param>
        /// <returns>List of effect values</returns>
        public List<object> GetEffects(string effectType)
        {
            var filteredEffects = GetRawEffects()
                .Where(effect => effect.type == effectType);

            return filteredEffects.Select(effect => effect.GetValue(this)).ToList();
        }

        /// <summary>
        /// Get raw effects before value resolution, considering suppression
        /// </summary>
        /// <returns>List of non-suppressed effects</returns>
        public List<Effect> GetRawEffects()
        {
            // Find effects that suppress other effects
            var suppressEffects = effects.Where(effect => effect.type == EffectNames.SuppressEffects);
            var suppressedEffects = new List<Effect>();

            foreach (var suppressEffect in suppressEffects)
            {
                var suppressedList = suppressEffect.GetValue(this) as List<Effect>;
                if (suppressedList != null)
                {
                    suppressedEffects.AddRange(suppressedList);
                }
            }

            // Return effects that are not suppressed
            return effects.Where(effect => !suppressedEffects.Contains(effect)).ToList();
        }

        /// <summary>
        /// Sum all numeric effects of a specific type
        /// </summary>
        /// <param name="effectType">Type of effect to sum</param>
        /// <returns>Total sum of effects</returns>
        public int SumEffects(string effectType)
        {
            var effectValues = GetEffects(effectType);
            return effectValues.OfType<int>().Sum();
        }

        /// <summary>
        /// Check if this object has any effects of the specified type
        /// </summary>
        /// <param name="effectType">Type of effect to check for</param>
        /// <returns>True if any effects of this type exist</returns>
        public bool AnyEffect(string effectType)
        {
            return GetEffects(effectType).Count > 0;
        }

        /// <summary>
        /// Get the most recently applied effect of a specific type
        /// </summary>
        /// <param name="effectType">Type of effect</param>
        /// <returns>Most recent effect value, or null</returns>
        public object MostRecentEffect(string effectType)
        {
            var effects = GetEffects(effectType);
            return effects.LastOrDefault();
        }

        /// <summary>
        /// Check if a game action can be performed on this object
        /// </summary>
        /// <param name="actionType">Type of action to check</param>
        /// <param name="context">Ability context (optional)</param>
        /// <returns>True if action is allowed</returns>
        public virtual bool AllowGameAction(string actionType, AbilityContext context = null)
        {
            context = context ?? game.GetFrameworkContext();

            // Check if there's a specific game action class for this
            var gameAction = game.Actions.GetAction(actionType, this);
            if (gameAction != null)
            {
                return gameAction.CanAffect(this, context);
            }

            // Fall back to restriction checking
            return CheckRestrictions(actionType, context);
        }

        /// <summary>
        /// Check if this object is restricted from performing an action
        /// </summary>
        /// <param name="actionType">Type of action</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if action is allowed (not restricted)</returns>
        public virtual bool CheckRestrictions(string actionType, AbilityContext context)
        {
            var restrictions = GetEffects(EffectNames.AbilityRestrictions);
            
            foreach (var restriction in restrictions)
            {
                if (restriction is IRestriction restrictionEffect)
                {
                    if (restrictionEffect.IsMatch(actionType, context, this))
                    {
                        return false; // Action is restricted
                    }
                }
            }

            return true; // Action is allowed
        }

        /// <summary>
        /// Check if this object is unique
        /// </summary>
        /// <returns>True if unique</returns>
        public virtual bool IsUnique()
        {
            return false; // Override in derived classes
        }

        /// <summary>
        /// Get the current type of this object, considering type-changing effects
        /// </summary>
        /// <returns>Current object type</returns>
        public virtual string GetObjectType()
        {
            if (AnyEffect(EffectNames.ChangeType))
            {
                return MostRecentEffect(EffectNames.ChangeType) as string ?? printedType;
            }
            return printedType;
        }

        /// <summary>
        /// Get the printed faction of this object
        /// </summary>
        /// <returns>Printed faction or null</returns>
        public virtual string GetPrintedFaction()
        {
            return null; // Override in derived classes
        }

        /// <summary>
        /// Check if this object has a specific keyword
        /// </summary>
        /// <param name="keyword">Keyword to check for</param>
        /// <returns>True if object has the keyword</returns>
        public virtual bool HasKeyword(string keyword)
        {
            return false; // Override in derived classes
        }

        /// <summary>
        /// Check if this object has a specific trait
        /// </summary>
        /// <param name="trait">Trait to check for</param>
        /// <returns>True if object has the trait</returns>
        public virtual bool HasTrait(string trait)
        {
            return false; // Override in derived classes
        }

        /// <summary>
        /// Get all traits of this object
        /// </summary>
        /// <returns>List of traits</returns>
        public virtual List<string> GetTraits()
        {
            return new List<string>(); // Override in derived classes
        }

        /// <summary>
        /// Check if this object belongs to a specific faction
        /// </summary>
        /// <param name="faction">Faction to check</param>
        /// <returns>True if object is of the faction</returns>
        public virtual bool IsFaction(string faction)
        {
            return false; // Override in derived classes
        }

        /// <summary>
        /// Check if this object has a specific token
        /// </summary>
        /// <param name="tokenType">Type of token to check for</param>
        /// <returns>True if object has the token</returns>
        public virtual bool HasToken(string tokenType)
        {
            return false; // Override in derived classes
        }

        /// <summary>
        /// Get a short summary of this object for UI display
        /// </summary>
        /// <param name="activePlayer">Player viewing the summary</param>
        /// <returns>Summary data</returns>
        public virtual Dictionary<string, object> GetShortSummary(Player activePlayer = null)
        {
            return new Dictionary<string, object>
            {
                {"id", id},
                {"label", objectName},
                {"name", objectName},
                {"facedown", facedown},
                {"type", GetObjectType()},
                {"uuid", uuid}
            };
        }

        /// <summary>
        /// Check if this object can be targeted by an ability
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="selectedCards">Already selected targets</param>
        /// <returns>True if object can be targeted</returns>
        public virtual bool CanBeTargeted(AbilityContext context, List<object> selectedCards = null)
        {
            // Check basic targeting restrictions
            if (!CheckRestrictions("target", context))
            {
                return false;
            }

            selectedCards = selectedCards ?? new List<object>();
            var targets = selectedCards.ToList();
            targets.Add(this);

            // Calculate targeting cost
            int targetingCost = context.player.GetTargetingCost(context.source as BaseCard, targets);

            // Check cost requirements based on current stage
            switch (context.stage)
            {
                case ObjectiveStages.PreTarget:
                    return CanAffordTargetingPreTarget(context, targetingCost);
                
                case ObjectiveStages.Target:
                case ObjectiveStages.Effect:
                    return CanAffordTargetingPostCost(context, targetingCost);
                
                default:
                    return true;
            }
        }

        /// <summary>
        /// Check if targeting can be afforded before paying main costs
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="targetingCost">Cost to target</param>
        /// <returns>True if affordable</returns>
        private bool CanAffordTargetingPreTarget(AbilityContext context, int targetingCost)
        {
            // Calculate ability cost (not card cost)
            int fateCost = 0;
            if (context.ability is IReducedCostAbility reducedCostAbility)
            {
                fateCost = reducedCostAbility.GetReducedCost(context);
            }

            // Calculate available fate after paying ability cost
            int alternateFate = context.player.GetAvailableAlternateFate(context.playType, context);
            int availableFate = Math.Max(context.player.fate - Math.Max(fateCost - alternateFate, 0), 0);

            return availableFate >= targetingCost && 
                   (targetingCost == 0 || context.player.CheckRestrictions("spendFate", context));
        }

        /// <summary>
        /// Check if targeting can be afforded after paying costs
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="targetingCost">Cost to target</param>
        /// <returns>True if affordable</returns>
        private bool CanAffordTargetingPostCost(AbilityContext context, int targetingCost)
        {
            return context.player.fate >= targetingCost && 
                   (targetingCost == 0 || context.player.CheckRestrictions("spendFate", context));
        }

        /// <summary>
        /// Get summary for UI controls (same as short summary by default)
        /// </summary>
        /// <param name="activePlayer">Player viewing the controls</param>
        /// <returns>Summary data for controls</returns>
        public virtual Dictionary<string, object> GetShortSummaryForControls(Player activePlayer)
        {
            return GetShortSummary(activePlayer);
        }

        /// <summary>
        /// Check if this object is participating in the current conflict
        /// </summary>
        /// <returns>True if participating in current conflict</returns>
        public virtual bool IsParticipating()
        {
            // GameObjects (like objectives) don't participate in conflicts like cards do
            // Only BaseCards can participate in conflicts
            // Since GameObject cannot be cast to BaseCard, return false
            return false;
        }

        /// <summary>
        /// Check if two GameObjects are the same
        /// </summary>
        /// <param name="other">Other GameObject to compare</param>
        /// <returns>True if same object</returns>
        public virtual bool IsSameObject(GameObject other)
        {
            return other != null && uuid == other.uuid;
        }

        /// <summary>
        /// Get all effects that match a condition
        /// </summary>
        /// <param name="predicate">Condition to match</param>
        /// <returns>List of matching effects</returns>
        public List<Effect> GetEffectsWhere(System.Func<Effect, bool> predicate)
        {
            return GetRawEffects().Where(predicate).ToList();
        }

        /// <summary>
        /// Check if this object has any effects matching a condition
        /// </summary>
        /// <param name="predicate">Condition to check</param>
        /// <returns>True if any effects match</returns>
        public bool AnyEffectWhere(System.Func<Effect, bool> predicate)
        {
            return GetRawEffects().Any(predicate);
        }

        /// <summary>
        /// Remove all effects matching a condition
        /// </summary>
        /// <param name="predicate">Condition to match for removal</param>
        public void RemoveEffectsWhere(System.Func<Effect, bool> predicate)
        {
            var effectsToRemove = effects.Where(predicate).ToList();
            foreach (var effect in effectsToRemove)
            {
                effects.Remove(effect);
            }
        }

        /// <summary>
        /// Clear all effects from this object
        /// </summary>
        public void ClearEffects()
        {
            effects.Clear();
        }

        /// <summary>
        /// Get count of effects of a specific type
        /// </summary>
        /// <param name="effectType">Type of effect to count</param>
        /// <returns>Number of effects of this type</returns>
        public int GetEffectCount(string effectType)
        {
            return GetEffects(effectType).Count;
        }

        /// <summary>
        /// Override ToString for debugging
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"{GetType().Name}: {objectName} ({uuid})";
        }

        /// <summary>
        /// Cleanup when GameObject is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            ClearEffects();
            Debug.Log($"🎮 GameObject destroyed: {objectName}");
        }
    }


    /// <summary>
    /// Interface for restriction effects
    /// </summary>
    public interface IRestriction
    {
        /// <summary>
        /// Check if this restriction applies to an action
        /// </summary>
        /// <param name="actionType">Type of action</param>
        /// <param name="context">Ability context</param>
        /// <param name="target">Target of the action</param>
        /// <returns>True if restriction applies (action blocked)</returns>
        bool IsMatch(string actionType, AbilityContext context, GameObject target);
    }

    /// <summary>
    /// Interface for abilities that can have reduced costs
    /// </summary>
    public interface IReducedCostAbility
    {
        /// <summary>
        /// Get the reduced cost for this ability
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Reduced cost amount</returns>
        int GetReducedCost(AbilityContext context);
    }

    /// <summary>
    /// Additional effect names for GameObject
    /// </summary>
    public static partial class EffectNames
    {
        public const string SuppressEffects = "suppressEffects";
        public const string ChangeType = "changeType";
    }

    /// <summary>
    /// Stages for objective resolution
    /// </summary>
    public static class ObjectiveStages
    {
        public const string PreTarget = "preTarget";
        public const string Target = "target";
        public const string Cost = "cost";
        public const string Effect = "effect";
        public const string PostEffect = "postEffect";
    }

    /// <summary>
    /// Extension methods for GameObject
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Add a simple value effect to a GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="effectType">Type of effect</param>
        /// <param name="value">Effect value</param>
        /// <param name="source">Effect source</param>
        public static void AddSimpleEffect(this GameObject gameObject, string effectType, object value, EffectSource source = null)
        {
            var effect = new Effect(effectType, value, source);
            gameObject.AddEffect(effect);
        }

        /// <summary>
        /// Add a conditional effect to a GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="effectType">Type of effect</param>
        /// <param name="valueFunc">Function to calculate effect value</param>
        /// <param name="source">Effect source</param>
        public static void AddConditionalEffect(this GameObject gameObject, string effectType, 
                                              System.Func<GameObject, object> valueFunc, EffectSource source = null)
        {
            var effect = new Effect(effectType, valueFunc, source);
            gameObject.AddEffect(effect);
        }

        /// <summary>
        /// Remove all effects of a specific type from a GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="effectType">Type of effects to remove</param>
        public static void RemoveEffectsOfType(this GameObject gameObject, string effectType)
        {
            gameObject.RemoveEffectsWhere(effect => effect.type == effectType);
        }

        /// <summary>
        /// Remove all effects from a specific source
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="source">Source of effects to remove</param>
        public static void RemoveEffectsFromSource(this GameObject gameObject, EffectSource source)
        {
            gameObject.RemoveEffectsWhere(effect => effect.source == source);
        }

        /// <summary>
        /// Check if GameObject has any effects from a specific source
        /// </summary>
        /// <param name="gameObject">GameObject to check</param>
        /// <param name="source">Source to check for</param>
        /// <returns>True if has effects from source</returns>
        public static bool HasEffectsFromSource(this GameObject gameObject, EffectSource source)
        {
            return gameObject.AnyEffectWhere(effect => effect.source == source);
        }
    }
}