using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Discards cards from play (or sacrifices them)
    /// </summary>
    [System.Serializable]
    public partial class DiscardFromPlayAction : CardGameAction
    {
        [Header("Discard From Play Configuration")]
        [SerializeField] private bool isSacrifice = false;
        
        /// <summary>
        /// Properties specific to discarding from play
        /// </summary>
        [System.Serializable]
        public class DiscardFromPlayProperties : CardActionProperties
        {
            public bool isSacrifice = false;
            
            public DiscardFromPlayProperties() : base() { }
            
            public DiscardFromPlayProperties(bool isSacrifice) : base()
            {
                this.isSacrifice = isSacrifice;
            }
        }
        
        #region Constructors
        
        public DiscardFromPlayAction() : base()
        {
            Initialize();
        }
        
        public DiscardFromPlayAction(bool isSacrifice = false) : base()
        {
            this.isSacrifice = isSacrifice;
            Initialize();
        }
        
        public DiscardFromPlayAction(DiscardFromPlayProperties properties) : base(properties)
        {
            this.isSacrifice = properties.isSacrifice;
            Initialize();
        }
        
        public DiscardFromPlayAction(System.Func<AbilityContext, DiscardFromPlayProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            
            if (isSacrifice)
            {
                actionName = "sacrifice";
                costMessage = "sacrificing {0}";
                effectMessage = "sacrifice {0}";
            }
            else
            {
                actionName = "discardFromPlay";
                costMessage = "discarding {0}";
                effectMessage = "discard {0}";
            }
            
            eventName = EventNames.OnCardLeavesPlay;
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
        public new DiscardFromPlayProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is DiscardFromPlayProperties discardProps)
                return discardProps;
                
            // Convert base properties to DiscardFromPlayProperties
            return new DiscardFromPlayProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                isSacrifice = this.isSacrifice
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var message = properties.isSacrifice ? "sacrifice {0}" : "discard {0}";
            return (message, new object[] { properties.target });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
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
            }
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            LeavesPlayEventHandler(gameEvent, additionalProperties);
        }
        
        /// <summary>
        /// Update event for cards leaving play
        /// </summary>
        protected virtual void UpdateLeavesPlayEvent(GameEvent gameEvent, BaseCard card, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.UpdateEvent(gameEvent, card, context, additionalProperties);
            
            gameEvent.AddProperty("isSacrifice", properties.isSacrifice);
            
            var destination = card.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile;
            gameEvent.AddProperty("destination", destination);
            
            // Pre-resolution effect for ancestral and snapshot
            gameEvent.SetPreResolutionEffect(() =>
            {
                gameEvent.AddProperty("cardStateWhenLeftPlay", card.CreateSnapshot());
                
                if (card.IsAncestral() && gameEvent.GetProperty("isContingent", false))
                {
                    gameEvent.SetProperty("destination", Locations.Hand);
                    context.game.AddMessage("{0} returns to {1}'s hand due to its Ancestral keyword", 
                        card, card.owner);
                }
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
        /// Handle leaves play event
        /// </summary>
        protected virtual void LeavesPlayEventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
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
            
            var isSacrifice = gameEvent.GetProperty("isSacrifice", false);
            LogExecution("{0} {1}", isSacrifice ? "Sacrificed" : "Discarded", card.name);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set this action as a sacrifice
        /// </summary>
        public void SetSacrifice(bool sacrifice)
        {
            isSacrifice = sacrifice;
            Initialize(); // Reinitialize with new sacrifice status
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to discard card from play
        /// </summary>
        public static DiscardFromPlayAction Card(BaseCard card)
        {
            var action = new DiscardFromPlayAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to sacrifice card
        /// </summary>
        public static DiscardFromPlayAction Sacrifice(BaseCard card = null)
        {
            var action = new DiscardFromPlayAction(true);
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to discard source card
        /// </summary>
        public static DiscardFromPlayAction Self()
        {
            var action = new DiscardFromPlayAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        /// <summary>
        /// Create action to sacrifice source card
        /// </summary>
        public static DiscardFromPlayAction SacrificeeSelf()
        {
            var action = new DiscardFromPlayAction(true);
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        #endregion
    }
}