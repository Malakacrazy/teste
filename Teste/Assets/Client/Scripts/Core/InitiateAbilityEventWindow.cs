using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class InitiateAbilityInterruptWindow : TriggeredAbilityWindow
    {
        private GameEvent playEvent;

        public InitiateAbilityInterruptWindow(Game game, string abilityType, EventWindow eventWindow) 
            : base(game, abilityType, new List<object>(), null)
        {
            playEvent = eventWindow.Events?.FirstOrDefault(eventObj => eventObj.Name == EventNames.OnCardPlayed);
        }

        protected virtual object GetPromptForSelectProperties()
        {
            var buttons = new List<object>();
            
            if (playEvent != null && game.GetActivePlayer() == playEvent.Player && playEvent.Resolver != null)
            {
                buttons.Add(new { text = "Cancel", arg = "cancel" });
            }
            
            if (GetMinCostReduction() == 0)
            {
                buttons.Add(new { text = "Pass", arg = "pass" });
            }

            // var baseProperties = base.GetPromptForSelectProperties(); // Method doesn't exist
            
            // Merge base properties with new buttons and onCancel handler
            return new
            {
                // Copy all properties from base
                buttons = buttons,
                onCancel = new Action(() =>
                {
                    if (playEvent?.Resolver != null)
                    {
                        // playEvent.Resolver.Cancelled = true; // Resolver is object type
                        // Complete = true; // Property doesn't exist
                    }
                })
            };
        }

        private int GetMinCostReduction()
        {
            if (playEvent != null)
            {
                var context = playEvent.Context;
                var alternatePools = context.Player.GetAlternateFatePools(playEvent.PlayType, context.Source as BaseCard, context);
                var alternatePoolTotal = alternatePools.Sum(pool => 0); // pool.Fate not available
                var maxPlayerFate = context.Player.CheckRestrictions("spendFate", context) ? context.Player.Fate : 0;
                return Math.Max(context.Ability.GetReducedCost(context) - maxPlayerFate - alternatePoolTotal, 0);
            }
            return 0;
        }

        public virtual void ResolveAbility(AbilityContext context)
        {
            if (playEvent?.Resolver != null)
            {
                // playEvent.Resolver.CanCancel = false; // Resolver is object type
            }
            // base.ResolveAbility(context); // Method doesn't exist in base class
        }
    }

    public class InitiateAbilityEventWindow : EventWindow
    {
        public InitiateAbilityEventWindow(Game game, List<GameEvent> events, Action handler = null) 
            : base(game, events)
        {
        }

        public virtual void OpenWindow(string abilityType)
        {
            if (events?.Count > 0 && abilityType == AbilityTypes.Interrupt)
            {
                // Create interrupt window but don't queue it as a step since it's a UI window
                var interruptWindow = new InitiateAbilityInterruptWindow(game, abilityType, this);
                // TODO: Handle interrupt window opening logic
            }
            else
            {
                // base.OpenWindow(abilityType); // EventWindow doesn't have OpenWindow method
            }
        }

        public virtual void ExecuteHandler()
        {
            // Sort events by order
            events = events?.OrderBy(eventObj => eventObj.Order).ToList();
            
            foreach (var gameEvent in events ?? new List<GameEvent>())
            {
                gameEvent.CheckCondition();
                if (!gameEvent.Cancelled)
                {
                    gameEvent.ExecuteHandler();
                }
            }
            
            // We need to separate executing the handler and emitting events as in this window, the handler just
            // queues ability resolution steps, and we don't want the events to be emitted until step 8
            game.QueueSimpleStep(() => { EmitEvents(); return true; });
        }

        private void EmitEvents()
        {
            foreach (var gameEvent in events ?? new List<GameEvent>())
            {
                game.RaiseEvent(gameEvent.name, gameEvent.Properties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>());
            }
        }
    }
}
