using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Simple step for basic game actions
    /// </summary>
    public partial class SimpleStep : IGameStep
    {
        private Game game;
        private Func<bool> stepFunction;
        
        public bool CanCancel { get; set; } = true;
        
        public SimpleStep(Game gameInstance, Func<bool> step)
        {
            game = gameInstance;
            stepFunction = step;
        }
        
        public bool Execute()
        {
            return Continue();
        }
        
        public bool IsComplete()
        {
            return true;
        }
        
        public bool Continue()
        {
            try
            {
                return stepFunction?.Invoke() ?? true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in SimpleStep: {e.Message}");
                return true; // Continue despite error
            }
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }
        
        public string GetDebugInfo()
        {
            return $"SimpleStep - Function: {(stepFunction != null ? "Set" : "Null")}";
        }
    }
}
