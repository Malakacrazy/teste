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
        
        private string GetRingName(string element)
        {
            switch (element?.ToLower())
            {
                case "air": return "Air";
                case "earth": return "Earth";
                case "fire": return "Fire";
                case "void": return "Void";
                case "water": return "Water";
                default: return element ?? "Unknown";
            }
        }

        public void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveElementProperties;
            var target = properties.Target as IList<Ring>;

            if (target != null && target.Count > 1)
            {
                var sortedRings = target.OrderBy(ring =>
                {
                    var ringContext = RingEffects.ContextFor(context.Player, ring.Element, false);
                    var aPriority = ringContext.Ability?.Priority ?? 0;
                    var bPriority = ringContext.Ability?.Priority ?? 0;
                    return context.Player.FirstPlayer ? aPriority - bPriority : bPriority - aPriority;
                }).ToList();

                // Merge additional properties
                var mergedProperties = additionalProperties ?? new GameAction.GameActionProperties();
                if (mergedProperties.GetType().GetProperty("optional") == null)
                {
                    mergedProperties = new GameAction.GameActionProperties { optional = false };
                }

                var effectObjects = sortedRings.Select(ring => new EffectChoice
                {
                    Title = GetRingName(ring.Element) + " Effect",
                    Handler = new Action(() => context.Game.OpenEventWindow(new List<GameEvent> { GetEvent(ring, context, mergedProperties) }))
                }).ToList();

                events.Add(context.Game.GetEvent(EventNames.Unnamed, new Dictionary<string, object>(), () => {
                    context.Game.OpenSimultaneousEffectWindow(effectObjects);
                    return true;
                }));
            }
            else if (target != null && target.Count > 0)
            {
                events.Add(GetEvent(target[0], context, additionalProperties));
            }
        }

        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveElementProperties;
            var ring = target as Ring;
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            {
                gameEvent.Player = properties.Player ?? context.Player;
                gameEvent.PhysicalRing = properties.PhysicalRing;
                
                // Handle optional property from additionalProperties
                if (additionalProperties?.GetType().GetProperty("optional") != null)
                {
                    var optionalProperty = additionalProperties.GetType().GetProperty("optional");
                    gameEvent.Optional = (bool)optionalProperty.GetValue(additionalProperties);
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
