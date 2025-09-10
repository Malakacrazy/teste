using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Window for resolving simultaneous effects with player choice ordering
    /// </summary>
    public class SimultaneousEffectWindow : ForcedTriggeredAbilityWindow
    {
        private List<EffectChoice> effectChoices = new List<EffectChoice>();

        public SimultaneousEffectWindow(Game game) : base(game, "delayedeffects")
        {
        }

        public virtual void AddChoice(EffectChoice choice)
        {
            if (choice.Condition == null)
            {
                choice.Condition = () => true;
            }
            effectChoices.Add(choice);
        }

        protected override bool FilterChoices()
        {
            var validChoices = effectChoices.Where(choice => choice.IsAvailable()).ToList();
            
            if (validChoices.Count == 0)
            {
                return true;
            }
            
            if (validChoices.Count == 1 || !currentPlayer.optionSettings.orderForcedAbilities)
            {
                ResolveEffect(validChoices[0]);
            }
            else
            {
                PromptBetweenChoices(validChoices);
            }
            
            return false;
        }

        protected virtual void PromptBetweenChoices(List<EffectChoice> choices)
        {
            var menuChoices = choices.Select(choice => choice.Title).ToList();
            var handlers = choices.Select(choice => new Action(() => ResolveEffect(choice))).ToList();

            game.PromptWithHandlerMenu(currentPlayer, new HandlerMenuPromptProperties
            {
                activePromptTitle = "Choose an effect to be resolved",
                waitingPromptTitle = "Waiting for opponent",
                source = "Order Simultaneous effects",
                choices = menuChoices.Select(choice => new MenuOption { text = choice }).ToList(),
                handlers = handlers
            });
        }

        protected virtual void ResolveEffect(EffectChoice choice)
        {
            effectChoices.Remove(choice);
            choice.Execute();
        }

        public override string GetDebugInfo()
        {
            return $"SimultaneousEffectWindow - Choices: {effectChoices.Count}";
        }
    }

    // EffectChoice is now defined in separate file: EffectChoice.cs
}