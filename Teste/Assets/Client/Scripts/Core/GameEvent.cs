using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Represents a game event with parameters and handlers
    /// </summary>
    public class GameEvent : IGameEvent
    {
        public string eventName;
        public Dictionary<string, object> parameters;
        public Func<bool> handler;
        public bool cancelled { get; set; } = false;
        public object condition;

        public GameEvent(string eventName, Dictionary<string, object> parameters, Func<bool> handler)
        {
            this.eventName = eventName;
            this.parameters = parameters ?? new Dictionary<string, object>();
            this.handler = handler;
        }

        public void Cancel()
        {
            cancelled = true;
        }

        // IGameEvent interface implementations
        public string EventName => eventName;
        public Dictionary<string, object> Parameters => parameters;
        public bool IsCancelled => cancelled;
        public string Name => eventName;

        public bool Execute()
        {
            if (cancelled) return false;
            return handler?.Invoke() ?? true;
        }
    }

    /// <summary>
    /// Interface for game events
    /// </summary>
    public interface IGameEvent
    {
        string EventName { get; }
        Dictionary<string, object> Parameters { get; }
        bool Execute();
        bool IsCancelled { get; }
        string Name { get; }  // Added for compatibility
        bool cancelled { get; set; }  // Added for compatibility
        void Cancel();  // Add Cancel method
    }
}
