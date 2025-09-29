using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Prompt for setting up province order during setup
    /// </summary>
    public class SetupProvincesPrompt : AllPlayerPrompt
    {
        private Dictionary<string, BaseCard> strongholdProvince;
        private Dictionary<string, bool> clickedDone;
        private Dictionary<string, List<BaseCard>> selectedCards;
        private Dictionary<string, List<BaseCard>> selectableCards;

        public SetupProvincesPrompt(Game game) : base(game)
        {
            strongholdProvince = new Dictionary<string, BaseCard>();
            clickedDone = new Dictionary<string, bool>();
            selectedCards = new Dictionary<string, List<BaseCard>>();
            selectableCards = new Dictionary<string, List<BaseCard>>();

            foreach (var player in game.GetPlayers())
            {
                selectedCards[player.uuid] = new List<BaseCard>();
                selectableCards[player.uuid] = player.provinceDeck.ToList();
            }
        }

        public override bool CompletionCondition(Player player)
        {
            return clickedDone.ContainsKey(player.uuid) && clickedDone[player.uuid];
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
            foreach (var player in game.GetPlayers())
            {
                player.SetSelectableCards(selectableCards[player.uuid]);
            }
        }

        public override PromptProperties ActivePrompt(Player player = null)
        {
            if (player == null) return base.ActivePrompt();

            string menuTitle = "Choose province order, or press Done to place them at random";
            var buttons = new List<ButtonProperties>();

            if (!strongholdProvince.ContainsKey(player.uuid) || strongholdProvince[player.uuid] == null)
            {
                menuTitle = "Select stronghold province";
            }
            else
            {
                buttons.Add(new ButtonProperties { text = "Done", arg = "done" });
                buttons.Add(new ButtonProperties { text = "Change stronghold province", arg = "change" });
            }

            return new PromptProperties
            {
                selectCard = true,
                selectRing = true,
                selectOrder = strongholdProvince.ContainsKey(player.uuid) && strongholdProvince[player.uuid] != null,
                menuTitle = menuTitle,
                buttons = buttons,
                promptTitle = "Place Provinces"
            };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player == null || !ActiveCondition(player) || card == null)
            {
                return false;
            }

            if (!selectableCards[player.uuid].Contains(card))
            {
                return false;
            }

            // First, select stronghold province
            if (!strongholdProvince.ContainsKey(player.uuid) || strongholdProvince[player.uuid] == null)
            {
                if (card.CannotBeStrongholdProvince())
                {
                    return false;
                }

                strongholdProvince[player.uuid] = card;
                card.inConflict = true;
                selectableCards[player.uuid].Remove(card);
                return true;
            }

            // Then select other provinces in order
            if (!selectedCards[player.uuid].Contains(card))
            {
                selectedCards[player.uuid].Add(card);
            }
            else
            {
                selectedCards[player.uuid].Remove(card);
            }

            player.SetSelectedCards(selectedCards[player.uuid]);
            return true;
        }

        public override PromptProperties WaitingPrompt()
        {
            return new PromptProperties
            {
                menuTitle = "Waiting for opponent to finish selecting provinces"
            };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "change" || (!strongholdProvince.ContainsKey(player.uuid) || strongholdProvince[player.uuid] == null))
            {
                if (strongholdProvince.ContainsKey(player.uuid) && strongholdProvince[player.uuid] != null)
                {
                    strongholdProvince[player.uuid].inConflict = false;
                    strongholdProvince[player.uuid] = null;
                }
                selectableCards[player.uuid] = player.provinceDeck.ToList();
                selectedCards[player.uuid].Clear();
                return true;
            }

            if (arg != "done")
            {
                return false;
            }

            // Place stronghold province
            strongholdProvince[player.uuid].inConflict = false;
            if (!strongholdProvince[player.uuid].StartsGameFaceup())
            {
                strongholdProvince[player.uuid].facedown = true;
            }

            clickedDone[player.uuid] = true;
            game.AddMessage("{0} has placed their provinces", player);
            player.MoveCard(strongholdProvince[player.uuid], Locations.StrongholdProvince);

            // Place other provinces (selected + shuffled remaining)
            var remainingCards = selectableCards[player.uuid].ToList();
            remainingCards.Shuffle(); // Assuming there's a Shuffle extension method
            var provinces = selectedCards[player.uuid].Concat(remainingCards).ToList();

            for (int i = 1; i < 5; i++)
            {
                if (i - 1 < provinces.Count)
                {
                    var provinceCard = provinces[i - 1];
                    if (!provinceCard.StartsGameFaceup())
                    {
                        provinceCard.facedown = true;
                    }
                    player.MoveCard(provinceCard, "province " + i.ToString());
                }
            }

            player.hideProvinceDeck = true;
            return true;
        }
    }
}