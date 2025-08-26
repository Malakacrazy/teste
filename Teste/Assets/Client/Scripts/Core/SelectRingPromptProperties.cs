using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for configuring a ring selection prompt
    /// </summary>
    [System.Serializable]
    public class SelectRingPromptProperties
    {
        [Header("Prompt Configuration")]
        public string activePromptTitle = "Select a ring";
        public string waitingPromptTitle = "Waiting for opponent to select a ring";
        public string controller = Players.Self;
        public string source = "";
        
        [Header("Ring Filtering")]
        public List<string> ringCondition = new List<string>();
        public bool onlyUnclaimed = false;
        public bool onlyClaimed = false;
        public bool onlyContested = false;
        
        [Header("Callback Functions")]
        public System.Func<Ring, bool> condition;
        public System.Func<Player, Ring, bool> onSelect;
        public System.Action<Player> onCancel;
        
        public SelectRingPromptProperties()
        {
            // Default ring condition that accepts any ring
            condition = (ring) => true;
            
            // Default select callback
            onSelect = (player, ring) => {
                Debug.Log($"{player.name} selected {ring.name} ring");
                return true;
            };
        }
        
        /// <summary>
        /// Create a simple ring selection prompt
        /// </summary>
        public static SelectRingPromptProperties Simple(string title, System.Func<Ring, bool> condition = null)
        {
            return new SelectRingPromptProperties
            {
                activePromptTitle = title,
                condition = condition ?? ((ring) => true)
            };
        }
    }
}
