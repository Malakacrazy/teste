using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Executes multiple game actions, each with potentially different targets
    /// </summary>
    [System.Serializable]
    public partial class MultipleGameAction : GameAction
    {
        /// <summary>
        /// Properties specific to multiple actions
        /// </summary>
        [System.Serializable]
        public class MultipleActionProperties : GameAction.GameActionProperties
        {
            public List<GameAction> gameActions = new List<GameAction>();
            
            public MultipleActionProperties() : base() { }
            
            public MultipleActionProperties(List<GameAction> gameActions) : base()
            {
                this.gameActions = gameActions ?? new List<GameAction>();
            }
        }
        
        #region Constructors
        
        public MultipleGameAction() : base()
        {
            Initialize();
        }
        
        public MultipleGameAction(List<GameAction> gameActions) : base()
        {
            Initialize();
            staticProperties = new MultipleActionProperties(gameActions);
        }
        
        public MultipleGameAction(params GameAction[] gameActions) : base()
        {
            Initialize();
            staticProperties = new MultipleActionProperties(gameActions.ToList());
        }
        
        public MultipleGameAction(MultipleActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public MultipleGameAction(System.Func<AbilityContext, MultipleActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "multiple";
            eventName = EventNames.OnMultipleAction;
            effectMessage = "execute {0} actions";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new MultipleActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is MultipleActionProperties multipleProps)
            {
                // Set default targets for each sub-action
                foreach (var gameAction in multipleProps.gameActions)
                {
                    gameAction.SetDefaultTarget(ctx => multipleProps.target);
                }
                return multipleProps;
            }
                
            // Convert base properties to MultipleActionProperties
            return new MultipleActionProperties()
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
                // Create a combined message from all actions
                var messages = properties.gameActions.Select(action => 
                {
                    var (msg, args) = action.GetEffectMessage(context, additionalProperties);
                    return string.Format(msg, args);
                }).ToArray();
                
                return ("{0}", new object[] { string.Join(" and ", messages) });
            }
            
            return ("execute {0} actions", new object[] { properties.gameActions?.Count ?? 0 });
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
        
        public override bool AllTargetsLegal(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.gameActions?.Any(gameAction => gameAction.HasLegalTarget(context, additionalProperties)) == true;
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
                    gameAction.AddEventsToArray(events, context, additionalProperties);
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Add a game action to the multiple action
        /// </summary>
        public void AddAction(GameAction gameAction)
        {
            if (gameAction == null) return;
            
            if (staticProperties is MultipleActionProperties multipleProps)
            {
                multipleProps.gameActions.Add(gameAction);
            }
            else
            {
                staticProperties = new MultipleActionProperties(new List<GameAction> { gameAction });
            }
        }
        
        /// <summary>
        /// Add multiple game actions
        /// </summary>
        public void AddActions(params GameAction[] gameActions)
        {
            foreach (var action in gameActions)
            {
                AddAction(action);
            }
        }
        
        /// <summary>
        /// Get all actions in the multiple action
        /// </summary>
        public List<GameAction> GetActions()
        {
            if (staticProperties is MultipleActionProperties multipleProps)
            {
                return multipleProps.gameActions.ToList();
            }
            return new List<GameAction>();
        }
        
        /// <summary>
        /// Clear all actions
        /// </summary>
        public void ClearActions()
        {
            if (staticProperties is MultipleActionProperties multipleProps)
            {
                multipleProps.gameActions.Clear();
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create multiple action from several actions
        /// </summary>
        public static MultipleGameAction Create(params GameAction[] gameActions)
        {
            return new MultipleGameAction(gameActions);
        }
        
        /// <summary>
        /// Create multiple action from list of actions
        /// </summary>
        public static MultipleGameAction FromList(List<GameAction> gameActions)
        {
            return new MultipleGameAction(gameActions);
        }
        
        /// <summary>
        /// Create empty multiple action (actions can be added later)
        /// </summary>
        public static MultipleGameAction Empty()
        {
            return new MultipleGameAction();
        }
        
        #endregion
    }
}