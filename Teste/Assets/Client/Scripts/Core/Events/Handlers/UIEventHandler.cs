using System;
using System.Collections.Generic;
using L5RGame.Events;
using UnityEngine;

namespace L5RGame.EventSystem.Handlers
{
    /// <summary>
    /// Event handler that manages UI updates based on game events.
    /// Replaces direct UI calls with event-driven UI updates.
    /// </summary>
    public class UIEventHandler : BaseMultiEventHandler
    {
        #region Private Fields
        
        private Game _game;
        private readonly Dictionary<string, System.Action<GameEvent>> _uiUpdateActions;
        
        #endregion
        
        #region Properties
        
        public override string HandlerName => "UI Event Handler";
        
        #endregion
        
        #region Constructor
        
        public UIEventHandler()
        {
            _uiUpdateActions = new Dictionary<string, System.Action<GameEvent>>();
            InitializeUIUpdateActions();
        }
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(IEventBus eventBus)
        {
            // Get the game instance
            _game = Game.Instance;
            if (_game == null)
            {
                throw new InvalidOperationException("Game instance not found - cannot initialize UIEventHandler");
            }
            
            base.Initialize(eventBus);
        }
        
        private void InitializeUIUpdateActions()
        {
            _uiUpdateActions["FateRemoved"] = HandleFateRemovedUI;
            _uiUpdateActions["CharacterHonored"] = HandleCharacterHonoredUI;
            _uiUpdateActions["CharacterDishonored"] = HandleCharacterDishonoredUI;
            _uiUpdateActions["CharacterStatusChanged"] = HandleCharacterStatusChangedUI;
            _uiUpdateActions["CardDrawn"] = HandleCardDrawnUI;
            _uiUpdateActions["CardPlayed"] = HandleCardPlayedUI;
            _uiUpdateActions["RingResolved"] = HandleRingResolvedUI;
            _uiUpdateActions["ConflictStarted"] = HandleConflictStartedUI;
            _uiUpdateActions["CharacterLeavesPlay"] = HandleCharacterLeavesPlayUI;
            _uiUpdateActions["CardMoved"] = HandleCardMovedUI;
            _uiUpdateActions["CharacterBowed"] = HandleCharacterBowedUI;
            _uiUpdateActions["CharacterReadied"] = HandleCharacterReadiedUI;
            _uiUpdateActions["EarthRingDrawDiscard"] = HandleEarthRingDrawDiscardUI;
            _uiUpdateActions["EarthRingNotResolved"] = HandleEarthRingNotResolvedUI;
            _uiUpdateActions["AirRingGainHonor"] = HandleAirRingGainHonorUI;
            _uiUpdateActions["AirRingTakeHonor"] = HandleAirRingTakeHonorUI;
            _uiUpdateActions["AirRingNotResolved"] = HandleAirRingNotResolvedUI;
        }
        
        #endregion
        
        #region Handled Event Types
        
        public override Type[] GetHandledEventTypes()
        {
            return new Type[]
            {
                typeof(FateRemovedEvent),
                typeof(CharacterHonoredEvent),
                typeof(CharacterDishonoredEvent),
                typeof(CharacterStatusChangedEvent),
                typeof(CardDrawnEvent),
                typeof(CardPlayedEvent),
                typeof(RingResolvedEvent),
                typeof(ConflictStartedEvent),
                typeof(CharacterLeavesPlayEvent),
                typeof(CardMovedEvent),
                typeof(CharacterBowedEvent),
                typeof(CharacterReadiedEvent),
                typeof(EarthRingDrawDiscardEvent),
                typeof(EarthRingNotResolvedEvent),
                typeof(AirRingGainHonorEvent),
                typeof(AirRingTakeHonorEvent),
                typeof(AirRingNotResolvedEvent)
            };
        }
        
        #endregion
        
        #region Event Handling
        
        public override void HandleEvent(GameEvent gameEvent)
        {
            if (gameEvent == null || _game == null)
                return;
            
            try
            {
                // Execute specific UI update action if available
                if (_uiUpdateActions.TryGetValue(gameEvent.EventName, out var action))
                {
                    action(gameEvent);
                }
                
                // Always trigger a general UI refresh for any game event
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                OnError(ex, gameEvent.EventName);
            }
        }
        
