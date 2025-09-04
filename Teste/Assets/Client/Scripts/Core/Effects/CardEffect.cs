using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class CardEffect : Effect
    {
        public string TargetController { get; protected set; }
        public string TargetLocation { get; protected set; }

        public CardEffect(Game game, BaseCard source, EffectProperties properties, object effect) 
            : base(game, source, properties, effect)
        {
            if (properties.Match == null)
            {
                properties.Match = (card, context) => card == context.Source;
                if (properties.Location == Locations.Any)
                {
                    properties.TargetLocation = Locations.Any;
                }
                else if (new[] { CardTypes.Province, CardTypes.Stronghold, CardTypes.Holding }
                    .Contains(source.Type))
                {
                    properties.TargetLocation = Locations.Provinces;
                }
            }
            
            TargetController = properties.TargetController ?? Players.Self;
            TargetLocation = properties.TargetLocation ?? Locations.PlayArea;
        }

        // Add missing properties and methods for compatibility
        protected Func<object, AbilityContext, bool> MatchFunction => GetMatchFunction();
        protected AbilityContext ContextValue => context;
        protected BaseCard SourceCard => source as BaseCard;
        protected Game GameInstance => game;
        
        private Func<object, AbilityContext, bool> GetMatchFunction()
        {
            // Return the match function from the base class or a default one
            return (target, ctx) => target != null;
        }

        public override bool IsValidTarget(object target)
        {
            if (target == MatchFunction)
            {
                // This is a hack to check whether this is a lasting effect
                return true;
            }

            var card = target as BaseCard;
            if (card == null) return false;

            return card.AllowGameAction("applyEffect", ContextValue) &&
                   (TargetController != Players.Self || card.Controller == SourceCard.Controller) &&
                   (TargetController != Players.Opponent || card.Controller != SourceCard.Controller);
        }

        public override List<object> GetTargets()
        {
            if (TargetLocation == Locations.Any)
            {
                return GameInstance.AllCards.Where(card => MatchFunction(card, ContextValue)).Cast<object>().ToList();
            }
            else if (TargetLocation == Locations.Provinces)
            {
                var cards = GameInstance.AllCards.Where(card => card.IsInProvince());
                return cards.Where(card => MatchFunction(card, ContextValue)).Cast<object>().ToList();
            }
            else if (TargetLocation == Locations.PlayArea)
            {
                return GameInstance.FindAnyCardsInPlay(card => MatchFunction(card, ContextValue)).Cast<object>().ToList();
            }
            
            return GameInstance.AllCards
                .Where(card => MatchFunction(card, ContextValue) && card.Location == TargetLocation)
                .Cast<object>()
                .ToList();
        }
    }
}
