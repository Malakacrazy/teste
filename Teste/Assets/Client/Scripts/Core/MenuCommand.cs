using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a menu command that can be selected by a player
    /// </summary>
    public class MenuCommand
    {
        [Header("Command Properties")]
        public string command;
        public string text;
        public object arg;
        public string uuid;
        public string method;
        public bool disabled = false;

        public MenuCommand()
        {
        }

        public MenuCommand(string command, string text, object arg = null, string uuid = null, string method = null)
        {
            this.command = command;
            this.text = text;
            this.arg = arg;
            this.uuid = uuid;
            this.method = method;
        }
    }
}
