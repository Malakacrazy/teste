using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITransferFateProperties : IPlayerActionProperties
    {
        int Amount { get; set; }
    }

    public class TransferFateProperties : PlayerActionProperties, ITransferFateProperties
    {
        public int Amount { get; set; }
    }

    public partial class TransferFateAction : PlayerAction
    {
        #region Constructors
        
        public TransferFateAction() : base()
        {
            Initialize();
        }
        
        public TransferFateAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TransferFateAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
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
        
        protected ITransferFateProperties DefaultProperties => new TransferFateProperties
        {
            Amount = 1
        };
        
        #endregion

        public override (string, object[]) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferFateProperties;
            return ("giving {1} fate to {2}", new object[] { properties.Amount, context.Player.Opponent });
        }

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferFateProperties;
            return ("take {1} fate from {0}", new object[] { properties.Target, properties.Amount });
        }

        public bool CanAffect(Player player, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferFateProperties;
            return player.Opponent != null && properties.Amount > 0 && 
                   player.Fate >= properties.Amount && base.CanAffect(player, context);
        }

        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferFateProperties;
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            var player = target as Player;
            gameEvent.Fate = properties.Amount;
            gameEvent.Origin = player;
            gameEvent.Recipient = player?.Opponent;
        }

        protected override bool CheckEventCondition(GameEvent eventObj, GameActionProperties additionalProperties = null)
        {
            return MoveFateEventCondition(eventObj);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var result = MoveFateEventHandler(gameEvent);
            if (result)
            {
                var amount = gameEvent.GetProperty("fate") as int? ?? 0;
                var origin = gameEvent.GetProperty("origin") as Player;
                var recipient = gameEvent.GetProperty("recipient") as Player;
                LogExecution("Transferred {0} fate from {1} to {2}", amount, origin?.name ?? "unknown", recipient?.name ?? "unknown");
            }
            return result;
        }
    }
}
