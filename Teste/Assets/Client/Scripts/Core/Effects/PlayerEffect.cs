using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class PlayerEffect : Effect
    {
        public string TargetController { get; protected set; }
        
        // Add missing properties and methods for compatibility
        protected Func<object, AbilityContext, bool> MatchFunction => GetMatchFunction();
        protected AbilityContext ContextValue => context;
        protected BaseCard SourceCard => source as BaseCard;
        protected Game GameInstance => game;
        
        private Func<object, AbilityContext, bool> GetMatchFunction()
        {
            // Return the match function from the base class or a default one
            return (target, ctx) => target != null;
        }

        public PlayerEffect(Game game, BaseCard source, EffectProperties properties, object effect) 
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

            if (TargetController == Players.Self && player == SourceCard.Controller.Opponent)
            {
                return false;
            }
            else if (TargetController == Players.Opponent && player == SourceCard.Controller)
            {
                return false;
            }
            return true;
        }

        public override List<object> GetTargets()
        {
            return GameInstance.GetPlayers()
                .Where(player => MatchFunction(player, ContextValue))
                .Cast<object>()
                .ToList();
        }
    }
}
