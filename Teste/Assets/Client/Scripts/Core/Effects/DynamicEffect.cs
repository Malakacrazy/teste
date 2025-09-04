using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Dynamic effect that evaluates its value using a function during runtime
    /// </summary>
    public class DynamicEffect : IEffect
    {
        public string Type { get; private set; }
        public EffectValue Value { get; private set; }
        public AbilityContext Context { get; set; }
        public string Duration { get; set; }
        public bool IsConditional { get; set; }
        
        private readonly Func<object, AbilityContext, object> valueFunction;

        public DynamicEffect(string type, Func<object, AbilityContext, object> value)
        {
            Type = type;
            valueFunction = value;
            Value = new EffectValue();
            Context = null;
            Duration = null;
        }

        public virtual void Apply(object target)
        {
            // Evaluate the dynamic value
            if (valueFunction != null && Context != null)
            {
                var dynamicValue = valueFunction(target, Context);
                Value.SetValue(dynamicValue);
            }
            
            Value.Apply(target);
        }

        public virtual void Unapply(object target)
        {
            Value.Unapply(target);
        }

        public virtual object GetValue()
        {
            return Value.GetValue();
        }

        public virtual bool Recalculate()
        {
            // Dynamic effects recalculate by re-evaluating their function
            // This is a simplified implementation
            return false;
        }

        public virtual void SetContext(AbilityContext context)
        {
            Context = context;
            Value?.SetContext(context);
        }

        public virtual object GetDebugInfo()
        {
            return new
            {
                type = Type,
                hasValueFunction = valueFunction != null,
                value = Value?.GetDebugInfo(),
                duration = Duration,
                isConditional = IsConditional
            };
        }
    }
}