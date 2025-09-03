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
            Context = new AbilityContext();
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
