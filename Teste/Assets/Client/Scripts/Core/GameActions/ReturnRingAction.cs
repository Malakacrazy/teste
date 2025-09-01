using System;
using UnityEngine;

namespace L5RGame
{
    public interface IReturnRingProperties : IRingActionProperties
    {
    }

    public class ReturnRingProperties : RingActionProperties, IReturnRingProperties
    {
    }

    public partial class ReturnRingAction : RingAction
    {
        #region Constructors
        
        public ReturnRingAction() : base()
        {
            Initialize();
        }
        
        public ReturnRingAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ReturnRingAction(System.Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "returnRing";
            eventName = EventNames.OnReturnRing;
            effectMessage = "return {0} to the unclaimed pool";
        }
        
        #endregion

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            return !ring.IsUnclaimed() && base.CanAffect(ring, context, additionalProperties);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var ring = gameEvent.GetProperty("ring") as Ring;
            if (ring != null)
            {
                ring.ResetRing();
                LogExecution("Returned {0} ring to the unclaimed pool", ring.Element);
                return true;
            }
            return false;
        }
    }
}
