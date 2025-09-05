using System;
using UnityEngine;

namespace L5RGame.Events
{
    /// <summary>
    /// Configuration settings for the EventStore
    /// </summary>
    [Serializable]
    public class EventStoreConfiguration
    {
        [Header("Storage Settings")]
        [Tooltip("Maximum number of events to store in memory")]
        public int MaxStoredEvents = 10000;
        
        [Tooltip("Maximum events before triggering cleanup")]
        public int MaxEventsBeforeCleanup = 15000;
        
        [Tooltip("Maximum events per type/player index")]
        public int MaxEventsPerIndex = 1000;
        
        [Tooltip("How often to run cleanup (every N events)")]
        public int CleanupInterval = 500;
        
        [Header("Snapshot Settings")]
        [Tooltip("Create snapshot every N seconds")]
        public int SnapshotIntervalSeconds = 60;
        
        [Tooltip("Create snapshot every N events")]
        public int SnapshotIntervalEvents = 1000;
        
        [Tooltip("Maximum snapshots to keep")]
        public int MaxSnapshots = 10;
        
        [Header("Persistence Settings")]
        [Tooltip("Enable saving events to disk")]
        public bool EnablePersistence = true;
        
        [Tooltip("Persist events every N seconds")]
        public int PersistenceIntervalSeconds = 30;
        
        [Tooltip("Number of events to save per batch")]
        public int EventsPersistBatchSize = 100;
        
        [Tooltip("Directory to save persistent events")]
        public string PersistencePath = "GameData/Events";
        
        [Header("Performance Settings")]
        [Tooltip("Enable compression for persisted events")]
        public bool EnableCompression = true;
        
        [Tooltip("Enable event indexing for faster queries")]
        public bool EnableIndexing = true;
        
        [Tooltip("Maximum memory usage in MB before cleanup")]
        public int MaxMemoryUsageMB = 100;
        
        /// <summary>
        /// Create default configuration
        /// </summary>
        public static EventStoreConfiguration CreateDefault()
        {
            return new EventStoreConfiguration();
        }
        
        /// <summary>
        /// Create high-performance configuration for production
        /// </summary>
        public static EventStoreConfiguration CreateHighPerformance()
        {
            return new EventStoreConfiguration
            {
                MaxStoredEvents = 50000,
                MaxEventsBeforeCleanup = 75000,
                MaxEventsPerIndex = 5000,
                CleanupInterval = 1000,
                SnapshotIntervalSeconds = 30,
                SnapshotIntervalEvents = 2000,
                MaxSnapshots = 20,
                EnablePersistence = true,
                PersistenceIntervalSeconds = 15,
                EventsPersistBatchSize = 500,
                EnableCompression = true,
                EnableIndexing = true,
                MaxMemoryUsageMB = 200
            };
        }
        
        /// <summary>
        /// Create low-memory configuration for mobile/limited devices
        /// </summary>
        public static EventStoreConfiguration CreateLowMemory()
        {
            return new EventStoreConfiguration
            {
                MaxStoredEvents = 2000,
                MaxEventsBeforeCleanup = 3000,
                MaxEventsPerIndex = 200,
                CleanupInterval = 100,
                SnapshotIntervalSeconds = 120,
                SnapshotIntervalEvents = 500,
                MaxSnapshots = 5,
                EnablePersistence = false,
                PersistenceIntervalSeconds = 60,
                EventsPersistBatchSize = 50,
                EnableCompression = false,
                EnableIndexing = false,
                MaxMemoryUsageMB = 25
            };
        }
        
        /// <summary>
        /// Create debugging configuration with frequent snapshots and persistence
        /// </summary>
        public static EventStoreConfiguration CreateDebugging()
        {
            return new EventStoreConfiguration
            {
                MaxStoredEvents = 20000,
                MaxEventsBeforeCleanup = 30000,
                MaxEventsPerIndex = 2000,
                CleanupInterval = 200,
                SnapshotIntervalSeconds = 10,
                SnapshotIntervalEvents = 100,
                MaxSnapshots = 50,
                EnablePersistence = true,
                PersistenceIntervalSeconds = 5,
                EventsPersistBatchSize = 25,
                EnableCompression = false,
                EnableIndexing = true,
                MaxMemoryUsageMB = 150
            };
        }
        
        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public void Validate()
        {
            MaxStoredEvents = Mathf.Max(100, MaxStoredEvents);
            MaxEventsBeforeCleanup = Mathf.Max(MaxStoredEvents + 100, MaxEventsBeforeCleanup);
            MaxEventsPerIndex = Mathf.Max(10, MaxEventsPerIndex);
            CleanupInterval = Mathf.Max(10, CleanupInterval);
            SnapshotIntervalSeconds = Mathf.Max(1, SnapshotIntervalSeconds);
            SnapshotIntervalEvents = Mathf.Max(10, SnapshotIntervalEvents);
            MaxSnapshots = Mathf.Max(1, MaxSnapshots);
            PersistenceIntervalSeconds = Mathf.Max(1, PersistenceIntervalSeconds);
            EventsPersistBatchSize = Mathf.Max(1, EventsPersistBatchSize);
            MaxMemoryUsageMB = Mathf.Max(10, MaxMemoryUsageMB);
            
            if (string.IsNullOrEmpty(PersistencePath))
            {
                PersistencePath = "GameData/Events";
            }
        }
        
        /// <summary>
        /// Get configuration summary
        /// </summary>
        public override string ToString()
        {
            return $"EventStore Config - MaxEvents: {MaxStoredEvents}, Snapshots: every {SnapshotIntervalSeconds}s/{SnapshotIntervalEvents} events, " +
                   $"Persistence: {(EnablePersistence ? "enabled" : "disabled")}, Memory: {MaxMemoryUsageMB}MB";
        }
    }
}