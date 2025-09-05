using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Represents a snapshot of game state at a specific point in time
    /// Used for event sourcing and game state recovery
    /// </summary>
    [Serializable]
    public class GameEventSnapshot
    {
        /// <summary>
        /// Unique identifier for this snapshot
        /// </summary>
        public string SnapshotId { get; set; }
        
        /// <summary>
        /// Timestamp when snapshot was created
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Number of events processed when snapshot was created
        /// </summary>
        public int EventCount { get; set; }
        
        /// <summary>
        /// Core game state information
        /// </summary>
        public Dictionary<string, object> GameState { get; set; }
        
        /// <summary>
        /// Player state information
        /// </summary>
        public Dictionary<string, object> PlayerStates { get; set; }
        
        /// <summary>
        /// Additional metadata about the snapshot
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
        
        /// <summary>
        /// Size of the snapshot in bytes (approximate)
        /// </summary>
        public long SizeBytes { get; set; }
        
        /// <summary>
        /// Version of the snapshot format
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// Initialize a new game event snapshot
        /// </summary>
        public GameEventSnapshot()
        {
            GameState = new Dictionary<string, object>();
            PlayerStates = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Get a value from game state
        /// </summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="key">Key to look up</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Retrieved value or default</returns>
        public T GetGameStateValue<T>(string key, T defaultValue = default)
        {
            if (GameState.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Get a player state value
        /// </summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="playerId">Player ID</param>
        /// <param name="key">Key to look up</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Retrieved value or default</returns>
        public T GetPlayerStateValue<T>(string playerId, string key, T defaultValue = default)
        {
            if (PlayerStates.TryGetValue(playerId, out var playerData) && 
                playerData is Dictionary<string, object> playerDict &&
                playerDict.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Get metadata value
        /// </summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="key">Key to look up</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Retrieved value or default</returns>
        public T GetMetadataValue<T>(string key, T defaultValue = default)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Set a game state value
        /// </summary>
        /// <param name="key">Key to set</param>
        /// <param name="value">Value to set</param>
        public void SetGameStateValue(string key, object value)
        {
            GameState[key] = value;
        }
        
        /// <summary>
        /// Set a player state value
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <param name="key">Key to set</param>
        /// <param name="value">Value to set</param>
        public void SetPlayerStateValue(string playerId, string key, object value)
        {
            if (!PlayerStates.ContainsKey(playerId))
            {
                PlayerStates[playerId] = new Dictionary<string, object>();
            }
            
            if (PlayerStates[playerId] is Dictionary<string, object> playerDict)
            {
                playerDict[key] = value;
            }
        }
        
        /// <summary>
        /// Set a metadata value
        /// </summary>
        /// <param name="key">Key to set</param>
        /// <param name="value">Value to set</param>
        public void SetMetadataValue(string key, object value)
        {
            Metadata[key] = value;
        }
        
        /// <summary>
        /// Calculate approximate size of this snapshot
        /// </summary>
        /// <returns>Size in bytes</returns>
        public long CalculateSize()
        {
            long size = 0;
            
            // Rough calculation based on dictionary contents
            size += GameState.Count * 100; // ~100 bytes per game state entry
            size += PlayerStates.Count * 500; // ~500 bytes per player state
            size += Metadata.Count * 50; // ~50 bytes per metadata entry
            size += SnapshotId?.Length * 2 ?? 0; // String length * 2 for UTF-16
            
            SizeBytes = size;
            return size;
        }
        
        /// <summary>
        /// Get snapshot summary information
        /// </summary>
        /// <returns>Summary string</returns>
        public string GetSummary()
        {
            var gameStateCount = GameState?.Count ?? 0;
            var playerCount = PlayerStates?.Count ?? 0;
            var metadataCount = Metadata?.Count ?? 0;
            
            return $"Snapshot {SnapshotId?.Substring(0, 8) ?? "unknown"} - " +
                   $"Events: {EventCount}, Players: {playerCount}, " +
                   $"Game State: {gameStateCount} entries, Metadata: {metadataCount} entries, " +
                   $"Size: {SizeBytes} bytes, Created: {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
        
        /// <summary>
        /// Validate snapshot integrity
        /// </summary>
        /// <returns>True if snapshot is valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SnapshotId) &&
                   Timestamp != default &&
                   EventCount >= 0 &&
                   GameState != null &&
                   PlayerStates != null &&
                   Metadata != null;
        }
        
        /// <summary>
        /// Create a copy of this snapshot
        /// </summary>
        /// <returns>Cloned snapshot</returns>
        public GameEventSnapshot Clone()
        {
            return new GameEventSnapshot
            {
                SnapshotId = SnapshotId,
                Timestamp = Timestamp,
                EventCount = EventCount,
                GameState = new Dictionary<string, object>(GameState),
                PlayerStates = new Dictionary<string, object>(PlayerStates),
                Metadata = new Dictionary<string, object>(Metadata),
                SizeBytes = SizeBytes,
                Version = Version
            };
        }
        
        /// <summary>
        /// Convert snapshot to string representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetSummary();
        }
    }
}