using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    /// <summary>
    /// Action window for player actions during the phase
    /// </summary>
    public class ActionWindow : MonoBehaviour, IGameStep
    {
        [Header("Window Configuration")]
        public string windowName;
        public string windowType;
        
        private Game game;
        private bool isComplete = false;
        private System.Action completionCallback;

        /// <summary>
        /// Initialize the action window with required parameters
        /// </summary>
        public void Initialize(Game gameInstance, string name, string type, System.Action onComplete = null)
        {
            game = gameInstance;
            windowName = name;
            windowType = type;
            completionCallback = onComplete;
        }

        public bool Execute()
        {
            if (game == null)
            {
                Debug.LogError("ActionWindow: Game instance is null!");
                return false;
            }
            
            game.AddMessage("Opening {0} action window", windowName);
            // Action windows remain active until players complete their actions
            return false; // Don't complete immediately
        }

        public bool IsComplete()
        {
            return isComplete;
        }
        
        public bool Continue()
        {
            // Continue processing this action window
            return !isComplete;
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Handle menu commands in the action window
        }
        
        public void OnCardClicked(Player player, BaseCard card)
        {
            // Handle card clicks in the action window
        }
        
        public void OnRingClicked(Player player, Ring ring)
        {
            // Handle ring clicks in the action window
        }
        
        public void Initialize()
        {
            // Initialize the action window state
            isComplete = false;
        }
        
        public void Cleanup()
        {
            // Clean up action window resources
            completionCallback?.Invoke();
        }
        
        /// <summary>
        /// Mark this action window as complete
        /// </summary>
        public void Complete()
        {
            if (!isComplete)
            {
                isComplete = true;
                game?.AddMessage("Closing {0} action window", windowName);
                completionCallback?.Invoke();
            }
        }
        
        /// <summary>
        /// Mark that an action has been taken in this window
        /// </summary>
        public void MarkActionAsTaken() 
        {
            // Could be used to track player activity
        }
        
        /// <summary>
        /// Get debug information about this action window
        /// </summary>
        public string GetDebugInfo() 
        {
            return $"ActionWindow: {windowName} ({windowType}) - Complete: {isComplete}";
        }
    }
}
