using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Maps event names to title generation functions
    /// </summary>
    public static class TriggeredAbilityWindowTitles
    {
        private static readonly Dictionary<string, Func<object, string>> EventToTitleFunc = 
            new Dictionary<string, Func<object, string>>
            {
                { "onInitiateAbilityEffects", (eventObj) => $"the effects of {GetEventCard(eventObj)?.name}" },
                { "onCardBowed", (eventObj) => $"{GetEventCard(eventObj)?.name} being bowed" },
                { "onClaimRing", (eventObj) => $"to the {GetEventRing(eventObj)?.element} ring being claimed" },
                { "onCardLeavesPlay", (eventObj) => $"{GetEventCard(eventObj)?.name} leaving play" },
                { "onCharacterEntersPlay", (eventObj) => $"{GetEventCard(eventObj)?.name} entering play" },
                { "onCardPlayed", (eventObj) => $"{GetEventCard(eventObj)?.name} being played" },
                { "onCardHonored", (eventObj) => $"{GetEventCard(eventObj)?.name} being honored" },
                { "onCardDishonored", (eventObj) => $"{GetEventCard(eventObj)?.name} being dishonored" },
                { "onMoveCharactersToConflict", (eventObj) => "characters moving to the conflict" },
                { "onPhaseEnded", (eventObj) => $"{GetEventPhase(eventObj)} phase ending" },
                { "onPhaseStarted", (eventObj) => $"{GetEventPhase(eventObj)} phase starting" },
                { "onReturnRing", (eventObj) => $"returning the {GetEventRing(eventObj)?.element} ring" },
                { "onSacrificed", (eventObj) => $"{GetEventCard(eventObj)?.name} being sacrificed" },
                { "onRemovedFromChallenge", (eventObj) => $"{GetEventCard(eventObj)?.name} being removed from the challenge" }
            };

        private static readonly Dictionary<string, string> AbilityTypeToWord = 
            new Dictionary<string, string>
            {
                { AbilityTypes.CancelInterrupt, "interrupt" },
                { AbilityTypes.Interrupt, "interrupt" },
                { AbilityTypes.Reaction, "reaction" },
                { AbilityTypes.ForcedReaction, "forced reaction" },
                { AbilityTypes.ForcedInterrupt, "forced interrupt" }
            };

        /// <summary>
        /// Generate title for ability window based on ability type and events
        /// </summary>
        public static string GetTitle(string abilityType, List<object> events)
        {
            if (events == null || events.Count == 0)
            {
                events = new List<object>();
            }

            string abilityWord = AbilityTypeToWord.ContainsKey(abilityType) 
                ? AbilityTypeToWord[abilityType] 
                : abilityType;

            var titles = events
                .Select(eventObj => GetEventTitle(eventObj))
                .Where(title => !string.IsNullOrEmpty(title))
                .ToList();

            if (abilityType == AbilityTypes.ForcedReaction || abilityType == AbilityTypes.ForcedInterrupt)
            {
                if (titles.Count > 0)
                {
                    return $"Choose {abilityWord} order for {FormatTitles(titles)}";
                }
                return $"Choose {abilityWord} order";
            }

            if (titles.Count > 0)
            {
                return $"Any {abilityWord}s to {FormatTitles(titles)}?";
            }
            return $"Any {abilityWord}s?";
        }

        /// <summary>
        /// Get action description for a specific event
        /// </summary>
        public static string GetAction(object eventObj)
        {
            string title = GetEventTitle(eventObj);
            return !string.IsNullOrEmpty(title) ? title : GetEventName(eventObj);
        }

        /// <summary>
        /// Format multiple titles into a readable string
        /// </summary>
        private static string FormatTitles(List<string> titles)
        {
            if (titles.Count == 0) return "";
            if (titles.Count == 1) return titles[0];
            if (titles.Count == 2) return $"{titles[1]} or {titles[0]}";
            
            string result = titles[0];
            for (int i = 1; i < titles.Count; i++)
            {
                result = $"{titles[i]}, {result}";
            }
            return result;
        }

        /// <summary>
        /// Get title for a specific event
        /// </summary>
        private static string GetEventTitle(object eventObj)
        {
            string eventName = GetEventName(eventObj);
            if (EventToTitleFunc.ContainsKey(eventName))
            {
                return EventToTitleFunc[eventName](eventObj);
            }
            return null;
        }

        /// <summary>
        /// Extract event name from event object
        /// </summary>
        private static string GetEventName(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Name;
            }
            return eventObj?.ToString() ?? "Unknown Event";
        }

        /// <summary>
        /// Extract card from event object
        /// </summary>
        private static BaseCard GetEventCard(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Card;
            }
            return null;
        }

        /// <summary>
        /// Extract ring from event object
        /// </summary>
        private static Ring GetEventRing(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Ring;
            }
            return null;
        }

        /// <summary>
        /// Extract phase from event object
        /// </summary>
        private static string GetEventPhase(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Phase ?? "Unknown";
            }
            return "Unknown";
        }
    }

    /// <summary>
    /// Base class for triggered ability windows that handles forced abilities
    /// </summary>
    public class ForcedTriggeredAbilityWindow : BaseStep, IAbilityWindow
    {
        protected List<AbilityContext> choices = new List<AbilityContext>();
        protected List<object> events = new List<object>();
        protected object eventWindow;
        protected List<object> eventsToExclude = new List<object>();
        protected string abilityType;
        protected Player currentPlayer;
        protected List<ResolvedAbility> resolvedAbilities = new List<ResolvedAbility>();

        // IAbilityWindow implementation
        public string AbilityType => abilityType;
        public List<object> Events => events;
        public event Action<IAbilityWindow> OnWindowClosed;

        public virtual void Open()
        {
            // Start processing the window
        }

        public virtual void Close()
        {
            OnWindowClosed?.Invoke(this);
        }

        public ForcedTriggeredAbilityWindow(Game game, string abilityType, object window, List<object> eventsToExclude = null) 
            : base(game)
        {
            this.abilityType = abilityType;
            this.eventWindow = window;
            this.eventsToExclude = eventsToExclude ?? new List<object>();
            this.currentPlayer = game.GetFirstPlayer();
        }

        public override bool Continue()
        {
            game.currentAbilityWindow = null; // Cannot convert ForcedTriggeredAbilityWindow to AbilityWindow
            
            if (eventWindow != null)
            {
                EmitEvents();
            }

            if (FilterChoices())
            {
                game.currentAbilityWindow = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a choice to the ability window
        /// </summary>
        public virtual void AddChoice(AbilityContext context)
        {
            if (context.eventObj != null && !IsEventCancelled(context.eventObj) && 
                !resolvedAbilities.Any(resolved => 
                    resolved.ability == context.ability && 
                    (context.ability.collectiveTrigger || resolved.eventObj == context.eventObj)))
            {
                choices.Add(context);
            }
        }

        /// <summary>
        /// Filter and process available choices
        /// </summary>
        protected virtual bool FilterChoices()
        {
            if (choices.Count == 0)
            {
                return true;
            }

            if (choices.Count == 1 || (currentPlayer.optionSettings.ContainsKey("orderForcedAbilities") && !(bool)currentPlayer.optionSettings["orderForcedAbilities"]))
            {
                ResolveAbility(choices[0]);
                return false;
            }

            var uniqueSources = choices.GroupBy(c => c.source).ToList();
            if (uniqueSources.Count == 1)
            {
                // All choices share a source
                PromptBetweenAbilities(choices, false);
            }
            else
            {
                // Choose a card to trigger
                PromptBetweenSources(choices);
            }
            return false;
        }

        /// <summary>
        /// Prompt player to choose between different source cards
        /// </summary>
        protected void PromptBetweenSources(List<AbilityContext> choices)
        {
            var promptProperties = GetPromptForSelectProperties();
            promptProperties["cardCondition"] = new Func<BaseCard, bool>(card => 
                choices.Any(context => context.source == card));
            promptProperties["onSelect"] = new Func<Player, BaseCard, bool>((player, card) =>
            {
                var cardChoices = choices.Where(context => context.source == card).ToList();
                PromptBetweenAbilities(cardChoices);
                return true;
            });

            var selectProps = new SelectCardPromptProperties
            {
                activePromptTitle = promptProperties.ContainsKey("activePromptTitle") ? (string)promptProperties["activePromptTitle"] : "",
                waitingPromptTitle = promptProperties.ContainsKey("waitingPromptTitle") ? (string)promptProperties["waitingPromptTitle"] : "",
                cardCondition = promptProperties.ContainsKey("cardCondition") ? (Func<BaseCard, bool>)promptProperties["cardCondition"] : null,
                onSelect = promptProperties.ContainsKey("onSelect") ? (Func<Player, BaseCard, bool>)promptProperties["onSelect"] : null
            };
            game.PromptForSelect(currentPlayer, selectProps);
        }

        /// <summary>
        /// Get properties for card selection prompts
        /// </summary>
        protected virtual Dictionary<string, object> GetPromptForSelectProperties()
        {
            var properties = GetPromptProperties();
            properties["location"] = Locations.Any;
            return properties;
        }

        /// <summary>
        /// Get base prompt properties
        /// </summary>
        protected Dictionary<string, object> GetPromptProperties()
        {
            return new Dictionary<string, object>
            {
                { "source", "Triggered Abilities" },
                { "controls", GetPromptControls() },
                { "activePromptTitle", TriggeredAbilityWindowTitles.GetTitle(abilityType, events) },
                { "waitingPromptTitle", "Waiting for opponent" }
            };
        }

        /// <summary>
        /// Get prompt controls for UI display
        /// </summary>
        protected List<object> GetPromptControls()
        {
            var map = new Dictionary<object, List<object>>();

            foreach (var eventObj in events)
            {
                var context = GetEventContext(eventObj);
                if (context?.source != null)
                {
                    var targets = map.ContainsKey(context.source) ? map[context.source] : new List<object>();
                    
                    if (context.target != null)
                    {
                        targets.Add(context.target);
                    }
                    else if (GetEventCard(eventObj) != null && GetEventCard(eventObj) != context.source)
                    {
                        targets.Add(GetEventCard(eventObj));
                    }
                    else if (context.eventObj != null && GetEventCard(context.eventObj) != null)
                    {
                        targets.Add(GetEventCard(context.eventObj));
                    }
                    else if (GetEventCard(eventObj) != null)
                    {
                        targets.Add(GetEventCard(eventObj));
                    }

                    map[context.source] = targets.Distinct().ToList();
                }
            }

            return map.Select(kvp => new
            {
                type = "targeting",
                source = GetSourceSummary(kvp.Key),
                targets = kvp.Value.Select(target => GetTargetSummary(target)).ToList()
            }).Cast<object>().ToList();
        }

        /// <summary>
        /// Prompt player to choose between different abilities on the same card
        /// </summary>
        protected void PromptBetweenAbilities(List<AbilityContext> choices, bool addBackButton = true)
        {
            var menuChoices = choices.Select(context => context.ability.title).Distinct().ToList();
            
            if (menuChoices.Count == 1)
            {
                // This card has only one ability which can be triggered
                PromptBetweenEventCards(choices, addBackButton);
                return;
            }

            // Multiple abilities available - prompt player to pick one
            var handlers = menuChoices.Select(title => 
                new Action(() => PromptBetweenEventCards(
                    choices.Where(context => context.ability.title == title).ToList()))).ToList();

            if (addBackButton)
            {
                menuChoices.Add("Back");
                handlers.Add(() => PromptBetweenSources(this.choices));
            }

            var promptProperties = GetPromptProperties();
            promptProperties["activePromptTitle"] = "Which ability would you like to use?";
            promptProperties["choices"] = menuChoices;
            promptProperties["handlers"] = handlers;

            var handlerProps = new HandlerMenuPromptProperties
            {
                activePromptTitle = "Which ability would you like to use?",
                waitingPromptTitle = promptProperties.ContainsKey("waitingPromptTitle") ? (string)promptProperties["waitingPromptTitle"] : "",
                handlers = handlers
            };
            game.PromptWithHandlerMenu(currentPlayer, handlerProps);
        }

        /// <summary>
        /// Prompt player to choose between different event cards
        /// </summary>
        protected void PromptBetweenEventCards(List<AbilityContext> choices, bool addBackButton = true)
        {
            if (choices[0].ability.collectiveTrigger)
            {
                // This ability only triggers once for all events in this window
                ResolveAbility(choices[0]);
                return;
            }

            var uniqueCards = choices.GroupBy(context => GetEventCard(context.eventObj)).ToList();
            if (uniqueCards.Count == 1)
            {
                // The events which this ability can respond to only affect a single card
                PromptBetweenEvents(choices, addBackButton);
                return;
            }

            // Several cards could be affected by this ability
            var promptProperties = GetPromptForSelectProperties();
            promptProperties["activePromptTitle"] = "Select a card to affect";
            promptProperties["cardCondition"] = new Func<BaseCard, bool>(card =>
                choices.Any(context => GetEventCard(context.eventObj) == card));
            promptProperties["buttons"] = addBackButton ? 
                new List<object> { new { text = "Back", arg = "back" } } : 
                new List<object>();
            promptProperties["onSelect"] = new Func<Player, BaseCard, bool>((player, card) =>
            {
                var cardChoices = choices.Where(context => GetEventCard(context.eventObj) == card).ToList();
                PromptBetweenEvents(cardChoices);
                return true;
            });
            promptProperties["onMenuCommand"] = new Func<Player, string, bool>((player, arg) =>
            {
                if (arg == "back")
                {
                    PromptBetweenSources(this.choices);
                    return true;
                }
                return false;
            });

            var selectProps2 = new SelectCardPromptProperties
            {
                activePromptTitle = promptProperties.ContainsKey("activePromptTitle") ? (string)promptProperties["activePromptTitle"] : "",
                waitingPromptTitle = promptProperties.ContainsKey("waitingPromptTitle") ? (string)promptProperties["waitingPromptTitle"] : "",
                cardCondition = promptProperties.ContainsKey("cardCondition") ? (Func<BaseCard, bool>)promptProperties["cardCondition"] : null,
                onSelect = promptProperties.ContainsKey("onSelect") ? (Func<Player, BaseCard, bool>)promptProperties["onSelect"] : null
            };
            game.PromptForSelect(currentPlayer, selectProps2);
        }

        /// <summary>
        /// Prompt player to choose between different events
        /// </summary>
        protected void PromptBetweenEvents(List<AbilityContext> choices, bool addBackButton = true)
        {
            var uniqueEvents = choices.GroupBy(context => context.eventObj).ToList();
            if (uniqueEvents.Count == 1)
            {
                // Only one event - resolve immediately
                ResolveAbility(choices[0]);
                return;
            }

            // Multiple events - prompt for choice
            var menuChoices = choices.Select(context => 
                TriggeredAbilityWindowTitles.GetAction(context.eventObj)).ToList();
            var handlers = choices.Select(context => 
                new Action(() => ResolveAbility(context))).ToList();

            if (addBackButton)
            {
                menuChoices.Add("Back");
                handlers.Add(() => PromptBetweenSources(this.choices));
            }

            var promptProperties = GetPromptProperties();
            promptProperties["activePromptTitle"] = "Choose an event to respond to";
            promptProperties["choices"] = menuChoices;
            promptProperties["handlers"] = handlers;

            var handlerProps2 = new HandlerMenuPromptProperties
            {
                activePromptTitle = "Choose an event to respond to",
                waitingPromptTitle = promptProperties.ContainsKey("waitingPromptTitle") ? (string)promptProperties["waitingPromptTitle"] : "",
                handlers = handlers
            };
            game.PromptWithHandlerMenu(currentPlayer, handlerProps2);
        }

        /// <summary>
        /// Resolve a chosen ability
        /// </summary>
        protected virtual void ResolveAbility(AbilityContext context)
        {
            var resolver = game.ResolveAbility(context);
            game.QueueSimpleStep(() =>
            {
                if (resolver.passPriority)
                {
                    PostResolutionUpdate(resolver);
                }
                return true;
            });
        }

        /// <summary>
        /// Update state after ability resolution
        /// </summary>
        public virtual void PostResolutionUpdate(AbilityResolver resolver)
        {
            resolvedAbilities.Add(new ResolvedAbility
            {
                ability = resolver.context.ability,
                eventObj = resolver.context.eventObj
            });
        }

        /// <summary>
        /// Emit events and collect ability choices
        /// </summary>
        protected void EmitEvents()
        {
            choices.Clear();
            events = GetWindowEvents().Except(eventsToExclude).ToList();

            foreach (var eventObj in events)
            {
                string eventName = GetEventName(eventObj);
                game.Emit($"{eventName}:{abilityType}", eventObj, this);
            }

            game.Emit($"aggregateEvent:{abilityType}", events, this);
        }

        // Helper methods
        protected virtual List<object> GetWindowEvents()
        {
            // Extract events from event window
            if (eventWindow is IEventWindow window)
            {
                return window.GetEvents();
            }
            return new List<object>();
        }

        protected bool IsEventCancelled(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.cancelled;
            }
            return false;
        }

        protected AbilityContext GetEventContext(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Context;
            }
            return null;
        }

        protected BaseCard GetEventCard(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Card;
            }
            return null;
        }

        protected string GetEventName(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Name;
            }
            return eventObj?.ToString() ?? "Unknown";
        }

        protected object GetSourceSummary(object source)
        {
            if (source is BaseCard card)
            {
                return card.GetShortSummary();
            }
            return source?.ToString() ?? "Unknown";
        }

        protected object GetTargetSummary(object target)
        {
            if (target is BaseCard card)
            {
                return card.GetShortSummaryForControls(currentPlayer);
            }
            return target?.ToString() ?? "Unknown";
        }
    }

    /// <summary>
    /// Advanced triggered ability window that handles optional abilities and bluff prompts
    /// </summary>
    public class TriggeredAbilityWindow : ForcedTriggeredAbilityWindow, IAbilityWindow
    {
        protected bool complete = false;
        protected bool prevPlayerPassed = false;

        public TriggeredAbilityWindow(Game game, string abilityType, object window, List<object> eventsToExclude = null)
            : base(game, abilityType, window, eventsToExclude)
        {
        }

        /// <summary>
        /// Check if player should get a bluff prompt
        /// </summary>
        protected bool ShowBluffPrompt(Player player)
        {
            // Show a bluff prompt if the player has an event which could trigger (but isn't in their hand)
            if (player.timerSettings.ContainsKey("eventsInDeck") && (bool)player.timerSettings["eventsInDeck"] && choices.Any(context => context.player == player))
            {
                return true;
            }

            // Show a bluff prompt if we're in Step 6, the player has the appropriate setting, and there's an event for the other player
            return abilityType == AbilityTypes.WouldInterrupt && 
                   player.timerSettings.ContainsKey("events") && (bool)player.timerSettings["events"] && 
                   events.Any(eventObj => 
                   {
                       var gameEvent = eventObj as IGameEvent;
                       return gameEvent?.Name == "OnInitiateAbilityEffects" &&
                              gameEvent.Card?.GetCardType() == CardTypes.Event &&
                              gameEvent.Context?.player != player;
                   });
        }

        /// <summary>
        /// Show prompt with bluff options
        /// </summary>
        protected void PromptWithBluffPrompt(Player player)
        {
            var promptProperties = new Dictionary<string, object>
            {
                { "source", "Triggered Abilities" },
                { "waitingPromptTitle", "Waiting for opponent" },
                { "activePrompt", new Dictionary<string, object>
                    {
                        { "promptTitle", TriggeredAbilityWindowTitles.GetTitle(abilityType, events) },
                        { "controls", GetPromptControls() },
                        { "buttons", new List<object>
                            {
                                new { timer = true, method = "pass" },
                                new { text = "I need more time", timerCancel = true },
                                new { text = "Don't ask again until end of round", timerCancel = true, method = "pass", arg = "pauseRound" },
                                new { text = "Pass", method = "pass" }
                            }
                        }
                    }
                }
            };

            game.PromptWithMenu(player, this, promptProperties);
        }

        /// <summary>
        /// Handle player passing
        /// </summary>
        public bool Pass(Player player, string arg = null)
        {
            if (arg == "pauseRound")
            {
                player.noTimer = true;
                player.resetTimerAtEndOfRound = true;
            }

            if (prevPlayerPassed || currentPlayer.opponent == null)
            {
                complete = true;
            }
            else
            {
                currentPlayer = currentPlayer.opponent;
                prevPlayerPassed = true;
            }

            return true;
        }

        /// <summary>
        /// Filter choices with additional logic for optional abilities
        /// </summary>
        protected override bool FilterChoices()
        {
            // If both players have passed, close the window
            if (complete)
            {
                return true;
            }

            // Remove any choices which involve the current player canceling their own abilities
            if (abilityType == AbilityTypes.WouldInterrupt && currentPlayer.optionSettings.ContainsKey("cancelOwnAbilities") && !(bool)currentPlayer.optionSettings["cancelOwnAbilities"])
            {
                choices = choices.Where(context => !(
                    context.player == currentPlayer &&
                    GetEventName(context.eventObj) == "OnInitiateAbilityEffects" &&
                    GetEventContext(context.eventObj)?.player == currentPlayer
                )).ToList();
            }

            // If the current player has no available choices, check for bluff prompt
            if (!choices.Any(context => context.player == currentPlayer && context.ability.IsInValidLocation(context)))
            {
                if (ShowBluffPrompt(currentPlayer))
                {
                    PromptWithBluffPrompt(currentPlayer);
                    return false;
                }
                // Otherwise pass
                Pass(currentPlayer);
                return FilterChoices();
            }

            // Filter choices for current player and prompt
            choices = choices.Where(context => 
                context.player == currentPlayer && context.ability.IsInValidLocation(context)).ToList();
            PromptBetweenSources(choices);
            return false;
        }

        /// <summary>
        /// Update state after ability resolution
        /// </summary>
        public override void PostResolutionUpdate(AbilityResolver resolver)
        {
            base.PostResolutionUpdate(resolver);
            prevPlayerPassed = false;
            currentPlayer = currentPlayer.opponent ?? currentPlayer;
        }

        /// <summary>
        /// Get enhanced prompt properties for triggered abilities
        /// </summary>
        protected override Dictionary<string, object> GetPromptForSelectProperties()
        {
            var properties = base.GetPromptForSelectProperties();
            properties["selectCard"] = currentPlayer.optionSettings.ContainsKey("markCardsUnselectable") ? (bool)currentPlayer.optionSettings["markCardsUnselectable"] : false;
            properties["buttons"] = new List<object> { new { text = "Pass", arg = "pass" } };
            properties["onMenuCommand"] = new Func<Player, string, bool>((player, arg) =>
            {
                Pass(player, arg);
                return true;
            });
            return properties;
        }
    }

    /// <summary>
    /// Represents a resolved ability for tracking purposes
    /// </summary>
    [System.Serializable]
    public class ResolvedAbility
    {
        public object ability;
        public object eventObj;
    }

    /// <summary>
    /// Interface for event windows
    /// </summary>
    public interface IEventWindow
    {
        List<object> GetEvents();
    }


}
