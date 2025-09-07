using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L5RGame.Events;
using UnityEngine;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Bridge that enables EventWindow to work with the new TimingAwareEventBus
    /// Provides backward compatibility while migrating to unified event system
    /// </summary>
    public class EventWindowBridge : IDisposable
    {
        #region Private Fields
        
        private readonly IUnifiedEventSystem _unifiedEventSystem;
        private readonly Game _game;
        private bool _isDisposed = false;
        
        // Compatibility tracking
        private readonly Dictionary<string, EventWindow> _activeEventWindows = new Dictionary<string, EventWindow>();
        private readonly List<GameEvent> _bridgedEvents = new List<GameEvent>();
        
        #endregion
        
        #region Constructor
        
        public EventWindowBridge(Game game, IUnifiedEventSystem unifiedEventSystem)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _unifiedEventSystem = unifiedEventSystem ?? throw new ArgumentNullException(nameof(unifiedEventSystem));
            
            Debug.Log("🌉 EventWindowBridge initialized - bridging legacy EventWindow to TimingAwareEventBus");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Process EventWindow events through the unified timing system
        /// </summary>
        /// <param name="eventWindow">Legacy EventWindow to process</param>
        /// <returns>Processing task</returns>
        public async Task ProcessEventWindowAsync(EventWindow eventWindow)
        {
            if (eventWindow == null || _isDisposed) return;
            
            try
            {
                var windowId = Guid.NewGuid().ToString();
                _activeEventWindows[windowId] = eventWindow;
                
                // Extract events from EventWindow
                var events = ExtractEventsFromWindow(eventWindow);
                if (events.Count == 0)
                {
                    Debug.Log("🌉 EventWindow has no events to process");
                    return;
                }
                
                Debug.Log($"🌉 Processing EventWindow with {events.Count} events through unified system");
                
                // Track bridged events
                lock (_bridgedEvents)
                {
                    _bridgedEvents.AddRange(events);
                }
                
                // Process through unified timing system
                var context = await _unifiedEventSystem.ProcessTimingSequenceAsync(events);
                
                // Update EventWindow state based on results
                UpdateEventWindowFromContext(eventWindow, context);
                
                // Emit legacy events for backward compatibility
                await EmitLegacyEventsAsync(events, context);
                
                _activeEventWindows.Remove(windowId);
                
                Debug.Log($"🌉 Completed EventWindow processing: {context.ProcessedEvents.Count} processed, " +
                         $"{events.Count(e => e.cancelled)} cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to process EventWindow through bridge: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Convert EventWindow pipeline to unified timing sequence
        /// </summary>
        /// <param name="eventWindow">EventWindow to convert</param>
        /// <returns>Timing context with converted events</returns>
        public TimingContext ConvertEventWindowToTimingContext(EventWindow eventWindow)
        {
            if (eventWindow == null) return new TimingContext();
            
            try
            {
                var context = new TimingContext();
                var events = ExtractEventsFromWindow(eventWindow);
                
                // Map EventWindow pipeline steps to timing windows
                foreach (var evt in events)
                {
                    var timingWindow = MapEventToTimingWindow(evt);
                    evt.AddEventData("mapped_timing_window", timingWindow.ToString());
                }
                
                context.ProcessedEvents.AddRange(events);
                
                Debug.Log($"🌉 Converted EventWindow to TimingContext with {events.Count} events");
                
                return context;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to convert EventWindow to TimingContext: {ex.Message}");
                return new TimingContext();
            }
        }
        
        /// <summary>
        /// Check if an event is currently being processed by the bridge
        /// </summary>
        /// <param name="gameEvent">Event to check</param>
        /// <returns>True if being processed by bridge</returns>
        public bool IsEventBeingBridged(GameEvent gameEvent)
        {
            if (gameEvent == null) return false;
            
            lock (_bridgedEvents)
            {
                return _bridgedEvents.Contains(gameEvent);
            }
        }
        
        /// <summary>
        /// Get all active EventWindows being processed
        /// </summary>
        /// <returns>Active EventWindows</returns>
        public IReadOnlyDictionary<string, EventWindow> GetActiveEventWindows()
        {
            return _activeEventWindows.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private List<GameEvent> ExtractEventsFromWindow(EventWindow eventWindow)
        {
            var events = new List<GameEvent>();
            
            try
            {
                // Use reflection to access EventWindow's internal events
                var eventsField = typeof(EventWindow).GetField("events", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (eventsField?.GetValue(eventWindow) is List<GameEvent> windowEvents)
                {
                    events.AddRange(windowEvents.Where(e => e != null));
                }
                else
                {
                    // Fallback: try to get events through public properties/methods
                    var publicEvents = GetEventsViaPublicInterface(eventWindow);
                    events.AddRange(publicEvents);
                }
                
                Debug.Log($"🌉 Extracted {events.Count} events from EventWindow");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to extract events from EventWindow: {ex.Message}");
            }
            
            return events;
        }
        
        private List<GameEvent> GetEventsViaPublicInterface(EventWindow eventWindow)
        {
            var events = new List<GameEvent>();
            
            try
            {
                // Try various public methods/properties that might expose events
                var windowType = eventWindow.GetType();
                
                // Look for GetEvents() method
                var getEventsMethod = windowType.GetMethod("GetEvents", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (getEventsMethod?.Invoke(eventWindow, null) is List<GameEvent> methodEvents)
                {
                    events.AddRange(methodEvents);
                }
                
                // Look for Events property
                var eventsProperty = windowType.GetProperty("Events", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (eventsProperty?.GetValue(eventWindow) is List<GameEvent> propertyEvents)
                {
                    events.AddRange(propertyEvents);
                }
                
                Debug.Log($"🌉 Retrieved {events.Count} events via public interface");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to get events via public interface: {ex.Message}");
            }
            
            return events;
        }
        
        private TimingWindow MapEventToTimingWindow(GameEvent gameEvent)
        {
            if (gameEvent == null) return TimingWindow.Handler;
            
            try
            {
                // Map based on event name patterns
                var eventName = gameEvent.name?.ToLower() ?? "";
                
                // Interrupt-type events
                if (eventName.Contains("would") || eventName.Contains("prevent"))
                    return TimingWindow.WouldInterrupt;
                
                if (eventName.Contains("forced") && eventName.Contains("interrupt"))
                    return TimingWindow.ForcedInterrupt;
                
                if (eventName.Contains("interrupt"))
                    return TimingWindow.Interrupt;
                
                // Reaction-type events
                if (eventName.Contains("forced") && (eventName.Contains("reaction") || eventName.Contains("trigger")))
                    return TimingWindow.ForcedReaction;
                
                if (eventName.Contains("reaction") || eventName.Contains("trigger"))
                    return TimingWindow.Reaction;
                
                // Default to Handler for most events
                return TimingWindow.Handler;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to map event to timing window: {ex.Message}");
                return TimingWindow.Handler;
            }
        }
        
        private void UpdateEventWindowFromContext(EventWindow eventWindow, TimingContext context)
        {
            if (eventWindow == null || context == null) return;
            
            try
            {
                // Update EventWindow state based on TimingContext results
                var cancelledEvents = context.ProcessedEvents.Count(e => e.cancelled);
                var completedEvents = context.ProcessedEvents.Count(e => !e.cancelled);
                
                // Use reflection to update internal EventWindow state if needed
                UpdateEventWindowState(eventWindow, "ProcessedEvents", completedEvents);
                UpdateEventWindowState(eventWindow, "CancelledEvents", cancelledEvents);
                UpdateEventWindowState(eventWindow, "ProcessingTime", DateTime.UtcNow - context.StartTime);
                
                Debug.Log($"🌉 Updated EventWindow state: {completedEvents} completed, {cancelledEvents} cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to update EventWindow from context: {ex.Message}");
            }
        }
        
        private void UpdateEventWindowState(EventWindow eventWindow, string propertyName, object value)
        {
            try
            {
                var windowType = eventWindow.GetType();
                var property = windowType.GetProperty(propertyName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (property?.CanWrite == true)
                {
                    property.SetValue(eventWindow, value);
                }
                else
                {
                    // Try field if property doesn't exist
                    var field = windowType.GetField(propertyName, 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    field?.SetValue(eventWindow, value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to update EventWindow state {propertyName}: {ex.Message}");
            }
        }
        
        private async Task EmitLegacyEventsAsync(List<GameEvent> events, TimingContext context)
        {
            try
            {
                foreach (var evt in events.Where(e => !e.cancelled))
                {
                    // Emit to legacy system for backward compatibility
                    _unifiedEventSystem.EmitLegacyEvent(evt.name, evt.GetData());
                    _unifiedEventSystem.EmitLegacyEvent(evt.name + ":" + AbilityTypes.OtherEffects, evt.GetData());
                }
                
                Debug.Log($"🌉 Emitted {events.Count(e => !e.cancelled)} legacy events for backward compatibility");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to emit legacy events: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clean up completed bridged events
        /// </summary>
        public void CleanupBridgedEvents()
        {
            if (_isDisposed) return;
            
            try
            {
                lock (_bridgedEvents)
                {
                    List<GameEvent> eventsToRemove = _bridgedEvents
                        .Where(e => e.resolved || e.cancelled)
                        .ToList();
                    
                    foreach (var evt in eventsToRemove)
                    {
                        _bridgedEvents.Remove(evt);
                    }
                    
                    if (eventsToRemove.Count > 0)
                    {
                        Debug.Log($"🌉 Cleaned up {eventsToRemove.Count} completed bridged events");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to cleanup bridged events: {ex.Message}");
            }
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                // Clean up resources
                _activeEventWindows.Clear();
                
                lock (_bridgedEvents)
                {
                    _bridgedEvents.Clear();
                }
                
                Debug.Log("🌉 EventWindowBridge disposed");
                _isDisposed = true;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for EventWindow compatibility
    /// </summary>
    public static class EventWindowExtensions
    {
        /// <summary>
        /// Process EventWindow through unified event system
        /// </summary>
        /// <param name="eventWindow">EventWindow to process</param>
        /// <param name="game">Game instance</param>
        /// <returns>Processing task</returns>
        public static async Task ProcessThroughUnifiedSystemAsync(this EventWindow eventWindow, Game game)
        {
            if (eventWindow == null || game == null) return;
            
            try
            {
                var unifiedSystem = game.GetUnifiedEventSystem();
                if (unifiedSystem == null)
                {
                    Debug.LogWarning("⚠️ No unified event system available, falling back to traditional EventWindow processing");
                    eventWindow.Execute(); // Fallback to traditional processing
                    return;
                }
                
                using var bridge = new EventWindowBridge(game, unifiedSystem);
                await bridge.ProcessEventWindowAsync(eventWindow);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to process EventWindow through unified system: {ex.Message}");
                // Fallback to traditional processing
                eventWindow.Execute();
            }
        }
        
        /// <summary>
        /// Convert EventWindow to timing context for analysis
        /// </summary>
        /// <param name="eventWindow">EventWindow to convert</param>
        /// <param name="game">Game instance</param>
        /// <returns>Timing context</returns>
        public static TimingContext ToTimingContext(this EventWindow eventWindow, Game game)
        {
            if (eventWindow == null || game == null) return new TimingContext();
            
            try
            {
                var unifiedSystem = game.GetUnifiedEventSystem();
                if (unifiedSystem == null) return new TimingContext();
                
                using var bridge = new EventWindowBridge(game, unifiedSystem);
                return bridge.ConvertEventWindowToTimingContext(eventWindow);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to convert EventWindow to TimingContext: {ex.Message}");
                return new TimingContext();
            }
        }
    }
}