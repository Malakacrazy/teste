using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Represents a menu command that can be executed on cards or game objects
    /// </summary>
    [System.Serializable]
    public class MenuCommand
    {
        public string command;
        public string text;
        public string arg;
        public string uuid;
        public string method;
        public bool disabled = false;
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public MenuCommand() { }

        public MenuCommand(string command, string text, string arg = null, string method = null)
        {
            this.command = command;
            this.text = text;
            this.arg = arg;
            this.method = method;
        }

        public bool CanExecute(Player player, object target)
        {
            if (disabled) return false;
            
            // Add any additional conditions here
            return true;
        }

        public void Execute(Game game, Player player, object target)
        {
            // Execute the command logic
            switch (command)
            {
                case "click":
                    // Handle click command
                    break;
                case "play":
                    // Handle play command  
                    break;
                case "activate":
                    // Handle activate command
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"Unknown menu command: {command}");
                    break;
            }
        }
    }
}
