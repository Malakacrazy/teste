using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Factory and utility class for creating and managing ring effects.
    /// Provides context creation for different elemental ring effects.
    /// </summary>
    public static class RingEffects
    {
        /// <summary>
        /// Dictionary mapping element names to their effect factories
        /// </summary>
        private static readonly Dictionary<string, Func<bool, BaseAbility>> ElementToEffect = new Dictionary<string, Func<bool, BaseAbility>>
        {
            ["air"] = (optional) => new AirRingEffect(optional),
            ["earth"] = (optional) => new EarthRingEffect(optional),
            ["fire"] = (optional) => new FireRingEffect(optional),
            ["void"] = (optional) => new VoidRingEffect(optional),
            ["water"] = (optional) => new WaterRingEffect(optional)
        };

        /// <summary>
        /// Dictionary mapping element names to their display names
        /// </summary>
        private static readonly Dictionary<string, string> RingNames = new Dictionary<string, string>
        {
            ["air"] = "Air Ring",
            ["earth"] = "Earth Ring", 
            ["fire"] = "Fire Ring",
            ["void"] = "Void Ring",
            ["water"] = "Water Ring"
        };

        /// <summary>
        /// Creates a framework context for executing a ring effect
        /// </summary>
        /// <param name="player">Player executing the ring effect</param>
        /// <param name="element">Ring element (air, earth, fire, void, water)</param>
        /// <param name="optional">Whether the effect is optional</param>
        /// <returns>Ability context configured for the ring effect</returns>
        public static AbilityContext ContextFor(Player player, string element, bool optional = true)
        {
            if (player?.game == null)
            {
                Debug.LogError($"RingEffects.ContextFor: Invalid player or game for element {element}");
                return null;
            }

            if (!ElementToEffect.ContainsKey(element))
            {
                throw new ArgumentException($"Unknown ring effect of {element}");
            }

            var factory = ElementToEffect[element];
            var context = player.game.GetFrameworkContext(player);
            
            if (context == null)
            {
                Debug.LogError($"RingEffects.ContextFor: Failed to get framework context for {element}");
                return null;
            }

            // Set the ring as the source
            if (player.game.rings.ContainsKey(element))
            {
                context.source = player.game.rings[element];
            }
            else
            {
                Debug.LogWarning($"RingEffects.ContextFor: Ring {element} not found in game");
            }

            // Create and set the ring effect ability
            context.ability = factory(optional);
            
            return context;
        }

        /// <summary>
        /// Gets the display name for a ring element
        /// </summary>
        /// <param name="element">Ring element</param>
        /// <returns>Display name of the ring</returns>
        public static string GetRingName(string element)
        {
            if (RingNames.ContainsKey(element))
            {
                return RingNames[element];
            }
            
            Debug.LogWarning($"RingEffects.GetRingName: Unknown ring element {element}");
            return $"{char.ToUpper(element[0])}{element.Substring(1)} Ring";
        }

        /// <summary>
        /// Gets all available ring elements
        /// </summary>
        /// <returns>List of all ring element names</returns>
        public static List<string> GetAllElements()
        {
            return new List<string>(ElementToEffect.Keys);
        }

        /// <summary>
        /// Gets all ring display names
        /// </summary>
        /// <returns>List of all ring display names</returns>
        public static List<string> GetAllRingNames()
        {
            return new List<string>(RingNames.Values);
        }

        /// <summary>
        /// Checks if an element is valid
        /// </summary>
        /// <param name="element">Element to check</param>
        /// <returns>True if the element is valid</returns>
        public static bool IsValidElement(string element)
        {
            return !string.IsNullOrEmpty(element) && ElementToEffect.ContainsKey(element);
        }

        /// <summary>
        /// Creates a ring effect ability directly without context
        /// </summary>
        /// <param name="element">Ring element</param>
        /// <param name="optional">Whether the effect is optional</param>
        /// <returns>Ring effect ability</returns>
        public static BaseAbility CreateRingEffect(string element, bool optional = true)
        {
            if (!ElementToEffect.ContainsKey(element))
            {
                throw new ArgumentException($"Unknown ring effect of {element}");
            }

            var factory = ElementToEffect[element];
            return factory(optional);
        }

        /// <summary>
        /// Gets the element from a ring name
        /// </summary>
        /// <param name="ringName">Display name of the ring</param>
        /// <returns>Element name, or null if not found</returns>
        public static string GetElementFromRingName(string ringName)
        {
            foreach (var kvp in RingNames)
            {
                if (kvp.Value.Equals(ringName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }

    #region Ring Effect Implementations

    /// <summary>
    /// Base class for all ring effects
    /// </summary>
    public abstract class RingEffect : BaseAbility
    {
        [Header("Ring Effect Properties")]
        public bool isOptional = true;

        protected RingEffect(bool optional = true)
        {
            isOptional = optional;
            abilityType = AbilityTypes.RingEffect;
            title = GetEffectName();
        }

        /// <summary>
        /// Gets the name of this ring effect
        /// </summary>
        /// <returns>Effect name</returns>
        public abstract string GetEffectName();

        /// <summary>
        /// Gets the element associated with this ring effect
        /// </summary>
        /// <returns>Element name</returns>
        public abstract string GetElement();

        public override bool IsTriggeredAbility()
        {
            return false; // Ring effects are not triggered abilities
        }
    }

    // AirRingEffect class moved to Rings/AirRingEffect.cs to avoid duplicate definition

    // EarthRingEffect class moved to Rings/EarthRingEffect.cs to avoid duplicate definition

    // FireRingEffect class moved to Rings/FireRingEffect.cs to avoid duplicate definition

    // VoidRingEffect class moved to Rings/VoidRingEffect.cs to avoid duplicate definition

    // WaterRingEffect class moved to Rings/WaterRingEffect.cs to avoid duplicate definition

    #endregion

    /// <summary>
    /// Extension methods for ring effects
    /// </summary>
    public static class RingEffectsExtensions
    {
        /// <summary>
        /// Execute a ring effect for a specific element
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="element">Ring element</param>
        /// <param name="player">Player executing the effect</param>
        /// <param name="optional">Whether the effect is optional</param>
        public static void ExecuteRingEffect(this Game game, string element, Player player, bool optional = true)
        {
            try
            {
                var context = RingEffects.ContextFor(player, element, optional);
                if (context?.ability != null)
                {
                    context.ability.Execute(context);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing ring effect for {element}: {e.Message}");
            }
        }

        /// <summary>
        /// Check if a ring has an available effect
        /// </summary>
        /// <param name="ring">Ring to check</param>
        /// <param name="player">Player who would execute the effect</param>
        /// <returns>True if the ring effect can be executed</returns>
        public static bool HasAvailableEffect(this Ring ring, Player player)
        {
            if (ring == null || player == null)
                return false;

            try
            {
                var context = RingEffects.ContextFor(player, ring.element, true);
                return context?.ability?.CanExecute(context) ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}