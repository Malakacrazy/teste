using System;
using UnityEngine;

namespace L5RGame
{
    public interface IDiscardStatusProperties : ITokenActionProperties
    {
    }

    public class DiscardStatusProperties : TokenActionProperties, IDiscardStatusProperties
    {
    }

    public partial class DiscardStatusAction : TokenAction
    {
        #region Constructors
        
        public DiscardStatusAction() : base()
        {
            Initialize();
        }
        
        public DiscardStatusAction(GameActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DiscardStatusAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "discardStatus";
            eventName = EventNames.OnStatusTokenDiscarded;
            effectMessage = "discard {0}'s status token";
            costMessage = "discarding {0}'s status token";
        }
        
        #endregion

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var token = gameEvent.GetProperty("token") as StatusToken;
            if (token != null)
            {
                if (token.Card.PersonalHonor == token)
                {
                    token.Card.MakeOrdinary();
                }
                LogExecution("Discarded {0} status token from {1}", token.Honored ? "honor" : "dishonor", token.Card.name);
                return true;
            }
            return false;
        }
    }
}
