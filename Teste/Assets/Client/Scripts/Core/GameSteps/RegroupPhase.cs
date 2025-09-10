using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Regroup Phase implementation
    /// V Regroup Phase
    /// 5.1 Regroup phase begins.
    ///     ACTION WINDOW
    /// 5.2 Ready cards.
    /// 5.3 Discard from provinces.
    /// 5.4 Return rings.
    /// 5.5 Pass first player token.
    /// 5.6 Regroup phase ends.
    /// </summary>
    public class RegroupPhase : Phase
    {
        public RegroupPhase(Game game) : base(game, GamePhases.Regroup)
        {
            InitializePhase(new List<BaseStep>
            {
                new ActionWindow(this.game, "Action Window"),
                new SimpleStep(game, ReadyCards),
                new SimpleStep(game, DiscardFromProvinces),
                new SimpleStep(game, ReturnRings),
                new SimpleStep(game, PassFirstPlayer),
                new EndRoundPrompt(game),
                new SimpleStep(game, RoundEnded)
            });
        }

        /// <summary>
        /// Ready all cards that should ready during the ready phase
        /// </summary>
        protected virtual bool ReadyCards()
        {
            var cardsToReady = game.GetAllCards()
                .Where(card => card.isBowed && card.ReadiesDuringReadyPhase())
                .ToList();

            if (cardsToReady.Count > 0)
            {
                var readyAction = game.actions.Ready(cardsToReady);
                readyAction.Resolve(cardsToReady, game.GetFrameworkContext());
            }

            return true;
        }

        /// <summary>
        /// Discard cards from provinces for all players
        /// </summary>
        protected virtual bool DiscardFromProvinces()
        {
            var playersInOrder = game.GetPlayersInFirstPlayerOrder();
            foreach (var player in playersInOrder)
            {
                game.QueueSimpleStep(() => DiscardFromProvincesForPlayer(player));
            }
            return true;
        }

        /// <summary>
        /// Handle province discard for a specific player
        /// </summary>
        protected virtual bool DiscardFromProvincesForPlayer(Player player)
        {
            var cardsToDiscard = new List<BaseCard>();
            var cardsOnUnbrokenProvinces = new List<BaseCard>();

            var provinceLocations = new[]
            {
                CardLocations.ProvinceOne,
                CardLocations.ProvinceTwo,
                CardLocations.ProvinceThree,
                CardLocations.ProvinceFour,
                CardLocations.StrongholdProvince
            };

            foreach (var location in provinceLocations)
            {
                var provinceCard = player.GetProvinceCardInProvince(location);
                var province = player.GetSourceList(location);
                var dynastyCards = province.Where(card => card.isDynasty && !card.facedown).ToList();

                if (dynastyCards.Count > 0 && provinceCard != null)
                {
                    if (provinceCard.isBroken)
                    {
                        cardsToDiscard.AddRange(dynastyCards);
                    }
                    else
                    {
                        cardsOnUnbrokenProvinces.AddRange(dynastyCards);
                    }
                }
            }

            if (cardsOnUnbrokenProvinces.Count > 0)
            {
                game.PromptForSelect(player, new SelectCardPromptProperties
                {
                    source = "Discard Dynasty Cards",
                    numCards = 0,
                    multiSelect = true,
                    optional = true,
                    activePromptTitle = "Select dynasty cards to discard",
                    waitingPromptTitle = "Waiting for opponent to discard dynasty cards",
                    cardType = CardTypes.Dynasty,
                    controller = Players.Self,
                    cardCondition = card => cardsOnUnbrokenProvinces.Contains(card),
                    onSelect = (selectedPlayer, cards) =>
                    {
                        cardsToDiscard.AddRange(cards);
                        FinishDiscardingCards(player, cardsToDiscard);
                        return true;
                    },
                    onCancel = (selectedPlayer) =>
                    {
                        FinishDiscardingCards(player, cardsToDiscard);
                        return true;
                    }
                });
            }
            else if (cardsToDiscard.Count > 0)
            {
                FinishDiscardingCards(player, cardsToDiscard);
            }

            // Queue replacement of dynasty cards
            game.QueueSimpleStep(() =>
            {
                var normalProvinces = new[]
                {
                    CardLocations.ProvinceOne,
                    CardLocations.ProvinceTwo,
                    CardLocations.ProvinceThree,
                    CardLocations.ProvinceFour
                };

                foreach (var location in normalProvinces)
                {
                    game.QueueSimpleStep(() =>
                    {
                        player.ReplaceDynastyCard(location);
                        return true;
                    });
                }
                return true;
            });

            return true;
        }

        /// <summary>
        /// Complete the card discarding process
        /// </summary>
        private void FinishDiscardingCards(Player player, List<BaseCard> cardsToDiscard)
        {
            if (cardsToDiscard.Count > 0)
            {
                game.AddMessage("{0} discards {1} from their provinces", 
                               player.name, string.Join(", ", cardsToDiscard.Select(c => c.name)));
                
                var discardAction = game.actions.DiscardCard(cardsToDiscard);
                discardAction.Resolve(cardsToDiscard, game.GetFrameworkContext());
            }
        }

        /// <summary>
        /// Return all claimed rings
        /// </summary>
        protected virtual bool ReturnRings()
        {
            var claimedRings = game.rings.Where(ring => ring.claimed).ToList();
            if (claimedRings.Count > 0)
            {
                var returnRingAction = game.actions.ReturnRing(claimedRings);
                returnRingAction.Resolve(claimedRings, game.GetFrameworkContext());
            }
            return true;
        }

        /// <summary>
        /// Pass the first player token to the other player
        /// </summary>
        protected virtual bool PassFirstPlayer()
        {
            var firstPlayer = game.GetFirstPlayer();
            var otherPlayer = game.GetOtherPlayer(firstPlayer);
            
            if (otherPlayer != null)
            {
                game.RaiseEvent(EventNames.OnPassFirstPlayer, 
                    new Dictionary<string, object> { { "player", otherPlayer } },
                    (eventData) =>
                    {
                        game.SetFirstPlayer(otherPlayer);
                        return true;
                    });
            }
            
            return true;
        }

        /// <summary>
        /// Signal that the round has ended
        /// </summary>
        protected virtual bool RoundEnded()
        {
            game.RaiseEvent(EventNames.OnRoundEnded);
            return true;
        }

        public override string GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            var firstPlayer = game.GetFirstPlayer();
            return $"{baseInfo} - First Player: {firstPlayer?.name ?? "None"}";
        }
    }
}