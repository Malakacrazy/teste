using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for action abilities that can be executed by players.
    /// Inherits from BaseAbility and adds card play restrictions and context creation.
    /// </summary>
    public partial class BaseAction : BaseAbility
    {
        [Header("Action Properties")]
        public BaseCard card;

        /// <summary>
        /// Constructor for BaseAction with card, costs, and optional target
        /// </summary>
        /// <param name="cardSource">The card this action belongs to</param>
        /// <param name="costs">List of costs required to execute this action</param>
        /// <param name="targetConfiguration">Optional target configuration</param>
        public BaseAction(BaseCard cardSource, List<ICost> costs = null, object targetConfiguration = null)
        {
            card = cardSource;
            
            // Initialize base ability properties
            var properties = new Dictionary<string, object>
            {
                ["cost"] = costs ?? new List<ICost>()
            };
            
            if (targetConfiguration != null)
            {
                properties["target"] = targetConfiguration;
            }
            
            Initialize(cardSource?.game, cardSource, properties);
            
            // Set action-specific properties
            abilityType = AbilityTypes.Action;
            cannotTargetFirst = true;
        }

        /// <summary>
        /// Constructor with CardAbilityProperties
        /// </summary>
        /// <param name="cardSource">The card this action belongs to</param>
        /// <param name="properties">Card ability properties</param>
        public BaseAction(BaseCard cardSource, CardAbilityProperties properties) : base(cardSource?.game, cardSource, properties)
        {
            card = cardSource;
            abilityType = AbilityTypes.Action;
            cannotTargetFirst = true;
        }

        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public BaseAction()
        {
            abilityType = AbilityTypes.Action;
            cannotTargetFirst = true;
        }

        /// <summary>
        /// Checks if this action meets all requirements for execution
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Empty string if requirements are met, error string otherwise</returns>
        public override string MeetsRequirements(AbilityContext context)
        {
            // Check limited card restrictions
            if (IsCardPlayed() && card != null && card.IsLimited() && 
                context.player.limitedPlayed >= context.player.maxLimited)
            {
                return "limited";
            }

            return base.MeetsRequirements(context);
        }

        /// <summary>
        /// Creates an ability context for this action
        /// </summary>
        /// <param name="player">Player executing the action (defaults to card controller)</param>
        /// <returns>New ability context</returns>
        public override AbilityContext CreateContext(Player player = null)
        {
            if (player == null)
                player = card?.controller;

            var properties = new AbilityContextProperties
            {
                ability = this,
                game = card?.game ?? game,
                player = player,
                source = card,
                stage = Stages.PreTarget
            };

            var contextGO = new GameObject("BaseActionContext");
            var context = contextGO.AddComponent<AbilityContext>();
            context.Initialize(properties);
            
            return context;
        }

        /// <summary>
        /// Indicates this is an action ability
        /// </summary>
        /// <returns>Always returns true</returns>
        public virtual bool IsAction()
        {
            return true;
        }

        /// <summary>
        /// Checks if this action represents playing a card
        /// </summary>
        /// <returns>True if this action plays the card</returns>
        public virtual bool IsCardPlayed()
        {
            // Default implementation - override in card play actions
            return false;
        }

        /// <summary>
        /// Gets the card associated with this action
        /// </summary>
        /// <returns>The source card</returns>
        public virtual BaseCard GetCard()
        {
            return card;
        }

        /// <summary>
        /// Checks if this action can be executed in the current game state
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if the action can be executed</returns>
        public override bool CanExecute(AbilityContext context)
        {
            // Check basic ability conditions
            if (!base.CanExecute(context))
                return false;

            // Check if card is in a valid state to execute actions
            if (card != null)
            {
                if (card.facedown && card.location != Locations.PlayArea)
                    return false;

                if (card.IsBlank())
                    return false;

                if (!card.CanTriggerAbilities(context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the title of this action for display
        /// </summary>
        /// <returns>Action title</returns>
        public override string GetTitle()
        {
            if (!string.IsNullOrEmpty(title))
                return title;

            return card?.printedName ?? "Unknown Action";
        }

        /// <summary>
        /// Executes the action with the given context
        /// </summary>
        /// <param name="context">Ability context</param>
        public override void Execute(AbilityContext context)
        {
            try
            {
                // Display message about action execution
                DisplayMessage(context);

                // Execute the action handler
                base.Execute(context);

                // Increment limited count if this is a limited card play
                if (IsCardPlayed() && card != null && card.IsLimited())
                {
                    context.player.limitedPlayed++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing BaseAction {GetTitle()}: {e.Message}");
            }
        }

        /// <summary>
        /// Display a message about the action being used
        /// </summary>
        /// <param name="context">Ability context</param>
        public override void DisplayMessage(AbilityContext context)
        {
            if (card != null && context?.game != null)
            {
                string actionText = !string.IsNullOrEmpty(title) ? title : "uses";
                context.game.AddMessage("{0} {1} {2}", context.player, actionText, card.printedName);
            }
        }

        /// <summary>
        /// Gets the reduced cost for this action in the given context
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Reduced cost amount</returns>
        public override int GetReducedCost(AbilityContext context)
        {
            if (card != null)
            {
                return card.GetReducedCost(context);
            }
            
            return base.GetReducedCost(context);
        }

        /// <summary>
        /// Checks if this action has legal targets
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if there are legal targets</returns>
        public override bool HasLegalTargets(AbilityContext context)
        {
            // If no targets required, always legal
            if (target == null && (targets == null || targets.Count == 0))
                return true;

            return base.HasLegalTargets(context);
        }

        /// <summary>
        /// String representation of this action
        /// </summary>
        /// <returns>String describing this action</returns>
        public override string ToString()
        {
            return $"BaseAction[{card?.printedName ?? "Unknown"}]: {GetTitle()}";
        }

        // Property compatibility aliases
        public BaseCard Card => card;
        public bool CannotBeCancelled => cannotTargetFirst; // Mapped to existing property
    }

    /// <summary>
    /// Extensions for BaseAction functionality
    /// </summary>
    public static class BaseActionExtensions
    {
        /// <summary>
        /// Check if an action is executable by a specific player
        /// </summary>
        /// <param name="action">Action to check</param>
        /// <param name="player">Player to check for</param>
        /// <returns>True if the player can execute this action</returns>
        public static bool CanBeExecutedBy(this BaseAction action, Player player)
        {
            if (action.card == null || player == null)
                return false;

            // Basic ownership check
            if (action.card.controller != player)
                return false;

            // Create context and check execution
            var context = action.CreateContext(player);
            return action.CanExecute(context);
        }

        /// <summary>
        /// Get all actions from a card
        /// </summary>
        /// <param name="card">Card to get actions from</param>
        /// <returns>List of actions on the card</returns>
        public static List<BaseAction> GetActions(this BaseCard card)
        {
            var actions = new List<BaseAction>();
            
            if (card.actions != null)
            {
                actions.AddRange(card.actions.OfType<BaseAction>());
            }

            return actions;
        }

        /// <summary>
        /// Get executable actions for a player on a card
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <param name="player">Player to check for</param>
        /// <returns>List of executable actions</returns>
        public static List<BaseAction> GetExecutableActions(this BaseCard card, Player player)
        {
            return card.GetActions().Where(action => action.CanBeExecutedBy(player)).ToList();
        }
    }
}