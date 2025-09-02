using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for exactly X number of cards.
    /// Players must select the exact number specified.
    /// Perfect C# port of the original JavaScript ExactlyXCardSelector.
    /// </summary>
    [Serializable]
    public class ExactlyXCardSelector : BaseCardSelector
    {
        [Header("Exactly X Configuration")]
        public int exactCards;
        
        public ExactlyXCardSelector(int numCards, CardSelectorProperties properties) 
            : base(properties)
        {
            exactCards = numCards;
            this.numCards = numCards;
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            // Match JavaScript logic exactly
            if (cardType.Count == 1)
            {
                string cardTypeName = cardType[0];
                return exactCards == 1 
                    ? $"Choose a {cardTypeName}" 
                    : $"Choose {exactCards} {cardTypeName}";
            }
            
            return exactCards == 1 
                ? "Select a card" 
                : $"Select {exactCards} cards";
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) == exactCards;
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            var matchedCards = new List<BaseCard>();
            int numMatchingCards = context.game.GetAllCards().Count(card => 
            {
                if (CanTarget(card, context, choosingPlayer, matchedCards))
                {
                    matchedCards.Add(card);
                    return true;
                }
                return false;
            });
            
            return numMatchingCards >= exactCards;
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) >= exactCards;
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return exactCards == 1;
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            if (selectedCards?.Contains(card) == true)
                return false; // Deselecting doesn't exceed limit
            
            return (selectedCards?.Count ?? 0) >= exactCards;
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            if (exactCards == 1)
                return selectedCards?.FirstOrDefault();
            
            return selectedCards ?? new List<BaseCard>();
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return (selectedCards?.Count ?? 0) > exactCards;
        }
        
        public override int GetMinimumRequired()
        {
            return exactCards;
        }
        
        public override int GetMaximumAllowed()
        {
            return exactCards;
        }
    }
}
