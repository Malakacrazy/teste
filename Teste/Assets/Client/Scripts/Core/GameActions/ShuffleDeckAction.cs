using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Shuffles a player's deck
    /// </summary>
    [System.Serializable]
    public partial class ShuffleDeckAction : PlayerAction
    {
        /// <summary>
        /// Properties specific to shuffling decks
        /// </summary>
        [System.Serializable]
        public class ShuffleDeckProperties : PlayerActionProperties
        {
            public string deck;
            
            public ShuffleDeckProperties() : base() { }
            
            public ShuffleDeckProperties(string deck) : base()
            {
                this.deck = deck;
            }
        }
        
        #region Constructors
        
        public ShuffleDeckAction() : base()
        {
            Initialize();
        }
        
        public ShuffleDeckAction(ShuffleDeckProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ShuffleDeckAction(System.Func<AbilityContext, ShuffleDeckProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "shuffleDeck";
            eventName = EventNames.OnDeckShuffled;
            effectMessage = "shuffle {0}'s deck";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new ShuffleDeckProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is ShuffleDeckProperties shuffleProps)
                return shuffleProps;
                
            // Convert base properties to ShuffleDeckProperties
            return new ShuffleDeckProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                deck = Locations.ConflictDeck // Default to conflict deck
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
            var deckType = properties.deck == Locations.DynastyDeck ? "dynasty deck" : "conflict deck";
            return ("shuffle {0}'s {1}", new object[] { properties.target, deckType });
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("deck", properties.deck);
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var deck = gameEvent.GetProperty("deck") as string;
            
            if (player != null && !string.IsNullOrEmpty(deck))
            {
                if (deck == Locations.ConflictDeck)
                {
                    player.ShuffleConflictDeck();
                    LogExecution("Shuffled {0}'s conflict deck", player.name);
                }
                else if (deck == Locations.DynastyDeck)
                {
                    player.ShuffleDynastyDeck();
                    LogExecution("Shuffled {0}'s dynasty deck", player.name);
                }
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Shuffle player's conflict deck
        /// </summary>
        public static ShuffleDeckAction ConflictDeck(Player player = null)
        {
            var action = new ShuffleDeckAction(new ShuffleDeckProperties(Locations.ConflictDeck));
            if (player != null)
                action.SetDefaultTarget(context => player);
            return action;
        }
        
        /// <summary>
        /// Shuffle player's dynasty deck
        /// </summary>
        public static ShuffleDeckAction DynastyDeck(Player player = null)
        {
            var action = new ShuffleDeckAction(new ShuffleDeckProperties(Locations.DynastyDeck));
            if (player != null)
                action.SetDefaultTarget(context => player);
            return action;
        }
        
        /// <summary>
        /// Shuffle opponent's conflict deck
        /// </summary>
        public static ShuffleDeckAction OpponentConflictDeck()
        {
            var action = new ShuffleDeckAction(new ShuffleDeckProperties(Locations.ConflictDeck));
            action.TargetOpponent();
            return action;
        }
        
        /// <summary>
        /// Shuffle opponent's dynasty deck
        /// </summary>
        public static ShuffleDeckAction OpponentDynastyDeck()
        {
            var action = new ShuffleDeckAction(new ShuffleDeckProperties(Locations.DynastyDeck));
            action.TargetOpponent();
            return action;
        }
        
        /// <summary>
        /// Shuffle both decks for a player
        /// </summary>
        public static List<ShuffleDeckAction> BothDecks(Player player = null)
        {
            return new List<ShuffleDeckAction>
            {
                ConflictDeck(player),
                DynastyDeck(player)
            };
        }
        
        #endregion
    }
}