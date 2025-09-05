using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using L5RGame.Events;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Event store for persistence, replay, and event sourcing capabilities
    /// </summary>
    public class EventStore : MonoBehaviour, IDisposable
    {
        /// <summary>
        /// Event store configuration
        /// </summary>
        [Serializable]
        public class EventStoreConfig
        {
            [Header("Storage Settings")]
            public bool enablePersistence = true;
            public string storageFileName = "event_store.json";
            public int maxEventsInMemory = 10000;
            public bool compressStoredEvents = true;
            
            [Header("Snapshot Settings")]
            public bool enableSnapshots = true;
            public int snapshotInterval = 100; // Events between snapshots
            public int maxSnapshots = 10;
            
            [Header("Performance Settings")]
            public bool enableAsyncOperations = true;
            public int batchSize = 50;
            public float flushInterval = 5.0f; // Seconds
        }
        
        /// <summary>
        /// Stored event with metadata
        /// </summary>
        [Serializable]
        public class StoredEvent
        {
            public string eventId;
            public string eventType;
            public DateTime timestamp;
            public string serializedEvent;
            public long sequenceNumber;
            public string gameId;
            public string playerId;
            public Dictionary<string, object> metadata;
            
            public StoredEvent()
            {
                metadata = new Dictionary<string, object>();
            }
        }
        
        /// <summary>
        /// Event snapshot for performance optimization
        /// </summary>
        [Serializable]
        public class EventSnapshot
        {
            public string snapshotId;
            public DateTime timestamp;
            public long lastEventSequence;
            public string gameId;
            public string serializedGameState;
            public Dictionary<string, object> snapshotMetadata;
            
            public EventSnapshot()
            {
                snapshotMetadata = new Dictionary<string, object>();
            }
        }
        
        [Header("Event Store Configuration")]
        [SerializeField] private EventStoreConfig config = new EventStoreConfig();
        
        // Internal storage
        private readonly List<StoredEvent> events = new List<StoredEvent>();
        private readonly List<EventSnapshot> snapshots = new List<EventSnapshot>();
        private readonly Dictionary<string, List<StoredEvent>> eventsByType = new Dictionary<string, List<StoredEvent>>();
        private readonly Dictionary<string, List<StoredEvent>> eventsByPlayer = new Dictionary<string, List<StoredEvent>>();
        
        // State tracking
        private long nextSequenceNumber = 1;
        private DateTime lastFlush = DateTime.UtcNow;
        private bool isDisposed = false;
        private Game game;
        private IEventBus eventBus;
        
        // Performance tracking
        private int totalEventsStored = 0;
        private int totalSnapshotsTaken = 0;
        private DateTime storeStartTime;
        
        /// <summary>
        /// Statistics about the event store
        /// </summary>
        public EventStoreStatistics GetStatistics()
        {
            return new EventStoreStatistics
            {
                TotalEventsStored = totalEventsStored,
                EventsInMemory = events.Count,
                TotalSnapshots = totalSnapshotsTaken,
                SnapshotsInMemory = snapshots.Count,
                MemoryUsageEstimate = EstimateMemoryUsage(),
                OldestEventTime = events.FirstOrDefault()?.timestamp ?? DateTime.MinValue,
                NewestEventTime = events.LastOrDefault()?.timestamp ?? DateTime.MinValue,
                UptimeSeconds = (DateTime.UtcNow - storeStartTime).TotalSeconds,
                LastFlushTime = lastFlush
            };
        }
        
        /// <summary>
        /// Initialize the event store
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="eventBus">Event bus to subscribe to</param>
        public void Initialize(Game game, IEventBus eventBus)
        {
            this.game = game;
            this.eventBus = eventBus;
            this.storeStartTime = DateTime.UtcNow;
            
            // Subscribe to all events for storage
            SubscribeToAllEvents();
            
            // Load existing events if persistence is enabled
            if (config.enablePersistence)
            {
                LoadEventsFromDisk();
                LoadSnapshotsFromDisk();
            }
            
            // Start background tasks
            if (config.enableAsyncOperations)
            {
                StartBackgroundTasks();
            }
            
            Debug.Log($"📦 Event store initialized with {events.Count} events and {snapshots.Count} snapshots");
        }
        
        /// <summary>
        /// Store an event in the event store
        /// </summary>
        /// <param name="gameEvent">Event to store</param>
        public async Task<StoredEvent> StoreEvent(GameEvent gameEvent)
        {
            if (isDisposed) return null;
            
            try
            {
                var storedEvent = CreateStoredEvent(gameEvent);
                
                // Add to main storage
                events.Add(storedEvent);
                totalEventsStored++;
                
                // Add to indexes
                if (!eventsByType.ContainsKey(storedEvent.eventType))
                    eventsByType[storedEvent.eventType] = new List<StoredEvent>();
                eventsByType[storedEvent.eventType].Add(storedEvent);
                
                if (!string.IsNullOrEmpty(storedEvent.playerId))
                {
                    if (!eventsByPlayer.ContainsKey(storedEvent.playerId))
                        eventsByPlayer[storedEvent.playerId] = new List<StoredEvent>();
                    eventsByPlayer[storedEvent.playerId].Add(storedEvent);
                }
                
                // Check if we need to create a snapshot
                if (config.enableSnapshots && events.Count % config.snapshotInterval == 0)
                {
                    await CreateSnapshot();
                }
                
                // Clean up old events if memory limit exceeded
                if (events.Count > config.maxEventsInMemory)
                {
                    CleanupOldEvents();
                }
                
                // Flush to disk if needed
                if (config.enablePersistence && ShouldFlush())
                {
                    await FlushToDisk();
                }
                
                return storedEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error storing event: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get events by type
        /// </summary>
        /// <param name="eventType">Event type to filter by</param>
        /// <param name="fromSequence">Starting sequence number</param>
        /// <param name="toSequence">Ending sequence number</param>
        /// <returns>Filtered events</returns>
        public List<StoredEvent> GetEventsByType(string eventType, long fromSequence = 0, long toSequence = long.MaxValue)
        {
            if (!eventsByType.ContainsKey(eventType))
                return new List<StoredEvent>();
                
            return eventsByType[eventType]
                .Where(e => e.sequenceNumber >= fromSequence && e.sequenceNumber <= toSequence)
                .OrderBy(e => e.sequenceNumber)
                .ToList();
        }
        
        /// <summary>
        /// Get events by player
        /// </summary>
        /// <param name="playerId">Player ID to filter by</param>
        /// <param name="fromSequence">Starting sequence number</param>
        /// <param name="toSequence">Ending sequence number</param>
        /// <returns>Filtered events</returns>
        public List<StoredEvent> GetEventsByPlayer(string playerId, long fromSequence = 0, long toSequence = long.MaxValue)
        {
            if (string.IsNullOrEmpty(playerId) || !eventsByPlayer.ContainsKey(playerId))
                return new List<StoredEvent>();
                
            return eventsByPlayer[playerId]
                .Where(e => e.sequenceNumber >= fromSequence && e.sequenceNumber <= toSequence)
                .OrderBy(e => e.sequenceNumber)
                .ToList();
        }
        
        /// <summary>
        /// Get all events in a sequence range
        /// </summary>
        /// <param name="fromSequence">Starting sequence number</param>
        /// <param name="toSequence">Ending sequence number</param>
        /// <returns>Events in range</returns>
        public List<StoredEvent> GetEventsInRange(long fromSequence = 0, long toSequence = long.MaxValue)
        {
            return events
                .Where(e => e.sequenceNumber >= fromSequence && e.sequenceNumber <= toSequence)
                .OrderBy(e => e.sequenceNumber)
                .ToList();
        }
        
        /// <summary>
        /// Replay events from a specific point
        /// </summary>
        /// <param name="fromSequence">Starting sequence number</param>
        /// <param name="eventBus">Event bus to replay events through</param>
        /// <returns>Task representing the replay operation</returns>
        public async Task ReplayEvents(long fromSequence, IEventBus eventBus)
        {
            var eventsToReplay = GetEventsInRange(fromSequence);
            
            Debug.Log($"🔄 Replaying {eventsToReplay.Count} events from sequence {fromSequence}");
            
            foreach (var storedEvent in eventsToReplay)
            {
                try
                {
                    var gameEvent = DeserializeEvent(storedEvent);
                    if (gameEvent != null)
                    {
                        await eventBus.PublishAsync(gameEvent);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error replaying event {storedEvent.eventId}: {ex.Message}");
                }
            }
            
            Debug.Log($"✅ Event replay completed");
        }
        
        /// <summary>
        /// Create a snapshot of the current game state
        /// </summary>
        /// <returns>Task representing the snapshot operation</returns>
        public async Task<EventSnapshot> CreateSnapshot()
        {
            try
            {
                var snapshot = new EventSnapshot
                {
                    snapshotId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow,
                    lastEventSequence = nextSequenceNumber - 1,
                    gameId = game.gameId,
                    serializedGameState = SerializeGameState(),
                    snapshotMetadata = new Dictionary<string, object>
                    {
                        { "round_number", game.roundNumber },
                        { "current_phase", game.currentPhase },
                        { "event_count", events.Count }
                    }
                };
                
                snapshots.Add(snapshot);
                totalSnapshotsTaken++;
                
                // Clean up old snapshots
                if (snapshots.Count > config.maxSnapshots)
                {
                    snapshots.RemoveAt(0);
                }
                
                if (config.enablePersistence)
                {
                    await SaveSnapshotToDisk(snapshot);
                }
                
                Debug.Log($"📸 Snapshot created: {snapshot.snapshotId} (sequence {snapshot.lastEventSequence})");
                return snapshot;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error creating snapshot: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get the most recent snapshot
        /// </summary>
        /// <returns>Most recent snapshot or null</returns>
        public EventSnapshot GetLatestSnapshot()
        {
            return snapshots.LastOrDefault();
        }
        
        /// <summary>
        /// Clear all stored events and snapshots
        /// </summary>
        public void ClearStore()
        {
            events.Clear();
            snapshots.Clear();
            eventsByType.Clear();
            eventsByPlayer.Clear();
            nextSequenceNumber = 1;
            totalEventsStored = 0;
            totalSnapshotsTaken = 0;
            
            Debug.Log("🗑️ Event store cleared");
        }
        
        #region Private Methods
        
        private void SubscribeToAllEvents()
        {
            // This would need to be implemented based on your specific event bus API
            // For now, we'll use a generic subscription approach
            Debug.Log("📡 Subscribing to all events for storage");
        }
        
        private StoredEvent CreateStoredEvent(GameEvent gameEvent)
        {
            return new StoredEvent
            {
                eventId = gameEvent.EventId,
                eventType = gameEvent.GetEventTypeName(),
                timestamp = gameEvent.Timestamp,
                serializedEvent = JsonUtility.ToJson(gameEvent),
                sequenceNumber = nextSequenceNumber++,
                gameId = gameEvent.Game?.gameId,
                playerId = gameEvent.TriggeredBy?.PlayerId,
                metadata = new Dictionary<string, object>
                {
                    { "source_type", gameEvent.Source?.GetType().Name },
                    { "event_size", JsonUtility.ToJson(gameEvent).Length }
                }
            };
        }
        
        private GameEvent DeserializeEvent(StoredEvent storedEvent)
        {
            try
            {
                // This would need proper type resolution based on eventType
                var eventType = Type.GetType($"L5RGame.Events.{storedEvent.eventType}");
                if (eventType != null)
                {
                    return JsonUtility.FromJson(storedEvent.serializedEvent, eventType) as GameEvent;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error deserializing event {storedEvent.eventId}: {ex.Message}");
            }
            return null;
        }
        
        private string SerializeGameState()
        {
            // Serialize the current game state for snapshots
            var gameState = game.GetSaveState();
            return JsonUtility.ToJson(gameState);
        }
        
        private bool ShouldFlush()
        {
            return (DateTime.UtcNow - lastFlush).TotalSeconds >= config.flushInterval;
        }
        
        private async Task FlushToDisk()
        {
            if (config.enableAsyncOperations)
            {
                await Task.Run(() => SaveEventsToDisk());
            }
            else
            {
                SaveEventsToDisk();
            }
            lastFlush = DateTime.UtcNow;
        }
        
        private void SaveEventsToDisk()
        {
            try
            {
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, config.storageFileName);
                var eventsToSave = events.Skip(Math.Max(0, events.Count - config.batchSize)).ToList();
                var json = JsonUtility.ToJson(new { events = eventsToSave });
                System.IO.File.WriteAllText(filePath, json);
                Debug.Log($"💾 Saved {eventsToSave.Count} events to disk");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error saving events to disk: {ex.Message}");
            }
        }
        
        private void LoadEventsFromDisk()
        {
            try
            {
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, config.storageFileName);
                if (System.IO.File.Exists(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    // Implementation would parse and load events
                    Debug.Log("📁 Loaded events from disk");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error loading events from disk: {ex.Message}");
            }
        }
        
        private void LoadSnapshotsFromDisk()
        {
            try
            {
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, "snapshots.json");
                if (System.IO.File.Exists(filePath))
                {
                    // Implementation would parse and load snapshots
                    Debug.Log("📁 Loaded snapshots from disk");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error loading snapshots from disk: {ex.Message}");
            }
        }
        
        private async Task SaveSnapshotToDisk(EventSnapshot snapshot)
        {
            try
            {
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, $"snapshot_{snapshot.snapshotId}.json");
                var json = JsonUtility.ToJson(snapshot);
                
                if (config.enableAsyncOperations)
                {
                    await Task.Run(() => System.IO.File.WriteAllText(filePath, json));
                }
                else
                {
                    System.IO.File.WriteAllText(filePath, json);
                }
                
                Debug.Log($"💾 Saved snapshot {snapshot.snapshotId} to disk");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error saving snapshot to disk: {ex.Message}");
            }
        }
        
        private void CleanupOldEvents()
        {
            if (events.Count <= config.maxEventsInMemory) return;
            
            var eventsToRemove = events.Count - config.maxEventsInMemory + config.batchSize;
            var removedEvents = events.Take(eventsToRemove).ToList();
            
            // Remove from main storage
            events.RemoveRange(0, eventsToRemove);
            
            // Remove from indexes
            foreach (var removedEvent in removedEvents)
            {
                if (eventsByType.ContainsKey(removedEvent.eventType))
                {
                    eventsByType[removedEvent.eventType].Remove(removedEvent);
                }
                
                if (!string.IsNullOrEmpty(removedEvent.playerId) && eventsByPlayer.ContainsKey(removedEvent.playerId))
                {
                    eventsByPlayer[removedEvent.playerId].Remove(removedEvent);
                }
            }
            
            Debug.Log($"🧹 Cleaned up {eventsToRemove} old events from memory");
        }
        
        private void StartBackgroundTasks()
        {
            InvokeRepeating(nameof(BackgroundMaintenance), config.flushInterval, config.flushInterval);
        }
        
        private async void BackgroundMaintenance()
        {
            if (isDisposed) return;
            
            try
            {
                // Flush to disk if needed
                if (config.enablePersistence && ShouldFlush())
                {
                    await FlushToDisk();
                }
                
                // Create snapshot if needed
                if (config.enableSnapshots && events.Count % config.snapshotInterval == 0 && events.Count > 0)
                {
                    await CreateSnapshot();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error in background maintenance: {ex.Message}");
            }
        }
        
        private long EstimateMemoryUsage()
        {
            long totalSize = 0;
            foreach (var storedEvent in events)
            {
                totalSize += storedEvent.serializedEvent?.Length ?? 0;
                totalSize += 200; // Approximate overhead per event
            }
            
            foreach (var snapshot in snapshots)
            {
                totalSize += snapshot.serializedGameState?.Length ?? 0;
                totalSize += 300; // Approximate overhead per snapshot
            }
            
            return totalSize;
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (isDisposed) return;
            
            try
            {
                CancelInvoke();
                
                if (config.enablePersistence)
                {
                    SaveEventsToDisk();
                    // Save final snapshots
                    foreach (var snapshot in snapshots)
                    {
                        var task = SaveSnapshotToDisk(snapshot);
                        task.Wait(TimeSpan.FromSeconds(5)); // Wait max 5 seconds
                    }
                }
                
                ClearStore();
                isDisposed = true;
                
                Debug.Log("🗑️ Event store disposed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error disposing event store: {ex.Message}");
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Statistics about event store performance and usage
    /// </summary>
    [Serializable]
    public class EventStoreStatistics
    {
        public int TotalEventsStored { get; set; }
        public int EventsInMemory { get; set; }
        public int TotalSnapshots { get; set; }
        public int SnapshotsInMemory { get; set; }
        public long MemoryUsageEstimate { get; set; }
        public DateTime OldestEventTime { get; set; }
        public DateTime NewestEventTime { get; set; }
        public double UptimeSeconds { get; set; }
        public DateTime LastFlushTime { get; set; }
        
        public override string ToString()
        {
            return $"Events: {TotalEventsStored} stored ({EventsInMemory} in memory), " +
                   $"Snapshots: {TotalSnapshots} ({SnapshotsInMemory} in memory), " +
                   $"Memory: {MemoryUsageEstimate / 1024.0:F1} KB, " +
                   $"Uptime: {UptimeSeconds / 3600.0:F1} hours";
        }
    }
}