using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Returns cards from play to their owner's deck
    /// </summary>
    [System.Serializable]
    public partial class ReturnToDeckAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to returning to deck
        /// </summary>
        [System.Serializable]
        public class ReturnToDeckProperties : CardActionProperties
        {
            public bool bottom = false;
            public bool shuffle = false;
            
            public ReturnToDeckProperties() : base() { }
            
            public ReturnToDeckProperties(bool bottom = false, bool shuffle = false) : base()
            {
                this.bottom = bottom;
                this.shuffle = shuffle;
            }
        }
        
        #region Constructors
        
        public ReturnToDeckAction() : base()
        {
            Initialize();
        }
        
        public ReturnToDeckAction(ReturnToDeckProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ReturnToDeckAction(System.Func<AbilityContext, ReturnToDeckProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "returnToDeck";
            eventName = EventNames.OnCardLeavesPlay;
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
        public new ReturnToDeckProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is ReturnToDeckProperties deckProps)
                return deckProps;
                
            // Convert base properties to ReturnToDeckProperties
            return new ReturnToDeckProperties()
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
            var message = properties.shuffle ? "shuffling {0} into their deck" : "returning {0} to their deck";
            return (message, new object[] { properties.target });
        }
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.shuffle)
            {
                return ("shuffle {0} into their deck", new object[] { properties.target });
            }
            
            var position = properties.bottom ? "bottom" : "top";
            return ($"return {{0}} to the {position} of their deck", new object[] { properties.target });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is DrawCard card))
                return false;
                
            // Card must be in play area to return to deck
            if (card.location != Locations.PlayArea)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void UpdateEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var card = target as DrawCard;
            if (card != null)
            {
                var properties = GetProperties(context, additionalProperties);
                
                UpdateLeavesPlayEvent(gameEvent, card, context, additionalProperties);
                
                // Determine destination deck
                var destination = card.isDynasty ? Locations.DynastyDeck : Locations.ConflictDeck;
                gameEvent.SetProperty("destination", destination);
                
                // Set placement options
                var options = new Dictionary<string, object>();
                if (properties.bottom)
                    options["bottom"] = true;
                gameEvent.SetProperty("options", options);
                
                // Set shuffle flag if needed
                var targetList = properties.target;
                if (properties.shuffle && (targetList?.Count == 0 || card == targetList?.LastOrDefault()))
                {
                    gameEvent.SetProperty("shuffle", true);
                }
            }
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            LeavesPlayEventHandler(gameEvent, additionalProperties);
            
            // Handle shuffling after card is moved
            var shouldShuffle = gameEvent.GetProperty("shuffle", false);
            var destination = gameEvent.GetProperty("destination") as string;
            var card = gameEvent.GetProperty("card") as DrawCard;
            
            if (shouldShuffle && card != null)
            {
                if (destination == Locations.DynastyDeck)
                {
                    card.owner.ShuffleDynastyDeck();
                }
                else if (destination == Locations.ConflictDeck)
                {
                    card.owner.ShuffleConflictDeck();
                }
            }
        }
        
        /// <summary>
        /// Update event for cards leaving play to deck
        /// </summary>
        protected virtual void UpdateLeavesPlayEvent(GameEvent gameEvent, DrawCard card, AbilityContext context, GameActionProperties additionalProperties)
        {
            base.UpdateEvent(gameEvent, card, context, additionalProperties);
            
            gameEvent.AddProperty("isSacrifice", false);
            
            // Pre-resolution effect for snapshot
            gameEvent.SetPreResolutionEffect(() =>
            {
                gameEvent.AddProperty("cardStateWhenLeftPlay", card.CreateSnapshot());
            });
            
            // Create contingent events for attachments and fate
            gameEvent.SetCreateContingentEvents(() =>
            {
                var contingentEvents = new List<GameEvent>();
                
                // Handle attachments leaving play
                if (card.attachments?.Count > 0)
                {
                    foreach (var attachment in card.attachments.Where(a => a.location == Locations.PlayArea))
                    {
                        var attachmentEvent = GameActions.DiscardFromPlay()
                            .GetEvent(attachment, context.game.GetFrameworkContext());
                        attachmentEvent.order = gameEvent.order - 1;
                        
                        var previousCondition = attachmentEvent.GetCondition();
                        attachmentEvent.SetCondition(() => previousCondition() && attachment.parent == card);
                        attachmentEvent.SetProperty("isContingent", true);
                        contingentEvents.Add(attachmentEvent);
                    }
                }
                
                // Handle fate removal
                if (card.fate > 0)
                {
                    var fateEvent = GameActions.RemoveFate(new RemoveFateAction.RemoveFateProperties { amount = card.fate })
                        .GetEvent(card, context.game.GetFrameworkContext());
                    fateEvent.order = gameEvent.order - 1;
                    fateEvent.SetProperty("isContingent", true);
                    contingentEvents.Add(fateEvent);
                }
                
                return contingentEvents;
            });
        }
        
        /// <summary>
        /// Handle leaves play event for returning to deck
        /// </summary>
        protected virtual void LeavesPlayEventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as DrawCard;
            var destination = gameEvent.GetProperty("destination") as string;
            
            CheckForRefillProvince(card, gameEvent, additionalProperties);
            
            if (!card.owner.IsLegalLocationForCard(card, destination))
            {
                gameEvent.context.game.AddMessage("{0} is not a legal location for {1} and it is discarded", 
                    destination, card);
                destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
            }
            
            var options = gameEvent.GetProperty("options") as Dictionary<string, object>;
            card.owner.MoveCard(card, destination, options);
            
            var bottom = options?.ContainsKey("bottom") == true;
            LogExecution("Returned {0} to {1} of deck", card.name, bottom ? "bottom" : "top");
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Return card to top of deck
        /// </summary>
        public static ReturnToDeckAction ToTop(DrawCard card = null)
        {
            var action = new ReturnToDeckAction(new ReturnToDeckProperties(false, false));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Return card to bottom of deck
        /// </summary>
        public static ReturnToDeckAction ToBottom(DrawCard card = null)
        {
            var action = new ReturnToDeckAction(new ReturnToDeckProperties(true, false));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Shuffle card into deck
        /// </summary>
        public static ReturnToDeckAction Shuffle(DrawCard card = null)
        {
            var action = new ReturnToDeckAction(new ReturnToDeckProperties(false, true));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Return source card to deck
        /// </summary>
        public static ReturnToDeckAction Self(bool bottom = false, bool shuffle = false)
        {
            var action = new ReturnToDeckAction(new ReturnToDeckProperties(bottom, shuffle));
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        #endregion
    }
}