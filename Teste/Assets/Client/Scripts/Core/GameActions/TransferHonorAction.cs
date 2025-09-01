using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITransferHonorProperties : IPlayerActionProperties
    {
        int Amount { get; set; }
        bool AfterBid { get; set; }
    }

    public class TransferHonorProperties : PlayerActionProperties, ITransferHonorProperties
    {
        public int Amount { get; set; }
        public bool AfterBid { get; set; }
    }

    public partial class TransferHonorAction : PlayerAction
    {
        #region Constructors
        
        public TransferHonorAction() : base()
        {
            Initialize();
        }
        
        public TransferHonorAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TransferHonorAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "takeHonor";
            eventName = EventNames.OnTransferHonor;
        }
        
        protected ITransferHonorProperties DefaultProperties => new TransferHonorProperties
        {
            Amount = 1,
            AfterBid = false
        };
        
        #endregion

        public override (string, object[]) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferHonorProperties;
            return ("giving {1} honor to {2}", new object[] { properties.Amount, context.Player.Opponent });
        }

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferHonorProperties;
            return ("take {1} honor from {0}", new object[] { properties.Target, properties.Amount });
        }

        public bool CanAffect(Player player, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferHonorProperties;
            return player.Opponent != null && properties.Amount > 0 && base.CanAffect(player, context);
        }

        protected void AddPropertiesToEvent(object eventObj, Player player, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferHonorProperties;
            base.AddPropertiesToEvent(eventObj, player, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Amount = properties.Amount;
                gameEvent.AfterBid = properties.AfterBid;
            }
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var amount = gameEvent.GetProperty("amount") as int? ?? 0;
            
            if (player != null && player.Opponent != null && amount > 0)
            {
                player.ModifyHonor(-amount);
                player.Opponent.ModifyHonor(amount);
                LogExecution("Transferred {0} honor from {1} to {2}", amount, player.name, player.Opponent.name);
                return true;
            }
            return false;
        }
    }
}
