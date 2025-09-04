using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Properties for card action abilities
    /// </summary>
    [System.Serializable]
    public class ActionProperties
    {
        public string title;
        public Func<AbilityContext, bool> condition;
        public Action<AbilityContext> effect;
        public List<ICost> cost;
        public AbilityLimit limit;
        public int max;
        public string maxIdentifier;
        public bool cannotTargetFirst;
        public string playType;
    }
    
    /// <summary>
    /// Properties for triggered abilities (reactions, interrupts)
    /// </summary>
    [System.Serializable]
    public class TriggeredAbilityProperties
    {
        public string title;
        public string when;
        public Func<object, AbilityContext, bool> condition;
        public Action<object, AbilityContext> effect;
        public List<ICost> cost;
        public AbilityLimit limit;
        public int max;
        public string maxIdentifier;
        public bool cannotTargetFirst;
        public List<string> location;
        public bool optional = true;
    }
    
    /// <summary>
    /// Properties for custom play actions
    /// </summary>
    [System.Serializable]
    public class CustomPlayActionProperties
    {
        public string title;
        public string playType;
        public Func<Player, BaseCard, bool> condition;
        public Action<Player, BaseCard, AbilityContext> effect;
        public List<ICost> cost;
        public int priorityLevelIncrease = 0;
        public bool cannotTargetFirst;
        public List<string> location;
    }
    
    /// <summary>
    /// Properties for persistent effects
    /// </summary>
    [System.Serializable]
    public class PersistentEffectProperties
    {
        public string location;
        public object effect;
        public object condition;
        public object match;
        public string targetController;
        public object Ref; // Reference for tracking effect in engine
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public PersistentEffectProperties()
        {
        }
        
        /// <summary>
        /// Constructor that takes a single effect argument
        /// </summary>
        public PersistentEffectProperties(object effectArg)
        {
            effect = effectArg;
        }
    }
    
    /// <summary>
    /// Properties for attachment conditions
    /// </summary>
    [System.Serializable]
    public class AttachmentConditionProperties
    {
        public int limit;
        public bool myControl;
        public bool unique;
        public object faction;
        public object trait;
        public object limitTrait;
    }
    
    /// <summary>
    /// Properties for while attached effects
    /// </summary>
    [System.Serializable]
    public class WhileAttachedProperties
    {
        public object condition;
        public object match;
        public object effect;
    }
    
    /// <summary>
    /// Persistent effect data
    /// </summary>
    [System.Serializable]
    public class PersistentEffect
    {
        public string duration;
        public string location;
        public object effect;
        public object condition;
        public object match;
        public string targetController;
        public object reference;
    }
    
    /// <summary>
    /// Helper class for creating ability properties
    /// </summary>
    public static class AbilityPropertiesHelper
    {
        public static ActionProperties CreateActionProperties(
            string title = "",
            Func<AbilityContext, bool> condition = null,
            Action<AbilityContext> effect = null,
            List<ICost> cost = null,
            AbilityLimit limit = null,
            int max = 0,
            string maxIdentifier = "",
            bool cannotTargetFirst = false,
            string playType = "")
        {
            return new ActionProperties
            {
                title = title,
                condition = condition,
                effect = effect,
                cost = cost ?? new List<ICost>(),
                limit = limit,
                max = max,
                maxIdentifier = maxIdentifier,
                cannotTargetFirst = cannotTargetFirst,
                playType = playType
            };
        }
        
        public static TriggeredAbilityProperties CreateTriggeredAbilityProperties(
            string title = "",
            string when = "",
            Func<object, AbilityContext, bool> condition = null,
            Action<object, AbilityContext> effect = null,
            List<ICost> cost = null,
            AbilityLimit limit = null,
            int max = 0,
            string maxIdentifier = "",
            bool cannotTargetFirst = false,
            List<string> location = null,
            bool optional = true)
        {
            return new TriggeredAbilityProperties
            {
                title = title,
                when = when,
                condition = condition,
                effect = effect,
                cost = cost ?? new List<ICost>(),
                limit = limit,
                max = max,
                maxIdentifier = maxIdentifier,
                cannotTargetFirst = cannotTargetFirst,
                location = location ?? new List<string> { Locations.PlayArea },
                optional = optional
            };
        }
        
        public static CustomPlayActionProperties CreateCustomPlayActionProperties(
            string title = "",
            string playType = "",
            Func<Player, BaseCard, bool> condition = null,
            Action<Player, BaseCard, AbilityContext> effect = null,
            List<ICost> cost = null,
            int priorityLevelIncrease = 0,
            bool cannotTargetFirst = false,
            List<string> location = null)
        {
            return new CustomPlayActionProperties
            {
                title = title,
                playType = playType,
                condition = condition,
                effect = effect,
                cost = cost ?? new List<ICost>(),
                priorityLevelIncrease = priorityLevelIncrease,
                cannotTargetFirst = cannotTargetFirst,
                location = location ?? new List<string> { Locations.Hand }
            };
        }
    }
}
