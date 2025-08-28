using System;
using System.Collections.Generic;
using System.Linq;

namespace L5RGame
{
    /// <summary>
    /// Character card that can participate in conflicts
    /// </summary>
    public partial class DrawCard : BaseCard
    {
        [UnityEngine.Header("Character Stats")]
        public int militarySkill = 0;
        public int politicalSkill = 0;
        public int glory = 0;
        public int fate = 0;

        [UnityEngine.Header("Character Properties")]
        public bool isUnique = false;
        public string clan = "";
        public List<string> traits = new List<string>();
        
        // Combat state
        public bool isParticipatingInConflict = false;
        public bool isAttacking = false;
        public bool isDefending = false;

        public virtual void Initialize(BaseCard template)
        {
            // Initialize from template
            
            if (template is DrawCard drawTemplate)
            {
                militarySkill = drawTemplate.militarySkill;
                politicalSkill = drawTemplate.politicalSkill;
                glory = drawTemplate.glory;
                fate = drawTemplate.fate;
                isUnique = drawTemplate.isUnique;
                clan = drawTemplate.clan;
                traits = new List<string>(drawTemplate.traits);
            }
        }

        public int GetMilitarySkill()
        {
            int baseSkill = militarySkill;
            baseSkill += SumEffects(EffectNames.ModifyMilitarySkill);
            return UnityEngine.Mathf.Max(0, baseSkill);
        }

        public int GetPoliticalSkill()
        {
            int baseSkill = politicalSkill;
            baseSkill += SumEffects(EffectNames.ModifyPoliticalSkill);
            return UnityEngine.Mathf.Max(0, baseSkill);
        }

        public int GetGlory()
        {
            int baseGlory = glory;
            baseGlory += SumEffects(EffectNames.ModifyGlory);
            return UnityEngine.Mathf.Max(0, baseGlory);
        }

        public bool CanDeclareAsAttacker(string conflictType, Ring ring, BaseCard province)
        {
            if (isBowed) return false;
            if (GetSkillForConflictType(conflictType) <= 0) return false;
            
            var context = AbilityContext.CreateCardContext(game, this, controller);
            return !CheckRestrictions("declareAsAttacker", context);
        }

        private int GetSkillForConflictType(string conflictType)
        {
            return conflictType == "military" ? GetMilitarySkill() : GetPoliticalSkill();
        }

        public int GetContributionToImperialFavor()
        {
            return GetGlory();
        }

        public override string GetCardType()
        {
            return CardTypes.Character;
        }

        public bool HasTrait(string trait)
        {
            return traits.Contains(trait);
        }

        public void AddTrait(string trait)
        {
            if (!traits.Contains(trait))
                traits.Add(trait);
        }

        public void RemoveTrait(string trait)
        {
            traits.Remove(trait);
        }
    }

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

    /// <summary>
    /// Card type constants
    /// </summary>
    public static class CardTypesConstants
    {
        public const string Character = "character";
        public const string Event = "event";
        public const string Attachment = "attachment";
        public const string Holding = "holding";
        public const string Province = "province";
        public const string Stronghold = "stronghold";
        public const string Role = "role";
    }

    /// <summary>
    /// Effect names for card effects
    /// </summary>
    public static class EffectNamesConstants
    {
        public const string ModifyMilitarySkill = "modifyMilitarySkill";
        public const string ModifyPoliticalSkill = "modifyPoliticalSkill";
        public const string ModifyGlory = "modifyGlory";
        public const string ModifyProvinceStrength = "modifyProvinceStrength";
        public const string ModifyGloryForImperialFavor = "modifyGloryForImperialFavor";
        public const string FateCostToAttack = "fateCostToAttack";
        public const string ForceConflictUnopposed = "forceConflictUnopposed";
        public const string DoesNotBowAsAttacker = "doesNotBowAsAttacker";
        public const string DoesNotBowAsDefender = "doesNotBowAsDefender";
        public const string CannotBeBypassedByCovert = "cannotBeBypassedByCovert";
        public const string GainCovert = "gainCovert";
        public const string TakeControl = "takeControl";
        public const string Blank = "blank";
    }

