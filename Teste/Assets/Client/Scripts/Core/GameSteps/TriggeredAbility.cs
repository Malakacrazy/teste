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

        /// <summary>
        /// Get event name from event object (instance method for TriggeredAbilityWindow)
        /// </summary>
        private string GetEventName(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Name;
            }
            return eventObj?.ToString() ?? "Unknown Event";
        }

        /// <summary>
        /// Get event context from event object
        /// </summary>
        private AbilityContext GetEventContext(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Context;
            }
            return null;
        }

        /// <summary>
        /// Get prompt controls for the current window
        /// </summary>
        protected virtual List<object> GetPromptControls()
        {
            // Placeholder implementation
            return new List<object>();
        }

        /// <summary>
        /// Get base prompt properties for select operations
        /// </summary>
        protected virtual Dictionary<string, object> GetPromptForSelectProperties()
        {
            return new Dictionary<string, object>
            {
                { "source", "Triggered Abilities" },
                { "waitingPromptTitle", "Waiting for opponent" }
            };
        }
    }

    // ForcedTriggeredAbilityWindow is now defined in separate file: GameSteps\ForcedTriggeredAbilityWindow.cs

    /// <summary>
    /// Advanced triggered ability window that handles optional abilities and bluff prompts
    /// </summary>
    public class TriggeredAbilityWindow : ForcedTriggeredAbilityWindow
    {
        protected bool complete = false;
        protected bool prevPlayerPassed = false;

        public TriggeredAbilityWindow(Game game, string abilityType, List<object> events, List<object> eventsToExclude = null)
            : base(game, abilityType, null, eventsToExclude?.Cast<GameEvent>().ToList())
        {
            // Convert object events to GameEvents and set them
            if (events != null)
            {
                this.events = events.Cast<GameEvent>().ToList();
            }
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
        protected override void PostResolutionUpdate(AbilityResolver resolver)
        {
            base.PostResolutionUpdate(resolver);
            prevPlayerPassed = false;
            currentPlayer = currentPlayer.opponent ?? currentPlayer;
        }

        // IAbilityWindow interface methods
        public void Open()
        {
            // Window opening logic - trigger Continue() which starts the filtering process
            Continue();
        }
        
        public void Close()
        {
            OnWindowClosed?.Invoke(this);
        }
        
        /// <summary>
        /// Get enhanced prompt properties for triggered abilities
        /// </summary>
        protected virtual Dictionary<string, object> GetPromptForSelectProperties()
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
