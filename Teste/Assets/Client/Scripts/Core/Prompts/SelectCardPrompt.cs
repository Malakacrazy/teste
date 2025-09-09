using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectCardPrompt : UiPrompt
    {
        public Player choosingPlayer;
        public SelectCardPromptProperties properties;
        public AbilityContext context;
        public BaseCardSelector selector;
        public List<BaseCard> selectedCards;
        public List<BaseCard> previouslySelectedCards;
        public bool onlyMustSelectMayBeChosen;
        public bool cannotUnselectMustSelect;

        public SelectCardPrompt(Game game, Player choosingPlayer, SelectCardPromptProperties properties) : base(game)
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
            
            if (properties.source == null)
            {
                properties.source = EffectSource.CreateEffectSource(game);
            }

            this.properties = properties;
            this.context = properties.context ?? new AbilityContext(game, choosingPlayer, properties.source);
            
            // Apply defaults
            ApplyDefaultProperties();
            
            // Handle game actions
            GameAction[] gameActions = null;
            if (properties.gameAction != null)
            {
                gameActions = new GameAction[] { properties.gameAction };
                
                var originalCardCondition = properties.cardCondition;
                properties.cardCondition = (card) =>
                    originalCardCondition(card) && 
                    gameActions.Any(gameAction => gameAction.CanAffect(card, context));
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
            if (properties.buttons == null)
                properties.buttons = new List<MenuOption>();
            if (properties.controls == null)
                properties.controls = GetDefaultControls().ToList<object>();
            if (properties.selectCard == null)
                properties.selectCard = (card) => { /* Default card selection */ };
            if (properties.cardCondition == null)
                properties.cardCondition = (card) => true;
            if (properties.onSelect == null)
                properties.onSelect = (player, card) => true;
            if (properties.onMenuCommand == null)
                properties.onMenuCommand = (player, arg) => true;
            if (properties.onCancel == null)
                properties.onCancel = () => true;
        }

        private PromptControl[] GetDefaultControls()
        {
            var targets = context.targets?.Values.ToList() ?? new List<object>();
            
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
                    source = GetSourceSummary(context.source),
                    targets = targets.OfType<BaseCard>()
                        .Select(target => target.GetShortSummaryForControls(choosingPlayer)?.ToString() ?? "").ToArray()
                }
            };
        }

        private string GetSourceSummary(object source)
        {
            if (source is BaseCard card)
                return card.GetShortSummary()?.ToString() ?? "";
            if (source is EffectSource effectSource)
                return effectSource.GetShortSummary()?.ToString() ?? "";
            if (source is Ring ring)
            {
                var ringSummary = ring.GetShortSummary();
                return ringSummary?.ToString() ?? ring.name ?? "Unknown Ring";
            }
            if (source is Player player)
            {
                var playerSummary = player.GetShortSummary();
                return playerSummary?.ToString() ?? player.name ?? "Unknown Player";
            }
            if (source is SelectChoice choice)
                return choice.GetShortSummary()?.ToString() ?? "";
            if (source is Spectator spectator)
            {
                var spectatorSummary = spectator.GetShortSummary();
                return spectatorSummary?.ToString() ?? spectator.name ?? "Unknown Spectator";
            }
            return source?.ToString() ?? "Unknown";
        }

        private void SavePreviouslySelectedCards()
        {
            previouslySelectedCards = choosingPlayer.selectedCards?.ToList() ?? new List<BaseCard>();
            choosingPlayer.ClearSelectedCards();
            choosingPlayer.SetSelectedCards(selectedCards);
        }

        public override bool Continue()
        {
            if (!IsComplete)
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
            var buttons = properties.buttons?.Select(m => new ButtonInfo { text = m.text, arg = m.arg, method = m.method, disabled = m.disabled }).ToList() ?? new List<ButtonInfo>();
            
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
                selectCard = true,
                selectRing = true,
                selectOrder = properties.ordered,
                menuTitle = properties.activePromptTitle ?? selector.DefaultActivePromptTitle(context),
                buttons = buttons.ToArray(),
                promptTitle = properties.source?.name,
                controls = properties.controls?.OfType<PromptControl>().ToArray() ?? new PromptControl[0]
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

            properties.onCardToggle?.Invoke(card);

            return true;
        }

        private bool FireOnSelect()
        {
            var cardParam = selector.FormatSelectParam(selectedCards);
            List<BaseCard> cards = cardParam as List<BaseCard> ?? selectedCards;
            if (properties.onSelectMultiple?.Invoke(choosingPlayer, cards) ?? true)
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
                properties.onCancel();
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
