using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class RingEffect : Effect
    {
        public RingEffect(Game game, BaseCard source, EffectProperties properties, IEffect effect) 
            : base(game, source, properties, effect)
        {
        }

        public override List<object> GetTargets()
        {
            return Game.Rings
                .Where(ring => Match(ring, Context))
                .Cast<object>()
                .ToList();
        }
    }
}
