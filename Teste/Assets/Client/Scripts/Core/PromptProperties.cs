using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Properties for menu prompts
    /// </summary>
    [System.Serializable]
    public class MenuPromptProperties
    {
        public string activePromptTitle = "Choose an option";
        public string waitingPromptTitle = "Waiting for opponent";
        public List<string> choices = new List<string>();
        public Func<Player, string, bool> onSelect;
        public string source = "";
        public bool skippable = false;
        public string defaultChoice = "";
    }

    /// <summary>
    /// Properties for handler menu prompts
    /// </summary>
    [System.Serializable]
    public class HandlerMenuPromptProperties
    {
        public string activePromptTitle = "Choose an option";
        public string waitingPromptTitle = "Waiting for opponent";
        public List<MenuOption> choices = new List<MenuOption>();
        public Func<Player, object, bool> onSelect;
        public string source = "";
        public bool skippable = false;
    }

    /// <summary>
    /// Properties for card selection prompts
    /// </summary>
    [System.Serializable]
    public class SelectCardPromptProperties
    {
        public string activePromptTitle = "Select a card";
        public string waitingPromptTitle = "Waiting for opponent to select a card";
        public string controller = "self";
        public Func<BaseCard, bool> cardCondition;
        public Func<BaseCard, AbilityContext, bool> cardConditionContext;
        public string location = "";
        public List<string> locations = new List<string>();
        public int numCards = 1;
        public bool optional = false;
        public bool multiSelect = false;
        public Func<Player, BaseCard, bool> onSelect;
        public Func<Player, List<BaseCard>, bool> onSelectMultiple;
        public Action<Player> onCancel;
        public string source = "";
        public bool hideDisabledTargets = false;
        public Func<BaseCard, string> getPromptText;
    }

    /// <summary>
    /// Properties for ring selection prompts
    /// </summary>
    [System.Serializable]
    public class SelectRingPromptProperties
    {
        public string activePromptTitle = "Select a ring";
        public string waitingPromptTitle = "Waiting for opponent to select a ring";
        public Func<Ring, bool> ringCondition;
        public bool optional = false;
        public Func<Player, Ring, bool> onSelect;
        public Action<Player> onCancel;
        public string source = "";
        public Func<Ring, string> getPromptText;
    }

    /// <summary>
    /// Menu option for prompts
    /// </summary>
    [System.Serializable]
    public class MenuOption
    {
        public string text;
        public string command;
        public string arg;
        public bool disabled = false;
        public Func<Player, bool> handler;
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
        
        public MenuCommand(string cmd, string txt = "", string argument = "", bool isDisabled = false)
        {
            command = cmd;
            text = txt;
            arg = argument;
            disabled = isDisabled;
        }
    }
}
