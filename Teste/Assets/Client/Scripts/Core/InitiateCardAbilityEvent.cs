using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Event fired when a card ability is initiated
    /// </summary>
    public class InitiateCardAbilityEvent : GameEvent
    {
        public List<BaseCard> CardTargets { get; private set; }
        public List<Ring> RingTargets { get; private set; }
        public List<object> SelectTargets { get; private set; }
        public List<StatusToken> TokenTargets { get; private set; }
        public List<object> AllTargets { get; private set; }

        public InitiateCardAbilityEvent(Dictionary<string, object> parameters, Action handler = null) 
            : base("OnInitiateAbilityEffects", parameters, handler != null ? (gameEvent) => handler() : null)
        {
            InitializeTargets();
        }

        private void InitializeTargets()
        {
            // Initialize empty target lists since AbilityContext properties are not implemented
            CardTargets = new List<BaseCard>();
            RingTargets = new List<Ring>();
            SelectTargets = new List<object>();
            TokenTargets = new List<StatusToken>();
            
            // TODO: Implement proper target initialization when AbilityContext properties are available
            // if (Context?.Ability != null && !Context.Ability.DoesNotTarget)
            // {
            //     CardTargets = FlattenTargets(Context.Targets?.Values);
            //     RingTargets = FlattenRingTargets(Context.Rings?.Values);
            //     SelectTargets = FlattenSelectTargets(Context.Selects?.Values);
            //     TokenTargets = FlattenTokenTargets(Context.Tokens?.Values);
            // }

            AllTargets = new List<object>();
            AllTargets.AddRange(CardTargets.Cast<object>());
            AllTargets.AddRange(RingTargets.Cast<object>());
            AllTargets.AddRange(SelectTargets);
            AllTargets.AddRange(TokenTargets.Cast<object>());
        }

        private List<BaseCard> FlattenTargets(IEnumerable<object> targets)
        {
            if (targets == null) return new List<BaseCard>();
            
            var result = new List<BaseCard>();
            foreach (var target in targets)
            {
                if (target is BaseCard card)
                {
                    result.Add(card);
                }
                else if (target is IEnumerable<BaseCard> cardList)
                {
                    result.AddRange(cardList);
                }
                else if (target is IEnumerable<object> objectList)
                {
                    result.AddRange(objectList.OfType<BaseCard>());
                }
            }
            return result;
        }

        private List<Ring> FlattenRingTargets(IEnumerable<object> targets)
        {
            if (targets == null) return new List<Ring>();
            
            var result = new List<Ring>();
            foreach (var target in targets)
            {
                if (target is Ring ring)
                {
                    result.Add(ring);
                }
                else if (target is IEnumerable<Ring> ringList)
                {
                    result.AddRange(ringList);
                }
                else if (target is IEnumerable<object> objectList)
                {
                    result.AddRange(objectList.OfType<Ring>());
                }
            }
            return result;
        }

        private List<object> FlattenSelectTargets(IEnumerable<object> targets)
        {
            if (targets == null) return new List<object>();
            
            var result = new List<object>();
            foreach (var target in targets)
            {
                if (target is IEnumerable<object> enumerable)
                {
                    result.AddRange(enumerable);
                }
                else
                {
                    result.Add(target);
                }
            }
            return result;
        }

        private List<StatusToken> FlattenTokenTargets(IEnumerable<object> targets)
        {
            if (targets == null) return new List<StatusToken>();
            
            var result = new List<StatusToken>();
            foreach (var target in targets)
            {
                if (target is StatusToken token)
                {
                    result.Add(token);
                }
                else if (target is IEnumerable<StatusToken> tokenList)
                {
                    result.AddRange(tokenList);
                }
                else if (target is IEnumerable<object> objectList)
                {
                    result.AddRange(objectList.OfType<StatusToken>());
                }
            }
            return result;
        }
    }
}
