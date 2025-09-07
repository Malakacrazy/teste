using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using L5RGame.Events;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// High-performance thread-safe event bus implementation for the L5R card game.
    /// Supports both synchronous and asynchronous event handling with comprehensive error handling.
    /// </summary>
    public class GameEventBus : IEventBus, IDisposable
    {
        #region Private Fields
        
        private readonly ConcurrentDictionary<Type, ConcurrentBag<EventSubscriptionInternal>> _subscriptions;
        private readonly ConcurrentDictionary<string, ConcurrentBag<EventSubscriptionInternal>> _namedSubscriptions;
        private readonly ConcurrentBag<EventSubscriptionInternal> _allEventSubscriptions;
        private readonly ConcurrentDictionary<string, EventSubscriptionInternal> _subscriptionLookup;
        
        protected readonly object _lockObject = new object();
        private volatile bool _enabled = true;
        private volatile bool _disposed = false;
        
        // Statistics and monitoring
        protected readonly EventBusStats _stats = new EventBusStats();
        private readonly ConcurrentBag<Exception> _recentErrors = new ConcurrentBag<Exception>();
        
        // Configuration
        private readonly bool _enableDebugLogging;
        private readonly bool _enablePerformanceMonitoring;
        private readonly int _maxErrorsToKeep = 100;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create a new GameEventBus instance
        /// </summary>
        /// <param name="enableDebugLogging">Enable debug logging</param>
        /// <param name="enablePerformanceMonitoring">Enable performance monitoring</param>
        public GameEventBus(bool enableDebugLogging = false, bool enablePerformanceMonitoring = false)
        {
            _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<EventSubscriptionInternal>>();
            _namedSubscriptions = new ConcurrentDictionary<string, ConcurrentBag<EventSubscriptionInternal>>();
            _allEventSubscriptions = new ConcurrentBag<EventSubscriptionInternal>();
            _subscriptionLookup = new ConcurrentDictionary<string, EventSubscriptionInternal>();
            
            _enableDebugLogging = enableDebugLogging;
            _enablePerformanceMonitoring = enablePerformanceMonitoring;
            
            if (_enableDebugLogging)
                Debug.Log("🚌 GameEventBus initialized with debug logging enabled");
        }
        
        #endregion
        
        #region IEventBus Implementation
        
        public bool IsEnabled => _enabled && !_disposed;
        
        public IEventSubscription Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            ThrowIfDisposed();
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var subscription = new EventSubscriptionInternal(
                typeof(T), 
                typeof(T).Name,
                evt => handler((T)evt),
                null // No async handler
            );
            
            // Add to type-based subscriptions
            var eventType = typeof(T);
            _subscriptions.AddOrUpdate(
                eventType,
                new ConcurrentBag<EventSubscriptionInternal> { subscription },
                (key, existing) => { existing.Add(subscription); return existing; }
            );
            
            // Add to lookup
            _subscriptionLookup.TryAdd(subscription.Id, subscription);
            
            // Update stats
            lock (_lockObject)
            {
                _stats.TotalSubscriptions++;
                _stats.ActiveSubscriptions++;
                _stats.SubscriptionCounts.TryGetValue(typeof(T).Name, out int count);
                _stats.SubscriptionCounts[typeof(T).Name] = count + 1;
            }
            
            if (_enableDebugLogging)
                Debug.Log($"🚌 Subscribed to {typeof(T).Name} events. Subscription ID: {subscription.Id}");
            
            return subscription;
        }
        
        public IEventSubscription Subscribe<T>(Func<T, Task> handler) where T : GameEvent
        {
            ThrowIfDisposed();
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var subscription = new EventSubscriptionInternal(
                typeof(T),
                typeof(T).Name, 
                null, // No sync handler
                async evt => await handler((T)evt)
            );
            
            // Add to type-based subscriptions
            var eventType = typeof(T);
            _subscriptions.AddOrUpdate(
                eventType,
                new ConcurrentBag<EventSubscriptionInternal> { subscription },
                (key, existing) => { existing.Add(subscription); return existing; }
            );
            
            // Add to lookup
            _subscriptionLookup.TryAdd(subscription.Id, subscription);
            
            // Update stats
            lock (_lockObject)
            {
                _stats.TotalSubscriptions++;
                _stats.ActiveSubscriptions++;
                _stats.SubscriptionCounts.TryGetValue(typeof(T).Name, out int count);
                _stats.SubscriptionCounts[typeof(T).Name] = count + 1;
            }
            
            if (_enableDebugLogging)
                Debug.Log($"🚌 Subscribed to {typeof(T).Name} events (async). Subscription ID: {subscription.Id}");
            
            return subscription;
        }
        
        public IEventSubscription Subscribe(string eventName, Action<GameEvent> handler)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var subscription = new EventSubscriptionInternal(
                typeof(GameEvent),
                eventName,
                handler,
                null // No async handler
            );
            
            // Add to name-based subscriptions
            _namedSubscriptions.AddOrUpdate(
                eventName,
                new ConcurrentBag<EventSubscriptionInternal> { subscription },
                (key, existing) => { existing.Add(subscription); return existing; }
            );
            
            // Add to lookup
            _subscriptionLookup.TryAdd(subscription.Id, subscription);
            
            // Update stats
            lock (_lockObject)
            {
                _stats.TotalSubscriptions++;
                _stats.ActiveSubscriptions++;
                _stats.SubscriptionCounts.TryGetValue(eventName, out int count);
                _stats.SubscriptionCounts[eventName] = count + 1;
            }
            
            if (_enableDebugLogging)
                Debug.Log($"🚌 Subscribed to '{eventName}' events by name. Subscription ID: {subscription.Id}");
            
            return subscription;
        }
        
        public IEventSubscription SubscribeToAll(Action<GameEvent> handler)
        {
            ThrowIfDisposed();
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var subscription = new EventSubscriptionInternal(
                typeof(GameEvent),
                "*", // Special marker for all events
                handler,
                null // No async handler
            );
            
            _allEventSubscriptions.Add(subscription);
            _subscriptionLookup.TryAdd(subscription.Id, subscription);
            
            // Update stats
            lock (_lockObject)
            {
                _stats.TotalSubscriptions++;
                _stats.ActiveSubscriptions++;
                _stats.SubscriptionCounts.TryGetValue("*", out int count);
                _stats.SubscriptionCounts["*"] = count + 1;
            }
            
            if (_enableDebugLogging)
                Debug.Log($"🚌 Subscribed to all events. Subscription ID: {subscription.Id}");
            
            return subscription;
        }
        
        public void Unsubscribe(IEventSubscription subscription)
        {
            if (subscription == null || _disposed)
                return;
            
            if (_subscriptionLookup.TryRemove(subscription.Id, out var internalSub))
            {
                internalSub.Unsubscribe();
                
                // Update stats
                lock (_lockObject)
                {
                    _stats.ActiveSubscriptions--;
                    var eventKey = internalSub.EventName ?? internalSub.EventType.Name;
                    if (_stats.SubscriptionCounts.TryGetValue(eventKey, out int count) && count > 0)
                    {
                        _stats.SubscriptionCounts[eventKey] = count - 1;
                    }
                }
                
                if (_enableDebugLogging)
                    Debug.Log($"🚌 Unsubscribed subscription {subscription.Id}");
            }
        }
        
        public void Publish<T>(T eventInstance) where T : GameEvent
        {
            if (!IsEnabled || eventInstance == null)
                return;
                
            ThrowIfDisposed();
            
            var startTime = _enablePerformanceMonitoring ? DateTime.UtcNow : default;
            
            try
            {
                // Update stats
                lock (_lockObject)
                {
                    _stats.TotalEventsPublished++;
                    _stats.LastEventTime = DateTime.UtcNow;
                    _stats.EventCounts.TryGetValue(eventInstance.EventName, out int count);
                    _stats.EventCounts[eventInstance.EventName] = count + 1;
                }
                
                if (_enableDebugLogging)
                    Debug.Log($"🚌 Publishing {eventInstance.EventName} event: {eventInstance}");
                
                // Publish to type-based subscribers
                if (_subscriptions.TryGetValue(typeof(T), out var typeSubscriptions))
                {
                    PublishToSubscriptions(typeSubscriptions, eventInstance);
                }
                
                // Publish to name-based subscribers
                if (_namedSubscriptions.TryGetValue(eventInstance.EventName, out var namedSubscriptions))
                {
                    PublishToSubscriptions(namedSubscriptions, eventInstance);
                }
                
                // Publish to all-event subscribers
                PublishToSubscriptions(_allEventSubscriptions, eventInstance);
                
                if (_enablePerformanceMonitoring)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    lock (_lockObject)
                    {
                        _stats.TotalProcessingTime = _stats.TotalProcessingTime.Add(elapsed);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, $"publishing {eventInstance.EventName}");
            }
        }
        
        public async Task PublishAsync<T>(T eventInstance) where T : GameEvent
        {
            if (!IsEnabled || eventInstance == null)
                return;
                
            ThrowIfDisposed();
            
            var startTime = _enablePerformanceMonitoring ? DateTime.UtcNow : default;
            
            try
            {
                // Update stats
                lock (_lockObject)
                {
                    _stats.TotalEventsPublished++;
                    _stats.LastEventTime = DateTime.UtcNow;
                    _stats.EventCounts.TryGetValue(eventInstance.EventName, out int count);
                    _stats.EventCounts[eventInstance.EventName] = count + 1;
                }
                
                if (_enableDebugLogging)
                    Debug.Log($"🚌 Publishing {eventInstance.EventName} event async: {eventInstance}");
                
                // Collect all async tasks
                var tasks = new List<Task>();
                
                // Publish to type-based subscribers
                if (_subscriptions.TryGetValue(typeof(T), out var typeSubscriptions))
                {
                    tasks.AddRange(PublishToSubscriptionsAsync(typeSubscriptions, eventInstance));
                }
                
                // Publish to name-based subscribers
                if (_namedSubscriptions.TryGetValue(eventInstance.EventName, out var namedSubscriptions))
                {
                    tasks.AddRange(PublishToSubscriptionsAsync(namedSubscriptions, eventInstance));
                }
                
                // Publish to all-event subscribers
                tasks.AddRange(PublishToSubscriptionsAsync(_allEventSubscriptions, eventInstance));
                
                // Wait for all handlers to complete
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }
                
                if (_enablePerformanceMonitoring)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    lock (_lockObject)
                    {
                        _stats.TotalProcessingTime = _stats.TotalProcessingTime.Add(elapsed);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, $"publishing {eventInstance.EventName} async");
            }
        }
        
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            
            if (_enableDebugLogging)
                Debug.Log($"🚌 EventBus {(enabled ? "enabled" : "disabled")}");
        }
        
        public void ClearAll()
        {
            if (_disposed)
                return;
                
            lock (_lockObject)
            {
                // Clear all subscriptions
                foreach (var subscription in _subscriptionLookup.Values)
                {
                    subscription.Unsubscribe();
                }
                
                _subscriptions.Clear();
                _namedSubscriptions.Clear();
                _subscriptionLookup.Clear();
                
                // Reset all-event subscriptions
                while (_allEventSubscriptions.TryTake(out _)) { }
                
                // Reset stats
                _stats.ActiveSubscriptions = 0;
                _stats.SubscriptionCounts.Clear();
                
                if (_enableDebugLogging)
                    Debug.Log("🚌 All event subscriptions cleared");
            }
        }
        
        public int GetSubscriptionCount()
        {
            return _stats.ActiveSubscriptions;
        }
        
        public int GetSubscriptionCount<T>() where T : GameEvent
        {
            if (_subscriptions.TryGetValue(typeof(T), out var subscriptions))
            {
                return subscriptions.Count(s => s.IsActive);
            }
            return 0;
        }
        
        public object GetDebugInfo()
        {
            var eventCounts = new Dictionary<string, int>(_stats.EventCounts);
            var subscriptionCounts = new Dictionary<string, int>(_stats.SubscriptionCounts);
            
            return new
            {
                enabled = IsEnabled,
                disposed = _disposed,
                totalEventsPublished = _stats.TotalEventsPublished,
                totalSubscriptions = _stats.TotalSubscriptions,
                activeSubscriptions = _stats.ActiveSubscriptions,
                lastEventTime = _stats.LastEventTime.ToString("HH:mm:ss.fff"),
                totalProcessingTime = _stats.TotalProcessingTime.TotalMilliseconds,
                errorCount = _stats.ErrorCount,
                eventCounts = eventCounts,
                subscriptionCounts = subscriptionCounts,
                recentErrorCount = _recentErrors.Count
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private void PublishToSubscriptions(IEnumerable<EventSubscriptionInternal> subscriptions, GameEvent eventInstance)
        {
            foreach (var subscription in subscriptions.Where(s => s.IsActive))
            {
                try
                {
                    if (subscription.SyncHandler != null)
                    {
                        subscription.SyncHandler(eventInstance);
                        subscription.IncrementInvocations();
                    }
                    else if (subscription.AsyncHandler != null)
                    {
                        // Fire and forget for sync publish
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await subscription.AsyncHandler(eventInstance);
                                subscription.IncrementInvocations();
                            }
                            catch (Exception ex)
                            {
                                HandleError(ex, $"async handler for {eventInstance.EventName}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    HandleError(ex, $"handler for {eventInstance.EventName}");
                }
            }
        }
        
        private IEnumerable<Task> PublishToSubscriptionsAsync(IEnumerable<EventSubscriptionInternal> subscriptions, GameEvent eventInstance)
        {
            var tasks = new List<Task>();
            
            foreach (var subscription in subscriptions.Where(s => s.IsActive))
            {
                if (subscription.AsyncHandler != null)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await subscription.AsyncHandler(eventInstance);
                            subscription.IncrementInvocations();
                        }
                        catch (Exception ex)
                        {
                            HandleError(ex, $"async handler for {eventInstance.EventName}");
                        }
                    }));
                }
                else if (subscription.SyncHandler != null)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            subscription.SyncHandler(eventInstance);
                            subscription.IncrementInvocations();
                        }
                        catch (Exception ex)
                        {
                            HandleError(ex, $"sync handler for {eventInstance.EventName}");
                        }
                    }));
                }
            }
            
            return tasks;
        }
        
        private void HandleError(Exception ex, string context)
        {
            lock (_lockObject)
            {
                _stats.ErrorCount++;
            }
            
            _recentErrors.Add(ex);
            
            // Keep only recent errors to prevent memory leaks
            if (_recentErrors.Count > _maxErrorsToKeep)
            {
                var errors = _recentErrors.ToArray();
                while (_recentErrors.TryTake(out _)) { }
                
                // Keep the most recent errors
                var recentErrors = errors.Skip(errors.Length - _maxErrorsToKeep / 2);
                foreach (var error in recentErrors)
                {
                    _recentErrors.Add(error);
                }
            }
            
            Debug.LogError($"🚌 EventBus error in {context}: {ex.Message}\n{ex.StackTrace}");
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameEventBus));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            ClearAll();
            
            if (_enableDebugLogging)
                Debug.Log("🚌 GameEventBus disposed");
        }
        
        #endregion
        
        #region Public Properties for Monitoring
        
        /// <summary>
        /// Get current statistics about the event bus
        /// </summary>
        public EventBusStats Stats => new EventBusStats
        {
            TotalEventsPublished = _stats.TotalEventsPublished,
            TotalSubscriptions = _stats.TotalSubscriptions,
            ActiveSubscriptions = _stats.ActiveSubscriptions,
            EventCounts = new Dictionary<string, int>(_stats.EventCounts),
            SubscriptionCounts = new Dictionary<string, int>(_stats.SubscriptionCounts),
            LastEventTime = _stats.LastEventTime,
            TotalProcessingTime = _stats.TotalProcessingTime,
            ErrorCount = _stats.ErrorCount
        };
        
        /// <summary>
        /// Get recent errors from the event bus
        /// </summary>
        public IEnumerable<Exception> RecentErrors => _recentErrors.ToArray();
        
        #endregion
    }
    
    /// <summary>
    /// Internal implementation of event subscription
    /// </summary>
    internal class EventSubscriptionInternal : IEventSubscription
    {
        private volatile bool _isActive = true;
        private int _invocationCount = 0;
        
        public string Id { get; }
        public Type EventType { get; }
        public string EventName { get; }
        public DateTime CreatedAt { get; }
        public bool IsActive => _isActive;
        public int InvocationCount => _invocationCount;
        
        public Action<GameEvent> SyncHandler { get; }
        public Func<GameEvent, Task> AsyncHandler { get; }
        
        public EventSubscriptionInternal(Type eventType, string eventName, Action<GameEvent> syncHandler, Func<GameEvent, Task> asyncHandler)
        {
            Id = Guid.NewGuid().ToString();
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            EventName = eventName;
            CreatedAt = DateTime.UtcNow;
            SyncHandler = syncHandler;
            AsyncHandler = asyncHandler;
        }
        
        public void IncrementInvocations()
        {
            System.Threading.Interlocked.Increment(ref _invocationCount);
        }
        
        public void Unsubscribe()
        {
            _isActive = false;
        }
        
        public void Dispose()
        {
            Unsubscribe();
        }
    }
}