using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for windows that handle forced triggered abilities
    /// </summary>
    public class ForcedTriggeredAbilityWindow : BaseStep, IAbilityWindow
    {
        protected List<AbilityContext> choices = new List<AbilityContext>();
        protected List<GameEvent> events = new List<GameEvent>();
        protected EventWindow eventWindow;
        protected List<GameEvent> eventsToExclude = new List<GameEvent>();
        protected string abilityType;
        protected Player currentPlayer;
        protected List<ResolvedAbility> resolvedAbilities = new List<ResolvedAbility>();

        // IAbilityWindow implementation
        public string AbilityType => abilityType;
        public List<object> Events => events.Cast<object>().ToList();
        public event Action<IAbilityWindow> OnWindowClosed;

        public ForcedTriggeredAbilityWindow(Game game, string abilityType, EventWindow window = null, List<GameEvent> eventsToExclude = null) 
            : base(game)
        {
            this.abilityType = abilityType;
            this.eventWindow = window;
            this.eventsToExclude = eventsToExclude ?? new List<GameEvent>();
            this.currentPlayer = game.GetFirstPlayer();
        }

        public override bool Continue()
        {
            game.currentAbilityWindow = this;
            
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

        public virtual void AddChoice(AbilityContext context)
        {
            bool eventNotCancelled = context.GetEvent() != null && !context.GetEvent().cancelled;
            bool abilityNotResolved = !resolvedAbilities.Any(resolved => 
                resolved.ability == context.ability && 
                (context.ability.collectiveTrigger || resolved.gameEvent == context.GetEvent()));

            if (eventNotCancelled && abilityNotResolved)
            {
                choices.Add(context);
            }
        }

        protected virtual bool FilterChoices()
        {
            if (choices.Count == 0)
            {
                return true;
            }

            if (choices.Count == 1 || !currentPlayer.optionSettings.orderForcedAbilities)
            {
                ResolveAbility(choices[0]);
                return false;
            }

            var uniqueSources = choices.Select(c => c.source).Distinct().ToList();
            if (uniqueSources.Count == 1)
            {
                PromptBetweenAbilities(choices, false);
            }
            else
            {
                PromptBetweenSources(choices);
            }
            
            return false;
        }

        protected virtual void PromptBetweenSources(List<AbilityContext> choices)
        {
            game.PromptForSelect(currentPlayer, new SelectCardPromptProperties
            {
                activePromptTitle = GetPromptTitle(),
                waitingPromptTitle = "Waiting for opponent",
                source = "Triggered Abilities",
                cardCondition = card => choices.Any(context => context.source == card),
                onSelect = (player, card) =>
                {
                    var filteredChoices = choices.Where(context => context.source == card).ToList();
                    PromptBetweenAbilities(filteredChoices);
                    return true;
                }
            });
        }

        protected virtual void PromptBetweenAbilities(List<AbilityContext> choices, bool addBackButton = true)
        {
            var menuChoices = choices.Select(context => context.ability.title).Distinct().ToList();
            
            if (menuChoices.Count == 1)
            {
                PromptBetweenEventCards(choices, addBackButton);
                return;
            }

            var handlers = new List<Action>();
            foreach (var title in menuChoices)
            {
                var filteredChoices = choices.Where(context => context.ability.title == title).ToList();
                handlers.Add(() => PromptBetweenEventCards(filteredChoices));
            }

            if (addBackButton)
            {
                menuChoices.Add("Back");
                handlers.Add(() => PromptBetweenSources(this.choices));
            }

            game.PromptWithHandlerMenu(currentPlayer, new HandlerMenuPromptProperties
            {
                activePromptTitle = "Which ability would you like to use?",
                waitingPromptTitle = "Waiting for opponent",
                source = "Triggered Abilities", 
                choices = menuChoices.Select(choice => new MenuOption { text = choice }).ToList(),
                handlers = handlers
            });
        }

        protected virtual void PromptBetweenEventCards(List<AbilityContext> choices, bool addBackButton = true)
        {
            if (choices[0].ability.collectiveTrigger)
            {
                ResolveAbility(choices[0]);
                return;
            }

            var uniqueEventCards = choices.Select(context => context.GetEvent()?.card).Where(card => card != null).Distinct().ToList();
            if (uniqueEventCards.Count == 1)
            {
                PromptBetweenEvents(choices, addBackButton);
                return;
            }

            game.PromptForSelect(currentPlayer, new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card to affect",
                waitingPromptTitle = "Waiting for opponent",
                source = "Triggered Abilities",
                cardCondition = card => choices.Any(context => context.GetEvent()?.card == card),
                buttons = addBackButton ? new List<MenuOption> { new MenuOption { text = "Back", arg = "back" } } : new List<MenuOption>(),
                onSelect = (player, card) =>
                {
                    var filteredChoices = choices.Where(context => context.GetEvent()?.card == card).ToList();
                    PromptBetweenEvents(filteredChoices);
                    return true;
                },
                onMenuCommand = (player, arg) =>
                {
                    if (arg == "back")
                    {
                        PromptBetweenSources(this.choices);
                        return true;
                    }
                    return false;
                }
            });
        }

        protected virtual void PromptBetweenEvents(List<AbilityContext> choices, bool addBackButton = true)
        {
            var uniqueChoices = choices.GroupBy(context => context.GetEvent()).Select(group => group.First()).ToList();
            
            if (uniqueChoices.Count == 1)
            {
                ResolveAbility(uniqueChoices[0]);
                return;
            }

            var menuChoices = uniqueChoices.Select(context => GetEventActionDescription(context.GetEvent())).ToList();
            var handlers = uniqueChoices.Select(context => new Action(() => ResolveAbility(context))).ToList();

            if (addBackButton)
            {
                menuChoices.Add("Back");
                handlers.Add(() => PromptBetweenSources(this.choices));
            }

            game.PromptWithHandlerMenu(currentPlayer, new HandlerMenuPromptProperties
            {
                activePromptTitle = "Choose an event to respond to",
                waitingPromptTitle = "Waiting for opponent",
                source = "Triggered Abilities",
                choices = menuChoices.Select(choice => new MenuOption { text = choice }).ToList(),
                handlers = handlers
            });
        }

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

        protected virtual void PostResolutionUpdate(AbilityResolver resolver)
        {
            resolvedAbilities.Add(new ResolvedAbility
            {
                ability = resolver.context.ability,
                gameEvent = resolver.context.GetEvent()
            });
        }

        protected virtual void EmitEvents()
        {
            choices.Clear();
            events = eventWindow.events.Except(eventsToExclude).ToList();
            
            foreach (var gameEvent in events)
            {
                game.Emit($"{gameEvent.name}:{abilityType}", gameEvent, this);
            }
            
            game.Emit($"aggregateEvent:{abilityType}", events, this);
        }

        protected virtual string GetPromptTitle()
        {
            // This would normally get title from TriggeredAbilityWindowTitles
            return $"Triggered Abilities - {abilityType}";
        }

        protected virtual string GetEventActionDescription(GameEvent gameEvent)
        {
            // This would normally get action description from TriggeredAbilityWindowTitles
            return gameEvent?.name ?? "Unknown event";
        }

        // IAbilityWindow methods
        public virtual void Open()
        {
            // Window is opened when Continue() is called and choices are filtered
        }

        public virtual void Close()
        {
            OnWindowClosed?.Invoke(this);
        }

        public override string GetDebugInfo()
        {
            return $"ForcedTriggeredAbilityWindow - Type: {abilityType} - Choices: {choices.Count} - Events: {events.Count}";
        }

        [System.Serializable]
        protected class ResolvedAbility
        {
            public BaseAbility ability;
            public GameEvent gameEvent;
        }
    }
}