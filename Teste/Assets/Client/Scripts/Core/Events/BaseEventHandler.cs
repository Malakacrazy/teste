using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using L5RGame.Events;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Base implementation for event handlers providing common functionality
    /// </summary>
    public abstract class BaseEventHandler : IEventHandler, IStatisticsEventHandler, IDisposable
    {
        #region Protected Fields
        
        protected IEventBus _eventBus;
        protected readonly List<IEventSubscription> _subscriptions = new List<IEventSubscription>();
        protected volatile bool _isEnabled = true;
        protected volatile bool _isDisposed = false;
        
        // Statistics
        protected int _eventsProcessed = 0;
        protected int _errorCount = 0;
        protected DateTime? _lastProcessedTime = null;
        
        #endregion
        
        #region Properties
        
        public string HandlerId { get; }
        public abstract string HandlerName { get; }
        
        public bool IsEnabled
        {
            get => _isEnabled && !_isDisposed;
            set => _isEnabled = value;
        }
        
        public int EventsProcessed => _eventsProcessed;
        public int ErrorCount => _errorCount;
        public DateTime? LastProcessedTime => _lastProcessedTime;
        
        #endregion
        
        #region Constructor
        
        protected BaseEventHandler()
        {
            HandlerId = Guid.NewGuid().ToString();
        }
        
        #endregion
        
        #region IEventHandler Implementation
        
        public virtual void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            try
            {
                SubscribeToEvents();
                OnInitialized();
                
                Debug.Log($"✅ {HandlerName} event handler initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize {HandlerName} event handler: {ex.Message}");
                throw;
            }
        }
        
        public virtual void Shutdown()
        {
            if (_isDisposed)
                return;
                
            try
            {
                OnShutdown();
                UnsubscribeFromAll();
                
                Debug.Log($"🔌 {HandlerName} event handler shut down");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error shutting down {HandlerName} event handler: {ex.Message}");
            }
            finally
            {
                _isDisposed = true;
            }
        }
        
        public virtual object GetDebugInfo()
        {
            return new
            {
                handlerId = HandlerId,
                handlerName = HandlerName,
                isEnabled = IsEnabled,
                isDisposed = _isDisposed,
                eventsProcessed = EventsProcessed,
                errorCount = ErrorCount,
                lastProcessedTime = LastProcessedTime?.ToString("HH:mm:ss.fff") ?? "Never",
                subscriptionCount = _subscriptions.Count,
                subscriptions = _subscriptions.ConvertAll(s => new { id = s.Id, eventType = s.EventType.Name, isActive = s.IsActive })
            };
        }
        
        #endregion
        
        #region IStatisticsEventHandler Implementation
        
        public virtual void ResetStatistics()
        {
            _eventsProcessed = 0;
            _errorCount = 0;
            _lastProcessedTime = null;
        }
        
        #endregion
        
        #region Protected Abstract Methods
        
        /// <summary>
        /// Subscribe to the events this handler is interested in
        /// </summary>
        protected abstract void SubscribeToEvents();
        
        #endregion
        
        #region Protected Virtual Methods
        
        /// <summary>
        /// Called after the handler has been initialized
        /// </summary>
        protected virtual void OnInitialized()
        {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Called before the handler is shut down
        /// </summary>
        protected virtual void OnShutdown()
        {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Called when an error occurs during event processing
        /// </summary>
        /// <param name="ex">Exception that occurred</param>
        /// <param name="eventInfo">Information about the event being processed</param>
        protected virtual void OnError(Exception ex, string eventInfo)
        {
            System.Threading.Interlocked.Increment(ref _errorCount);
            Debug.LogError($"❌ Error in {HandlerName} processing {eventInfo}: {ex.Message}\n{ex.StackTrace}");
        }
        
        /// <summary>
        /// Check if the handler should process the given event
        /// </summary>
        /// <param name="gameEvent">Event to check</param>
        /// <returns>True if should process</returns>
        protected virtual bool ShouldProcessEvent(GameEvent gameEvent)
        {
            return IsEnabled && gameEvent != null && !gameEvent.IsCancelled();
        }
        
        #endregion
        
        #region Protected Helper Methods
        
        /// <summary>
        /// Subscribe to a specific event type with error handling
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Event handler function</param>
        protected void Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            if (_eventBus == null)
                throw new InvalidOperationException("Event bus not initialized");
            
            var subscription = _eventBus.Subscribe<T>(evt =>
            {
                try
                {
                    if (ShouldProcessEvent(evt))
                    {
                        handler(evt);
                        RecordEventProcessed();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex, typeof(T).Name);
                }
            });
            
            _subscriptions.Add(subscription);
        }
        
        /// <summary>
        /// Subscribe to a specific event type with async handler and error handling
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Async event handler function</param>
        protected void SubscribeAsync<T>(Func<T, Task> handler) where T : GameEvent
        {
            if (_eventBus == null)
                throw new InvalidOperationException("Event bus not initialized");
            
            var subscription = _eventBus.Subscribe<T>(async evt =>
            {
                try
                {
                    if (ShouldProcessEvent(evt))
                    {
                        await handler(evt);
                        RecordEventProcessed();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex, $"{typeof(T).Name} (async)");
                }
            });
            
            _subscriptions.Add(subscription);
        }
        
        /// <summary>
        /// Subscribe to events by name with error handling
        /// </summary>
        /// <param name="eventName">Name of event</param>
        /// <param name="handler">Event handler function</param>
        protected void SubscribeByName(string eventName, Action<GameEvent> handler)
        {
            if (_eventBus == null)
                throw new InvalidOperationException("Event bus not initialized");
            
            var subscription = _eventBus.Subscribe(eventName, evt =>
            {
                try
                {
                    if (ShouldProcessEvent(evt))
                    {
                        handler(evt);
                        RecordEventProcessed();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex, eventName);
                }
            });
            
            _subscriptions.Add(subscription);
        }
        
        /// <summary>
        /// Subscribe to all events with error handling
        /// </summary>
        /// <param name="handler">Event handler function</param>
        protected void SubscribeToAll(Action<GameEvent> handler)
        {
            if (_eventBus == null)
                throw new InvalidOperationException("Event bus not initialized");
            
            var subscription = _eventBus.SubscribeToAll(evt =>
            {
                try
                {
                    if (ShouldProcessEvent(evt))
                    {
                        handler(evt);
                        RecordEventProcessed();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex, "All Events");
                }
            });
            
            _subscriptions.Add(subscription);
        }
        
        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        protected void UnsubscribeFromAll()
        {
            foreach (var subscription in _subscriptions)
            {
                try
                {
                    subscription?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Error disposing subscription: {ex.Message}");
                }
            }
            _subscriptions.Clear();
        }
        
        /// <summary>
        /// Record that an event was processed
        /// </summary>
        private void RecordEventProcessed()
        {
            System.Threading.Interlocked.Increment(ref _eventsProcessed);
            _lastProcessedTime = DateTime.UtcNow;
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            Shutdown();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Base class for event handlers that handle a specific event type
    /// </summary>
    /// <typeparam name="T">Event type to handle</typeparam>
    public abstract class BaseEventHandler<T> : BaseEventHandler, IEventHandler<T> where T : GameEvent
    {
        protected override void SubscribeToEvents()
        {
            Subscribe<T>(Handle);
        }
        
        public abstract void Handle(T gameEvent);
    }
    
    /// <summary>
    /// Base class for asynchronous event handlers that handle a specific event type
    /// </summary>
    /// <typeparam name="T">Event type to handle</typeparam>
    public abstract class BaseAsyncEventHandler<T> : BaseEventHandler, IAsyncEventHandler<T> where T : GameEvent
    {
        protected override void SubscribeToEvents()
        {
            SubscribeAsync<T>(HandleAsync);
        }
        
        public abstract Task HandleAsync(T gameEvent);
    }
    
    /// <summary>
    /// Base class for event handlers that handle multiple event types
    /// </summary>
    public abstract class BaseMultiEventHandler : BaseEventHandler, IMultiEventHandler
    {
        protected override void SubscribeToEvents()
        {
            var eventTypes = GetHandledEventTypes();
            foreach (var eventType in eventTypes)
            {
                // Subscribe using reflection to maintain type safety
                var subscribeMethod = typeof(BaseEventHandler).GetMethod("Subscribe", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var genericSubscribe = subscribeMethod.MakeGenericMethod(eventType);
                
                // Create a delegate that calls HandleEvent
                var delegateType = typeof(Action<>).MakeGenericType(eventType);
                var handleMethod = typeof(BaseMultiEventHandler).GetMethod("HandleEventTyped", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var genericHandleMethod = handleMethod.MakeGenericMethod(eventType);
                var handler = Delegate.CreateDelegate(delegateType, this, genericHandleMethod);
                
                genericSubscribe.Invoke(this, new object[] { handler });
            }
        }
        
        // Helper method for handling typed events
        private void HandleEventTyped<TEvent>(TEvent gameEvent) where TEvent : GameEvent
        {
            HandleEvent(gameEvent);
        }
        
        public abstract void HandleEvent(GameEvent gameEvent);
        public abstract Type[] GetHandledEventTypes();
    }
}