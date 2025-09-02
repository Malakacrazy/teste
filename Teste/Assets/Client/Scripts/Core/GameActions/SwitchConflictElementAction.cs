using System;
using UnityEngine;

namespace L5RGame
{
    public interface ISwitchConflictElementProperties : IRingActionProperties
    {
    }

    public class SwitchConflictElementProperties : RingActionProperties, ISwitchConflictElementProperties
    {
    }

    public partial class SwitchConflictElementAction : RingAction
    {
        #region Constructors
        
        public SwitchConflictElementAction() : base()
        {
            Initialize();
        }
        
        public SwitchConflictElementAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public SwitchConflictElementAction(Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "switchConflictElement";
            eventName = EventNames.OnSwitchConflictElement;
            costMessage = "switching the contested ring to {0}";
            effectMessage = "switch the contested ring to {0}";
        }
        
        #endregion

        public override bool CanAffect(Ring ring, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return ring.IsUnclaimed() && context.Game.IsDuringConflict() && 
                   base.CanAffect(ring, context, additionalProperties);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var ring = gameEvent.GetProperty("ring") as Ring;
            if (ring != null && gameEvent.context.game.currentConflict != null)
            {
                gameEvent.context.game.currentConflict.SwitchElement(ring.Element);
                LogExecution("Switched conflict element to {0}", ring.Element);
                return true;
            }
            return false;
        }
    }
}
