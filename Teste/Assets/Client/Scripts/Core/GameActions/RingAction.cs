using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IRingActionProperties : IGameActionProperties
    {
    }

    public class RingActionProperties : GameAction.GameActionProperties, IRingActionProperties
    {
        public new List<object> Target { get; set; } = new List<object>();
        public new bool CannotBeCancelled { get; set; }
        public new bool Optional { get; set; }
        public new GameAction ParentAction { get; set; }
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

        protected bool CheckEventCondition(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Ring != null)
            {
                return CanAffect(gameEvent.Ring, gameEvent.Context, (GameActionProperties)additionalProperties);
            }
            return false;
        }

        public virtual bool CanAffect(Ring ring, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return base.CanAffect(ring, context, additionalProperties);
        }

        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            var ring = target as Ring;
            if (ring != null)
            {
                gameEvent.Ring = ring;
            }
        }
    }
}
