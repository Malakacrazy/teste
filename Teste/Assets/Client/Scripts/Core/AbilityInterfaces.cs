using System;
using System.Collections.Generic;
using UnityEngine;
using L5RGame.Types;

namespace L5RGame
{
    #region Target Interfaces and Classes

    /// <summary>
    /// Base interface for all targeting specifications
    /// </summary>
    public interface IBaseTarget
    {
        string ActivePromptTitle { get; set; }
        Func<AbilityContext, PlayersEnum> Player { get; set; }
        List<GameAction> GameActions { get; set; }
    }

    /// <summary>
    /// Base target implementation
    /// </summary>
    [Serializable]
    public abstract class BaseTarget : IBaseTarget
    {
        public string ActivePromptTitle { get; set; }
        public Func<AbilityContext, PlayersEnum> Player { get; set; }
        public List<GameAction> GameActions { get; set; } = new List<GameAction>();
    }

    /// <summary>
    /// Interface for choice-based targeting
    /// </summary>
    public interface IChoicesInterface
    {
        Dictionary<string, object> Choices { get; set; }
    }

    /// <summary>
    /// Target for selecting from multiple choices
    /// </summary>
    [Serializable]
    public class TargetSelect : BaseTarget, IChoicesInterface
    {
        public TargetModesEnum Mode { get; set; } = TargetModesEnum.Select;
        public Dictionary<string, object> Choices { get; set; } = new Dictionary<string, object>();
        public bool Targets { get; set; }
    }

    /// <summary>
    /// Target for ring selection
    /// </summary>
    [Serializable]
    public class TargetRing : BaseTarget
    {
        public TargetModesEnum Mode { get; set; } = TargetModesEnum.Ring;
        public Func<Ring, AbilityContext, bool> RingCondition { get; set; }
    }

    /// <summary>
    /// Target for ability selection
    /// </summary>
    [Serializable]
    public class TargetAbility : BaseTarget
    {
        public TargetModesEnum Mode { get; set; } = TargetModesEnum.Ability;
        public List<CardTypesEnum> CardTypes { get; set; } = new List<CardTypesEnum>();
        public Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
        public Func<BaseAbility, bool> AbilityCondition { get; set; }
    }

    /// <summary>
    /// Target for token selection
    /// </summary>
    [Serializable]
    public class TargetToken : BaseTarget
    {
        public TargetModesEnum Mode { get; set; } = TargetModesEnum.Token;
        public Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
    }

    /// <summary>
    /// Base interface for card targeting
    /// </summary>
    public interface IBaseTargetCard : IBaseTarget
    {
        List<CardTypesEnum> CardTypes { get; set; }
        PlayersEnum Controller { get; set; }
        List<LocationsEnum> Locations { get; set; }
        bool Optional { get; set; }
    }

    /// <summary>
    /// Base card target implementation
    /// </summary>
    [Serializable]
    public abstract class BaseTargetCard : BaseTarget, IBaseTargetCard
    {
        public List<CardTypesEnum> CardTypes { get; set; } = new List<CardTypesEnum>();
        public PlayersEnum Controller { get; set; }
        public List<LocationsEnum> Locations { get; set; } = new List<LocationsEnum>();
        public bool Optional { get; set; }
    }

    /// <summary>
    /// Target for exact number or up to number of cards
    /// </summary>
    [Serializable]
    public class TargetCardExactlyUpTo : BaseTargetCard
    {
        public TargetModesEnum Mode { get; set; }
        public int NumCards { get; set; }

        public TargetCardExactlyUpTo(TargetModesEnum mode, int numCards)
        {
            Mode = mode;
            NumCards = numCards;
        }
    }

    /// <summary>
    /// Target for variable number of cards
    /// </summary>
    [Serializable]
    public class TargetCardExactlyUpToVariable : BaseTargetCard
    {
        public TargetModesEnum Mode { get; set; }
        public Func<AbilityContext, int> NumCardsFunc { get; set; }

