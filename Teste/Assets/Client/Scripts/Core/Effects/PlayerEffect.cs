using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class PlayerEffect : Effect
    {
        public string TargetController { get; protected set; }

        public PlayerEffect(Game game, BaseCard source, EffectProperties properties, IEffect effect) 
            : base(game, source, properties, effect)
        {
            TargetController = properties.TargetController ?? Players.Self;
            
            if (properties.Match == null)
            {
                properties.Match = (player, context) => true;
            }
        }

        public override bool IsValidTarget(object target)
        {
            var player = target as Player;
            if (player == null) return false;

            if (TargetController == Players.Self && player == Source.Controller.Opponent)
            {
                return false;
            }
            else if (TargetController == Players.Opponent && player == Source.Controller)
            {
                return false;
            }
            return true;
        }

        public override List<object> GetTargets()
        {
            return Game.GetPlayers()
                .Where(player => Match(player, Context))
                .Cast<object>()
                .ToList();
        }
    }
}
