using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for menu prompts
    /// </summary>
    [System.Serializable]
    public class MenuPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public List<string> buttons;
        public System.Func<Player, string, bool> onSelect;
        public string source;
    }
    
    /// <summary>
    /// Properties for handler menu prompts
    /// </summary>
    [System.Serializable] 
    public class HandlerMenuPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public List<object> choices;
        public System.Func<Player, object, bool> onSelect;
        public string source;
    }
    
    /// <summary>
    /// Properties for card selection prompts
    /// </summary>
    [System.Serializable]
    public class SelectCardPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public string controller;
        public System.Func<BaseCard, bool> cardCondition;
        public System.Func<Player, BaseCard, bool> onSelect;
        public string source;
        public int numCards = 1;
        public bool optional = false;
        public string mode = "single";
        public List<string> location;
    }
    
    /// <summary>
    /// Properties for ring selection prompts  
    /// </summary>
    [System.Serializable]
    public class SelectRingPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public string controller;
        public System.Func<Ring, bool> ringCondition;
        public System.Func<Player, Ring, bool> onSelect;
        public string source;
        public bool optional = false;
    }
    

    

    

    
    /// <summary>
    /// Menu command structure
    /// </summary>
    [System.Serializable]
    public class MenuCommand
    {
        public string command;
        public string text;
        public string arg;
        public bool disabled;
        public Dictionary<string, object> properties;
        
        public MenuCommand()
        {
            properties = new Dictionary<string, object>();
        }
        
        public MenuCommand(string cmd, string txt, string argument = null)
        {
            command = cmd;
            text = txt;
            arg = argument;
            disabled = false;
            properties = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Helper class for menu command processing
    /// </summary>
    public static class MenuCommandsHelper
    {
        public static void CardMenuClick(MenuCommand menuItem, Game game, Player player, BaseCard card)
        {
            // Placeholder implementation for card menu clicks
            Debug.Log($"Card menu click: {menuItem.command} on {card.name} by {player.name}");
        }
        
        public static void RingMenuClick(MenuCommand menuItem, Game game, Player player, Ring ring)
        {
            // Placeholder implementation for ring menu clicks
            Debug.Log($"Ring menu click: {menuItem.command} on {ring.element} ring by {player.name}");
        }
    }
    

    

    

    

    

    
    /// <summary>
    /// Card ability type enumeration
    /// </summary>
    public static class CardAbilityTypes
    {
        public const string Action = "action";
        public const string Reaction = "reaction";
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedreaction";
        public const string ForcedInterrupt = "forcedinterrupt";
        public const string Persistent = "persistent";
    }
    

}
