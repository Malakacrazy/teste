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

        public bool Continue()
        {
            return !completed;
        }

        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Handle menu commands during initiate ability event window
        }

        public void OnCardClicked(Player player, BaseCard card)
        {
            // Handle card clicks during initiate ability event window
        }

        public void OnRingClicked(Player player, Ring ring)
        {
            // Handle ring clicks during initiate ability event window
        }

        public void Initialize()
        {
            // Initialize initiate ability event window
            completed = false;
        }

        public void Cleanup()
        {
            // Clean up initiate ability event window resources
        }
    }
}
