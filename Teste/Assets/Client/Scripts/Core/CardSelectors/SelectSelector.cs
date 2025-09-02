using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for choice-based selections (not card-based).
    /// Perfect C# port of the original JavaScript SelectSelector.
    /// </summary>
    [Serializable]
    public class SelectSelector : BaseCardSelector
    {
        [Header("Select Choice Configuration")]
        public List<Func<AbilityContext, bool>> choices;
        
        public SelectSelector(CardSelectorProperties properties) : base(properties)
        {
            choices = properties.choices ?? new List<Func<AbilityContext, bool>>();
        }
        
        /// <summary>
        /// Constructor with explicit choices
        /// </summary>
        public SelectSelector(CardSelectorProperties properties, List<Func<AbilityContext, bool>> choiceConditions)
            : base(properties)
        {
            choices = choiceConditions ?? new List<Func<AbilityContext, bool>>();
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            if (context == null || choices == null || choices.Count == 0)
                return false;
            
            // Match JavaScript logic with underscore _.any()
            return choices.Any(condition => 
            {
                try
                {
                    return condition(context);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ SelectSelector choice condition error: {e.Message}");
                    return false;
                }
            });
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            return activePromptTitle ?? "Select one";
        }
        
        /// <summary>
        /// Get all valid choice indices
        /// </summary>
        public List<int> GetValidChoiceIndices(AbilityContext context)
        {
            if (context == null || choices == null)
                return new List<int>();
            
            var validIndices = new List<int>();
            for (int i = 0; i < choices.Count; i++)
            {
                try
                {
                    if (choices[i](context))
                        validIndices.Add(i);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ SelectSelector choice {i} condition error: {e.Message}");
                }
            }
            
            return validIndices;
        }
        
        /// <summary>
        /// Get count of valid choices
        /// </summary>
        public int GetValidChoiceCount(AbilityContext context)
        {
            return GetValidChoiceIndices(context).Count;
        }
        
        // Override base card methods to handle choice-specific logic
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            // Choice selectors typically select one choice
            return (selectedCards?.Count ?? 0) >= 1;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            // Choice selectors are typically single selection
            return (selectedCards?.Count ?? 0) >= 1;
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            // For choice selectors, we might want to return the choice index or value
            return selectedCards?.FirstOrDefault();
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return true; // Usually auto-fire on choice selection
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) >= 1 || optional;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) > 1;
        }
        
        /// <summary>
        /// Format choice selection parameter
        /// </summary>
        public int FormatChoiceSelectParam(List<int> selectedChoiceIndices)
        {
            return selectedChoiceIndices?.FirstOrDefault() ?? -1;
        }
        
        /// <summary>
        /// Check if a specific choice index is valid
        /// </summary>
        public bool IsValidChoice(int choiceIndex, AbilityContext context)
        {
            if (context == null || choices == null || choiceIndex < 0 || choiceIndex >= choices.Count)
                return false;
            
            try
            {
                return choices[choiceIndex](context);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ SelectSelector choice {choiceIndex} validation error: {e.Message}");
                return false;
            }
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new
            {
                baseInfo,
                choiceCount = choices?.Count ?? 0,
                hasChoices = choices != null && choices.Count > 0,
                selectorType = "Select"
            };
        }
        
        /// <summary>
        /// Get debug info with context for better debugging
        /// </summary>
        public object GetDebugInfo(AbilityContext context)
        {
            var baseInfo = base.GetDebugInfo();
            var validChoices = GetValidChoiceIndices(context);
            
            return new
            {
                baseInfo,
                choiceCount = choices?.Count ?? 0,
                validChoiceCount = validChoices.Count,
                validChoiceIndices = validChoices,
                hasChoices = choices != null && choices.Count > 0,
                selectorType = "Select"
            };
        }
    }
}