        public TargetCardExactlyUpToVariable(TargetModesEnum mode, Func<AbilityContext, int> numCardsFunc)
        {
            Mode = mode;
            NumCardsFunc = numCardsFunc;
        }
    }

    /// <summary>
    /// Target based on maximum stat
    /// </summary>
    [Serializable]
    public class TargetCardMaxStat : BaseTargetCard
    {
        public TargetModesEnum Mode { get; set; } = TargetModesEnum.MaxStat;
        public int NumCards { get; set; }
        public Func<BaseCard, int> CardStat { get; set; }
        public Func<int> MaxStat { get; set; }
    }

    /// <summary>
    /// Target for single or unlimited cards
    /// </summary>
    [Serializable]
    public class TargetCardSingleUnlimited : BaseTargetCard
    {
        public TargetModesEnum Mode { get; set; } = TargetModesEnum.Single;
    }

    /// <summary>
    /// Sub-target interface for dependent targets
    /// </summary>
    public interface ISubTarget
    {
        string DependsOn { get; set; }
    }

    /// <summary>
    /// Sub-target implementation
    /// </summary>
    [Serializable]
    public class SubTarget : ISubTarget
    {
        public string DependsOn { get; set; }
    }

    /// <summary>
    /// Action-specific card target
    /// </summary>
    public interface IActionCardTarget
    {
        Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
    }

    /// <summary>
    /// Action-specific ring target
    /// </summary>
    public interface IActionRingTarget
    {
        Func<Ring, AbilityContext, bool> RingCondition { get; set; }
    }

    #endregion

    #region Ability Property Interfaces

    /// <summary>
    /// Properties for duel initiation
    /// </summary>
    [Serializable]
    public class InitiateDuel
    {
        public bool OpponentChoosesDuelTarget { get; set; }
        public string DuelType { get; set; }
        public Func<AbilityContext, BaseCard> InitiatingPlayer { get; set; }
        public Func<AbilityContext, BaseCard> RespondingPlayer { get; set; }
    }

    /// <summary>
    /// Base ability properties interface
    /// </summary>
    public interface IAbilityProps
    {
        string Title { get; set; }
        List<LocationsEnum> Location { get; set; }
        List<ICost> Cost { get; set; }
        AbilityLimit Limit { get; set; }
        int Max { get; set; }
        IBaseTarget Target { get; set; }
        Dictionary<string, IBaseTarget> Targets { get; set; }
        object InitiateDuel { get; set; }
        bool CannotBeMirrored { get; set; }
        bool PrintedAbility { get; set; }
        bool CannotTargetFirst { get; set; }
        string Effect { get; set; }
        object EffectArgs { get; set; }
        List<GameAction> GameActions { get; set; }
        Action<AbilityContext> Handler { get; set; }
        object Then { get; set; }
    }

    /// <summary>
    /// Base ability properties implementation
    /// </summary>
    [Serializable]
    public class AbilityProps : IAbilityProps
    {
        public string Title { get; set; }
        public List<LocationsEnum> Location { get; set; } = new List<LocationsEnum>();
        public List<ICost> Cost { get; set; } = new List<ICost>();
        public AbilityLimit Limit { get; set; }
        public int Max { get; set; }
        public IBaseTarget Target { get; set; }
        public Dictionary<string, IBaseTarget> Targets { get; set; } = new Dictionary<string, IBaseTarget>();
        public object InitiateDuel { get; set; }
        public bool CannotBeMirrored { get; set; }
        public bool PrintedAbility { get; set; }
        public bool CannotTargetFirst { get; set; }
        public string Effect { get; set; }
        public object EffectArgs { get; set; }
        public List<GameAction> GameActions { get; set; } = new List<GameAction>();
        public Action<AbilityContext> Handler { get; set; }
        public object Then { get; set; }
    }

