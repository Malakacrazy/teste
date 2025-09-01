using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player discards cards at random from hand
    /// </summary>
    [System.Serializable]
    public partial class RandomDiscardAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to random discard
        /// </summary>
        [System.Serializable]
        public class RandomDiscardProperties : PlayerActionProperties
        {
            public int amount = 1;
            
            public RandomDiscardProperties() : base() { }
            
            public RandomDiscardProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public RandomDiscardAction() : base()
        {
            Initialize();
        }
        
        public RandomDiscardAction(RandomDiscardProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public RandomDiscardAction(System.Func<AbilityContext, RandomDiscardProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "discard";
            eventName = EventNames.OnCardsDiscardedFromHand;
            effectMessage = "make {0} discard {1} cards at random";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new RandomDiscardProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is RandomDiscardProperties discardProps)
                return discardProps;
                
            // Convert base properties to RandomDiscardProperties
            return new RandomDiscardProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                amount = 1
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("make {0} discard {1} cards at random", new object[] { properties.target, properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Must have cards to discard and amount must be positive
            if (properties.amount <= 0 || player.hand.Count == 0)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("amount", properties.amount);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var requestedAmount = gameEvent.GetProperty("amount", 1);
            
            if (player == null)
                return false;
                
            var amount = Mathf.Min(requestedAmount, player.hand.Count);
            if (amount == 0)
                return false;
                
            // Shuffle hand and take random cards
            var shuffledHand = player.hand.OrderBy(x => UnityEngine.Random.value).ToList();
            var cardsToDiscard = shuffledHand.Take(amount).ToList();
            
            gameEvent.context.game.AddMessage("{0} discards {1} at random", player, cardsToDiscard);
            
            if (amount == 1)
            {
                // Single card discard - just move it
                var card = cardsToDiscard[0];
                var destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
                player.MoveCard(card, destination);
            }
            else
            {
                // Multiple cards - let player choose order for discard
                var handler = new System.Action<Player, List<DrawCard>>((p, cards) =>
                {
                    // If no specific order chosen, use all cards to discard
                    if (cards == null || cards.Count == 0)
                        cards = cardsToDiscard;
                    else
                        cards = cards.Concat(cardsToDiscard.Where(c => !cards.Contains(c))).ToList();
                        
                    foreach (var card in cards)
                    {
                        var destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
                        p.MoveCard(card, destination);
                    }
                });
                
                gameEvent.context.game.PromptForSelect(player, new SelectCardPromptProperties
                {
                    activePromptTitle = "Choose order for random discard",
                    mode = TargetModes.UpTo,
                    numCards = cardsToDiscard.Count,
                    optional = true,
                    ordered = true,
                    location = Locations.Hand,
                    controller = Players.Self,
                    source = gameEvent.context.source,
                    cardCondition = card => cardsToDiscard.Contains(card),
                    onSelect = handler,
                    onCancel = () => handler(player, null)
                });
            }
            
            LogExecution("{0} discarded {1} cards at random", player.name, amount);
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Player discards specific amount at random
        /// </summary>
        public static RandomDiscardAction Amount(int amount, Player target = null)
        {
            var action = new RandomDiscardAction(new RandomDiscardProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Player discards 1 card at random
        /// </summary>
        public static RandomDiscardAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Opponent discards cards at random
        /// </summary>
        public static RandomDiscardAction Opponent(int amount = 1)
        {
            var action = new RandomDiscardAction(new RandomDiscardProperties(amount));
            action.TargetOpponent();
            return action;
        }
        
        #endregion
    }
}