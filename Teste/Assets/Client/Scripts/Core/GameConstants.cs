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
        public const string Any = "any";
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
        public const string Limbo = "limbo";
        public const string None = "none";
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

    /// <summary>
    /// Token types for game actions
    /// </summary>
    public static class TokenTypes
    {
        public const string Fate = "fate";
        public const string Honor = "honor";
        public const string Status = "status";
        public const string Dishonor = "dishonor";
        public const string Bow = "bow";
        public const string Ready = "ready";
    }


    /// <summary>
    /// When types for lasting effects
    /// </summary>
    public static class WhenType
    {
        public const string AtStartOfPhase = "atStartOfPhase";
        public const string AtEndOfPhase = "atEndOfPhase";
        public const string AtStartOfRound = "atStartOfRound";
        public const string AtEndOfRound = "atEndOfRound";
    }

    /// <summary>
    /// Card selector constants for targeting
    /// </summary>
    public static class CardSelectorConstants
    {
        public const string Any = "any";
        public const string Self = "self";
        public const string Others = "others";
    }

    /// <summary>
    /// Target modes for selection 
    /// </summary>
    public static class TargetModes
    {
        public const string Single = "single";
        public const string Multiple = "multiple";
        public const string UpTo = "upTo";
        public const string Exactly = "exactly";
        public const string Ability = "ability";
        public const string AutoSingle = "autoSingle";
        public const string ExactlyVariable = "exactlyVariable";
        public const string MaxStat = "maxStat";
        public const string Token = "token";
        public const string Unlimited = "unlimited";
        public const string UpToVariable = "upToVariable";
        public const string Ring = "ring";
        public const string Select = "select";
    }

    /// <summary>
    /// Ability types for different ability classifications
    /// </summary>
    public static class AbilityTypes
    {
        public const string Action = "action";
        public const string Reaction = "reaction";
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedReaction";
        public const string ForcedInterrupt = "forcedInterrupt";
        public const string WouldInterrupt = "wouldInterrupt";
        public const string CancelInterrupt = "cancelInterrupt";
        public const string Persistent = "persistent";
        public const string OtherEffects = "otherEffects";
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

    public static partial class EffectNames
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
        
        // Additional effect names for compilation
        public const string RestrictNumberOfDefenders = "restrictNumberOfDefenders";
        public const string SetConflictTotalSkill = "setConflictTotalSkill";
        public const string ChangeConflictSkillFunction = "changeConflictSkillFunction";
        public const string CannotContribute = "cannotContribute";
        public const string ModifyConflictElementsToResolve = "modifyConflictElementsToResolve";
        public const string AttachmentLimit = "attachmentLimit";
        public const string AttachmentMyControlOnly = "attachmentMyControlOnly";
        public const string RestrictHonorBid = "restrictHonorBid";
        public const string AddHonorBidOption = "addHonorBidOption";
        public const string ModifyCardsDrawnInDrawPhase = "modifyCardsDrawnInDrawPhase";
        public const string MaxCardsDrawnInDrawPhase = "maxCardsDrawnInDrawPhase";
        public const string ModifyFateCollectedInDynastyPhase = "modifyFateCollectedInDynastyPhase";
        public const string ModifyFateCollectionMultiplier = "modifyFateCollectionMultiplier";
        public const string AttachmentUniqueRestriction = "attachmentUniqueRestriction";
        public const string AttachmentFactionRestriction = "attachmentFactionRestriction";
        public const string AttachmentTraitRestriction = "attachmentTraitRestriction";
        public const string AdditionalTriggerCost = "additionalTriggerCost";
        public const string AdditionalPlayCost = "additionalPlayCost";
        public const string ShowTopConflictCard = "showTopConflictCard";
        public const string EventsCannotBeCancelled = "eventsCannotBeCancelled";
        public const string ShowTopDynastyCard = "showTopDynastyCard";
        public const string CannotApplyLastingEffects = "cannotApplyLastingEffects";
        public const string MustBeChosen = "mustBeChosen";
        
        // Additional effect names for StaticEffect class
        public const string CanBeSeenWhenFacedown = "canBeSeenWhenFacedown";
        public const string CannotParticipateAsAttacker = "cannotParticipateAsAttacker";
        public const string CannotParticipateAsDefender = "cannotParticipateAsDefender";
        public const string AbilityRestrictions = "abilityRestrictions";
        public const string DoesNotBow = "doesNotBow";
        public const string DoesNotReady = "doesNotReady";
        public const string ModifyBaseMilitarySkillMultiplier = "modifyBaseMilitarySkillMultiplier";
        public const string ModifyMilitarySkillMultiplier = "modifyMilitarySkillMultiplier";
        public const string ModifyBothSkills = "modifyBothSkills";
        public const string ModifyBasePoliticalSkillMultiplier = "modifyBasePoliticalSkillMultiplier";
        public const string ModifyPoliticalSkillMultiplier = "modifyPoliticalSkillMultiplier";
        public const string SetBaseMilitarySkill = "setBaseMilitarySkill";
        public const string SetBasePoliticalSkill = "setBasePoliticalSkill";
        public const string SetGlory = "setGlory";
        public const string SetMilitarySkill = "setMilitarySkill";
        public const string SetPoliticalSkill = "setPoliticalSkill";
        public const string HonorStatusDoesNotModifySkill = "honorStatusDoesNotModifySkill";
        public const string HonorStatusReverseModifySkill = "honorStatusReverseModifySkill";
        
        // Additional missing effect names for compilation
        public const string CannotBeCancelled = "cannotBeCancelled";
        public const string CannotHaveFateRemoved = "cannotHaveFateRemoved";
        public const string CannotBeDiscarded = "cannotBeDiscarded";
        public const string CannotBeBowed = "cannotBeBowed";
        public const string CannotBeTargeted = "cannotBeTargeted";
        public const string CannotBeMovedToConflict = "cannotBeMovedToConflict";
        public const string CannotLeavePlay = "cannotLeavePlay";
        public const string CannotPlay = "cannotPlay";
        public const string CannotTriggerAbilities = "cannotTriggerAbilities";
        public const string CannotInitiateKeywords = "cannotInitiateKeywords";
        public const string Dishonored = "dishonored";
        public const string Honored = "honored";
    }

    /// <summary>
    /// Duration constants for lasting effects (partial class continued from EffectSource.cs)
    /// </summary>
    public static partial class Durations
    {
        public const string UntilEndOfTurn = "untilEndOfTurn";
        
        // Duration factory methods that return objects with extension methods
        public static object UntilEndOfTurnEffect() => new DurationEffect(UntilEndOfTurn);
        public static object UntilEndOfPhaseEffect() => new DurationEffect(UntilEndOfPhase);
        public static object UntilEndOfConflictEffect() => new DurationEffect(UntilEndOfConflict);
        public static object UntilEndOfRoundEffect() => new DurationEffect(UntilEndOfRound);
    }

    /// <summary>
    /// Duration effect object with extension methods
    /// </summary>
    public class DurationEffect
    {
        public string Duration { get; }
        
        public DurationEffect(string duration)
        {
            Duration = duration;
        }
        
        public DurationEffect UntilEndOfTurn() => new DurationEffect(Durations.UntilEndOfTurn);
        public DurationEffect UntilEndOfPhase() => new DurationEffect(Durations.UntilEndOfPhase);
        public DurationEffect UntilEndOfConflict() => new DurationEffect(Durations.UntilEndOfConflict);
        public DurationEffect UntilEndOfRound() => new DurationEffect(Durations.UntilEndOfRound);
        public DurationEffect CustomDuration() => new DurationEffect(Durations.Custom);
        public DurationEffect PersistentEffect() => new DurationEffect(Durations.Persistent);
    }

    /// <summary>
    /// Extension methods for applying duration effects to objects
    /// </summary>
    public static class DurationEffectExtensions
    {
        /// <summary>
        /// Apply an effect until the end of the current conflict
        /// </summary>
        public static void UntilEndOfConflict(this object source, System.Func<object> effectProperties)
        {
            // Placeholder implementation - would integrate with game's effect system
            UnityEngine.Debug.Log($"Applied effect until end of conflict from {source?.GetType().Name}");
        }

        /// <summary>
        /// Apply an effect until the end of the current phase
        /// </summary>
        public static void UntilEndOfPhase(this object source, System.Func<object> effectProperties)
        {
            // Placeholder implementation - would integrate with game's effect system
            UnityEngine.Debug.Log($"Applied effect until end of phase from {source?.GetType().Name}");
        }

        /// <summary>
        /// Apply an effect until the end of the current round
        /// </summary>
        public static void UntilEndOfRound(this object source, System.Func<object> effectProperties)
        {
            // Placeholder implementation - would integrate with game's effect system
            UnityEngine.Debug.Log($"Applied effect until end of round from {source?.GetType().Name}");
        }

        /// <summary>
        /// Apply an effect until the end of the current turn
        /// </summary>
        public static void UntilEndOfTurn(this object source, System.Func<object> effectProperties)
        {
            // Placeholder implementation - would integrate with game's effect system
            UnityEngine.Debug.Log($"Applied effect until end of turn from {source?.GetType().Name}");
        }

        /// <summary>
        /// Apply a custom duration effect
        /// </summary>
        public static void CustomDuration(this object source, System.Func<object> effectProperties)
        {
            // Placeholder implementation - would integrate with game's effect system
            UnityEngine.Debug.Log($"Applied custom duration effect from {source?.GetType().Name}");
        }

        /// <summary>
        /// Apply a persistent effect
        /// </summary>
        public static void PersistentEffect(this object source, System.Func<object> effectProperties)
        {
            // Placeholder implementation - would integrate with game's effect system
            UnityEngine.Debug.Log($"Applied persistent effect from {source?.GetType().Name}");
        }
    }

    /// <summary>
    /// Missing constants for compilation
    /// </summary>
    public static class AbilityId
    {
        public const string WaterRing = "water_ring";
        public const string AirRing = "air_ring";
        public const string EarthRing = "earth_ring";
        public const string FireRing = "fire_ring";
        public const string VoidRing = "void_ring";
    }

    public static class CardLocation
    {
        public const string Hand = Locations.Hand;
        public const string PlayArea = Locations.PlayArea;
        public const string ConflictDeck = Locations.ConflictDeck;
        public const string DynastyDeck = Locations.DynastyDeck;
        public const string ConflictDiscardPile = Locations.ConflictDiscardPile;
        public const string DynastyDiscardPile = Locations.DynastyDiscardPile;
        public const string RemovedFromGame = Locations.RemovedFromGame;
    }

    public static class AbilityTrigger
    {
        public const string LeavesPlay = "leavesPlay";
        public const string EntersPlay = "entersPlay";
        public const string OnBowed = "onBowed";
        public const string OnReadied = "onReadied";
        public const string OnHonored = "onHonored";
        public const string OnDishonored = "onDishonored";
        public const string AfterConflict = "afterConflict";
        public const string DuringConflict = "duringConflict";
    }

    public static class TargetConfiguration
    {
        public static string CardTypeFilter { get; set; } = "";
    }
    
    /// <summary>
    /// Global helper functions for compilation
    /// </summary>
    public static class GlobalHelpers
    {
        public static string GetImplementationStatus(string context = "default")
        {
            return $"Implementation status for {context}: Active";
        }
    }
}
