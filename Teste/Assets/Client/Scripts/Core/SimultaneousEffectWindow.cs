using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class SimultaneousEffectWindow : MonoBehaviour, IGameStep
    {
        private Game game;
        private List<EffectChoice> choices = new List<EffectChoice>();
        private bool completed = false;

        public SimultaneousEffectWindow(Game game)
        {
            this.game = game;
        }

        public void AddChoice(EffectChoice choice)
        {
            if (choice != null)
            {
                choices.Add(choice);
            }
        }

        public bool Execute()
        {
            // Execute simultaneous effect window logic
            completed = true;
            return true;
        }

        public bool IsComplete()
        {
            return completed;
        }
    }
}
