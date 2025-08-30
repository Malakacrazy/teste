using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    // These are partial class definitions that extend the base implementations
    // Use partial to allow classes to be split across multiple files
    
    #region Card Actions
    
    // Note: These partial declarations are defined in their respective main files
    
    public partial class PlayCardAction : CardGameAction
    {
        public PlayCardAction() : base() { }
        public PlayCardAction(CardActionProperties properties) : base(properties) { }
        public PlayCardAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class DiscardCardAction : CardGameAction
    {
        public DiscardCardAction() : base() { }
        public DiscardCardAction(CardActionProperties properties) : base(properties) { }
        public DiscardCardAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class MoveCardAction : CardGameAction
    {
        public MoveCardAction() : base() { }
        public MoveCardAction(CardActionProperties properties) : base(properties) { }
        public MoveCardAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class RevealAction : CardGameAction
    {
        public RevealAction() : base() { }
        public RevealAction(CardActionProperties properties) : base(properties) { }
        public RevealAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class BowAction : CardGameAction
    {
        public BowAction() : base() { }
        public BowAction(CardActionProperties properties) : base(properties) { }
        public BowAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class ReadyAction : CardGameAction
    {
        public ReadyAction() : base() { }
        public ReadyAction(CardActionProperties properties) : base(properties) { }
        public ReadyAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class HonorAction : CardGameAction
    {
        public HonorAction() : base() { }
        public HonorAction(CardActionProperties properties) : base(properties) { }
        public HonorAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class DishonorAction : CardGameAction
    {
        public DishonorAction() : base() { }
        public DishonorAction(CardActionProperties properties) : base(properties) { }
        public DishonorAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class AttachAction : CardGameAction
    {
        public AttachAction() : base() { }
        public AttachAction(CardActionProperties properties) : base(properties) { }
        public AttachAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class SendHomeAction : CardGameAction
    {
        public SendHomeAction() : base() { }
        public SendHomeAction(CardActionProperties properties) : base(properties) { }
        public SendHomeAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class BreakAction : CardGameAction
    {
        public BreakAction() : base() { }
        public BreakAction(CardActionProperties properties) : base(properties) { }
        public BreakAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class PutIntoPlayAction : CardGameAction
    {
        public PutIntoPlayAction() : base() { }
        public PutIntoPlayAction(CardActionProperties properties) : base(properties) { }
        public PutIntoPlayAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class TakeControlAction : CardGameAction
    {
        public TakeControlAction() : base() { }
        public TakeControlAction(CardActionProperties properties) : base(properties) { }
        public TakeControlAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class ReturnToHandAction : CardGameAction
    {
        public ReturnToHandAction() : base() { }
        public ReturnToHandAction(CardActionProperties properties) : base(properties) { }
        public ReturnToHandAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class ReturnToDeckAction : CardGameAction
    {
        public ReturnToDeckAction() : base() { }
        public ReturnToDeckAction(CardActionProperties properties) : base(properties) { }
        public ReturnToDeckAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class RemoveFromGameAction : CardGameAction
    {
        public RemoveFromGameAction() : base() { }
        public RemoveFromGameAction(CardActionProperties properties) : base(properties) { }
        public RemoveFromGameAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class FlipDynastyAction : CardGameAction
    {
        public FlipDynastyAction() : base() { }
        public FlipDynastyAction(CardActionProperties properties) : base(properties) { }
        public FlipDynastyAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class DiscardFromPlayAction : CardGameAction
    {
        public DiscardFromPlayAction() : base() { }
        public DiscardFromPlayAction(CardActionProperties properties) : base(properties) { }
        public DiscardFromPlayAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class RemoveFateAction : CardGameAction
    {
        public RemoveFateAction() : base() { }
        public RemoveFateAction(CardActionProperties properties) : base(properties) { }
        public RemoveFateAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class CreateTokenAction : CardGameAction
    {
        public CreateTokenAction() : base() { }
        public CreateTokenAction(CardActionProperties properties) : base(properties) { }
        public CreateTokenAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class TurnCardFacedownAction : CardGameAction
    {
        public TurnCardFacedownAction() : base() { }
        public TurnCardFacedownAction(CardActionProperties properties) : base(properties) { }
        public TurnCardFacedownAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class LookAtAction : CardGameAction
    {
        public LookAtAction() : base() { }
        public LookAtAction(CardActionProperties properties) : base(properties) { }
        public LookAtAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class DuelAction : CardGameAction
    {
        public DuelAction() : base() { }
        public DuelAction(CardActionProperties properties) : base(properties) { }
        public DuelAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class MoveToConflictAction : CardGameAction
    {
        public MoveToConflictAction() : base() { }
        public MoveToConflictAction(CardActionProperties properties) : base(properties) { }
        public MoveToConflictAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    public partial class AttachToRingAction : CardGameAction
    {
        public AttachToRingAction() : base() { }
        public AttachToRingAction(CardActionProperties properties) : base(properties) { }
        public AttachToRingAction(Func<AbilityContext, CardActionProperties> factory) : base(factory) { }
    }
    
    #endregion
    
    #region Player Actions
    
    public partial class DrawAction : PlayerAction
    {
        public DrawAction() : base() { }
        public DrawAction(PlayerActionProperties properties) : base(properties) { }
        public DrawAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class GainFateAction : PlayerAction
    {
        public GainFateAction() : base() { }
        public GainFateAction(PlayerActionProperties properties) : base(properties) { }
        public GainFateAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class LoseFateAction : PlayerAction
    {
        public LoseFateAction() : base() { }
        public LoseFateAction(PlayerActionProperties properties) : base(properties) { }
        public LoseFateAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class GainHonorAction : PlayerAction
    {
        public GainHonorAction() : base() { }
        public GainHonorAction(PlayerActionProperties properties) : base(properties) { }
        public GainHonorAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class LoseHonorAction : PlayerAction
    {
        public LoseHonorAction() : base() { }
        public LoseHonorAction(PlayerActionProperties properties) : base(properties) { }
        public LoseHonorAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class ModifyBidAction : PlayerAction
    {
        public ModifyBidAction() : base() { }
        public ModifyBidAction(PlayerActionProperties properties) : base(properties) { }
        public ModifyBidAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class SetDialAction : PlayerAction
    {
        public SetDialAction() : base() { }
        public SetDialAction(PlayerActionProperties properties) : base(properties) { }
        public SetDialAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class InitiateConflictAction : PlayerAction
    {
        public InitiateConflictAction() : base() { }
        public InitiateConflictAction(PlayerActionProperties properties) : base(properties) { }
        public InitiateConflictAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class TransferFateAction : PlayerAction
    {
        public TransferFateAction() : base() { }
        public TransferFateAction(PlayerActionProperties properties) : base(properties) { }
        public TransferFateAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class TransferHonorAction : PlayerAction
    {
        public TransferHonorAction() : base() { }
        public TransferHonorAction(PlayerActionProperties properties) : base(properties) { }
        public TransferHonorAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class ChosenDiscardAction : PlayerAction
    {
        public ChosenDiscardAction() : base() { }
        public ChosenDiscardAction(PlayerActionProperties properties) : base(properties) { }
        public ChosenDiscardAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class RandomDiscardAction : PlayerAction
    {
        public RandomDiscardAction() : base() { }
        public RandomDiscardAction(PlayerActionProperties properties) : base(properties) { }
        public RandomDiscardAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class DeckSearchAction : PlayerAction
    {
        public DeckSearchAction() : base() { }
        public DeckSearchAction(PlayerActionProperties properties) : base(properties) { }
        public DeckSearchAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class ShuffleDeckAction : PlayerAction
    {
        public ShuffleDeckAction() : base() { }
        public ShuffleDeckAction(PlayerActionProperties properties) : base(properties) { }
        public ShuffleDeckAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class RefillFaceupAction : PlayerAction
    {
        public RefillFaceupAction() : base() { }
        public RefillFaceupAction(PlayerActionProperties properties) : base(properties) { }
        public RefillFaceupAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    public partial class DiscardFavorAction : PlayerAction
    {
        public DiscardFavorAction() : base() { }
        public DiscardFavorAction(PlayerActionProperties properties) : base(properties) { }
        public DiscardFavorAction(Func<AbilityContext, PlayerActionProperties> factory) : base(factory) { }
    }
    
    #endregion
    
    #region Ring Actions
    
    public partial class PlaceFateAction : RingAction
    {
        public PlaceFateAction() : base() { }
        public PlaceFateAction(GameActionProperties properties) : base(properties) { }
        public PlaceFateAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class SelectRingAction : RingAction
    {
        public SelectRingAction() : base() { }
        public SelectRingAction(GameActionProperties properties) : base(properties) { }
        public SelectRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class ReturnRingAction : RingAction
    {
        public ReturnRingAction() : base() { }
        public ReturnRingAction(GameActionProperties properties) : base(properties) { }
        public ReturnRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class TakeRingAction : RingAction
    {
        public TakeRingAction() : base() { }
        public TakeRingAction(GameActionProperties properties) : base(properties) { }
        public TakeRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class TakeFateRingAction : RingAction
    {
        public TakeFateRingAction() : base() { }
        public TakeFateRingAction(GameActionProperties properties) : base(properties) { }
        public TakeFateRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class PlaceFateRingAction : RingAction
    {
        public PlaceFateRingAction() : base() { }
        public PlaceFateRingAction(GameActionProperties properties) : base(properties) { }
        public PlaceFateRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class ResolveConflictRingAction : RingAction
    {
        public ResolveConflictRingAction() : base() { }
        public ResolveConflictRingAction(GameActionProperties properties) : base(properties) { }
        public ResolveConflictRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class ResolveElementAction : RingAction
    {
        public ResolveElementAction() : base() { }
        public ResolveElementAction(GameActionProperties properties) : base(properties) { }
        public ResolveElementAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class SwitchConflictElementAction : RingAction
    {
        public SwitchConflictElementAction() : base() { }
        public SwitchConflictElementAction(GameActionProperties properties) : base(properties) { }
        public SwitchConflictElementAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class SwitchConflictTypeAction : RingAction
    {
        public SwitchConflictTypeAction() : base() { }
        public SwitchConflictTypeAction(GameActionProperties properties) : base(properties) { }
        public SwitchConflictTypeAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class LastingEffectRingAction : RingAction
    {
        public LastingEffectRingAction() : base() { }
        public LastingEffectRingAction(GameActionProperties properties) : base(properties) { }
        public LastingEffectRingAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    #endregion
    
    #region Token Actions
    
    public partial class AddTokenAction : TokenAction
    {
        public AddTokenAction() : base() { }
        public AddTokenAction(L5RGame.GameActionProperties properties) : base(properties) { }
        public AddTokenAction(Func<AbilityContext, L5RGame.GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class DiscardStatusAction : TokenAction
    {
        public DiscardStatusAction() : base() { }
        public DiscardStatusAction(L5RGame.GameActionProperties properties) : base(properties) { }
        public DiscardStatusAction(Func<AbilityContext, L5RGame.GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class MoveTokenAction : TokenAction
    {
        public MoveTokenAction() : base() { }
        public MoveTokenAction(L5RGame.GameActionProperties properties) : base(properties) { }
        public MoveTokenAction(Func<AbilityContext, L5RGame.GameActionProperties> factory) : base(factory) { }
    }
    
    #endregion
    
    #region General Actions
    
    public partial class LastingEffectCardAction : GameAction
    {
        public LastingEffectCardAction() : base() { }
        public LastingEffectCardAction(GameActionProperties properties) : base(properties) { }
        public LastingEffectCardAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class ResolveAbilityAction : GameAction
    {
        public ResolveAbilityAction() : base() { }
        public ResolveAbilityAction(GameActionProperties properties) : base(properties) { }
        public ResolveAbilityAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class SelectCardAction : GameAction
    {
        public SelectCardAction() : base() { }
        public SelectCardAction(GameActionProperties properties) : base(properties) { }
        public SelectCardAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    public partial class CardMenuAction : GameAction
    {
        public CardMenuAction() : base() { }
        public CardMenuAction(GameActionProperties properties) : base(properties) { }
        public CardMenuAction(Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }
    
    #endregion
}
