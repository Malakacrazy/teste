using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Action window step
    /// </summary>
    public partial class ActionWindow : MonoBehaviour, IGameStep
    {
        private Player currentPlayer;
        private Game game;
        private string windowName;
        private string windowType;
        private System.Action onComplete;
        private bool isComplete = false;
        private bool actionTaken = false;

        public ActionWindow() { }

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
        
        public bool IsComplete() => isComplete;
        public bool CanCancel => true;

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
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }

        public string GetDebugInfo()
        {
            var playerInfo = currentPlayer != null ? $" ({currentPlayer.name})" : "";
            return $"ActionWindow{playerInfo} - {windowName} ({windowType}) - Action taken: {actionTaken}";
        }
    }
}
