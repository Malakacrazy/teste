using UnityEngine;

namespace L5RGame
{
    public interface IMoveTokenProperties : ITokenActionProperties
    {
        DrawCard Recipient { get; set; }
    }

    public class MoveTokenProperties : TokenActionProperties, IMoveTokenProperties
    {
        public DrawCard Recipient { get; set; }
    }

    public partial class MoveTokenAction : TokenAction
    {
        #region Constructors
        
        public MoveTokenAction() : base()
        {
            Initialize();
        }
        
        public MoveTokenAction(TokenActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public MoveTokenAction(System.Func<AbilityContext, TokenActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "moveStatusToken";
            eventName = EventNames.OnStatusTokenMoved;
        }
        
        #endregion

        public override (string, object[]) GetEffectMessage(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IMoveTokenProperties;
            var target = properties.Target as StatusToken;
            return ("move {0}'s status token to {1}", new object[] { target?.Card, properties.Recipient });
        }

        public override bool CanAffect(StatusToken token, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context) as IMoveTokenProperties;
            if (properties.Recipient == null || properties.Recipient.Location != Locations.PlayArea)
            {
                return false;
            }
            else if (token.Honored && (properties.Recipient.IsHonored || !properties.Recipient.CheckRestrictions("receiveHonorToken", context)))
            {
                return false;
            }
            else if (token.Dishonored && (properties.Recipient.IsDishonored || !properties.Recipient.CheckRestrictions("receiveDishonorToken", context)))
            {
                return false;
            }
            return base.CanAffect(token, context, additionalProperties);
        }

        protected override void AddPropertiesToEvent(object eventObj, StatusToken token, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context) as IMoveTokenProperties;
            base.AddPropertiesToEvent(eventObj, token, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Recipient = properties.Recipient;
            }
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var token = gameEvent.GetProperty("token") as StatusToken;
            var recipient = gameEvent.GetProperty("recipient") as DrawCard;
            
            if (token != null && recipient != null)
            {
                if (token.Card.PersonalHonor == token)
                {
                    token.Card.MakeOrdinary();
                    
                    if ((recipient.IsHonored && token.Dishonored) || 
                        (recipient.IsDishonored && token.Honored))
                    {
                        recipient.MakeOrdinary();
                    }
                    else if (recipient.PersonalHonor == null)
                    {
                        recipient.SetPersonalHonor(token);
                    }
                }
                LogExecution("Moved {0} token from {1} to {2}", token.Honored ? "honor" : "dishonor", token.Card.name, recipient.name);
                return true;
            }
            return false;
        }
    }
}
