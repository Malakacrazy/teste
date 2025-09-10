using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Main coordinator for all triggered ability windows in the game.
    /// Manages the timing structure and determines which abilities can be triggered when.
    /// </summary>
    public class AbilityWindow : MonoBehaviour
    {
        [Header("Window Configuration")]
        public bool debugMode = false;
        public float windowTimeout = 30f;
        public bool allowBluffPrompts = true;
        public bool autoResolveForced = false;

        [Header("Current Window State")]
        public bool isWindowOpen = false;
        public string currentWindowType;
        public Player currentPlayer;
        public List<AbilityContext> availableChoices = new List<AbilityContext>();
        
        // Event management
        private Game game;
        private List<IAbilityWindow> activeWindows = new List<IAbilityWindow>();
        private Queue<PendingWindow> pendingWindows = new Queue<PendingWindow>();
        private Dictionary<string, List<AbilityRegistration>> abilityRegistrations = new Dictionary<string, List<AbilityRegistration>>();

        // Window types
        private readonly Dictionary<string, Type> windowTypes = new Dictionary<string, Type>
        {
            { AbilityTypes.Reaction, typeof(TriggeredAbilityWindow) },
            { AbilityTypes.Interrupt, typeof(TriggeredAbilityWindow) },
            { AbilityTypes.ForcedReaction, typeof(ForcedTriggeredAbilityWindow) },
            { AbilityTypes.ForcedInterrupt, typeof(ForcedTriggeredAbilityWindow) },
            { AbilityTypes.WouldInterrupt, typeof(TriggeredAbilityWindow) },
            { AbilityTypes.CancelInterrupt, typeof(TriggeredAbilityWindow) }
        };

        void Awake()
        {
            game = FindObjectOfType<Game>();
            if (game == null)
            {
                Debug.LogError("AbilityWindow: Could not find Game component!");
                return;
            }

            // Register for game events
            game.OnEventTriggered += HandleEventTriggered;
            game.OnPhaseChanged += HandlePhaseChanged;
            game.OnGameStateChangedWithData += HandleGameStateChanged;
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.OnEventTriggered -= HandleEventTriggered;
                game.OnPhaseChanged -= HandlePhaseChanged;
                game.OnGameStateChangedWithData -= HandleGameStateChanged;
            }
        }

        #region Public API

        /// <summary>
        /// Register an ability to trigger on specific events
        /// </summary>
        public void RegisterAbility(string eventName, string abilityType, BaseCard source, object ability, Func<AbilityContext, bool> condition = null)
        {
            if (!abilityRegistrations.ContainsKey(eventName))
            {
                abilityRegistrations[eventName] = new List<AbilityRegistration>();
            }

            var registration = new AbilityRegistration
            {
                eventName = eventName,
                abilityType = abilityType,
                source = source,
                ability = ability,
                condition = condition ?? ((context) => true),
                isActive = true
            };

            abilityRegistrations[eventName].Add(registration);

            if (debugMode)
            {
                Debug.Log($"🎯 Registered {abilityType} for {eventName} on {source.name}");
            }
        }

        /// <summary>
        /// Unregister an ability (when card leaves play, etc.)
        /// </summary>
        public void UnregisterAbility(string eventName, BaseCard source, object ability)
        {
            if (abilityRegistrations.ContainsKey(eventName))
            {
                abilityRegistrations[eventName].RemoveAll(reg => 
                    reg.source == source && reg.ability == ability);
            }
        }

        /// <summary>
        /// Unregister all abilities from a source
        /// </summary>
        public void UnregisterAllAbilities(BaseCard source)
        {
            foreach (var eventRegistrations in abilityRegistrations.Values)
            {
                eventRegistrations.RemoveAll(reg => reg.source == source);
            }

            if (debugMode)
            {
                Debug.Log($"🚫 Unregistered all abilities for {source.name}");
            }
        }

        /// <summary>
        /// Open a triggered ability window for specific events
        /// </summary>
        public void OpenWindow(string abilityType, List<object> events, List<object> eventsToExclude = null)
        {
            if (events == null || events.Count == 0)
            {
                return;
            }

            var pendingWindow = new PendingWindow
            {
                abilityType = abilityType,
                events = events,
                eventsToExclude = eventsToExclude ?? new List<object>(),
                timestamp = Time.time
            };

            pendingWindows.Enqueue(pendingWindow);
            ProcessNextWindow();
        }

        /// <summary>
        /// Check if any abilities can trigger for the given events
        /// </summary>
        public bool HasTriggersForEvents(List<object> events, string abilityType)
        {
            foreach (var eventObj in events)
            {
                string eventName = GetEventName(eventObj);
                if (abilityRegistrations.ContainsKey(eventName))
                {
                    var matchingAbilities = abilityRegistrations[eventName]
                        .Where(reg => reg.abilityType == abilityType && reg.isActive)
                        .ToList();

                    if (matchingAbilities.Any())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get all available choices for current window
        /// </summary>
        public List<AbilityContext> GetAvailableChoices()
        {
            return availableChoices.ToList();
        }

        /// <summary>
        /// Force close current window (for emergency situations)
        /// </summary>
        public void ForceCloseWindow()
        {
            if (isWindowOpen && activeWindows.Count > 0)
            {
                var currentWindow = activeWindows.Last();
                CloseWindow(currentWindow);
                
                Debug.LogWarning("⚠️ Force closed ability window");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle events being triggered in the game
        /// </summary>
        private void HandleEventTriggered(object eventObj, string phase)
        {
            string eventName = GetEventName(eventObj);
            
            if (debugMode)
            {
                Debug.Log($"🎯 Event triggered: {eventName} in {phase} phase");
            }

            // Check for each ability type in priority order
            var abilityTypePriority = new string[]
            {
                AbilityTypes.CancelInterrupt,
                AbilityTypes.WouldInterrupt,
                AbilityTypes.ForcedInterrupt,
                AbilityTypes.Interrupt,
                AbilityTypes.ForcedReaction,
                AbilityTypes.Reaction
            };

            foreach (string abilityType in abilityTypePriority)
            {
                var events = new List<object> { eventObj };
                if (HasTriggersForEvents(events, abilityType))
                {
                    OpenWindow(abilityType, events);
                }
            }
        }

        /// <summary>
        /// Handle game phase changes
        /// </summary>
        private void HandlePhaseChanged(string oldPhase, string newPhase)
        {
            // Clear phase-specific registrations
            ClearPhaseSpecificRegistrations(oldPhase);
            
            // Reset player timers if needed
            foreach (var player in game.GetPlayers())
            {
                if (player.resetTimerAtEndOfRound && newPhase == "dynasty")
                {
                    player.noTimer = false;
                    player.resetTimerAtEndOfRound = false;
                }
            }

            if (debugMode)
            {
                Debug.Log($"🔄 Phase changed: {oldPhase} → {newPhase}");
            }
        }

        /// <summary>
        /// Handle general game state changes
        /// </summary>
        private void HandleGameStateChanged(string change, object data)
        {
            switch (change)
            {
                case "card_left_play":
                    if (data is BaseCard card)
                    {
                        UnregisterAllAbilities(card);
                    }
                    break;
                    
                case "game_ended":
                    CloseAllWindows();
                    break;
                    
                case "player_disconnected":
                    if (data is Player player)
                    {
                        HandlePlayerDisconnected(player);
                    }
                    break;
            }
        }

        #endregion

        #region Window Management

        /// <summary>
        /// Process the next pending window
        /// </summary>
        private void ProcessNextWindow()
        {
            if (isWindowOpen || pendingWindows.Count == 0)
            {
                return;
            }

            var pendingWindow = pendingWindows.Dequeue();
            
            // Check if window is still valid (events might have been cancelled)
            var validEvents = pendingWindow.events.Where(IsEventStillValid).ToList();
            if (validEvents.Count == 0)
            {
                ProcessNextWindow(); // Try next window
                return;
            }

            // Create appropriate window type
            IAbilityWindow window = CreateWindow(pendingWindow.abilityType, validEvents, pendingWindow.eventsToExclude);
            if (window != null)
            {
                OpenWindow(window);
            }
            else
            {
                ProcessNextWindow(); // Try next window
            }
        }

        /// <summary>
        /// Create a specific type of ability window
        /// </summary>
        private IAbilityWindow CreateWindow(string abilityType, List<object> events, List<object> eventsToExclude)
        {
            if (!windowTypes.ContainsKey(abilityType))
            {
                Debug.LogError($"Unknown ability type: {abilityType}");
                return null;
            }

            Type windowType = windowTypes[abilityType];
            
            if (windowType == typeof(TriggeredAbilityWindow))
            {
                return new TriggeredAbilityWindow(game, abilityType, events, eventsToExclude);
            }
            else if (windowType == typeof(ForcedTriggeredAbilityWindow))
            {
                // Convert object lists to GameEvent lists for ForcedTriggeredAbilityWindow
                var gameEvents = events?.Cast<GameEvent>().ToList();
                var excludedGameEvents = eventsToExclude?.Cast<GameEvent>().ToList();
                
                // Create an EventWindow if we have events
                EventWindow eventWindow = null;
                if (gameEvents != null && gameEvents.Count > 0)
                {
                    eventWindow = new EventWindow(game, gameEvents);
                }
                
                return new ForcedTriggeredAbilityWindow(game, abilityType, eventWindow, excludedGameEvents);
            }

            return null;
        }

        /// <summary>
        /// Open a specific window
        /// </summary>
        private void OpenWindow(IAbilityWindow window)
        {
            activeWindows.Add(window);
            isWindowOpen = true;
            currentWindowType = window.AbilityType;
            
            // Collect available choices
            CollectChoicesForWindow(window);
            
            // Start the window
            window.OnWindowClosed += HandleWindowClosed;
            window.Open();

            if (debugMode)
            {
                Debug.Log($"🪟 Opened {currentWindowType} window with {availableChoices.Count} choices");
            }
        }

        /// <summary>
        /// Close a specific window
        /// </summary>
        private void CloseWindow(IAbilityWindow window)
        {
            activeWindows.Remove(window);
            window.OnWindowClosed -= HandleWindowClosed;
            window.Close();

            if (activeWindows.Count == 0)
            {
                isWindowOpen = false;
                currentWindowType = null;
                availableChoices.Clear();
                
                // Process next window if any
                ProcessNextWindow();
            }

            if (debugMode)
            {
                Debug.Log($"🚪 Closed {window.AbilityType} window");
            }
        }

        /// <summary>
        /// Handle window being closed
        /// </summary>
        private void HandleWindowClosed(IAbilityWindow window)
        {
            CloseWindow(window);
        }

        /// <summary>
        /// Close all active windows
        /// </summary>
        private void CloseAllWindows()
        {
            var windowsToClose = activeWindows.ToList();
            foreach (var window in windowsToClose)
            {
                CloseWindow(window);
            }
            
            pendingWindows.Clear();
            availableChoices.Clear();
            isWindowOpen = false;
        }

        #endregion

        #region Choice Collection

        /// <summary>
        /// Collect all available ability choices for a window
        /// </summary>
        private void CollectChoicesForWindow(IAbilityWindow window)
        {
            availableChoices.Clear();

            foreach (var eventObj in window.Events)
            {
                string eventName = GetEventName(eventObj);
                
                if (abilityRegistrations.ContainsKey(eventName))
                {
                    var matchingRegistrations = abilityRegistrations[eventName]
                        .Where(reg => reg.abilityType == window.AbilityType && reg.isActive)
                        .ToList();

                    foreach (var registration in matchingRegistrations)
                    {
                        var context = CreateAbilityContext(registration, eventObj);
                        
                        if (context != null && registration.condition(context))
                        {
                            availableChoices.Add(context);
                            window.AddChoice(context);
                        }
                    }
                }
            }

            if (debugMode)
            {
                Debug.Log($"📋 Collected {availableChoices.Count} choices for {window.AbilityType}");
            }
        }

        /// <summary>
        /// Create ability context for a registration
        /// </summary>
        private AbilityContext CreateAbilityContext(AbilityRegistration registration, object eventObj)
        {
            var player = registration.source.controller;
            
            var contextProperties = new AbilityContextProperties
            {
                game = game,
                source = registration.source,
                player = player,
                ability = registration.ability as BaseAbility ?? new BaseAbility(),
                eventObj = eventObj
            };

            var contextGO = new UnityEngine.GameObject("AbilityContext");
            var context = contextGO.AddComponent<AbilityContext>();
            context.Initialize(contextProperties);
            return context;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get event name from event object
        /// </summary>
        private string GetEventName(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return gameEvent.Name;
            }
            if (eventObj is GameEvent ge)
            {
                return ge.name;
            }
            // Try to get name property via reflection if available
            var nameProperty = eventObj?.GetType().GetProperty("name");
            if (nameProperty != null)
            {
                return nameProperty.GetValue(eventObj)?.ToString() ?? "Unknown";
            }
            return eventObj?.GetType().Name ?? "Unknown";
        }

        /// <summary>
        /// Get ability name from ability object
        /// </summary>
        private string GetAbilityName(object abilityObj)
        {
            if (abilityObj is BaseAbility baseAbility)
            {
                return !string.IsNullOrEmpty(baseAbility.title) ? baseAbility.title : "Ability";
            }
            
            // Try to get name or title property via reflection
            var nameProperty = abilityObj?.GetType().GetProperty("name") ?? abilityObj?.GetType().GetProperty("title");
            if (nameProperty != null)
            {
                return nameProperty.GetValue(abilityObj)?.ToString() ?? "Ability";
            }
            
            return abilityObj?.GetType().Name ?? "Unknown Ability";
        }

        /// <summary>
        /// Check if an event is still valid
        /// </summary>
        private bool IsEventStillValid(object eventObj)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                return !gameEvent.cancelled;
            }
            return true;
        }

        /// <summary>
        /// Clear registrations that are phase-specific
        /// </summary>
        private void ClearPhaseSpecificRegistrations(string phase)
        {
            // Remove any temporary registrations that were only for this phase
            foreach (var eventRegistrations in abilityRegistrations.Values)
            {
                eventRegistrations.RemoveAll(reg => reg.isPhaseSpecific && reg.phase == phase);
            }
        }

        /// <summary>
        /// Handle player disconnection
        /// </summary>
        private void HandlePlayerDisconnected(Player player)
        {
            // Auto-pass for disconnected player
            if (isWindowOpen && currentPlayer == player)
            {
                var currentWindow = activeWindows.LastOrDefault();
                if (currentWindow is TriggeredAbilityWindow triggeredWindow)
                {
                    triggeredWindow.Pass(player);
                }
            }
        }

        #endregion

        #region Debug and Testing

        /// <summary>
        /// Get debug information about current state
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"AbilityWindow Debug Info:\n";
            info += $"Window Open: {isWindowOpen}\n";
            info += $"Current Type: {currentWindowType ?? "None"}\n";
            info += $"Active Windows: {activeWindows.Count}\n";
            info += $"Pending Windows: {pendingWindows.Count}\n";
            info += $"Available Choices: {availableChoices.Count}\n";
            info += $"Total Registrations: {abilityRegistrations.Sum(kvp => kvp.Value.Count)}\n";

            if (availableChoices.Count > 0)
            {
                info += "\nCurrent Choices:\n";
                foreach (var choice in availableChoices)
                {
                    string sourceName = choice.source is BaseCard card ? card.name : choice.source?.ToString() ?? "Unknown";
                    string abilityName = GetAbilityName(choice.ability);
                    info += $"  - {sourceName}: {abilityName}\n";
                }
            }

            return info;
        }

        /// <summary>
        /// Simulate triggering an event (for testing)
        /// </summary>
        public void SimulateEvent(string eventName, BaseCard card = null, Player player = null)
        {
            var mockEvent = new MockGameEvent
            {
                Name = eventName,
                Card = card,
                Context = player != null ? AbilityContext.CreateContext(game, card, player) : null,
                cancelled = false
            };

            HandleEventTriggered(mockEvent, game.currentPhase);
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Represents a registered ability
    /// </summary>
    [System.Serializable]
    public class AbilityRegistration
    {
        public string eventName;
        public string abilityType;
        public BaseCard source;
        public object ability;
        public Func<AbilityContext, bool> condition;
        public bool isActive = true;
        public bool isPhaseSpecific = false;
        public string phase;
    }

    /// <summary>
    /// Represents a pending window to be opened
    /// </summary>
    public class PendingWindow
    {
        public string abilityType;
        public List<object> events;
        public List<object> eventsToExclude;
        public float timestamp;
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
    /// Mock game event for testing
    /// </summary>
    public class MockGameEvent : IGameEvent
    {
        public string Name { get; set; }
        public string EventName => Name;
        public BaseCard Card { get; set; }
        public Ring Ring { get; set; }
        public string Phase { get; set; }
        public AbilityContext Context { get; set; }
        public bool cancelled { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        public void Cancel()
        {
            cancelled = true;
        }
        
        public bool IsCancelled()
        {
            return cancelled;
        }
        
        public bool IsResolved()
        {
            return false; // MockGameEvent is never resolved
        }
        
        public void Execute()
        {
            // Mock execution - do nothing
        }
    }

    /// <summary>
    /// Extension methods for ability window management
    /// </summary>
    public static class AbilityWindowExtensions
    {
        /// <summary>
        /// Register a card's abilities automatically
        /// </summary>
        public static void RegisterCardAbilities(this AbilityWindow abilityWindow, BaseCard card)
        {
            // Auto-register common abilities based on card properties
            if (card.HasKeyword("reaction"))
            {
                abilityWindow.RegisterAbility("onCardEntersPlay", AbilityTypes.Reaction, card, card.reactionAbility);
            }
            
            if (card.HasKeyword("interrupt"))
            {
                abilityWindow.RegisterAbility("onInitiateAbilityEffects", AbilityTypes.Interrupt, card, card.interruptAbility);
            }
            
            // Register triggered abilities from IronPython scripts
            if (card.pythonScript != null)
            {
                card.pythonScript.RegisterTriggeredAbilities(abilityWindow, card);
            }
        }

        /// <summary>
        /// Create a simple triggered ability window
        /// </summary>
        public static void CreateSimpleWindow(this AbilityWindow abilityWindow, string abilityType, object eventObj)
        {
            abilityWindow.OpenWindow(abilityType, new List<object> { eventObj });
        }
    }

    #endregion
}