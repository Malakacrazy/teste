using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Effect that applies to rings
    /// </summary>
    [System.Serializable]
    public class RingTargetEffect : Effect
    {
        [Header("Ring Effect Properties")]
        public Ring targetRing;
        
        public RingTargetEffect(Game game, BaseCard sourceCard, EffectProperties properties, object effectImplementation) 
            : base(game, sourceCard, properties, effectImplementation)
        {
            // Constructor implementation handled by base class
        }
        
        public RingTargetEffect() : base(null, null, new EffectProperties(), null)
        {
            // Default constructor for serialization
        }
        
        public override bool IsValidTarget(object target)
        {
            return target is Ring ring && (targetRing == null || ring == targetRing);
        }
        
        protected object GetTargetContext(object target)
        {
            if (target is Ring ring)
            {
                return new RingTargetEffectContext 
                { 
                    ring = ring, 
                    game = game, 
                    source = source 
                };
            }
            // GetTargetContext is not available in base class, return null
            return null;
        }
    }
    
    /// <summary>
    /// Context for ring target effects
    /// </summary>
    [System.Serializable]
    public class RingTargetEffectContext
    {
        public Ring ring;
        public Game game;
        public EffectSource source;
    }
}