    /// <summary>
    /// Action-specific properties interface
    /// </summary>
    public interface IActionProps : IAbilityProps
    {
        Func<AbilityContext, bool> Condition { get; set; }
        string Phase { get; set; }
        bool AnyPlayer { get; set; }
        Func<BaseCard, bool> ConflictProvinceCondition { get; set; }
        bool CanTriggerOutsideConflict { get; set; }
    }

    /// <summary>
    /// Action properties implementation
    /// </summary>
    [Serializable]
    public class ActionProps : AbilityProps, IActionProps
    {
        public Func<AbilityContext, bool> Condition { get; set; }
        public string Phase { get; set; }
        public bool AnyPlayer { get; set; }
        public Func<BaseCard, bool> ConflictProvinceCondition { get; set; }
        public bool CanTriggerOutsideConflict { get; set; }
    }

    /// <summary>
    /// Triggered ability-specific card target
    /// </summary>
    public interface ITriggeredAbilityCardTarget
    {
        Func<BaseCard, TriggeredAbilityContext, bool> CardCondition { get; set; }
    }

    /// <summary>
    /// Triggered ability-specific ring target
    /// </summary>
    public interface ITriggeredAbilityRingTarget
    {
        Func<Ring, TriggeredAbilityContext, bool> RingCondition { get; set; }
    }

    /// <summary>
    /// When-type trigger conditions
    /// </summary>
    [Serializable]
    public class WhenType : Dictionary<EventNamesEnum, Func<object, TriggeredAbilityContext, bool>>
    {
        public WhenType() : base() { }
        
        public WhenType(Dictionary<EventNamesEnum, Func<object, TriggeredAbilityContext, bool>> conditions) : base(conditions) { }
    }

    /// <summary>
    /// Triggered ability properties with when conditions
    /// </summary>
    public interface ITriggeredAbilityWhenProps : IAbilityProps
    {
        WhenType When { get; set; }
        bool CollectiveTrigger { get; set; }
    }

    /// <summary>
    /// Triggered ability properties with aggregate when conditions
    /// </summary>
    public interface ITriggeredAbilityAggregateWhenProps : IAbilityProps
    {
        Func<List<object>, TriggeredAbilityContext, bool> AggregateWhen { get; set; }
        bool CollectiveTrigger { get; set; }
    }

    /// <summary>
    /// Triggered ability when properties implementation
    /// </summary>
    [Serializable]
    public class TriggeredAbilityWhenProps : AbilityProps, ITriggeredAbilityWhenProps
    {
        public WhenType When { get; set; } = new WhenType();
        public bool CollectiveTrigger { get; set; }
    }

    /// <summary>
    /// Triggered ability aggregate when properties implementation
    /// </summary>
    [Serializable]
    public class TriggeredAbilityAggregateWhenProps : AbilityProps, ITriggeredAbilityAggregateWhenProps
    {
        public Func<List<object>, TriggeredAbilityContext, bool> AggregateWhen { get; set; }
        public bool CollectiveTrigger { get; set; }
    }

    #endregion

    #region Persistent Effect Interfaces

    /// <summary>
    /// Persistent effect properties interface
    /// </summary>
    public interface IPersistentEffectProps
    {
        List<LocationsEnum> Location { get; set; }
        Func<AbilityContext, bool> Condition { get; set; }
        Func<BaseCard, AbilityContext, bool> Match { get; set; }
        PlayersEnum TargetController { get; set; }
        LocationsEnum TargetLocation { get; set; }
        object Effect { get; set; }
    }

    /// <summary>
    /// Persistent effect properties implementation
    /// </summary>
    [Serializable]
    public class PersistentEffectProps : IPersistentEffectProps
    {
        public List<LocationsEnum> Location { get; set; } = new List<LocationsEnum>();
        public Func<AbilityContext, bool> Condition { get; set; }
        public Func<BaseCard, AbilityContext, bool> Match { get; set; }
        public PlayersEnum TargetController { get; set; }
        public LocationsEnum TargetLocation { get; set; }
        public object Effect { get; set; }
    }

