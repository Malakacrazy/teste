using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface ILastingEffectRingProperties : ILastingEffectGeneralProperties
    {
    }

    public class LastingEffectRingProperties : LastingEffectGeneralProperties, ILastingEffectRingProperties
    {
    }

    public partial class LastingEffectRingAction : RingAction
    {
        #region Constructors
        
        public LastingEffectRingAction() : base()
        {
            Initialize();
        }
        
        public LastingEffectRingAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public LastingEffectRingAction(System.Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "applyLastingEffect";
            eventName = EventNames.OnEffectApplied;
            effectMessage = "apply a lasting effect";
        }
        
        protected ILastingEffectRingProperties DefaultProperties => new LastingEffectRingProperties
        {
            Duration = Durations.UntilEndOfConflict,
            Effect = new List<object>()
        };
        
        #endregion

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var ring = gameEvent.GetProperty("ring") as Ring;
            if (ring != null)
            {
                var properties = GetProperties(gameEvent.context, additionalProperties) as ILastingEffectRingProperties;
                
                var effectProperties = new
                {
                    match = ring,
                    duration = properties.Duration,
                    condition = properties.Condition,
                    until = properties.Until,
                    effect = properties.Effect
                };

                // Apply the lasting effect based on duration
                switch (properties.Duration)
                {
                    case Durations.UntilEndOfConflict:
                        gameEvent.context.Source.UntilEndOfConflict(() => effectProperties);
                        break;
                    case Durations.UntilEndOfPhase:
                        gameEvent.context.Source.UntilEndOfPhase(() => effectProperties);
                        break;
                    case Durations.UntilEndOfRound:
                        gameEvent.context.Source.UntilEndOfRound(() => effectProperties);
                        break;
                    // Add other duration cases as needed
                }
                
                LogExecution("Applied lasting effect to {0} for duration {1}", ring.name, properties.Duration);
                return true;
            }
            return false;
        }
    }
}
