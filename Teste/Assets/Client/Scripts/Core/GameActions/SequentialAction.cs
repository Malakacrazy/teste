using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Executes multiple game actions in sequence
    /// </summary>
    [System.Serializable]
    public class SequentialAction : GameAction
    {
        /// <summary>
        /// Properties specific to sequential actions
        /// </summary>
        [System.Serializable]
        public class SequentialProperties : GameActionProperties
        {
            public List<GameAction> gameActions = new List<GameAction>();
            
            public SequentialProperties() : base() { }
            
            public SequentialProperties(List<GameAction> gameActions) : base()
            {
                this.gameActions = gameActions ?? new List<GameAction>();
            }
        }
        
        #region Constructors
        
        public SequentialAction() : base()
        {
            Initialize();
        }
        
        public SequentialAction(List<GameAction> gameActions) : base()
        {
            Initialize();
            staticProperties = new SequentialProperties(gameActions);
        }
        
        public SequentialAction(params GameAction[] gameActions) : base()
        {
            Initialize();
            staticProperties = new SequentialProperties(gameActions.ToList());
        }
        
        public SequentialAction(SequentialProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public SequentialAction(System.Func<AbilityContext, SequentialProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "sequential";
            eventName = EventNames.OnSequentialAction;
            effectMessage = "execute {0} actions in sequence";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new SequentialProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is SequentialProperties seqProps)
            {
                // Set default targets for each sub-action
                foreach (var gameAction in seqProps.gameActions)
                {
                    gameAction.SetDefaultTarget(ctx => seqProps.target);
                }
                return seqProps;
            }
                
            // Convert base properties to SequentialProperties
            return new SequentialProperties()
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
            
            if (properties.gameActions?.Count > 0)
            {
                // Return the effect message of the first action
                return properties.gameActions[0].GetEffectMessage(context, additionalProperties);
            }
            
            return ("execute {0} actions in sequence", new object[] { properties.gameActions?.Count ?? 0 });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.gameActions?.Any(gameAction => gameAction.HasLegalTarget(context, additionalProperties)) == true;
        }
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.gameActions?.Any(gameAction => gameAction.CanAffect(target, context, additionalProperties)) == true;
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.gameActions?.Any(gameAction => 
                gameAction.HasTargetsChosenByInitiatingPlayer(context, additionalProperties)) == true;
        }
        
        #endregion
        
        #region Event Management
        
        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.gameActions?.Count == 0)
                return;
            
            foreach (var gameAction in properties.gameActions)
            {
                if (gameAction.HasLegalTarget(context, additionalProperties))
                {
                    var eventsForThisAction = new List<GameEvent>();
                    
                    // Queue steps to execute each action sequentially
                    context.game.QueueSimpleStep(() =>
                    {
                        gameAction.AddEventsToArray(eventsForThisAction, context, additionalProperties);
                        return true;
                    });
                    
                    context.game.QueueSimpleStep(() =>
                    {
                        context.game.OpenEventWindow(eventsForThisAction);
                        return true;
                    });
                    
                    context.game.QueueSimpleStep(() =>
                    {
                        foreach (var gameEvent in eventsForThisAction)
                        {
                            events.Add(gameEvent);
                        }
                        return true;
                    });
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Add a game action to the sequence
        /// </summary>
        public void AddAction(GameAction gameAction)
        {
            if (gameAction == null) return;
            
            if (staticProperties is SequentialProperties seqProps)
            {
                seqProps.gameActions.Add(gameAction);
            }
            else
            {
                staticProperties = new SequentialProperties(new List<GameAction> { gameAction });
            }
        }
        
        /// <summary>
        /// Add multiple game actions to the sequence
        /// </summary>
        public void AddActions(params GameAction[] gameActions)
        {
            foreach (var action in gameActions)
            {
                AddAction(action);
            }
        }
        
        /// <summary>
        /// Get all actions in the sequence
        /// </summary>
        public List<GameAction> GetActions()
        {
            if (staticProperties is SequentialProperties seqProps)
            {
                return seqProps.gameActions.ToList();
            }
            return new List<GameAction>();
        }
        
        /// <summary>
        /// Clear all actions from the sequence
        /// </summary>
        public void ClearActions()
        {
            if (staticProperties is SequentialProperties seqProps)
            {
                seqProps.gameActions.Clear();
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create sequential action from multiple actions
        /// </summary>
        public static SequentialAction Create(params GameAction[] gameActions)
        {
            return new SequentialAction(gameActions);
        }
        
        /// <summary>
        /// Create sequential action from list of actions
        /// </summary>
        public static SequentialAction FromList(List<GameAction> gameActions)
        {
            return new SequentialAction(gameActions);
        }
        
        /// <summary>
        /// Create empty sequential action (actions can be added later)
        /// </summary>
        public static SequentialAction Empty()
        {
            return new SequentialAction();
        }
        
        #endregion
    }
}