    #endregion

    #region Attachment and Token Interfaces

    /// <summary>
    /// Trait limit specification
    /// </summary>
    [Serializable]
    public class TraitLimit : Dictionary<string, int>
    {
        public TraitLimit() : base() { }
        public TraitLimit(Dictionary<string, int> limits) : base(limits) { }
    }

    /// <summary>
    /// Attachment condition properties interface
    /// </summary>
    public interface IAttachmentConditionProps
    {
        int Limit { get; set; }
        bool MyControl { get; set; }
        bool Unique { get; set; }
        List<string> Faction { get; set; }
        List<string> Trait { get; set; }
        List<TraitLimit> LimitTrait { get; set; }
    }

    /// <summary>
    /// Attachment condition properties implementation
    /// </summary>
    [Serializable]
    public class AttachmentConditionProps : IAttachmentConditionProps
    {
        public int Limit { get; set; }
        public bool MyControl { get; set; }
        public bool Unique { get; set; }
        public List<string> Faction { get; set; } = new List<string>();
        public List<string> Trait { get; set; } = new List<string>();
        public List<TraitLimit> LimitTrait { get; set; } = new List<TraitLimit>();
    }

    /// <summary>
    /// Honor token interface
    /// </summary>
    public interface IHonoredToken
    {
        bool Honored { get; }
        BaseCard Card { get; }
        string Type { get; }
    }

    /// <summary>
    /// Dishonor token interface
    /// </summary>
    public interface IDishonoredToken
    {
        bool Dishonored { get; }
        BaseCard Card { get; }
        string Type { get; }
    }

    /// <summary>
    /// Honored token implementation
    /// </summary>
    [Serializable]
    public class HonoredToken : IHonoredToken
    {
        public bool Honored { get; } = true;
        public BaseCard Card { get; set; }
        public string Type { get; } = "token";
    }

    /// <summary>
    /// Dishonored token implementation
    /// </summary>
    [Serializable]
    public class DishonoredToken : IDishonoredToken
    {
        public bool Dishonored { get; } = true;
        public BaseCard Card { get; set; }
        public string Type { get; } = "token";
    }

    /// <summary>
    /// Generic token interface
    /// </summary>
    public interface IToken
    {
        BaseCard Card { get; }
        string Type { get; }
    }

    #endregion

}

namespace L5RGame.Types
{
    #region Enumerations
    
    // Enum types for type safety in method signatures and generics
    // These are in a separate namespace (L5RGame.Types) to avoid conflicts with static classes
    // The static string constants in Constants.cs remain for runtime string values
    
    /// <summary>
    /// Target modes enumeration for type-safe targeting specifications
    /// </summary>
    public enum TargetModesEnum
    {
        Select,
        Ring,
        Ability,
        Token,
        Exactly,
        UpTo,
        ExactlyVariable,
        UpToVariable,
        MaxStat,
        Single,
        Unlimited
    }

    /// <summary>
    /// Players enumeration for type-safe player targeting
    /// </summary>
    public enum PlayersEnum
    {
        Self,
        Opponent,
        Any,
        Current,
        FirstPlayer,
        NonFirstPlayer
    }

    /// <summary>
    /// Card types enumeration for type-safe card type specifications
    /// </summary>
    public enum CardTypesEnum
    {
        Character,
        Attachment,
        Event,
        Holding,
        Province,
        Stronghold,
        Role
    }

    /// <summary>
    /// Locations enumeration for type-safe location specifications
    /// </summary>
    public enum LocationsEnum
    {
        Hand,
        ConflictDeck,
        ConflictDiscardPile,
        DynastyDeck,
        DynastyDiscardPile,
        PlayArea,
        ProvinceOne,
        ProvinceTwo,
        ProvinceThree,
        ProvinceFour,
        StrongholdProvince,
        RemovedFromGame,
        Limbo,
        Any,
        Provinces,
        Role,
        BeingPlayed,
        ProvinceDeck,
        UnderneathStronghold,
        None
    }

