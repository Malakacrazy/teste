using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Player searches their deck for cards
    /// </summary>
    [System.Serializable]
    public partial class DeckSearchAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to deck search
        /// </summary>
        [System.Serializable]
        public class DeckSearchProperties : PlayerActionProperties
        {
            public int amount = -1;
            public bool reveal;
            [System.NonSerialized]
            public System.Func<DrawCard, AbilityContext, bool> cardCondition;
            
            public DeckSearchProperties() : base() { }
            
            public DeckSearchProperties(int amount) : base()
            {
                this.amount = amount;
            }
        }
        
        #region Constructors
        
        public DeckSearchAction() : base()
        {
            Initialize();
        }
        
        public DeckSearchAction(DeckSearchProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DeckSearchAction(System.Func<AbilityContext, DeckSearchProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "deckSearch";
            eventName = EventNames.OnDeckSearch;
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new DeckSearchProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is DeckSearchProperties searchProps)
            {
                // Set default reveal based on condition
                if (!searchProps.reveal)
                    searchProps.reveal = searchProps.cardCondition != null;
                
                // Set default condition if not specified
                if (searchProps.cardCondition == null)
                    searchProps.cardCondition = (card, ctx) => true;
                    
                return searchProps;
            }
                
            // Convert base properties to DeckSearchProperties
            return new DeckSearchProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                cardCondition = (card, ctx) => true
            };
        }
        
        #endregion
        
        #region Default Targets
        
        protected override List<object> DefaultTargets(AbilityContext context)
        {
            // Default to the acting player
            return context.player != null ? 
                new List<object> { context.player } : 
                new List<object>();
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            string message;
            if (properties.amount > 0)
            {
                var cardText = properties.amount > 1 ? $"{properties.amount} cards" : "card";
                message = $"look at the top {cardText} of their deck";
            }
            else
            {
                message = "search their deck";
            }
            
            return (message, new object[0]);
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is Player player))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Must have cards in deck and amount must not be zero
            if (properties.amount == 0 || player.conflictDeck.Count == 0)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("amount", properties.amount);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var context = gameEvent.context;
            var player = gameEvent.GetProperty("player") as Player;
            var properties = GetProperties(context, additionalProperties);
            var requestedAmount = gameEvent.GetProperty("amount", -1);
            
            if (player == null)
                return false;
                
            var amount = requestedAmount > -1 ? requestedAmount : player.conflictDeck.Count;
            var cards = player.conflictDeck.Take(amount).OfType<DrawCard>().ToList();
            
            // Filter by condition if searching entire deck
            if (requestedAmount == -1)
            {
                cards = cards.Where(card => properties.cardCondition(card, context)).ToList();
            }
            
            var revealText = properties.reveal ? "reveal and " : "";
            
            context.game.PromptWithHandlerMenu(player, new HandlerMenuPromptProperties
            {
                activePromptTitle = $"Select a card to {revealText}put in your hand",
                context = context,
                cards = cards.Cast<BaseCard>().ToList(),
                cardCondition = (card, ctx) => card is DrawCard drawCard && properties.cardCondition(drawCard, ctx),
                choices = new List<MenuOption> { new MenuOption { text = "Take nothing" } },
                handlers = new List<System.Action>
                {
                    () =>
                    {
                        context.game.AddMessage("{0} takes nothing", player);
                        player.ShuffleConflictDeck();
                    }
                },
                cardHandler = card =>
                {
                    if (properties.reveal)
                    {
                        context.game.AddMessage("{0} takes {1} and adds it to their hand", player, card);
                    }
                    else
                    {
                        context.game.AddMessage("{0} takes a card into their hand", player);
                    }
                    
                    player.MoveCard(card, Locations.Hand);
                    player.ShuffleConflictDeck();
                }
            });
            
            LogExecution("{0} searched their deck", player.name);
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Search entire deck with condition
        /// </summary>
        public static DeckSearchAction WithCondition(System.Func<DrawCard, AbilityContext, bool> condition, bool reveal = true, Player target = null)
        {
            var action = new DeckSearchAction(new DeckSearchProperties(-1) 
            { 
                cardCondition = condition, 
                reveal = reveal 
            });
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Look at top cards of deck
        /// </summary>
        public static DeckSearchAction TopCards(int amount, Player target = null)
        {
            var action = new DeckSearchAction(new DeckSearchProperties(amount));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Search for character cards
        /// </summary>
        public static DeckSearchAction ForCharacter(Player target = null)
        {
            return WithCondition((card, ctx) => card.type == CardTypes.Character, true, target);
        }
        
        /// <summary>
        /// Search for attachment cards
        /// </summary>
        public static DeckSearchAction ForAttachment(Player target = null)
        {
            return WithCondition((card, ctx) => card.type == CardTypes.Attachment, true, target);
        }
        
        /// <summary>
        /// Search for specific cost
        /// </summary>
        public static DeckSearchAction ForCost(int cost, Player target = null)
        {
            return WithCondition((card, ctx) => card.cost == cost, true, target);
        }
        
        #endregion
    }
}