    /// <summary>
    /// Event names for game events
    /// </summary>
    public static class EventNamesConstants
    {
        public const string OnConflictDeclared = "onConflictDeclared";
        public const string OnDefendersDeclared = "onDefendersDeclared";
        public const string OnConflictFinished = "onConflictFinished";
        public const string OnCovertResolved = "onCovertResolved";
        public const string OnClaimRing = "onClaimRing";
        public const string OnReturnHome = "onReturnHome";
        public const string OnParticipantsReturnHome = "onParticipantsReturnHome";
        public const string AfterConflict = "afterConflict";
    }

    /// <summary>
    /// Keywords used in the game
    /// </summary>
    public static class Keywords
    {
        public const string Limited = "limited";
        public const string Restricted = "restricted";
        public const string Covert = "covert";
        public const string Ancestral = "ancestral";
        public const string Pride = "pride";
        public const string Courtesy = "courtesy";
        public const string Sincerity = "sincerity";
    }

    /// <summary>
    /// Location names for cards
    /// </summary>
    public static class Locations
    {
        public const string Hand = "hand";
        public const string PlayArea = "play area";
        public const string DynastyDiscardPile = "dynasty discard pile";
        public const string ConflictDiscardPile = "conflict discard pile";
        public const string ProvinceOne = "province 1";
        public const string ProvinceTwo = "province 2";
        public const string ProvinceThree = "province 3";
        public const string ProvinceFour = "province 4";
        public const string StrongholdProvince = "stronghold province";
        public const string RemovedFromGame = "removed from game";
        public const string Provinces = "provinces";
        public const string Role = "role";
        public const string BeingPlayed = "being played";
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
        public const string ProvinceDeck = "province deck";
        public const string UnderneathStronghold = "underneath stronghold";
    }

    /// <summary>
    /// Player references for card conditions
    /// </summary>
    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
        public const string Any = "any";
    }

    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    // Backward compatibility aliases
    public static class CardTypes
    {
        public const string Character = CardTypesConstants.Character;
        public const string Event = CardTypesConstants.Event;
        public const string Attachment = CardTypesConstants.Attachment;
        public const string Holding = CardTypesConstants.Holding;
        public const string Province = CardTypesConstants.Province;
        public const string Stronghold = CardTypesConstants.Stronghold;
        public const string Role = CardTypesConstants.Role;
    }

    public static class EffectNames
    {
        public const string ModifyMilitarySkill = EffectNamesConstants.ModifyMilitarySkill;
        public const string ModifyPoliticalSkill = EffectNamesConstants.ModifyPoliticalSkill;
        public const string ModifyGlory = EffectNamesConstants.ModifyGlory;
        public const string ModifyProvinceStrength = EffectNamesConstants.ModifyProvinceStrength;
        public const string ModifyGloryForImperialFavor = EffectNamesConstants.ModifyGloryForImperialFavor;
        public const string FateCostToAttack = EffectNamesConstants.FateCostToAttack;
        public const string ForceConflictUnopposed = EffectNamesConstants.ForceConflictUnopposed;
        public const string DoesNotBowAsAttacker = EffectNamesConstants.DoesNotBowAsAttacker;
        public const string DoesNotBowAsDefender = EffectNamesConstants.DoesNotBowAsDefender;
        public const string CannotBeBypassedByCovert = EffectNamesConstants.CannotBeBypassedByCovert;
        public const string GainCovert = EffectNamesConstants.GainCovert;
        public const string TakeControl = EffectNamesConstants.TakeControl;
        public const string Blank = EffectNamesConstants.Blank;
        public const string ModifyConflictElementsToResolve = "modifyConflictElementsToResolve";
        public const string RestrictNumberOfDefenders = "restrictNumberOfDefenders";
        public const string AttachmentLimit = "attachmentLimit";
        public const string AttachmentMyControlOnly = "attachmentMyControlOnly";
        public const string AttachmentUniqueRestriction = "attachmentUniqueRestriction";
        public const string AttachmentFactionRestriction = "attachmentFactionRestriction";
        public const string AttachmentTraitRestriction = "attachmentTraitRestriction";
        public const string AdditionalTriggerCost = "additionalTriggerCost";
        public const string AdditionalPlayCost = "additionalPlayCost";
        public const string SetConflictTotalSkill = "setConflictTotalSkill";
        public const string ChangeConflictSkillFunction = "changeConflictSkillFunction";
        public const string CannotContribute = "cannotContribute";
        public const string ShowTopConflictCard = "showTopConflictCard";
        public const string EventsCannotBeCancelled = "eventsCannotBeCancelled";
        public const string ShowTopDynastyCard = "showTopDynastyCard";
    }
}
