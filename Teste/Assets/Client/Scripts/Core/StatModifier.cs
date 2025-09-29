using System;
using System.Reflection;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a modification to a card's statistics (military, political skill, cost, etc.).
    /// Used to track temporary and permanent stat changes from various sources.
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        [Header("Modifier Properties")]
        public int amount;
        public string modifierName;
        public bool countsAsBase;
        public string sourceType;
        public bool overrides;

        /// <summary>
        /// Creates a new stat modifier
        /// </summary>
        /// <param name="modifierAmount">Amount to modify by (can be negative)</param>
        /// <param name="name">Name/description of the modifier</param>
        /// <param name="overridesBase">Whether this modifier overrides the base value</param>
        /// <param name="type">Type of source causing this modifier</param>
        public StatModifier(int modifierAmount, string name, bool overridesBase = false, string type = null)
        {
            amount = modifierAmount;
            modifierName = name ?? "Unknown Modifier";
            overrides = overridesBase;
            sourceType = type;
            countsAsBase = false;
        }

        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public StatModifier()
        {
            amount = 0;
            modifierName = "Unknown Modifier";
            overrides = false;
            sourceType = null;
            countsAsBase = false;
        }

        /// <summary>
        /// Gets the effect name from an effect object
        /// </summary>
        /// <param name="effect">Effect object to extract name from</param>
        /// <returns>Effect name or "Unknown"</returns>
        public static string GetEffectName(object effect)
        {
            if (effect == null)
                return "Unknown";

            try
            {
                // Try to access context.source.name using reflection
                var effectType = effect.GetType();
                var contextProperty = effectType.GetProperty("context");
                var contextField = effectType.GetField("context");
                
                object contextValue = null;
                if (contextProperty != null)
                {
                    contextValue = contextProperty.GetValue(effect);
                }
                else if (contextField != null)
                {
                    contextValue = contextField.GetValue(effect);
                }
                
                if (contextValue != null)
                {
                    var context = contextValue;
                    if (context != null)
                    {
                        var contextType = context.GetType();
                        var sourceProperty = contextType.GetProperty("source");
                        var sourceField = contextType.GetField("source");
                        
                        object source = null;
                        if (sourceProperty != null)
                        {
                            source = sourceProperty.GetValue(context);
                        }
                        else if (sourceField != null)
                        {
                            source = sourceField.GetValue(context);
                        }
                        
                        if (source != null)
                        {
                            var sourceType = source.GetType();
                            var nameProperty = sourceType.GetProperty("name");
                            var nameField = sourceType.GetField("name");
                            
                            object nameValue = null;
                            if (nameProperty != null)
                            {
                                nameValue = nameProperty.GetValue(source);
                            }
                            else if (nameField != null)
                            {
                                nameValue = nameField.GetValue(source);
                            }
                                
                            if (nameValue != null)
                            {
                                return nameValue.ToString() ?? "Unknown";
                            }
                            
                            // Fallback to source object name
                            if (source is UnityEngine.Object unityObj)
                                return unityObj.name;
                        }
                    }
                }
                
                // Fallback to effect type name
                return effectType.Name;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"StatModifier.GetEffectName: Error extracting name from effect: {e.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the effect type from an effect object
        /// </summary>
        /// <param name="effect">Effect object to extract type from</param>
        /// <returns>Effect type or null</returns>
        public static string GetEffectType(object effect)
        {
            if (effect == null)
                return null;

            try
            {
                // Try to access context.source.type using reflection
                var effectType = effect.GetType();
                var contextProperty = effectType.GetProperty("context");
                var contextField = effectType.GetField("context");
                
                object contextValue = null;
                if (contextProperty != null)
                {
                    contextValue = contextProperty.GetValue(effect);
                }
                else if (contextField != null)
                {
                    contextValue = contextField.GetValue(effect);
                }
                
                if (contextValue != null)
                {
                    var context = contextValue;
                    if (context != null)
                    {
                        var contextType = context.GetType();
                        var sourceProperty = contextType.GetProperty("source");
                        var sourceField = contextType.GetField("source");
                        
                        object source = null;
                        if (sourceProperty != null)
                        {
                            source = sourceProperty.GetValue(context);
                        }
                        else if (sourceField != null)
                        {
                            source = sourceField.GetValue(context);
                        }
                        
                        if (source != null)
                        {
                            var sourceType = source.GetType();
                            var typeProperty = sourceType.GetProperty("type");
                            var typeField = sourceType.GetField("type");
                            
                            object typeValue = null;
                            if (typeProperty != null)
                            {
                                typeValue = typeProperty.GetValue(source);
                            }
                            else if (typeField != null)
                            {
                                typeValue = typeField.GetValue(source);
                            }
                                
                            if (typeValue != null)
                            {
                                return typeValue.ToString();
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"StatModifier.GetEffectType: Error extracting type from effect: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the card type from a card object
        /// </summary>
        /// <param name="card">Card object to extract type from</param>
        /// <returns>Card type or null</returns>
        public static string GetCardType(object card)
        {
            if (card == null)
                return null;

            try
            {
                var cardType = card.GetType();
                var typeProperty = cardType.GetProperty("type");
                var typeField = cardType.GetField("type");
                
                object typeValue = null;
                if (typeProperty != null)
                {
                    typeValue = typeProperty.GetValue(card);
                }
                else if (typeField != null)
                {
                    typeValue = typeField.GetValue(card);
                }
                
                if (typeValue != null)
                {
                    return typeValue.ToString();
                }

                // Check if it's a BaseCard
                if (card is BaseCard baseCard)
                {
                    return baseCard.type;
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"StatModifier.GetCardType: Error extracting type from card: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a stat modifier from an effect
        /// </summary>
        /// <param name="modifierAmount">Amount to modify by</param>
        /// <param name="effect">Effect causing the modification</param>
        /// <param name="overridesBase">Whether this modifier overrides the base value</param>
        /// <param name="customName">Custom name for the modifier</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromEffect(int modifierAmount, object effect, bool overridesBase = false, string customName = null)
        {
            var effectName = customName ?? GetEffectName(effect);
            var effectType = GetEffectType(effect);
            
            return new StatModifier(modifierAmount, effectName, overridesBase, effectType);
        }

        /// <summary>
        /// Creates a stat modifier from a card
        /// </summary>
        /// <param name="modifierAmount">Amount to modify by</param>
        /// <param name="card">Card causing the modification</param>
        /// <param name="modifierName">Name for the modifier</param>
        /// <param name="overridesBase">Whether this modifier overrides the base value</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromCard(int modifierAmount, object card, string modifierName, bool overridesBase = false)
        {
            var cardType = GetCardType(card);
            var name = modifierName ?? GetCardName(card) ?? "Card Effect";
            
            return new StatModifier(modifierAmount, name, overridesBase, cardType);
        }

        /// <summary>
        /// Creates a stat modifier from a status token
        /// </summary>
        /// <param name="modifierAmount">Amount to modify by</param>
        /// <param name="tokenName">Name of the status token</param>
        /// <param name="overridesBase">Whether this modifier overrides the base value</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromStatusToken(int modifierAmount, string tokenName, bool overridesBase = false)
        {
            var name = tokenName ?? "Status Token";
            return new StatModifier(modifierAmount, name, overridesBase, "token");
        }

        /// <summary>
        /// Creates a stat modifier from a persistent effect
        /// </summary>
        /// <param name="modifierAmount">Amount to modify by</param>
        /// <param name="effectName">Name of the effect</param>
        /// <param name="overridesBase">Whether this modifier overrides the base value</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromPersistentEffect(int modifierAmount, string effectName, bool overridesBase = false)
        {
            var name = effectName ?? "Persistent Effect";
            return new StatModifier(modifierAmount, name, overridesBase, "persistent");
        }

        /// <summary>
        /// Creates a stat modifier from a temporary effect
        /// </summary>
        /// <param name="modifierAmount">Amount to modify by</param>
        /// <param name="effectName">Name of the effect</param>
        /// <param name="overridesBase">Whether this modifier overrides the base value</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromTemporaryEffect(int modifierAmount, string effectName, bool overridesBase = false)
        {
            var name = effectName ?? "Temporary Effect";
            return new StatModifier(modifierAmount, name, overridesBase, "temporary");
        }

        /// <summary>
        /// Gets the name of a card object
        /// </summary>
        /// <param name="card">Card object</param>
        /// <returns>Card name or null</returns>
        private static string GetCardName(object card)
        {
            if (card == null)
                return null;

            try
            {
                // Check if it's a BaseCard
                if (card is BaseCard baseCard)
                {
                    return baseCard.printedName ?? baseCard.name;
                }

                // Try reflection for other card types
                var cardType = card.GetType();
                var nameProperty = cardType.GetProperty("name");
                var printedNameProperty = cardType.GetProperty("printedName");
                var nameField = cardType.GetField("name");
                var printedNameField = cardType.GetField("printedName");
                
                object nameValue = null;
                if (nameProperty != null)
                {
                    nameValue = nameProperty.GetValue(card);
                }
                else if (printedNameProperty != null)
                {
                    nameValue = printedNameProperty.GetValue(card);
                }
                else if (nameField != null)
                {
                    nameValue = nameField.GetValue(card);
                }
                else if (printedNameField != null)
                {
                    nameValue = printedNameField.GetValue(card);
                }
                
                if (nameValue != null)
                {
                    return nameValue.ToString();
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"StatModifier.GetCardName: Error extracting name from card: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if this modifier should stack with another modifier
        /// </summary>
        /// <param name="other">Other modifier to check against</param>
        /// <returns>True if modifiers can stack</returns>
        public bool CanStackWith(StatModifier other)
        {
            if (other == null)
                return true;

            // Overriding modifiers don't stack
            if (overrides || other.overrides)
                return false;

            // Same source modifiers typically don't stack (unless specifically allowed)
            if (modifierName == other.modifierName && sourceType == other.sourceType)
                return false;

            return true;
        }

        /// <summary>
        /// Applies this modifier to a base value
        /// </summary>
        /// <param name="baseValue">Base value to modify</param>
        /// <returns>Modified value</returns>
        public int Apply(int baseValue)
        {
            if (overrides)
                return amount;
            
            return Math.Max(0, baseValue + amount);
        }

        /// <summary>
        /// Creates a copy of this stat modifier
        /// </summary>
        /// <returns>Copied stat modifier</returns>
        public StatModifier Copy()
        {
            return new StatModifier(amount, modifierName, overrides, sourceType)
            {
                countsAsBase = countsAsBase
            };
        }

        /// <summary>
        /// Checks if this modifier has the same source as another
        /// </summary>
        /// <param name="other">Other modifier to compare</param>
        /// <returns>True if they have the same source</returns>
        public bool HasSameSource(StatModifier other)
        {
            if (other == null)
                return false;

            return modifierName == other.modifierName && sourceType == other.sourceType;
        }

        /// <summary>
        /// String representation of the stat modifier
        /// </summary>
        /// <returns>String describing the modifier</returns>
        public override string ToString()
        {
            var sign = amount >= 0 ? "+" : "";
            var overrideText = overrides ? " (override)" : "";
            return $"{modifierName}: {sign}{amount}{overrideText}";
        }

        /// <summary>
        /// Gets a display-friendly description of the modifier
        /// </summary>
        /// <returns>Display description</returns>
        public string GetDisplayDescription()
        {
            var sign = amount > 0 ? "+" : "";
            if (overrides)
                return $"{modifierName}: Set to {amount}";
            else
                return $"{modifierName}: {sign}{amount}";
        }

        // Property aliases for compatibility
        public int Amount => amount;
        public string Name => modifierName;
        public bool CountsAsBase => countsAsBase;
        public string Type => sourceType;
        public bool Overrides => overrides;
    }

    /// <summary>
    /// Extension methods for stat modifier functionality
    /// </summary>
    public static class StatModifierExtensions
    {
        /// <summary>
        /// Creates a stat modifier for military skill modification
        /// </summary>
        /// <param name="amount">Amount to modify military skill</param>
        /// <param name="source">Source of the modification</param>
        /// <param name="overrides">Whether to override base value</param>
        /// <returns>Military skill modifier</returns>
        public static StatModifier CreateMilitaryModifier(this int amount, object source, bool overrides = false)
        {
            var sourceName = StatModifier.GetEffectName(source) ?? "Military Modifier";
            return new StatModifier(amount, $"{sourceName} (Military)", overrides, StatModifier.GetEffectType(source));
        }

        /// <summary>
        /// Creates a stat modifier for political skill modification
        /// </summary>
        /// <param name="amount">Amount to modify political skill</param>
        /// <param name="source">Source of the modification</param>
        /// <param name="overrides">Whether to override base value</param>
        /// <returns>Political skill modifier</returns>
        public static StatModifier CreatePoliticalModifier(this int amount, object source, bool overrides = false)
        {
            var sourceName = StatModifier.GetEffectName(source) ?? "Political Modifier";
            return new StatModifier(amount, $"{sourceName} (Political)", overrides, StatModifier.GetEffectType(source));
        }

        /// <summary>
        /// Creates a stat modifier for both skill modification
        /// </summary>
        /// <param name="amount">Amount to modify both skills</param>
        /// <param name="source">Source of the modification</param>
        /// <param name="overrides">Whether to override base value</param>
        /// <returns>Both skills modifier</returns>
        public static StatModifier CreateBothSkillsModifier(this int amount, object source, bool overrides = false)
        {
            var sourceName = StatModifier.GetEffectName(source) ?? "Skills Modifier";
            return new StatModifier(amount, $"{sourceName} (Both Skills)", overrides, StatModifier.GetEffectType(source));
        }

        /// <summary>
        /// Creates a stat modifier for cost modification
        /// </summary>
        /// <param name="amount">Amount to modify cost</param>
        /// <param name="source">Source of the modification</param>
        /// <param name="overrides">Whether to override base value</param>
        /// <returns>Cost modifier</returns>
        public static StatModifier CreateCostModifier(this int amount, object source, bool overrides = false)
        {
            var sourceName = StatModifier.GetEffectName(source) ?? "Cost Modifier";
            return new StatModifier(amount, $"{sourceName} (Cost)", overrides, StatModifier.GetEffectType(source));
        }

        /// <summary>
        /// Creates a stat modifier for glory modification
        /// </summary>
        /// <param name="amount">Amount to modify glory</param>
        /// <param name="source">Source of the modification</param>
        /// <param name="overrides">Whether to override base value</param>
        /// <returns>Glory modifier</returns>
        public static StatModifier CreateGloryModifier(this int amount, object source, bool overrides = false)
        {
            var sourceName = StatModifier.GetEffectName(source) ?? "Glory Modifier";
            return new StatModifier(amount, $"{sourceName} (Glory)", overrides, StatModifier.GetEffectType(source));
        }
    }
}
