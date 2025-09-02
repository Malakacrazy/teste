using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITakeFateRingProperties : IRingActionProperties
    {
        int Amount { get; set; }
    }

    public class TakeFateRingProperties : RingActionProperties, ITakeFateRingProperties
    {
        public int Amount { get; set; }
    }

    public partial class TakeFateRingAction : RingAction
    {
        #region Constructors
        
        public TakeFateRingAction() : base()
        {
            Initialize();
        }
        
        public TakeFateRingAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TakeFateRingAction(System.Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "takeFate";
            eventName = EventNames.OnMoveFate;
        }
        
        protected ITakeFateRingProperties DefaultProperties => new TakeFateRingProperties
        {
            Amount = 1
        };
        
        #endregion

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITakeFateRingProperties;
            return ("take {1} fate from {0}", new object[] { properties.Target, properties.Amount });
        }

        public override bool CanAffect(Ring ring, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITakeFateRingProperties;
            return context.Player.CheckRestrictions("takeFateFromRings", context) &&
                   ring.Fate > 0 && properties.Amount > 0 && base.CanAffect(ring, context);
        }

        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITakeFateRingProperties;
            var ring = target as Ring;
            
            if (ring != null)
            {
                gameEvent.Fate = properties.Amount;
                gameEvent.Origin = ring;
                gameEvent.Context = context;
                gameEvent.Recipient = context.Player;
            }
        }

        protected bool CheckEventCondition(GameEvent eventObj)
        {
            return MoveFateEventCondition(eventObj);
        }

        protected bool IsEventFullyResolved(GameEvent gameEvent, Ring ring, AbilityContext context, GameActionProperties additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ITakeFateRingProperties;
            
            if (gameEvent != null)
            {
                return !gameEvent.Cancelled && 
                       gameEvent.Name == this.EventName && 
                       gameEvent.Fate == properties.Amount && 
                       gameEvent.Origin == ring && 
                       gameEvent.Recipient == context.Player;
            }
            
            return false;
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var result = MoveFateEventHandler(gameEvent);
            if (result)
            {
                var amount = gameEvent.GetProperty("fate") as int? ?? 0;
                var ring = gameEvent.GetProperty("origin") as Ring;
                LogExecution("Took {0} fate from {1}", amount, ring?.name ?? "ring");
            }
            return result;
        }
    }
}
