using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public enum Direction
    {
        Decrease,
        Increase,
        Prompt
    }

    public interface IModifyBidProperties : IPlayerActionProperties
    {
        int Amount { get; set; }
        Direction Direction { get; set; }
    }

    public class ModifyBidProperties : GameAction.GameActionProperties, IModifyBidProperties
    {
        public int Amount { get; set; }
        public Direction Direction { get; set; }
        
        public new List<object> Target { get; set; } = new List<object>();
        public new bool CannotBeCancelled { get; set; }
        public new bool Optional { get; set; }
        public new GameAction ParentAction { get; set; }
        public Player PlayerTarget { get; set; }
    }

    public partial class ModifyBidAction : PlayerAction
    {
        #region Constructors
        
        public ModifyBidAction() : base()
        {
            Initialize();
        }
        
        public ModifyBidAction(PlayerActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ModifyBidAction(System.Func<AbilityContext, PlayerActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "modifyBid";
            eventName = EventNames.OnModifyBid;
            
            defaultProperties = new ModifyBidProperties
            {
                Amount = 1,
                Direction = Direction.Increase
            };
        }
        
        #endregion

        protected override List<object> DefaultTargets(AbilityContext context)
        {
            return new List<object> { context.Player };
        }

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IModifyBidProperties;
            if (properties.Direction == Direction.Prompt)
            {
                return ("modify their honor bid by {0}", new object[] { properties.Amount });
            }
            return ("{0} their bid by {1}", new object[] { properties.Direction.ToString().ToLower(), properties.Amount });
        }

        public bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var player = target as Player;
            if (player == null) return false;
            
            var properties = GetProperties(context, additionalProperties) as IModifyBidProperties;
            if (properties.Amount == 0 || (properties.Direction == Direction.Decrease && player.HonorBid == 0))
            {
                return false;
            }
            return base.CanAffect(target, context, additionalProperties);
        }

        public void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IModifyBidProperties;
            if (properties.Direction != Direction.Prompt)
            {
                base.AddEventsToArray(events, context);
                return;
            }

            var targets = properties.Target as IEnumerable<Player>;
            if (targets != null)
            {
                foreach (var player in targets)
                {
                    if (player.HonorBid == 0)
                    {
                        var gameEvent = GetEvent(player, context, additionalProperties) as GameEvent;
                        if (gameEvent != null)
                        {
                            gameEvent.Direction = Direction.Increase.ToString().ToLower();
                            context.Game.AddMessage("{0} chooses to increase their honor bid", player);
                            events.Add(gameEvent);
                        }
                    }
                    else
                    {
                        var choices = new List<string> { "Increase honor bid", "Decrease honor bid" };
                        Action<string> choiceHandler = choice =>
                        {
                            var gameEvent = GetEvent(player, context, additionalProperties) as GameEvent;
                            if (gameEvent != null)
                            {
                                if (choice == "Increase honor bid")
                                {
                                    context.Game.AddMessage("{0} chooses to increase their honor bid", player);
                                    gameEvent.Direction = Direction.Increase.ToString().ToLower();
                                }
                                else
                                {
                                    context.Game.AddMessage("{0} chooses to decrease their honor bid", player);
                                    gameEvent.Direction = Direction.Decrease.ToString().ToLower();
                                }
                                events.Add(gameEvent);
                            }
                        };

                        var promptProperties = new
                        {
                            context = context,
                            choices = choices,
                            choiceHandler = choiceHandler
                        };

                        context.Game.PromptWithHandlerMenu(player, promptProperties);
                    }
                }
            }
        }

        protected void AddPropertiesToEvent(object eventObj, Player player, AbilityContext context, GameAction.GameActionProperties additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as IModifyBidProperties;
            base.AddPropertiesToEvent(eventObj, player, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Amount = properties.Amount;
                gameEvent.Direction = properties.Direction.ToString().ToLower();
            }
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var player = gameEvent.GetProperty("player") as Player;
            var amount = gameEvent.GetProperty("amount") as int? ?? 0;
            var direction = gameEvent.GetProperty("direction") as Direction? ?? Direction.Increase;
            
            if (player != null && amount > 0)
            {
                if (direction == Direction.Increase)
                {
                    player.HonorBidModifier += amount;
                    LogExecution("Increased {0}'s honor bid by {1}", player.name, amount);
                }
                else
                {
                    player.HonorBidModifier -= amount;
                    LogExecution("Decreased {0}'s honor bid by {1}", player.name, amount);
                }
                return true;
            }
            return false;
        }
    }
}
