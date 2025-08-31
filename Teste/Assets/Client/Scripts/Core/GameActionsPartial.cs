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
    }
    
    public partial class DiscardCardAction : CardGameAction
    {
    }
    
    public partial class ReadyAction : CardGameAction
    {
    }
    
    public partial class AttachAction : CardGameAction
    {
    }
    
    public partial class SendHomeAction : CardGameAction
    {
    }
    
    public partial class PutIntoPlayAction : CardGameAction
    {
    }
    
    public partial class ReturnToHandAction : CardGameAction
    {
    }
    
    public partial class RemoveFromGameAction : CardGameAction
    {
    }
    
    public partial class FlipDynastyAction : CardGameAction
    {
    }
    
    public partial class DiscardFromPlayAction : CardGameAction
    {
    }
    
    public partial class RemoveFateAction : CardGameAction
    {
    }
    
    public partial class CreateTokenAction : CardGameAction
    {
    }
    
    public partial class TurnCardFacedownAction : CardGameAction
    {
    }
    
    public partial class LookAtAction : CardGameAction
    {
    }
    
    public partial class DuelAction : CardGameAction
    {
    }
    
    public partial class MoveToConflictAction : CardGameAction
    {
    }
    
    #endregion
    
    #region Player Actions
    
    public partial class GainFateAction : PlayerAction
    {
    }
    
    public partial class LoseFateAction : PlayerAction
    {
    }
    
    public partial class LoseHonorAction : PlayerAction
    {
    }
    
    public partial class ModifyBidAction : PlayerAction
    {
    }
    
    public partial class SetDialAction : PlayerAction
    {
    }
    
    public partial class InitiateConflictAction : PlayerAction
    {
    }
    
    public partial class TransferFateAction : PlayerAction
    {
    }
    
    public partial class TransferHonorAction : PlayerAction
    {
    }
    
    public partial class ChosenDiscardAction : PlayerAction
    {
    }
    
    public partial class RandomDiscardAction : PlayerAction
    {
    }
    
    public partial class DeckSearchAction : PlayerAction
    {
    }
    
    public partial class ShuffleDeckAction : PlayerAction
    {
    }
    
    public partial class RefillFaceupAction : PlayerAction
    {
    }
    
    public partial class DiscardFavorAction : PlayerAction
    {
    }
    
    #endregion
    
    #region Ring Actions
    
    public partial class PlaceFateAction : RingAction
    {
    }
    
    public partial class SelectRingAction : RingAction
    {
    }
    
    public partial class ReturnRingAction : RingAction
    {
    }
    
    public partial class TakeRingAction : RingAction
    {
    }
    
    public partial class TakeFateRingAction : RingAction
    {
    }
    
    public partial class PlaceFateRingAction : RingAction
    {
    }
    
    public partial class ResolveConflictRingAction : RingAction
    {
    }
    
    public partial class ResolveElementAction : RingAction
    {
    }
    
    public partial class SwitchConflictElementAction : RingAction
    {
    }
    
    public partial class SwitchConflictTypeAction : RingAction
    {
    }
    
    public partial class LastingEffectRingAction : RingAction
    {
    }
    
    #endregion
    
    #region Token Actions
    
    public partial class AddTokenAction : TokenAction
    {
    }
    
    public partial class DiscardStatusAction : TokenAction
    {
    }
    
    public partial class MoveTokenAction : TokenAction
    {
    }
    
    #endregion
    
    #region General Actions
    
    public partial class LastingEffectCardAction : GameAction
    {
    }
    
    public partial class ResolveAbilityAction : GameAction
    {
    }
    
    public partial class SelectCardAction : GameAction
    {
    }
    
    public partial class CardMenuAction : GameAction
    {
    }
    
    #endregion
}