using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Executes multiple game actions simultaneously with shared targeting (all must succeed)
    /// </summary>
    [System.Serializable]
    public partial class JointGameAction : GameAction
    {
        /// <summary>
        /// Properties specific to joint actions
        /// </summary>
        [System.Serializable]
        public class JointGameProperties : GameAction.GameActionProperties
        {
            public List<GameAction> gameActions = new List<GameAction>();
            
            public JointGameProperties() : base() { }
            
            public JointGameProperties(List<GameAction> gameActions) : base()
            {
                this.gameActions = gameActions ?? new List<GameAction>();
            }
        }
        
        #region Constructors
        
        public JointGameAction() : base()
        {
            Initialize();
        }
        
        public JointGameAction(List<GameAction> gameActions) : base()
        {
            Initialize();
            staticProperties = new JointGameProperties(gameActions);
        }
        
        public JointGameAction(params GameAction[] gameActions) : base()
        {
            Initialize();
            staticProperties = new JointGameProperties(gameActions.ToList());
        }
        
        public JointGameAction(JointGameProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public JointGameAction(System.Func<AbilityContext, JointGameProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "joint";
            eventName = EventNames.OnJointAction;
            effectMessage = "do several things";
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new JointGameProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is JointGameProperties jointProps)
            {
                // Set default targets for each sub-action
                foreach (var gameAction in jointProps.gameActions)
                {
                    gameAction.SetDefaultTarget(ctx => jointProps.target);
                }
                return jointProps;
            }
                
            // Convert base properties to JointGameProperties
            return new JointGameProperties()
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
                
                return ("do several things: {0}", new object[] { string.Join(", ", messages) });
            }
            
            return ("do several things", new object[0]);
        }
        
        #endregion
        
        #region Targeting
        
        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            // For joint actions, ALL actions must have legal targets
            return properties.gameActions?.All(gameAction => gameAction.HasLegalTarget(context, additionalProperties)) == true;
        }
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            // For joint actions, ALL actions must be able to affect the target
            return properties.gameActions?.All(gameAction => gameAction.CanAffect(target, context, additionalProperties)) == true;
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
            
            // Only proceed if ALL actions have legal targets
            if (HasLegalTarget(context, additionalProperties))
            {
                foreach (var gameAction in properties.gameActions)
                {
                    gameAction.AddEventsToArray(events, context, additionalProperties);
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Add a game action to the joint action
        /// </summary>
        public void AddAction(GameAction gameAction)
        {
            if (gameAction == null) return;
            
            if (staticProperties is JointGameProperties jointProps)
            {
                jointProps.gameActions.Add(gameAction);
            }
            else
            {
                staticProperties = new JointGameProperties(new List<GameAction> { gameAction });
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
        /// Get all actions in the joint action
        /// </summary>
        public List<GameAction> GetActions()
        {
            if (staticProperties is JointGameProperties jointProps)
            {
                return jointProps.gameActions.ToList();
            }
            return new List<GameAction>();
        }
        
        /// <summary>
        /// Clear all actions
        /// </summary>
        public void ClearActions()
        {
            if (staticProperties is JointGameProperties jointProps)
            {
                jointProps.gameActions.Clear();
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create joint action from several actions
        /// </summary>
        public static JointGameAction Create(params GameAction[] gameActions)
        {
            return new JointGameAction(gameActions);
        }
        
        /// <summary>
        /// Create joint action from list of actions
        /// </summary>
        public static JointGameAction FromList(List<GameAction> gameActions)
        {
            return new JointGameAction(gameActions);
        }
        
        /// <summary>
        /// Create empty joint action (actions can be added later)
        /// </summary>
        public static JointGameAction Empty()
        {
            return new JointGameAction();
        }
        
        /// <summary>
        /// Create joint action to bow and honor a character
        /// </summary>
        public static JointGameAction BowAndHonor(BaseCard character)
        {
            return Create(
                new BowAction(new GameAction.GameActionProperties { target = new List<object> { character } }),
                new HonorAction(new GameAction.GameActionProperties { target = new List<object> { character } })
            );
        }
        
        /// <summary>
        /// Create joint action to ready and place fate
        /// </summary>
        public static JointGameAction ReadyAndPlaceFate(BaseCard character, int fate = 1)
        {
            return Create(
                new ReadyAction(new GameAction.GameActionProperties { target = new List<object> { character } }),
                new PlaceFateAction(new PlaceFateAction.PlaceFateProperties(fate) { target = new List<object> { character } })
            );
        }
        
        /// <summary>
        /// Create joint action to gain fate and honor
        /// </summary>
        public static JointGameAction GainFateAndHonor(Player player, int fate = 1, int honor = 1)
        {
            return Create(
                GameActions.GainFate(player, fate),
                GameActions.GainHonor(player, honor)
            );
        }
        
        #endregion
    }
}