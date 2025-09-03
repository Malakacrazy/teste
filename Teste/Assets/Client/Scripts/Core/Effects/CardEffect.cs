using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class CardEffect : Effect
    {
        public string TargetController { get; protected set; }
        public string TargetLocation { get; protected set; }

        public CardEffect(Game game, BaseCard source, EffectProperties properties, IEffect effect) 
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

        public override bool IsValidTarget(object target)
        {
            if (target == Match)
            {
                // This is a hack to check whether this is a lasting effect
                return true;
            }

            var card = target as BaseCard;
            if (card == null) return false;

            return card.AllowGameAction("applyEffect", Context) &&
                   (TargetController != Players.Self || card.Controller == Source.Controller) &&
                   (TargetController != Players.Opponent || card.Controller != Source.Controller);
        }

        public override List<object> GetTargets()
        {
            if (TargetLocation == Locations.Any)
            {
                return Game.AllCards.Where(card => Match(card, Context)).Cast<object>().ToList();
            }
            else if (TargetLocation == Locations.Provinces)
            {
                var cards = Game.AllCards.Where(card => card.IsInProvince());
                return cards.Where(card => Match(card, Context)).Cast<object>().ToList();
            }
            else if (TargetLocation == Locations.PlayArea)
            {
                return Game.FindAnyCardsInPlay(card => Match(card, Context)).Cast<object>().ToList();
            }
            
            return Game.AllCards
                .Where(card => Match(card, Context) && card.Location == TargetLocation)
                .Cast<object>()
                .ToList();
        }
    }
}
