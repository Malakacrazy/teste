using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectRingPrompt : UiPrompt
    {
        public Player choosingPlayer;
        public PromptProperties properties;
        public AbilityContext context;
        public Ring selectedRing;

        public SelectRingPrompt(Game game, Player choosingPlayer, PromptProperties properties) : base(game)
        {
            this.choosingPlayer = choosingPlayer;
            
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
            this.context = properties.context ?? new AbilityContext(game, choosingPlayer, properties.source);
            
            ApplyDefaultProperties();
            selectedRing = null;
        }

        private void ApplyDefaultProperties()
        {
            properties.buttons = properties.buttons ?? new ButtonInfo[0];
            properties.controls = properties.controls ?? GetDefaultControls();
            properties.ringCondition = properties.ringCondition ?? ((ring, ctx) => true);
            properties.onSelect = properties.onSelect ?? ((player, ring) => true);
            properties.onMenuCommand = properties.onMenuCommand ?? ((player, arg) => true);
            properties.onCancel = properties.onCancel ?? ((player) => true);
        }

        private PromptControl[] GetDefaultControls()
        {
            if (properties.context == null)
                return new PromptControl[0];
                
            var targets = properties.context.targets?.Values
                .Select(target => target.GetShortSummaryForControls(choosingPlayer))
                .ToArray() ?? new string[0];
                
            if (targets.Length == 0 && properties.context.eventArgs?.card != null)
            {
                targets = new string[] { properties.context.eventArgs.card.GetShortSummaryForControls(choosingPlayer) };
            }
            
            return new PromptControl[]
            {
                new PromptControl
                {
                    type = "targeting",
                    source = properties.context.source.GetShortSummary(),
                    targets = targets
                }
            };
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer;
        }

        public override bool Continue()
        {
            if (!IsComplete())
            {
                HighlightSelectableRings();
            }

            return base.Continue();
        }

        private void HighlightSelectableRings()
        {
            var selectableRings = game.rings.Where(ring => 
                properties.ringCondition(ring, context)).ToList();
            choosingPlayer.SetSelectableRings(selectableRings);
        }

        public override PromptInfo ActivePrompt()
        {
            var buttons = properties.buttons?.ToList() ?? new List<ButtonInfo>();
            
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

            if (!properties.ringCondition(ring, context))
                return true;

            if (properties.onSelect(player, ring))
            {
                Complete();
            }

            return true;
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "cancel")
            {
                properties.onCancel(player);
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
