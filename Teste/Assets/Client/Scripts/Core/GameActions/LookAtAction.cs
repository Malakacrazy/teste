using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Look at cards privately (only the acting player sees them)
    /// </summary>
    [System.Serializable]
    public partial class LookAtAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to looking at cards
        /// </summary>
        [System.Serializable]
        public class LookAtProperties : CardActionProperties
        {
            public string message = "{0} sees {1}";
            public System.Func<List<BaseCard>, object[]> messageArgsFactory;
            
            public LookAtProperties() : base() { }
            
            public LookAtProperties(string message) : base()
            {
                this.message = message;
            }
        }
        
        #region Constructors
        
        public LookAtAction() : base()
        {
            Initialize();
        }
        
        public LookAtAction(LookAtProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public LookAtAction(System.Func<AbilityContext, LookAtProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "lookAt";
            eventName = EventNames.OnLookAtCards;
            effectMessage = "look at a facedown card";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new LookAtProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is LookAtProperties lookProps)
                return lookProps;
                
            // Convert base properties to LookAtProperties
            return new LookAtProperties()
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
                
            // Cannot look at cards that are already face up and in certain locations
            if (!card.facedown && (card.IsInProvince() || card.location == Locations.PlayArea))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var cards = properties.target.OfType<BaseCard>()
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
            var cards = target as List<BaseCard> ?? (target is BaseCard card ? new List<BaseCard> { card } : new List<BaseCard>());
            
            gameEvent.AddProperty("cards", cards);
            gameEvent.context = context;
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var context = gameEvent.context;
            var cards = gameEvent.GetProperty("cards") as List<BaseCard>;
            var properties = GetProperties(context, additionalProperties);
            
            if (cards?.Count > 0)
            {
                // Determine message arguments
                object[] messageArgs;
                if (properties.messageArgsFactory != null)
                {
                    messageArgs = properties.messageArgsFactory(cards);
                }
                else
                {
                    messageArgs = new object[] { context.source, cards };
                }
                
                context.game.AddMessage(properties.message, messageArgs);
                LogExecution("{0} looked at {1} cards", context.player.name, cards.Count);
            }
            return true;
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
        /// Look at specific card
        /// </summary>
        public static LookAtAction Card(BaseCard card)
        {
            var action = new LookAtAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Look at multiple cards
        /// </summary>
        public static LookAtAction Cards(params BaseCard[] cards)
        {
            var action = new LookAtAction();
            action.SetDefaultTarget(context => cards.ToList());
            return action;
        }
        
        /// <summary>
        /// Look at top cards of deck
        /// </summary>
        public static LookAtAction TopOfDeck(int amount = 1, Player player = null)
        {
            var action = new LookAtAction();
            action.SetDefaultTarget(context =>
            {
                var targetPlayer = player ?? context.player;
                return targetPlayer.conflictDeck.Take(amount).ToList();
            });
            return action;
        }
        
        /// <summary>
        /// Look at cards in hand
        /// </summary>
        public static LookAtAction Hand(Player player = null)
        {
            var action = new LookAtAction();
            action.SetDefaultTarget(context =>
            {
                var targetPlayer = player ?? context.player.opponent;
                return targetPlayer.hand.ToList();
            });
            return action;
        }
        
        /// <summary>
        /// Look at facedown dynasty cards
        /// </summary>
        public static LookAtAction FacedownDynastyCards(Player player = null)
        {
            var action = new LookAtAction();
            action.SetDefaultTarget(context =>
            {
                var targetPlayer = player ?? context.player;
                return targetPlayer.dynastyCards.Where(c => c.facedown).ToList();
            });
            return action;
        }
        
        /// <summary>
        /// Look at cards with custom message
        /// </summary>
        public static LookAtAction WithMessage(string message, System.Func<List<BaseCard>, object[]> messageArgsFactory = null)
        {
            return new LookAtAction(new LookAtProperties(message) { messageArgsFactory = messageArgsFactory });
        }
        
        #endregion
    }
}