using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Takes control of opponent's cards
    /// </summary>
    [System.Serializable]
    public partial class TakeControlAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to taking control
        /// </summary>
        [System.Serializable]
        public class TakeControlProperties : CardActionProperties
        {
            public string duration = Durations.Custom;
            public string targetLocation = Locations.PlayArea;
            public object effect;
            public Dictionary<string, System.Func<GameEvent, bool>> until;
            
            public TakeControlProperties() : base() { }
            
            public TakeControlProperties(string duration) : base()
            {
                this.duration = duration;
            }
        }
        
        #region Constructors
        
        public TakeControlAction() : base()
        {
            Initialize();
        }
        
        public TakeControlAction(TakeControlProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TakeControlAction(System.Func<AbilityContext, TakeControlProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "takeControl";
            eventName = EventNames.OnTakeControl;
            effectMessage = "take control of {0}";
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Holding 
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new TakeControlProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is TakeControlProperties controlProps)
            {
                // Set default effect if not specified
                if (controlProps.effect == null)
                {
                    controlProps.effect = EffectEngine.TakeControl(controlProps.target, context.player);
                }
                return controlProps;
            }
                
            // Convert base properties to TakeControlProperties
            return new TakeControlProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                effect = EffectEngine.TakeControl(baseProps.target, context.player)
            };
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is DrawCard card))
                return false;
                
            // Cannot take control if another unique copy is already in play under player's control
            if (card.IsUnique() && card.AnotherUniqueInPlay(context.player))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as DrawCard;
            var context = gameEvent.context;
            var properties = GetProperties(context, additionalProperties);
            
            if (card != null && context?.source != null)
            {
                // Create lasting effect for taking control
                var effectProperties = new Dictionary<string, object>
                {
                    { "match", card },
                    { "duration", properties.duration },
                    { "effect", properties.effect }
                };
                
                if (properties.until != null)
                {
                    effectProperties["until"] = properties.until;
                }
                
                // Apply the lasting effect based on duration
                switch (properties.duration)
                {
                    case Durations.UntilEndOfTurn:
                        context.source.UntilEndOfTurn(() => effectProperties);
                        break;
                    case Durations.UntilEndOfPhase:
                        context.source.UntilEndOfPhase(() => effectProperties);
                        break;
                    case Durations.UntilEndOfConflict:
                        context.source.UntilEndOfConflict(() => effectProperties);
                        break;
                    case Durations.Custom:
                        context.source.CustomDuration(() => effectProperties);
                        break;
                    case Durations.Persistent:
                        context.source.PersistentEffect(() => effectProperties);
                        break;
                }
                
                LogExecution("{0} took control of {1}", context.player.name, card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Take control until end of turn
        /// </summary>
        public static TakeControlAction UntilEndOfTurn(DrawCard card = null)
        {
            var action = new TakeControlAction(new TakeControlProperties(Durations.UntilEndOfTurn));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Take control until end of conflict
        /// </summary>
        public static TakeControlAction UntilEndOfConflict(DrawCard card = null)
        {
            var action = new TakeControlAction(new TakeControlProperties(Durations.UntilEndOfConflict));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Take control until end of phase
        /// </summary>
        public static TakeControlAction UntilEndOfPhase(DrawCard card = null)
        {
            var action = new TakeControlAction(new TakeControlProperties(Durations.UntilEndOfPhase));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Take permanent control
        /// </summary>
        public static TakeControlAction Permanent(DrawCard card = null)
        {
            var action = new TakeControlAction(new TakeControlProperties(Durations.Persistent));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Take control with custom duration condition
        /// </summary>
        public static TakeControlAction WithCondition(Dictionary<string, System.Func<GameEvent, bool>> until, DrawCard card = null)
        {
            var action = new TakeControlAction(new TakeControlProperties(Durations.Custom) { until = until });
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        #endregion
    }
}