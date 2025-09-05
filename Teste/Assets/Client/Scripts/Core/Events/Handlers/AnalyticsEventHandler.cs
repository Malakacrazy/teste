using System;
using System.Collections.Generic;
using L5RGame.Events;

namespace L5RGame.EventSystem.Handlers
{
    /// <summary>
    /// Event handler that logs game events to the analytics system.
    /// Replaces direct Game.Analytics.LogEvent() calls with event-driven analytics.
    /// </summary>
    public class AnalyticsEventHandler : BaseMultiEventHandler
    {
        #region Properties
        
        public override string HandlerName => "Analytics Event Handler";
        
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
                typeof(AbilityExecutedEvent),
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
            if (gameEvent == null)
                return;
            
            try
            {
                switch (gameEvent)
                {
                    case FateRemovedEvent fateRemoved:
                        HandleFateRemovedEvent(fateRemoved);
                        break;
                        
                    case CharacterHonoredEvent honored:
                        HandleCharacterHonoredEvent(honored);
                        break;
                        
                    case CharacterDishonoredEvent dishonored:
                        HandleCharacterDishonoredEvent(dishonored);
                        break;
                        
                    case CharacterStatusChangedEvent statusChanged:
                        HandleCharacterStatusChangedEvent(statusChanged);
                        break;
                        
                    case CardDrawnEvent cardDrawn:
                        HandleCardDrawnEvent(cardDrawn);
                        break;
                        
                    case CardPlayedEvent cardPlayed:
                        HandleCardPlayedEvent(cardPlayed);
                        break;
                        
                    case RingResolvedEvent ringResolved:
                        HandleRingResolvedEvent(ringResolved);
                        break;
                        
                    case AbilityExecutedEvent abilityExecuted:
                        HandleAbilityExecutedEvent(abilityExecuted);
                        break;
                        
                    case ConflictStartedEvent conflictStarted:
                        HandleConflictStartedEvent(conflictStarted);
                        break;
                        
                    case CharacterLeavesPlayEvent characterLeaves:
                        HandleCharacterLeavesPlayEvent(characterLeaves);
                        break;
                        
                    case CardMovedEvent cardMoved:
                        HandleCardMovedEvent(cardMoved);
                        break;
                        
                    case CharacterBowedEvent characterBowed:
                        HandleCharacterBowedEvent(characterBowed);
                        break;
                        
                    case CharacterReadiedEvent characterReadied:
                        HandleCharacterReadiedEvent(characterReadied);
                        break;
                        
                    case EarthRingDrawDiscardEvent earthDrawDiscard:
                        HandleEarthRingDrawDiscardEvent(earthDrawDiscard);
                        break;
                        
                    case EarthRingNotResolvedEvent earthNotResolved:
                        HandleEarthRingNotResolvedEvent(earthNotResolved);
                        break;
                        
                    case AirRingGainHonorEvent airGainHonor:
                        HandleAirRingGainHonorEvent(airGainHonor);
                        break;
                        
                    case AirRingTakeHonorEvent airTakeHonor:
                        HandleAirRingTakeHonorEvent(airTakeHonor);
                        break;
                        
                    case AirRingNotResolvedEvent airNotResolved:
                        HandleAirRingNotResolvedEvent(airNotResolved);
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError(ex, gameEvent.EventName);
            }
        }
        
        #endregion
        
        #region Specific Event Handlers
        
        private void HandleFateRemovedEvent(FateRemovedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner.PlayerId },
                { "amount_removed", evt.AmountRemoved },
                { "fate_before", evt.FateRemaining + evt.AmountRemoved },
                { "fate_after", evt.FateRemaining },
                { "will_leave_play", evt.WillLeavePlay },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("fate_removed", analyticsData);
        }
        
        private void HandleCharacterHonoredEvent(CharacterHonoredEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner.PlayerId },
                { "was_already_honored", evt.WasAlreadyHonored },
                { "power_bonus", evt.GetData<int>("power_bonus", 0) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("character_honored", analyticsData);
        }
        
