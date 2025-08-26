using System.Collections.Generic;
using System;
using UnityEngine;

namespace L5RGame
{
    public class InitiateCardAbilityEvent : MonoBehaviour
    {
        public Dictionary<string, object> parameters;
        public Func<bool> handler;

        public InitiateCardAbilityEvent(Dictionary<string, object> parameters, Func<bool> handler)
        {
            this.parameters = parameters ?? new Dictionary<string, object>();
            this.handler = handler;
        }

        public bool Execute()
        {
            return handler?.Invoke() ?? false;
        }
    }
}
