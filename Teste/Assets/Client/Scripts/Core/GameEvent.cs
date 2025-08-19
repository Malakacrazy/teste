using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GameEvent : MonoBehaviour
    {
        public GameEvent(string eventName, Dictionary<string, object> parameters, System.Func<bool> handler) { }
    }
}