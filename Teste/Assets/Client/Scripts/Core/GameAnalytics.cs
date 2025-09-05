using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Static analytics system for the L5R card game.
    /// This class provides a centralized way to log analytics events.
    /// </summary>
    public static class GameAnalytics
    {
        #region Private Fields
        
        private static bool _isInitialized = false;
        private static bool _enableLogging = true;
        private static readonly Dictionary<string, int> _eventCounts = new Dictionary<string, int>();
        private static readonly List<AnalyticsEvent> _recentEvents = new List<AnalyticsEvent>();
        private static readonly int _maxRecentEvents = 1000;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Whether analytics logging is enabled
        /// </summary>
        public static bool IsEnabled => _enableLogging;
        
        /// <summary>
        /// Whether the analytics system has been initialized
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Get the total number of events logged
        /// </summary>
        public static int TotalEventsLogged => _recentEvents.Count;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the analytics system
        /// </summary>
        /// <param name="enableLogging">Whether to enable event logging</param>
        public static void Initialize(bool enableLogging = true)
        {
            _enableLogging = enableLogging;
            _isInitialized = true;
            
            Debug.Log($"📊 GameAnalytics initialized (logging: {enableLogging})");
        }
        
        /// <summary>
        /// Shutdown the analytics system
        /// </summary>
        public static void Shutdown()
        {
            _isInitialized = false;
            _eventCounts.Clear();
            _recentEvents.Clear();
            
            Debug.Log("📊 GameAnalytics shut down");
        }
        
        #endregion
        
        #region Event Logging
        
        /// <summary>
        /// Log an analytics event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Event data dictionary</param>
        public static void LogEvent(string eventName, Dictionary<string, object> eventData = null)
        {
            if (!_isInitialized || !_enableLogging || string.IsNullOrEmpty(eventName))
            {
                return;
            }
            
            try
            {
                // Create analytics event
                var analyticsEvent = new AnalyticsEvent
                {
                    EventName = eventName,
                    EventData = eventData ?? new Dictionary<string, object>(),
                    Timestamp = DateTime.UtcNow,
                    SessionId = GetSessionId()
                };
                
                // Add to recent events (with size limit)
                lock (_recentEvents)
                {
                    _recentEvents.Add(analyticsEvent);
                    
                    if (_recentEvents.Count > _maxRecentEvents)
                    {
                        _recentEvents.RemoveAt(0);
                    }
                }
                
                // Update event counts
                lock (_eventCounts)
                {
                    if (_eventCounts.ContainsKey(eventName))
                    {
                        _eventCounts[eventName]++;
                    }
                    else
                    {
                        _eventCounts[eventName] = 1;
                    }
                }
                
                // Log to console in development
                if (Application.isEditor || Debug.isDebugBuild)
                {
                    LogToConsole(analyticsEvent);
                }
                
                // In a real implementation, you would send this data to your analytics service
                // SendToAnalyticsService(analyticsEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to log analytics event '{eventName}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log a simple event with just a name
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        public static void LogEvent(string eventName)
        {
            LogEvent(eventName, null);
        }
        
        /// <summary>
        /// Log a player action event
        /// </summary>
        /// <param name="playerName">Name of the player</param>
        /// <param name="action">Action performed</param>
        /// <param name="additionalData">Additional data</param>
        public static void LogPlayerAction(string playerName, string action, Dictionary<string, object> additionalData = null)
        {
            var eventData = new Dictionary<string, object>
            {
                { "player_name", playerName },
                { "action", action }
            };
            
            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    eventData[kvp.Key] = kvp.Value;
                }
            }
            
            LogEvent("player_action", eventData);
        }
        
        /// <summary>
        /// Log a game performance metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Unit of measurement</param>
        public static void LogPerformanceMetric(string metricName, float value, string unit = "")
        {
            var eventData = new Dictionary<string, object>
            {
                { "metric_name", metricName },
                { "value", value },
                { "unit", unit }
            };
            
            LogEvent("performance_metric", eventData);
        }
        
        #endregion
        
        #region Data Retrieval
        
        /// <summary>
        /// Get recent analytics events
        /// </summary>
        /// <param name="count">Number of recent events to get (default: all)</param>
        /// <returns>List of recent analytics events</returns>
        public static List<AnalyticsEvent> GetRecentEvents(int count = -1)
        {
            lock (_recentEvents)
            {
                if (count <= 0 || count >= _recentEvents.Count)
                {
                    return new List<AnalyticsEvent>(_recentEvents);
                }
                
                var startIndex = _recentEvents.Count - count;
                return _recentEvents.GetRange(startIndex, count);
            }
        }
        
        /// <summary>
        /// Get event counts by event name
        /// </summary>
        /// <returns>Dictionary of event names to counts</returns>
        public static Dictionary<string, int> GetEventCounts()
        {
            lock (_eventCounts)
            {
                return new Dictionary<string, int>(_eventCounts);
            }
        }
        
        /// <summary>
        /// Get count for a specific event type
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <returns>Count of events</returns>
        public static int GetEventCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return 0;
                
            lock (_eventCounts)
            {
                return _eventCounts.TryGetValue(eventName, out int count) ? count : 0;
            }
        }
        
        /// <summary>
        /// Get analytics summary
        /// </summary>
        /// <returns>Analytics summary object</returns>
        public static AnalyticsSummary GetSummary()
        {
            lock (_eventCounts)
            lock (_recentEvents)
            {
                return new AnalyticsSummary
                {
                    TotalEvents = _recentEvents.Count,
                    UniqueEventTypes = _eventCounts.Count,
                    EventCounts = new Dictionary<string, int>(_eventCounts),
                    LastEventTime = _recentEvents.Count > 0 ? _recentEvents[_recentEvents.Count - 1].Timestamp : (DateTime?)null,
                    SessionId = GetSessionId(),
                    IsEnabled = _enableLogging
                };
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Enable or disable analytics logging
        /// </summary>
        /// <param name="enabled">Whether to enable logging</param>
        public static void SetEnabled(bool enabled)
        {
            _enableLogging = enabled;
            Debug.Log($"📊 GameAnalytics logging {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Clear all analytics data
        /// </summary>
        public static void ClearData()
        {
            lock (_eventCounts)
            lock (_recentEvents)
            {
                _eventCounts.Clear();
                _recentEvents.Clear();
            }
            
            Debug.Log("📊 Analytics data cleared");
        }
        
        #endregion
        
        #region Private Helpers
        
        private static void LogToConsole(AnalyticsEvent analyticsEvent)
        {
            var dataStr = "";
            if (analyticsEvent.EventData.Count > 0)
            {
                var dataParts = new List<string>();
                foreach (var kvp in analyticsEvent.EventData)
                {
                    dataParts.Add($"{kvp.Key}={kvp.Value}");
                }
                dataStr = $" | {string.Join(", ", dataParts)}";
            }
            
            Debug.Log($"📊 Analytics: {analyticsEvent.EventName}{dataStr}");
        }
        
        private static string _sessionId;
        private static string GetSessionId()
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                _sessionId = Guid.NewGuid().ToString();
            }
            return _sessionId;
        }
        
        #endregion
        
        #region Unity Integration
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            Initialize(true);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a single analytics event
    /// </summary>
    [Serializable]
    public class AnalyticsEvent
    {
        public string EventName { get; set; }
        public Dictionary<string, object> EventData { get; set; }
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; }
        
        public override string ToString()
        {
            return $"{EventName} @ {Timestamp:HH:mm:ss} ({EventData.Count} data points)";
        }
    }
    
    /// <summary>
    /// Summary of analytics data
    /// </summary>
    [Serializable]
    public class AnalyticsSummary
    {
        public int TotalEvents { get; set; }
        public int UniqueEventTypes { get; set; }
        public Dictionary<string, int> EventCounts { get; set; }
        public DateTime? LastEventTime { get; set; }
        public string SessionId { get; set; }
        public bool IsEnabled { get; set; }
        
        public override string ToString()
        {
            return $"Analytics: {TotalEvents} events, {UniqueEventTypes} types, Last: {LastEventTime?.ToString("HH:mm:ss") ?? "None"}";
        }
    }
}