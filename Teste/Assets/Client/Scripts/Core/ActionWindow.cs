using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Action window step
    /// </summary>
    public partial class ActionWindow : MonoBehaviour, IGameStep
    {
        protected Player currentPlayer;
        protected Game game;
        protected string windowName;
        protected string windowType;
        protected System.Action onComplete;
        protected bool isComplete = false;
        protected bool actionTaken = false;

        public ActionWindow() { }

        public ActionWindow(Game game, string name)
        {
            this.game = game;
            this.windowName = name;
        }

        public ActionWindow(Game game, Player player = null)
        {
            this.game = game;
            currentPlayer = player;
        }
        
        public void Initialize(Game gameInstance, string name, string type, System.Action onCompleteCallback)
        {
            game = gameInstance;
            windowName = name;
            windowType = type;
            onComplete = onCompleteCallback;
            isComplete = false;
            actionTaken = false;
        }

        public bool Execute()
        {
            return Continue();
        }
        
        public bool Continue()
        {
            return isComplete;
        }
        
        public bool IsComplete => isComplete;
        public bool CanCancel => true;
        public string StepName => windowName ?? "ActionWindow";

        public void Complete()
        {
            if (!isComplete)
            {
                isComplete = true;
                onComplete?.Invoke();
            }
        }
        
        public void MarkActionAsTaken()
        {
            actionTaken = true;
        }
        
        public bool CancelStep() 
        { 
            isComplete = true; 
            return true; 
        }
        
        public void QueueStep(IGameStep step) 
        { 
            // ActionWindow doesn't support queuing sub-steps by default
        }
        
        public bool OnMenuCommand(Player player, string command, string arg1, string arg2) { return false; }
        public bool OnCardClicked(Player player, BaseCard card) { return false; }
        public bool OnRingClicked(Player player, Ring ring) { return false; }
        public void Initialize() { }
        public void Cleanup() { }

        // Property aliases for API compatibility
        public string WindowName => windowName;
        
        public string GetDebugInfo()
        {
            var playerInfo = currentPlayer != null ? $" ({currentPlayer.name})" : "";
            return $"ActionWindow{playerInfo} - {windowName} ({windowType}) - Action taken: {actionTaken}";
        }
    }
}
