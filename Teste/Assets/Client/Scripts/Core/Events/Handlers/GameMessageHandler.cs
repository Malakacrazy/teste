using System;
using System.Collections.Generic;
using L5RGame.Events;
using UnityEngine;

namespace L5RGame.EventSystem.Handlers
{
    /// <summary>
    /// Event handler that generates game messages based on events.
    /// Replaces direct game.AddMessage() calls with event-driven messaging.
    /// </summary>
    public class GameMessageHandler : BaseMultiEventHandler
    {
        #region Private Fields
        
        private Game _game;
        
        #endregion
        
        #region Properties
        
        public override string HandlerName => "Game Message Handler";
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(IEventBus eventBus)
        {
            // Get the game instance
            _game = Game.Instance;
            if (_game == null)
            {
                throw new InvalidOperationException("Game instance not found - cannot initialize GameMessageHandler");
            }
            
            base.Initialize(eventBus);
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
                typeof(CardMovedEvent)
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
                switch (gameEvent)
                {
                    case FateRemovedEvent fateRemoved:
                        HandleFateRemovedMessage(fateRemoved);
                        break;
                        
                    case CharacterHonoredEvent honored:
                        HandleCharacterHonoredMessage(honored);
                        break;
                        
                    case CharacterDishonoredEvent dishonored:
                        HandleCharacterDishonoredMessage(dishonored);
                        break;
                        
                    case CharacterStatusChangedEvent statusChanged:
                        HandleCharacterStatusChangedMessage(statusChanged);
                        break;
                        
                    case CardDrawnEvent cardDrawn:
                        HandleCardDrawnMessage(cardDrawn);
                        break;
                        
                    case CardPlayedEvent cardPlayed:
                        HandleCardPlayedMessage(cardPlayed);
                        break;
                        
                    case RingResolvedEvent ringResolved:
                        HandleRingResolvedMessage(ringResolved);
                        break;
                        
                    case ConflictStartedEvent conflictStarted:
                        HandleConflictStartedMessage(conflictStarted);
                        break;
                        
                    case CharacterLeavesPlayEvent characterLeaves:
                        HandleCharacterLeavesPlayMessage(characterLeaves);
                        break;
                        
                    case CardMovedEvent cardMoved:
                        HandleCardMovedMessage(cardMoved);
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError(ex, gameEvent.EventName);
            }
        }
        
        #endregion
        
        #region Specific Message Handlers
        
        private void HandleFateRemovedMessage(FateRemovedEvent evt)
        {
            string message;
            
            if (evt.WillLeavePlay)
            {
                message = $"{evt.TriggeredBy?.name ?? "System"} removes {evt.AmountRemoved} fate from {evt.Character.Name}, causing it to leave play";
            }
            else
            {
                message = $"{evt.TriggeredBy?.name ?? "System"} removes {evt.AmountRemoved} fate from {evt.Character.Name} " +
                         $"({evt.FateRemaining + evt.AmountRemoved} → {evt.FateRemaining} fate remaining)";
            }
            
            AddGameMessage(message);
        }
        
        private void HandleCharacterHonoredMessage(CharacterHonoredEvent evt)
        {
            if (evt.WasAlreadyHonored)
            {
                AddGameMessage($"{evt.Character.Name} is already honored");
            }
            else
            {
                AddGameMessage($"{evt.TriggeredBy?.name ?? "System"} honors {evt.Character.Name}");
            }
        }
        
        private void HandleCharacterDishonoredMessage(CharacterDishonoredEvent evt)
        {
            if (evt.WasAlreadyDishonored)
            {
                AddGameMessage($"{evt.Character.Name} is already dishonored");
            }
            else
            {
                AddGameMessage($"{evt.TriggeredBy?.name ?? "System"} dishonors {evt.Character.Name}");
            }
        }
        
        private void HandleCharacterStatusChangedMessage(CharacterStatusChangedEvent evt)
        {
            string action = evt.IsBowed ? "bows" : "readies";
            AddGameMessage($"{evt.TriggeredBy?.name ?? "System"} {action} {evt.Character.Name}");
        }
        
        private void HandleCardDrawnMessage(CardDrawnEvent evt)
        {
            if (evt.Card != null)
            {
                // Only show specific card drawn in debug/spectator mode
                if (Application.isEditor || _game.IsSpectator(evt.DrawingPlayer))
                {
                    AddGameMessage($"{evt.DrawingPlayer.name} draws {evt.Card.Name} from {evt.DrawnFrom}");
                }
                else
                {
                    AddGameMessage($"{evt.DrawingPlayer.name} draws a card from {evt.DrawnFrom}");
                }
            }
            else
            {
                AddGameMessage($"{evt.DrawingPlayer.name} draws a card from {evt.DrawnFrom}");
            }
        }
        
