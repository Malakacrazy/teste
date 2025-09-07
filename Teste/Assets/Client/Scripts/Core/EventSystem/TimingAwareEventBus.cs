using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using L5RGame.Events;
using UnityEngine;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Event bus that understands L5R timing windows and provides unified event processing
    /// Extends GameEventBus with timing-aware capabilities
    /// </summary>
    public class TimingAwareEventBus : GameEventBus, IUnifiedEventSystem
    {
        #region Private Fields
        
        private readonly Stack<TimingContext> _contextStack = new Stack<TimingContext>();
        private readonly List<(BaseAbility ability, Func<bool> condition)> _thenAbilities = 
            new List<(BaseAbility ability, Func<bool> condition)>();
        private readonly ConcurrentDictionary<TimingWindow, ConcurrentBag<ITimingAwareHandler>> _timingHandlers = 
            new ConcurrentDictionary<TimingWindow, ConcurrentBag<ITimingAwareHandler>>();
        private readonly object _timingLock = new object();
        
        // Current processing state
        private volatile bool _isProcessingTimingSequence = false;
        private TimingWindow? _currentTimingWindow = null;
        private readonly List<GameEvent> _currentEvents = new List<GameEvent>();
        
        // Legacy compatibility
        private Action<string, Dictionary<string, object>> _legacyEventEmitter;
        
        #endregion
        
        #region Constructor
        
        public TimingAwareEventBus() : base()
        {
            InitializeTimingAwareEventBus();
        }
        
        public TimingAwareEventBus(bool enableDebugLogging, bool enablePerformanceMonitoring) 
            : base(enableDebugLogging, enablePerformanceMonitoring)
        {
            InitializeTimingAwareEventBus();
        }
        
        private void InitializeTimingAwareEventBus()
        {
            UnityEngine.Debug.Log("🕰️ TimingAwareEventBus initialized with L5R timing support");
            
            // Initialize timing window handlers
            foreach (TimingWindow window in Enum.GetValues(typeof(TimingWindow)))
            {
                _timingHandlers.TryAdd(window, new ConcurrentBag<ITimingAwareHandler>());
            }
        }
        
        #endregion
        
        #region IUnifiedEventSystem Implementation
        
        #region Timing-Specific Methods
        
        public void PublishAtTiming<T>(T eventInstance, TimingWindow window) where T : GameEvent
        {
            if (eventInstance == null) return;
            
            try
            {
                // Add timing metadata to event
                eventInstance.AddEventData("timing_window", window.ToString());
                eventInstance.AddEventData("timing_priority", GetTimingPriority(window));
                
                // Publish to regular event bus first
                Publish(eventInstance);
                
                // Then process timing-specific handlers
                PublishToTimingHandlers(eventInstance, window);
                
                // Update statistics
                lock (_lockObject)
                {
                    _stats.TotalEventsPublished++;
                    var windowKey = $"timing_{window}";
                    _stats.EventCounts[windowKey] = _stats.EventCounts.GetValueOrDefault(windowKey, 0) + 1;
                }
                
                UnityEngine.Debug.Log($"⏰ Published {eventInstance.GetType().Name} at {window} timing");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to publish event at timing {window}: {ex.Message}");
                
                lock (_lockObject)
                {
                    _stats.ErrorCount++;
                }
            }
        }
        
        public async Task<TimingContext> ProcessTimingSequenceAsync(List<GameEvent> events)
        {
            if (events == null || events.Count == 0)
                return new TimingContext();
            
            var context = new TimingContext();
            context.StartTime = DateTime.UtcNow;
            
            try
            {
                lock (_timingLock)
                {
                    _isProcessingTimingSequence = true;
                    _currentEvents.Clear();
                    _currentEvents.AddRange(events);
                }
                
                PushTimingContext(context);
                
                // Process through all timing windows in sequence
                var timingWindows = Enum.GetValues(typeof(TimingWindow)).Cast<TimingWindow>().OrderBy(w => (int)w);
                
                foreach (var window in timingWindows)
                {
                    await ProcessTimingWindowAsync(events, window, context);
                    
                    // Handle cancellations and contingent events after each window
                    ProcessCancellations(context);
                    await ProcessContingentEventsAsync(context);
                    
                    // Break early if all events are cancelled
                    if (events.All(e => e.cancelled))
                        break;
                }
                
                // Process "then" abilities after all windows complete
                await ProcessThenAbilitiesAsync();
                
                PopTimingContext();
                
                UnityEngine.Debug.Log($"⏰ Completed timing sequence for {events.Count} events in {DateTime.UtcNow - context.StartTime:F2} seconds");
                
                return context;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to process timing sequence: {ex.Message}");
                throw;
            }
            finally
            {
                lock (_timingLock)
                {
                    _isProcessingTimingSequence = false;
                    _currentTimingWindow = null;
                    _currentEvents.Clear();
                }
            }
        }
        
        public TimingContext ProcessTimingSequence(List<GameEvent> events)
        {
            return ProcessTimingSequenceAsync(events).GetAwaiter().GetResult();
        }
        
        #endregion
        
        #region Event Lifecycle Management
        
        public void CancelEvent(GameEvent gameEvent, string reason)
        {
            if (gameEvent == null) return;
            
            try
            {
                gameEvent.cancelled = true;
                gameEvent.AddEventData("cancellation_reason", reason);
                gameEvent.AddEventData("cancelled_at", DateTime.UtcNow);
                
                // Publish cancellation event
                var cancellationEvent = new EventCancelledEvent(
                    gameEvent.GetProperty("game") as Game,
                    gameEvent.TriggeredBy,
                    gameEvent,
                    reason,
                    this
                );
                
                Publish(cancellationEvent);
                
                UnityEngine.Debug.Log($"🚫 Cancelled event {gameEvent.GetType().Name}: {reason}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to cancel event: {ex.Message}");
            }
        }
        
        public void AddContingentEvents(GameEvent parentEvent, IEnumerable<GameEvent> contingentEvents)
        {
            if (parentEvent == null || contingentEvents == null) return;
            
            try
            {
                var context = CurrentContext;
                if (context?.AllowContingentEvents == true)
                {
                    foreach (var contingentEvent in contingentEvents)
                    {
                        contingentEvent.AddEventData("parent_event", parentEvent.EventId);
                        contingentEvent.AddEventData("contingent_from", parentEvent.GetType().Name);
                        context.ContingentEvents.Add(contingentEvent);
                    }
                    
                    UnityEngine.Debug.Log($"📎 Added {contingentEvents.Count()} contingent events from {parentEvent.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to add contingent events: {ex.Message}");
            }
        }
        
        public void ReplaceEvent(GameEvent originalEvent, GameEvent replacementEvent)
        {
            if (originalEvent == null || replacementEvent == null) return;
            
            try
            {
                originalEvent.cancelled = true;
                originalEvent.AddEventData("replaced_by", replacementEvent.EventId);
                originalEvent.AddEventData("replacement_reason", "event_replacement");
                
                replacementEvent.AddEventData("replaces_event", originalEvent.EventId);
                replacementEvent.AddEventData("original_event_type", originalEvent.GetType().Name);
                
                // Add replacement to current context
                var context = CurrentContext;
                context?.ContingentEvents.Add(replacementEvent);
                
                UnityEngine.Debug.Log($"🔄 Replaced {originalEvent.GetType().Name} with {replacementEvent.GetType().Name}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to replace event: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Ability Management
        
        public void QueueThenAbility(BaseAbility ability, Func<bool> condition)
        {
            if (ability == null) return;
            
            try
            {
                lock (_thenAbilities)
                {
                    _thenAbilities.Add((ability, condition));
                }
                
                UnityEngine.Debug.Log($"⏳ Queued 'then' ability: {ability.Title}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to queue 'then' ability: {ex.Message}");
            }
        }
        
        public async Task ProcessThenAbilitiesAsync()
        {
            List<(BaseAbility ability, Func<bool> condition)> abilitiesToProcess;
            
            lock (_thenAbilities)
            {
                abilitiesToProcess = new List<(BaseAbility, Func<bool>)>(_thenAbilities);
                _thenAbilities.Clear();
            }
            
            if (abilitiesToProcess.Count == 0) return;
            
            try
            {
                foreach (var (ability, condition) in abilitiesToProcess)
                {
                    try
                    {
                        if (condition == null || condition())
                        {
                            // Create ability context and execute
                            var context = new AbilityContext();
                            context.source = ability.card;
                            context.player = ability.card?.owner;
                            ability.ExecuteAbility(context);
                            
                            UnityEngine.Debug.Log($"✅ Executed 'then' ability: {ability.Title}");
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"❌ Skipped 'then' ability (condition failed): {ability.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"❌ Failed to execute 'then' ability {ability.Title}: {ex.Message}");
                    }
                }
                
                UnityEngine.Debug.Log($"⏳ Processed {abilitiesToProcess.Count} 'then' abilities");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to process 'then' abilities: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Timing Context Management
        
        public void PushTimingContext(TimingContext context)
        {
            if (context == null) return;
            
            lock (_timingLock)
            {
                _contextStack.Push(context);
                UnityEngine.Debug.Log($"📥 Pushed timing context: {context}");
            }
        }
        
        public TimingContext PopTimingContext()
        {
            lock (_timingLock)
            {
                if (_contextStack.Count > 0)
                {
                    var context = _contextStack.Pop();
                    UnityEngine.Debug.Log($"📤 Popped timing context: {context}");
                    return context;
                }
                
                return null;
            }
        }
        
        public TimingContext CurrentContext
        {
            get
            {
                lock (_timingLock)
                {
                    return _contextStack.Count > 0 ? _contextStack.Peek() : null;
                }
            }
        }
        
        #endregion
        
        #region Legacy Compatibility
        
        public void EmitLegacyEvent(string eventName, Dictionary<string, object> eventData)
        {
            try
            {
                _legacyEventEmitter?.Invoke(eventName, eventData);
                UnityEngine.Debug.Log($"🔗 Emitted legacy event: {eventName}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to emit legacy event {eventName}: {ex.Message}");
            }
        }
        
        public async Task ProcessLegacyEventWindowAsync(List<GameEvent> windowEvents)
        {
            if (windowEvents == null || windowEvents.Count == 0) return;
            
            try
            {
                // Convert EventWindow events to timing sequence
                var timingEvents = windowEvents.Where(e => !e.cancelled).ToList();
                await ProcessTimingSequenceAsync(timingEvents);
                
                // Emit legacy events for backward compatibility
                foreach (var evt in timingEvents.Where(e => !e.cancelled))
                {
                    EmitLegacyEvent(evt.name, evt.GetData());
                    EmitLegacyEvent(evt.name + ":OtherEffects", evt.GetData());
                }
                
                UnityEngine.Debug.Log($"🔄 Processed {windowEvents.Count} legacy EventWindow events");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to process legacy EventWindow: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Set the legacy event emitter for backward compatibility
        /// </summary>
        /// <param name="emitter">Legacy event emitter function</param>
        public void SetLegacyEventEmitter(Action<string, Dictionary<string, object>> emitter)
        {
            _legacyEventEmitter = emitter;
        }
        
        #endregion
        
        #region Advanced Features
        
        public IEventSubscription SubscribeAtTiming<T>(Func<T, TimingWindow, TimingContext, Task> handler, 
            TimingWindow window, int priority = 0) where T : GameEvent
        {
            if (handler == null) return null;
            
            try
            {
                // Create a timing-aware handler wrapper
                var timingHandler = new TimingSpecificHandler<T>(handler, window, priority);
                
                // Add to timing-specific handlers
                if (_timingHandlers.TryGetValue(window, out var handlers))
                {
                    handlers.Add(timingHandler);
                }
                
                // Create subscription
                var subscription = new EventSubscriptionInternal(
                    typeof(T),
                    $"{typeof(T).Name}_Timing_{window}",
                    evt => timingHandler.HandleAtTimingAsync(evt, window, CurrentContext),
                    null
                );
                
                UnityEngine.Debug.Log($"🎯 Subscribed to {typeof(T).Name} at {window} timing (priority: {priority})");
                
                return subscription;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to subscribe at timing {window}: {ex.Message}");
                return null;
            }
        }
        
        public IReadOnlyList<GameEvent> CurrentEvents
        {
            get
            {
                lock (_timingLock)
                {
                    return _currentEvents.AsReadOnly();
                }
            }
        }
        
        public TimingWindow? CurrentTimingWindow
        {
            get
            {
                lock (_timingLock)
                {
                    return _currentTimingWindow;
                }
            }
        }
        
        public bool IsProcessingTimingSequence
        {
            get
            {
                lock (_timingLock)
                {
                    return _isProcessingTimingSequence;
                }
            }
        }
        
        #endregion
        
        #endregion
        
        #region Private Helper Methods
        
        private async Task ProcessTimingWindowAsync(List<GameEvent> events, TimingWindow window, TimingContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var eventsForWindow = events.Where(e => !e.cancelled).ToList();
            
            if (eventsForWindow.Count == 0) return;
            
            try
            {
                lock (_timingLock)
                {
                    _currentTimingWindow = window;
                    context.CurrentWindow = window;
                }
                
                // Publish timing window started event
                var windowStartEvent = new TimingWindowStartedEvent(
                    eventsForWindow[0].GetProperty("game") as Game,
                    eventsForWindow[0].TriggeredBy,
                    window,
                    context,
                    eventsForWindow,
                    this
                );
                Publish(windowStartEvent);
                
                // Process events at this timing window
                foreach (var evt in eventsForWindow.Where(e => !e.cancelled))
                {
                    await ProcessEventAtWindowAsync(evt, window, context);
                    context.ProcessedEvents.Add(evt);
                }
                
                stopwatch.Stop();
                
                // Publish timing window completed event
                var completedCount = eventsForWindow.Count(e => !e.cancelled);
                var cancelledCount = eventsForWindow.Count(e => e.cancelled);
                
                var windowCompletedEvent = new TimingWindowCompletedEvent(
                    eventsForWindow[0].GetProperty("game") as Game,
                    eventsForWindow[0].TriggeredBy,
                    window,
                    context,
                    completedCount,
                    cancelledCount,
                    stopwatch.Elapsed,
                    this
                );
                Publish(windowCompletedEvent);
                
                UnityEngine.Debug.Log($"⏰ Completed {window} window: {completedCount} processed, {cancelledCount} cancelled in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to process {window} timing window: {ex.Message}");
                throw;
            }
            finally
            {
                lock (_timingLock)
                {
                    _currentTimingWindow = null;
                }
            }
        }
        
        private async Task ProcessEventAtWindowAsync(GameEvent gameEvent, TimingWindow window, TimingContext context)
        {
            if (gameEvent.cancelled) return;
            
            try
            {
                // Process timing-specific handlers
                if (_timingHandlers.TryGetValue(window, out var handlers))
                {
                    var orderedHandlers = handlers
                        .Where(h => h.ShouldHandleAtTiming(gameEvent, window))
                        .OrderByDescending(h => h.TimingPriority)
                        .ToList();
                    
                    foreach (var handler in orderedHandlers)
                    {
                        if (!gameEvent.cancelled)
                        {
                            await handler.HandleAtTimingAsync(gameEvent, window, context);
                        }
                    }
                }
                
                // Process regular event bus handlers for this event
                PublishAtTiming(gameEvent, window);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ Failed to process event {gameEvent.GetType().Name} at {window}: {ex.Message}");
            }
        }
        
        private void PublishToTimingHandlers<T>(T eventInstance, TimingWindow window) where T : GameEvent
        {
            if (!_timingHandlers.TryGetValue(window, out var handlers)) return;
            
            var context = CurrentContext ?? new TimingContext();
            
            // Process timing-aware handlers
            var tasks = handlers
                .Where(h => h.ShouldHandleAtTiming(eventInstance, window))
                .OrderByDescending(h => h.TimingPriority)
                .Select(async handler =>
                {
                    try
                    {
                        await handler.HandleAtTimingAsync(eventInstance, window, context);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"❌ Timing handler error: {ex.Message}");
                    }
                });
            
            // Execute all handlers concurrently
            Task.WhenAll(tasks).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    UnityEngine.Debug.LogError($"❌ Multiple timing handler errors: {t.Exception.Message}");
                }
            }, TaskScheduler.Current);
        }
        
        private void ProcessCancellations(TimingContext context)
        {
            if (context?.AllowCancellations != true) return;
            
            var cancelledCount = context.ProcessedEvents.Count(e => e.cancelled);
            if (cancelledCount > 0)
            {
                UnityEngine.Debug.Log($"🚫 Processed {cancelledCount} event cancellations");
            }
        }
        
        private async Task ProcessContingentEventsAsync(TimingContext context)
        {
            if (context?.ContingentEvents?.Count > 0)
            {
                var contingentEvents = new List<GameEvent>(context.ContingentEvents);
                context.ContingentEvents.Clear();
                
                // Process contingent events through timing sequence
                if (contingentEvents.Count > 0)
                {
                    await ProcessTimingSequenceAsync(contingentEvents);
                    UnityEngine.Debug.Log($"📎 Processed {contingentEvents.Count} contingent events");
                }
            }
        }
        
        private static int GetTimingPriority(TimingWindow window)
        {
            return (int)window;
        }
        
        #endregion
        
        #region Disposal
        
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clear timing-specific resources
                _contextStack.Clear();
                
                lock (_thenAbilities)
                {
                    _thenAbilities.Clear();
                }
                
                foreach (var handlers in _timingHandlers.Values)
                {
                    // Timing handlers don't need disposal as they're lightweight wrappers
                }
                _timingHandlers.Clear();
                
                lock (_timingLock)
                {
                    _currentTimingWindow = null;
                    _currentEvents.Clear();
                }
                
                UnityEngine.Debug.Log("🌉 TimingAwareEventBus disposed");
            }
            
            // Call parent dispose
            base.Dispose();
        }
        
        #endregion
    }
    
    #region Helper Classes
    
    /// <summary>
    /// Wrapper for timing-specific event handlers
    /// </summary>
    internal class TimingSpecificHandler<T> : ITimingAwareHandler where T : GameEvent
    {
        private readonly Func<T, TimingWindow, TimingContext, Task> _handler;
        public TimingWindow[] SupportedTimingWindows { get; }
        public int TimingPriority { get; }
        
        public TimingSpecificHandler(Func<T, TimingWindow, TimingContext, Task> handler, TimingWindow window, int priority)
        {
            _handler = handler;
            SupportedTimingWindows = new[] { window };
            TimingPriority = priority;
        }
        
        public bool ShouldHandleAtTiming(GameEvent gameEvent, TimingWindow window)
        {
            return gameEvent is T && SupportedTimingWindows.Contains(window);
        }
        
        public async Task HandleAtTimingAsync(GameEvent gameEvent, TimingWindow window, TimingContext context)
        {
            if (gameEvent is T typedEvent && ShouldHandleAtTiming(gameEvent, window))
            {
                await _handler(typedEvent, window, context);
            }
        }
    }
    
    /// <summary>
    /// Event published when an event is cancelled
    /// </summary>
    public class EventCancelledEvent : GameEvent
    {
        public GameEvent CancelledEvent { get; private set; }
        public string CancellationReason { get; private set; }
        
        public EventCancelledEvent(Game game, Player triggeredBy, GameEvent cancelledEvent, 
            string reason, object source = null)
            : base(game, triggeredBy, source)
        {
            CancelledEvent = cancelledEvent;
            CancellationReason = reason;
            
            AddEventData("cancelled_event_type", cancelledEvent.GetType().Name);
            AddEventData("cancelled_event_id", cancelledEvent.EventId);
            AddEventData("cancellation_reason", reason);
        }
    }
    
    #endregion
}