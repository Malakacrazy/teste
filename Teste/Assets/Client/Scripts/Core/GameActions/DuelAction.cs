using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Initiates a duel between characters
    /// </summary>
    [System.Serializable]
    public partial class DuelAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to duels
        /// </summary>
        [System.Serializable]
        public class DuelProperties : CardActionProperties
        {
            public DuelTypes type;
            public DrawCard challenger;
            public GameAction gameAction;
            public System.Func<Duel, AbilityContext, GameAction> gameActionFactory;
            public string message;
            public System.Func<Duel, AbilityContext, object[]> messageArgsFactory;
            public System.Action<AbilityContext, object> costHandler;
            public System.Func<DrawCard, int> statistic;
            public object challengerEffect;
            public object targetEffect;
            public GameAction refuseGameAction;
            
            public DuelProperties() : base() { }
            
            public DuelProperties(DuelTypes type, GameAction gameAction) : base()
            {
                this.type = type;
                this.gameAction = gameAction;
            }
            
            public DuelProperties(DuelTypes type, System.Func<Duel, AbilityContext, GameAction> gameActionFactory) : base()
            {
                this.type = type;
                this.gameActionFactory = gameActionFactory;
            }
        }
        
        #region Constructors
        
        public DuelAction() : base()
        {
            Initialize();
        }
        
        public DuelAction(DuelProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DuelAction(System.Func<AbilityContext, DuelProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "duel";
            eventName = EventNames.OnDuelInitiated;
            effectMessage = "initiate a {0} duel";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new DuelProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is DuelProperties duelProps)
            {
                // Set default challenger if not specified
                if (duelProps.challenger == null)
                {
                    duelProps.challenger = context.source as DrawCard;
                }
                return duelProps;
            }
                
            // Convert base properties to DuelProperties
            return new DuelProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                challenger = context.source as DrawCard
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.target is List<object> targets && targets.Count > 1)
            {
                var targetCards = targets.OfType<DrawCard>().ToArray();
                var targetNames = string.Join(" and ", targetCards.Select((card, index) => "{" + (index + 2) + "}"));
                return ($"initiate a {properties.type} duel : {{0}} vs. {targetNames}", 
                    new object[] { properties.challenger }.Concat(targetCards.Cast<object>()).ToArray());
            }
            
            return ($"initiate a {properties.type} duel : {{0}} vs. {{1}}", 
                new object[] { properties.challenger, properties.target });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is DrawCard card))
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            if (!base.CanAffect(target, context, additionalProperties))
                return false;
                
            // Challenger must exist and not have dash in this duel type
            if (properties.challenger == null || properties.challenger.HasDash(properties.type))
                return false;
                
            // Target must be in play area and not have dash in this duel type
            if (card.location != Locations.PlayArea || card.HasDash(properties.type))
                return false;
            
            return true;
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            // Create mock duel to test game action
            var mockDuel = new Duel(context.game, properties.challenger, new List<DrawCard>(), 
                properties.type, properties.statistic);
                
            var gameAction = properties.gameActionFactory?.Invoke(mockDuel, context) ?? properties.gameAction;
            
            return gameAction?.HasTargetsChosenByInitiatingPlayer(context, additionalProperties) ?? false;
        }
        
        #endregion
        
        #region Event Management
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var context = gameEvent.context;
            var cards = gameEvent.GetProperty("cards") as List<DrawCard>;
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.challenger?.location != Locations.PlayArea || 
                cards?.All(card => card.location != Locations.PlayArea) == true)
            {
                context.game.AddMessage("The duel cannot proceed as at least one participant for each side has to be in play");
                return;
            }
            
            // Create the duel
            var duel = new Duel(context.game, properties.challenger, cards, properties.type, properties.statistic);
            
            // Apply lasting effects if specified
            if (properties.challengerEffect != null)
            {
                GameActions.CardLastingEffect(new LastingEffectCardAction.LastingEffectCardProperties
                {
                    effect = properties.challengerEffect,
                    duration = Durations.Custom,
                    until = new Dictionary<string, System.Func<GameEvent, bool>>
                    {
                        { "onDuelFinished", evt => evt.GetProperty("duel") == duel }
                    }
                }).Resolve(properties.challenger, context);
            }
            
            if (properties.targetEffect != null)
            {
                GameActions.CardLastingEffect(new LastingEffectCardAction.LastingEffectCardProperties
                {
                    effect = properties.targetEffect,
                    duration = Durations.Custom,
                    until = new Dictionary<string, System.Func<GameEvent, bool>>
                    {
                        { "onDuelFinished", evt => evt.GetProperty("duel") == duel }
                    }
                }).Resolve(properties.target, context);
            }
            
            // Queue duel flow
            var costHandlerWrapper = properties.costHandler != null ? 
                new System.Action<object>(prompt => properties.costHandler(context, prompt)) : null;
                
            var resolutionHandler = new System.Action<Duel>(completedDuel => ResolveDuel(completedDuel, context, additionalProperties));
            
            context.game.QueueStep(new DuelFlow(context.game, duel, costHandlerWrapper, resolutionHandler));
        }
        
        #endregion
        
        #region Duel Resolution
        
        /// <summary>
        /// Resolve the duel's game action
        /// </summary>
        private void ResolveDuel(Duel duel, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            var gameAction = properties.gameActionFactory?.Invoke(duel, context) ?? properties.gameAction;
            
            if (gameAction?.HasLegalTarget(context) == true)
            {
                string message;
                object[] messageArgs;
                
                if (!string.IsNullOrEmpty(properties.message))
                {
                    message = properties.message;
                    messageArgs = properties.messageArgsFactory?.Invoke(duel, context) ?? new object[0];
                }
                else
                {
                    var (effectMsg, effectArgs) = gameAction.GetEffectMessage(context);
                    message = effectMsg;
                    messageArgs = effectArgs;
                }
                
                context.game.AddMessage("Duel Effect: " + message, messageArgs);
                gameAction.Resolve(null, context);
            }
            else
            {
                context.game.AddMessage("The duel has no effect");
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create military duel with specific resolution
        /// </summary>
        public static DuelAction Military(GameAction winnerEffect, DrawCard challenger = null)
        {
            var action = new DuelAction(new DuelProperties(DuelTypes.Military, winnerEffect));
            if (challenger != null)
                action.SetDefaultTarget(context => challenger);
            return action;
        }
        
        /// <summary>
        /// Create political duel with specific resolution
        /// </summary>
        public static DuelAction Political(GameAction winnerEffect, DrawCard challenger = null)
        {
            var action = new DuelAction(new DuelProperties(DuelTypes.Political, winnerEffect));
            if (challenger != null)
                action.SetDefaultTarget(context => challenger);
            return action;
        }
        
        #endregion
    }
}