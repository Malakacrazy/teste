using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class DetachedEffect : StaticEffect
    {
        private Func<object, AbilityContext, object, object> applyFunc;
        private Func<object, AbilityContext, object, object> unapplyFunc;
        private Dictionary<string, object> state;

        public DetachedEffect(string type, 
            Func<object, AbilityContext, object, object> applyFunc, 
            Func<object, AbilityContext, object, object> unapplyFunc) 
            : base(type)
        {
            this.applyFunc = applyFunc;
            this.unapplyFunc = unapplyFunc;
            this.state = new Dictionary<string, object>();
        }

        public override void Apply(object target)
        {
            var card = target as BaseCard;
            if (card == null) return;

            var currentState = state.ContainsKey(card.Uuid) ? state[card.Uuid] : null;
            state[card.Uuid] = applyFunc(target, Context, currentState);
        }

        public override void Unapply(object target)
        {
            var card = target as BaseCard;
            if (card == null) return;

            var currentState = state.ContainsKey(card.Uuid) ? state[card.Uuid] : null;
            state[card.Uuid] = unapplyFunc(target, Context, currentState);
        }

        public override void SetContext(AbilityContext context)
        {
            Context = context;
            foreach (var stateValue in state.Values)
            {
                var contextHolder = stateValue as IContextHolder;
                if (contextHolder != null)
                {
                    contextHolder.Context = context;
                }
            }
        }
    }

    public interface IContextHolder
    {
        AbilityContext Context { get; set; }
    }
}
