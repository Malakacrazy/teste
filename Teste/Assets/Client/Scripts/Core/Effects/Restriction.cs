using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Restriction system for controlling when abilities and effects can be used.
    /// Perfect C# port of the original JavaScript Restriction class.
    /// Provides comprehensive restriction checking for various game scenarios.
    /// </summary>
    [Serializable]
    public class Restriction : EffectValue
    {
        #region Fields
        
        [Header("Restriction Configuration")]
        public string type;
        public string restriction;
        public object parameters;
        
        #endregion
        
        #region Static Restriction Definitions
        
        /// <summary>
        /// Leave play action types that trigger leave play restrictions
        /// </summary>
        private static readonly string[] LeavePlayTypes = new[]
        {
            "discardFromPlay",
            "sacrifice", 
            "returnToHand",
            "returnToDeck",
            "removeFromGame"
        };
        
        /// <summary>
        /// Dictionary of restriction checking functions
        /// Perfect port of the JavaScript checkRestrictions object
        /// </summary>
        private static readonly Dictionary<string, Func<AbilityContext, Restriction, BaseCard, bool>> CheckRestrictions = 
            new Dictionary<string, Func<AbilityContext, Restriction, BaseCard, bool>>
            {
                ["abilitiesTriggeredByOpponents"] = (context, effect, card) =>
                    context.player == effect.context.player.Opponent && 
                    context.ability.IsTriggeredAbility() && 
                    context.ability.abilityType != AbilityTypes.ForcedReaction && 
                    context.ability.abilityType != AbilityTypes.ForcedInterrupt,
                    
                ["attachmentsWithSameClan"] = (context, effect, card) =>
                    context.source.GetCardType() == CardTypes.Attachment &&
                    context.source.GetPrintedFaction() != "neutral" && 
                    card.IsFaction(context.source.GetPrintedFaction()),
                    
                ["characters"] = (context, effect, card) => 
                    context.source.GetCardType() == CardTypes.Character,
                    
                ["copiesOfDiscardEvents"] = (context, effect, card) =>
                    context.source.GetCardType() == CardTypes.Event && 
                    context.player.conflictDiscardPile.Any(discardCard => discardCard.name == context.source.name),
                    
                ["copiesOfX"] = (context, effect, card) => 
                    context.source.name == effect.parameters?.ToString(),
                    
                ["events"] = (context, effect, card) => 
                    context.source.GetCardType() == CardTypes.Event,
                    
                ["eventsWithSameClan"] = (context, effect, card) =>
                    context.source.GetCardType() == CardTypes.Event &&
                    context.source.GetPrintedFaction() != "neutral" && 
                    card.IsFaction(context.source.GetPrintedFaction()),
                    
                ["nonSpellEvents"] = (context, effect, card) => 
                    context.source.GetCardType() == CardTypes.Event && !context.source.HasTrait("spell"),
                    
                ["opponentsCardEffects"] = (context, effect, card) =>
                {
                    var cardTypes = new[] { CardTypes.Event, CardTypes.Character, CardTypes.Holding, 
                                          CardTypes.Attachment, CardTypes.Stronghold, CardTypes.Province, CardTypes.Role };
                    return context.player == effect.context.player.Opponent && 
                           (context.ability.IsCardAbility() || !context.ability.IsCardPlayed()) &&
                           cardTypes.Contains(context.source.GetCardType());
                },
                
                ["opponentsEvents"] = (context, effect, card) =>
                    context.player != null && context.player == effect.context.player.Opponent && 
                    context.source.GetCardType() == CardTypes.Event,
                    
                ["opponentsRingEffects"] = (context, effect, card) =>
                    context.player != null && context.player == effect.context.player.Opponent && 
                    context.source.GetCardType() == "ring",
                    
                ["opponentsTriggeredAbilities"] = (context, effect, card) =>
                    context.player == effect.context.player.Opponent && context.ability.IsTriggeredAbility(),
                    
                ["opponentsCardAbilities"] = (context, effect, card) =>
                    context.player == effect.context.player.Opponent && context.ability.IsCardAbility(),
                    
                ["reactions"] = (context, effect, card) => 
                    context.ability.abilityType == AbilityTypes.Reaction,
                    
                ["source"] = (context, effect, card) => 
                    context.source == effect.context.source,
                    
                ["keywordAbilities"] = (context, effect, card) => 
                    context.ability.IsKeywordAbility(),
                    
                ["nonKeywordAbilities"] = (context, effect, card) => 
                    !context.ability.IsKeywordAbility(),
                    
                ["nonForcedAbilities"] = (context, effect, card) => 
                    context.ability.IsTriggeredAbility() && 
                    context.ability.abilityType != AbilityTypes.ForcedReaction && 
                    context.ability.abilityType != AbilityTypes.ForcedInterrupt,
                    
                ["equalOrMoreExpensiveCharacterTriggeredAbilities"] = (context, effect, card) => 
                    context.source.GetCardType() == CardTypes.Character && 
                    !context.ability.IsKeywordAbility() && 
                    context.source.printedCost >= card.printedCost,
                    
                ["equalOrMoreExpensiveCharacterKeywords"] = (context, effect, card) => 
                    context.source.GetCardType() == CardTypes.Character && 
                    context.ability.IsKeywordAbility() && 
                    context.source.printedCost >= card.printedCost
            };
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Constructor with string type
        /// </summary>
        /// <param name="restrictionType">Type of restriction</param>
        public Restriction(string restrictionType) : base()
        {
            type = restrictionType;
        }
        
        /// <summary>
        /// Constructor with properties object
        /// </summary>
        /// <param name="properties">Restriction properties</param>
        public Restriction(RestrictionProperties properties) : base()
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
                
            type = properties.type;
            restriction = properties.restricts;
            parameters = properties.parameters;
        }
        
        /// <summary>
        /// Constructor with property object (flexible)
        /// </summary>
        /// <param name="properties">Dynamic properties object</param>
        public Restriction(object properties) : base()
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
                
            if (properties is string str)
            {
                type = str;
                return;
            }
            
            // Use reflection to extract properties
            var propertiesType = properties.GetType();
            
            var typeProperty = propertiesType.GetProperty("type");
            if (typeProperty != null)
                type = typeProperty.GetValue(properties)?.ToString();
                
            var restrictsProperty = propertiesType.GetProperty("restricts");
            if (restrictsProperty != null)
                restriction = restrictsProperty.GetValue(properties)?.ToString();
                
            var paramsProperty = propertiesType.GetProperty("params") ?? propertiesType.GetProperty("parameters");
            if (paramsProperty != null)
                parameters = paramsProperty.GetValue(properties);
        }
        
        #endregion
        
        #region EffectValue Implementation
        
        /// <summary>
        /// Get the value of this restriction (returns self)
        /// Perfect port of JavaScript getValue method
        /// </summary>
        /// <returns>This restriction instance</returns>
        public override object GetValue()
        {
            return this;
        }
        
        #endregion
        
        #region Restriction Matching
        
        /// <summary>
        /// Check if this restriction matches a specific type and context
        /// Perfect port of JavaScript isMatch method
        /// </summary>
        /// <param name="matchType">Type to match against</param>
        /// <param name="context">Ability context</param>
        /// <param name="card">Card being checked (optional)</param>
        /// <returns>True if restriction matches</returns>
        public bool IsMatch(string matchType, AbilityContext context, BaseCard card = null)
        {
            if (type == "leavePlay")
            {
                return LeavePlayTypes.Contains(matchType) && CheckCondition(context, card);
            }
            
            return (string.IsNullOrEmpty(type) || type == matchType) && CheckCondition(context, card);
        }
        
        /// <summary>
        /// Check the restriction condition
        /// Perfect port of JavaScript checkCondition method
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="card">Card being checked (optional)</param>
        /// <returns>True if condition is met</returns>
        public bool CheckCondition(AbilityContext context, BaseCard card = null)
        {
            if (string.IsNullOrEmpty(restriction))
            {
                return true;
            }
            
            if (context == null)
            {
                throw new ArgumentException("checkCondition called without a context");
            }
            
            if (!CheckRestrictions.ContainsKey(restriction))
            {
                // Fallback to trait checking
                return context.source.HasTrait(restriction);
            }
            
            try
            {
                return CheckRestrictions[restriction](context, this, card);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error checking restriction '{restriction}': {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Add a new restriction type to the system
        /// </summary>
        /// <param name="name">Name of the restriction</param>
        /// <param name="checkFunction">Function to check the restriction</param>
        public static void RegisterRestriction(string name, Func<AbilityContext, Restriction, BaseCard, bool> checkFunction)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Restriction name cannot be null or empty");
                
            if (checkFunction == null)
                throw new ArgumentNullException(nameof(checkFunction));
                
            CheckRestrictions[name] = checkFunction;
            Debug.Log($"Registered custom restriction: {name}");
        }
        
        /// <summary>
        /// Check if a restriction type is registered
        /// </summary>
        /// <param name="restrictionName">Name of the restriction</param>
        /// <returns>True if restriction is registered</returns>
        public static bool HasRestriction(string restrictionName)
        {
            return CheckRestrictions.ContainsKey(restrictionName);
        }
        
        /// <summary>
        /// Get all registered restriction names
        /// </summary>
        /// <returns>List of restriction names</returns>
        public static List<string> GetAllRestrictionNames()
        {
            return CheckRestrictions.Keys.ToList();
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information about this restriction
        /// </summary>
        /// <returns>Debug information object</returns>
        public override object GetDebugInfo()
        {
            return new
            {
                type,
                restriction,
                parameters = parameters?.ToString(),
                hasRestrictionFunction = !string.IsNullOrEmpty(restriction) && CheckRestrictions.ContainsKey(restriction),
                registeredRestrictions = CheckRestrictions.Count
            };
        }
        
        /// <summary>
        /// String representation of this restriction
        /// </summary>
        /// <returns>String description</returns>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(restriction))
                return $"Restriction[{type}]: {restriction}";
            if (!string.IsNullOrEmpty(type))
                return $"Restriction[{type}]";
            return "Restriction[empty]";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Properties for restriction configuration
    /// </summary>
    [Serializable]
    public class RestrictionProperties
    {
        public string type;
        public string restricts;
        public object parameters;
        public Func<AbilityContext, bool> condition;
        public object source;
    }
}
