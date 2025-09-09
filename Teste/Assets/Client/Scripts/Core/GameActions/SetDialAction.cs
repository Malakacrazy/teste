using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface ISetDialProperties : IPlayerActionProperties
    {
        int Value { get; set; }
    }

    public class SetDialProperties : PlayerAction.PlayerActionProperties, ISetDialProperties
    {
        public int Value { get; set; }
        
        // IGameActionProperties implementation
        public List<object> Target { get; set; } = new List<object>();
        public bool CannotBeCancelled { get; set; }
        public bool Optional { get; set; }
        public GameAction ParentAction { get; set; }
        
        // IPlayerActionProperties implementation
        public Player PlayerTarget { get; set; }
    }

    public partial class SetDialAction : PlayerAction
    {
        #region Constructors
        
        public SetDialAction() : base()
        {
            Initialize();
        }
        
        public SetDialAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public SetDialAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "setDial";
            eventName = EventNames.OnSetHonorDial;
            
            var setDialProps = new SetDialProperties
            {
                Value = 0
            };
            // Create base GameActionProperties
            defaultProperties = new GameAction.GameActionProperties();
        }
        
        #endregion


        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ISetDialProperties;
            return ("set {0}'s dial to {1}", new object[] { properties.Target, properties.Value });
        }

        public bool CanAffect(Player player, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ISetDialProperties;
            return properties.Value > 0 && properties.Value < 6 && base.CanAffect(player, context);
        }

        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ISetDialProperties;
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            gameEvent.Value = properties.Value;
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var value = gameEvent.GetProperty("value") as int? ?? 0;
            if (player != null && value > 0)
            {
                player.SetShowBid(value);
                LogExecution("Set {0}'s dial to {1}", player.name, value);
                return true;
            }
            return false;
        }
    }
}