    /// <summary>
    /// Event names enumeration for type-safe event specifications
    /// </summary>
    public enum EventNamesEnum
    {
        OnCardPlayed,
        OnCardEntersPlay,
        OnCardLeavesPlay,
        OnConflictDeclared,
        OnConflictInitiated,
        OnConflictFinished,
        OnHonorDialsRevealed,
        OnPhaseStarted,
        OnPhaseEnded,
        OnRoundEnded,
        OnGameEnded,
        OnAbilityTriggered,
        OnEffectApplied,
        OnDuelInitiated,
        OnDuelFinished,
        OnCardsDiscarded,
        OnCardsDrawn,
        OnFateGained,
        OnFateLost,
        OnHonorGained,
        OnHonorLost,
        OnRingClaimed,
        OnRingContested,
        OnCharacterEntersConflict,
        OnCharacterLeavesConflict,
        OnMovementPhaseEnd,
        OnPassPriority,
        OnPassActionPhasePriority,
        OnDefendersDeclared,
        OnCovertResolved,
        OnClaimRing,
        OnReturnHome,
        OnParticipantsReturnHome,
        AfterConflict,
        Unnamed
    }

    /// <summary>
    /// Durations enumeration for type-safe duration specifications
    /// </summary>
    public enum DurationsEnum
    {
        UntilEndOfConflict,
        UntilEndOfPhase,
        UntilEndOfRound,
        UntilEndOfDuel,
        UntilPassPriority,
        UntilOpponentPassPriority,
        UntilNextPassPriority,
        Persistent,
        Custom
    }
    
    #endregion
}

namespace L5RGame
{
    #region Extension Methods

    /// <summary>
    /// Extension methods for target interfaces
    /// </summary>
    public static class TargetExtensions
    {
        /// <summary>
        /// Check if a target is optional
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if target is optional</returns>
        public static bool IsOptional(this IBaseTarget target)
        {
            return target is IBaseTargetCard cardTarget && cardTarget.Optional;
        }

        /// <summary>
        /// Get the target mode
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>Target mode</returns>
        public static TargetModesEnum GetMode(this IBaseTarget target)
        {
            return target switch
            {
                TargetSelect => TargetModesEnum.Select,
                TargetRing => TargetModesEnum.Ring,
                TargetAbility => TargetModesEnum.Ability,
                TargetToken => TargetModesEnum.Token,
                TargetCardExactlyUpTo exactUpTo => exactUpTo.Mode,
                TargetCardExactlyUpToVariable variable => variable.Mode,
                TargetCardMaxStat => TargetModesEnum.MaxStat,
                TargetCardSingleUnlimited single => single.Mode,
                _ => TargetModesEnum.Single
            };
        }
    }

    /// <summary>
    /// Extension methods for ability properties
    /// </summary>
    public static class AbilityPropsExtensions
    {
        /// <summary>
        /// Check if ability has a specific location requirement
        /// </summary>
        /// <param name="props">Ability properties</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if location is required</returns>
        public static bool RequiresLocation(this IAbilityProps props, LocationsEnum location)
        {
            return props.Location != null && props.Location.Contains(location);
        }

        /// <summary>
        /// Check if ability has any costs
        /// </summary>
        /// <param name="props">Ability properties</param>
        /// <returns>True if there are costs</returns>
        public static bool HasCosts(this IAbilityProps props)
        {
            return props.Cost != null && props.Cost.Count > 0;
        }

        /// <summary>
        /// Check if ability has targets
        /// </summary>
        /// <param name="props">Ability properties</param>
        /// <returns>True if there are targets</returns>
        public static bool HasTargets(this IAbilityProps props)
        {
            return props.Target != null || (props.Targets != null && props.Targets.Count > 0);
        }
    }

    #endregion
}