        private void HandleCharacterDishonoredEvent(CharacterDishonoredEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner.PlayerId },
                { "was_already_dishonored", evt.WasAlreadyDishonored },
                { "power_penalty", evt.GetData<int>("power_penalty", 0) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("character_dishonored", analyticsData);
        }
        
        private void HandleCharacterStatusChangedEvent(CharacterStatusChangedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner.PlayerId },
                { "was_bowed", evt.WasBowed },
                { "is_bowed", evt.IsBowed },
                { "status_change", evt.StatusChange },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("character_status_changed", analyticsData);
        }
        
        private void HandleCardDrawnEvent(CardDrawnEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.DrawingPlayer.PlayerId },
                { "drawn_from", evt.DrawnFrom },
                { "hand_size_after", evt.GetData<int>("hand_size_after", 0) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            if (evt.Card != null)
            {
                analyticsData.Add("card_id", evt.Card.CardId);
                analyticsData.Add("card_name", evt.Card.Name);
                analyticsData.Add("card_type", evt.Card.CardType.ToString());
            }
            
            LogAnalyticsEvent("card_drawn", analyticsData);
        }
        
        private void HandleCardPlayedEvent(CardPlayedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.PlayingPlayer.PlayerId },
                { "card_id", evt.Card.CardId },
                { "card_name", evt.Card.Name },
                { "card_type", evt.Card.CardType.ToString() },
                { "cost_paid", evt.CostPaid },
                { "played_to", evt.PlayedTo },
                { "fate_remaining", evt.GetData<int>("fate_remaining", 0) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("card_played", analyticsData);
        }
        
        private void HandleRingResolvedEvent(RingResolvedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "ring_element", evt.RingElement },
                { "effect_chosen", evt.EffectChosen },
                { "effect_target", evt.EffectTarget?.ToString() },
                { "ring_claimed", evt.GetData<bool>("ring_claimed", false) },
                { "ring_contested", evt.GetData<bool>("ring_contested", false) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent($"{evt.RingElement}_ring_resolved", analyticsData);
        }
        
        private void HandleAbilityExecutedEvent(AbilityExecutedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "ability_title", evt.GetData<string>("ability_title") },
                { "ability_type", evt.GetData<string>("ability_type") },
                { "successful", evt.Successful },
                { "failure_reason", evt.FailureReason },
                { "ability_cost", evt.GetData<object>("ability_cost") },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("ability_executed", analyticsData);
        }
        
        private void HandleConflictStartedEvent(ConflictStartedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "attacking_player", evt.AttackingPlayer.PlayerId },
                { "defending_player", evt.DefendingPlayer.PlayerId },
                { "conflict_type", evt.ConflictType },
                { "target_ring", evt.TargetRing?.element },
                { "conflict_id", evt.GetData<string>("conflict_id") },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("conflict_started", analyticsData);
        }
        
        private void HandleCharacterLeavesPlayEvent(CharacterLeavesPlayEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner.PlayerId },
                { "destination", evt.Destination.ToString() },
                { "reason", evt.Reason },
                { "fate_tokens", evt.GetData<int>("fate_tokens", 0) },
                { "was_honored", evt.GetData<bool>("was_honored", false) },
                { "was_dishonored", evt.GetData<bool>("was_dishonored", false) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("character_leaves_play", analyticsData);
        }
        
        private void HandleCardMovedEvent(CardMovedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "card_id", evt.Card.CardId },
                { "card_name", evt.Card.Name },
                { "from_location", evt.FromLocation.ToString() },
                { "to_location", evt.ToLocation.ToString() },
                { "from_controller", evt.FromController.PlayerId },
                { "to_controller", evt.ToController.PlayerId },
                { "zone_change", evt.GetData<bool>("zone_change", false) },
                { "controller_change", evt.GetData<bool>("controller_change", false) },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("card_moved", analyticsData);
        }
        
        private void HandleCharacterBowedEvent(CharacterBowedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner?.PlayerId },
                { "was_already_bowed", evt.WasAlreadyBowed },
                { "bow_status_changed", !evt.WasAlreadyBowed },
                { "reason", evt.Reason },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("character_bowed", analyticsData);
        }
        
        private void HandleCharacterReadiedEvent(CharacterReadiedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "character_id", evt.Character.CardId },
                { "character_name", evt.Character.Name },
                { "character_owner", evt.Character.Owner?.PlayerId },
                { "was_already_ready", evt.WasAlreadyReady },
                { "ready_status_changed", !evt.WasAlreadyReady },
                { "reason", evt.Reason },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("character_readied", analyticsData);
        }
        
        private void HandleEarthRingDrawDiscardEvent(EarthRingDrawDiscardEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "ring_element", "earth" },
                { "cards_drawn", evt.CardsDrawn },
                { "cards_discarded", evt.CardsDiscarded },
                { "opponent_discarded", evt.OpponentDiscarded },
                { "discard_was_random", evt.DiscardWasRandom },
                { "card_advantage", evt.CardAdvantage },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            if (evt.TriggeredBy?.Opponent != null)
            {
                analyticsData.Add("opponent_id", evt.TriggeredBy.Opponent.PlayerId);
                analyticsData.Add("player_hand_size_after", evt.TriggeredBy.Hand.Count);
                analyticsData.Add("opponent_hand_size_after", evt.TriggeredBy.Opponent.Hand.Count);
            }
            
            LogAnalyticsEvent("earth_ring_draw_discard", analyticsData);
        }
        
        private void HandleEarthRingNotResolvedEvent(EarthRingNotResolvedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "ring_element", "earth" },
                { "reason", evt.Reason },
                { "resolution_status", "not_resolved" },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("earth_ring_not_resolved", analyticsData);
        }
        
        private void HandleAirRingGainHonorEvent(AirRingGainHonorEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "ring_element", "air" },
                { "choice", "gain_honor" },
                { "honor_gained", evt.HonorGained },
                { "total_honor_after", evt.TotalHonorAfter },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("air_ring_gain_honor", analyticsData);
        }
        
        private void HandleAirRingTakeHonorEvent(AirRingTakeHonorEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "opponent_id", evt.Target?.PlayerId },
                { "ring_element", "air" },
                { "choice", "take_honor" },
                { "honor_taken", evt.HonorTaken },
                { "player_honor_before", evt.PlayerHonorBefore },
                { "player_honor_after", evt.PlayerHonorAfter },
                { "target_honor_before", evt.TargetHonorBefore },
                { "target_honor_after", evt.TargetHonorAfter },
                { "honor_swing", evt.HonorSwing },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("air_ring_take_honor", analyticsData);
        }
        
        private void HandleAirRingNotResolvedEvent(AirRingNotResolvedEvent evt)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", evt.TriggeredBy?.PlayerId },
                { "ring_element", evt.RingElement },
                { "reason", evt.Reason },
                { "resolution_status", "not_resolved" },
                { "source", evt.Source?.ToString() },
                { "timestamp", evt.Timestamp }
            };
            
            LogAnalyticsEvent("air_ring_not_resolved", analyticsData);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Log an analytics event using the game's analytics system
        /// </summary>
        /// <param name="eventName">Name of the analytics event</param>
        /// <param name="eventData">Event data to log</param>
        private void LogAnalyticsEvent(string eventName, Dictionary<string, object> eventData)
        {
            try
            {
                // Use the static Game analytics system
                GameAnalytics.LogEvent(eventName, eventData);
                
                // Also log for debugging if needed
                if (UnityEngine.Application.isEditor)
                {
                    UnityEngine.Debug.Log($"📊 Analytics: {eventName} - {string.Join(", ", eventData.Keys)}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to log analytics event '{eventName}': {ex.Message}");
            }
        }
        
        #endregion
        
        #region Overrides
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            UnityEngine.Debug.Log("📊 Analytics event handler initialized - will track all game events");
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
                analyticsSystem = "GameAnalytics Static",
                debugLogging = UnityEngine.Application.isEditor
            };
        }
        
        #endregion
    }
}