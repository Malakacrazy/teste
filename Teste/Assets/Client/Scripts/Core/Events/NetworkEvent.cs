using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published for network synchronization and multiplayer coordination
    /// </summary>
    [Serializable]
    public class NetworkEvent : GameEvent
    {
        /// <summary>
        /// The type of network event
        /// </summary>
        public string NetworkEventType { get; private set; }
        
        /// <summary>
        /// The network action
        /// </summary>
        public string NetworkAction { get; private set; }
        
        /// <summary>
        /// Data to synchronize across network
        /// </summary>
        public Dictionary<string, object> SyncData { get; private set; }
        
        /// <summary>
        /// Target player(s) for the network event (null for all players)
        /// </summary>
        public List<string> TargetPlayerIds { get; private set; }
        
        /// <summary>
        /// Whether this event should be broadcast to spectators
        /// </summary>
        public bool BroadcastToSpectators { get; private set; }
        
        /// <summary>
        /// Priority level for network synchronization
        /// </summary>
        public int Priority { get; private set; }
        
        /// <summary>
        /// Whether this is a reliable network event (guaranteed delivery)
        /// </summary>
        public bool IsReliable { get; private set; }
        
        /// <summary>
        /// Sequence number for ordering
        /// </summary>
        public long SequenceNumber { get; private set; }
        
        /// <summary>
        /// Initialize network event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the network event</param>
        /// <param name="networkEventType">Type of network event</param>
        /// <param name="networkAction">Network action</param>
        /// <param name="syncData">Data to synchronize</param>
        /// <param name="targetPlayerIds">Target players (null for all)</param>
        /// <param name="broadcastToSpectators">Whether to broadcast to spectators</param>
        /// <param name="priority">Priority level</param>
        /// <param name="isReliable">Whether guaranteed delivery</param>
        /// <param name="source">Source of the network event</param>
        public NetworkEvent(Game game, Player triggeredBy, string networkEventType, string networkAction,
            Dictionary<string, object> syncData = null, List<string> targetPlayerIds = null,
            bool broadcastToSpectators = true, int priority = 0, bool isReliable = true, object source = null) 
            : base(game, triggeredBy, source)
        {
            NetworkEventType = networkEventType;
            NetworkAction = networkAction;
            SyncData = syncData ?? new Dictionary<string, object>();
            TargetPlayerIds = targetPlayerIds;
            BroadcastToSpectators = broadcastToSpectators;
            Priority = priority;
            IsReliable = isReliable;
            SequenceNumber = DateTime.UtcNow.Ticks;
            
            // Add specific event data
            AddEventData("network_event_type", NetworkEventType);
            AddEventData("network_action", NetworkAction);
            AddEventData("target_count", TargetPlayerIds?.Count ?? 0);
            AddEventData("broadcast_to_spectators", BroadcastToSpectators);
            AddEventData("priority", Priority);
            AddEventData("is_reliable", IsReliable);
            AddEventData("sequence_number", SequenceNumber);
            AddEventData("player_id", triggeredBy?.PlayerId);
            
            // Add sync data
            foreach (var kvp in SyncData)
            {
                AddEventData($"sync_{kvp.Key}", kvp.Value);
            }
            
            // Add target player information
            if (TargetPlayerIds != null)
            {
                for (int i = 0; i < TargetPlayerIds.Count; i++)
                {
                    AddEventData($"target_player_{i}", TargetPlayerIds[i]);
                }
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            var targetInfo = TargetPlayerIds != null ? $" to {TargetPlayerIds.Count} player(s)" : " to all players";
            var spectatorInfo = BroadcastToSpectators ? " and spectators" : "";
            return $"Network {NetworkEventType} '{NetworkAction}'{targetInfo}{spectatorInfo} (priority {Priority})";
        }
        
        /// <summary>
        /// Static factory methods for common network events
        /// </summary>
        public static NetworkEvent GameStateSync(Game game, Player triggeredBy, Dictionary<string, object> gameState, object source = null)
        {
            return new NetworkEvent(game, triggeredBy, "game_state", "sync", gameState, null, true, 5, true, source);
        }
        
        public static NetworkEvent PlayerAction(Game game, Player player, string actionType, Dictionary<string, object> actionData, object source = null)
        {
            var syncData = new Dictionary<string, object>(actionData)
            {
                { "action_type", actionType },
                { "acting_player", player.PlayerId }
            };
            
            return new NetworkEvent(game, player, "player_action", actionType, syncData, null, true, 3, true, source);
        }
        
        public static NetworkEvent CardMovement(Game game, Player player, BaseCard card, string fromLocation, string toLocation, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "card_id", card.CardId },
                { "card_name", card.Name },
                { "from_location", fromLocation },
                { "to_location", toLocation },
                { "card_owner", card.Owner?.PlayerId }
            };
            
            return new NetworkEvent(game, player, "card_movement", "move_card", syncData, null, true, 2, true, source);
        }
        
        public static NetworkEvent PhaseTransition(Game game, string fromPhase, string toPhase, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "from_phase", fromPhase },
                { "to_phase", toPhase },
                { "round_number", game.roundNumber }
            };
            
            return new NetworkEvent(game, null, "phase_transition", "change_phase", syncData, null, true, 5, true, source);
        }
        
        public static NetworkEvent ConflictUpdate(Game game, Conflict conflict, string updateType, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "conflict_id", conflict.uuid },
                { "conflict_type", conflict.conflictType },
                { "attacking_player", conflict.attackingPlayer?.PlayerId },
                { "defending_player", conflict.defendingPlayer?.PlayerId },
                { "update_type", updateType }
            };
            
            return new NetworkEvent(game, conflict.attackingPlayer, "conflict", updateType, syncData, null, true, 4, true, source);
        }
        
        public static NetworkEvent ChatMessage(Game game, Player player, string message, bool isSpectator = false, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "message", message },
                { "sender", player.PlayerId },
                { "is_spectator", isSpectator }
            };
            
            return new NetworkEvent(game, player, "chat", "message", syncData, null, !isSpectator, 1, false, source);
        }
        
        public static NetworkEvent PlayerDisconnected(Game game, Player player, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "disconnected_player", player.PlayerId },
                { "disconnect_time", DateTime.UtcNow }
            };
            
            return new NetworkEvent(game, player, "connection", "player_disconnected", syncData, null, true, 5, true, source);
        }
        
        public static NetworkEvent PlayerReconnected(Game game, Player player, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "reconnected_player", player.PlayerId },
                { "reconnect_time", DateTime.UtcNow }
            };
            
            return new NetworkEvent(game, player, "connection", "player_reconnected", syncData, null, true, 5, true, source);
        }
        
        public static NetworkEvent PrivateMessage(Game game, Player sender, List<string> targetPlayerIds, string message, object source = null)
        {
            var syncData = new Dictionary<string, object>
            {
                { "message", message },
                { "sender", sender.PlayerId }
            };
            
            return new NetworkEvent(game, sender, "private_chat", "message", syncData, targetPlayerIds, false, 1, false, source);
        }
    }
}