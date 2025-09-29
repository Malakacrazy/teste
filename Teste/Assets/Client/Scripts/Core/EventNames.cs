using UnityEngine;

namespace L5RGame
{
    public static partial class EventNames
    {
        public const string OnAddTokenToCard = "onAddTokenToCard";
        public const string OnBreakProvince = "onBreakProvince";
        public const string OnCancel = "onCancel";
        public const string OnCardAttached = "onCardAttached";
        public const string OnCardBowed = "onCardBowed";
        public const string OnCardDishonored = "onCardDishonored";
        public const string OnCardHonored = "onCardHonored";
        // OnCardLeavesPlay moved to Constants.cs to avoid duplicate definition
        public const string OnCardMoved = "onCardMoved";
        public const string OnCardReadied = "onCardReadied";
        public const string OnCardRevealed = "onCardRevealed";
        // OnCardsDiscarded moved to Constants.cs to avoid duplicate definition
        public const string OnCardsDiscardedFromHand = "onCardsDiscardedFromHand";
        // OnCardsDrawn moved to Constants.cs to avoid duplicate definition
        public const string OnCardTurnedFacedown = "onCardTurnedFacedown";
        public const string OnCharacterEntersPlay = "onCharacterEntersPlay";
        public const string OnChooseAction = "onChooseAction";
        public const string OnConditionalAction = "onConditionalAction";
        // OnConflictInitiated moved to Constants.cs to avoid duplicate definition
        public const string OnCreateToken = "onCreateToken";
        public const string OnDeckSearch = "onDeckSearch";
        public const string OnDeckShuffled = "onDeckShuffled";
        public const string OnDiscardFavor = "onDiscardFavor";
        // OnDuelInitiated moved to Constants.cs to avoid duplicate definition
        public const string OnDynastyCardTurnedFaceup = "onDynastyCardTurnedFaceup";
        // OnEffectApplied moved to Constants.cs to avoid duplicate definition
        public const string OnHandlerAction = "onHandlerAction";
        public const string OnIfAbleAction = "onIfAbleAction";
        public const string OnJointAction = "onJointAction";
        public const string OnLookAtCards = "onLookAtCards";
        public const string OnModifyBid = "onModifyBid";
        public const string OnModifyFate = "onModifyFate";
        public const string OnModifyHonor = "onModifyHonor";
        public const string OnMoveFate = "onMoveFate";
        public const string OnMoveToConflict = "onMoveToConflict";
        public const string OnMultipleAction = "onMultipleAction";
        public const string OnResolveConflictRing = "onResolveConflictRing";
        public const string OnResolveRingElement = "onResolveRingElement";
        public const string OnReturnRing = "onReturnRing";
        public const string OnSendHome = "onSendHome";
        public const string OnSequentialAction = "onSequentialAction";
        public const string OnSetHonorDial = "onSetHonorDial";
        public const string OnStatusTokenDiscarded = "onStatusTokenDiscarded";
        public const string OnSwitchConflictElement = "onSwitchConflictElement";
        public const string OnSwitchConflictType = "onSwitchConflictType";
        public const string OnTakeRing = "onTakeRing";
        public const string OnTransferHonor = "onTransferHonor";
        // OnPhaseEnded moved to Constants.cs to avoid duplicate definition
        public const string OnPhaseCreated = "onPhaseCreated";
        // OnPhaseStarted moved to Constants.cs to avoid duplicate definition
        public const string OnPassDuringDynasty = "onPassDuringDynasty";
        public const string OnPassFirstPlayer = "onPassFirstPlayer";
        // OnRoundEnded moved to Constants.cs to avoid duplicate definition
        // Additional event names from Game.cs partial class
        // Unnamed moved to Constants.cs to avoid duplicate definition
        public const string OnCardAbilityInitiated = "onCardAbilityInitiated";
        // OnCardPlayed moved to Constants.cs to avoid duplicate definition
        public const string OnCardAbilityTriggered = "onCardAbilityTriggered";
        // OnDefendersDeclared moved to Constants.cs to avoid duplicate definition
        // AfterConflict moved to Constants.cs to avoid duplicate definition
        // OnCovertResolved moved to Constants.cs to avoid duplicate definition
        // OnClaimRing moved to Constants.cs to avoid duplicate definition
        // OnReturnHome moved to Constants.cs to avoid duplicate definition
        // OnParticipantsReturnHome moved to Constants.cs to avoid duplicate definition
        // Additional event names from EffectEngine.cs partial class
        // OnConflictFinished moved to Constants.cs to avoid duplicate definition
        // OnDuelFinished moved to Constants.cs to avoid duplicate definition
        // OnPassActionPhasePriority moved to Constants.cs to avoid duplicate definition
        // Additional missing event names found in errors
        // OnHonorDialsRevealed moved to Constants.cs to avoid duplicate definition
        public const string AfterDuel = "afterDuel";
        public const string OnDuelResolution = "onDuelResolution";
        
        // Conflict-specific event names (moved from Conflict.cs and Constants.cs)
        public const string OnConflictPass = "onConflictPass";
        public const string OnConflictDeclared = "onConflictDeclared";
        public const string OnAttackersChosen = "onAttackersChosen";
        public const string OnDefendersChosen = "onDefendersChosen";
        public const string OnConflictResolved = "onConflictResolved";
        
        // Additional event names (moved from EffectSource.cs)
        public const string OnGameStateChanged = "onGameStateChanged";
        public const string OnPassPriority = "onPassPriority";
        public const string OnDuelEnded = "onDuelEnded";
        
        // Status token event names (moved from StatusToken.cs)
        public const string OnStatusTokenAdded = "onStatusTokenAdded";
    }
}