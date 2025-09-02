using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ISelectRingProperties : IRingActionProperties
    {
        string ActivePromptTitle { get; set; }
        string Player { get; set; }
        bool Targets { get; set; }
        Func<Ring, AbilityContext, bool> RingCondition { get; set; }
        Action CancelHandler { get; set; }
        Func<Ring, object> SubActionProperties { get; set; }
        string Message { get; set; }
        Func<Ring, Player, object[]> MessageArgs { get; set; }
        GameAction GameAction { get; set; }
    }

    public class SelectRingProperties : RingActionProperties, ISelectRingProperties
    {
        public string ActivePromptTitle { get; set; }
        public string Player { get; set; }
        public bool Targets { get; set; }
        public Func<Ring, AbilityContext, bool> RingCondition { get; set; }
        public Action CancelHandler { get; set; }
        public Func<Ring, object> SubActionProperties { get; set; }
        public string Message { get; set; }
        public Func<Ring, Player, object[]> MessageArgs { get; set; }
        public GameAction GameAction { get; set; }
    }

    public partial class SelectRingAction : RingAction
    {
        protected ISelectRingProperties DefaultProperties => new SelectRingProperties
        {
            RingCondition = (ring, context) => true,
            SubActionProperties = ring => new { target = ring },
            GameAction = null
        };

        public SelectRingAction() : base()
        {
            Initialize();
        }

        public SelectRingAction(RingActionProperties properties) : base(properties)
        {
            Initialize();
        }

        public SelectRingAction(Func<AbilityContext, RingActionProperties> propertiesFactory) : base(propertiesFactory)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();
            actionName = "selectRing";
            eventName = EventNames.OnGameStateChanged;
        }

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("choose a ring for {0}", new object[] { properties.target });
        }

        public virtual bool CanAffect(Ring ring, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectRingProperties;
            if (properties.Player == Players.Opponent && context.Player.Opponent == null)
            {
                return false;
            }
            return base.CanAffect(ring, context) && properties.RingCondition(ring, context);
        }

        public bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return context.Game.Rings.Values.Any(ring => CanAffect(ring, context, additionalProperties));
        }

        public void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectRingProperties;
            
            if (properties.Player == Players.Opponent && context.Player.Opponent == null)
            {
                return;
            }
            else if (!context.Game.Rings.Values.Any(ring => properties.RingCondition(ring, context)))
            {
                return;
            }
            
            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;
            if (properties.Targets && context.ChoosingPlayerOverride != null)
            {
                player = context.ChoosingPlayerOverride;
            }
            
            var buttons = new List<object>();
            if (properties.CancelHandler != null)
            {
                buttons.Add(new { text = "Cancel", arg = "cancel" });
            }
            
            Action<Player, Ring> onSelect = (p, ring) =>
            {
                if (!string.IsNullOrEmpty(properties.Message))
                {
                    context.Game.AddMessage(properties.Message, properties.MessageArgs(ring, p));
                }
                properties.GameAction.AddEventsToArray(events, context, MergeProperties(additionalProperties, properties.SubActionProperties(ring)) as GameActionProperties);
            };
            
            var promptProperties = new Dictionary<string, object>
            {
                ["context"] = context,
                ["buttons"] = buttons,
                ["onCancel"] = properties.CancelHandler,
                ["onSelect"] = onSelect,
                ["activePromptTitle"] = properties.ActivePromptTitle,
                ["ringCondition"] = properties.RingCondition,
                ["targets"] = properties.Targets
            };
            
            context.Game.PromptForRingSelect(player, promptProperties);
        }

        public bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectRingProperties;
            return properties.Targets && properties.Player != Players.Opponent;
        }

        private object MergeProperties(GameActionProperties additionalProperties, object subActionProperties)
        {
            if (additionalProperties == null) return subActionProperties;
            if (subActionProperties == null) return additionalProperties;
            
            return new { additionalProperties, subActionProperties };
        }
    }
}
