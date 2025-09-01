using System;
using System.Collections.Generic;

namespace L5RGame
{
    [System.Serializable]
    public class CardMoveOptions
    {
        public bool reveal = true;
        public bool shuffle = false;
        public string placement = "top";
        public Player controller = null;
        
        public CardMoveOptions() { }
        
        public CardMoveOptions(Dictionary<string, object> properties)
        {
            if (properties != null)
            {
                if (properties.ContainsKey("reveal")) reveal = (bool)properties["reveal"];
                if (properties.ContainsKey("shuffle")) shuffle = (bool)properties["shuffle"];
                if (properties.ContainsKey("placement")) placement = (string)properties["placement"];
                if (properties.ContainsKey("controller")) controller = properties["controller"] as Player;
            }
        }
    }
}