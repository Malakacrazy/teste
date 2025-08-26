namespace L5RGame
{
    /// <summary>
    /// Constants for event names throughout the game
    /// </summary>
    public static class EventNames
    {
        public const string Unnamed = "Unnamed";
        public const string OnCardAbilityInitiated = "OnCardAbilityInitiated";
        public const string OnCardPlayed = "OnCardPlayed";
        public const string OnCardAbilityTriggered = "OnCardAbilityTriggered";
        public const string OnCharacterEnteredPlay = "OnCharacterEnteredPlay";
        public const string OnCardEntersPlay = "OnCardEntersPlay";
        public const string OnAttachmentPlayed = "OnAttachmentPlayed";
        public const string OnEventPlayed = "OnEventPlayed";
        public const string OnConflictInitiated = "OnConflictInitiated";
        public const string OnConflictDeclared = "OnConflictDeclared";
        public const string OnConflictEnded = "OnConflictEnded";
        public const string OnPhaseStarted = "OnPhaseStarted";
        public const string OnPhaseEnded = "OnPhaseEnded";
        public const string OnRoundEnded = "OnRoundEnded";
        public const string OnHonorDialsRevealed = "OnHonorDialsRevealed";
        public const string OnDeckShuffled = "onDeckShuffled";
        public const string OnFateCollected = "onFateCollected";
        public const string OnCardMoved = "onCardMoved";
    }

    /// <summary>
    /// Constants for card types
    /// </summary>
    public static class CardTypes
    {
        public const string Character = "character";
        public const string Attachment = "attachment";
        public const string Event = "event";
        public const string Holding = "holding";
        public const string Stronghold = "stronghold";
        public const string Province = "province";
        public const string Role = "role";
    }

    /// <summary>
    /// Constants for game locations where cards can be placed
    /// </summary>
    public static class Locations
    {
        // Hand and deck locations
        public const string Hand = "hand";
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
        public const string ConflictDiscardPile = "conflict discard pile";
        public const string DynastyDiscardPile = "dynasty discard pile";
        public const string RemovedFromGame = "removed from game";
        
        // Province locations
        public const string StrongholdProvince = "stronghold province";
        public const string ProvinceOne = "province 1";
        public const string ProvinceTwo = "province 2";
        public const string ProvinceThree = "province 3";
        public const string ProvinceFour = "province 4";
        public const string ProvinceDeck = "province deck";
        public const string Provinces = "provinces";
        
        // Play area locations
        public const string PlayArea = "play area";
        public const string BeingPlayed = "being played";
        public const string AttachmentBeingPlayed = "being played as attachment";
        
        // Conflict locations
        public const string ConflictProvince = "conflict province";
        
        // Special locations
        public const string AdditionalProvince = "additional province";
        public const string UnderCard = "underneath";
        public const string UnderneathStronghold = "underneath stronghold";
        public const string Role = "role";
        public const string Any = "any";
    }

    /// <summary>
    /// Constants for play types when cards are played
    /// </summary>
    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
        public const string PlayFromDiscard = "playFromDiscard";
        public const string PlayFromRemovedFromGame = "playFromRemovedFromGame";
        public const string PlayFromDeck = "playFromDeck";
        public const string DynastyFromProvince = "dynastyFromProvince";
    }

    /// <summary>
    /// Constants for conflict types
    /// </summary>
    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    /// <summary>
    /// Constants for elements (ring types)
    /// </summary>
    public static class Elements
    {
        public const string Air = "air";
        public const string Earth = "earth";
        public const string Fire = "fire";
        public const string Water = "water";
        public const string Void = "void";
    }

    /// <summary>
    /// Constants for player actions
    /// </summary>
    public static class PlayerActions
    {
        public const string Pass = "pass";
        public const string PlayCard = "playCard";
        public const string TriggerAbility = "triggerAbility";
        public const string DeclareConflict = "declareConflict";
        public const string DefendConflict = "defendConflict";
        public const string ChooseRing = "chooseRing";
        public const string BreakProvince = "breakProvince";
    }

    /// <summary>
    /// Constants for game phases
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
    /// Constants for ability timing windows
    /// </summary>
    public static class AbilityTypes
    {
        public const string Action = "action";
        public const string Reaction = "reaction";
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedreaction";
        public const string ForcedInterrupt = "forcedinterrupt";
        public const string Keyword = "keyword";
        public const string Persistent = "persistent";
        public const string WouldInterrupt = "wouldinterrupt";
        public const string CancelInterrupt = "cancelinterrupt";
    }

    /// <summary>
    /// Constants for targeting keywords
    /// </summary>
    public static class TargetModes
    {
        public const string Select = "select";
        public const string AutoTarget = "autoTarget";
        public const string Single = "single";
        public const string Unlimited = "unlimited";
        public const string UpTo = "upTo";
        public const string Exactly = "exactly";
    }

    /// <summary>
    /// Constants for ability keywords
    /// </summary>
    public static class Keywords
    {
        public const string Ancestral = "ancestral";
        public const string Sincerity = "sincerity";
        public const string Pride = "pride";
        public const string Courtesy = "courtesy";
        public const string Covert = "covert";
        public const string Rally = "rally";
        public const string Support = "support";
        public const string Disguise = "disguise";
        public const string Eminent = "eminent";
        public const string Restricted = "restricted";
        public const string Limited = "limited";
    }

    /// <summary>
    /// Constants for status tokens
    /// </summary>
    public static class StatusTokens
    {
        public const string Honored = "honored";
        public const string Dishonored = "dishonored";
        public const string Tainted = "tainted";
        public const string Poisoned = "poisoned";
        public const string Bowed = "bowed";
        public const string Ready = "ready";
        public const string Participating = "participating";
    }

    /// <summary>
    /// Constants for menu commands
    /// </summary>
    public static class MenuCommands
    {
        public const string Pass = "pass";
        public const string Done = "done";
        public const string Cancel = "cancel";
        public const string Ok = "ok";
        public const string Yes = "yes";
        public const string No = "no";
        public const string Continue = "continue";
        public const string ChooseOption = "chooseOption";
    }

    /// <summary>
    /// Constants for prompt titles and messages
    /// </summary>
    public static class PromptTitles
    {
        public const string SelectTarget = "Select a target";
        public const string SelectCards = "Select cards";
        public const string ChooseAction = "Choose an action";
        public const string PayCosts = "Pay costs";
        public const string ChoosePlayer = "Choose a player";
        public const string SelectRing = "Select a ring";
        public const string BidHonor = "Bid honor";
        public const string EndRound = "End of round";
        public const string GameWon = "Game Won";
    }

    /// <summary>
    /// Constants for ring states
    /// </summary>
    public static class RingStates
    {
        public const string Unclaimed = "unclaimed";
        public const string Claimed = "claimed";
        public const string Contested = "contested";
        public const string Resolved = "resolved";
    }

    /// <summary>
    /// Constants for conflict results
    /// </summary>
    public static class ConflictResults
    {
        public const string AttackerWins = "attackerWins";
        public const string DefenderWins = "defenderWins";
        public const string NoWinner = "noWinner";
    }

    /// <summary>
    /// Constants for game chat message types
    /// </summary>
    public static class ChatMessageTypes
    {
        public const string Chat = "chat";
        public const string System = "system";
        public const string Alert = "alert";
        public const string Victory = "victory";
        public const string Defeat = "defeat";
        public const string PhaseChange = "phaseChange";
        public const string CardPlayed = "cardPlayed";
        public const string AbilityTriggered = "abilityTriggered";
    }

    /// <summary>
    /// Constants for effect names
    /// </summary>
    public static class EffectNames
    {
        public const string CannotDeclareConflictsOfType = "cannotDeclareConflictsOfType";
        public const string SetConflictDeclarationType = "setConflictDeclarationType";
        public const string SetMaxConflicts = "setMaxConflicts";
        public const string AlternateFatePool = "alternateFatePool";
        public const string FateCostToTarget = "fateCostToTarget";
        public const string ChangePlayerGloryModifier = "changePlayerGloryModifier";
        public const string ChangePlayerSkillModifier = "changePlayerSkillModifier";
        public const string ShowTopConflictCard = "showTopConflictCard";
        public const string ShowTopDynastyCard = "showTopDynastyCard";
        public const string EventsCannotBeCancelled = "eventsCannotBeCancelled";
        
        // Card ability effects
        public const string CopyCharacter = "copyCharacter";
        public const string GainAbility = "gainAbility";
        public const string Blank = "blank";
        public const string AddTrait = "addTrait";
        public const string AddFaction = "addFaction";
        public const string DoesNotReady = "doesNotReady";
        public const string CanBeSeenWhenFacedown = "canBeSeenWhenFacedown";
        public const string HideWhenFaceUp = "hideWhenFaceUp";
        public const string TakeControl = "takeControl";
        public const string IncreaseLimitOnAbilities = "increaseLimitOnAbilities";
        
        // Attachment effects
        public const string AttachmentLimit = "attachmentLimit";
        public const string AttachmentMyControlOnly = "attachmentMyControlOnly";
        public const string AttachmentUniqueRestriction = "attachmentUniqueRestriction";
        public const string AttachmentFactionRestriction = "attachmentFactionRestriction";
        public const string AttachmentTraitRestriction = "attachmentTraitRestriction";
        public const string CannotHaveOtherRestrictedAttachments = "cannotHaveOtherRestrictedAttachments";
    }

    // Note: Players constants are in EffectSource.cs

    /// <summary>
    /// Constants for deck references
    /// </summary>
    public static class Decks
    {
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
    }
}
