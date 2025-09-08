using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class HandlerMenuPrompt : UiPrompt
    {
        public Player player;
        public PromptProperties properties;
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        public AbilityContext context;

        public HandlerMenuPrompt(Game game, Player player, PromptProperties properties) : base(game)
        {
            this.player = player;
            
            // Handle source assignment
            if (properties.source is string sourceStr)
            {
                properties.source = new EffectSource(game, sourceStr);
            }
            else if (properties.context?.source != null)
            {
                properties.source = properties.context.source;
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
                for (int i = 0; i < properties.choices.Length; i++)
                {
                    buttons.Add(new ButtonInfo
                    {
                        text = properties.choices[i],
                        arg = i.ToString()
                    });
                }
            }

            if (game.manualMode && (properties.choices == null || !properties.choices.Contains("Cancel")))
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
            if (properties.controls?.type == "targeting")
            {
                return new PromptControl[]
                {
                    new PromptControl
                    {
                        type = "targeting",
                        source = properties.source.GetShortSummary(),
                        targets = properties.controls.targets?.Select(target => 
                            target.GetShortSummaryForControls(player)).ToArray()
                    }
                };
            }

            if (context.source.type == "")
            {
                return new PromptControl[0];
            }

            var targets = context.targets?.Values.SelectMany(t => t).ToList() ?? new List<object>();
            
            if (properties.target != null)
            {
                targets = properties.target is Array targetArray ? 
                    targetArray.Cast<object>().ToList() : 
                    new List<object> { properties.target };
            }

            if (targets.Count == 0 && context.eventArgs?.card != null)
            {
                targets = new List<object> { context.eventArgs.card };
            }

            return new PromptControl[]
            {
                new PromptControl
                {
                    type = "targeting",
                    source = context.source.GetShortSummary(),
                    targets = targets.OfType<BaseCard>()
                        .Select(target => target.GetShortSummaryForControls(player)).ToArray()
                }
            };
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = properties.waitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool MenuCommand(Player player, string arg)
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
                if (properties.choiceHandler != null)
                {
                    properties.choiceHandler(properties.choices[choiceIndex]);
                    Complete();
                    return true;
                }

                if (properties.handlers != null && choiceIndex < properties.handlers.Length)
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
