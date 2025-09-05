using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame.Events
{
    /// <summary>
    /// High-performance object pool for game events to reduce garbage collection
    /// </summary>
    public static class EventPool
    {
        #region Private Fields
        
        private static readonly Dictionary<Type, ConcurrentQueue<GameEvent>> _eventPools;
        private static readonly Dictionary<Type, int> _poolSizes;
        private static readonly Dictionary<Type, int> _poolHits;
        private static readonly Dictionary<Type, int> _poolMisses;
        private static readonly object _statisticsLock = new object();
        
        // Configuration
        private const int DEFAULT_POOL_SIZE = 50;
        private const int MAX_POOL_SIZE = 200;
        private const int POOL_CLEANUP_INTERVAL = 1000; // Every 1000 gets/returns
        
        private static int _totalOperations = 0;
        private static DateTime _lastCleanup = DateTime.UtcNow;
        
        #endregion
        
        #region Static Constructor
        
        static EventPool()
        {
            _eventPools = new Dictionary<Type, ConcurrentQueue<GameEvent>>();
            _poolSizes = new Dictionary<Type, int>();
            _poolHits = new Dictionary<Type, int>();
            _poolMisses = new Dictionary<Type, int>();
            
            // Pre-initialize pools for common event types
            InitializeCommonEventPools();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get an event from the pool or create a new one
        /// </summary>
        /// <typeparam name="T">Type of event to get</typeparam>
        /// <returns>Event instance (pooled or new)</returns>
        public static T Get<T>() where T : GameEvent, new()
        {
            var eventType = typeof(T);
            
            // Try to get from pool first
            if (_eventPools.TryGetValue(eventType, out var pool) && pool.TryDequeue(out var pooledEvent))
            {
                RecordPoolHit(eventType);
                return (T)pooledEvent;
            }
            
            // Create new instance if pool is empty
            RecordPoolMiss(eventType);
            return new T();
        }
        
        /// <summary>
        /// Return an event to the pool for reuse
        /// </summary>
        /// <param name="gameEvent">Event to return to pool</param>
        public static void Return(GameEvent gameEvent)
        {
            if (gameEvent == null)
                return;
                
            var eventType = gameEvent.GetType();
            
            // Reset the event for reuse
            ResetEventForReuse(gameEvent);
            
            // Get or create pool for this event type
            if (!_eventPools.TryGetValue(eventType, out var pool))
            {
                pool = new ConcurrentQueue<GameEvent>();
                _eventPools[eventType] = pool;
                _poolSizes[eventType] = 0;
            }
            
            // Only add to pool if we haven't exceeded max size
            if (_poolSizes[eventType] < MAX_POOL_SIZE)
            {
                pool.Enqueue(gameEvent);
                _poolSizes[eventType]++;
            }
            
            // Periodic cleanup
            _totalOperations++;
            if (_totalOperations % POOL_CLEANUP_INTERVAL == 0)
            {
                PerformPoolCleanup();
            }
        }
        
        /// <summary>
        /// Get statistics about pool performance
        /// </summary>
        /// <returns>Pool statistics</returns>
        public static EventPoolStatistics GetStatistics()
        {
            lock (_statisticsLock)
            {
                var stats = new EventPoolStatistics
                {
                    TotalOperations = _totalOperations,
                    PoolTypes = _eventPools.Count,
                    LastCleanupTime = _lastCleanup
                };
                
                foreach (var poolType in _eventPools.Keys)
                {
                    var hits = _poolHits.GetValueOrDefault(poolType, 0);
                    var misses = _poolMisses.GetValueOrDefault(poolType, 0);
                    var total = hits + misses;
                    var hitRate = total > 0 ? (double)hits / total : 0;
                    
                    stats.PoolDetails[poolType.Name] = new PoolTypeStatistics
                    {
                        PoolSize = _poolSizes.GetValueOrDefault(poolType, 0),
                        Hits = hits,
                        Misses = misses,
                        HitRate = hitRate
                    };
                }
                
                return stats;
            }
        }
        
        /// <summary>
        /// Clear all pools (useful for cleanup or testing)
        /// </summary>
        public static void ClearAllPools()
        {
            lock (_statisticsLock)
            {
                _eventPools.Clear();
                _poolSizes.Clear();
                _poolHits.Clear();
                _poolMisses.Clear();
                _totalOperations = 0;
                
                Debug.Log("🧹 EventPool: All pools cleared");
                
                // Re-initialize common pools
                InitializeCommonEventPools();
            }
        }
        
        /// <summary>
        /// Pre-warm a specific event type pool
        /// </summary>
        /// <typeparam name="T">Event type to pre-warm</typeparam>
        /// <param name="count">Number of instances to create</param>
        public static void PreWarm<T>(int count = DEFAULT_POOL_SIZE) where T : GameEvent, new()
        {
            var eventType = typeof(T);
            
            if (!_eventPools.TryGetValue(eventType, out var pool))
            {
                pool = new ConcurrentQueue<GameEvent>();
                _eventPools[eventType] = pool;
                _poolSizes[eventType] = 0;
            }
            
            for (int i = 0; i < count && _poolSizes[eventType] < MAX_POOL_SIZE; i++)
            {
                var eventInstance = new T();
                ResetEventForReuse(eventInstance);
                pool.Enqueue(eventInstance);
                _poolSizes[eventType]++;
            }
            
            Debug.Log($"🔥 EventPool: Pre-warmed {count} instances of {eventType.Name}");
        }
        
        #endregion
        
        #region Private Methods
        
        private static void InitializeCommonEventPools()
        {
            // Pre-warm pools for the most common event types
            PreWarm<ActionExecutedEvent>(DEFAULT_POOL_SIZE);
            PreWarm<PlayerActionEvent>(DEFAULT_POOL_SIZE);
            PreWarm<GameStateChangedEvent>(20);
            PreWarm<ValidationEvent>(30);
            PreWarm<CardDrawnEvent>(40);
            PreWarm<GameMessageEvent>(60);
        }
        
        private static void ResetEventForReuse(GameEvent gameEvent)
        {
            // Note: EventId and Timestamp are auto-generated read-only properties
            // They will be set automatically when the event is created
            
            // Clear event data dictionary
            if (gameEvent.GetAllEventData() is Dictionary<string, object> eventData)
            {
                eventData.Clear();
            }
            
            // Note: Specific event properties will need to be reset when the event is reused
            // This is handled by the event classes themselves when they're retrieved from the pool
        }
        
        private static void RecordPoolHit(Type eventType)
        {
            lock (_statisticsLock)
            {
                _poolHits[eventType] = _poolHits.GetValueOrDefault(eventType, 0) + 1;
            }
        }
        
        private static void RecordPoolMiss(Type eventType)
        {
            lock (_statisticsLock)
            {
                _poolMisses[eventType] = _poolMisses.GetValueOrDefault(eventType, 0) + 1;
            }
        }
        
        private static void PerformPoolCleanup()
        {
            var now = DateTime.UtcNow;
            var timeSinceLastCleanup = now - _lastCleanup;
            
            // Only cleanup if it's been a while
            if (timeSinceLastCleanup.TotalMinutes < 1)
                return;
                
            lock (_statisticsLock)
            {
                var poolsToCleanup = new List<Type>();
                
                foreach (var kvp in _eventPools)
                {
                    var eventType = kvp.Key;
                    var pool = kvp.Value;
                    var poolSize = _poolSizes.GetValueOrDefault(eventType, 0);
                    
                    // If pool is too large, reduce it
                    if (poolSize > DEFAULT_POOL_SIZE * 2)
                    {
                        var itemsToRemove = poolSize - DEFAULT_POOL_SIZE;
                        for (int i = 0; i < itemsToRemove && pool.TryDequeue(out _); i++)
                        {
                            _poolSizes[eventType]--;
                        }
                        
                        poolsToCleanup.Add(eventType);
                    }
                }
                
                _lastCleanup = now;
                
                if (poolsToCleanup.Count > 0)
                {
                    Debug.Log($"🧹 EventPool: Cleaned up {poolsToCleanup.Count} oversized pools");
                }
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Statistics for EventPool performance monitoring
    /// </summary>
    [Serializable]
    public class EventPoolStatistics
    {
        public int TotalOperations { get; set; }
        public int PoolTypes { get; set; }
        public DateTime LastCleanupTime { get; set; }
        public Dictionary<string, PoolTypeStatistics> PoolDetails { get; set; } = new Dictionary<string, PoolTypeStatistics>();
        
        public double OverallHitRate
        {
            get
            {
                var totalHits = 0;
                var totalRequests = 0;
                
                foreach (var detail in PoolDetails.Values)
                {
                    totalHits += detail.Hits;
                    totalRequests += detail.Hits + detail.Misses;
                }
                
                return totalRequests > 0 ? (double)totalHits / totalRequests : 0;
            }
        }
        
        public override string ToString()
        {
            return $"EventPool Stats - Operations: {TotalOperations:N0}, Types: {PoolTypes}, Hit Rate: {OverallHitRate:P1}";
        }
    }
    
    [Serializable]
    public class PoolTypeStatistics
    {
        public int PoolSize { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public double HitRate { get; set; }
    }
}