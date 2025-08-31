using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Returns cards from play to their owner's hand
    /// </summary>
    [System.Serializable]
    public partial class ReturnToHandAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to returning to hand
        /// </summary>
        [System.Serializable]
        public class ReturnToHandProperties : CardActionProperties
        {
            public ReturnToHandProperties() : base() { }
        }
        
        #region Constructors
        
        public ReturnToHandAction() : base()
        {
            Initialize();
        }
        
        public ReturnToHandAction(ReturnToHandProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ReturnToHandAction(System.Func<AbilityContext, ReturnToHandProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "returnToHand";
            eventName = EventNames.OnCardLeavesPlay;
            effectMessage = "return {0} to their hand";
            costMessage = "returning {0} to their hand";
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Event 
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new ReturnToHandProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is ReturnToHandProperties returnProps)
                return returnProps;
                
            // Convert base properties to ReturnToHandProperties
            return new ReturnToHandProperties()
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
                
            // Card must be in play area to return to hand
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
                UpdateLeavesPlayEvent(gameEvent, card, context, additionalProperties);
                gameEvent.SetProperty("destination", Locations.Hand);
            }
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            LeavesPlayEventHandler(gameEvent, additionalProperties);
        }
        
        /// <summary>
        /// Update event for cards leaving play to hand
        /// </summary>
        protected virtual void UpdateLeavesPlayEvent(GameEvent gameEvent, DrawCard card, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.UpdateEvent(gameEvent, card, context, additionalProperties);
            
            gameEvent.AddProperty("isSacrifice", false);
            gameEvent.AddProperty("destination", Locations.Hand);
            
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
        /// Handle leaves play event for returning to hand
        /// </summary>
        protected virtual void LeavesPlayEventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as DrawCard;
            var destination = Locations.Hand;
            
            CheckForRefillProvince(card, gameEvent, additionalProperties);
            
            if (!card.owner.IsLegalLocationForCard(card, destination))
            {
                gameEvent.context.game.AddMessage("{0} is not a legal location for {1} and it is discarded", 
                    destination, card);
                destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
            }
            
            var options = gameEvent.GetProperty("options") as Dictionary<string, object>;
            card.owner.MoveCard(card, destination, options);
            
            LogExecution("Returned {0} to hand", card.name);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to return specific card to hand
        /// </summary>
        public static ReturnToHandAction Card(DrawCard card)
        {
            var action = new ReturnToHandAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to return source card to hand
        /// </summary>
        public static ReturnToHandAction Self()
        {
            var action = new ReturnToHandAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        /// <summary>
        /// Create action to return all attachments of a card to hand
        /// </summary>
        public static ReturnToHandAction Attachments(BaseCard parentCard)
        {
            var action = new ReturnToHandAction();
            action.SetDefaultTarget(context => 
                parentCard.attachments?.Where(a => a.location == Locations.PlayArea).ToList() ?? new List<object>());
            return action;
        }
        
        /// <summary>
        /// Create action to return lowest cost character to hand
        /// </summary>
        public static ReturnToHandAction LowestCostCharacter(Player targetPlayer = null)
        {
            var action = new ReturnToHandAction();
            action.SetDefaultTarget(context =>
            {
                var player = targetPlayer ?? context.player.opponent;
                var eligibleCharacters = player.cardsInPlay
                    .Where(c => c.type == CardTypes.Character)
                    .OrderBy(c => c.cost)
                    .ToList();
                    
                return eligibleCharacters.Any() ? new List<object> { eligibleCharacters.First() } : new List<object>();
            });
            return action;
        }
        
        #endregion
    }
}