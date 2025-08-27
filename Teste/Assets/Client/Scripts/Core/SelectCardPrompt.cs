using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Card selection prompt
    /// </summary>
    public partial class SelectCardPrompt : IGameStep
    {
        private Game game;
        private Player player;
        private SelectCardPromptProperties properties;
        private List<BaseCard> selectedCards = new List<BaseCard>();
        
        public SelectCardPrompt(Game gameInstance, Player promptPlayer, SelectCardPromptProperties props)
        {
            game = gameInstance;
            player = promptPlayer;
            properties = props;
        }
        
        public bool Execute()
        {
            return Continue();
        }
        
        public bool IsComplete()
        {
            return selectedCards.Count >= properties.numCards;
        }
        
        public bool Continue()
        {
            game.AddMessage("Waiting for {0} to select cards...", player);
            
            // Set selectable cards based on condition
            var selectableCards = new List<BaseCard>();
            
            // Find cards that match the condition
            foreach (var gamePlayer in game.GetPlayers())
            {
                var allCards = new List<BaseCard>();
                allCards.AddRange(gamePlayer.cardsInPlay);
                allCards.AddRange(gamePlayer.hand);
                allCards.AddRange(gamePlayer.provinceOne);
                allCards.AddRange(gamePlayer.provinceTwo);
                allCards.AddRange(gamePlayer.provinceThree);
                allCards.AddRange(gamePlayer.provinceFour);
                
                foreach (var card in allCards)
                {
                    if (properties.cardCondition?.Invoke(card) ?? true)
                    {
                        selectableCards.Add(card);
                    }
                }
            }
            
            player.SetSelectableCards(selectableCards);
            return false;
        }
        
        public void OnCardClicked(Player clickingPlayer, BaseCard card)
        {
            if (clickingPlayer != player) return;
            
            if (properties.cardCondition?.Invoke(card) ?? true)
            {
                selectedCards.Add(card);
                
                if (selectedCards.Count >= properties.numCards)
                {
                    // Selection complete
                    if (properties.onSelect != null)
                    {
                        bool success = true;
                        foreach (var selectedCard in selectedCards)
                        {
                            success = success && properties.onSelect(player, selectedCard);
                        }
                        
                        if (success)
                        {
                            player.ClearSelectableCards();
                            game.pipeline.Continue();
                        }
                    }
                }
            }
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (command == "pass" || command == "done")
            {
                if (properties.optional || selectedCards.Count > 0)
                {
                    player.ClearSelectableCards();
                    game.pipeline.Continue();
                }
            }
        }
        
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() 
        {
            player?.ClearSelectableCards();
        }
    }
}
