using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class EventWindow : MonoBehaviour, IGameStep
    {
        private Game game;
        private List<GameEvent> events;
        private bool completed = false;

        public EventWindow(Game game, List<GameEvent> events)
        {
            this.game = game;
            this.events = events ?? new List<GameEvent>();
        }

        public bool Execute()
        {
            // Execute all events in the window
            foreach (var evt in events)
            {
                if (!evt.cancelled)
                {
                    evt.Execute();
                }
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
