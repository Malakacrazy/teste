using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IDiscardFavorProperties : IPlayerActionProperties
    {
    }

    public class DiscardFavorProperties : PlayerActionProperties, IDiscardFavorProperties
    {
    }

    public partial class DiscardFavorAction : PlayerAction
    {
        #region Constructors
        
        public DiscardFavorAction() : base()
        {
            Initialize();
        }
        
        public DiscardFavorAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DiscardFavorAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "discardFavor";
            eventName = EventNames.OnDiscardFavor;
            costMessage = "discarding the Imperial Favor";
            effectMessage = "make {0} lose the Imperial Favor";
        }
        
        #endregion

        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var player = target as Player;
            if (player == null) return false;
            
            return player.ImperialFavor && base.CanAffect(target, context, additionalProperties);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            if (player != null && player.ImperialFavor)
            {
                player.LoseImperialFavor();
                LogExecution("{0} lost the Imperial Favor", player.name);
                return true;
            }
            return false;
        }
    }
}
