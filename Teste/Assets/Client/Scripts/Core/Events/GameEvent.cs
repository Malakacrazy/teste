using System;
using UnityEngine;

namespace L5RGame.Events
{
    /// <summary>
    /// Base class for all game events in the L5R event system.
    /// Provides common properties and functionality for event tracking and debugging.
    /// </summary>
    [Serializable]
    public abstract class GameEvent
    {
        /// <summary>
        /// Unique identifier for this event instance
        /// </summary>
        public string EventId { get; private set; }
        
        /// <summary>
        /// When this event occurred
        /// </summary>
        public DateTime Timestamp { get; private set; }
        
        /// <summary>
        /// The game instance where this event occurred
        /// </summary>
        public Game Game { get; protected set; }
        
        /// <summary>
        /// The player who triggered this event (if applicable)
        /// </summary>
        public Player TriggeredBy { get; protected set; }
        
        /// <summary>
        /// Source object that created this event
        /// </summary>
        public object Source { get; protected set; }
        
        /// <summary>
        /// Additional event data for extensibility
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> EventData { get; private set; }
        
        /// <summary>
        /// Initialize base event properties
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the event</param>
        /// <param name="source">Source object</param>
        protected GameEvent(Game game, Player triggeredBy = null, object source = null)
        {
            EventId = System.Guid.NewGuid().ToString();
            Timestamp = System.DateTime.Now;
            Game = game;
            TriggeredBy = triggeredBy;
            Source = source;
            EventData = new System.Collections.Generic.Dictionary<string, object>();
        }
        
        /// <summary>
        /// Add custom data to the event
        /// </summary>
        /// <param name="key">Data key</param>
        /// <param name="value">Data value</param>
        public void AddEventData(string key, object value)
        {
            EventData[key] = value;
        }
        
        /// <summary>
        /// Get custom data from the event
        /// </summary>
        /// <param name="key">Data key</param>
        /// <returns>Data value or null</returns>
        public object GetEventData(string key)
        {
            return EventData.TryGetValue(key, out object value) ? value : null;
        }
        
        /// <summary>
        /// Get event type name for logging and debugging
        /// </summary>
        public virtual string GetEventTypeName()
        {
            return GetType().Name;
        }
        
        /// <summary>
        /// Get a human-readable description of this event
        /// </summary>
        /// <returns>Event description</returns>
        public virtual string GetDescription()
        {
            return $"{GetEventTypeName()} triggered by {TriggeredBy?.Name ?? "System"} at {Timestamp:HH:mm:ss}";
        }
        
        /// <summary>
        /// Convert event to string for debugging
        /// </summary>
        public override string ToString()
        {
            return $"[{GetEventTypeName()}] {GetDescription()}";
        }
    }
}