        #endregion
        
        #region Specific UI Handlers
        
        private void HandleFateRemovedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as FateRemovedEvent;
            if (evt?.Character == null) return;
            
            // Update character fate display
            RefreshCharacterUI(evt.Character);
            
            // Show fate removal animation/effect
            ShowFateRemovedEffect(evt.Character, evt.AmountRemoved);
            
            // If character will leave play, prepare exit animation
            if (evt.WillLeavePlay)
            {
                PrepareCharacterExitAnimation(evt.Character);
            }
        }
        
        private void HandleCharacterHonoredUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CharacterHonoredEvent;
            if (evt?.Character == null) return;
            
            // Update character honor status display
            RefreshCharacterUI(evt.Character);
            
            // Show honor effect
            if (!evt.WasAlreadyHonored)
            {
                ShowHonorEffect(evt.Character);
            }
        }
        
        private void HandleCharacterDishonoredUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CharacterDishonoredEvent;
            if (evt?.Character == null) return;
            
            // Update character dishonor status display
            RefreshCharacterUI(evt.Character);
            
            // Show dishonor effect
            if (!evt.WasAlreadyDishonored)
            {
                ShowDishonorEffect(evt.Character);
            }
        }
        
        private void HandleCharacterStatusChangedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CharacterStatusChangedEvent;
            if (evt?.Character == null) return;
            
            // Update character bow/ready status display
            RefreshCharacterUI(evt.Character);
            
            // Show bow/ready animation
            if (evt.IsBowed && !evt.WasBowed)
            {
                ShowBowAnimation(evt.Character);
            }
            else if (!evt.IsBowed && evt.WasBowed)
            {
                ShowReadyAnimation(evt.Character);
            }
        }
        
        private void HandleCardDrawnUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CardDrawnEvent;
            if (evt?.DrawingPlayer == null) return;
            
            // Update hand size display
            RefreshPlayerHandUI(evt.DrawingPlayer);
            
            // Show card draw animation
            ShowCardDrawAnimation(evt.DrawingPlayer, evt.DrawnFrom);
            
            // Update deck size if drawn from deck
            if (evt.DrawnFrom == "deck")
            {
                RefreshPlayerDeckUI(evt.DrawingPlayer);
            }
        }
        
        private void HandleCardPlayedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CardPlayedEvent;
            if (evt?.Card == null || evt?.PlayingPlayer == null) return;
            
            // Update player hand and fate display
            RefreshPlayerHandUI(evt.PlayingPlayer);
            RefreshPlayerFateUI(evt.PlayingPlayer);
            
            // Show card play animation
            ShowCardPlayAnimation(evt.Card, evt.PlayedTo);
            
            // Update play area
            RefreshPlayAreaUI();
        }
        
        private void HandleRingResolvedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as RingResolvedEvent;
            if (evt?.Ring == null) return;
            
            // Update ring display
            RefreshRingUI(evt.Ring);
            
            // Show ring resolution effect
            ShowRingResolutionEffect(evt.Ring, evt.EffectChosen);
            
            // Update any target if specified
            if (evt.EffectTarget is BaseCard targetCard)
            {
                RefreshCharacterUI(targetCard);
            }
        }
        
        private void HandleConflictStartedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as ConflictStartedEvent;
            if (evt?.Conflict == null) return;
            
            // Switch to conflict UI mode
            ShowConflictUI(evt.Conflict);
            
            // Update ring display for conflict
            if (evt.TargetRing != null)
            {
                RefreshRingUI(evt.TargetRing);
                HighlightConflictRing(evt.TargetRing);
            }
            
            // Show conflict start animation
            ShowConflictStartAnimation(evt.Conflict);
        }
        
        private void HandleCharacterLeavesPlayUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CharacterLeavesPlayEvent;
            if (evt?.Character == null) return;
            
            // Show character exit animation
            if (System.Enum.TryParse<CardLocation>(evt.Destination, out var destination))
            {
                ShowCharacterExitAnimation(evt.Character, destination);
            }
            
            // Update play area after animation completes
            DelayedRefreshPlayAreaUI();
        }
        
        private void HandleCardMovedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CardMovedEvent;
            if (evt?.Card == null) return;
            
            // Show card movement animation
            ShowCardMovementAnimation(evt.Card, evt.FromLocation, evt.ToLocation);
            
            // Update relevant UI areas
            RefreshLocationUI(evt.FromLocation);
            RefreshLocationUI(evt.ToLocation);
            
            // Update controller displays if changed
            if (evt.FromController != evt.ToController)
            {
                RefreshPlayerUI(evt.FromController);
                RefreshPlayerUI(evt.ToController);
            }
        }
        
        #endregion
        
        #region UI Helper Methods
        
        /// <summary>
        /// Refresh the general game UI
        /// </summary>
        private void RefreshGameUI()
        {
            try
            {
                // Request UI refresh through game's public method
                // Cannot invoke event directly from outside the declaring class
                Debug.Log("🔄 Game UI refresh requested");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh game UI: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a specific character
        /// </summary>
        /// <param name="character">Character to refresh</param>
        private void RefreshCharacterUI(BaseCard character)
        {
            try
            {
                // In a real implementation, this would update specific character UI elements
                // For now, trigger a general refresh
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh character UI for {character?.Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a player's hand
        /// </summary>
        /// <param name="player">Player whose hand to refresh</param>
        private void RefreshPlayerHandUI(Player player)
        {
            try
            {
                // Update hand size display, card positions, etc.
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh hand UI for {player?.name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a player's fate display
        /// </summary>
        /// <param name="player">Player whose fate to refresh</param>
        private void RefreshPlayerFateUI(Player player)
        {
            try
            {
                // Update fate counter display
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh fate UI for {player?.name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a player's deck
        /// </summary>
        /// <param name="player">Player whose deck to refresh</param>
        private void RefreshPlayerDeckUI(Player player)
        {
            try
            {
                // Update deck size display
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh deck UI for {player?.name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for the play area
        /// </summary>
        private void RefreshPlayAreaUI()
        {
            try
            {
                // Update character positions, attachments, etc.
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh play area UI: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a specific ring
        /// </summary>
        /// <param name="ring">Ring to refresh</param>
        private void RefreshRingUI(Ring ring)
        {
            try
            {
                // Update ring status, claimed state, etc.
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh ring UI for {ring?.element}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a specific card location
        /// </summary>
        /// <param name="location">Location to refresh</param>
        private void RefreshLocationUI(CardLocation location)
        {
            try
            {
                // Update specific location displays
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh location UI for {location}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh UI for a specific player
        /// </summary>
        /// <param name="player">Player to refresh</param>
        private void RefreshPlayerUI(Player player)
        {
            try
            {
                // Update all player-related UI elements
                RefreshPlayerHandUI(player);
                RefreshPlayerFateUI(player);
                RefreshPlayerDeckUI(player);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh player UI for {player?.name}: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Animation and Effect Methods
        
        /// <summary>
        /// Show fate removed effect
        /// </summary>
        /// <param name="character">Character losing fate</param>
        /// <param name="amount">Amount of fate removed</param>
        private void ShowFateRemovedEffect(BaseCard character, int amount)
        {
            Debug.Log($"🎭 Showing fate removed effect: -{amount} fate from {character.Name}");
            // In a real implementation, this would trigger particle effects, animations, etc.
        }
        
        /// <summary>
        /// Show honor effect
        /// </summary>
        /// <param name="character">Character being honored</param>
        private void ShowHonorEffect(BaseCard character)
        {
            Debug.Log($"🎭 Showing honor effect on {character.Name}");
            // Golden glow, honor token animation, etc.
        }
        
        /// <summary>
        /// Show dishonor effect
        /// </summary>
        /// <param name="character">Character being dishonored</param>
        private void ShowDishonorEffect(BaseCard character)
        {
            Debug.Log($"🎭 Showing dishonor effect on {character.Name}");
            // Dark aura, dishonor token animation, etc.
        }
        
        /// <summary>
        /// Show bow animation
        /// </summary>
        /// <param name="character">Character being bowed</param>
        private void ShowBowAnimation(BaseCard character)
        {
            Debug.Log($"🎭 Showing bow animation for {character.Name}");
            // Rotate character card, change opacity, etc.
        }
        
        /// <summary>
        /// Show ready animation
        /// </summary>
        /// <param name="character">Character being readied</param>
        private void ShowReadyAnimation(BaseCard character)
        {
            Debug.Log($"🎭 Showing ready animation for {character.Name}");
            // Unrotate character card, restore opacity, etc.
        }
        
        /// <summary>
        /// Show card draw animation
        /// </summary>
        /// <param name="player">Player drawing cards</param>
        /// <param name="source">Source of the draw</param>
        private void ShowCardDrawAnimation(Player player, string source)
        {
            Debug.Log($"🎭 Showing card draw animation for {player.name} from {source}");
            // Card flying from deck/source to hand
        }
        
        /// <summary>
        /// Show card play animation
        /// </summary>
        /// <param name="card">Card being played</param>
        /// <param name="destination">Where the card is being played</param>
        private void ShowCardPlayAnimation(BaseCard card, string destination)
        {
            Debug.Log($"🎭 Showing card play animation: {card.Name} to {destination}");
            // Card flying from hand to play area
        }
        
        /// <summary>
        /// Show ring resolution effect
        /// </summary>
        /// <param name="ring">Ring being resolved</param>
        /// <param name="effect">Effect chosen</param>
        private void ShowRingResolutionEffect(Ring ring, string effect)
        {
            Debug.Log($"🎭 Showing ring resolution effect: {ring.element} ring - {effect}");
            // Ring glowing, effect particles, etc.
        }
        
        /// <summary>
        /// Show conflict UI
        /// </summary>
        /// <param name="conflict">Conflict starting</param>
        private void ShowConflictUI(Conflict conflict)
        {
            Debug.Log($"🎭 Showing conflict UI for {conflict.conflictType} conflict");
            // Switch to conflict view, highlight participants, etc.
        }
        
        /// <summary>
        /// Highlight the ring being contested
        /// </summary>
        /// <param name="ring">Ring in conflict</param>
        private void HighlightConflictRing(Ring ring)
        {
            Debug.Log($"🎭 Highlighting conflict ring: {ring.element}");
            // Add highlight border, pulsing effect, etc.
        }
        
        /// <summary>
        /// Show conflict start animation
        /// </summary>
        /// <param name="conflict">Conflict starting</param>
        private void ShowConflictStartAnimation(Conflict conflict)
        {
            Debug.Log($"🎭 Showing conflict start animation");
            // Battle transition effects, sound effects, etc.
        }
        
        /// <summary>
        /// Prepare character exit animation
        /// </summary>
        /// <param name="character">Character leaving play</param>
        private void PrepareCharacterExitAnimation(BaseCard character)
        {
            Debug.Log($"🎭 Preparing exit animation for {character.Name}");
            // Fade out, shrink, etc.
        }
        
        /// <summary>
        /// Show character exit animation
        /// </summary>
        /// <param name="character">Character leaving play</param>
        /// <param name="destination">Where character is going</param>
        private void ShowCharacterExitAnimation(BaseCard character, CardLocation destination)
        {
            Debug.Log($"🎭 Showing exit animation: {character.Name} to {destination}");
            // Full exit animation with destination-appropriate effects
        }
        
        /// <summary>
        /// Show card movement animation
        /// </summary>
        /// <param name="card">Card being moved</param>
        /// <param name="from">Source location</param>
        /// <param name="to">Destination location</param>
        private void ShowCardMovementAnimation(BaseCard card, CardLocation from, CardLocation to)
        {
            Debug.Log($"🎭 Showing card movement: {card.Name} from {from} to {to}");
            // Card flying between locations
        }
        
        /// <summary>
        /// Refresh play area UI after a delay (for animations)
        /// </summary>
        private void DelayedRefreshPlayAreaUI()
        {
            // In a real implementation, this would use coroutines or async/await
            // For now, just do an immediate refresh
            RefreshPlayAreaUI();
        }
        
        /// <summary>
        /// Handle character bowed event UI
        /// </summary>
        /// <param name="gameEvent">Character bowed event</param>
        private void HandleCharacterBowedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CharacterBowedEvent;
            if (evt?.Character == null) return;
            
            // Update character bow status display
            RefreshCharacterUI(evt.Character);
            
            // Show bow animation if character wasn't already bowed
            if (!evt.WasAlreadyBowed)
            {
                ShowBowAnimation(evt.Character);
                Debug.Log($"🎭 {evt.Character.Name} bowed ({evt.Reason})");
            }
        }
        
        /// <summary>
        /// Handle character readied event UI
        /// </summary>
        /// <param name="gameEvent">Character readied event</param>
        private void HandleCharacterReadiedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as CharacterReadiedEvent;
            if (evt?.Character == null) return;
            
            // Update character ready status display
            RefreshCharacterUI(evt.Character);
            
            // Show ready animation if character wasn't already ready
            if (!evt.WasAlreadyReady)
            {
                ShowReadyAnimation(evt.Character);
                Debug.Log($"🎭 {evt.Character.Name} readied ({evt.Reason})");
            }
        }
        
        /// <summary>
        /// Handle earth ring draw discard event UI
        /// </summary>
        /// <param name="gameEvent">Earth ring draw discard event</param>
        private void HandleEarthRingDrawDiscardUI(GameEvent gameEvent)
        {
            var evt = gameEvent as EarthRingDrawDiscardEvent;
            if (evt?.TriggeredBy == null) return;
            
            // Update player hand UI
            RefreshPlayerHandUI(evt.TriggeredBy);
            
            // Update opponent hand if they discarded
            if (evt.OpponentDiscarded && evt.TriggeredBy.Opponent != null)
            {
                RefreshPlayerHandUI(evt.TriggeredBy.Opponent);
                
                // Show discard animation
                ShowCardDiscardAnimation(evt.TriggeredBy.Opponent, evt.CardsDiscarded, evt.DiscardWasRandom);
            }
            
            // Show draw animation
            ShowCardDrawAnimation(evt.TriggeredBy, "deck");
            
            // Show earth ring resolution effect
            ShowEarthRingEffect(evt.CardsDrawn, evt.CardsDiscarded);
            
            Debug.Log($"🎭 Earth ring resolved: {evt.TriggeredBy.Name} drew {evt.CardsDrawn}, opponent discarded {evt.CardsDiscarded}");
        }
        
        /// <summary>
        /// Handle earth ring not resolved event UI
        /// </summary>
        /// <param name="gameEvent">Earth ring not resolved event</param>
        private void HandleEarthRingNotResolvedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as EarthRingNotResolvedEvent;
            if (evt?.TriggeredBy == null) return;
            
            // Show "not resolved" message/effect
            ShowRingNotResolvedEffect("earth");
            
            Debug.Log($"🎭 Earth ring not resolved by {evt.TriggeredBy.Name} ({evt.Reason})");
        }
        
        /// <summary>
        /// Handle air ring gain honor event UI
        /// </summary>
        /// <param name="gameEvent">Air ring gain honor event</param>
        private void HandleAirRingGainHonorUI(GameEvent gameEvent)
        {
            var evt = gameEvent as AirRingGainHonorEvent;
            if (evt?.TriggeredBy == null) return;
            
            // Update player honor display
            RefreshPlayerHonorUI(evt.TriggeredBy);
            
            // Show honor gain animation
            ShowHonorGainAnimation(evt.TriggeredBy, evt.HonorGained);
            
            // Show air ring resolution effect
            ShowAirRingEffect("gain_honor", evt.HonorGained);
            
            Debug.Log($"🎭 Air ring resolved: {evt.TriggeredBy.Name} gained {evt.HonorGained} honor (total: {evt.TotalHonorAfter})");
        }
        
        /// <summary>
        /// Handle air ring take honor event UI
        /// </summary>
        /// <param name="gameEvent">Air ring take honor event</param>
        private void HandleAirRingTakeHonorUI(GameEvent gameEvent)
        {
            var evt = gameEvent as AirRingTakeHonorEvent;
            if (evt?.TriggeredBy == null || evt?.Target == null) return;
            
            // Update both players' honor displays
            RefreshPlayerHonorUI(evt.TriggeredBy);
            RefreshPlayerHonorUI(evt.Target);
            
            // Show honor transfer animation
            ShowHonorTransferAnimation(evt.Target, evt.TriggeredBy, evt.HonorTaken);
            
            // Show air ring resolution effect
            ShowAirRingEffect("take_honor", evt.HonorTaken);
            
            Debug.Log($"🎭 Air ring resolved: {evt.TriggeredBy.Name} took {evt.HonorTaken} honor from {evt.Target.Name} (swing: {evt.HonorSwing})");
        }
        
        /// <summary>
        /// Handle air ring not resolved event UI
        /// </summary>
        /// <param name="gameEvent">Air ring not resolved event</param>
        private void HandleAirRingNotResolvedUI(GameEvent gameEvent)
        {
            var evt = gameEvent as AirRingNotResolvedEvent;
            if (evt?.TriggeredBy == null) return;
            
            // Show "not resolved" message/effect
            ShowRingNotResolvedEffect(evt.RingElement);
            
            Debug.Log($"🎭 {evt.RingElement} ring not resolved by {evt.TriggeredBy.Name} ({evt.Reason})");
        }
        
        /// <summary>
        /// Refresh UI for a player's honor display
        /// </summary>
        /// <param name="player">Player whose honor to refresh</param>
        private void RefreshPlayerHonorUI(Player player)
        {
            try
            {
                // Update honor counter display
                RefreshGameUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to refresh honor UI for {player?.name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Show card discard animation
        /// </summary>
        /// <param name="player">Player discarding cards</param>
        /// <param name="amount">Number of cards discarded</param>
        /// <param name="random">Whether discard was random</param>
        private void ShowCardDiscardAnimation(Player player, int amount, bool random)
        {
            string discardType = random ? "randomly" : "by choice";
            Debug.Log($"🎭 Showing discard animation: {player.name} discards {amount} card(s) {discardType}");
        }
        
        /// <summary>
        /// Show earth ring effect
        /// </summary>
        /// <param name="cardsDrawn">Cards drawn</param>
        /// <param name="cardsDiscarded">Cards discarded</param>
        private void ShowEarthRingEffect(int cardsDrawn, int cardsDiscarded)
        {
            Debug.Log($"🎭 Showing earth ring effect: draw {cardsDrawn}, discard {cardsDiscarded}");
        }
        
        /// <summary>
        /// Show air ring effect
        /// </summary>
        /// <param name="choice">Choice made</param>
        /// <param name="amount">Honor amount</param>
        private void ShowAirRingEffect(string choice, int amount)
        {
            Debug.Log($"🎭 Showing air ring effect: {choice} - {amount} honor");
        }
        
        /// <summary>
        /// Show ring not resolved effect
        /// </summary>
        /// <param name="ringElement">Ring element</param>
        private void ShowRingNotResolvedEffect(string ringElement)
        {
            Debug.Log($"🎭 Showing not resolved effect for {ringElement} ring");
        }
        
        /// <summary>
        /// Show honor gain animation
        /// </summary>
        /// <param name="player">Player gaining honor</param>
        /// <param name="amount">Amount gained</param>
        private void ShowHonorGainAnimation(Player player, int amount)
        {
            Debug.Log($"🎭 Showing honor gain animation: {player.name} gains {amount} honor");
        }
        
        /// <summary>
        /// Show honor transfer animation
        /// </summary>
        /// <param name="from">Player losing honor</param>
        /// <param name="to">Player gaining honor</param>
        /// <param name="amount">Amount transferred</param>
        private void ShowHonorTransferAnimation(Player from, Player to, int amount)
        {
            Debug.Log($"🎭 Showing honor transfer animation: {amount} honor from {from.name} to {to.name}");
        }
        
        #endregion
        
        #region Overrides
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Debug.Log("🎭 UI event handler initialized - will manage UI updates based on game events");
        }
        
        public override object GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo() as dynamic;
            
            return new
            {
                handlerId = baseInfo.handlerId,
                handlerName = baseInfo.handlerName,
                isEnabled = baseInfo.isEnabled,
                isDisposed = baseInfo.isDisposed,
                eventsProcessed = baseInfo.eventsProcessed,
                errorCount = baseInfo.errorCount,
                lastProcessedTime = baseInfo.lastProcessedTime,
                subscriptionCount = baseInfo.subscriptionCount,
                handledEventTypes = GetHandledEventTypes().Length,
                gameInstanceFound = _game != null,
                uiUpdateActionsCount = _uiUpdateActions.Count
            };
        }
        
        #endregion
    }
}