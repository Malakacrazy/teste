using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Interface for the event bus system.
    /// Provides pub/sub functionality for game events with type safety and async support.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribe to a specific event type
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to</typeparam>
        /// <param name="handler">Event handler function</param>
        /// <returns>Subscription token for unsubscribing</returns>
        IEventSubscription Subscribe<T>(Action<T> handler) where T : L5RGame.GameEvent;
        
        /// <summary>
        /// Subscribe to a specific event type with async handler
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to</typeparam>
        /// <param name="handler">Async event handler function</param>
        /// <returns>Subscription token for unsubscribing</returns>
        IEventSubscription Subscribe<T>(Func<T, Task> handler) where T : L5RGame.GameEvent;
        
        /// <summary>
        /// Subscribe to events by name (for dynamic event handling)
        /// </summary>
        /// <param name="eventName">Name of the event type</param>
        /// <param name="handler">Event handler function</param>
        /// <returns>Subscription token for unsubscribing</returns>
        IEventSubscription Subscribe(string eventName, Action<L5RGame.GameEvent> handler);
        
        /// <summary>
        /// Subscribe to all events (useful for logging/debugging)
        /// </summary>
        /// <param name="handler">Event handler that receives all events</param>
        /// <returns>Subscription token for unsubscribing</returns>
        IEventSubscription SubscribeToAll(Action<L5RGame.GameEvent> handler);
        
        /// <summary>
        /// Unsubscribe from an event using the subscription token
        /// </summary>
        /// <param name="subscription">Subscription token to remove</param>
        void Unsubscribe(IEventSubscription subscription);
        
        /// <summary>
        /// Publish an event synchronously
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventInstance">Event instance to publish</param>
        void Publish<T>(T eventInstance) where T : L5RGame.GameEvent;
        
        /// <summary>
        /// Publish an event asynchronously
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventInstance">Event instance to publish</param>
        /// <returns>Task representing the async operation</returns>
        Task PublishAsync<T>(T eventInstance) where T : L5RGame.GameEvent;
        
        /// <summary>
        /// Clear all subscriptions (for cleanup)
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// Get count of active subscriptions
        /// </summary>
        /// <returns>Number of active subscriptions</returns>
        int GetSubscriptionCount();
        
        /// <summary>
        /// Get count of subscriptions for a specific event type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>Number of subscriptions for this event type</returns>
        int GetSubscriptionCount<T>() where T : L5RGame.GameEvent;
        
        /// <summary>
        /// Enable or disable the event bus
        /// </summary>
        /// <param name="enabled">Whether the event bus should process events</param>
        void SetEnabled(bool enabled);
        
        /// <summary>
        /// Check if event bus is enabled
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Get debug information about the event bus state
        /// </summary>
        /// <returns>Debug information</returns>
        object GetDebugInfo();
    }
    
    /// <summary>
    /// Represents a subscription to an event.
    /// Used for unsubscribing and tracking subscription state.
    /// </summary>
    public interface IEventSubscription : IDisposable
    {
        /// <summary>
        /// Unique identifier for this subscription
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Event type this subscription is for
        /// </summary>
        Type EventType { get; }
        
        /// <summary>
        /// Event name this subscription is for (for named subscriptions)
        /// </summary>
        string EventName { get; }
        
        /// <summary>
        /// Whether this subscription is still active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// When this subscription was created
        /// </summary>
        DateTime CreatedAt { get; }
        
        /// <summary>
        /// Number of times this handler has been invoked
        /// </summary>
        int InvocationCount { get; }
        
        /// <summary>
        /// Unsubscribe this subscription
        /// </summary>
        void Unsubscribe();
    }
    
    /// <summary>
    /// Exception thrown by the event system
    /// </summary>
    public class EventBusException : Exception
    {
        public EventBusException(string message) : base(message) { }
        public EventBusException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Statistics about event bus performance
    /// </summary>
    public class EventBusStats
    {
        public int TotalEventsPublished { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public Dictionary<string, int> EventCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SubscriptionCounts { get; set; } = new Dictionary<string, int>();
        public DateTime LastEventTime { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public int ErrorCount { get; set; }
        
        public override string ToString()
        {
            return $"Events: {TotalEventsPublished}, Subs: {ActiveSubscriptions}/{TotalSubscriptions}, Errors: {ErrorCount}";
        }
    }
}