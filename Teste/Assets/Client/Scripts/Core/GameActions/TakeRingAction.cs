using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITakeRingProperties : IRingActionProperties
    {
        bool TakeFate { get; set; }
    }

    public class TakeRingProperties : RingActionProperties, ITakeRingProperties
    {
        public bool TakeFate { get; set; }
    }

    public partial class TakeRingAction : RingAction
    {
        #region Constructors
        
        public TakeRingAction() : base()
        {
            Initialize();
        }
        
        public TakeRingAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TakeRingAction(System.Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "takeFate";
            eventName = EventNames.OnTakeRing;
            effectMessage = "take {0}";
        }
        
        protected override ITakeRingProperties DefaultProperties => new TakeRingProperties
        {
            TakeFate = true
        };
        
        #endregion

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            return ring.ClaimedBy != context.Player.Name && base.CanAffect(ring, context, additionalProperties);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var ring = gameEvent.GetProperty("ring") as Ring;
            if (ring != null)
            {
                var properties = GetProperties(gameEvent.context, additionalProperties) as ITakeRingProperties;
                var context = gameEvent.context;
                
                ring.ClaimRing(context.Player);
                ring.Contested = false;
                
                if (properties.TakeFate && context.Player.CheckRestrictions("takeFateFromRings", context))
                {
                    context.Game.AddMessage("{0} takes {1} fate from {2}", context.Player, ring.Fate, ring);
                    context.Player.ModifyFate(ring.Fate);
                    ring.RemoveFate();
                    LogExecution("Claimed {0} and took {1} fate", ring.name, ring.Fate);
                }
                else
                {
                    LogExecution("Claimed {0}", ring.name);
                }
                return true;
            }
            return false;
        }
    }
}
