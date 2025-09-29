using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Game phases enumeration
    /// </summary>
    public static class Phases
    {
        public const string Setup = "setup";
        public const string Dynasty = "dynasty";
        public const string Draw = "draw";
        public const string Conflict = "conflict";
        public const string Fate = "fate";
        public const string Regroup = "regroup";
    }

    /// <summary>
    /// Card types constants
    /// </summary>
    public static class CardTypes
    {
        public const string Character = "character";
        public const string Attachment = "attachment";
        public const string Event = "event";
        public const string Holding = "holding";
        public const string Province = "province";
        public const string Stronghold = "stronghold";
        public const string Role = "role";
    }

    /// <summary>
    /// Location constants
    /// </summary>
    public static class Locations
    {
        public const string Hand = "hand";
        public const string ConflictDeck = "conflict deck";
        public const string ConflictDiscardPile = "conflict discard pile";
        public const string DynastyDeck = "dynasty deck";
        public const string DynastyDiscardPile = "dynasty discard pile";
        public const string PlayArea = "play area";
        public const string ProvinceOne = "province 1";
        public const string ProvinceTwo = "province 2";
        public const string ProvinceThree = "province 3";
        public const string ProvinceFour = "province 4";
        public const string StrongholdProvince = "stronghold province";
        public const string RemovedFromGame = "removed from game";
        public const string Limbo = "limbo";
        public const string Any = "any";
        public const string Provinces = "provinces";
        public const string Role = "role";
        public const string BeingPlayed = "being played";
        public const string ProvinceDeck = "province deck";
        public const string UnderneathStronghold = "underneath stronghold";
        public const string None = "none";
    }

    /// <summary>
    /// Card location constants - alias for Locations for backward compatibility
    /// </summary>
    public static class CardLocations
    {
        public const string Any = Locations.Any;
        public const string Hand = Locations.Hand;
        public const string PlayArea = Locations.PlayArea;
        public const string DynastyDiscardPile = Locations.DynastyDiscardPile;
        public const string ConflictDiscardPile = Locations.ConflictDiscardPile;
        public const string ProvinceOne = Locations.ProvinceOne;
        public const string ProvinceTwo = Locations.ProvinceTwo;
        public const string ProvinceThree = Locations.ProvinceThree;
        public const string ProvinceFour = Locations.ProvinceFour;
        public const string StrongholdProvince = Locations.StrongholdProvince;
        public const string RemovedFromGame = Locations.RemovedFromGame;
        public const string Provinces = Locations.Provinces;
        public const string Role = Locations.Role;
        public const string BeingPlayed = Locations.BeingPlayed;
        public const string ConflictDeck = Locations.ConflictDeck;
        public const string DynastyDeck = Locations.DynastyDeck;
        public const string ProvinceDeck = Locations.ProvinceDeck;
        public const string UnderneathStronghold = Locations.UnderneathStronghold;
        public const string Limbo = Locations.Limbo;
        public const string None = Locations.None;
    }

    /// <summary>
    /// Ability types constants
    /// </summary>
    public static class AbilityTypes
    {
        public const string Action = "action";
        public const string Reaction = "reaction";
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedreaction";
        public const string ForcedInterrupt = "forcedinterrupt";
        public const string WhenRevealed = "whenrevealed";
        public const string Persistent = "persistent";
        public const string RingEffect = "ringeffect";
        public const string Keyword = "keyword";
        public const string WouldInterrupt = "wouldinterrupt";
        public const string CancelInterrupt = "cancelinterrupt";
        public const string OtherEffects = "othereffects";
    }

    /// <summary>
    /// Keywords constants
    /// </summary>
    public static class Keywords
    {
        public const string Ancestral = "ancestral";
        public const string Restricted = "restricted";
        public const string Limited = "limited";
        public const string Sincerity = "sincerity";
        public const string Courtesy = "courtesy";
        public const string Pride = "pride";
        public const string Covert = "covert";
        public const string Disguised = "disguised";
        public const string Rally = "rally";
        public const string Charge = "charge";
    }

    /// <summary>
    /// Effect names constants
    /// </summary>
    public static partial class EffectNames
    {
        public const string ModifyMilitarySkill = "modifyMilitarySkill";
        public const string ModifyPoliticalSkill = "modifyPoliticalSkill";
        public const string ModifyBothSkills = "modifyBothSkills";
        public const string ModifyGlory = "modifyGlory";
        public const string ModifyCost = "modifyCost";
        public const string GainExtraFateWhenPlayed = "gainExtraFateWhenPlayed";
        public const string CannotParticipateInConflicts = "cannotParticipateInConflicts";
        public const string CannotBeDeclaredAsAttacker = "cannotBeDeclaredAsAttacker";
        public const string CannotBeDeclaredAsDefender = "cannotBeDeclaredAsDefender";
        public const string MustBeDeclaredAsAttacker = "mustBeDeclaredAsAttacker";
        public const string MustBeDeclaredAsDefender = "mustBeDeclaredAsDefender";
        public const string CannotBeTargeted = "cannotBeTargeted";
        public const string CannotTriggerAbilities = "cannotTriggerAbilities";
        public const string Blank = "blank";
        public const string TakeControl = "takeControl";
        public const string Honored = "honored";
        public const string Dishonored = "dishonored";
        
        // Additional constants from GameConstants integration
        public const string ModifyProvinceStrength = "modifyProvinceStrength";
        public const string ModifyGloryForImperialFavor = "modifyGloryForImperialFavor";
        public const string FateCostToAttack = "fateCostToAttack";
        public const string ForceConflictUnopposed = "forceConflictUnopposed";
        public const string DoesNotBowAsAttacker = "doesNotBowAsAttacker";
        public const string DoesNotBowAsDefender = "doesNotBowAsDefender";
        public const string CannotBeBypassedByCovert = "cannotBeBypassedByCovert";
        public const string GainCovert = "gainCovert";
        public const string CannotBeCancelled = "cannotBeCancelled";
        public const string CannotHaveFateRemoved = "cannotHaveFateRemoved";
        public const string CannotBeDiscarded = "cannotBeDiscarded";
        public const string CannotBeBowed = "cannotBeBowed";
        public const string CannotBeMovedToConflict = "cannotBeMovedToConflict";
        public const string CannotLeavePlay = "cannotLeavePlay";
        public const string CannotPlay = "cannotPlay";
        public const string DelayedEffect = "delayedEffect";
        
        // Missing effect names from compilation errors
        public const string AdditionalTriggerCost = "additionalTriggerCost";
        public const string AdditionalPlayCost = "additionalPlayCost";
        public const string ModifyConflictElementsToResolve = "modifyConflictElementsToResolve";
        public const string RestrictNumberOfDefenders = "restrictNumberOfDefenders";
        public const string SetConflictTotalSkill = "setConflictTotalSkill";
        public const string ChangeConflictSkillFunction = "changeConflictSkillFunction";
        public const string CannotContribute = "cannotContribute";
        public const string MustBeChosen = "mustBeChosen";
        public const string CanBeSeenWhenFacedown = "canBeSeenWhenFacedown";
        public const string CannotParticipateAsAttacker = "cannotParticipateAsAttacker";
        public const string CannotParticipateAsDefender = "cannotParticipateAsDefender";
        public const string AbilityRestrictions = "abilityRestrictions";
        public const string DoesNotBow = "doesNotBow";
        public const string DoesNotReady = "doesNotReady";
        public const string ShowTopConflictCard = "showTopConflictCard";
        public const string ModifyBaseMilitarySkillMultiplier = "modifyBaseMilitarySkillMultiplier";
        public const string ModifyMilitarySkillMultiplier = "modifyMilitarySkillMultiplier";
        public const string ModifyBasePoliticalSkillMultiplier = "modifyBasePoliticalSkillMultiplier";
        public const string ModifyPoliticalSkillMultiplier = "modifyPoliticalSkillMultiplier";
        public const string SetBaseMilitarySkill = "setBaseMilitarySkill";
        public const string SetBasePoliticalSkill = "setBasePoliticalSkill";
        public const string SetGlory = "setGlory";
        public const string SetMilitarySkill = "setMilitarySkill";
        public const string SetPoliticalSkill = "setPoliticalSkill";
        public const string ModifyFateCollectedInDynastyPhase = "modifyFateCollectedInDynastyPhase";
        public const string ModifyFateCollectionMultiplier = "modifyFateCollectionMultiplier";
        public const string HonorStatusDoesNotModifySkill = "honorStatusDoesNotModifySkill";
        public const string HonorStatusReverseModifySkill = "honorStatusReverseModifySkill";
        public const string CannotApplyLastingEffects = "cannotApplyLastingEffects";
        
        // Missing effect names from compilation errors
        public const string RestrictHonorBid = "restrictHonorBid";
        public const string AddHonorBidOption = "addHonorBidOption";
        public const string EventsCannotBeCancelled = "eventsCannotBeCancelled";
        public const string ShowTopDynastyCard = "showTopDynastyCard";
        public const string ModifyCardsDrawnInDrawPhase = "modifyCardsDrawnInDrawPhase";
        public const string MaxCardsDrawnInDrawPhase = "maxCardsDrawnInDrawPhase";
    }

    /// <summary>
    /// Event names constants
    /// </summary>
    public static partial class EventNames
    {
        public const string OnCardPlayed = "onCardPlayed";
        public const string OnCardEntersPlay = "onCardEntersPlay";
        public const string OnCardLeavesPlay = "onCardLeavesPlay";
        // OnConflictDeclared moved to EventNames.cs to avoid duplicate definition
        public const string OnConflictInitiated = "onConflictInitiated";
        public const string OnConflictFinished = "onConflictFinished";
        public const string OnHonorDialsRevealed = "onHonorDialsRevealed";
        public const string OnPhaseStarted = "onPhaseStarted";
        public const string OnPhaseEnded = "onPhaseEnded";
        public const string OnRoundEnded = "onRoundEnded";
        public const string OnGameEnded = "onGameEnded";
        public const string OnAbilityTriggered = "onAbilityTriggered";
        public const string OnEffectApplied = "onEffectApplied";
        public const string OnDuelInitiated = "onDuelInitiated";
        public const string OnDuelFinished = "onDuelFinished";
        public const string OnCardsDiscarded = "onCardsDiscarded";
        public const string OnCardsDrawn = "onCardsDrawn";
        public const string OnFateGained = "onFateGained";
        public const string OnFateLost = "onFateLost";
        public const string OnHonorGained = "onHonorGained";
        public const string OnHonorLost = "onHonorLost";
        public const string OnRingClaimed = "onRingClaimed";
        public const string OnRingContested = "onRingContested";
        public const string OnCharacterEntersConflict = "onCharacterEntersConflict";
        public const string OnCharacterLeavesConflict = "onCharacterLeavesConflict";
        public const string OnMovementPhaseEnd = "onMovementPhaseEnd";
        // OnPassPriority moved to EventNames.cs to avoid duplicate definition
        public const string OnPassActionPhasePriority = "onPassActionPhasePriority";
        public const string OnDefendersDeclared = "onDefendersDeclared";
        public const string OnCovertResolved = "onCovertResolved";
        public const string OnClaimRing = "onClaimRing";
        public const string OnReturnHome = "onReturnHome";
        public const string OnParticipantsReturnHome = "onParticipantsReturnHome";
        public const string AfterConflict = "afterConflict";
        public const string Unnamed = "unnamed";
        
        // Missing event names from compilation errors
        public const string OnTakeControl = "onTakeControl";
        public const string OnStatusTokenMoved = "onStatusTokenMoved";
        public const string OnStatusTokenRemoved = "onStatusTokenRemoved";
    }

    /// <summary>
    /// Players enumeration for targeting
    /// </summary>
    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
        public const string Any = "any";
        public const string Current = "current";
        public const string FirstPlayer = "firstPlayer";
        public const string NonFirstPlayer = "nonFirstPlayer";
    }

    /// <summary>
    /// Target modes for different targeting scenarios
    /// </summary>
    public static class TargetModes
    {
        public const string Select = "select";
        public const string Ring = "ring";
        public const string Ability = "ability";
        public const string Token = "token";
        public const string Exactly = "exactly";
        public const string UpTo = "upTo";
        public const string ExactlyVariable = "exactlyVariable";
        public const string UpToVariable = "upToVariable";
        public const string MaxStat = "maxStat";
        public const string Single = "single";
        public const string Unlimited = "unlimited";
        public const string AutoSingle = "autoSingle";
    }

    /// <summary>
    /// Duration types for effects
    /// </summary>
    public static partial class Durations
    {
        public const string UntilEndOfConflict = "untilEndOfConflict";
        public const string UntilEndOfPhase = "untilEndOfPhase";
        public const string UntilEndOfRound = "untilEndOfRound";
        public const string UntilEndOfTurn = "untilEndOfTurn";
        public const string UntilEndOfDuel = "untilEndOfDuel";
        public const string UntilPassPriority = "untilPassPriority";
        public const string UntilOpponentPassPriority = "untilOpponentPassPriority";
        public const string UntilNextPassPriority = "untilNextPassPriority";
        public const string Persistent = "persistent";
        public const string Custom = "custom";
    }

    /// <summary>
    /// Game stages for ability execution
    /// </summary>
    public static class Stages
    {
        public const string PreTarget = "pretarget";
        public const string Target = "target";
        public const string Cost = "cost";
        public const string Effect = "effect";
        public const string PostEffect = "posteffect";
    }

    /// <summary>
    /// Ring elements constants
    /// </summary>
    public static class RingElements
    {
        public const string Air = "air";
        public const string Earth = "earth";
        public const string Fire = "fire";
        public const string Void = "void";
        public const string Water = "water";
    }

    /// <summary>
    /// Conflict types constants
    /// </summary>
    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    /// <summary>
    /// Game action types constants
    /// </summary>
    public static class GameActionTypes
    {
        public const string MoveTo = "moveTo";
        public const string Discard = "discard";
        public const string Bow = "bow";
        public const string Ready = "ready";
        public const string GainHonor = "gainHonor";
        public const string LoseHonor = "loseHonor";
        public const string GainFate = "gainFate";
        public const string SpendFate = "spendFate";
        public const string PlayCard = "playCard";
        public const string PutIntoPlay = "putIntoPlay";
        public const string RemoveFromGame = "removeFromGame";
        public const string Reveal = "reveal";
        public const string LookAt = "lookAt";
        public const string Shuffle = "shuffle";
        public const string Search = "search";
        public const string TakeControl = "takeControl";
        public const string Attach = "attach";
        public const string Detach = "detach";
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string Break = "break";
        public const string SendHome = "sendHome";
        public const string FlipDynasty = "flipDynasty";
        public const string CreateToken = "createToken";
        public const string PlaceFate = "placeFate";
        public const string RemoveFate = "removeFate";
        public const string ModifyStats = "modifyStats";
        public const string ResolveAbility = "resolveAbility";
        public const string LastingEffect = "lastingEffect";
        // DelayedEffect moved to EffectNames to avoid duplicate - it's more appropriately an effect than an action
        public const string CardMenuCommand = "cardMenuCommand";
        public const string SelectCard = "selectCard";
        public const string ChooseAction = "chooseAction";
        public const string Duel = "duel";
        public const string MoveToConflict = "moveToConflict";
        public const string ReturnToHand = "returnToHand";
        public const string ReturnToDeck = "returnToDeck";
        public const string TurnFacedown = "turnFacedown";
        public const string AttachToRing = "attachToRing";
    }

    /// <summary>
    /// Prompt types for user interface
    /// </summary>
    public static class PromptTypes
    {
        public const string SelectCard = "selectCard";
        public const string SelectRing = "selectRing";
        public const string SelectAbility = "selectAbility";
        public const string SelectChoice = "selectChoice";
        public const string MenuChoice = "menuChoice";
        public const string Confirm = "confirm";
        public const string HandlerMenu = "handlerMenu";
    }

    /// <summary>
    /// Message formatting constants
    /// </summary>
    public static class MessageFormats
    {
        public const string PlayCard = "{0} plays {1}";
        public const string UseAbility = "{0} uses {1}";
        public const string GainFate = "{0} gains {1} fate";
        public const string LoseFate = "{0} loses {1} fate";
        public const string GainHonor = "{0} gains {1} honor";
        public const string LoseHonor = "{0} loses {1} honor";
        public const string DrawCards = "{0} draws {1} cards";
        public const string DiscardCards = "{0} discards {1} cards";
    }

    /// <summary>
    /// Timing windows for event processing
    /// </summary>
    public static class TimingWindows
    {
        public const string Interrupt = "interrupt";
        public const string Handler = "handler";
        public const string Reaction = "reaction";
    }

    /// <summary>
    /// Helper class for working with constants
    /// </summary>
    public static class ConstantsHelper
    {
        /// <summary>
        /// Get all card types as a list
        /// </summary>
        /// <returns>List of all card type constants</returns>
        public static List<string> GetAllCardTypes()
        {
            return new List<string>
            {
                CardTypes.Character,
                CardTypes.Attachment,
                CardTypes.Event,
                CardTypes.Holding,
                CardTypes.Province,
                CardTypes.Stronghold,
                CardTypes.Role
            };
        }

        /// <summary>
        /// Get all locations as a list
        /// </summary>
        /// <returns>List of all location constants</returns>
        public static List<string> GetAllLocations()
        {
            return new List<string>
            {
                Locations.Hand,
                Locations.ConflictDeck,
                Locations.ConflictDiscardPile,
                Locations.DynastyDeck,
                Locations.DynastyDiscardPile,
                Locations.PlayArea,
                Locations.ProvinceOne,
                Locations.ProvinceTwo,
                Locations.ProvinceThree,
                Locations.ProvinceFour,
                Locations.StrongholdProvince,
                Locations.RemovedFromGame
            };
        }

        /// <summary>
        /// Get all ring elements as a list
        /// </summary>
        /// <returns>List of all ring element constants</returns>
        public static List<string> GetAllRingElements()
        {
            return new List<string>
            {
                RingElements.Air,
                RingElements.Earth,
                RingElements.Fire,
                RingElements.Void,
                RingElements.Water
            };
        }

        /// <summary>
        /// Check if a string is a valid card type
        /// </summary>
        /// <param name="cardType">String to check</param>
        /// <returns>True if it's a valid card type</returns>
        public static bool IsValidCardType(string cardType)
        {
            return GetAllCardTypes().Contains(cardType);
        }

        /// <summary>
        /// Check if a string is a valid location
        /// </summary>
        /// <param name="location">String to check</param>
        /// <returns>True if it's a valid location</returns>
        public static bool IsValidLocation(string location)
        {
            return GetAllLocations().Contains(location);
        }

        /// <summary>
        /// Check if a string is a valid ring element
        /// </summary>
        /// <param name="element">String to check</param>
        /// <returns>True if it's a valid ring element</returns>
        public static bool IsValidRingElement(string element)
        {
            return GetAllRingElements().Contains(element);
        }

        /// <summary>
        /// Get the opposite conflict type
        /// </summary>
        /// <param name="conflictType">Current conflict type</param>
        /// <returns>Opposite conflict type</returns>
        public static string GetOppositeConflictType(string conflictType)
        {
            return conflictType == ConflictTypes.Military ? ConflictTypes.Political : ConflictTypes.Military;
        }
    }

    /// <summary>
    /// Game phases enumeration
    /// </summary>
    public static class GamePhases
    {
        public const string Setup = "setup";
        public const string Dynasty = "dynasty";
        public const string Draw = "draw";
        public const string Conflict = "conflict";
        public const string Fate = "fate";
        public const string Regroup = "regroup";
    }

    /// <summary>
    /// Token types enumeration
    /// </summary>
    public static class TokenTypes
    {
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string Fate = "fate";
        public const string Taint = "taint";
        public const string Poison = "poison";
        public const string Bond = "bond";
        public const string Character = "character";
        public const string Attachment = "attachment";
    }

    /// <summary>
    /// Ability identifier class
    /// </summary>
    public class AbilityId
    {
        public string Value { get; set; }
        
        // Ring effect constants
        public static readonly AbilityId EarthRing = new AbilityId("earth-ring");
        public static readonly AbilityId FireRing = new AbilityId("fire-ring");
        public static readonly AbilityId WaterRing = new AbilityId("water-ring");
        public static readonly AbilityId VoidRing = new AbilityId("void-ring");
        public static readonly AbilityId AirRing = new AbilityId("air-ring");
        
        public AbilityId(string value)
        {
            Value = value;
        }
        
        public override string ToString()
        {
            return Value;
        }
        
        public static implicit operator string(AbilityId abilityId)
        {
            return abilityId?.Value;
        }
        
        public static implicit operator AbilityId(string value)
        {
            return new AbilityId(value);
        }
    }

    /// <summary>
    /// Ability trigger configuration
    /// </summary>
    public class AbilityTrigger
    {
        public string Event { get; set; }
        public object Condition { get; set; }
        public string Cost { get; set; }
        public string Target { get; set; }
        public bool FateRemoved { get; set; }
        
        public AbilityTrigger(string eventName)
        {
            Event = eventName;
        }
    }

}