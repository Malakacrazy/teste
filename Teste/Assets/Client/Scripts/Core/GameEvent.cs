using System.Collections.Generic;
using System;
using UnityEngine;

namespace L5RGame
{
    public interface IGameEvent
    {
        string Name { get; }
        void Cancel();
        bool Execute();
        bool cancelled { get; }
    }

    public class GameEvent : MonoBehaviour, IGameEvent
    {
        public string eventName;
        public Dictionary<string, object> parameters;
        public Func<bool> handler;
        public bool cancelled { get; private set; } = false;
        
        public string Name => eventName;

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

        public bool Execute()
        {
            if (cancelled)
                return false;
            
            return handler?.Invoke() ?? false;
        }
    }
}
