using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for triggered ability windows
    /// </summary>
    public abstract class BaseAbilityWindow : IAbilityWindow
    {
        protected Game game;
        protected string abilityType;
        protected List<object> events;
        protected List<object> eventsToExclude;
        protected List<AbilityContext> choices = new List<AbilityContext>();
        protected bool isOpen = false;

        public string AbilityType => abilityType;
        public List<object> Events => events;
        public event Action<IAbilityWindow> OnWindowClosed;

        protected BaseAbilityWindow(Game game, string abilityType, List<object> events, List<object> eventsToExclude = null)
        {
            this.game = game;
            this.abilityType = abilityType;
            this.events = events ?? new List<object>();
            this.eventsToExclude = eventsToExclude ?? new List<object>();
        }

        public virtual void AddChoice(AbilityContext context)
        {
            if (context != null && !choices.Contains(context))
            {
                choices.Add(context);
            }
        }

        public virtual void Open()
        {
            isOpen = true;
            ProcessChoices();
        }

        public virtual void Close()
        {
            isOpen = false;
            choices.Clear();
            OnWindowClosed?.Invoke(this);
        }

        protected abstract void ProcessChoices();
    }

    /// <summary>
    /// Window for regular triggered abilities (reactions, interrupts)
    /// </summary>
    public class TriggeredAbilityWindow : BaseAbilityWindow
    {
        public TriggeredAbilityWindow(Game game, string abilityType, List<object> events, List<object> eventsToExclude = null)
            : base(game, abilityType, events, eventsToExclude)
        {
        }

        protected override void ProcessChoices()
        {
            if (choices.Count == 0)
            {
                Close();
                return;
            }

            // Process triggered abilities - player chooses which to trigger
            foreach (var choice in choices)
            {
                game.PromptForSelect(choice.player, new SelectCardPromptProperties
                {
                    activePromptTitle = $"Trigger {choice.ability}?",
                    cardCondition = (card) => card == choice.source,
                    onSelect = (player, card) => 
                    {
                        ExecuteChoice(choice);
                        return true;
                    }
                });
            }
        }

        public void Pass(Player player)
        {
            // Player passes on triggered abilities
            Close();
        }

        private void ExecuteChoice(AbilityContext choice)
        {
            choice.ability?.Execute(choice);
        }
    }

    /// <summary>
    /// Window for forced triggered abilities (must resolve)
    /// </summary>
    public class ForcedTriggeredAbilityWindow : BaseAbilityWindow
    {
        public ForcedTriggeredAbilityWindow(Game game, string abilityType, List<object> events, List<object> eventsToExclude = null)
            : base(game, abilityType, events, eventsToExclude)
        {
        }

        protected override void ProcessChoices()
        {
            // Forced abilities must be resolved automatically
            foreach (var choice in choices)
            {
                choice.ability?.Execute(choice);
            }
            Close();
        }
    }
}
