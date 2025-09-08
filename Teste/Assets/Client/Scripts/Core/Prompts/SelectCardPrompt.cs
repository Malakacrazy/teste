using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectCardPrompt : UiPrompt
    {
        public Player choosingPlayer;
        public PromptProperties properties;
        public AbilityContext context;
        public BaseCardSelector selector;
        public List<BaseCard> selectedCards;
        public List<BaseCard> previouslySelectedCards;
        public bool onlyMustSelectMayBeChosen;
        public bool cannotUnselectMustSelect;

        public SelectCardPrompt(Game game, Player choosingPlayer, PromptProperties properties) : base(game)
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
            
            if (properties.source == null)
            {
                properties.source = new EffectSource(game);
            }

            this.properties = properties;
            this.context = properties.context ?? new AbilityContext(game, choosingPlayer, properties.source);
            
            // Apply defaults
            ApplyDefaultProperties();
            
            // Handle game actions
            if (properties.gameAction != null)
            {
                if (!properties.gameAction.GetType().IsArray)
                {
                    properties.gameAction = new GameAction[] { properties.gameAction as GameAction };
                }
                
                var originalCardCondition = properties.cardCondition;
                properties.cardCondition = (card, ctx) =>
                    originalCardCondition(card, ctx) && 
                    (properties.gameAction as GameAction[]).Any(gameAction => gameAction.CanAffect(card, ctx));
            }
            
            this.selector = properties.selector ?? CardSelector.For(properties);
            this.selectedCards = new List<BaseCard>();
            
            if (properties.mustSelect != null)
            {
                if (selector.HasEnoughSelected(properties.mustSelect) && 
                    selector.numCards > 0 && 
                    properties.mustSelect.Count >= selector.numCards)
                {
                    onlyMustSelectMayBeChosen = true;
                }
                else
                {
                    selectedCards.AddRange(properties.mustSelect);
                    cannotUnselectMustSelect = true;
                }
            }
            
            SavePreviouslySelectedCards();
        }

        private void ApplyDefaultProperties()
        {
            properties.buttons = properties.buttons ?? new ButtonInfo[0];
            properties.controls = properties.controls ?? GetDefaultControls();
            properties.selectCard = properties.selectCard ?? true;
            properties.cardCondition = properties.cardCondition ?? ((card, ctx) => true);
            properties.onSelect = properties.onSelect ?? ((player, cards) => true);
            properties.onMenuCommand = properties.onMenuCommand ?? ((player, arg) => true);
            properties.onCancel = properties.onCancel ?? ((player) => true);
        }

        private PromptControl[] GetDefaultControls()
        {
            var targets = context.targets?.Values.SelectMany(t => t).ToList() ?? new List<object>();
            
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
                        .Select(target => target.GetShortSummaryForControls(choosingPlayer)).ToArray()
                }
            };
        }

        private void SavePreviouslySelectedCards()
        {
            previouslySelectedCards = choosingPlayer.selectedCards?.ToList() ?? new List<BaseCard>();
            choosingPlayer.ClearSelectedCards();
            choosingPlayer.SetSelectedCards(selectedCards);
        }

        public override bool Continue()
        {
            if (!IsComplete())
            {
                HighlightSelectableCards();
            }

            return base.Continue();
        }

        private void HighlightSelectableCards()
        {
            var selectableCards = selector.FindPossibleCards(context)
                .Where(card => CheckCardCondition(card))
                .ToList();
            choosingPlayer.SetSelectableCards(selectableCards);
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer;
        }

        public override PromptInfo ActivePrompt()
        {
            var buttons = properties.buttons?.ToList() ?? new List<ButtonInfo>();
            
            if (!selector.AutomaticFireOnSelect(context) && 
                selector.HasEnoughSelected(selectedCards, context) || 
                selector.optional)
            {
                if (!buttons.Any(button => button.arg == "done"))
                {
                    buttons.Insert(0, new ButtonInfo { text = "Done", arg = "done" });
                }
            }
            
            if (game.manualMode && !buttons.Any(button => button.arg == "cancel"))
            {
                buttons.Add(new ButtonInfo { text = "Cancel Prompt", arg = "cancel" });
            }

            return new PromptInfo
            {
                selectCard = properties.selectCard ?? false,
                selectRing = true,
                selectOrder = properties.ordered,
                menuTitle = properties.activePromptTitle ?? selector.DefaultActivePromptTitle(context),
                buttons = buttons.ToArray(),
                promptTitle = properties.source?.name,
                controls = properties.controls
            };
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = properties.waitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player != choosingPlayer)
                return false;

            if (!CheckCardCondition(card))
                return false;

            if (!SelectCard(card))
                return false;

            if (selector.AutomaticFireOnSelect(context) && 
                selector.HasReachedLimit(selectedCards, context))
            {
                FireOnSelect();
            }

            return true;
        }

        private bool CheckCardCondition(BaseCard card)
        {
            if (onlyMustSelectMayBeChosen && !properties.mustSelect.Contains(card))
            {
                return false;
            }
            else if (selectedCards.Contains(card))
            {
                return true;
            }

            return selector.CanTarget(card, context, choosingPlayer, selectedCards) &&
                   !selector.WouldExceedLimit(selectedCards, card);
        }

        private bool SelectCard(BaseCard card)
        {
            if (selector.HasReachedLimit(selectedCards, context) && !selectedCards.Contains(card))
            {
                return false;
            }
            else if (cannotUnselectMustSelect && properties.mustSelect.Contains(card))
            {
                return false;
            }

            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
            }
            else
            {
                selectedCards.Remove(card);
            }
            
            choosingPlayer.SetSelectedCards(selectedCards);

            properties.onCardToggle?.Invoke(choosingPlayer, card);

            return true;
        }

        private bool FireOnSelect()
        {
            var cardParam = selector.FormatSelectParam(selectedCards);
            if (properties.onSelect(choosingPlayer, cardParam))
            {
                Complete();
                return true;
            }
            
            ClearSelection();
            return false;
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (arg == "cancel")
            {
                properties.onCancel(player);
                Complete();
                return true;
            }
            else if (arg == "done" && selector.HasEnoughSelected(selectedCards, context))
            {
                return FireOnSelect();
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
            ClearSelection();
            base.Complete();
        }

        private void ClearSelection()
        {
            selectedCards.Clear();
            choosingPlayer.ClearSelectedCards();
            choosingPlayer.ClearSelectableCards();
            choosingPlayer.ClearSelectableRings();

            // Restore previous selections
            choosingPlayer.SetSelectedCards(previouslySelectedCards);
        }
    }
}
