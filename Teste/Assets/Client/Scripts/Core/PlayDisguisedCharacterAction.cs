using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Cost for choosing a character to disguise when playing a disguised character
    /// </summary>
    public class ChooseDisguisedCharacterCost : ICost
    {
        private readonly bool intoConflictOnly;

        public ChooseDisguisedCharacterCost(bool intoConflictOnly = false)
        {
            this.intoConflictOnly = intoConflictOnly;
        }

        public bool CanPay(AbilityContext context)
        {
            return context.player.cardsInPlay.Any(card =>
                card.GetCardType() == CardTypes.Character &&
                context.source is BaseCard sourceCard &&
                sourceCard.CanDisguise(card, context, intoConflictOnly));
        }

        public void Resolve(AbilityContext context, CostResults results)
        {
            if (context.source is not BaseCard sourceCard)
            {
                results.cancelled = true;
                return;
            }

            var validCards = context.player.cardsInPlay
                .Where(card => card.GetCardType() == CardTypes.Character &&
                              sourceCard.CanDisguise(card, context, intoConflictOnly))
                .ToList();

            if (!validCards.Any())
            {
                results.cancelled = true;
                return;
            }

            // Create selection context for disguised character
            context.game.PromptForSelect(context.player, new SelectCardPromptProperties
            {
                activePromptTitle = "Choose a character to replace",
                cardType = CardTypes.Character,
                controller = Players.Self,
                cardCondition = card => sourceCard.CanDisguise(card, context, intoConflictOnly),
                context = context,
                onSelect = (player, card) =>
                {
                    context.SetCost("chooseDisguisedCharacter", card);
                    return true;
                },
                onCancel = () =>
                {
                    results.cancelled = true;
                    return true;
                }
            });
        }

        public void Pay(AbilityContext context, CostResults results)
        {
            // Cost is paid through selection - no additional payment needed
            results.success = true;
        }

        public bool IsOptional => false;
        public string Name => "Choose Disguised Character";
    }

    /// <summary>
    /// Reduceable fate cost for disguised character play that accounts for replaced character cost
    /// </summary>
    public class DisguisedReduceableFateCost : ReduceableFateCost
    {
        private readonly bool intoConflictOnly;

        public DisguisedReduceableFateCost(bool intoConflictOnly = false)
        {
            this.intoConflictOnly = intoConflictOnly;
        }

        public override bool CanPay(AbilityContext context)
        {
            if (context.source is not BaseCard sourceCard)
                return false;

            var maxCharacterCost = 0;
            foreach (var card in context.player.cardsInPlay)
            {
                if (sourceCard.CanDisguise(card, context, intoConflictOnly))
                {
                    maxCharacterCost = Math.Max(maxCharacterCost, card.GetCost());
                }
            }

            var minCost = Math.Max(context.player.GetMinimumCost(context.playType, context) - maxCharacterCost, 0);
            return context.player.fate >= minCost &&
                   (minCost == 0 || context.player.CheckRestrictions("spendFate", context));
        }

        public override int GetReducedCost(AbilityContext context)
        {
            var baseCost = base.GetReducedCost(context);
            var disguisedCharacter = context.GetCost("chooseDisguisedCharacter") as BaseCard;
            
            if (disguisedCharacter != null)
            {
                return Math.Max(baseCost - disguisedCharacter.GetCost(), 0);
            }

            return baseCost;
        }
    }

    /// <summary>
    /// Action for playing a character with the Disguised keyword
    /// </summary>
    public class PlayDisguisedCharacterAction : BaseAction
    {
        [Header("Disguise Properties")]
        public bool intoConflictOnly = false;

        /// <summary>
        /// Constructor for disguised character play action
        /// </summary>
        /// <param name="card">The card being played</param>
        /// <param name="intoConflictOnly">Whether the card can only be played into conflict</param>
        public PlayDisguisedCharacterAction(BaseCard card, bool intoConflictOnly = false) : base(card)
        {
            this.intoConflictOnly = intoConflictOnly;
            title = "Play this character with Disguise";
            
            // Set up costs for disguised play
            cost = new List<ICost>
            {
                new ChooseDisguisedCharacterCost(intoConflictOnly),
                new DisguisedReduceableFateCost(intoConflictOnly)
            };
        }

        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public PlayDisguisedCharacterAction() : base()
        {
            title = "Play this character with Disguise";
        }

        /// <summary>
        /// Checks if this action meets all requirements for execution
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="ignoredRequirements">Requirements to ignore</param>
        /// <returns>Empty string if requirements are met, error string otherwise</returns>
        public virtual string MeetsRequirements(AbilityContext context = null, List<string> ignoredRequirements = null)
        {
            context = context ?? CreateContext();
            ignoredRequirements = ignoredRequirements ?? new List<string>();

            // Phase requirement - must be conflict phase
            if (!ignoredRequirements.Contains("phase") && 
                context.game.currentPhase != Phases.Conflict)
            {
                return "phase";
            }

            // Location requirement
            if (!ignoredRequirements.Contains("location") && 
                !context.player.IsCardInPlayableLocation(context.source as BaseCard, context.playType))
            {
                return "location";
            }

            // Trigger restriction
            if (!ignoredRequirements.Contains("cannotTrigger") && 
                context.source is BaseCard sourceCard && !sourceCard.CanPlay(context, context.playType))
            {
                return "cannotTrigger";
            }

            // Unique restriction
            if (context.source is BaseCard uniqueCard && uniqueCard.AnotherUniqueInPlay(context.player))
            {
                return "unique";
            }

            return base.MeetsRequirements(context);
        }

        /// <summary>
        /// Executes the disguised character play action
        /// </summary>
        /// <param name="context">Ability context</param>
        public override void ExecuteHandler(AbilityContext context)
        {
            if (context.source is not BaseCard sourceCard)
            {
                Debug.LogError("PlayDisguisedCharacterAction: Source is not a BaseCard");
                return;
            }

            var extraFate = sourceCard.SumEffects(EffectNames.GainExtraFateWhenPlayed);
            var events = new List<GameEvent>
            {
                context.game.GetEvent(EventNames.OnCardPlayed, new Dictionary<string, object>
                {
                    ["player"] = context.player,
                    ["card"] = sourceCard,
                    ["context"] = context,
                    ["originalLocation"] = sourceCard.location,
                    ["playType"] = context.playType
                })
            };

            var replacedCharacter = context.GetCost("chooseDisguisedCharacter") as BaseCard;
            if (replacedCharacter == null)
            {
                Debug.LogError("PlayDisguisedCharacterAction: No character selected for disguise");
                return;
            }

            bool intoConflict = this.intoConflictOnly;

            // If replaced character is in conflict and we have a choice, prompt for location
            if (replacedCharacter.inConflict && !this.intoConflictOnly)
            {
                context.game.PromptWithHandlerMenu(context.player, new MenuPromptProperties
                {
                    activePromptTitle = "Where do you wish to play this character?",
                    source = sourceCard,
                    choices = new List<string> { "Conflict", "Home" },
                    handlers = new List<System.Action>
                    {
                        () => intoConflict = true,
                        () => { /* Stay home - no action needed */ }
                    }
                });
            }

            context.game.QueueSimpleStep(() =>
            {
                // Display play message
                string locationText = intoConflict ? " into the conflict" : "";
                context.game.AddMessage("{0} plays {1}{2} using Disguised, choosing to replace {3}",
                    context.player, sourceCard, locationText, replacedCharacter);

                // Create appropriate game action for putting card into play
                GameAction putIntoPlayAction;
                if (intoConflict)
                {
                    putIntoPlayAction = context.game.actions.PutIntoConflict(sourceCard, extraFate);
                }
                else
                {
                    putIntoPlayAction = context.game.actions.PutIntoPlay(sourceCard, extraFate);
                }

                putIntoPlayAction.AddEventsToArray(events, context);

                // Add event for transferring attachments and fate
                events.Add(context.game.GetEvent(EventNames.Unnamed, new Dictionary<string, object>(), () =>
                {
                    var moveEvents = new List<GameEvent>();

                    // Transfer fate from replaced character
                    if (replacedCharacter.fate > 0)
                    {
                        var placeFateAction = context.game.actions.PlaceFate(sourceCard, replacedCharacter.fate, replacedCharacter);
                        placeFateAction.AddEventsToArray(moveEvents, context);
                    }

                    // Transfer attachments
                    foreach (var attachment in replacedCharacter.attachments.ToList())
                    {
                        var attachAction = context.game.actions.Attach(sourceCard, attachment);
                        attachAction.AddEventsToArray(moveEvents, context);
                    }

                    // Transfer personal honor status token
                    if (replacedCharacter.personalHonor != null)
                    {
                        var moveStatusAction = context.game.actions.MoveStatusToken(replacedCharacter.personalHonor, sourceCard);
                        moveStatusAction.AddEventsToArray(moveEvents, context);
                    }

                    // Discard the replaced character
                    moveEvents.Add(context.game.GetEvent(EventNames.Unnamed, new Dictionary<string, object>(), () =>
                    {
                        var discardAction = context.game.actions.DiscardFromPlay(replacedCharacter, true);
                        context.game.OpenThenEventWindow(discardAction.GetEvent(replacedCharacter, context));
                    }));

                    context.game.OpenThenEventWindow(moveEvents);
                }));

                context.game.OpenThenEventWindow(events);
                return true;
            });
        }

        /// <summary>
        /// Indicates this action represents playing a card
        /// </summary>
        /// <returns>Always returns true</returns>
        public override bool IsCardPlayed()
        {
            return true;
        }

        /// <summary>
        /// Indicates this is a keyword ability
        /// </summary>
        /// <returns>Always returns true</returns>
        public override bool IsKeywordAbility()
        {
            return true;
        }

        /// <summary>
        /// Gets the title for this action
        /// </summary>
        /// <returns>Action title</returns>
        public override string GetTitle()
        {
            return title ?? "Play this character with Disguise";
        }

        /// <summary>
        /// Checks if the action can be executed
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if the action can be executed</returns>
        public override bool CanExecute(AbilityContext context)
        {
            if (!base.CanExecute(context))
                return false;

            // Must be in conflict phase
            if (context.game.currentPhase != Phases.Conflict)
                return false;

            // Must have valid characters to disguise
            if (context.source is BaseCard sourceCard)
            {
                return context.player.cardsInPlay.Any(card =>
                    card.GetCardType() == CardTypes.Character &&
                    sourceCard.CanDisguise(card, context, intoConflictOnly));
            }

            return false;
        }

        /// <summary>
        /// String representation of this action
        /// </summary>
        /// <returns>String describing this action</returns>
        public override string ToString()
        {
            return $"PlayDisguisedCharacterAction[{card?.printedName ?? "Unknown"}]: {GetTitle()}";
        }
    }

    /// <summary>
    /// Extension methods for disguised character functionality
    /// </summary>
    public static class DisguisedCharacterExtensions
    {
        /// <summary>
        /// Check if a character can be disguised by another character
        /// </summary>
        /// <param name="disguisingCard">Card that will disguise</param>
        /// <param name="targetCard">Card to be replaced</param>
        /// <param name="context">Ability context</param>
        /// <param name="intoConflictOnly">Whether only into conflict is allowed</param>
        /// <returns>True if the disguise is valid</returns>
        public static bool CanDisguise(this BaseCard disguisingCard, BaseCard targetCard, 
            AbilityContext context, bool intoConflictOnly = false)
        {
            if (disguisingCard == null || targetCard == null || context == null)
                return false;

            // Target must be a character
            if (targetCard.GetCardType() != CardTypes.Character)
                return false;

            // Target must be in play
            if (!targetCard.IsInPlay())
                return false;

            // Target must be controlled by the player
            if (targetCard.controller != context.player)
                return false;

            // If into conflict only, target must be in conflict or able to enter
            if (intoConflictOnly && !targetCard.inConflict && !targetCard.CanParticipateInConflict())
                return false;

            // Check for disguise restrictions
            if (targetCard.HasRestriction("cannotBeDisguised", context))
                return false;

            // Check faction restrictions for disguise
            if (!CheckDisguiseFactionRestrictions(disguisingCard, targetCard))
                return false;

            return true;
        }

        /// <summary>
        /// Check faction restrictions for disguising
        /// </summary>
        /// <param name="disguisingCard">Card that will disguise</param>
        /// <param name="targetCard">Card to be replaced</param>
        /// <returns>True if faction restrictions allow the disguise</returns>
        private static bool CheckDisguiseFactionRestrictions(BaseCard disguisingCard, BaseCard targetCard)
        {
            // Basic implementation - can be extended with specific faction rules
            return true;
        }

        /// <summary>
        /// Get all characters that can be disguised by this card
        /// </summary>
        /// <param name="card">Card to check disguise options for</param>
        /// <param name="context">Ability context</param>
        /// <param name="intoConflictOnly">Whether only into conflict is allowed</param>
        /// <returns>List of valid disguise targets</returns>
        public static List<BaseCard> GetValidDisguiseTargets(this BaseCard card, AbilityContext context, bool intoConflictOnly = false)
        {
            if (context?.player?.cardsInPlay == null)
                return new List<BaseCard>();

            return context.player.cardsInPlay
                .Where(target => card.CanDisguise(target, context, intoConflictOnly))
                .ToList();
        }

        /// <summary>
        /// Check if a card has the Disguised keyword
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if the card has Disguised</returns>
        public static bool HasDisguised(this BaseCard card)
        {
            return card.HasKeyword(Keywords.Disguised) || card.HasTrait("Disguised");
        }
    }
}