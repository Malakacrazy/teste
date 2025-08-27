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

        public bool Continue()
        {
            return !completed;
        }

        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Handle menu commands during event window
        }

        public void OnCardClicked(Player player, BaseCard card)
        {
            // Handle card clicks during event window
        }

        public void OnRingClicked(Player player, Ring ring)
        {
            // Handle ring clicks during event window
        }

        public void Initialize()
        {
            // Initialize event window
            completed = false;
        }

        public void Cleanup()
        {
            // Clean up event window resources
        }
    }
}
