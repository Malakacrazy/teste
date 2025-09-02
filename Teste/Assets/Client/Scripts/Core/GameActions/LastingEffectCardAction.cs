using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ILastingEffectCardProperties : ILastingEffectGeneralProperties
    {
        object TargetLocation { get; set; } // Can be Locations or Locations[]
    }

    public class LastingEffectCardProperties : CardGameAction.CardActionProperties, ILastingEffectCardProperties
    {
        public object TargetLocation { get; set; } // Can be Locations or Locations[]
        
        // Properties from LastingEffectGeneralProperties
        public string Duration { get; set; }
        public Func<AbilityContext, bool> Condition { get; set; }
        public string Until { get; set; }
        public object Effect { get; set; }
        
        public new GameAction ParentAction { get; set; }
    }

    public partial class LastingEffectCardAction : CardGameAction
    {
        #region Constructors
        
        public LastingEffectCardAction() : base()
        {
            Initialize();
        }
        
        public LastingEffectCardAction(CardGameAction.CardActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public LastingEffectCardAction(System.Func<AbilityContext, CardGameAction.CardActionProperties> factory) : base(factory)
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
            effectMessage = "apply a lasting effect to {0}";
            
            defaultProperties = new LastingEffectCardProperties
            {
                Duration = Durations.UntilEndOfConflict,
                Effect = new List<object>()
            };
        }
        
        #endregion

        protected ILastingEffectCardProperties GetProperties(AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ILastingEffectCardProperties;
            
            if (properties.Effect != null && !(properties.Effect is IList<object>))
            {
                properties.Effect = new List<object> { properties.Effect };
            }
            
            return properties;
        }

        public override bool CanAffect(object target, AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var card = target as BaseCard;
            if (card == null) return false;
            
            var properties = GetProperties(context, additionalProperties);
            var effectList = properties.Effect as IList<object>;
            
            if (effectList != null)
            {
                // Convert effect factories to actual effects
                var effects = effectList.Select(factory => 
                {
                    if (factory is Func<Game, EffectSource, object, object> effectFactory)
                    {
                        return effectFactory(context.Game, context.Source as EffectSource, properties);
                    }
                    return factory;
                }).ToList();

                properties.Effect = effects;
            }

            var lastingEffectRestrictions = card.GetEffects(EffectNames.CannotApplyLastingEffects);
            
            return base.CanAffect(target, context, additionalProperties) && 
                   (effectList?.Any(props => 
                   {
                       // Assuming props has an Effect property that can be checked
                       var effect = GetEffectFromProps(props);
                       return (effect as dynamic)?.CanBeApplied(card) == true && 
                              !lastingEffectRestrictions.Any(condition => 
                              {
                                  if (condition is Func<object, bool> conditionFunc)
                                  {
                                      return conditionFunc(effect);
                                  }
                                  return false;
                              });
                   }) ?? false);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            if (card != null)
            {
                var properties = GetProperties(gameEvent.context, additionalProperties);
                var lastingEffectRestrictions = card.GetEffects(EffectNames.CannotApplyLastingEffects);
                
                var effectProperties = new
                {
                    match = card,
                    location = Locations.Any,
                    duration = properties.Duration,
                    condition = properties.Condition,
                    until = properties.Until,
                    targetLocation = properties.TargetLocation
                };

                var effectList = properties.Effect as IList<object>;
                if (effectList != null)
                {
                    var effects = effectList.Select(factory =>
                    {
                        if (factory is Func<Game, EffectSource, object, object> effectFactory)
                        {
                            return effectFactory(gameEvent.context.Game, gameEvent.context.Source as EffectSource, effectProperties);
                        }
                        return factory;
                    }).ToList();

                    var filteredEffects = effects.Where(props =>
                    {
                        var effect = GetEffectFromProps(props);
                        return (effect as dynamic)?.CanBeApplied(card) == true &&
                               !lastingEffectRestrictions.Any(condition =>
                               {
                                   if (condition is Func<object, bool> conditionFunc)
                                   {
                                       return conditionFunc(effect);
                                   }
                                   return false;
                               });
                    }).ToList();

                    foreach (var effect in filteredEffects)
                    {
                        var gameEffect = GetEffectFromProps(effect) as GameEffect;
                        if (gameEffect != null)
                        {
                            gameEvent.context.Game.EffectEngine.Add(gameEffect);
                        }
                    }
                    
                    LogExecution("Applied {0} lasting effects to {1} for duration {2}", filteredEffects.Count(), card.name, properties.Duration);
                    return filteredEffects.Count > 0;
                }
            }
            return false;
        }

        private object GetEffectFromProps(object props)
        {
            // This method should extract the effect from the properties object
            // Implementation depends on your property structure
            if (props != null)
            {
                var type = props.GetType();
                var effectProperty = type.GetProperty("Effect") ?? type.GetProperty("effect");
                return effectProperty?.GetValue(props);
            }
            return null;
        }
    }
}
