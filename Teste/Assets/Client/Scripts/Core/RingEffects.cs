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

    /// <summary>
    /// Air Ring Effect: Draw a card and opponent discards a card
    /// </summary>
    public class AirRingEffect : RingEffect
    {
        public AirRingEffect(bool optional = true) : base(optional) { }

        public override string GetEffectName() => "Air Ring Effect";
        public override string GetElement() => RingElements.Air;

        public override void ExecuteHandler(AbilityContext context)
        {
            if (context?.player == null)
                return;

            // Draw a card
            context.game.actions.Draw(context.player, 1).Execute(context);

            // Opponent discards a card from hand
            var opponent = context.game.GetOtherPlayer(context.player);
            if (opponent != null && opponent.hand.Count > 0)
            {
                if (isOptional)
                {
                    context.game.PromptWithHandlerMenu(context.player, new MenuPromptProperties
                    {
                        activePromptTitle = "Choose opponent discard option",
                        source = context.source,
                        choices = new List<string> { "Force opponent to discard", "Skip" },
                        handlers = new List<System.Action>
                        {
                            () => ForceOpponentDiscard(context, opponent),
                            () => { /* Skip - no action needed */ }
                        }
                    });
                }
                else
                {
                    ForceOpponentDiscard(context, opponent);
                }
            }
        }

        private void ForceOpponentDiscard(AbilityContext context, Player opponent)
        {
            context.game.actions.DiscardAtRandom(opponent, 1).Execute(context);
        }
    }

    /// <summary>
    /// Earth Ring Effect: Draw a card and gain fate equal to opponent's hand size
    /// </summary>
    public class EarthRingEffect : RingEffect
    {
        public EarthRingEffect(bool optional = true) : base(optional) { }

        public override string GetEffectName() => "Earth Ring Effect";
        public override string GetElement() => RingElements.Earth;

        public override void ExecuteHandler(AbilityContext context)
        {
            if (context?.player == null)
                return;

            // Draw a card
            context.game.actions.Draw(context.player, 1).Execute(context);

            // Gain fate equal to opponent's hand size
            var opponent = context.game.GetOtherPlayer(context.player);
            if (opponent != null)
            {
                var fateGain = opponent.hand.Count;
                if (fateGain > 0)
                {
                    context.game.actions.GainFate(context.player, fateGain).Execute(context);
                    context.game.AddMessage("{0} gains {1} fate from Earth Ring (opponent's hand size)", 
                        context.player, fateGain);
                }
            }
        }
    }

    /// <summary>
    /// Fire Ring Effect: Bow a character and gain honor equal to its glory
    /// </summary>
    public class FireRingEffect : RingEffect
    {
        public FireRingEffect(bool optional = true) : base(optional) { }

        public override string GetEffectName() => "Fire Ring Effect";
        public override string GetElement() => RingElements.Fire;

        public override void ExecuteHandler(AbilityContext context)
        {
            if (context?.player == null)
                return;

            // Target selection for bowing
            var validTargets = context.game.FindAnyCardsInPlay(card =>
                card.GetCardType() == CardTypes.Character && 
                card.CanBeBowed(context));

            if (validTargets.Count == 0)
                return;

            context.game.PromptForSelect(context.player, new SelectCardPromptProperties
            {
                activePromptTitle = "Choose a character to bow",
                cardType = CardTypes.Character,
                cardCondition = card => card.CanBeBowed(context),
                onSelect = (player, card) =>
                {
                    // Bow the character
                    context.game.actions.Bow(card).Execute(context);
                    
                    // Gain honor equal to its glory
                    var glory = card.GetGlory();
                    if (glory > 0)
                    {
                        context.game.actions.GainHonor(context.player, glory).Execute(context);
                        context.game.AddMessage("{0} gains {1} honor from Fire Ring (character's glory)", 
                            context.player, glory);
                    }
                    return true;
                },
                onCancel = () => true
            });
        }
    }

    /// <summary>
    /// Void Ring Effect: Remove fate from characters and gain honor
    /// </summary>
    public class VoidRingEffect : RingEffect
    {
        public VoidRingEffect(bool optional = true) : base(optional) { }

        public override string GetEffectName() => "Void Ring Effect";
        public override string GetElement() => RingElements.Void;

        public override void ExecuteHandler(AbilityContext context)
        {
            if (context?.player == null)
                return;

            var charactersWithFate = context.game.FindAnyCardsInPlay(card =>
                card.GetCardType() == CardTypes.Character && card.fate > 0);

            if (charactersWithFate.Count == 0)
                return;

            // Remove 1 fate from each character
            int totalFateRemoved = 0;
            foreach (var character in charactersWithFate)
            {
                if (character.fate > 0)
                {
                    context.game.actions.RemoveFate(character, 1).Execute(context);
                    totalFateRemoved++;
                }
            }

            // Gain honor equal to fate removed
            if (totalFateRemoved > 0)
            {
                context.game.actions.GainHonor(context.player, totalFateRemoved).Execute(context);
                context.game.AddMessage("{0} gains {1} honor from Void Ring (fate removed)", 
                    context.player, totalFateRemoved);
            }
        }
    }

    /// <summary>
    /// Water Ring Effect: Ready or bow a character and draw or discard a card
    /// </summary>
    public class WaterRingEffect : RingEffect
    {
        public WaterRingEffect(bool optional = true) : base(optional) { }

        public override string GetEffectName() => "Water Ring Effect";
        public override string GetElement() => RingElements.Water;

        public override void ExecuteHandler(AbilityContext context)
        {
            if (context?.player == null)
                return;

            // First choice: Ready or bow a character
            var bowableCharacters = context.game.FindAnyCardsInPlay(card =>
                card.GetCardType() == CardTypes.Character && card.CanBeBowed(context));
            var readyableCharacters = context.game.FindAnyCardsInPlay(card =>
                card.GetCardType() == CardTypes.Character && card.CanBeReadied(context));

            if (bowableCharacters.Count > 0 || readyableCharacters.Count > 0)
            {
                var choices = new List<string>();
                var handlers = new List<System.Action>();

                if (readyableCharacters.Count > 0)
                {
                    choices.Add("Ready a character");
                    handlers.Add(() => ChooseCharacterToReady(context, readyableCharacters));
                }

                if (bowableCharacters.Count > 0)
                {
                    choices.Add("Bow a character");
                    handlers.Add(() => ChooseCharacterToBow(context, bowableCharacters));
                }

                choices.Add("Skip character effect");
                handlers.Add(() => ChooseCardEffect(context));

                context.game.PromptWithHandlerMenu(context.player, new MenuPromptProperties
                {
                    activePromptTitle = "Choose Water Ring character effect",
                    source = context.source,
                    choices = choices,
                    handlers = handlers
                });
            }
            else
            {
                ChooseCardEffect(context);
            }
        }

        private void ChooseCharacterToReady(AbilityContext context, List<BaseCard> readyableCharacters)
        {
            context.game.PromptForSelect(context.player, new SelectCardPromptProperties
            {
                activePromptTitle = "Choose a character to ready",
                cardType = CardTypes.Character,
                cardCondition = card => card.CanBeReadied(context),
                onSelect = (player, card) =>
                {
                    context.game.actions.Ready(card).Execute(context);
                    ChooseCardEffect(context);
                    return true;
                }
            });
        }

        private void ChooseCharacterToBow(AbilityContext context, List<BaseCard> bowableCharacters)
        {
            context.game.PromptForSelect(context.player, new SelectCardPromptProperties
            {
                activePromptTitle = "Choose a character to bow",
                cardType = CardTypes.Character,
                cardCondition = card => card.CanBeBowed(context),
                onSelect = (player, card) =>
                {
                    context.game.actions.Bow(card).Execute(context);
                    ChooseCardEffect(context);
                    return true;
                }
            });
        }

        private void ChooseCardEffect(AbilityContext context)
        {
            // Second choice: Draw or discard a card
            var choices = new List<string> { "Draw a card" };
            var handlers = new List<System.Action>
            {
                () => context.game.actions.Draw(context.player, 1).Execute(context)
            };

            if (context.player.hand.Count > 0)
            {
                choices.Add("Discard a card");
                handlers.Add(() => PromptForCardDiscard(context));
            }

            context.game.PromptWithHandlerMenu(context.player, new MenuPromptProperties
            {
                activePromptTitle = "Choose Water Ring card effect",
                source = context.source,
                choices = choices,
                handlers = handlers
            });
        }

        private void PromptForCardDiscard(AbilityContext context)
        {
            context.game.PromptForSelect(context.player, new SelectCardPromptProperties
            {
                activePromptTitle = "Choose a card to discard",
                location = Locations.Hand,
                controller = Players.Self,
                onSelect = (player, card) =>
                {
                    context.game.actions.Discard(card).Execute(context);
                    return true;
                }
            });
        }
    }

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