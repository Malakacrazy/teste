using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Moves a card to a different location
    /// </summary>
    [System.Serializable]
    public partial class MoveCardAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to moving cards
        /// </summary>
        [System.Serializable]
        public class MoveCardProperties : CardActionProperties
        {
            public string destination;
            public bool switchCard = false;
            public bool shuffle = false;
            public bool faceup = false;
            public bool bottom = false;
            public bool changePlayer = false;
            
            public MoveCardProperties() : base() { }
            
            public MoveCardProperties(string destination) : base()
            {
                this.destination = destination;
            }
        }
        
        #region Constructors
        
        public MoveCardAction() : base()
        {
            Initialize();
        }
        
        public MoveCardAction(MoveCardProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public MoveCardAction(System.Func<AbilityContext, MoveCardProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        public MoveCardAction(BaseCard card, string destination) : base(new MoveCardProperties { destination = destination })
        {
            if (card != null)
                GetProperties(null).target.Add(card);
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "move";
            eventName = EventNames.OnCardMoved;
            effectMessage = "move {0}";
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Event, 
                CardTypes.Holding 
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new MoveCardProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is MoveCardProperties moveProps)
                return moveProps;
                
            // Convert base properties to MoveCardProperties
            return new MoveCardProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("shuffling {0} into their deck", new object[] { properties.target });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Check take control restrictions if changing player
            if (properties.changePlayer)
            {
                if (!card.CheckRestrictions(EffectNames.TakeControl, context))
                    return false;
                    
                // Check for unique conflicts
                if (card.IsUnique() && card.AnotherUniqueInPlay(context.player))
                    return false;
            }
            
            // Cannot move cards already in play area
            if (card.location == Locations.PlayArea)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            var card = target as BaseCard;
            if (card != null)
            {
                gameEvent.AddProperty("cardStateWhenMoved", card.CreateSnapshot());
            }
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var context = gameEvent.context;
            var card = gameEvent.GetProperty("card") as BaseCard;
            var properties = GetProperties(context, additionalProperties);
            
            if (card == null || string.IsNullOrEmpty(properties.destination))
                return false;
            
            // Handle card switching
            if (properties.switchCard)
            {
                var otherCard = card.controller.GetDynastyCardInProvince(properties.destination);
                if (otherCard != null)
                {
                    card.owner.MoveCard(otherCard, card.location);
                }
            }
            else
            {
                CheckForRefillProvince(card, gameEvent, additionalProperties);
            }
            
            // Determine target player
            var targetPlayer = properties.changePlayer && card.controller.opponent != null ? 
                card.controller.opponent : card.controller;
            
            // Move the card
            var moveOptions = new Dictionary<string, object>();
            if (properties.bottom)
                moveOptions["placement"] = "bottom";
                
            targetPlayer.MoveCard(card, properties.destination, new CardMoveOptions(moveOptions));
            
            // Handle shuffling
            var targetList = properties.target as List<object>;
            if (properties.shuffle && (targetList?.Count == 0 || card == targetList?.LastOrDefault()))
            {
                if (properties.destination == Locations.ConflictDeck)
                {
                    card.owner.ShuffleConflictDeck();
                }
                else if (properties.destination == Locations.DynastyDeck)
                {
                    card.owner.ShuffleDynastyDeck();
                }
            }
            
            // Handle face up/down
            if (properties.faceup)
            {
                card.facedown = false;
            }
            
            // Check for illegal attachments
            card.CheckForIllegalAttachments();
            
            LogExecution("Moved {0} to {1}", card.name, properties.destination);
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Move card to hand
        /// </summary>
        public static MoveCardAction ToHand(BaseCard card = null)
        {
            var action = new MoveCardAction(new MoveCardProperties(Locations.Hand));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Move card to top of deck
        /// </summary>
        public static MoveCardAction ToDeckTop(BaseCard card = null)
        {
            var action = new MoveCardAction(new MoveCardProperties(Locations.ConflictDeck));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Move card to bottom of deck
        /// </summary>
        public static MoveCardAction ToDeckBottom(BaseCard card = null)
        {
            var action = new MoveCardAction(new MoveCardProperties(Locations.ConflictDeck) { bottom = true });
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Shuffle card into deck
        /// </summary>
        public static MoveCardAction ShuffleIntoDeck(BaseCard card = null)
        {
            var action = new MoveCardAction(new MoveCardProperties(Locations.ConflictDeck) { shuffle = true });
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Move card to discard pile
        /// </summary>
        public static MoveCardAction ToDiscardPile(BaseCard card = null)
        {
            var action = new MoveCardAction(new MoveCardProperties(Locations.ConflictDiscardPile));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Switch two dynasty cards
        /// </summary>
        public static MoveCardAction SwitchDynastyCards(BaseCard card, string targetProvince)
        {
            var action = new MoveCardAction(new MoveCardProperties(targetProvince) { switchCard = true });
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        #endregion
    }
}