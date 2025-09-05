using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Event logger that captures all game events for debugging, replay, and analysis.
    /// Can save events to file and replay them for debugging purposes.
    /// </summary>
    public class EventLogger : MonoBehaviour
    {
        [Header("Event Logger Configuration")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool saveToFile = false;
        [SerializeField] private int maxEventsInMemory = 1000;
        [SerializeField] private string logFileName = "game_events.log";
        
        [Header("Filter Settings")]
        [SerializeField] private bool logFateEvents = true;
        [SerializeField] private bool logRingEvents = true;
        [SerializeField] private bool logCharacterEvents = true;
        [SerializeField] private bool logCardEvents = true;
        [SerializeField] private bool logAbilityEvents = true;
        
        private IEventBus eventBus;
        private List<GameEventLogEntry> eventHistory = new List<GameEventLogEntry>();
        private string logFilePath;
        
        [Header("Runtime Statistics")]
        [SerializeField] private int totalEventsLogged = 0;
        [SerializeField] private int eventsInMemory = 0;
        
        /// <summary>
        /// Initialize the event logger
        /// </summary>
        /// <param name="eventBus">Event bus to subscribe to</param>
        public void Initialize(IEventBus eventBus)
        {
            this.eventBus = eventBus;
            
            if (saveToFile)
            {
                logFilePath = Path.Combine(Application.persistentDataPath, logFileName);
                Debug.Log($"📝 Event logger will save to: {logFilePath}");
            }
            
            // Subscribe to all event types
            SubscribeToAllEvents();
            
            Debug.Log($"📝 Event logger initialized (logging: {enableLogging}, save to file: {saveToFile})");
        }
        
        /// <summary>
        /// Subscribe to all game event types
        /// </summary>
        private void SubscribeToAllEvents()
        {
            if (logFateEvents)
            {
                eventBus.Subscribe<FateRemovedEvent>(LogEvent);
            }
            
            if (logRingEvents)
            {
                eventBus.Subscribe<RingResolvedEvent>(LogEvent);
            }
            
            if (logCharacterEvents)
            {
                eventBus.Subscribe<CharacterHonoredEvent>(LogEvent);
                eventBus.Subscribe<CharacterDishonoredEvent>(LogEvent);
                eventBus.Subscribe<CharacterLeavesPlayEvent>(LogEvent);
            }
            
            if (logCardEvents)
            {
                eventBus.Subscribe<CardDrawnEvent>(LogEvent);
            }
            
            if (logAbilityEvents)
            {
                eventBus.Subscribe<AbilityExecutedEvent>(LogEvent);
            }
        }
        
        /// <summary>
        /// Log a game event
        /// </summary>
        /// <param name="gameEvent">Event to log</param>
        private void LogEvent(GameEvent gameEvent)
        {
            if (!enableLogging) return;
            
            try
            {
                var logEntry = new GameEventLogEntry
                {
                    EventId = gameEvent.EventId,
                    EventType = gameEvent.GetType().Name,
                    Timestamp = gameEvent.Timestamp,
                    PlayerName = gameEvent.TriggeredBy?.Name ?? "System",
                    Description = gameEvent.GetDescription(),
                    EventData = new Dictionary<string, object>(gameEvent.EventData),
                    SourceType = gameEvent.Source?.GetType().Name
                };
                
                // Add to memory
                eventHistory.Add(logEntry);
                totalEventsLogged++;
                eventsInMemory = eventHistory.Count;
                
                // Maintain memory limit
                if (eventHistory.Count > maxEventsInMemory)
                {
                    eventHistory.RemoveAt(0);
                    eventsInMemory = eventHistory.Count;
                }
                
                // Save to file if enabled
                if (saveToFile)
                {
                    SaveEventToFile(logEntry);
                }
                
                Debug.Log($"📝 Logged: {logEntry.EventType} - {logEntry.Description}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error logging event {gameEvent.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save a single event to file
        /// </summary>
        /// <param name="logEntry">Event to save</param>
        private void SaveEventToFile(GameEventLogEntry logEntry)
        {
            try
            {
                var json = JsonUtility.ToJson(logEntry, true);
                File.AppendAllText(logFilePath, json + "\n---\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error saving event to file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get recent events from memory
        /// </summary>
        /// <param name="count">Number of recent events to get</param>
        /// <returns>List of recent events</returns>
        public List<GameEventLogEntry> GetRecentEvents(int count = 10)
        {
            return eventHistory.TakeLast(count).ToList();
        }
        
        /// <summary>
        /// Get events of a specific type
        /// </summary>
        /// <param name="eventType">Type of events to get</param>
        /// <param name="count">Maximum number of events</param>
        /// <returns>List of events of the specified type</returns>
        public List<GameEventLogEntry> GetEventsByType(string eventType, int count = 10)
        {
            return eventHistory
                .Where(e => e.EventType == eventType)
                .TakeLast(count)
                .ToList();
        }
        
        /// <summary>
        /// Get events by player
        /// </summary>
        /// <param name="playerName">Player name</param>
        /// <param name="count">Maximum number of events</param>
        /// <returns>List of events by the specified player</returns>
        public List<GameEventLogEntry> GetEventsByPlayer(string playerName, int count = 10)
        {
            return eventHistory
                .Where(e => e.PlayerName == playerName)
                .TakeLast(count)
                .ToList();
        }
        
        /// <summary>
        /// Clear event history
        /// </summary>
        [ContextMenu("Clear Event History")]
        public void ClearEventHistory()
        {
            eventHistory.Clear();
            eventsInMemory = 0;
            Debug.Log("📝 Event history cleared");
        }
        
        /// <summary>
        /// Export all events to file
        /// </summary>
        [ContextMenu("Export Events to File")]
        public void ExportEventsToFile()
        {
            try
            {
                var exportPath = Path.Combine(Application.persistentDataPath, $"exported_events_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                var json = JsonUtility.ToJson(new EventHistoryExport { Events = eventHistory }, true);
                File.WriteAllText(exportPath, json);
                Debug.Log($"📄 Exported {eventHistory.Count} events to: {exportPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error exporting events: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Show event statistics
        /// </summary>
        [ContextMenu("Show Event Statistics")]
        public void ShowEventStatistics()
        {
            if (eventHistory.Count == 0)
            {
                Debug.Log("📊 No events logged yet");
                return;
            }
            
            var eventTypeCounts = eventHistory
                .GroupBy(e => e.EventType)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
            
            var playerCounts = eventHistory
                .Where(e => e.PlayerName != "System")
                .GroupBy(e => e.PlayerName)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
            
            Debug.Log($"📊 Event Logger Statistics:\n" +
                     $"• Total Events Logged: {totalEventsLogged}\n" +
                     $"• Events in Memory: {eventsInMemory}\n" +
                     $"• First Event: {eventHistory.First().Timestamp}\n" +
                     $"• Last Event: {eventHistory.Last().Timestamp}\n");
            
            Debug.Log("📈 Event Type Breakdown:");
            foreach (var kvp in eventTypeCounts)
            {
                Debug.Log($"  • {kvp.Key}: {kvp.Value}");
            }
            
            if (playerCounts.Count > 0)
            {
                Debug.Log("👥 Player Activity:");
                foreach (var kvp in playerCounts)
                {
                    Debug.Log($"  • {kvp.Key}: {kvp.Value} events");
                }
            }
        }
        
        /// <summary>
        /// Print recent events to console
        /// </summary>
        /// <param name="count">Number of recent events to print</param>
        [ContextMenu("Print Recent Events")]
        public void PrintRecentEvents(int count = 10)
        {
            var recentEvents = GetRecentEvents(count);
            
            Debug.Log($"📋 Last {recentEvents.Count} Events:");
            for (int i = 0; i < recentEvents.Count; i++)
            {
                var evt = recentEvents[i];
                Debug.Log($"{i + 1}. [{evt.Timestamp:HH:mm:ss}] {evt.EventType}: {evt.Description}");
            }
        }
        
        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        void OnDestroy()
        {
            if (eventBus != null)
            {
                // Unsubscribe from all events
                if (logFateEvents)
                    eventBus.Unsubscribe<FateRemovedEvent>(LogEvent);
                if (logRingEvents)
                    eventBus.Unsubscribe<RingResolvedEvent>(LogEvent);
                if (logCharacterEvents)
                {
                    eventBus.Unsubscribe<CharacterHonoredEvent>(LogEvent);
                    eventBus.Unsubscribe<CharacterDishonoredEvent>(LogEvent);
                    eventBus.Unsubscribe<CharacterLeavesPlayEvent>(LogEvent);
                }
                if (logCardEvents)
                    eventBus.Unsubscribe<CardDrawnEvent>(LogEvent);
                if (logAbilityEvents)
                    eventBus.Unsubscribe<AbilityExecutedEvent>(LogEvent);
                
                Debug.Log("📝 Event logger cleanup completed");
            }
            
            // Final export if enabled
            if (saveToFile && eventHistory.Count > 0)
            {
                ExportEventsToFile();
            }
        }
    }
    
    /// <summary>
    /// Log entry for a game event
    /// </summary>
    [Serializable]
    public class GameEventLogEntry
    {
        public string EventId;
        public string EventType;
        public DateTime Timestamp;
        public string PlayerName;
        public string Description;
        public Dictionary<string, object> EventData;
        public string SourceType;
    }
    
    /// <summary>
    /// Container for exporting event history
    /// </summary>
    [Serializable]
    public class EventHistoryExport
    {
        public List<GameEventLogEntry> Events;
    }
}