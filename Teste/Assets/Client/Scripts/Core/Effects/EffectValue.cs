using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class EffectValue
    {
        protected object value;
        public AbilityContext Context { get; set; }

        public EffectValue(object value = null)
        {
            this.value = value ?? true;
            // Create a dummy context properties object to satisfy constructor
            var dummyProps = new AbilityContextProperties
            {
                game = null,
                source = null,
                player = null,
                ability = null,
                costs = new Dictionary<string, object>(),
                targets = new Dictionary<string, object>(),
                rings = new Dictionary<string, object>(),
                selects = new Dictionary<string, object>(),
                tokens = new Dictionary<string, object>()
            };
            Context = new AbilityContext(dummyProps);
        }

        public virtual void SetValue(object value)
        {
            this.value = value;
        }

        public virtual object GetValue()
        {
            return value;
        }

        public virtual void SetContext(AbilityContext context)
        {
            Context = context;
        }

        public virtual void Reset()
        {
            // Base implementation - override in derived classes if needed
        }

        public virtual void Apply(object target)
        {
            // Base implementation - override in derived classes if needed
        }

        public virtual void Unapply(object target)
        {
            // Base implementation - override in derived classes if needed
        }

        public virtual object GetDebugInfo()
        {
            return new { value = value, context = Context != null };
        }
    }
}
