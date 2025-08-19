using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GameChat : MonoBehaviour
    {
        public List<string> messages = new List<string>();
        
        public void Initialize() { }
        public string FormatMessage(string message, params object[] args) => string.Format(message, args);
        public void AddMessage(string message) { messages.Add(message); }
        public void AddAlert(string type, string message) { messages.Add($"[{type}] {message}"); }
        public void AddChatMessage(Player player, string message) { messages.Add($"{player.name}: {message}"); }
    }
}