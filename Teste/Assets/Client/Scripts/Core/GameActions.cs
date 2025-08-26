using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GameActions : MonoBehaviour
    {
        public GameAction GetAction(string actionName, object value) => new GameAction();
    }
    
    public partial class GameAction
    {
        public void AddEventsToArray(List<GameEvent> events, AbilityContext context) { }
        public void Resolve(Player player, object context) { } // Added missing method
    }
}