using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for card selector configuration
    /// </summary>
    [Serializable]
    public class CardSelectorProperties
    {
        [Header("Selection Parameters")]
        public int numCards = 1;
        public Func<int> numCardsFunc = null;
        public Func<BaseCard, AbilityContext, bool> cardCondition = null;
        public List<string> cardType = new List<string>();
        public bool multiSelect = false;
        public string mode = null;
        public string maxStat = null;
        public bool targets = false; // Maps to checkTarget in BaseCardSelector
        
        [Header("Advanced Selection Properties")]
        public Func<BaseCard, int> cardStat = null; // For MaxStatCardSelector
        public Func<Ring, AbilityContext, bool> ringCondition = null; // For RingSelector
        public List<Func<AbilityContext, bool>> choices = null; // For SelectSelector
        public GameAction gameAction = null; // For various selectors
        
        [Header("Targeting")]
        public string controller = Players.Any;
        public List<string> location = new List<string>();
        
        [Header("Options")]
        public bool optional = false;
        public bool ordered = false;
        public string activePromptTitle = null;
        
        public CardSelectorProperties()
        {
            // Set default card condition if none provided
            cardCondition ??= (card, context) => true;
            
            // Set default card types if none provided
            if (cardType.Count == 0)
            {
                cardType = new List<string>
                {
                    CardTypes.Attachment,
                    CardTypes.Character,
                    CardTypes.Event,
                    CardTypes.Holding,
                    CardTypes.Stronghold,
                    CardTypes.Role,
                    CardTypes.Province
                };
            }
        }
        
        /// <summary>
        /// Copy constructor for property inheritance
        /// </summary>
        public CardSelectorProperties(CardSelectorProperties other)
        {
            numCards = other.numCards;
            numCardsFunc = other.numCardsFunc;
            cardCondition = other.cardCondition;
            cardType = new List<string>(other.cardType);
            multiSelect = other.multiSelect;
            mode = other.mode;
            maxStat = other.maxStat;
            controller = other.controller;
            location = new List<string>(other.location);
            optional = other.optional;
            ordered = other.ordered;
            activePromptTitle = other.activePromptTitle;
        }
    }
    
    /// <summary>
    /// Factory class for creating card selectors based on mode and properties.
    /// Provides a unified interface for different card selection strategies.
    /// </summary>
    public static class CardSelector
    {
        #region Default Properties
        
        /// <summary>
        /// Default properties used when none are specified
        /// </summary>
        private static readonly CardSelectorProperties DefaultProperties = new CardSelectorProperties
        {
            numCards = 1,
            cardCondition = (card, context) => true,
            numCardsFunc = () => 1,
            cardType = new List<string>
            {
                CardTypes.Attachment,
                CardTypes.Character, 
                CardTypes.Event,
                CardTypes.Holding,
                CardTypes.Stronghold,
                CardTypes.Role,
                CardTypes.Province
            },
            multiSelect = false
        };
        
        #endregion
        
        #region Mode to Selector Mapping
        
        /// <summary>
        /// Dictionary mapping selector modes to their factory functions
        /// </summary>
        private static readonly Dictionary<string, Func<CardSelectorProperties, BaseCardSelector>> ModeToSelector = 
            new Dictionary<string, Func<CardSelectorProperties, BaseCardSelector>>
            {
                [TargetModes.Ability] = p => new SingleCardSelector(p),
                [TargetModes.AutoSingle] = p => new SingleCardSelector(p),
                [TargetModes.Exactly] = p => new ExactlyXCardSelector(p.numCards, p),
                [TargetModes.ExactlyVariable] = p => new ExactlyVariableXCardSelector(
                    context => p.numCardsFunc != null ? p.numCardsFunc() : 1, p),
                [TargetModes.MaxStat] = p => new MaxStatCardSelector(p),
                [TargetModes.Single] = p => new SingleCardSelector(p),
                [TargetModes.Token] = p => new SingleCardSelector(p),
                [TargetModes.Unlimited] = p => new UnlimitedCardSelector(p),
                [TargetModes.UpTo] = p => new UpToXCardSelector(p.numCards, p),
                [TargetModes.UpToVariable] = p => new UpToVariableXCardSelector(
                    context => p.numCardsFunc != null ? p.numCardsFunc() : 1, p),
                [TargetModes.Ring] = p => new RingSelector(p),
                [TargetModes.Select] = p => new SelectSelector(p)
            };
        
        #endregion
        
        #region Public Factory Methods
        
        /// <summary>
        /// Main factory method for creating card selectors
        /// </summary>
        /// <param name="properties">Selector properties</param>
        /// <returns>Configured card selector</returns>
        public static BaseCardSelector For(object properties)
        {
            var selectorProperties = GetDefaultedProperties(properties);
            
            if (!ModeToSelector.TryGetValue(selectorProperties.mode, out var factory))
            {
                throw new ArgumentException($"Unknown card selector mode: {selectorProperties.mode}");
            }
            
            var selector = factory(selectorProperties);
            
            // Log selector creation in debug builds
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🎯 Created {selectorProperties.mode} selector for {selectorProperties.numCards} cards");
#endif
            
            return selector;
        }
        
        /// <summary>
        /// Create a single card selector
        /// </summary>
        public static SingleCardSelector Single(Func<BaseCard, AbilityContext, bool> condition = null)
        {
            var properties = new CardSelectorProperties
            {
                numCards = 1,
                mode = TargetModes.Single,
                cardCondition = condition ?? ((card, context) => true)
            };
            
            return new SingleCardSelector(properties);
        }
        
        /// <summary>
        /// Create an up-to-X card selector
        /// </summary>
        public static UpToXCardSelector UpTo(int maxCards, Func<BaseCard, AbilityContext, bool> condition = null)
        {
            var properties = new CardSelectorProperties
            {
                numCards = maxCards,
                mode = TargetModes.UpTo,
                cardCondition = condition ?? ((card, context) => true)
            };
            
            return new UpToXCardSelector(maxCards, properties);
        }
        
        /// <summary>
        /// Create an exactly-X card selector
        /// </summary>
        public static ExactlyXCardSelector Exactly(int numCards, Func<BaseCard, AbilityContext, bool> condition = null)
        {
            var properties = new CardSelectorProperties
            {
                numCards = numCards,
                mode = TargetModes.Exactly,
                cardCondition = condition ?? ((card, context) => true)
            };
            
            return new ExactlyXCardSelector(numCards, properties);
        }
        
        /// <summary>
        /// Create an unlimited card selector
        /// </summary>
        public static UnlimitedCardSelector Unlimited(Func<BaseCard, AbilityContext, bool> condition = null)
        {
            var properties = new CardSelectorProperties
            {
                numCards = 0,
                mode = TargetModes.Unlimited,
                cardCondition = condition ?? ((card, context) => true)
            };
            
            return new UnlimitedCardSelector(properties);
        }
        
        /// <summary>
        /// Create an exactly-variable-X card selector
        /// </summary>
        public static ExactlyVariableXCardSelector ExactlyVariable(Func<AbilityContext, int> numCardsFunc, Func<BaseCard, AbilityContext, bool> condition = null)
        {
            var properties = new CardSelectorProperties
            {
                mode = TargetModes.ExactlyVariable,
                cardCondition = condition ?? ((card, context) => true)
            };
            
            return new ExactlyVariableXCardSelector(numCardsFunc, properties);
        }
        
        /// <summary>
        /// Create a max stat card selector
        /// </summary>
        public static MaxStatCardSelector MaxStat(string statName, Func<BaseCard, AbilityContext, bool> condition = null)
        {
            var properties = new CardSelectorProperties
            {
                mode = TargetModes.MaxStat,
                maxStat = statName,
                cardCondition = condition ?? ((card, context) => true)
            };
            
            return new MaxStatCardSelector(properties);
        }
        
        #endregion
        
        #region Property Processing
        
        /// <summary>
        /// Convert input properties to CardSelectorProperties with defaults applied
        /// </summary>
        /// <param name="inputProperties">Input properties object</param>
        /// <returns>Processed CardSelectorProperties</returns>
        public static CardSelectorProperties GetDefaultedProperties(object inputProperties)
        {
            var properties = ConvertToCardSelectorProperties(inputProperties);
            
            // Apply defaults
            ApplyDefaultValues(properties);
            
            // Auto-determine mode if not specified
            if (string.IsNullOrEmpty(properties.mode))
            {
                properties.mode = DetermineMode(properties);
            }
            
            return properties;
        }
        
        /// <summary>
        /// Convert various input types to CardSelectorProperties
        /// </summary>
        private static CardSelectorProperties ConvertToCardSelectorProperties(object input)
        {
            if (input == null)
                return new CardSelectorProperties();
                
            if (input is CardSelectorProperties existing)
                return new CardSelectorProperties(existing);
                
            // Handle anonymous objects and dynamic properties
            var properties = new CardSelectorProperties();
            var inputType = input.GetType();
            
            // Use reflection to copy properties
            foreach (var prop in inputType.GetProperties())
            {
                var selectorProp = typeof(CardSelectorProperties).GetProperty(prop.Name);
                if (selectorProp != null && selectorProp.CanWrite)
                {
                    try
                    {
                        var value = prop.GetValue(input);
                        selectorProp.SetValue(properties, value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"⚠️ Failed to copy property {prop.Name}: {e.Message}");
                    }
                }
            }
            
            return properties;
        }
        
        /// <summary>
        /// Apply default values to properties
        /// </summary>
        private static void ApplyDefaultValues(CardSelectorProperties properties)
        {
            // Copy defaults for null/empty values
            properties.cardCondition ??= DefaultProperties.cardCondition;
            properties.numCardsFunc ??= DefaultProperties.numCardsFunc;
            
            if (properties.cardType.Count == 0)
            {
                properties.cardType = new List<string>(DefaultProperties.cardType);
            }
        }
        
        /// <summary>
        /// Auto-determine selector mode based on properties
        /// </summary>
        private static string DetermineMode(CardSelectorProperties properties)
        {
            // Check for max stat mode
            if (!string.IsNullOrEmpty(properties.maxStat))
            {
                return TargetModes.MaxStat;
            }
            
            // Check for single card selection
            if (properties.numCards == 1 && !properties.multiSelect)
            {
                return TargetModes.Single;
            }
            
            // Check for unlimited selection
            if (properties.numCards == 0)
            {
                return TargetModes.Unlimited;
            }
            
            // Default to up-to mode for multiple cards
            return TargetModes.UpTo;
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information about available selector modes
        /// </summary>
        public static string GetAvailableModes()
        {
            return string.Join(", ", ModeToSelector.Keys);
        }
        
        /// <summary>
        /// Validate selector properties
        /// </summary>
        public static bool ValidateProperties(CardSelectorProperties properties, out string errorMessage)
        {
            errorMessage = null;
            
            if (properties == null)
            {
                errorMessage = "Properties cannot be null";
                return false;
            }
            
            if (string.IsNullOrEmpty(properties.mode))
            {
                errorMessage = "Mode must be specified";
                return false;
            }
            
            if (!ModeToSelector.ContainsKey(properties.mode))
            {
                errorMessage = $"Unknown mode: {properties.mode}. Available modes: {GetAvailableModes()}";
                return false;
            }
            
            if (properties.numCards < 0)
            {
                errorMessage = "Number of cards cannot be negative";
                return false;
            }
            
            return true;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for CardSelector
    /// </summary>
    public static class CardSelectorExtensions
    {
        /// <summary>
        /// Create a selector with a specific card type filter
        /// </summary>
        public static BaseCardSelector ForCardType(this CardSelectorProperties properties, string cardType)
        {
            properties.cardType = new List<string> { cardType };
            return CardSelector.For(properties);
        }
        
        /// <summary>
        /// Create a selector with a specific location filter
        /// </summary>
        public static BaseCardSelector InLocation(this CardSelectorProperties properties, string location)
        {
            properties.location = new List<string> { location };
            return CardSelector.For(properties);
        }
        
        /// <summary>
        /// Create a selector with a controller filter
        /// </summary>
        public static BaseCardSelector ControlledBy(this CardSelectorProperties properties, string controller)
        {
            properties.controller = controller;
            return CardSelector.For(properties);
        }
        
        /// <summary>
        /// Make the selector optional
        /// </summary>
        public static BaseCardSelector Optional(this BaseCardSelector selector)
        {
            selector.optional = true;
            return selector;
        }
        
        /// <summary>
        /// Set a custom prompt title for the selector
        /// </summary>
        public static BaseCardSelector WithPrompt(this BaseCardSelector selector, string promptTitle)
        {
            // This would typically be handled by the prompt system
            return selector;
        }
    }
}
