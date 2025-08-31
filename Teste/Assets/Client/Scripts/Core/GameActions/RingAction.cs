using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IRingActionProperties : IGameActionProperties
    {
    }

    public class RingActionProperties : GameActionProperties, IRingActionProperties
    {
    }

    public class RingAction : GameAction
    {
        #region Constructors
        
        protected RingAction() : base()
        {
            Initialize();
        }
        
        protected RingAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        protected RingAction(System.Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            targetTypes = new List<string> { "ring" };
        }
        
        #endregion

        public virtual List<Ring> DefaultTargets(AbilityContext context)
        {
            return context.Game.CurrentConflict != null 
                ? new List<Ring> { context.Game.CurrentConflict.Ring } 
                : new List<Ring>();
        }

        protected override bool CheckEventCondition(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Ring != null)
            {
                return CanAffect(gameEvent.Ring, gameEvent.Context, additionalProperties);
            }
            return false;
        }

        public virtual bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            return base.CanAffect(ring, context, additionalProperties);
        }

        protected virtual void AddPropertiesToEvent(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            base.AddPropertiesToEvent(eventObj, ring, context, additionalProperties);
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Ring = ring;
            }
        }
    }
}
