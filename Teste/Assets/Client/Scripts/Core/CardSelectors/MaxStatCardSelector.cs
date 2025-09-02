using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Selector for cards based on stat limits and maximum stat sums.
    /// Perfect C# port of the original JavaScript MaxStatCardSelector.
    /// </summary>
    [Serializable]
    public class MaxStatCardSelector : BaseCardSelector
    {
        [Header("Max Stat Configuration")]
        public Func<BaseCard, int> cardStat;
        public Func<int> maxStat;
        public int maxCards;
        
        public MaxStatCardSelector(CardSelectorProperties properties) : base(properties)
        {
            cardStat = properties.cardStat;
            maxStat = properties.maxStat != null ? () => int.Parse(properties.maxStat) : null;
            maxCards = properties.numCards;
            this.numCards = properties.numCards;
            
            // If maxStat string is provided, try to create appropriate function
            if (!string.IsNullOrEmpty(properties.maxStat) && cardStat == null)
            {
                cardStat = GetStatFunction(properties.maxStat);
            }
        }
        
        /// <summary>
        /// Constructor with explicit functions
        /// </summary>
        public MaxStatCardSelector(CardSelectorProperties properties, Func<BaseCard, int> cardStatFunc, Func<int> maxStatFunc)
            : base(properties)
        {
            cardStat = cardStatFunc;
            maxStat = maxStatFunc;
            maxCards = properties.numCards;
            this.numCards = properties.numCards;
        }
        
        /// <summary>
        /// Get the function to extract the stat value from a card
        /// </summary>
        private Func<BaseCard, int> GetStatFunction(string stat)
        {
            if (string.IsNullOrEmpty(stat))
                return card => 0;
            
            switch (stat.ToLower())
            {
                case "military":
                case "militaryskill":
                    return card => card.GetMilitarySkill();
                case "political":
                case "politicalskill":
                    return card => card.GetPoliticalSkill();
                case "glory":
                    return card => card.GetGlory();
                case "cost":
                case "fate":
                    return card => card.GetCost();
                case "strength":
                case "provincestrength":
                    return card => card.GetStrength();
                default:
                    Debug.LogWarning($"⚠️ Unknown stat type for MaxStatCardSelector: {stat}");
                    return card => 0;
            }
        }
        
        public override bool CanTarget(BaseCard card, AbilityContext context, Player choosingPlayer, List<BaseCard> selectedCards = null)
        {
            selectedCards ??= new List<BaseCard>();
            
            if (!base.CanTarget(card, context, choosingPlayer, selectedCards))
                return false;
            
            if (cardStat == null || maxStat == null)
                return true;
            
            return cardStat(card) <= maxStat();
        }
        
        public override bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card)
        {
            if (cardStat == null || maxStat == null)
                return false;
            
            selectedCards ??= new List<BaseCard>();
            
            int currentStatSum = selectedCards.Sum(c => cardStat(c));
            return cardStat(card) + currentStatSum > maxStat();
        }
        
        public override bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            selectedCards ??= new List<BaseCard>();
            return maxCards > 0 && selectedCards.Count >= maxCards;
        }
        
        public override bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            selectedCards ??= new List<BaseCard>();
            
            bool statExceeded = false;
            if (cardStat != null && maxStat != null)
            {
                int currentStatSum = selectedCards.Sum(c => cardStat(c));
                statExceeded = currentStatSum > maxStat();
            }
            
            bool countExceeded = maxCards > 0 && selectedCards.Count > maxCards;
            
            return statExceeded || countExceeded;
        }
        
        public override string DefaultActivePromptTitle(AbilityContext context = null)
        {
            return activePromptTitle ?? "Select cards within stat limit";
        }
        
        public override bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return false; // Let player confirm selection
        }
        
        public override object FormatSelectParam(List<BaseCard> selectedCards)
        {
            return selectedCards ?? new List<BaseCard>();
        }
        
        public override bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            var validTargets = GetAllLegalTargets(context, choosingPlayer);
            return validTargets.Count > 0;
        }
        
        public override bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return optional || (selectedCards?.Count ?? 0) > 0;
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new
            {
                baseInfo,
                maxCards,
                hasCardStat = cardStat != null,
                hasMaxStat = maxStat != null,
                currentMaxStat = maxStat?.Invoke() ?? -1
            };
        }
    }
}
