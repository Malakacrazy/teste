using System;
using UnityEngine;

namespace L5RGame
{
    public interface ISwitchConflictTypeProperties : IRingActionProperties
    {
        string TargetConflictType { get; set; }
    }

    public class SwitchConflictTypeProperties : RingActionProperties, ISwitchConflictTypeProperties
    {
        public string TargetConflictType { get; set; }
    }

    public partial class SwitchConflictTypeAction : RingAction
    {
        #region Constructors
        
        public SwitchConflictTypeAction() : base()
        {
            Initialize();
        }
        
        public SwitchConflictTypeAction(GameActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public SwitchConflictTypeAction(Func<AbilityContext, GameActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "switchConflictType";
            eventName = EventNames.OnSwitchConflictType;
            costMessage = "switching the conflict type from {0} to {1}";
            effectMessage = "switch the conflict type from {0} to {1}";
        }
        
        #endregion


        protected override ISwitchConflictTypeProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            return base.GetProperties(context, additionalProperties) as ISwitchConflictTypeProperties;
        }

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            if (context.Game.CurrentConflict == null)
            {
                return false;
            }
            
            var properties = GetProperties(context);
            return ring.ConflictType != properties.TargetConflictType;
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            if (gameEvent.context.game.currentConflict != null)
            {
                gameEvent.context.game.currentConflict.SwitchType();
                LogExecution("Switched conflict type");
                return true;
            }
            return false;
        }
    }
}
