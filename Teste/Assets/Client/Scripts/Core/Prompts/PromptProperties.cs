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

    /// <summary>
    /// Helper methods for creating prompt properties
    /// </summary>
    public static class PromptPropertiesHelper
    {
        /// <summary>
        /// Create a simple card selection prompt
        /// </summary>
        public static SelectCardPromptProperties CreateSelectCard(string title, Func<BaseCard, bool> cardCondition, Func<Player, BaseCard, bool> onSelect)
        {
            return new SelectCardPromptProperties
            {
                activePromptTitle = title,
                cardCondition = cardCondition,
                onSelect = onSelect
            };
        }

        /// <summary>
        /// Create a simple ring selection prompt
        /// </summary>
        public static SelectRingPromptProperties CreateSelectRing(string title, Func<Ring, bool> ringCondition, Func<Player, Ring, bool> onSelect)
        {
            return new SelectRingPromptProperties
            {
                activePromptTitle = title,
                ringCondition = ringCondition,
                onSelect = onSelect
            };
        }

        /// <summary>
        /// Create a simple menu prompt
        /// </summary>
        public static HandlerMenuPromptProperties CreateMenu(string title, List<MenuOption> choices, List<Action> handlers)
        {
            return new HandlerMenuPromptProperties
            {
                activePromptTitle = title,
                choices = choices ?? new List<MenuOption>(),
                handlers = handlers ?? new List<Action>()
            };
        }
    }

    /// <summary>
    /// Extension methods for prompt properties
    /// </summary>
    public static class PromptPropertiesExtensions
    {
        /// <summary>
        /// Set the source for a prompt
        /// </summary>
        public static T WithSource<T>(this T properties, EffectSource source) where T : PromptProperties
        {
            properties.source = source;
            return properties;
        }

        /// <summary>
        /// Set the context for a prompt
        /// </summary>
        public static T WithContext<T>(this T properties, AbilityContext context) where T : PromptProperties
        {
            properties.context = context;
            return properties;
        }

        /// <summary>
        /// Set whether the prompt allows cancellation
        /// </summary>
        public static T WithCancellation<T>(this T properties, bool canCancel) where T : PromptProperties
        {
            properties.canCancel = canCancel;
            return properties;
        }

        /// <summary>
        /// Make a card selection prompt optional
        /// </summary>
        public static SelectCardPromptProperties WithOptional(this SelectCardPromptProperties properties, bool optional = true)
        {
            properties.optional = optional;
            return properties;
        }

        /// <summary>
        /// Set the number of cards to select
        /// </summary>
        public static SelectCardPromptProperties WithNumCards(this SelectCardPromptProperties properties, int numCards)
        {
            properties.numCards = numCards;
            return properties;
        }

        /// <summary>
        /// Enable multi-select for card selection
        /// </summary>
        public static SelectCardPromptProperties WithMultiSelect(this SelectCardPromptProperties properties, bool multiSelect = true)
        {
            properties.multiSelect = multiSelect;
            return properties;
        }

        /// <summary>
        /// Set the card type filter
        /// </summary>
        public static SelectCardPromptProperties WithCardType(this SelectCardPromptProperties properties, string cardType)
        {
            properties.cardType = cardType;
            return properties;
        }
    }

}
