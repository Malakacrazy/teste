using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Handles cost reduction logic for cards in the game.
    /// Supports conditional reductions, usage limits, and type filtering.
    /// </summary>
    public class CostReducer
    {
        private Game game;
        private Card source;
        private int uses;
        private UsageLimit limit;
        private string cardType;
        private Func<Card, Card, bool> match;
        private Func<object, Card, bool> targetCondition;
        private object amountValue;
        private List<string> playingTypes;

        /// <summary>
        /// Creates a new cost reducer with the specified properties.
        /// </summary>
        /// <param name="game">The game instance</param>
        /// <param name="source">The card that is the source of this cost reduction</param>
        /// <param name="properties">Properties dictionary containing configuration</param>
        public CostReducer(Game game, Card source, CostReducerProperties properties)
        {
            this.game = game;
            this.source = source;
            this.uses = 0;
            this.limit = properties.Limit;
            this.cardType = properties.CardType;
            this.match = properties.Match ?? ((card, src) => true);
            this.targetCondition = properties.TargetCondition;
            this.amountValue = properties.Amount ?? (object)1;

            // Handle playingTypes - can be single string or list
            if (properties.PlayingTypes != null)
            {
                if (properties.PlayingTypes is string singleType)
                {
                    this.playingTypes = new List<string> { singleType };
                }
                else if (properties.PlayingTypes is List<string> typeList)
                {
                    this.playingTypes = typeList;
                }
            }

            if (this.limit != null)
            {
                this.limit.RegisterEvents(game);
            }
        }

        /// <summary>
        /// Determines if this reducer can reduce the cost for the given card and conditions.
        /// </summary>
        /// <param name="playingType">The type of play action (e.g., "play", "marshal")</param>
        /// <param name="card">The card being played</param>
        /// <param name="target">Optional target for the card</param>
        /// <param name="ignoreType">Whether to ignore card type checking</param>
        /// <returns>True if cost can be reduced</returns>
        public bool CanReduce(string playingType, Card card, object target = null, bool ignoreType = false)
        {
            if (limit != null && limit.IsAtMax(source.Controller))
            {
                return false;
            }
            else if (!ignoreType && !string.IsNullOrEmpty(cardType) && card.GetCardType() != cardType)
            {
                return false;
            }
            else if (playingTypes != null && !playingTypes.Contains(playingType))
            {
                return false;
            }

            return match(card, source) && CheckTargetCondition(target);
        }

        /// <summary>
        /// Checks if the target meets the required condition.
        /// </summary>
        private bool CheckTargetCondition(object target)
        {
            if (targetCondition == null)
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            return targetCondition(target, source);
        }

        /// <summary>
        /// Gets the amount to reduce the cost by.
        /// </summary>
        /// <param name="card">The card being played</param>
        /// <param name="player">The player playing the card</param>
        /// <returns>The cost reduction amount</returns>
        public int GetAmount(Card card, Player player)
        {
            if (amountValue is Func<Card, Player, int> amountFunc)
            {
                return amountFunc(card, player);
            }

            return Convert.ToInt32(amountValue);
        }

        /// <summary>
        /// Marks this reducer as having been used (increments usage limit if present).
        /// </summary>
        public void MarkUsed()
        {
            if (limit != null)
            {
                limit.Increment(source.Controller);
            }
        }

        /// <summary>
        /// Checks if this reducer has expired (reached max usage and is not repeatable).
        /// </summary>
        /// <returns>True if the reducer has expired</returns>
        public bool IsExpired()
        {
            return limit != null && limit.IsAtMax(source.Controller) && !limit.IsRepeatable();
        }

        /// <summary>
        /// Unregisters any events this reducer has registered.
        /// </summary>
        public void UnregisterEvents()
        {
            if (limit != null)
            {
                limit.UnregisterEvents(game);
            }
        }
    }

    /// <summary>
    /// Properties for configuring a CostReducer.
    /// </summary>
    [Serializable]
    public class CostReducerProperties
    {
        public UsageLimit Limit { get; set; }
        public string CardType { get; set; }
        public Func<Card, Card, bool> Match { get; set; }
        public Func<object, Card, bool> TargetCondition { get; set; }
        public object Amount { get; set; }
        public object PlayingTypes { get; set; } // Can be string or List<string>
    }
}