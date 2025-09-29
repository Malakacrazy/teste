using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Prompt for conflict card mulligan during setup
    /// </summary>
    public class MulliganConflictPrompt : MulliganDynastyPrompt
    {
        public MulliganConflictPrompt(Game game) : base(game)
        {
        }

        public override bool CompletionCondition(Player player)
        {
            return player.takenConflictMulligan;
        }

        public override PromptProperties ActivePrompt()
        {
            var basePrompt = base.ActivePrompt();
            basePrompt.menuTitle = "Select conflict cards to mulligan";
            basePrompt.promptTitle = "Conflict Mulligan";
            return basePrompt;
        }

        protected override void HighlightSelectableCards()
        {
            foreach (var player in game.GetPlayers())
            {
                if (!selectableCards.ContainsKey(player.name))
                {
                    selectableCards[player.name] = player.hand.ToList();
                }
                player.SetSelectableCards(selectableCards[player.name]);
            }
        }

        protected override bool CardCondition(BaseCard card)
        {
            return card.location == Locations.Hand;
        }

        public override PromptProperties WaitingPrompt()
        {
            return new PromptProperties
            {
                menuTitle = "Waiting for opponent to mulligan conflict cards"
            };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "done")
            {
                if (selectedCards[player.name].Count > 0)
                {
                    // Move selected cards to bottom of conflict deck
                    foreach (var card in selectedCards[player.name])
                    {
                        player.MoveCard(card, "conflict deck bottom");
                    }

                    // Draw replacement cards
                    player.DrawCardsToHand(selectedCards[player.name].Count);
                    player.ShuffleConflictDeck();

                    game.AddMessage("{0} has mulliganed {1} cards from the conflict deck",
                        player, selectedCards[player.name].Count);
                }
                else
                {
                    game.AddMessage("{0} has kept all conflict cards", player);
                }

                // Set dynasty cards face down
                var provinceLocations = new List<string>
                {
                    Locations.ProvinceOne,
                    Locations.ProvinceTwo,
                    Locations.ProvinceThree,
                    Locations.ProvinceFour
                };

                foreach (var location in provinceLocations)
                {
                    var card = player.GetDynastyCardInProvince(location);
                    if (card != null)
                    {
                        card.facedown = true;
                    }
                }

                player.ClearSelectedCards();
                player.ClearSelectableCards();
                player.takenConflictMulligan = true;
                readyToStart = true;
                return true;
            }
            return false;
        }
    }
}