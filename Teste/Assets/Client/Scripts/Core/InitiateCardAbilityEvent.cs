using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Represents an initiate card ability event
    /// </summary>
    public class InitiateCardAbilityEvent : GameEvent
    {
        private Dictionary<string, object> parameters;
        private Func<bool> handler;

        public InitiateCardAbilityEvent(Dictionary<string, object> eventParameters, Func<bool> eventHandler) 
            : base("initiateCardAbility", eventParameters, eventHandler)
        {
            parameters = eventParameters ?? new Dictionary<string, object>();
            handler = eventHandler;
        }

        public void Execute()
        {
            handler?.Invoke();
        }

        public Dictionary<string, object> GetParameters()
        {
            return parameters;
        }
    }
}
