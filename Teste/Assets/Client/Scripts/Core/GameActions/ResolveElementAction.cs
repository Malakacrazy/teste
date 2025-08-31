using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface IResolveElementProperties : IRingActionProperties
    {
        Ring PhysicalRing { get; set; }
        Player Player { get; set; }
    }

    public class ResolveElementProperties : RingActionProperties, IResolveElementProperties
    {
        public Ring PhysicalRing { get; set; }
        public Player Player { get; set; }
    }

    public partial class ResolveElementAction : RingAction
    {
        #region Constructors
        
        public ResolveElementAction() : base()
        {
            Initialize();
        }
        
        public ResolveElementAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ResolveElementAction(System.Func<AbilityContext, RingActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "resolveElement";
            eventName = EventNames.OnResolveRingElement;
            effectMessage = "resolve {0} effect";
        }
        
        #endregion

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveElementProperties;
            var target = properties.Target as IList<Ring>;

            if (target != null && target.Count > 1)
            {
                var sortedRings = target.OrderBy(ring =>
                {
                    var ringContext = RingEffects.ContextFor(context.Player, ring.Element);
                    var aPriority = ringContext.Ability.DefaultPriority;
                    var bPriority = ringContext.Ability.DefaultPriority;
                    return context.Player.FirstPlayer ? aPriority - bPriority : bPriority - aPriority;
                }).ToList();

                // Merge additional properties
                var mergedProperties = additionalProperties ?? new { };
                if (mergedProperties.GetType().GetProperty("optional") == null)
                {
                    mergedProperties = new { optional = false };
                }

                var effectObjects = sortedRings.Select(ring => new
                {
                    title = RingEffects.GetRingName(ring.Element) + " Effect",
                    handler = new Action(() => context.Game.OpenEventWindow(GetEvent(ring, context, mergedProperties)))
                }).ToList();

                events.Add(new GameEvent(EventNames.Unnamed, new { }, 
                    () => context.Game.OpenSimultaneousEffectWindow(effectObjects)));
            }
            else if (target != null && target.Count > 0)
            {
                events.Add(GetEvent(target[0], context, additionalProperties));
            }
        }

        protected override void AddPropertiesToEvent(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveElementProperties;
            base.AddPropertiesToEvent(eventObj, ring, context, additionalProperties);

            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Player = properties.Player ?? context.Player;
                gameEvent.PhysicalRing = properties.PhysicalRing;
                
                // Handle optional property from additionalProperties
                if (additionalProperties != null)
                {
                    var optionalProperty = additionalProperties.GetType().GetProperty("optional");
                    if (optionalProperty != null)
                    {
                        gameEvent.Optional = (bool)optionalProperty.GetValue(additionalProperties);
                    }
                }
            }
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var ring = gameEvent.GetProperty("ring") as Ring;
            var player = gameEvent.GetProperty("player") as Player;
            var optional = gameEvent.GetProperty("optional") as bool? ?? false;
            
            if (ring != null && player != null)
            {
                var ringContext = RingEffects.ContextFor(player, ring.Element, optional);
                gameEvent.context.Game.ResolveAbility(ringContext);
                LogExecution("Resolved {0} ring effect for {1}", ring.Element, player.name);
                return true;
            }
            return false;
        }
    }
}
