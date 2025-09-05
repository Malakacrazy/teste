using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace L5RGame.Events
{
    /// <summary>
    /// High-performance Event Store for event sourcing, persistence, and replay capabilities
    /// </summary>
    public class EventStore : IDisposable
    {
        #region Private Fields
        
        private readonly ConcurrentQueue<GameEvent> _eventQueue;
        private readonly ConcurrentDictionary<string, List<GameEvent>> _eventsByType;
        private readonly ConcurrentDictionary<string, List<GameEvent>> _eventsByPlayer;
        private readonly List<GameEventSnapshot> _snapshots;
        private readonly EventStoreConfiguration _config;
        private readonly object _snapshotLock = new object();
        private readonly object _persistenceLock = new object();
        
        private bool _isDisposed;
        private int _totalEventsProcessed;
        private DateTime _lastSnapshotTime;
        private DateTime _lastPersistenceTime;
        private long _memoryUsageBytes;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Total number of events stored
        /// </summary>
        public int TotalEvents => _eventQueue.Count;
        
        /// <summary>
        /// Number of snapshots created
        /// </summary>
        public int SnapshotCount => _snapshots.Count;
        
        /// <summary>
        /// Current memory usage in bytes
        /// </summary>
        public long MemoryUsageBytes => _memoryUsageBytes;
        
        /// <summary>
        /// Configuration settings
        /// </summary>
        public EventStoreConfiguration Configuration => _config;
        
        /// <summary>
        /// Statistics about event processing
        /// </summary>
        public EventStoreStatistics Statistics => GetStatistics();
        
        #endregion
        
        #region Constructor
        
        public EventStore(EventStoreConfiguration config = null)
        {
            _config = config ?? new EventStoreConfiguration();
            _eventQueue = new ConcurrentQueue<GameEvent>();
            _eventsByType = new ConcurrentDictionary<string, List<GameEvent>>();
            _eventsByPlayer = new ConcurrentDictionary<string, List<GameEvent>>();
            _snapshots = new List<GameEventSnapshot>();
            
            _lastSnapshotTime = DateTime.UtcNow;
            _lastPersistenceTime = DateTime.UtcNow;
            
            Debug.Log($"📚 EventStore initialized with config: {_config}");
        }
        
        #endregion
        
        #region Event Storage
        
        /// <summary>
        /// Store an event in the event store
        /// </summary>
        /// <param name="gameEvent">Event to store</param>
        public void StoreEvent(GameEvent gameEvent)
        {
            if (gameEvent == null || _isDisposed)
                return;
                
            try
            {
                // Add to main queue
                _eventQueue.Enqueue(gameEvent);
                
                // Index by type
                var eventType = gameEvent.GetType().Name;
                _eventsByType.AddOrUpdate(eventType, 
                    new List<GameEvent> { gameEvent },
                    (key, existing) => 
                    {
                        lock (existing)
                        {
                            existing.Add(gameEvent);
                            // Limit size to prevent memory issues
                            if (existing.Count > _config.MaxEventsPerIndex)
                            {
                                existing.RemoveRange(0, existing.Count - _config.MaxEventsPerIndex);
                            }
                            return existing;
                        }
                    });
                
                // Index by player
                if (gameEvent.TriggeredBy != null)
                {
                    var playerId = gameEvent.TriggeredBy.PlayerId;
                    _eventsByPlayer.AddOrUpdate(playerId,
                        new List<GameEvent> { gameEvent },
                        (key, existing) =>
                        {
                            lock (existing)
                            {
                                existing.Add(gameEvent);
                                if (existing.Count > _config.MaxEventsPerIndex)
                                {
                                    existing.RemoveRange(0, existing.Count - _config.MaxEventsPerIndex);
                                }
                                return existing;
                            }
                        });
                }
                
                _totalEventsProcessed++;
                UpdateMemoryUsage();
                
                // Check if we need to create a snapshot
                if (ShouldCreateSnapshot())
                {
                    _ = Task.Run(() => CreateSnapshot(gameEvent.Game));
                }
                
                // Check if we need to persist events
                if (ShouldPersistEvents())
                {
                    _ = Task.Run(() => PersistEvents());
                }
                
                // Cleanup old events if needed
                if (_totalEventsProcessed % _config.CleanupInterval == 0)
                {
                    _ = Task.Run(() => CleanupOldEvents());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to store event {gameEvent?.EventName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Store multiple events atomically
        /// </summary>
        /// <param name="events">Events to store</param>
        public void StoreEvents(IEnumerable<GameEvent> events)
        {
            if (events == null || _isDisposed)
                return;
                
            foreach (var gameEvent in events)
            {
                StoreEvent(gameEvent);
            }
        }
        
        #endregion
        
        #region Event Retrieval
        
        /// <summary>
        /// Get all events of a specific type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>Events of specified type</returns>
        public IEnumerable<T> GetEventsByType<T>() where T : GameEvent
        {
            var eventType = typeof(T).Name;
            if (_eventsByType.TryGetValue(eventType, out var events))
            {
                lock (events)
                {
                    return events.OfType<T>().ToList();
                }
            }
            return new List<T>();
        }
        
        /// <summary>
        /// Get all events triggered by a specific player
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <returns>Events triggered by player</returns>
        public IEnumerable<GameEvent> GetEventsByPlayer(string playerId)
        {
            if (_eventsByPlayer.TryGetValue(playerId, out var events))
            {
                lock (events)
                {
                    return events.ToList();
                }
            }
            return new List<GameEvent>();
        }
        
        /// <summary>
        /// Get events within a time range
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <returns>Events in time range</returns>
        public IEnumerable<GameEvent> GetEventsByTimeRange(DateTime startTime, DateTime endTime)
        {
            return _eventQueue.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList();
        }
        
        /// <summary>
        /// Get the last N events
        /// </summary>
        /// <param name="count">Number of events to retrieve</param>
        /// <returns>Recent events</returns>
        public IEnumerable<GameEvent> GetRecentEvents(int count)
        {
            return _eventQueue.TakeLast(count).ToList();
        }
        
        /// <summary>
        /// Get all events (use with caution for large stores)
        /// </summary>
        /// <returns>All stored events</returns>
        public IEnumerable<GameEvent> GetAllEvents()
        {
            return _eventQueue.ToList();
        }
        
        #endregion
        
        #region Snapshots
        
        /// <summary>
        /// Create a snapshot of the current game state
        /// </summary>
        /// <param name="game">Game instance</param>
        private async Task CreateSnapshot(Game game)
        {
            if (game == null || _isDisposed)
                return;
                
            try
            {
                await Task.Run(() =>
                {
                    lock (_snapshotLock)
                    {
                        var snapshot = new GameEventSnapshot
                        {
                            SnapshotId = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow,
                            EventCount = _totalEventsProcessed,
                            GameState = CaptureGameState(game),
                            PlayerStates = CapturePlayerStates(game),
                            Metadata = new Dictionary<string, object>
                            {
                                { "snapshot_reason", "periodic" },
                                { "memory_usage_bytes", _memoryUsageBytes },
                                { "total_events", _totalEventsProcessed }
                            }
                        };
                        
                        _snapshots.Add(snapshot);
                        _lastSnapshotTime = DateTime.UtcNow;
                        
                        // Limit snapshot count
                        if (_snapshots.Count > _config.MaxSnapshots)
                        {
                            _snapshots.RemoveRange(0, _snapshots.Count - _config.MaxSnapshots);
                        }
                        
                        Debug.Log($"📸 Created game snapshot {snapshot.SnapshotId} at event #{_totalEventsProcessed}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to create snapshot: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the most recent snapshot
        /// </summary>
        /// <returns>Latest snapshot or null</returns>
        public GameEventSnapshot GetLatestSnapshot()
        {
            lock (_snapshotLock)
            {
                return _snapshots.LastOrDefault();
            }
        }
        
        /// <summary>
        /// Get all snapshots
        /// </summary>
        /// <returns>All snapshots</returns>
        public IEnumerable<GameEventSnapshot> GetAllSnapshots()
        {
            lock (_snapshotLock)
            {
                return _snapshots.ToList();
            }
        }
        
        #endregion
        
        #region Event Replay
        
        /// <summary>
        /// Replay events from a specific point in time
        /// </summary>
        /// <param name="fromTime">Start time for replay</param>
        /// <param name="eventHandler">Handler for each replayed event</param>
        /// <returns>Number of events replayed</returns>
        public async Task<int> ReplayEventsFromTime(DateTime fromTime, Action<GameEvent> eventHandler)
        {
            if (eventHandler == null)
                return 0;
                
            var eventsToReplay = GetEventsByTimeRange(fromTime, DateTime.UtcNow).OrderBy(e => e.Timestamp);
            int replayedCount = 0;
            
            foreach (var gameEvent in eventsToReplay)
            {
                try
                {
                    eventHandler(gameEvent);
                    replayedCount++;
                    
                    // Small delay to prevent overwhelming the system
                    if (replayedCount % 100 == 0)
                    {
                        await Task.Delay(1);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Failed to replay event {gameEvent.EventId}: {ex.Message}");
                }
            }
            
            Debug.Log($"🔄 Replayed {replayedCount} events from {fromTime}");
            return replayedCount;
        }
        
        /// <summary>
        /// Replay events from a specific snapshot
        /// </summary>
        /// <param name="snapshotId">Snapshot ID to replay from</param>
        /// <param name="eventHandler">Handler for each replayed event</param>
        /// <returns>Number of events replayed</returns>
        public async Task<int> ReplayEventsFromSnapshot(string snapshotId, Action<GameEvent> eventHandler)
        {
            var snapshot = _snapshots.FirstOrDefault(s => s.SnapshotId == snapshotId);
            if (snapshot == null)
            {
                Debug.LogWarning($"⚠️ Snapshot {snapshotId} not found");
                return 0;
            }
            
            return await ReplayEventsFromTime(snapshot.Timestamp, eventHandler);
        }
        
        #endregion
        
        #region Persistence
        
        /// <summary>
        /// Persist events to disk
        /// </summary>
        private async Task PersistEvents()
        {
            if (!_config.EnablePersistence || _isDisposed)
                return;
                
            try
            {
                await Task.Run(() =>
                {
                    lock (_persistenceLock)
                    {
                        var eventsToSave = GetRecentEvents(_config.EventsPersistBatchSize).ToList();
                        if (!eventsToSave.Any())
                            return;
                            
                        var filePath = GetPersistenceFilePath();
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        
                        var eventData = eventsToSave.Select(e => new
                        {
                            EventId = e.EventId,
                            EventName = e.EventName,
                            Timestamp = e.Timestamp,
                            TriggeredBy = e.TriggeredBy?.PlayerId,
                            EventData = e.GetAllEventData()
                        });
                        
                        var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(filePath, json);
                        
                        _lastPersistenceTime = DateTime.UtcNow;
                        
                        Debug.Log($"💾 Persisted {eventsToSave.Count} events to {filePath}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to persist events: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load events from disk
        /// </summary>
        /// <returns>Number of events loaded</returns>
        public async Task<int> LoadPersistedEvents()
        {
            if (!_config.EnablePersistence)
                return 0;
                
            try
            {
                var filePath = GetPersistenceFilePath();
                if (!File.Exists(filePath))
                    return 0;
                    
                var json = await File.ReadAllTextAsync(filePath);
                var eventData = JsonSerializer.Deserialize<dynamic[]>(json);
                
                Debug.Log($"📂 Loaded {eventData?.Length ?? 0} events from {filePath}");
                return eventData?.Length ?? 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load persisted events: {ex.Message}");
                return 0;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool ShouldCreateSnapshot()
        {
            var timeSinceLastSnapshot = DateTime.UtcNow - _lastSnapshotTime;
            return timeSinceLastSnapshot.TotalSeconds >= _config.SnapshotIntervalSeconds ||
                   _totalEventsProcessed % _config.SnapshotIntervalEvents == 0;
        }
        
        private bool ShouldPersistEvents()
        {
            if (!_config.EnablePersistence)
                return false;
                
            var timeSinceLastPersistence = DateTime.UtcNow - _lastPersistenceTime;
            return timeSinceLastPersistence.TotalSeconds >= _config.PersistenceIntervalSeconds;
        }
        
        private void UpdateMemoryUsage()
        {
            // Rough estimate of memory usage
            _memoryUsageBytes = _eventQueue.Count * 1000; // ~1KB per event estimate
        }
        
        private async Task CleanupOldEvents()
        {
            if (_totalEventsProcessed < _config.MaxEventsBeforeCleanup)
                return;
                
            try
            {
                await Task.Run(() =>
                {
                    // Remove old events to prevent memory issues
                    var eventsToRemove = _eventQueue.Count - _config.MaxStoredEvents;
                    if (eventsToRemove > 0)
                    {
                        for (int i = 0; i < eventsToRemove; i++)
                        {
                            _eventQueue.TryDequeue(out _);
                        }
                        
                        Debug.Log($"🧹 Cleaned up {eventsToRemove} old events");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to cleanup old events: {ex.Message}");
            }
        }
        
        private Dictionary<string, object> CaptureGameState(Game game)
        {
            return new Dictionary<string, object>
            {
                { "turn_number", game.TurnNumber },
                { "current_phase", game.CurrentPhase ?? "unknown" },
                { "game_id", game.GameId ?? "unknown" }
            };
        }
        
        private Dictionary<string, object> CapturePlayerStates(Game game)
        {
            var playerStates = new Dictionary<string, object>();
            
            if (game.Players != null)
            {
                foreach (var player in game.Players)
                {
                    playerStates[player.PlayerId] = new
                    {
                        fate = player.Fate,
                        honor = player.Honor,
                        hand_size = player.Hand?.Count ?? 0,
                        deck_size = player.Deck?.Count ?? 0
                    };
                }
            }
            
            return playerStates;
        }
        
        private string GetPersistenceFilePath()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(_config.PersistencePath, $"events_{timestamp}.json");
        }
        
        private EventStoreStatistics GetStatistics()
        {
            return new EventStoreStatistics
            {
                TotalEventsProcessed = _totalEventsProcessed,
                CurrentEventCount = _eventQueue.Count,
                SnapshotCount = _snapshots.Count,
                MemoryUsageBytes = _memoryUsageBytes,
                EventTypeCount = _eventsByType.Count,
                PlayerCount = _eventsByPlayer.Count,
                LastSnapshotTime = _lastSnapshotTime,
                LastPersistenceTime = _lastPersistenceTime
            };
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            
            // Final persistence
            if (_config.EnablePersistence)
            {
                _ = Task.Run(() => PersistEvents());
            }
            
            Debug.Log($"📚 EventStore disposed. Processed {_totalEventsProcessed} total events.");
        }
        
        #endregion
    }
}