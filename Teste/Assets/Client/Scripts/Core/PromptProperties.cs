using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Properties for handler menu prompts
    /// </summary>
    [System.Serializable]
    public class HandlerMenuPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public List<MenuOption> choices = new List<MenuOption>();
        public List<Action> handlers = new List<Action>();
        public bool canCancel = true;
        public float timeoutSeconds = 30f;
        
        // Additional properties for compatibility
        public AbilityContext context;
        public List<BaseCard> cards = new List<BaseCard>();
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        public System.Action<BaseCard> cardHandler;
        public System.Action<string> choiceHandler;
        public GameAction gameAction;
        public List<object> target = new List<object>();
        public bool targets;
    }

    /// <summary>
    /// Properties for select card prompts
    /// </summary>
    [System.Serializable]
    public class SelectCardPromptProperties
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
        public AbilityContext context;
        public string mode;
        public bool ordered;
        public string location;
        public System.Action<Player, List<BaseCard>> onSelectAction;
        public System.Action<Player> onCancel;
        public bool optional = false;
        public EffectSource source;
    }

    /// <summary>
    /// Properties for select ring prompts
    /// </summary>
    [System.Serializable]
    public class SelectRingPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public Func<Ring, bool> ringCondition;
        public Func<Player, Ring, bool> onSelect;
        public List<MenuOption> buttons = new List<MenuOption>();
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

    /// <summary>
    /// Ability types for triggered abilities
    /// </summary>
    public static class AbilityTypes
    {
        public const string Action = "action";
        public const string Reaction = "reaction";
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedreaction";
        public const string ForcedInterrupt = "forcedinterrupt";
        public const string WouldInterrupt = "wouldinterrupt";
        public const string CancelInterrupt = "cancelinterrupt";
        public const string Persistent = "persistent";
    }
}