        private void HandleCardPlayedMessage(CardPlayedEvent evt)
        {
            string costInfo = evt.CostPaid > 0 ? $" (paying {evt.CostPaid} fate)" : "";
            AddGameMessage($"{evt.PlayingPlayer.name} plays {evt.Card.Name}{costInfo}");
        }
        
        private void HandleRingResolvedMessage(RingResolvedEvent evt)
        {
            string targetInfo = evt.EffectTarget != null ? $" on {evt.EffectTarget}" : "";
            
            switch (evt.EffectChosen?.ToLower())
            {
                case "honor":
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring, honoring{targetInfo}");
                    break;
                    
                case "dishonor":
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring, dishonoring{targetInfo}");
                    break;
                    
                case "fate_removed":
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring, removing fate from{targetInfo}");
                    break;
                    
                case "ready":
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring, readying{targetInfo}");
                    break;
                    
                case "bow":
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring, bowing{targetInfo}");
                    break;
                    
                case "draw_and_discard":
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring, drawing a card and forcing opponent to discard");
                    break;
                    
                case "not_resolved":
                    AddGameMessage($"{evt.TriggeredBy?.name} chooses not to resolve the {evt.RingElement} ring");
                    break;
                    
                default:
                    AddGameMessage($"{evt.TriggeredBy?.name} resolves the {evt.RingElement} ring");
                    break;
            }
        }
        
        private void HandleConflictStartedMessage(ConflictStartedEvent evt)
        {
            string ringInfo = evt.TargetRing != null ? $" at the {evt.TargetRing.element} ring" : "";
            AddGameMessage($"{evt.AttackingPlayer.name} declares a {evt.ConflictType} conflict against {evt.DefendingPlayer.name}{ringInfo}");
        }
        
        private void HandleCharacterLeavesPlayMessage(CharacterLeavesPlayEvent evt)
        {
            string reasonText = !string.IsNullOrEmpty(evt.Reason) ? $" ({evt.Reason})" : "";
            string destinationText = evt.Destination != "DiscardPile" ? $" to {evt.Destination}" : "";
            
            AddGameMessage($"{evt.Character.Name} leaves play{destinationText}{reasonText}");
        }
        
        private void HandleCardMovedMessage(CardMovedEvent evt)
        {
            // Only log important card moves to avoid spam
            if (ShouldLogCardMove(evt))
            {
                var fromText = GetLocationDisplayName(evt.FromLocation);
                var toText = GetLocationDisplayName(evt.ToLocation);
                
                if (evt.FromController != evt.ToController)
                {
                    AddGameMessage($"{evt.Card.Name} moves from {evt.FromController.name}'s {fromText} to {evt.ToController.name}'s {toText}");
                }
                else
                {
                    AddGameMessage($"{evt.Card.Name} moves from {fromText} to {toText}");
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Add a message to the game log
        /// </summary>
        /// <param name="message">Message to add</param>
        private void AddGameMessage(string message)
        {
            try
            {
                _game?.AddMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to add game message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determine if a card move should be logged
        /// </summary>
        /// <param name="evt">Card moved event</param>
        /// <returns>True if should log</returns>
        private bool ShouldLogCardMove(CardMovedEvent evt)
        {
            // Log moves to/from play area
            if (evt.FromLocation == CardLocation.PlayArea || evt.ToLocation == CardLocation.PlayArea)
                return true;
            
            // Log controller changes
            if (evt.FromController != evt.ToController)
                return true;
            
            // Log moves to discard pile (death, discard effects)
            if (evt.ToLocation == CardLocation.DiscardPile)
                return true;
            
            // Don't log routine moves (hand to deck shuffling, etc.)
            return false;
        }
        
        /// <summary>
        /// Get display name for a card location
        /// </summary>
        /// <param name="location">Card location</param>
        /// <returns>Display name</returns>
        private string GetLocationDisplayName(CardLocation location)
        {
            return location switch
            {
                CardLocation.PlayArea => "play area",
                CardLocation.Hand => "hand",
                CardLocation.Deck => "deck",
                CardLocation.DiscardPile => "discard pile",
                CardLocation.RemovedFromGame => "removed from game",
                _ => location.ToString().ToLower()
            };
        }
        
        #endregion
        
        #region Overrides
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Debug.Log("💬 Game message handler initialized - will generate messages for key game events");
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
                gameInstanceId = _game?.gameId ?? "Unknown"
            };
        }
        
        #endregion
    }
}