using System;
using System.Collections.Generic;
using System.Linq;

namespace L5RGame
{
    /// <summary>
    /// Province card that can be attacked
    /// </summary>
    public partial class ProvinceCard : BaseCard
    {
        [UnityEngine.Header("Province Properties")]
        public int strength = 3;
        public string element = "";
        public bool isFaceup = false;
        
        // Province state
        public bool canBeAttacked = true;
        public List<BaseCard> dynastyCards = new List<BaseCard>();

        public virtual void Initialize(BaseCard template)
        {
            // Initialize from template
            
            if (template is ProvinceCard provinceTemplate)
            {
                strength = provinceTemplate.strength;
                element = provinceTemplate.element;
                isFaceup = provinceTemplate.isFaceup;
                canBeAttacked = provinceTemplate.canBeAttacked;
            }

            isProvince = true;
        }

        public int GetStrength()
        {
            int baseStrength = strength;
            baseStrength += SumEffects(EffectNames.ModifyProvinceStrength);
            return UnityEngine.Mathf.Max(0, baseStrength);
        }

        public bool CanBeAttacked()
        {
            if (isBroken) return false;
            if (!canBeAttacked) return false;
            
            var context = AbilityContext.CreateCardContext(game, this, controller);
            return !CheckRestrictions("beAttacked", context);
        }

        public void BreakProvince()
        {
            if (!isBroken)
            {
                isBroken = true;
                game.AddMessage("{0} is broken!", name);
                
                // Move dynasty cards to discard
                foreach (var card in dynastyCards.ToList())
                {
                    if (card != null)
                    {
                        controller.MoveCard(card, Locations.DynastyDiscardPile);
                    }
                }
                dynastyCards.Clear();
            }
        }

        public override string GetCardType()
        {
            return CardTypes.Province;
        }
    }
}