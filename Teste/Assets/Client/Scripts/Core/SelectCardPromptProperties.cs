using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for configuring a card selection prompt
    /// </summary>
    [System.Serializable]
    public class SelectCardPromptProperties
    {
        [Header("Prompt Configuration")]
        public string activePromptTitle = "Select a card";
        public string waitingPromptTitle = "Waiting for opponent to select a card";
        public string controller = Players.Self;
        public string source = "";
        
        [Header("Selection Rules")]
        public int numCards = 1;
        public int maxCards = 1;
        public int minCards = 0;
        public bool multiSelect = false;
        public bool optional = false;
        
        [Header("Card Filtering")]
        public string location = Locations.Any;
        public string cardType = "";
        public List<string> cardTypes = new List<string>();
        public List<string> traits = new List<string>();
        public string faction = "";
        
        [Header("Callback Functions")]
        public System.Func<BaseCard, bool> cardCondition;
        public System.Func<Player, BaseCard, bool> onSelect;
        public System.Func<Player, List<BaseCard>, bool> onSelectMultiple;
        public System.Action<Player> onCancel;
        
        public SelectCardPromptProperties()
        {
            // Default card condition that accepts any card
            cardCondition = (card) => true;
            
            // Default single select callback
            onSelect = (player, card) => {
                Debug.Log($"{player.name} selected {card.name}");
                return true;
            };
        }
        
        /// <summary>
        /// Create a simple card selection prompt
        /// </summary>
        public static SelectCardPromptProperties Simple(string title, System.Func<BaseCard, bool> condition = null)
        {
            return new SelectCardPromptProperties
            {
                activePromptTitle = title,
                cardCondition = condition ?? ((card) => true)
            };
        }
        
        /// <summary>
        /// Create a multi-select card prompt
        /// </summary>
        public static SelectCardPromptProperties MultiSelect(string title, int minCards, int maxCards, System.Func<BaseCard, bool> condition = null)
        {
            return new SelectCardPromptProperties
            {
                activePromptTitle = title,
                minCards = minCards,
                maxCards = maxCards,
                multiSelect = true,
                cardCondition = condition ?? ((card) => true)
            };
        }
    }
}
