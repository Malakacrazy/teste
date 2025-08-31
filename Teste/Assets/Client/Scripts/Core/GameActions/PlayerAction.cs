using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for actions that target players
    /// </summary>
    [System.Serializable]
    public abstract class PlayerAction : GameAction
    {
        /// <summary>
        /// Properties for player-targeted actions
        /// </summary>
        [System.Serializable]
        public class PlayerActionProperties : GameActionProperties
        {
            public PlayerActionProperties() : base() { }
            
            public PlayerActionProperties(List<object> targets) : base(targets) { }
        }
        
        #region Constructors
        
        protected PlayerAction() : base()
        {
            Initialize();
        }
        
        protected PlayerAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        protected PlayerAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            targetTypes = new List<string> { "player" };
        }
        
        #endregion
        
        #region Default Targets
        
        protected override List<object> DefaultTargets(AbilityContext context)
        {
            // Default to opponent player
            return context.player?.opponent != null ? 
                new List<object> { context.player.opponent } : 
                new List<object>();
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool CheckEventCondition(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            return player != null && CanAffect(player, gameEvent.context, additionalProperties);
        }
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("player", target as Player);
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new PlayerActionProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is PlayerActionProperties playerProps)
                return playerProps;
                
            // Convert base properties to PlayerActionProperties
            return new PlayerActionProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Target the acting player
        /// </summary>
        protected void TargetSelf()
        {
            SetDefaultTarget(context => context.player);
        }
        
        /// <summary>
        /// Target the opponent player
        /// </summary>
        protected void TargetOpponent()
        {
            SetDefaultTarget(context => context.player?.opponent);
        }
        
        /// <summary>
        /// Target both players
        /// </summary>
        protected void TargetBothPlayers()
        {
            SetDefaultTarget(context =>
            {
                var targets = new List<object>();
                if (context.player != null)
                    targets.Add(context.player);
                if (context.player?.opponent != null)
                    targets.Add(context.player.opponent);
                return targets;
            });
        }
        
        #endregion
    }
}