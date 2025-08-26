using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class InitiateAbilityEventWindow : MonoBehaviour, IGameStep
    {
        private Game game;
        private List<InitiateCardAbilityEvent> events;
        private bool completed = false;

        public InitiateAbilityEventWindow(Game game, List<InitiateCardAbilityEvent> events)
        {
            this.game = game;
            this.events = events ?? new List<InitiateCardAbilityEvent>();
        }

        public bool Execute()
        {
            // Execute all events
            foreach (var evt in events)
            {
                evt.Execute();
            }
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
