using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base implementation for triggered ability windows
    /// </summary>
    public class TriggeredAbilityWindow : IAbilityWindow
    {
        public string AbilityType { get; private set; }
        public List<object> Events { get; private set; }
        public event Action<IAbilityWindow> OnWindowClosed;
        
        protected Game game;
        protected List<object> eventsToExclude;
        protected List<AbilityContext> choices = new List<AbilityContext>();
        protected bool isOpen = false;
        
        public TriggeredAbilityWindow(Game game, string abilityType, List<object> events, List<object> eventsToExclude)
        {
            this.game = game;
            AbilityType = abilityType;
            Events = events ?? new List<object>();
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
            // Placeholder implementation
        }
        
        public virtual void Close()
        {
            isOpen = false;
            OnWindowClosed?.Invoke(this);
        }
        
        public virtual void Pass(Player player)
        {
            // Handle player passing
            Close();
        }
    }
    
    /// <summary>
    /// Window for forced triggered abilities that must resolve
    /// </summary>
    public class ForcedTriggeredAbilityWindow : TriggeredAbilityWindow
    {
        public ForcedTriggeredAbilityWindow(Game game, string abilityType, List<object> events, List<object> eventsToExclude)
            : base(game, abilityType, events, eventsToExclude)
        {
        }
        
        public override void Open()
        {
            isOpen = true;
            // Forced abilities automatically resolve
            if (choices.Count > 0)
            {
                // Execute the first available choice automatically
                var choice = choices[0];
                game.ResolveAbility(choice);
            }
            Close();
        }
    }
}
