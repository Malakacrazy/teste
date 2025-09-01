using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ILastingEffectGeneralProperties : IGameActionProperties
    {
        string Duration { get; set; }
        Func<AbilityContext, bool> Condition { get; set; }
        string Until { get; set; }
        object Effect { get; set; }
    }

    public interface ILastingEffectProperties : ILastingEffectGeneralProperties
    {
        string TargetController { get; set; }
    }

    public class LastingEffectGeneralProperties : GameActionProperties, ILastingEffectGeneralProperties
    {
        public string Duration { get; set; }
        public Func<AbilityContext, bool> Condition { get; set; }
        public string Until { get; set; }
        public object Effect { get; set; }
    }

    public class LastingEffectProperties : LastingEffectGeneralProperties, ILastingEffectProperties
    {
        public string TargetController { get; set; }
    }

    public partial class LastingEffectAction : GameAction
    {
        #region Constructors
        
        public LastingEffectAction() : base()
        {
            Initialize();
        }
        
        public LastingEffectAction(GameActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public LastingEffectAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory)
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
        
        protected ILastingEffectProperties DefaultProperties => new LastingEffectProperties
        {
            Duration = Durations.UntilEndOfConflict,
            Effect = new List<object>()
        };
        
        #endregion

        protected ILastingEffectProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ILastingEffectProperties;
            
            if (properties.Effect != null && !(properties.Effect is IList<object>))
            {
                properties.Effect = new List<object> { properties.Effect };
            }
            
            return properties;
        }

        public bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var effectList = properties.Effect as IList<object>;
            return effectList != null && effectList.Count > 0;
        }

        public void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            if (HasLegalTarget(context, additionalProperties))
            {
                events.Add(GetEvent(null, context, additionalProperties));
            }
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(gameEvent.context, additionalProperties);
            if (properties != null)
            {
                // This would need to be implemented based on your duration system
                // For example: gameEvent.context.Source.ApplyDuration(properties.Duration, () => properties);
                switch (properties.Duration)
                {
                    case Durations.UntilEndOfConflict:
                        gameEvent.context.Source.UntilEndOfConflict(() => properties);
                        break;
                    case Durations.UntilEndOfPhase:
                        gameEvent.context.Source.UntilEndOfPhase(() => properties);
                        break;
                    case Durations.UntilEndOfRound:
                        gameEvent.context.Source.UntilEndOfRound(() => properties);
                        break;
                    // Add other duration cases as needed
                }
                
                LogExecution("Applied general lasting effect with duration {0}", properties.Duration);
                return true;
            }
            return false;
        }
    }
}
