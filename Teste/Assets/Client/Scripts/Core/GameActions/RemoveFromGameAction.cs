using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Removes cards from the game entirely
    /// </summary>
    [System.Serializable]
    public partial class RemoveFromGameAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to removing from game
        /// </summary>
        [System.Serializable]
        public class RemoveFromGameProperties : CardActionProperties
        {
            public string location;
            
            public RemoveFromGameProperties() : base() { }
            
            public RemoveFromGameProperties(string location) : base()
            {
                this.location = location;
            }
        }
        
        #region Constructors
        
        public RemoveFromGameAction() : base()
        {
            Initialize();
        }
        
        public RemoveFromGameAction(RemoveFromGameProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public RemoveFromGameAction(System.Func<AbilityContext, RemoveFromGameProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "removeFromGame";
            eventName = EventNames.OnCardLeavesPlay;
            costMessage = "removing {0} from the game";
            effectMessage = "remove {0} from the game";
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Holding 
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new RemoveFromGameProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is RemoveFromGameProperties removeProps)
                return removeProps;
                
            // Convert base properties to RemoveFromGameProperties
            return new RemoveFromGameProperties()
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
            if (!(target is BaseCard card))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // If specific location is specified, card must be in that location
            if (!string.IsNullOrEmpty(properties.location))
            {
                if (properties.location != card.location)
                    return false;
            }
            else
            {
                // Default location rules
                // Holdings must be in province locations
                if (card.type == CardTypes.Holding)
                {
                    if (!card.location.Contains("province"))
                        return false;
                }
                // Other cards must be in play area
                else if (card.location != Locations.PlayArea)
                {
                    return false;
                }
            }
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void UpdateEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var card = target as BaseCard;
            if (card != null)
            {
                UpdateLeavesPlayEvent(gameEvent, card, context, additionalProperties);
                gameEvent.SetProperty("destination", Locations.RemovedFromGame);
            }
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            LeavesPlayEventHandler(gameEvent, additionalProperties);
        }
        
        /// <summary>
        /// Update event for cards leaving play to removed from game
        /// </summary>
        protected virtual void UpdateLeavesPlayEvent(GameEvent gameEvent, BaseCard card, AbilityContext context, GameActionProperties additionalProperties)
        {
            base.UpdateEvent(gameEvent, card, context, additionalProperties);
            
            gameEvent.AddProperty("isSacrifice", false);
            gameEvent.AddProperty("destination", Locations.RemovedFromGame);
            
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
        /// Handle leaves play event for removing from game
        /// </summary>
        protected virtual void LeavesPlayEventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            var destination = Locations.RemovedFromGame;
            
            CheckForRefillProvince(card, gameEvent, additionalProperties);
            
            var options = gameEvent.GetProperty("options") as Dictionary<string, object>;
            card.owner.MoveCard(card, destination, options);
            
            LogExecution("Removed {0} from the game", card.name);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to remove specific card from game
        /// </summary>
        public static RemoveFromGameAction Card(BaseCard card)
        {
            var action = new RemoveFromGameAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to remove source card from game
        /// </summary>
        public static RemoveFromGameAction Self()
        {
            var action = new RemoveFromGameAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        /// <summary>
        /// Create action to remove card from specific location
        /// </summary>
        public static RemoveFromGameAction FromLocation(string location, BaseCard card = null)
        {
            var action = new RemoveFromGameAction(new RemoveFromGameProperties(location));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to remove all attachments of a card from game
        /// </summary>
        public static RemoveFromGameAction Attachments(BaseCard parentCard)
        {
            var action = new RemoveFromGameAction();
            action.SetDefaultTarget(context => 
                parentCard.attachments?.Where(a => a.location == Locations.PlayArea).ToList() ?? new List<object>());
            return action;
        }
        
        #endregion
    }
}