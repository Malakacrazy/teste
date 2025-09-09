using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Base properties for all prompts
    /// </summary>
    [System.Serializable]
    public class PromptProperties
    {
        public string promptTitle;
        public string waitingPromptTitle;
        public string activePromptTitle;
        public string controller;
        public EffectSource source;
        public AbilityContext context;
        public PromptInfo activePrompt;
        public bool canCancel = true;
        public float timeoutSeconds = 30f;
        
        // Common properties used by many prompt types
        public GameAction gameAction;
        public System.Action<Player, List<BaseCard>> onSelect;
        public System.Func<bool> onCancel;
        public System.Func<Player, string, bool> onMenuCommand;
        public List<MenuOption> buttons = new List<MenuOption>();
        public List<object> controls = new List<object>();
        public List<BaseCard> cards = new List<BaseCard>();
        public List<MenuOption> choices = new List<MenuOption>();
        public List<object> target = new List<object>();
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        public Func<Ring, bool> ringCondition;
        public BaseCardSelector selector;
        public List<BaseCard> mustSelect = new List<BaseCard>();
        public bool optional = false;
        public bool ordered = false;
        public System.Action<BaseCard> selectCard;
        public System.Action<BaseCard> onCardToggle;
        public System.Action<BaseCard> cardHandler;
        public System.Action<string> choiceHandler;
        public List<Action> handlers = new List<Action>();
        public bool targets = false;
    }

    /// <summary>
    /// Properties for menu prompts
    /// </summary>
    [System.Serializable]
    public class MenuPromptProperties : PromptProperties
    {
        public List<MenuOption> choices = new List<MenuOption>();
        public System.Action<Player, string> choiceHandler;
        public System.Action onCancel;
    }

    /// <summary>
    /// Properties for handler menu prompts
    /// </summary>
    [System.Serializable]
    public class HandlerMenuPromptProperties : PromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public string controller;
        public List<MenuOption> choices = new List<MenuOption>();
        public List<Action> handlers = new List<Action>();
        public bool canCancel = true;
        public float timeoutSeconds = 30f;
        
        // Additional properties for compatibility
        public List<BaseCard> cards = new List<BaseCard>();
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        public System.Action<BaseCard> cardHandler;
        public System.Action<string> choiceHandler;
        public GameAction gameAction;
        public List<object> target = new List<object>();
        public bool targets;
        public List<object> controls = new List<object>();
        public System.Action<Player, List<BaseCard>> onSelect;
    }

    /// <summary>
    /// Properties for select card prompts
    /// </summary>
    [System.Serializable]
    public class SelectCardPromptProperties : PromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public string cardType;
        public string controller;
        public bool multiSelect = false;
        public int numCards = 1;
        public Func<BaseCard, bool> cardCondition;
        public Func<Player, BaseCard, bool> onSelect;
        public Func<Player, List<BaseCard>, bool> onSelectMultiple;
        public List<MenuOption> buttons = new List<MenuOption>();
        
        // Additional properties for compatibility
        public string mode;
        public bool ordered;
        public string location;
        public System.Action<Player, List<BaseCard>> onSelectAction;
        public System.Func<bool> onCancel;
        public System.Func<Player, string, bool> onMenuCommand;
        public BaseCardSelector selector;
        public List<BaseCard> mustSelect = new List<BaseCard>();
        public bool optional = false;
        public GameAction gameAction;
        public List<object> controls = new List<object>();
        public System.Action<BaseCard> selectCard;
        public System.Action<BaseCard> onCardToggle;
    }

    /// <summary>
    /// Properties for select ring prompts
    /// </summary>
    [System.Serializable]
    public class SelectRingPromptProperties : PromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public Func<Ring, bool> ringCondition;
        public Func<Player, Ring, bool> onSelect;
        public List<MenuOption> buttons = new List<MenuOption>();
        public List<object> controls = new List<object>();
        public bool optional = false;
        public bool ordered = false;
        public System.Func<bool> onCancel;
        public System.Func<Player, string, bool> onMenuCommand;
    }

    /// <summary>
    /// Menu option for prompts
    /// </summary>
    [System.Serializable]
    public class MenuOption
    {
        public string text;
        public string arg;
        public string method;
        public bool disabled = false;
    }

}
