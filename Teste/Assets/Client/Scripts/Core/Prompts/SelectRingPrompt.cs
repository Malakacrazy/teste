using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectRingPrompt : UiPrompt
    {
        public Player choosingPlayer;
        public SelectRingPromptProperties properties;
        public AbilityContext context;
        public Ring selectedRing;

        public SelectRingPrompt(Game game, Player choosingPlayer, SelectRingPromptProperties properties) : base(game)
        {
            this.choosingPlayer = choosingPlayer;
            
            // Handle source assignment
            if (properties.source == null && properties.context?.source != null)
            {
                properties.source = (EffectSource)properties.context.source;
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

            this.properties = properties;
            this.context = properties.context ?? new AbilityContext(game, choosingPlayer, properties.source);
            
            ApplyDefaultProperties();
            selectedRing = null;
        }

        private void ApplyDefaultProperties()
        {
            if (properties.buttons == null)
                properties.buttons = new List<MenuOption>();
            if (properties.controls == null)
                properties.controls = GetDefaultControls().ToList<object>();
            if (properties.ringCondition == null)
                properties.ringCondition = (ring) => true;
            if (properties.onSelect == null)
                properties.onSelect = (player, ring) => true;
            if (properties.onMenuCommand == null)
                properties.onMenuCommand = (player, arg) => true;
            if (properties.onCancel == null)
                properties.onCancel = () => true;
        }

        private PromptControl[] GetDefaultControls()
        {
            if (properties.context == null)
                return new PromptControl[0];
                
            string[] targets = new string[0];
            if (properties.context.targets?.Values != null)
            {
                targets = properties.context.targets.Values
                    .OfType<BaseCard>()
                    .Select(target => target.GetShortSummaryForControls(choosingPlayer)?.ToString() ?? "")
                    .ToArray();
            }
                
            if (targets.Length == 0 && properties.context.eventArgs is { } eventArgs)
            {
                // Try to get card property dynamically
                var cardProp = eventArgs.GetType().GetProperty("card");
                if (cardProp?.GetValue(eventArgs) is BaseCard card)
                {
                    targets = new string[] { card.GetShortSummaryForControls(choosingPlayer)?.ToString() ?? "" };
                }
            }
            
            return new PromptControl[]
            {
                new PromptControl
                {
                    type = "targeting",
                    source = GetSourceSummary(properties.context.source)?.ToString() ?? "",
                    targets = targets
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

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer;
        }

        public override bool Continue()
        {
            if (!IsComplete)
            {
                HighlightSelectableRings();
            }

            return base.Continue();
        }

        private void HighlightSelectableRings()
        {
            var selectableRings = game.rings.Values.Where(ring => 
                properties.ringCondition(ring)).ToList();
            choosingPlayer.SetSelectableRings(selectableRings);
        }

        public override PromptInfo ActivePrompt()
        {
            var buttons = properties.buttons?.Select(m => new ButtonInfo { text = m.text, arg = m.arg, method = m.method, disabled = m.disabled }).ToList() ?? new List<ButtonInfo>();
            
            if (properties.optional)
            {
                buttons.Add(new ButtonInfo { text = "Done", arg = "done" });
            }
            
            if (game.manualMode && !buttons.Any(button => button.arg == "cancel"))
            {
                buttons.Add(new ButtonInfo { text = "Cancel Prompt", arg = "cancel" });
            }

            return new PromptInfo
            {
                source = properties.source,
                selectCard = true,
                selectRing = true,
                selectOrder = properties.ordered,
                menuTitle = properties.activePromptTitle ?? DefaultActivePromptTitle(),
                buttons = buttons.ToArray(),
                promptTitle = properties.source?.name
            };
        }

        private string DefaultActivePromptTitle()
        {
            return "Choose a ring";
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = properties.waitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool OnRingClicked(Player player, Ring ring)
        {
            if (player != choosingPlayer)
                return false;

            if (!properties.ringCondition(ring))
                return true;

            // Since this is a ring selection, complete directly
            Complete();

            return true;
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (arg == "cancel")
            {
                properties.onCancel();
                Complete();
                return true;
            }
            else if (properties.onMenuCommand(player, arg))
            {
                Complete();
                return true;
            }
            
            return false;
        }

        public override void Complete()
        {
            choosingPlayer.ClearSelectableRings();
            base.Complete();
        }
    }
}
