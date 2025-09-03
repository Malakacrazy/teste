using System;
using UnityEngine;

namespace L5RGame
{
    public static class EffectBuilder
    {
        public static class Card
        {
            public static Func<Game, BaseCard, EffectProperties, Effect> Static(string type, object value)
            {
                return (game, source, props) => new CardEffect(game, source, props, new StaticEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Dynamic(string type, Func<object, AbilityContext, object> value)
            {
                return (game, source, props) => new CardEffect(game, source, props, new DynamicEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Detached(string type, DetachedValue value)
            {
                return (game, source, props) => new CardEffect(game, source, props, new DetachedEffect(type, value.Apply, value.Unapply));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Flexible(string type, object value)
            {
                if (value is Func<object, AbilityContext, object> func)
                {
                    return Dynamic(type, func);
                }
                return Static(type, value);
            }
        }

        public static class Player
        {
            public static Func<Game, BaseCard, EffectProperties, Effect> Static(string type, object value)
            {
                return (game, source, props) => new PlayerEffect(game, source, props, new StaticEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Dynamic(string type, Func<object, AbilityContext, object> value)
            {
                return (game, source, props) => new PlayerEffect(game, source, props, new DynamicEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Detached(string type, DetachedValue value)
            {
                return (game, source, props) => new PlayerEffect(game, source, props, new DetachedEffect(type, value.Apply, value.Unapply));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Flexible(string type, object value)
            {
                if (value is Func<object, AbilityContext, object> func)
                {
                    return Dynamic(type, func);
                }
                return Static(type, value);
            }
        }

        public static class Conflict
        {
            public static Func<Game, BaseCard, EffectProperties, Effect> Static(string type, object value)
            {
                return (game, source, props) => new ConflictEffect(game, source, props, new StaticEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Dynamic(string type, Func<object, AbilityContext, object> value)
            {
                return (game, source, props) => new ConflictEffect(game, source, props, new DynamicEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Detached(string type, DetachedValue value)
            {
                return (game, source, props) => new ConflictEffect(game, source, props, new DetachedEffect(type, value.Apply, value.Unapply));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Flexible(string type, object value)
            {
                if (value is Func<object, AbilityContext, object> func)
                {
                    return Dynamic(type, func);
                }
                return Static(type, value);
            }
        }

        public static class Ring
        {
            public static Func<Game, BaseCard, EffectProperties, Effect> Static(string type, object value)
            {
                return (game, source, props) => new RingEffect(game, source, props, new StaticEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Dynamic(string type, Func<object, AbilityContext, object> value)
            {
                return (game, source, props) => new RingEffect(game, source, props, new DynamicEffect(type, value));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Detached(string type, DetachedValue value)
            {
                return (game, source, props) => new RingEffect(game, source, props, new DetachedEffect(type, value.Apply, value.Unapply));
            }

            public static Func<Game, BaseCard, EffectProperties, Effect> Flexible(string type, object value)
            {
                if (value is Func<object, AbilityContext, object> func)
                {
                    return Dynamic(type, func);
                }
                return Static(type, value);
            }
        }
    }

    public class DetachedValue
    {
        public Func<object, AbilityContext, object, object> Apply { get; set; }
        public Func<object, AbilityContext, object, object> Unapply { get; set; }
    }
}
