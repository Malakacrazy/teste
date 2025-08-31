using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Attaches a card to a ring
    /// </summary>
    [System.Serializable]
    public partial class AttachToRingAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to attach to ring actions
        /// </summary>
        [System.Serializable]
        public class AttachToRingActionProperties : CardActionProperties
        {
            public DrawCard attachment;
            
            public AttachToRingActionProperties() : base() { }
            
            public AttachToRingActionProperties(DrawCard attachment) : base()
            {
                this.attachment = attachment;
            }
        }
        
        #region Constructors
        
        public AttachToRingAction() : base()
        {
            Initialize();
        }
        
        public AttachToRingAction(AttachToRingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public AttachToRingAction(System.Func<AbilityContext, AttachToRingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "attachToRing";
            eventName = EventNames.OnCardAttached;
            effectMessage = "attach {1} to {0}";
            targetTypes = new List<string> { "ring" };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new AttachToRingActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is AttachToRingActionProperties attachProps)
                return attachProps;
                
            // Convert base properties to AttachToRingActionProperties
            return new AttachToRingActionProperties()
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
            if (!(target is Ring ring))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Basic validation
            if (context?.player == null || ring == null)
                return false;
                
            // Attachment validation
            if (properties.attachment == null)
                return false;
                
            // Check if another unique copy is already in play
            if (properties.attachment.IsUnique() && properties.attachment.AnotherUniqueInPlay(context.player))
                return false;
                
            // Check if attachment can attach to this ring
            if (!properties.attachment.CanAttach(ring, context))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var parent = gameEvent.GetProperty("parent") as Ring;
            return parent != null && CanAffect(parent, gameEvent.context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool IsEventFullyResolved(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var parent = gameEvent.GetProperty("parent") as Ring;
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
            gameEvent.AddProperty("parent", target as Ring);
            gameEvent.AddProperty("card", properties.attachment);
            gameEvent.context = context;
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var parent = gameEvent.GetProperty("parent") as Ring;
            var attachment = gameEvent.GetProperty("card") as DrawCard;
            var context = gameEvent.context;
            
            if (parent == null || attachment == null || context == null)
                return false;
            
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
            
            // Attach to ring
            parent.AddAttachment(attachment);
            attachment.parent = parent;
            
            // Update controller if necessary
            if (attachment.controller != context.player)
            {
                attachment.controller = context.player;
                attachment.UpdateEffectContexts();
            }
            
            LogExecution("Attached {0} to ring {1}", attachment.name, parent.element);
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to attach specific attachment to ring
        /// </summary>
        public static AttachToRingAction Attachment(DrawCard attachment, Ring ring = null)
        {
            var action = new AttachToRingAction(new AttachToRingActionProperties(attachment));
            if (ring != null)
                action.SetDefaultTarget(context => ring);
            return action;
        }
        
        /// <summary>
        /// Create action with dynamic attachment selection
        /// </summary>
        public static AttachToRingAction WithAttachment(System.Func<AbilityContext, DrawCard> attachmentSelector)
        {
            return new AttachToRingAction(context => new AttachToRingActionProperties(attachmentSelector(context)));
        }
        
        #endregion
    }
}