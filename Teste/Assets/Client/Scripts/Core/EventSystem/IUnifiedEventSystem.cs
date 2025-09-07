using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using L5RGame.Events;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Timing windows for L5R card game event processing
    /// Represents the sequential priority levels in L5R timing
    /// </summary>
    public enum TimingWindow
    {
        /// <summary>
        /// Prevention/replacement effects that would stop an event
        /// </summary>
        WouldInterrupt = 0,
        
        /// <summary>
        /// Mandatory interrupts that must be processed
        /// </summary>
        ForcedInterrupt = 1,
        
        /// <summary>
        /// Optional interrupts that players can choose to use
        /// </summary>
        Interrupt = 2,
        
        /// <summary>
        /// Core event execution and resolution
        /// </summary>
        Handler = 3,
        
        /// <summary>
        /// Mandatory reactions that trigger after event resolution
        /// </summary>
        ForcedReaction = 4,
        
        /// <summary>
        /// Optional reactions that players can choose to use
        /// </summary>
        Reaction = 5
    }
    
    /// <summary>
    /// Context information for timing window processing
    /// </summary>
    public class TimingContext
    {
        public string ContextId { get; set; } = Guid.NewGuid().ToString();
        public TimingWindow CurrentWindow { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<GameEvent> ProcessedEvents { get; set; } = new List<GameEvent>();
        public List<GameEvent> ContingentEvents { get; set; } = new List<GameEvent>();
        public bool AllowCancellations { get; set; } = true;
        public bool AllowContingentEvents { get; set; } = true;
        
        public override string ToString()
        {
            return $"TimingContext[{ContextId.Substring(0, 8)}] - {CurrentWindow} - {ProcessedEvents.Count} events";
        }
    }
    
    /// <summary>
    /// Interface for handlers that are aware of L5R timing windows
    /// </summary>
    public interface ITimingAwareHandler
    {
        /// <summary>
        /// Timing windows this handler supports
        /// </summary>
        TimingWindow[] SupportedTimingWindows { get; }
        
        /// <summary>
        /// Priority within a timing window (higher = processed first)
        /// </summary>
        int TimingPriority { get; }
        
        /// <summary>
        /// Check if this handler should process the event at the given timing
        /// </summary>
        /// <param name="gameEvent">Event to process</param>
        /// <param name="window">Current timing window</param>
        /// <returns>True if should process</returns>
        bool ShouldHandleAtTiming(GameEvent gameEvent, TimingWindow window);
        
        /// <summary>
        /// Process event at specific timing window
        /// </summary>
        /// <param name="gameEvent">Event to process</param>
        /// <param name="window">Current timing window</param>
        /// <param name="context">Timing context</param>
        Task HandleAtTimingAsync(GameEvent gameEvent, TimingWindow window, TimingContext context);
    }
    
    /// <summary>
    /// Unified event system that combines modern EventBus with L5R timing rules
    /// </summary>
    public interface IUnifiedEventSystem : IEventBus
    {
        #region Timing-Specific Methods
        
        /// <summary>
        /// Publish event at a specific timing window
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventInstance">Event to publish</param>
        /// <param name="window">Timing window</param>
        void PublishAtTiming<T>(T eventInstance, TimingWindow window) where T : GameEvent;
        
        /// <summary>
        /// Process events through the complete L5R timing sequence
        /// </summary>
        /// <param name="events">Events to process</param>
        /// <returns>Timing context with processing results</returns>
        Task<TimingContext> ProcessTimingSequenceAsync(List<GameEvent> events);
        
        /// <summary>
        /// Process events synchronously through timing sequence
        /// </summary>
        /// <param name="events">Events to process</param>
        /// <returns>Timing context with processing results</returns>
        TimingContext ProcessTimingSequence(List<GameEvent> events);
        
        #endregion
        
        #region Event Lifecycle Management
        
        /// <summary>
        /// Cancel an event with reason
        /// </summary>
        /// <param name="gameEvent">Event to cancel</param>
        /// <param name="reason">Cancellation reason</param>
        void CancelEvent(GameEvent gameEvent, string reason);
        
        /// <summary>
        /// Add contingent events that spawn from a parent event
        /// </summary>
        /// <param name="parentEvent">Parent event</param>
        /// <param name="contingentEvents">Events that spawn from parent</param>
        void AddContingentEvents(GameEvent parentEvent, IEnumerable<GameEvent> contingentEvents);
        
        /// <summary>
        /// Replace an event with a different event
        /// </summary>
        /// <param name="originalEvent">Event to replace</param>
        /// <param name="replacementEvent">Replacement event</param>
        void ReplaceEvent(GameEvent originalEvent, GameEvent replacementEvent);
        
        #endregion
        
        #region Ability Management
        
        /// <summary>
        /// Queue a "then" ability to execute after event resolution
        /// </summary>
        /// <param name="ability">Ability to queue</param>
        /// <param name="condition">Condition that must be met</param>
        void QueueThenAbility(BaseAbility ability, Func<bool> condition);
        
        /// <summary>
        /// Process all queued "then" abilities
        /// </summary>
        Task ProcessThenAbilitiesAsync();
        
        #endregion
        
        #region Timing Context Management
        
        /// <summary>
        /// Push a new timing context onto the stack
        /// </summary>
        /// <param name="context">Context to push</param>
        void PushTimingContext(TimingContext context);
        
        /// <summary>
        /// Pop the current timing context from the stack
        /// </summary>
        /// <returns>Popped context</returns>
        TimingContext PopTimingContext();
        
        /// <summary>
        /// Get the current timing context
        /// </summary>
        TimingContext CurrentContext { get; }
        
        #endregion
        
        #region Legacy Compatibility
        
        /// <summary>
        /// Emit event to legacy event system for backward compatibility
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="eventData">Event data</param>
        void EmitLegacyEvent(string eventName, Dictionary<string, object> eventData);
        
        /// <summary>
        /// Process EventWindow-style events
        /// </summary>
        /// <param name="windowEvents">Events from EventWindow</param>
        Task ProcessLegacyEventWindowAsync(List<GameEvent> windowEvents);
        
        #endregion
        
        #region Advanced Features
        
        /// <summary>
        /// Subscribe to events at specific timing windows
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Handler function</param>
        /// <param name="window">Timing window</param>
        /// <param name="priority">Priority within window</param>
        /// <returns>Subscription object</returns>
        IEventSubscription SubscribeAtTiming<T>(Func<T, TimingWindow, TimingContext, Task> handler, 
            TimingWindow window, int priority = 0) where T : GameEvent;
        
        /// <summary>
        /// Get events currently being processed
        /// </summary>
        IReadOnlyList<GameEvent> CurrentEvents { get; }
        
        /// <summary>
        /// Get timing window currently being processed
        /// </summary>
        TimingWindow? CurrentTimingWindow { get; }
        
        /// <summary>
        /// Check if timing sequence is currently processing
        /// </summary>
        bool IsProcessingTimingSequence { get; }
        
        #endregion
    }
    
    /// <summary>
    /// Event published when a timing window begins
    /// </summary>
    public class TimingWindowStartedEvent : GameEvent
    {
        public TimingWindow Window { get; private set; }
        public TimingContext Context { get; private set; }
        public List<GameEvent> EventsInWindow { get; private set; }
        
        public TimingWindowStartedEvent(Game game, Player triggeredBy, TimingWindow window, 
            TimingContext context, List<GameEvent> events, object source = null)
            : base(game, triggeredBy, source)
        {
            Window = window;
            Context = context;
            EventsInWindow = new List<GameEvent>(events);
            
            AddEventData("timing_window", window.ToString());
            AddEventData("context_id", context.ContextId);
            AddEventData("events_count", events.Count);
        }
    }
    
    /// <summary>
    /// Event published when a timing window completes
    /// </summary>
    public class TimingWindowCompletedEvent : GameEvent
    {
        public TimingWindow Window { get; private set; }
        public TimingContext Context { get; private set; }
        public int ProcessedEventsCount { get; private set; }
        public int CancelledEventsCount { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }
        
        public TimingWindowCompletedEvent(Game game, Player triggeredBy, TimingWindow window, 
            TimingContext context, int processedCount, int cancelledCount, TimeSpan processingTime, object source = null)
            : base(game, triggeredBy, source)
        {
            Window = window;
            Context = context;
            ProcessedEventsCount = processedCount;
            CancelledEventsCount = cancelledCount;
            ProcessingTime = processingTime;
            
            AddEventData("timing_window", window.ToString());
            AddEventData("context_id", context.ContextId);
            AddEventData("processed_count", processedCount);
            AddEventData("cancelled_count", cancelledCount);
            AddEventData("processing_time_ms", processingTime.TotalMilliseconds);
        }
    }
}