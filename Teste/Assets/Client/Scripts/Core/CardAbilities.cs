using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for creating a CardAbility
    /// </summary>
    [System.Serializable]
    public class CardAbilityProperties : BaseAbilityProperties
    {
        public string title;
        public object limit;
        public List<string> location;
        public bool printedAbility = true;
        public bool cannotBeCancelled = false;
        public bool cannotTargetFirst = false;
        public bool cannotBeMirrored = false;
        public int? max;
        public string abilityIdentifier;
        public object origin;
        public object initiateDuel;
        public string message;
        public object messageArgs;
        public string effect;
        public object effectArgs;
        public string when; // For triggered abilities
        public string effectName; // For persistent effects
    }

    /// <summary>
    /// Duel configuration for abilities that initiate duels
    /// </summary>
    [System.Serializable]
    public class DuelConfiguration
    {
        public bool opponentChoosesDuelTarget = false;
        public string duelType = "military"; // military, political, honor
        public object additionalProperties;
        
        // For function-based duel configuration
        public Func<AbilityContext, DuelConfiguration> configurationFunction;
    }

    /// <summary>
    /// Represents a card-based ability that can be triggered or activated.
    /// Handles complex interactions like dueling, cost management, and location validation.
    /// </summary>
    public class CardAbility : ThenAbility
    {
        [Header("Card Ability Properties")]
        public string title;
        public AbilityLimit limit;
        public List<string> location = new List<string>();
        public bool printedAbility = true;
        public bool cannotBeCancelled = false;
        public bool cannotTargetFirst = false;
        public bool cannotBeMirrored = false;
        public int? max;
        public string abilityIdentifier;
        public string maxIdentifier;
        public object origin;

        // Duel-related properties
        public DuelConfiguration duelConfig;
        public bool isDuelAbility = false;

        // Cost management
        protected List<object> abilityCost = new List<object>();

        // Message properties
        public string customMessage;
        public object customMessageArgs;
        public string customEffect;
        public object customEffectArgs;

        public CardAbility(Game game, BaseCard card, CardAbilityProperties properties) : base(game, card, properties)
        {
            InitializeCardAbility(properties);
            ProcessDuelConfiguration(properties);
            SetupLimitAndMax(properties);
            AddEventCosts(card);
        }

        /// <summary>
        /// Initialize basic card ability properties
        /// </summary>
        private void InitializeCardAbility(CardAbilityProperties properties)
        {
            title = properties.title;
            limit = properties.limit as AbilityLimit ?? AbilityLimit.PerRound(1);
            location = BuildLocation(card, properties.location);
            printedAbility = properties.printedAbility;
            cannotBeCancelled = properties.cannotBeCancelled;
            cannotTargetFirst = properties.cannotTargetFirst;
            cannotBeMirrored = properties.cannotBeMirrored;
            max = properties.max;
            abilityIdentifier = properties.abilityIdentifier;
            origin = properties.origin;

            // Message properties
            customMessage = properties.message;
            customMessageArgs = properties.messageArgs;
            customEffect = properties.effect;
            customEffectArgs = properties.effectArgs;

            abilityCost = cost.Cast<object>().ToList();

            // Setup ability identifier
            if (string.IsNullOrEmpty(abilityIdentifier))
            {
                abilityIdentifier = printedAbility ? card.id + "1" : "";
            }
            maxIdentifier = card.name + abilityIdentifier;
        }

        /// <summary>
        /// Process duel configuration if this ability initiates duels
        /// </summary>
        private void ProcessDuelConfiguration(CardAbilityProperties properties)
        {
            if (properties.initiateDuel == null)
            {
                return;
            }

            isDuelAbility = true;
            
            // Convert initiateDuel property to DuelConfiguration
            if (properties.initiateDuel is DuelConfiguration config)
            {
                duelConfig = config;
            }
            else if (properties.initiateDuel is Func<AbilityContext, DuelConfiguration> configFunc)
            {
                duelConfig = new DuelConfiguration { configurationFunction = configFunc };
            }
            else
            {
                // Assume it's a simple boolean or object with opponentChoosesDuelTarget
                duelConfig = new DuelConfiguration
                {
                    opponentChoosesDuelTarget = GetBoolProperty(properties.initiateDuel, "opponentChoosesDuelTarget")
                };
            }

            SetupDuelTargeting();
        }

        /// <summary>
        /// Setup targeting for duel abilities
        /// </summary>
        private void SetupDuelTargeting()
        {
            if (card.GetCardType() == CardTypes.Character)
            {
                // Character can initiate duel directly
                SetupCharacterDuelTargeting();
            }
            else
            {
                // Non-character needs to target both challenger and target
                SetupNonCharacterDuelTargeting();
            }
        }

        /// <summary>
        /// Setup duel targeting for character cards
        /// </summary>
        private void SetupCharacterDuelTargeting()
        {
            // Add condition that source must be participating
            var prevCondition = condition;
            condition = (context) => 
            {
                var sourceCard = context.source as BaseCard;
                return sourceCard != null && sourceCard.IsParticipating() && 
                       (prevCondition == null || prevCondition(context));
            };

            // Setup target for duel
            target = (context) => 
            {
                var targetProps = new TargetProperties
                {
                    cardType = CardTypes.Character,
                    player = (ctx) => GetDuelTargetPlayer(ctx),
                    controller = Players.Opponent,
                    cardCondition = (card) => card.IsParticipating(),
                    gameAction = (ctx) => CreateDuelAction(ctx, ctx.source as BaseCard)
                };
                return targetProps;
            };
        }

        /// <summary>
        /// Setup duel targeting for non-character cards
        /// </summary>
        private void SetupNonCharacterDuelTargeting()
        {
            targets = new Dictionary<string, object>
            {
                {
                    "challenger",
                    new TargetProperties
                    {
                        cardType = CardTypes.Character,
                        controller = Players.Self,
                        cardCondition = (card) => card.IsParticipating()
                    }
                },
                {
                    "duelTarget",
                    new TargetProperties
                    {
                        dependsOn = "challenger",
                        cardType = CardTypes.Character,
                        player = (context) => GetDuelTargetPlayer(context),
                        controller = Players.Opponent,
                        cardCondition = (card) => card.IsParticipating(),
                        gameAction = (context) => 
                        {
                            var challenger = context.GetTarget("challenger") as BaseCard;
                            return CreateDuelAction(context, challenger);
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Get the player who should choose the duel target
        /// </summary>
        private string GetDuelTargetPlayer(AbilityContext context)
        {
            var config = GetDuelConfiguration(context);
            return config.opponentChoosesDuelTarget ? Players.Opponent : Players.Self;
        }

        /// <summary>
        /// Create duel action for the ability
        /// </summary>
        private object CreateDuelAction(AbilityContext context, BaseCard challenger)
        {
            var config = GetDuelConfiguration(context);
            var duelProperties = new Dictionary<string, object>
            {
                { "challenger", challenger }
            };

            // Add additional properties from configuration
            if (config.additionalProperties != null)
            {
                // Merge additional properties
                if (config.additionalProperties is Dictionary<string, object> additionalDict)
                {
                    foreach (var kvp in additionalDict)
                    {
                        duelProperties[kvp.Key] = kvp.Value;
                    }
                }
            }

            return game.actions.Duel(duelProperties);
        }

        /// <summary>
        /// Get duel configuration for current context
        /// </summary>
        private DuelConfiguration GetDuelConfiguration(AbilityContext context)
        {
            if (duelConfig.configurationFunction != null)
            {
                return duelConfig.configurationFunction(context);
            }
            return duelConfig;
        }

        /// <summary>
        /// Setup limit and max tracking
        /// </summary>
        private void SetupLimitAndMax(CardAbilityProperties properties)
        {
            if (limit != null)
            {
                limit.RegisterEvents(game);
                limit.ability = this;
            }

            if (max.HasValue)
            {
                card.controller.RegisterAbilityMax(maxIdentifier, max.Value);
            }
        }

        /// <summary>
        /// Add event-specific costs (like fate cost for events)
        /// </summary>
        private void AddEventCosts(BaseCard card)
        {
            if (card.GetCardType() == CardTypes.Event)
            {
                cost.Add(game.costs.PayReduceableFateCost());
            }
        }

        /// <summary>
        /// Build valid locations for this ability
        /// </summary>
        private List<string> BuildLocation(BaseCard card, List<string> specifiedLocation)
        {
            var defaultLocationForType = new Dictionary<string, string>
            {
                { CardTypes.Event, Locations.Hand },
                { CardTypes.Holding, Locations.Provinces },
                { CardTypes.Province, Locations.Provinces },
                { CardTypes.Role, Locations.Role },
                { CardTypes.Stronghold, Locations.StrongholdProvince }
            };

            List<string> defaultedLocation;
            
            if (specifiedLocation != null && specifiedLocation.Count > 0)
            {
                defaultedLocation = specifiedLocation.ToList();
            }
            else
            {
                string defaultLoc = defaultLocationForType.ContainsKey(card.GetCardType()) 
                    ? defaultLocationForType[card.GetCardType()] 
                    : Locations.PlayArea;
                defaultedLocation = new List<string> { defaultLoc };
            }

            // Expand Provinces to specific province locations
            if (defaultedLocation.Contains(Locations.Provinces))
            {
                defaultedLocation.Remove(Locations.Provinces);
                defaultedLocation.AddRange(new string[]
                {
                    Locations.ProvinceOne,
                    Locations.ProvinceTwo,
                    Locations.ProvinceThree,
                    Locations.ProvinceFour,
                    Locations.StrongholdProvince
                });
            }

            return defaultedLocation;
        }

        /// <summary>
        /// Check if this ability meets all requirements to be used
        /// </summary>
        public virtual string MeetsRequirements(AbilityContext context, List<string> ignoredRequirements = null)
        {
            ignoredRequirements = ignoredRequirements ?? new List<string>();

            // Check if card is blank and this is a printed ability
            if (card.IsBlank() && printedAbility)
            {
                return "blank";
            }

            // Check if triggered abilities can be used
            if (IsTriggeredAbility() && !card.CanTriggerAbilities(context))
            {
                return "cannotTrigger";
            }

            // Check if events can be played
            if (card.GetCardType() == CardTypes.Event && !card.CanPlay(context, context.playType))
            {
                return "cannotTrigger";
            }

            // Check keyword abilities
            if (IsKeywordAbility() && !card.CanInitiateKeywords(context))
            {
                return "cannotInitiate";
            }

            // Check ability limit
            if (!ignoredRequirements.Contains("limit") && limit != null && limit.IsAtMax(context.player))
            {
                return "limit";
            }

            // Check max uses
            if (!ignoredRequirements.Contains("max") && max.HasValue && 
                context.player.IsAbilityAtMax(maxIdentifier))
            {
                return "max";
            }

            // Check limited card restriction
            if (IsCardPlayed() && card.IsLimited() && 
                context.player.limitedPlayed >= context.player.maxLimited)
            {
                return "limited";
            }

            // Check base requirements
            if (condition != null && !condition(context))
            {
                return "condition";
            }

            return null; // No issues found
        }

        /// <summary>
        /// Get all costs for this ability
        /// </summary>
        public virtual List<object> GetCosts(AbilityContext context, bool playCosts = true, bool triggerCosts = true)
        {
            var costs = abilityCost.ToList();

            if (!context.subResolution)
            {
                // Add additional trigger costs
                if (triggerCosts && context.player.AnyEffect(EffectNames.AdditionalTriggerCost))
                {
                    var additionalTriggerCosts = context.player.GetEffects(EffectNames.AdditionalTriggerCost)
                        .Where(effect => effect is Func<AbilityContext, IEnumerable<object>>)
                        .SelectMany(effect => ((Func<AbilityContext, IEnumerable<object>>)effect)(context))
                        .ToList();
                    costs.AddRange(additionalTriggerCosts);
                }

                // Add additional play costs
                if (playCosts && context.player.AnyEffect(EffectNames.AdditionalPlayCost))
                {
                    var additionalPlayCosts = context.player.GetEffects(EffectNames.AdditionalPlayCost)
                        .Where(effect => effect is Func<AbilityContext, IEnumerable<object>>)
                        .SelectMany(effect => ((Func<AbilityContext, IEnumerable<object>>)effect)(context))
                        .ToList();
                    costs.AddRange(additionalPlayCosts);
                }
            }

            return costs;
        }

        /// <summary>
        /// Get reduced cost for this ability (primarily fate cost)
        /// </summary>
        public virtual int GetReducedCost(AbilityContext context)
        {
            var fateCost = cost.FirstOrDefault(c => c is IReduceableCost);
            if (fateCost is IReduceableCost reduceable)
            {
                return reduceable.GetReducedCost(context);
            }
            return 0;
        }

        /// <summary>
        /// Check if this ability is in a valid location to be used
        /// </summary>
        public virtual bool IsInValidLocation(AbilityContext context)
        {
            if (card.GetCardType() == CardTypes.Event)
            {
                return context.player.IsCardInPlayableLocation(context.source as BaseCard, context.playType);
            }
            return location.Contains(card.location);
        }

        /// <summary>
        /// Display message when ability is used
        /// </summary>
        public virtual void DisplayMessage(AbilityContext context)
        {
            string messageVerb = card.GetCardType() == CardTypes.Event ? "plays" : "uses";
            
            // Use custom message if provided
            if (!string.IsNullOrEmpty(customMessage))
            {
                DisplayCustomMessage(context);
                return;
            }

            // Build standard message
            var messageArgs = new List<object>();
            
            // Player uses/plays card
            messageArgs.Add(context.player.name);
            messageArgs.Add($" {messageVerb} ");
            messageArgs.Add(context.source);

            // Handle gained abilities
            var abilityOrigin = origin ?? (context.ability as BaseAbility)?.source;
            if (abilityOrigin != null && abilityOrigin != context.source)
            {
                messageArgs.Add("'s gained ability from ");
                messageArgs.Add(abilityOrigin);
            }
            else
            {
                messageArgs.Add("");
                messageArgs.Add("");
            }

            // Add cost messages
            var costMessages = GetCostMessages(context);
            if (costMessages.Count > 0)
            {
                messageArgs.Add(", ");
                messageArgs.Add(string.Join(", ", costMessages));
            }
            else
            {
                messageArgs.Add("");
                messageArgs.Add("");
            }

            // Add effect message
            var effectMessage = GetEffectMessage(context);
            if (!string.IsNullOrEmpty(effectMessage))
            {
                messageArgs.Add(" to ");
                messageArgs.Add(effectMessage);
            }
            else
            {
                messageArgs.Add("");
                messageArgs.Add("");
            }

            // Format final message
            string finalMessage = string.Join("", messageArgs.Select(arg => arg.ToString()));
            game.AddMessage(finalMessage);
        }

        /// <summary>
        /// Display custom message for ability
        /// </summary>
        private void DisplayCustomMessage(AbilityContext context)
        {
            var messageArgs = GetCustomMessageArgs(context);
            game.AddMessage(customMessage, messageArgs);
        }

        /// <summary>
        /// Get custom message arguments
        /// </summary>
        private object[] GetCustomMessageArgs(AbilityContext context)
        {
            if (customMessageArgs == null)
            {
                return new object[0];
            }

            if (customMessageArgs is Func<AbilityContext, object[]> messageFunc)
            {
                return messageFunc(context);
            }

            if (customMessageArgs is object[] args)
            {
                return args;
            }

            return new object[] { customMessageArgs };
        }

        /// <summary>
        /// Get cost messages for display
        /// </summary>
        private List<string> GetCostMessages(AbilityContext context)
        {
            var costMessages = new List<string>();

            foreach (var costObj in cost)
            {
                if (costObj is ICostWithMessage costWithMessage)
                {
                    var costCard = context.GetCost(costWithMessage.GetActionName(context));
                    string cardDescription = costCard?.ToString() ?? "";
                    
                    if (costCard is BaseCard card && card.facedown)
                    {
                        cardDescription = "a facedown card";
                    }

                    var (format, args) = costWithMessage.GetCostMessage(context);
                    var formattedArgs = new List<object> { cardDescription };
                    formattedArgs.AddRange(args);
                    
                    string message = string.Format(format, formattedArgs.ToArray());
                    costMessages.Add(message);
                }
            }

            return costMessages;
        }

        /// <summary>
        /// Get effect message for display
        /// </summary>
        private string GetEffectMessage(AbilityContext context)
        {
            if (!string.IsNullOrEmpty(customEffect))
            {
                var effectArgs = GetCustomEffectArgs(context);
                return string.Format(customEffect, effectArgs);
            }

            // Get message from game actions
            var gameActions = GetGameActions(context);
            if (gameActions.Count > 0)
            {
                // This would need to be implemented based on your action system
                return "performs action"; // Placeholder
            }

            return "";
        }

        /// <summary>
        /// Get custom effect arguments
        /// </summary>
        private object[] GetCustomEffectArgs(AbilityContext context)
        {
            var effectArgs = new List<object> { context.target ?? context.ring ?? context.source };

            if (customEffectArgs != null)
            {
                if (customEffectArgs is Func<AbilityContext, object[]> effectFunc)
                {
                    effectArgs.AddRange(effectFunc(context));
                }
                else if (customEffectArgs is object[] args)
                {
                    effectArgs.AddRange(args);
                }
                else
                {
                    effectArgs.Add(customEffectArgs);
                }
            }

            return effectArgs.ToArray();
        }

        /// <summary>
        /// Check if this ability represents a card being played
        /// </summary>
        public virtual bool IsCardPlayed()
        {
            return card.GetCardType() == CardTypes.Event;
        }

        /// <summary>
        /// Check if this is a triggered ability
        /// </summary>
        public override bool IsTriggeredAbility()
        {
            return true;
        }

        /// <summary>
        /// Check if this is a keyword ability
        /// </summary>
        public virtual bool IsKeywordAbility()
        {
            // Override in derived classes for specific keyword abilities
            return false;
        }

        /// <summary>
        /// Check if this ability is a card ability
        /// </summary>
        public override bool IsCardAbility()
        {
            return true;
        }

        /// <summary>
        /// Get boolean property from object (helper for duel configuration)
        /// </summary>
        private bool GetBoolProperty(object obj, string propertyName)
        {
            if (obj is Dictionary<string, object> dict)
            {
                return dict.ContainsKey(propertyName) && (bool)dict[propertyName];
            }
            return false;
        }
    }

    #region Supporting Interfaces and Classes

    /// <summary>
    /// Interface for costs that can be reduced
    /// </summary>
    public interface IReduceableCost
    {
        int GetReducedCost(AbilityContext context);
    }

    /// <summary>
    /// Interface for costs that have display messages
    /// </summary>
    public interface ICostWithMessage
    {
        string GetActionName(AbilityContext context);
        (string format, object[] args) GetCostMessage(AbilityContext context);
    }

    /// <summary>
    /// Base class for then abilities (abilities that can chain with "then" effects)
    /// </summary>
    public abstract class ThenAbility : BaseAbility
    {
        protected ThenAbility(Game game, BaseCard card, CardAbilityProperties properties) 
            : base(game, card, properties)
        {
        }

        // ThenAbility specific functionality would go here
        // This is a placeholder for the full ThenAbility implementation
    }



    /// <summary>
    /// Target properties for ability targeting
    /// </summary>
    [System.Serializable]
    public class TargetProperties
    {
        public string cardType;
        public Func<AbilityContext, string> player;
        public string controller;
        public Func<BaseCard, bool> cardCondition;
        public Func<AbilityContext, object> gameAction;
        public string dependsOn;
    }

    /// <summary>
    /// Extension methods for CardAbility
    /// </summary>
    public static class CardAbilityExtensions
    {
        /// <summary>
        /// Create a simple card ability
        /// </summary>
        public static CardAbility CreateSimpleAbility(this BaseCard card, string title, 
            Func<AbilityContext, bool> condition, Action<AbilityContext> effect)
        {
            var properties = new CardAbilityProperties
            {
                title = title,
                condition = condition,
                handler = effect
            };

            return new CardAbility(card.game, card, properties);
        }

        /// <summary>
        /// Create a duel ability
        /// </summary>
        public static CardAbility CreateDuelAbility(this BaseCard card, string title, 
            DuelConfiguration duelConfig, Func<AbilityContext, bool> condition = null)
        {
            var properties = new CardAbilityProperties
            {
                title = title,
                condition = condition,
                initiateDuel = duelConfig
            };

            return new CardAbility(card.game, card, properties);
        }
    }

    #endregion
}