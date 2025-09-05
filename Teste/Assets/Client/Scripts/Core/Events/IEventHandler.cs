using System;
using System.Threading.Tasks;
using L5RGame.Events;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Base interface for all event handlers in the game.
    /// Event handlers are decoupled components that react to game events.
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Unique identifier for this handler
        /// </summary>
        string HandlerId { get; }
        
        /// <summary>
        /// Display name for this handler
        /// </summary>
        string HandlerName { get; }
        
        /// <summary>
        /// Whether this handler is currently enabled
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Initialize the handler with the event bus
        /// </summary>
        /// <param name="eventBus">Event bus to subscribe to</param>
        void Initialize(IEventBus eventBus);
        
        /// <summary>
        /// Shutdown the handler and clean up resources
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Get debug information about this handler
        /// </summary>
        /// <returns>Debug information</returns>
        object GetDebugInfo();
    }
    
    /// <summary>
    /// Base interface for synchronous event handlers
    /// </summary>
    /// <typeparam name="T">Event type to handle</typeparam>
    public interface IEventHandler<in T> : IEventHandler where T : GameEvent
    {
        /// <summary>
        /// Handle the event synchronously
        /// </summary>
        /// <param name="gameEvent">Event to handle</param>
        void Handle(T gameEvent);
    }
    
    /// <summary>
    /// Base interface for asynchronous event handlers
    /// </summary>
    /// <typeparam name="T">Event type to handle</typeparam>
    public interface IAsyncEventHandler<in T> : IEventHandler where T : GameEvent
    {
        /// <summary>
        /// Handle the event asynchronously
        /// </summary>
        /// <param name="gameEvent">Event to handle</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleAsync(T gameEvent);
    }
    
    /// <summary>
    /// Interface for event handlers that handle multiple event types
    /// </summary>
    public interface IMultiEventHandler : IEventHandler
    {
        /// <summary>
        /// Handle any game event
        /// </summary>
        /// <param name="gameEvent">Event to handle</param>
        void HandleEvent(GameEvent gameEvent);
        
        /// <summary>
        /// Get the event types this handler is interested in
        /// </summary>
        /// <returns>Array of event types</returns>
        Type[] GetHandledEventTypes();
    }
    
    /// <summary>
    /// Interface for event handlers that need priority ordering
    /// </summary>
    public interface IPriorityEventHandler : IEventHandler
    {
        /// <summary>
        /// Priority of this handler (lower numbers = higher priority)
        /// </summary>
        int Priority { get; }
    }
    
    /// <summary>
    /// Interface for event handlers that can filter events before processing
    /// </summary>
    public interface IFilteringEventHandler : IEventHandler
    {
        /// <summary>
        /// Check if this handler should process the given event
        /// </summary>
        /// <param name="gameEvent">Event to check</param>
        /// <returns>True if the handler should process this event</returns>
        bool ShouldHandle(GameEvent gameEvent);
    }
    
    /// <summary>
    /// Interface for event handlers that maintain statistics
    /// </summary>
    public interface IStatisticsEventHandler : IEventHandler
    {
        /// <summary>
        /// Number of events this handler has processed
        /// </summary>
        int EventsProcessed { get; }
        
        /// <summary>
        /// Number of errors this handler has encountered
        /// </summary>
        int ErrorCount { get; }
        
        /// <summary>
        /// Last time this handler processed an event
        /// </summary>
        DateTime? LastProcessedTime { get; }
        
        /// <summary>
        /// Reset statistics
        /// </summary>
        void ResetStatistics();
    }
}