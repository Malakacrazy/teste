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
    /// Generic game event interface
    /// </summary>
    public interface IGameEvent
    {
        string Name { get; }
        bool cancelled { get; set; }
        void Cancel();
        bool Execute();
    }
    
    /// <summary>
    /// Base game event implementation
    /// </summary>
    public class GameEvent : IGameEvent
    {
        public string Name { get; set; }
        public string eventName { get; set; }
        public bool cancelled { get; set; }
        public Player player;
        public BaseCard card;
        public Ring ring;
        public Dictionary<string, object> context;
        
        public GameEvent()
        {
            Name = "Unknown";
            eventName = "Unknown";
            cancelled = false;
        }
        
        public GameEvent(string name)
        {
            Name = name;
            eventName = name;
            cancelled = false;
        }
        
        public virtual void Cancel()
        {
            cancelled = true;
        }
        
        public virtual bool Execute()
        {
            return !cancelled;
        }
    }
    
    /// <summary>
    /// Interface for ability windows
    /// </summary>
    public interface IAbilityWindow
    {
        string AbilityType { get; }
        List<object> Events { get; }
        event Action<IAbilityWindow> OnWindowClosed;
        
        void AddChoice(AbilityContext context);
        void Open();
        void Close();
    }
    
    /// <summary>
    /// Base triggered ability window
    /// </summary>
    public class TriggeredAbilityWindow : IAbilityWindow
    {
        public string AbilityType { get; private set; }
        public List<object> Events { get; private set; }
        public event Action<IAbilityWindow> OnWindowClosed;
        
        protected Game game;
        protected List<object> eventsToExclude;
        protected List<AbilityContext> choices = new List<AbilityContext>();
        
        public TriggeredAbilityWindow(Game gameInstance, string abilityType, List<object> events, List<object> excludedEvents)
        {
            game = gameInstance;
            AbilityType = abilityType;
            Events = events ?? new List<object>();
            eventsToExclude = excludedEvents ?? new List<object>();
        }
        
        public virtual void AddChoice(AbilityContext context)
        {
            if (context != null && !choices.Contains(context))
            {
                choices.Add(context);
            }
        }
        
        public virtual void Open()
        {
            // Placeholder - implement window opening logic
            Debug.Log($"Opening {AbilityType} window with {choices.Count} choices");
        }
        
        public virtual void Close()
        {
            // Placeholder - implement window closing logic
            Debug.Log($"Closing {AbilityType} window");
            OnWindowClosed?.Invoke(this);
        }
        
        public virtual void Pass(Player player)
        {
            // Player passes on this window
            Debug.Log($"{player.name} passes on {AbilityType} window");
            Close();
        }
    }
    
    /// <summary>
    /// Forced triggered ability window (for forced reactions/interrupts)
    /// </summary>
    public class ForcedTriggeredAbilityWindow : TriggeredAbilityWindow
    {
        public ForcedTriggeredAbilityWindow(Game gameInstance, string abilityType, List<object> events, List<object> excludedEvents)
            : base(gameInstance, abilityType, events, excludedEvents)
        {
        }
        
        public override void Open()
        {
            // Forced abilities must be resolved
            Debug.Log($"Opening forced {AbilityType} window - must resolve");
            base.Open();
        }
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
    /// Ring class for conflict resolution
    /// </summary>
    [System.Serializable]
    public class Ring : EffectSource
    {
        [Header("Ring Properties")]
        public string element;
        public string conflictType;
        public bool claimed = false;
        public Player claimedBy;
        public List<BaseCard> attachments = new List<BaseCard>();
        
        public Ring(Game game, string ringElement, string initialConflictType)
        {
            Initialize(game, $"{ringElement} Ring");
            element = ringElement;
            conflictType = initialConflictType;
        }
        
        public void FlipConflictType()
        {
            conflictType = conflictType == ConflictTypes.Military ? ConflictTypes.Political : ConflictTypes.Military;
            game.AddMessage("{0} ring flipped to {1}", element, conflictType);
        }
        
        public void ClaimRing(Player player)
        {
            claimed = true;
            claimedBy = player;
            game.AddMessage("{0} claims the {1} ring", player, element);
        }
        
        public void ResetRing()
        {
            claimed = false;
            claimedBy = null;
        }
        
        public bool IsContested()
        {
            return false; // Placeholder
        }
        
        public List<BaseCard> GetAttachments()
        {
            return attachments.ToList();
        }
        
        public void AddAttachment(BaseCard attachment)
        {
            if (!attachments.Contains(attachment))
            {
                attachments.Add(attachment);
            }
        }
        
        public void RemoveAttachment(BaseCard attachment)
        {
            attachments.Remove(attachment);
        }
    }
    
    /// <summary>
    /// Faction information
    /// </summary>
    [System.Serializable]
    public class Faction
    {
        public string name;
        public string id;
        public string color;
        public string emblem;
        
        public Faction() { }
        
        public Faction(string factionName, string factionId)
        {
            name = factionName;
            id = factionId;
        }
    }
    
    /// <summary>
    /// Deck information
    /// </summary>
    [System.Serializable]
    public class Deck
    {
        public string name;
        public string id;
        public Faction faction;
        public List<BaseCard> cards = new List<BaseCard>();
        public BaseCard stronghold;
        public BaseCard role;
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
        
        public Deck() { }
        
        public Deck(string deckName, string deckId)
        {
            name = deckName;
            id = deckId;
        }
    }
    
    /// <summary>
    /// Conflict opportunities tracking
    /// </summary>
    [System.Serializable]
    public class ConflictOpportunities
    {
        public int military = 0;
        public int political = 0;  
        public int total = 0;
        
        public void Reset()
        {
            military = 0;
            political = 0;
            total = 0;
        }
    }
    
    /// <summary>
    /// Game phases enumeration (matches GameConstants)
    /// </summary>
    public static class GamePhases
    {
        public const string Setup = "setup";
        public const string Dynasty = "dynasty";
        public const string Draw = "draw";
        public const string Conflict = "conflict";
        public const string Fate = "fate";
        public const string Regroup = "regroup";
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
    
    /// <summary>
    /// Base interface for card abilities
    /// </summary>
    public interface CardAbility
    {
        string Title { get; }
        string AbilityType { get; }
        AbilityLimit Limit { get; }
        bool CanExecute(AbilityContext context);
        void Execute(AbilityContext context);
    }
}
