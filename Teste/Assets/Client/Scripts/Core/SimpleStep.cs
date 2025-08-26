using UnityEngine;
using System;

namespace L5RGame
{
    public class SimpleStep : MonoBehaviour, IGameStep
    {
        private Func<bool> handler;
        private Game gameInstance;

        public SimpleStep(Game game, Func<bool> stepHandler)
        {
            gameInstance = game;
            handler = stepHandler;
        }

        public bool Execute()
        {
            return handler?.Invoke() ?? false;
        }

        public bool IsComplete()
        {
            return true; // Simple steps complete immediately after execution
        }
    }
}
