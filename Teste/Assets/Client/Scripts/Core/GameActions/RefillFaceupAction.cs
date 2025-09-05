using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IRefillFaceupProperties : IPlayerActionProperties
    {
        string Location { get; set; }
    }

    public class RefillFaceupProperties : PlayerActionProperties, IRefillFaceupProperties
    {
        public string Location { get; set; }
    }

    public partial class RefillFaceupAction : PlayerAction
    {
        #region Constructors
        
        public RefillFaceupAction() : base()
        {
            Initialize();
        }
        
        public RefillFaceupAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public RefillFaceupAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "refill";
            effectMessage = "refill its province faceup";
        }
        
        #endregion

        protected override List<object> DefaultTargets(AbilityContext context)
        {
            return new List<object> { context.Player };
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            if (player != null)
            {
                var properties = GetProperties(gameEvent.context, additionalProperties) as IRefillFaceupProperties;
                
                if (player.ReplaceDynastyCard(properties.Location))
                {
                    gameEvent.context.Game.QueueSimpleStep(() =>
                    {
                        var card = player.GetDynastyCardInProvince(properties.Location);
                        if (card != null)
                        {
                            card.Facedown = false;
                        }
                        return true;
                    });
                    LogExecution("Refilled {0} province faceup for {1}", properties.Location, player.name);
                    return true;
                }
            }
            return false;
        }
    }
}
