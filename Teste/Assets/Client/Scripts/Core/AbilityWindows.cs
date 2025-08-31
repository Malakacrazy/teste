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

}
