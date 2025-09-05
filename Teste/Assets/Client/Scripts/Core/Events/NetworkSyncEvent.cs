using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event for network synchronization in multiplayer games
    /// </summary>
    [Serializable]
    public class NetworkSyncEvent : GameEvent
    {
        /// <summary>
        /// Type of network synchronization
        /// </summary>
        public NetworkSyncType SyncType { get; private set; }
        
        /// <summary>
        /// Data to synchronize
        /// </summary>
        public Dictionary<string, object> SyncData { get; private set; }
        
        /// <summary>
        /// Priority level (higher = more urgent)
        /// </summary>
        public int Priority { get; private set; }
        
        /// <summary>
        /// Whether this requires confirmation from other players
        /// </summary>
        public bool RequiresConfirmation { get; private set; }
        
        /// <summary>
        /// Target players for this sync (null = all players)
        /// </summary>
        public List<string> TargetPlayers { get; private set; }
        
        public NetworkSyncEvent(Game game, Player triggeredBy, NetworkSyncType syncType, 
            Dictionary<string, object> syncData, int priority = 0, bool requiresConfirmation = false,
            List<string> targetPlayers = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            SyncType = syncType;
            SyncData = syncData ?? new Dictionary<string, object>();
            Priority = priority;
            RequiresConfirmation = requiresConfirmation;
            TargetPlayers = targetPlayers;
            
            AddEventData("sync_type", syncType.ToString());
            AddEventData("priority", priority);
            AddEventData("requires_confirmation", requiresConfirmation);
            AddEventData("target_players_count", targetPlayers?.Count ?? 0);
            AddEventData("sync_data_count", SyncData.Count);
        }
        
        public string GetDescription()
        {
            var targets = TargetPlayers == null ? "all players" : $"{TargetPlayers.Count} specific players";
            return $"Network sync ({SyncType}) from {TriggeredBy.Name} to {targets} [P{Priority}]";
        }
    }
    
    public enum NetworkSyncType
    {
        GameState,
        PlayerAction,
        CardPlay,
        PhaseChange,
        TurnChange,
        AbilityActivation,
        Heartbeat,
        Error,
        Disconnect
    }
}