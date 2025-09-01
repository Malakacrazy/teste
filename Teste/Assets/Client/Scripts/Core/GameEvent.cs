using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Interface for game events
    /// </summary>
    public interface IGameEvent
    {
        string Name { get; }
        BaseCard Card { get; }
        Ring Ring { get; }
        string Phase { get; }
        AbilityContext Context { get; }
        bool cancelled { get; set; }
        Dictionary<string, object> Parameters { get; }
        void Cancel();
        bool IsCancelled();
        bool IsResolved();
        void Execute();
    }
    
    /// <summary>
    /// Represents a game event in the L5R event system.
    /// Events can be cancelled, have conditions checked, and execute handlers with proper timing.
    /// </summary>
    public class GameEvent : IGameEvent
    {
        [Header("Event Configuration")]
        [SerializeField] protected string eventName;
        [SerializeField] protected bool isCancelled = false;
        [SerializeField] protected bool isResolved = false;
        [SerializeField] protected bool isContingent = false;
        [SerializeField] protected int executionOrder = 0;
        
        // Event properties
        protected Dictionary<string, object> properties = new Dictionary<string, object>();
        protected System.Action<GameEvent> handler;
        protected System.Func<GameEvent, bool> condition;
        protected System.Func<GameEvent, bool> checkFullyResolvedFunc;
        protected System.Func<List<GameEvent>> createContingentEventsFunc;
        protected System.Action preResolutionEffectFunc;
        
        // Event relationships
        protected EventWindow eventWindow;
        protected AbilityContext contextObject;
        protected GameEvent replacementEventObject;
        
        // Event metadata
        protected DateTime createdTime;
        protected string sourceDescription;
        
        // Events
        public event Action<GameEvent> OnEventCancelled;
        public event Action<GameEvent> OnEventResolved;
        public event Action<GameEvent, string> OnPropertyChanged;
        
        #region Properties
        
        /// <summary>
        /// Name of this event
        /// </summary>
        public string name
        {
            get => eventName;
            set
            {
                eventName = value;
                OnPropertyChanged?.Invoke(this, nameof(name));
            }
        }
        
        /// <summary>
        /// Whether this event has been cancelled
        /// </summary>
        public bool cancelled
        {
            get => isCancelled;
            set
            {
                if (isCancelled != value)
                {
                    isCancelled = value;
                    if (value)
                    {
                        OnEventCancelled?.Invoke(this);
                    }
                    OnPropertyChanged?.Invoke(this, nameof(cancelled));
                }
            }
        }
        
        /// <summary>
        /// Whether this event has been resolved
        /// </summary>
        public bool resolved
        {
            get => isResolved;
            protected set
            {
                if (isResolved != value)
                {
                    isResolved = value;
                    if (value)
                    {
                        OnEventResolved?.Invoke(this);
                    }
                    OnPropertyChanged?.Invoke(this, nameof(resolved));
                }
            }
        }
        
        /// <summary>
        /// Execution order for this event (lower numbers execute first)
        /// </summary>
        public int order
        {
            get => executionOrder;
            set
            {
                executionOrder = value;
                OnPropertyChanged?.Invoke(this, nameof(order));
            }
        }
        
        /// <summary>
        /// Whether this is a contingent event
        /// </summary>
        public bool IsContingent
        {
            get => isContingent;
            set => isContingent = value;
        }
        
        /// <summary>
        /// The event window containing this event
        /// </summary>
        public EventWindow window
        {
            get => eventWindow;
            set => eventWindow = value;
        }
        
        /// <summary>
        /// Ability context associated with this event
        /// </summary>
        public AbilityContext context
        {
            get => contextObject;
            set => contextObject = value;
        }
        
        /// <summary>
        /// Event that replaces this one if it gets replaced
        /// </summary>
        public GameEvent replacementEvent
        {
            get => replacementEventObject;
            set => replacementEventObject = value;
        }
        
        /// <summary>
        /// Time when this event was created
        /// </summary>
        public DateTime CreatedTime => createdTime;
        
        /// <summary>
        /// Description of what created this event
        /// </summary>
        public string SourceDescription
        {
            get => sourceDescription;
            set => sourceDescription = value;
        }
        
        /// <summary>
        /// All properties of this event
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => properties;
        
        /// <summary>
        /// Player associated with this event
        /// </summary>
        public Player Player => properties.ContainsKey("player") ? properties["player"] as Player : null;
        
        /// <summary>
        /// Resolver for this event
        /// </summary>
        public object Resolver => properties.ContainsKey("resolver") ? properties["resolver"] : null;
        
        /// <summary>
        /// Play type for this event
        /// </summary>
        public string PlayType => properties.ContainsKey("playType") ? properties["playType"] as string : null;
        
        /// <summary>
        /// Whether this event has been cancelled (uppercase alias for cancelled)
        /// </summary>
        public bool Cancelled => cancelled;
        
        /// <summary>
        /// Execution order (uppercase alias for order)
        /// </summary>
        public int Order => order;
        
        /// <summary>
        /// Whether this event has a handler
        /// </summary>
        public bool HasHandler => handler != null;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Create a new game event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Initial properties</param>
        /// <param name="handler">Event handler</param>
        public GameEvent(string eventName, Dictionary<string, object> properties = null, System.Action<GameEvent> handler = null)
        {
            this.eventName = eventName ?? EventNames.Unnamed;
            this.handler = handler;
            this.properties = properties ?? new Dictionary<string, object>();
            this.createdTime = DateTime.UtcNow;
            
            InitializeDefaults();
        }
        
        /// <summary>
        /// Initialize default values
        /// </summary>
        protected virtual void InitializeDefaults()
        {
            // Default condition - always true unless event is cancelled, resolved, or unnamed
            condition = (gameEvent) =>
            {
                if (gameEvent.cancelled || gameEvent.resolved || gameEvent.name == EventNames.Unnamed)
                    return false;
                return true;
            };
            
            // Default fully resolved check
            checkFullyResolvedFunc = (gameEvent) => !gameEvent.cancelled;
            
            // Default contingent events creation
            createContingentEventsFunc = () => new List<GameEvent>();
            
            // Default pre-resolution effect
            preResolutionEffectFunc = () => { };
        }
        
        #endregion
        
        #region Event Management
        
        /// <summary>
        /// Cancel this event
        /// </summary>
        public virtual void Cancel()
        {
            if (cancelled || resolved)
                return;
            
            cancelled = true;
            
            // Remove from event window if present
            if (eventWindow != null)
            {
                eventWindow.RemoveEvent(this);
            }
            
            LogEvent($"Event cancelled: {name}");
        }
        
        /// <summary>
        /// Set the event window for this event
        /// </summary>
        /// <param name="window">Event window</param>
        public virtual void SetWindow(EventWindow window)
        {
            eventWindow = window;
        }
        
        /// <summary>
        /// Remove event window reference
        /// </summary>
        public virtual void UnsetWindow()
        {
            eventWindow = null;
        }
        
        /// <summary>
        /// Check if this event's condition is still met
        /// </summary>
        public virtual void CheckCondition()
        {
            if (cancelled || resolved || name == EventNames.Unnamed)
                return;
            
            if (condition != null && !condition(this))
            {
                LogEvent($"Event condition failed, cancelling: {name}");
                Cancel();
            }
        }
        
        /// <summary>
        /// Get the final resolution event (follows replacement chain)
        /// </summary>
        /// <returns>Final event in replacement chain</returns>
        public virtual GameEvent GetResolutionEvent()
        {
            if (replacementEvent != null)
            {
                return replacementEvent.GetResolutionEvent();
            }
            return this;
        }
        
        /// <summary>
        /// Check if this event is fully resolved
        /// </summary>
        /// <returns>True if fully resolved</returns>
        public virtual bool IsFullyResolved()
        {
            var resolutionEvent = GetResolutionEvent();
            return checkFullyResolvedFunc?.Invoke(resolutionEvent) ?? !resolutionEvent.cancelled;
        }
        
        /// <summary>
        /// Execute the event handler
        /// </summary>
        public virtual void ExecuteHandler()
        {
            if (cancelled)
            {
                LogEvent($"Attempted to execute cancelled event: {name}");
                return;
            }
            
            if (resolved)
            {
                LogEvent($"Attempted to execute already resolved event: {name}");
                return;
            }
            
            resolved = true;
            
            try
            {
                LogEvent($"Executing event handler: {name}");
                handler?.Invoke(this);
            }
            catch (Exception ex)
            {
                LogError($"Error executing event handler for {name}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Replace the event handler
        /// </summary>
        /// <param name="newHandler">New handler to use</param>
        public virtual void ReplaceHandler(System.Action<GameEvent> newHandler)
        {
            handler = newHandler;
            LogEvent($"Event handler replaced for: {name}");
        }
        
        /// <summary>
        /// Set the condition function
        /// </summary>
        /// <param name="conditionFunc">Condition function</param>
        public virtual void SetCondition(System.Func<GameEvent, bool> conditionFunc)
        {
            condition = conditionFunc;
        }
        
        /// <summary>
        /// Set the fully resolved check function
        /// </summary>
        /// <param name="checkFunc">Check function</param>
        public virtual void SetCheckFullyResolvedFunc(System.Func<GameEvent, bool> checkFunc)
        {
            checkFullyResolvedFunc = checkFunc;
        }
        
        /// <summary>
        /// Set the contingent events creation function
        /// </summary>
        /// <param name="createFunc">Creation function</param>
        public virtual void SetCreateContingentEventsFunc(System.Func<List<GameEvent>> createFunc)
        {
            createContingentEventsFunc = createFunc;
        }
        
        /// <summary>
        /// Set the pre-resolution effect
        /// </summary>
        /// <param name="effectFunc">Effect function</param>
        public virtual void SetPreResolutionEffect(System.Action effectFunc)
        {
            preResolutionEffectFunc = effectFunc;
        }
        
        #endregion
        
        #region Event Chain Management
        
        /// <summary>
        /// Create contingent events based on this event
        /// </summary>
        /// <returns>List of contingent events</returns>
        public virtual List<GameEvent> CreateContingentEvents()
        {
            try
            {
                var contingentEvents = createContingentEventsFunc?.Invoke() ?? new List<GameEvent>();
                
                // Mark contingent events
                foreach (var contingentEvent in contingentEvents)
                {
                    contingentEvent.IsContingent = true;
                    contingentEvent.SourceDescription = $"Contingent from {name}";
                }
                
                if (contingentEvents.Count > 0)
                {
                    LogEvent($"Created {contingentEvents.Count} contingent events from {name}");
                }
                
                return contingentEvents;
            }
            catch (Exception ex)
            {
                LogError($"Error creating contingent events for {name}: {ex.Message}");
                return new List<GameEvent>();
            }
        }
        
        /// <summary>
        /// Execute pre-resolution effect
        /// </summary>
        public virtual void PreResolutionEffect()
        {
            try
            {
                preResolutionEffectFunc?.Invoke();
            }
            catch (Exception ex)
            {
                LogError($"Error in pre-resolution effect for {name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Replace this event with another event
        /// </summary>
        /// <param name="newEvent">Replacement event</param>
        public virtual void ReplaceWith(GameEvent newEvent)
        {
            if (newEvent == null)
            {
                LogError("Attempted to replace event with null");
                return;
            }
            
            replacementEventObject = newEvent;
            newEvent.context = contextObject;
            newEvent.window = eventWindow;
            
            LogEvent($"Event {name} replaced with {newEvent.name}");
        }
        
        #endregion
        
        #region Property Management
        
        /// <summary>
        /// Add a property to this event
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        public virtual void AddProperty(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            
            properties[key] = value;
            OnPropertyChanged?.Invoke(this, key);
        }
        
        /// <summary>
        /// Get a property value
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Property value or null if not found</returns>
        public virtual object GetProperty(string key)
        {
            return properties.TryGetValue(key, out var value) ? value : null;
        }
        
        /// <summary>
        /// Get a property value with type conversion
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="defaultValue">Default value if not found or conversion fails</param>
        /// <returns>Property value or default</returns>
        public virtual T GetProperty<T>(string key, T defaultValue = default(T))
        {
            var value = GetProperty(key);
            
            if (value == null)
                return defaultValue;
            
            try
            {
                if (value is T directValue)
                    return directValue;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Set a property value
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        public virtual void SetProperty(string key, object value)
        {
            AddProperty(key, value);
        }
        
        /// <summary>
        /// Remove a property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property was removed</returns>
        public virtual bool RemoveProperty(string key)
        {
            if (properties.Remove(key))
            {
                OnPropertyChanged?.Invoke(this, key);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Check if property exists
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property exists</returns>
        public virtual bool HasProperty(string key)
        {
            return properties.ContainsKey(key);
        }
        
        /// <summary>
        /// Clear all properties
        /// </summary>
        public virtual void ClearProperties()
        {
            properties.Clear();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if this event can be cancelled
        /// </summary>
        /// <returns>True if event can be cancelled</returns>
        public virtual bool CanBeCancelled()
        {
            return !cancelled && !resolved && 
                   (!HasProperty("cannotBeCancelled") || !GetProperty<bool>("cannotBeCancelled"));
        }
        
        /// <summary>
        /// Get involved cards from this event
        /// </summary>
        /// <returns>List of cards involved in this event</returns>
        public virtual List<BaseCard> GetInvolvedCards()
        {
            var cards = new List<BaseCard>();
            
            // Check common property names for cards
            var cardProperties = new[] { "card", "cards", "target", "targets", "source", "participant", "participants" };
            
            foreach (var propName in cardProperties)
            {
                var value = GetProperty(propName);
                
                if (value is BaseCard card)
                {
                    cards.Add(card);
                }
                else if (value is IEnumerable<BaseCard> cardList)
                {
                    cards.AddRange(cardList);
                }
                else if (value is IEnumerable<object> objectList)
                {
                    cards.AddRange(objectList.OfType<BaseCard>());
                }
            }
            
            return cards.Distinct().ToList();
        }
        
        /// <summary>
        /// Get involved players from this event
        /// </summary>
        /// <returns>List of players involved in this event</returns>
        public virtual List<Player> GetInvolvedPlayers()
        {
            var players = new List<Player>();
            
            // Check common property names for players
            var playerProperties = new[] { "player", "players", "attacker", "defender", "controller", "owner" };
            
            foreach (var propName in playerProperties)
            {
                var value = GetProperty(propName);
                
                if (value is Player player)
                {
                    players.Add(player);
                }
                else if (value is IEnumerable<Player> playerList)
                {
                    players.AddRange(playerList);
                }
            }
            
            // Also check cards for their controllers
            var cards = GetInvolvedCards();
            foreach (var card in cards)
            {
                if (card.controller != null)
                    players.Add(card.controller);
            }
            
            return players.Distinct().ToList();
        }
        
        /// <summary>
        /// Check if this event involves a specific card
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if card is involved</returns>
        public virtual bool InvolveCard(BaseCard card)
        {
            if (card == null)
                return false;
            
            return GetInvolvedCards().Contains(card);
        }
        
        /// <summary>
        /// Check if this event involves a specific player
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if player is involved</returns>
        public virtual bool InvolvePlayer(Player player)
        {
            if (player == null)
                return false;
            
            return GetInvolvedPlayers().Contains(player);
        }
        
        /// <summary>
        /// Get a summary description of this event
        /// </summary>
        /// <returns>Event summary</returns>
        public virtual string GetSummary()
        {
            var summary = $"Event: {name}";
            
            if (cancelled)
                summary += " [CANCELLED]";
            else if (resolved)
                summary += " [RESOLVED]";
            
            var involvedCards = GetInvolvedCards();
            if (involvedCards.Count > 0)
            {
                summary += $" (Cards: {string.Join(", ", involvedCards.Select(c => c.name))})";
            }
            
            var involvedPlayers = GetInvolvedPlayers();
            if (involvedPlayers.Count > 0)
            {
                summary += $" (Players: {string.Join(", ", involvedPlayers.Select(p => p.name))})";
            }
            
            return summary;
        }
        
        #endregion
        
        #region IGameEvent Implementation
        
        public string Name => name;
        public BaseCard Card => GetProperty("card") as BaseCard;
        public Ring Ring => GetProperty("ring") as Ring;
        public string Phase => GetProperty("phase") as string;
        public AbilityContext Context => context;
        public Dictionary<string, object> Parameters => properties;
        // cancelled property already defined above
        public bool IsCancelled() => isCancelled;
        public bool IsResolved() => isResolved;
        public void Execute() => ExecuteHandler();
        
        #endregion
        
        #region Logging
        
        /// <summary>
        /// Log event message
        /// </summary>
        /// <param name="message">Message to log</param>
        protected virtual void LogEvent(string message)
        {
            Debug.Log($"📅 GameEvent: {message}");
        }
        
        /// <summary>
        /// Log event error
        /// </summary>
        /// <param name="message">Error message to log</param>
        protected virtual void LogError(string message)
        {
            Debug.LogError($"📅❌ GameEvent Error: {message}");
        }
        
        #endregion
        
        #region Operators and Utility
        
        public override string ToString()
        {
            return GetSummary();
        }
        
        public override bool Equals(object obj)
        {
            return obj is GameEvent other && 
                   name == other.name && 
                   createdTime == other.createdTime;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(name, createdTime);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a simple event with just a name
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <returns>New game event</returns>
        public static GameEvent Create(string eventName)
        {
            return new GameEvent(eventName);
        }
        
        /// <summary>
        /// Create an event with properties
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Event properties</param>
        /// <returns>New game event</returns>
        public static GameEvent Create(string eventName, Dictionary<string, object> properties)
        {
            return new GameEvent(eventName, properties);
        }
        
        /// <summary>
        /// Create an event with a handler
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="handler">Event handler</param>
        /// <returns>New game event</returns>
        public static GameEvent Create(string eventName, System.Action<GameEvent> handler)
        {
            return new GameEvent(eventName, null, handler);
        }
        
        /// <summary>
        /// Create an event with properties and handler
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Event properties</param>
        /// <param name="handler">Event handler</param>
        /// <returns>New game event</returns>
        public static GameEvent Create(string eventName, Dictionary<string, object> properties, System.Action<GameEvent> handler)
        {
            return new GameEvent(eventName, properties, handler);
        }
        
        #endregion
    }
    

    
    /// <summary>
    /// Extension methods for GameEvent
    /// </summary>
    public static class GameEventExtensions
    {
        /// <summary>
        /// Check if event is active (not cancelled or resolved)
        /// </summary>
        public static bool IsActive(this GameEvent gameEvent)
        {
            return !gameEvent.cancelled && !gameEvent.resolved;
        }
        
        /// <summary>
        /// Check if event is completed (cancelled or resolved)
        /// </summary>
        public static bool IsCompleted(this GameEvent gameEvent)
        {
            return gameEvent.cancelled || gameEvent.resolved;
        }
        
        /// <summary>
        /// Get event age in seconds
        /// </summary>
        public static double GetAgeInSeconds(this GameEvent gameEvent)
        {
            return (DateTime.UtcNow - gameEvent.CreatedTime).TotalSeconds;
        }
        
        /// <summary>
        /// Add multiple properties at once
        /// </summary>
        public static void AddProperties(this GameEvent gameEvent, Dictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    gameEvent.AddProperty(kvp.Key, kvp.Value);
                }
            }
        }
        
        /// <summary>
        /// Clone this event with a new name
        /// </summary>
        public static GameEvent CloneWithName(this GameEvent gameEvent, string newName)
        {
            var clone = new GameEvent(newName, new Dictionary<string, object>(gameEvent.Properties));
            clone.order = gameEvent.order;
            clone.context = gameEvent.context;
            clone.IsContingent = gameEvent.IsContingent;
            clone.SourceDescription = $"Clone of {gameEvent.name}";
            
            return clone;
        }
        
        /// <summary>
        /// Check if event has any of the specified properties
        /// </summary>
        public static bool HasAnyProperty(this GameEvent gameEvent, params string[] propertyNames)
        {
            return propertyNames.Any(prop => gameEvent.HasProperty(prop));
        }
        
        /// <summary>
        /// Check if event has all of the specified properties
        /// </summary>
        public static bool HasAllProperties(this GameEvent gameEvent, params string[] propertyNames)
        {
            return propertyNames.All(prop => gameEvent.HasProperty(prop));
        }

        /// <summary>
        /// Set whether this event should create contingent events
        /// </summary>
        public static void SetCreateContingentEvents(this GameEvent gameEvent, bool value)
        {
            gameEvent.AddProperty("createContingentEvents", value);
        }

        public static void SetHandler(this GameEvent gameEvent, System.Action handler)
        {
            gameEvent.AddProperty("handler", handler);
        }
    }
}
