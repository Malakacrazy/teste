using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class HandlerMenuPrompt : UiPrompt
    {
        public Player player;
        public HandlerMenuPromptProperties properties;
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        public AbilityContext context;

        public HandlerMenuPrompt(Game game, Player player, HandlerMenuPromptProperties properties) : base(game)
        {
            this.player = player;
            
            // Handle source assignment
            if (properties.source == null && properties.context?.source != null)
            {
                properties.source = properties.context.source as EffectSource;
            }
            else if (properties.source == null)
            {
                // Create default effect source
                properties.source = EffectSource.CreateEffectSource(game);
            }

            if (properties.source != null && string.IsNullOrEmpty(properties.waitingPromptTitle))
            {
                properties.waitingPromptTitle = $"Waiting for opponent to use {properties.source.name}";
            }
            else if (properties.source == null)
            {
                properties.source = new EffectSource(game);
            }

            this.properties = properties;
            this.cardCondition = properties.cardCondition ?? ((card, ctx) => true);
            this.context = properties.context ?? new AbilityContext(game, player, properties.source);
        }

        public override bool ActiveCondition(Player player)
        {
            return player == this.player;
        }

        public override PromptInfo ActivePrompt()
        {
            var buttons = new List<ButtonInfo>();
            
            if (properties.cards != null)
            {
                var cardQuantities = new Dictionary<string, int>();
                foreach (var card in properties.cards)
                {
                    if (cardQuantities.ContainsKey(card.id))
                        cardQuantities[card.id]++;
                    else
                        cardQuantities[card.id] = 1;
                }

                var uniqueCards = properties.cards.GroupBy(card => card.id).Select(g => g.First());
                
                foreach (var card in uniqueCards)
                {
                    string text = card.name;
                    if (cardQuantities[card.id] > 1)
                    {
                        text += $" ({cardQuantities[card.id]})";
                    }
                    
                    buttons.Add(new ButtonInfo
                    {
                        text = text,
                        arg = card.id,
                        card = card,
                        disabled = !cardCondition(card, context)
                    });
                }
            }

            if (properties.choices != null)
            {
                for (int i = 0; i < properties.choices.Count; i++)
                {
                    buttons.Add(new ButtonInfo
                    {
                        text = properties.choices[i].text,
                        arg = properties.choices[i].arg ?? i.ToString()
                    });
                }
            }

            if (game.manualMode && (properties.choices == null || !properties.choices.Any(c => c.text == "Cancel")))
            {
                buttons.Add(new ButtonInfo { text = "Cancel Prompt", arg = "cancel" });
            }

            return new PromptInfo
            {
                menuTitle = properties.activePromptTitle ?? "Select one",
                buttons = buttons.ToArray(),
                controls = GetAdditionalPromptControls(),
                promptTitle = properties.source.name
            };
        }

        public PromptControl[] GetAdditionalPromptControls()
        {
            var firstControl = properties.controls?.FirstOrDefault() as PromptControl;
            if (firstControl?.type == "targeting")
            {
                return new PromptControl[]
                {
                    new PromptControl
                    {
                        type = "targeting",
                        source = properties.source.GetShortSummary(),
                        targets = firstControl.targets
                    }
                };
            }

            if (context.source.GetType().Name == "")
            {
                return new PromptControl[0];
            }

            var targets = context.targets?.Values.ToList() ?? new List<object>();
            
            if (properties.target != null)
            {
                targets = properties.target;
            }

            if (targets.Count == 0 && context.eventArgs is { } eventArgs)
            {
                // Try to get card property dynamically
                var cardProp = eventArgs.GetType().GetProperty("card");
                if (cardProp?.GetValue(eventArgs) is BaseCard card)
                {
                    targets = new List<object> { card };
                }
            }

            return new PromptControl[]
            {
                new PromptControl
                {
                    type = "targeting",
                    source = GetSourceSummary(context.source)?.ToString(),
                    targets = targets.OfType<BaseCard>()
                        .Select(target => target.GetShortSummaryForControls(player)?.ToString() ?? "").ToArray()
                }
            };
        }

        private object GetSourceSummary(object source)
        {
            if (source is BaseCard card)
                return card.GetShortSummary();
            if (source is EffectSource effectSource)
                return effectSource.GetShortSummary();
            if (source is Ring ring)
                return ring.GetShortSummary();
            if (source is Player player)
                return player.GetShortSummary();
            if (source is SelectChoice choice)
                return choice.GetShortSummary();
            if (source is Spectator spectator)
                return spectator.GetShortSummary();
            return source?.ToString() ?? "Unknown";
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = properties.waitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (arg == "cancel")
            {
                Complete();
                return true;
            }

            if (properties.cards != null)
            {
                var card = properties.cards.FirstOrDefault(c => c.id == arg);
                if (card != null && properties.cardHandler != null)
                {
                    properties.cardHandler(card);
                    Complete();
                    return true;
                }
            }

            if (int.TryParse(arg, out int choiceIndex))
            {
                if (properties.choiceHandler != null && choiceIndex < properties.choices.Count)
                {
                    properties.choiceHandler(properties.choices[choiceIndex].arg ?? arg);
                    Complete();
                    return true;
                }

                if (properties.handlers != null && choiceIndex < properties.handlers.Count)
                {
                    properties.handlers[choiceIndex]?.Invoke();
                    Complete();
                    return true;
                }
            }

            return false;
        }
    }
}
