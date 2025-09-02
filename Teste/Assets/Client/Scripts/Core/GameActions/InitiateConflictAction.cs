using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IInitiateConflictProperties : IPlayerActionProperties
    {
        bool CanPass { get; set; }
        string ForcedDeclaredType { get; set; }
    }

    public class InitiateConflictProperties : GameAction.GameActionProperties, IInitiateConflictProperties
    {
        public bool CanPass { get; set; }
        public string ForcedDeclaredType { get; set; }
        
        public new List<object> Target { get; set; } = new List<object>();
        public new bool CannotBeCancelled { get; set; }
        public new bool Optional { get; set; }
        public new GameAction ParentAction { get; set; }
        public Player PlayerTarget { get; set; }
    }

    public partial class InitiateConflictAction : PlayerAction
    {
        #region Constructors
        
        public InitiateConflictAction() : base()
        {
            Initialize();
        }
        
        public InitiateConflictAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public InitiateConflictAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "initiateConflict";
            eventName = EventNames.OnConflictInitiated;
            effectMessage = "declare a new conflict";
            
            defaultProperties = new InitiateConflictProperties
            {
                CanPass = true
            };
        }
        
        #endregion

        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var player = target as Player;
            if (player == null) return false;
            
            var properties = GetProperties(context, additionalProperties) as IInitiateConflictProperties;
            return base.CanAffect(target, context, additionalProperties) && 
                   player.HasLegalConflictDeclaration(new ConflictProperties { forcedDeclaredType = properties.ForcedDeclaredType });
        }

        protected override List<object> DefaultTargets(AbilityContext context)
        {
            return new List<object> { context.Player };
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            if (player != null)
            {
                var properties = GetProperties(gameEvent.context, additionalProperties) as IInitiateConflictProperties;
                gameEvent.context.Game.InitiateConflict(player, properties.CanPass, properties.ForcedDeclaredType);
                LogExecution("Initiated conflict for {0} (CanPass: {1})", player.name, properties.CanPass);
                return true;
            }
            return false;
        }
    }
}
