using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class ConflictEffect : Effect
    {
        public ConflictEffect(Game game, BaseCard source, EffectProperties properties, IEffect effect) 
            : base(game, source, properties, effect)
        {
            // Override any erroneous match passed through properties
            properties.Match = (conflict, context) => true;
        }

        public override List<object> GetTargets()
        {
            return Game.CurrentConflict != null ? new List<object> { Game.CurrentConflict } : new List<object>();
        }
    }
}
