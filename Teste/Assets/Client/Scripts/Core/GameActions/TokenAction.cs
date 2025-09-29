using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface ITokenActionProperties : IGameActionProperties
    {
    }

    public class TokenActionProperties : GameAction.GameActionProperties, ITokenActionProperties
    {
        public new List<object> Target { get; set; } = new List<object>();
        public new bool CannotBeCancelled { get; set; }
        public new bool Optional { get; set; }
        public new GameAction ParentAction { get; set; }
    }

    public class TokenAction : GameAction
    {
        #region Constructors
        
        protected TokenAction() : base()
        {
            Initialize();
        }
        
        protected TokenAction(TokenActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        protected TokenAction(System.Func<AbilityContext, TokenActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            targetTypes = new List<string> { "token" };
        }
        
        #endregion

        public virtual List<StatusToken> DefaultTargets(AbilityContext context)
        {
            var sourceCard = context.Source as BaseCard;
            if (sourceCard?.PersonalHonor > 0)
            {
                var honorToken = new StatusToken(context.game, sourceCard, sourceCard.PersonalHonor > 0);
                return new List<StatusToken> { honorToken };
            }
            return new List<StatusToken>();
        }

        public virtual bool CanAffect(StatusToken target, AbilityContext context, object additionalProperties = null)
        {
            return target.Type == "token";
        }

        protected bool CheckEventCondition(GameEvent eventObj, GameActionProperties additionalProperties = null)
        {
            if (eventObj != null && eventObj.Token != null)
            {
                return CanAffect(eventObj.Token, eventObj.Context, additionalProperties);
            }
            return false;
        }

        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            if (target is StatusToken token)
            {
                gameEvent.Token = token;
            }
        }
    }
}
