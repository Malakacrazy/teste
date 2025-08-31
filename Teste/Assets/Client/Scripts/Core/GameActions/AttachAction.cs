using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Attaches one card to another (typically attachments to characters)
    /// </summary>
    [System.Serializable]
    public partial class AttachAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to attach actions
        /// </summary>
        [System.Serializable]
        public class AttachActionProperties : CardActionProperties
        {
            public DrawCard attachment;
            
            public AttachActionProperties() : base() { }
            
            public AttachActionProperties(DrawCard attachment) : base()
            {
                this.attachment = attachment;
            }
        }
        
        #region Constructors
        
        public AttachAction() : base()
        {
            Initialize();
        }
        
        public AttachAction(AttachActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public AttachAction(System.Func<AbilityContext, AttachActionProperties> factory) : base(context => factory(context))
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "attach";
            eventName = EventNames.OnCardAttached;
            effectMessage = "attach {1} to {0}";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new AttachActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is AttachActionProperties attachProps)
                return attachProps;
                
            // Convert base properties to AttachActionProperties
            return new AttachActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return ("attach {1} to {0}", new object[] { properties.target, properties.attachment });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Basic validation
            if (context?.player == null || card.location != Locations.PlayArea)
                return false;
                
            // Attachment validation
            if (properties.attachment == null)
                return false;
                
            // Check if another unique copy is already in play
            if (properties.attachment.IsUnique() && properties.attachment.AnotherUniqueInPlay(context.player))
                return false;
                
            // Check if attachment can attach to this card
            if (!properties.attachment.CanAttach(card, context))
                return false;
                
            // Check if card allows this attachment
            if (!card.AllowAttachment(properties.attachment))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var parent = gameEvent.GetProperty("parent") as BaseCard;
            return parent != null && CanAffect(parent, gameEvent.context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var parent = gameEvent.GetProperty("parent") as BaseCard;
            var card = gameEvent.GetProperty("card") as DrawCard;
            
            return parent == target && 
                   card == properties.attachment && 
                   gameEvent.name == eventName && 
                   !gameEvent.IsCancelled();
        }
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            gameEvent.name = eventName;
            gameEvent.AddProperty("parent", target as BaseCard);
            gameEvent.AddProperty("card", properties.attachment);
            gameEvent.context = context;
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var parent = gameEvent.GetProperty("parent") as BaseCard;
            var attachment = gameEvent.GetProperty("card") as DrawCard;
            var context = gameEvent.context;
            
            if (parent == null || attachment == null || context == null)
                return;
            
            // If attachment is already in play, remove it from current parent
            if (attachment.location == Locations.PlayArea)
            {
                attachment.parent?.RemoveAttachment(attachment);
            }
            else
            {
                // Remove from current pile and move to play
                attachment.controller.RemoveCardFromPile(attachment);
                attachment.isNew = true;
                attachment.MoveTo(Locations.PlayArea);
            }
            
            // Attach to new parent
            parent.AddAttachment(attachment);
            attachment.parent = parent;
            
            // Update controller if necessary
            if (attachment.controller != context.player)
            {
                attachment.controller = context.player;
                attachment.UpdateEffectContexts();
            }
            
            LogExecution("Attached {0} to {1}", attachment.name, parent.name);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to attach specific attachment to target
        /// </summary>
        public static AttachAction Attachment(DrawCard attachment, BaseCard target = null)
        {
            var action = new AttachAction(new AttachActionProperties(attachment));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action with dynamic attachment selection
        /// </summary>
        public static AttachAction WithAttachment(System.Func<AbilityContext, DrawCard> attachmentSelector)
        {
            return new AttachAction(context => new AttachActionProperties(attachmentSelector(context)));
        }
        
        #endregion
    }
}