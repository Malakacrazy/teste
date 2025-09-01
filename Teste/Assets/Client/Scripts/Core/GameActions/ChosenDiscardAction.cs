using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player chooses cards to discard from hand
    /// </summary>
    [System.Serializable]
    public partial class ChosenDiscardAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to chosen discard
        /// </summary>
        [System.Serializable]
        public class ChosenDiscardProperties : PlayerActionProperties
        {
            public int amount = 1;
            
            public ChosenDiscardProperties() : base() { }
            
            public ChosenDiscardProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public ChosenDiscardAction() : base()
        {
            Initialize();
        }
        
        public ChosenDiscardAction(ChosenDiscardProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ChosenDiscardAction(System.Func<AbilityContext, ChosenDiscardProperties> factory) : base(factory)
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
            effectMessage = "make {0} discard {1} cards";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new ChosenDiscardProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is ChosenDiscardProperties discardProps)
                return discardProps;
                
            // Convert base properties to ChosenDiscardProperties
            return new ChosenDiscardProperties()
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
            return ("make {0} discard {1} cards", new object[] { properties.target, properties.amount });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Must have cards to discard and amount must be positive
            if (player.hand.Count == 0 || properties.amount == 0)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            foreach (var player in properties.target.OfType<Player>())
            {
                var amount = Mathf.Min(player.hand.Count, properties.amount);
                if (amount > 0)
                {
                    // Check if someone else is choosing for this player
                    if (context.choosingPlayerOverride != null && context.choosingPlayerOverride != player)
                    {
                        // Random selection by override player
                        var gameEvent = GetEvent(player, context, additionalProperties);
                        var shuffledHand = player.hand.OrderBy(x => UnityEngine.Random.value).Take(amount).ToList();
                        gameEvent.AddProperty("cards", shuffledHand);
                        events.Add(gameEvent);
                        return;
                    }
                    
                    // Player chooses their own cards
                    var cardText = amount == 1 ? "a card" : $"{amount} cards";
                    var controllerType = player == context.player ? Players.Self : Players.Opponent;
                    
                    context.game.PromptForSelect(player, new SelectCardPromptProperties
                    {
                        activePromptTitle = $"Choose {cardText} to discard",
                        context = context,
                        mode = TargetModes.Exactly,
                        numCards = amount,
                        ordered = true,
                        location = Locations.Hand,
                        controller = controllerType,
                        onSelect = (selectedPlayer, cards) =>
                        {
                            var gameEvent = GetEvent(selectedPlayer, context, additionalProperties);
                            gameEvent.AddProperty("cards", cards);
                            events.Add(gameEvent);
                            return true;
                        }
                    });
                }
            }
        }
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("cards", new List<DrawCard>());
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var cards = gameEvent.GetProperty("cards") as List<DrawCard>;
            
            if (player != null && cards?.Count > 0)
            {
                gameEvent.context.game.AddMessage("{0} discards {1}", player, cards);
                
                foreach (var card in cards)
                {
                    var destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
                    player.MoveCard(card, destination);
                }
                
                LogExecution("{0} discarded {1} chosen cards", player.name, cards.Count);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Player chooses specific amount to discard
        /// </summary>
        public static ChosenDiscardAction Amount(int amount, Player target = null)
        {
            var action = new ChosenDiscardAction(new ChosenDiscardProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Player chooses 1 card to discard
        /// </summary>
        public static ChosenDiscardAction One(Player target = null)
        {
            return Amount(1, target);
        }
        
        /// <summary>
        /// Opponent chooses cards to discard
        /// </summary>
        public static ChosenDiscardAction Opponent(int amount = 1)
        {
            var action = new ChosenDiscardAction(new ChosenDiscardProperties(amount));
            action.TargetOpponent();
            return action;
        }
        
        #endregion
    }
}