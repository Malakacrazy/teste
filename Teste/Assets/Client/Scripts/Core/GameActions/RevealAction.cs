using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Reveals cards to all players
    /// </summary>
    [System.Serializable]
    public partial class RevealAction : CardGameAction
    {
        public List<BaseCard> cards
        {
            get { return GetProperties(null).target?.Cast<BaseCard>()?.ToList() ?? new List<BaseCard>(); }
            set { 
                var props = GetProperties(null);
                props.target.Clear();
                if (value != null) props.target.AddRange(value.Cast<object>());
            }
        }
        /// <summary>
        /// Properties specific to revealing cards
        /// </summary>
        [System.Serializable]
        public class RevealProperties : CardActionProperties
        {
            public bool chatMessage = false;
            public Player player;
            public bool onDeclaration = false;
            
            public RevealProperties() : base() { }
            
            public RevealProperties(bool chatMessage) : base()
            {
                this.chatMessage = chatMessage;
            }
        }
        
        #region Constructors
        
        public RevealAction() : base()
        {
            Initialize();
        }
        
        public RevealAction(RevealProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public RevealAction(System.Func<AbilityContext, RevealProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "reveal";
            eventName = EventNames.OnCardRevealed;
            effectMessage = "reveal a card";
            costMessage = "revealing {0}";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new RevealProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is RevealProperties revealProps)
                return revealProps;
                
            // Convert base properties to RevealProperties
            return new RevealProperties()
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
                
            // Cannot reveal cards that are already face up and in certain locations
            if (!card.facedown && (card.IsInProvince() || card.location == Locations.PlayArea))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            gameEvent.AddProperty("onDeclaration", properties.onDeclaration);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            var context = gameEvent.context;
            var properties = GetProperties(context, additionalProperties);
            
            if (card != null)
            {
                if (properties.chatMessage)
                {
                    var player = properties.player ?? context.player;
                    context.game.AddMessage("{0} reveals {1} due to {2}", 
                        player, card, context.source);
                }
                
                card.facedown = false;
                LogExecution("Revealed {0}", card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Reveal card with chat message
        /// </summary>
        public static RevealAction WithMessage(BaseCard card = null, Player player = null)
        {
            var action = new RevealAction(new RevealProperties(true) { player = player });
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Reveal card silently
        /// </summary>
        public static RevealAction Silent(BaseCard card = null)
        {
            var action = new RevealAction(new RevealProperties(false));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Reveal card on declaration
        /// </summary>
        public static RevealAction OnDeclaration(BaseCard card = null)
        {
            var action = new RevealAction(new RevealProperties(true) { onDeclaration = true });
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Reveal multiple cards
        /// </summary>
        public static RevealAction Cards(params BaseCard[] cards)
        {
            var action = new RevealAction();
            action.SetDefaultTarget(context => cards.ToList());
            return action;
        }
        
        #endregion
    }
}