using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Prompt for dynasty card mulligan during setup
    /// </summary>
    public class MulliganDynastyPrompt : AllPlayerPrompt
    {
        protected Dictionary<string, List<BaseCard>> selectedCards;
        protected Dictionary<string, List<BaseCard>> selectableCards;

        public MulliganDynastyPrompt(Game game) : base(game)
        {
            selectedCards = new Dictionary<string, List<BaseCard>>();
            selectableCards = new Dictionary<string, List<BaseCard>>();

            foreach (var player in game.GetPlayers())
            {
                selectedCards[player.name] = new List<BaseCard>();
            }
        }

        public override bool CompletionCondition(Player player)
        {
            return player.takenDynastyMulligan;
        }

        public override bool Continue()
        {
            if (!IsComplete)
            {
                HighlightSelectableCards();
            }

            return base.Continue();
        }

        protected virtual void HighlightSelectableCards()
        {
            foreach (var player in game.GetPlayers())
            {
                if (!selectableCards.ContainsKey(player.name))
                {
                    var provinceLocations = new List<string>
                    {
                        Locations.ProvinceOne,
                        Locations.ProvinceTwo,
                        Locations.ProvinceThree,
                        Locations.ProvinceFour
                    };

                    selectableCards[player.name] = provinceLocations
                        .Select(location => player.GetDynastyCardInProvince(location))
                        .Where(card => card != null)
                        .ToList();
                }
                player.SetSelectableCards(selectableCards[player.name]);
            }
        }

        public override PromptProperties ActivePrompt()
        {
            return new PromptProperties
            {
                selectCard = true,
                selectRing = true,
                menuTitle = "Select dynasty cards to mulligan",
                buttons = new List<ButtonProperties>
                {
                    new ButtonProperties { text = "Done", arg = "done" }
                },
                promptTitle = "Dynasty Mulligan"
            };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player == null || !ActiveCondition(player) || card == null)
            {
                return false;
            }

            if (!CardCondition(card))
            {
                return false;
            }

            if (!selectedCards[player.name].Contains(card))
            {
                selectedCards[player.name].Add(card);
            }
            else
            {
                selectedCards[player.name].Remove(card);
            }

            player.SetSelectedCards(selectedCards[player.name]);
            return true;
        }

        protected virtual bool CardCondition(BaseCard card)
        {
            return card.isDynasty && card.IsInProvince();
        }

        public override PromptProperties WaitingPrompt()
        {
            return new PromptProperties
            {
                menuTitle = "Waiting for opponent to mulligan dynasty cards"
            };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "done")
            {
                if (selectedCards[player.name].Count > 0)
                {
                    // Replace selected cards with new ones from dynasty deck
                    foreach (var card in selectedCards[player.name])
                    {
                        if (player.dynastyDeck.Size() > 0)
                        {
                            var topCard = player.dynastyDeck.First();
                            player.MoveCard(topCard, card.location);
                        }
                    }

                    // Move selected cards to bottom of dynasty deck
                    foreach (var card in selectedCards[player.name])
                    {
                        string location = card.location;
                        player.MoveCard(card, "dynasty deck bottom");
                        player.ReplaceDynastyCard(location);
                    }

                    player.ShuffleDynastyDeck();
                    game.AddMessage("{0} has mulliganed {1} cards from the dynasty deck",
                        player, selectedCards[player.name].Count);
                }
                else
                {
                    game.AddMessage("{0} has kept all dynasty cards", player);
                }

                player.ClearSelectedCards();
                player.ClearSelectableCards();
                player.takenDynastyMulligan = true;
                return true;
            }
            return false;
        }
    }
}