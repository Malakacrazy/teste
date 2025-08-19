using UnityEngine;

namespace L5RGame
{
    public class ChatCommands : MonoBehaviour
    {
        public void Initialize(Game game) { }
        public bool ExecuteCommand(Player player, string command, string[] args) => false;
        public void Manual(string playerName) { }
    }
}