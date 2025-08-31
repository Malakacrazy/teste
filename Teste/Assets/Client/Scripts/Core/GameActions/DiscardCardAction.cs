using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Discards cards from hand or other locations
    /// </summary>
    [System.Serializable]
    public partial class DiscardCardAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to discarding cards
        /// </summary>
        [System.Serializable]
        public class DiscardCardProperties : CardActionProperties
        {
            public DiscardCardProperties() : base() { }
        }
        
        #region Constructors
        
        public DiscardCardAction() : base()
        {
            Initialize();
        }
        
        public DiscardCardAction(DiscardCardProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DiscardCardAction(System.Func<AbilityContext, DiscardCardProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "discardCard";
            eventName = EventNames.OnCardsDiscarded;
            costMessage = "discarding {0}";
            effectMessage = "discard {0}";
            targetTypes = new List<string> 
            { 
                CardTypes.Attachment, 
                CardTypes.Character, 
                CardTypes.Event, 
                CardTypes.Holding 
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new DiscardCardProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is DiscardCardProperties discardProps)
                return discardProps;
                
            // Convert base properties to DiscardCardProperties
            return new DiscardCardProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is DrawCard card))
                return false;
                
            // Check discard restrictions for cards in hand
            if (card.location == Locations.Hand && !card.controller.CheckRestrictions("discard", context))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var cards = properties.target.OfType<DrawCard>()
                .Where(card => CanAffect(card, context, additionalProperties))
                .ToList();
                
            if (cards.Count == 0)
                return;
                
            var gameEvent = CreateEvent(null, context, additionalProperties);
            UpdateEvent(gameEvent, cards, context, additionalProperties);
            events.Add(gameEvent);
        }
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var cards = target as List<DrawCard>;
            if (cards == null)
            {
                var properties = GetProperties(context, additionalProperties);
                cards = properties.target.OfType<DrawCard>().ToList();
            }
            
            gameEvent.AddProperty("cards", cards);
            gameEvent.context = context;
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var cards = gameEvent.GetProperty("cards") as List<DrawCard>;
            
            if (cards?.Count > 0)
            {
                foreach (var card in cards)
                {
                    CheckForRefillProvince(card, gameEvent, additionalProperties);
                    
                    var destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
                    card.controller.MoveCard(card, destination);
                }
                
                LogExecution("Discarded {0} cards", cards.Count);
            }
        }
        
        protected override bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return !gameEvent.IsCancelled() && gameEvent.name == eventName;
        }
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Discard specific cards
        /// </summary>
        public static DiscardCardAction Cards(params DrawCard[] cards)
        {
            var action = new DiscardCardAction();
            action.SetDefaultTarget(context => cards.ToList());
            return action;
        }
        
        /// <summary>
        /// Discard cards from hand
        /// </summary>
        public static DiscardCardAction FromHand(Player player, int amount = 1)
        {
            var action = new DiscardCardAction();
            action.SetDefaultTarget(context => 
                player.hand.Take(amount).ToList());
            return action;
        }
        
        /// <summary>
        /// Discard random cards from hand
        /// </summary>
        public static DiscardCardAction RandomFromHand(Player player, int amount = 1)
        {
            var action = new DiscardCardAction();
            action.SetDefaultTarget(context => 
                player.hand.OrderBy(x => UnityEngine.Random.value).Take(amount).ToList());
            return action;
        }
        
        #endregion
    }
}