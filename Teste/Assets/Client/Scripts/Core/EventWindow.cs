using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Handles event processing with multiple interruption points for triggered abilities.
    /// This is the core of L5R's timing system, allowing players to respond to events
    /// at different priority levels (WouldInterrupt → ForcedInterrupt → Interrupt → Handler → ForcedReaction → Reaction)
    /// </summary>
    public class EventWindow : BaseStepWithPipeline, IGameStep
    {
        [Header("Event Window Data")]
        [SerializeField] private List<GameEvent> events = new List<GameEvent>();
        [SerializeField] private List<ThenAbilityInfo> thenAbilities = new List<ThenAbilityInfo>();
        [SerializeField] private List<ProvinceRefillInfo> provincesToRefill = new List<ProvinceRefillInfo>();
        
        // State tracking
        private EventWindow previousEventWindow;
        
        /// <summary>
        /// Information about a "then" ability that should execute after events resolve
        /// </summary>
        [System.Serializable]
        public class ThenAbilityInfo
        {
            public BaseAbility ability;
            public AbilityContext context;
            public System.Func<GameEvent, bool> condition;
            
            public ThenAbilityInfo(BaseAbility ability, AbilityContext context, System.Func<GameEvent, bool> condition = null)
            {
                this.ability = ability;
                this.context = context;
                this.condition = condition ?? (gameEvent => gameEvent.IsFullyResolved());
            }
        }
        
        /// <summary>
        /// Information about provinces that need to be refilled after events
        /// </summary>
        [System.Serializable]
        public class ProvinceRefillInfo
        {
            public Player player;
            public string location;
            
            public ProvinceRefillInfo(Player player, string location)
            {
                this.player = player;
                this.location = location;
            }
        }
        
        // Properties
        public List<GameEvent> Events => events;
        public int EventCount => events.Count;
        public bool HasEvents => events.Count > 0;
        
        /// <summary>
        /// Constructor - creates event window from list of events
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="eventList">Events to process</param>
        public EventWindow(Game game, List<GameEvent> eventList) : base(game)
        {
            events = new List<GameEvent>();
            thenAbilities = new List<ThenAbilityInfo>();
            provincesToRefill = new List<ProvinceRefillInfo>();
            
            // Add only non-cancelled events
            foreach (var gameEvent in eventList)
            {
                if (!gameEvent.IsCancelled())
                {
                    AddEvent(gameEvent);
                }
            }
            
            InitializePipeline();
        }
        
        /// <summary>
        /// Static factory method for easier creation
        /// </summary>
        public static EventWindow Create(Game game, params GameEvent[] events)
        {
            return new EventWindow(game, events.ToList());
        }
        
        /// <summary>
        /// Initialize the event processing pipeline
        /// </summary>
        protected override void InitializePipeline()
        {
            pipeline.Initialize(new List<IGameStep>
            {
                new SimpleStep(game, SetCurrentEventWindow),
                new SimpleStep(game, CheckEventCondition),
                new SimpleStep(game, () => OpenWindow(AbilityTypes.WouldInterrupt)),
                new SimpleStep(game, CreateContingentEvents),
                new SimpleStep(game, () => OpenWindow(AbilityTypes.ForcedInterrupt)),
                new SimpleStep(game, () => OpenWindow(AbilityTypes.Interrupt)),
                new SimpleStep(game, CheckForOtherEffects),
                new SimpleStep(game, PreResolutionEffects),
                new SimpleStep(game, ExecuteHandler),
                new SimpleStep(game, CheckGameState),
                new SimpleStep(game, CheckThenAbilities),
                new SimpleStep(game, () => OpenWindow(AbilityTypes.ForcedReaction)),
                new SimpleStep(game, () => OpenWindow(AbilityTypes.Reaction)),
                new SimpleStep(game, ResetCurrentEventWindow)
            });
        }
        
        /// <summary>
        /// Add an event to this window
        /// </summary>
        public GameEvent AddEvent(GameEvent gameEvent)
        {
            gameEvent.SetWindow(this);
            events.Add(gameEvent);
            
            // Debug logging
            Debug.Log($"📋 EventWindow: Added event '{gameEvent.name}' to window");
            
            return gameEvent;
        }
        
        /// <summary>
        /// Remove an event from this window
        /// </summary>
        public GameEvent RemoveEvent(GameEvent gameEvent)
        {
            events.Remove(gameEvent);
            
            Debug.Log($"📋 EventWindow: Removed event '{gameEvent.name}' from window");
            
            return gameEvent;
        }
        
        /// <summary>
        /// Add a "then" ability that should execute after events resolve
        /// </summary>
        public void AddThenAbility(BaseAbility ability, AbilityContext context, System.Func<GameEvent, bool> condition = null)
        {
            thenAbilities.Add(new ThenAbilityInfo(ability, context, condition));
            
            Debug.Log($"🔄 EventWindow: Added then ability from {ability.name}");
        }
        
        /// <summary>
        /// Queue a province to be refilled after events
        /// </summary>
        public void QueueProvinceRefill(Player player, string location)
        {
            provincesToRefill.Add(new ProvinceRefillInfo(player, location));
            
            Debug.Log($"🏯 EventWindow: Queued province refill for {player.name} at {location}");
        }
        
        #region Pipeline Steps
        
        /// <summary>
        /// Set this as the current event window
        /// </summary>
        private bool SetCurrentEventWindow()
        {
            previousEventWindow = game.currentEventWindow;
            game.currentEventWindow = this;
            
            Debug.Log($"📋 EventWindow: Set as current event window (processing {events.Count} events)");
            
            return true;
        }
        
        /// <summary>
        /// Check conditions for all events
        /// </summary>
        private bool CheckEventCondition()
        {
            foreach (var gameEvent in events)
            {
                gameEvent.CheckCondition();
            }
            
            return true;
        }
        
        /// <summary>
        /// Open an ability window for the specified type
        /// </summary>
        private bool OpenWindow(string abilityType)
        {
            if (!HasEvents)
            {
                return true;
            }
            
            Debug.Log($"🪟 EventWindow: Opening {abilityType} window");
            
            if (abilityType == AbilityTypes.ForcedReaction || abilityType == AbilityTypes.ForcedInterrupt)
            {
                QueueStep(new ForcedTriggeredAbilityWindow(game, abilityType, this));
            }
            else
            {
                QueueStep(new TriggeredAbilityWindow(game, abilityType, this));
            }
            
            return true;
        }
        
        /// <summary>
        /// Create contingent events (primarily for LeavesPlay events)
        /// </summary>
        private bool CreateContingentEvents()
        {
            var contingentEvents = new List<GameEvent>();
            
            foreach (var gameEvent in events)
            {
                contingentEvents.AddRange(gameEvent.CreateContingentEvents());
            }
            
            if (contingentEvents.Count > 0)
            {
                Debug.Log($"🔗 EventWindow: Created {contingentEvents.Count} contingent events");
                
                // Exclude current events from the new window, we just want to give players 
                // opportunities to respond to the contingent events
                var currentEvents = events.Cast<object>().ToList(); // Copy current events
                QueueStep(new TriggeredAbilityWindow(game, AbilityTypes.WouldInterrupt, this, currentEvents));
                
                // Add contingent events to this window
                foreach (var contingentEvent in contingentEvents)
                {
                    AddEvent(contingentEvent);
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check for persistent/delayed effect cancels
        /// </summary>
        private bool CheckForOtherEffects()
        {
            foreach (var gameEvent in events)
            {
                game.EmitEvent(gameEvent.name + ":" + AbilityTypes.OtherEffects, gameEvent.GetData());
            }
            
            return true;
        }
        
        /// <summary>
        /// Execute pre-resolution effects for all events
        /// </summary>
        private bool PreResolutionEffects()
        {
            foreach (var gameEvent in events)
            {
                gameEvent.PreResolutionEffect();
            }
            
            return true;
        }
        
        /// <summary>
        /// Execute the main handlers for all events
        /// </summary>
        private bool ExecuteHandler()
        {
            // Sort events by order
            events = events.OrderBy(e => e.order).ToList();
            
            Debug.Log($"⚡ EventWindow: Executing {events.Count} event handlers");
            
            foreach (var gameEvent in events)
            {
                // Need to check condition here to ensure the event won't fizzle due to 
                // another event's resolution (e.g. double honoring an ordinary character)
                gameEvent.CheckCondition();
                
                if (!gameEvent.IsCancelled())
                {
                    Debug.Log($"⚡ Executing event: {gameEvent.name}");
                    gameEvent.ExecuteHandler();
                    game.EmitEvent(gameEvent.name, gameEvent.GetData());
                }
                else
                {
                    Debug.Log($"❌ Event cancelled: {gameEvent.name}");
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check game state after events resolve
        /// </summary>
        private bool CheckGameState()
        {
            bool anyHandlerExecuted = events.Any(e => e.HasHandler && !e.IsCancelled());
            game.CheckGameState(anyHandlerExecuted, events);
            
            return true;
        }
        
        /// <summary>
        /// Check and execute "then" abilities
        /// </summary>
        private bool CheckThenAbilities()
        {
            foreach (var thenAbility in thenAbilities)
            {
                // Check if all events in the context meet the condition
                bool allEventsMeetCondition = thenAbility.context.events.OfType<GameEvent>().All(e => thenAbility.condition(e));
                
                if (allEventsMeetCondition)
                {
                    Debug.Log($"🔄 Executing then ability: {thenAbility.ability.name}");
                    
                    var newContext = thenAbility.ability.CreateContext(thenAbility.context.player);
                    game.ResolveAbility(newContext);
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Reset to previous event window
        /// </summary>
        private bool ResetCurrentEventWindow()
        {
            if (previousEventWindow != null)
            {
                previousEventWindow.CheckEventCondition();
                game.currentEventWindow = previousEventWindow;
                
                Debug.Log($"📋 EventWindow: Restored previous event window");
            }
            else
            {
                game.currentEventWindow = null;
                Debug.Log($"📋 EventWindow: Cleared current event window");
            }
            
            return true;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get events of a specific type
        /// </summary>
        public List<T> GetEventsOfType<T>() where T : GameEvent
        {
            return events.OfType<T>().ToList();
        }
        
        /// <summary>
        /// Get events with a specific name
        /// </summary>
        public List<GameEvent> GetEventsNamed(string eventName)
        {
            return events.Where(e => e.name == eventName).ToList();
        }
        
        /// <summary>
        /// Check if any events involve a specific card
        /// </summary>
        public bool AnyEventsInvolveCard(BaseCard card)
        {
            return events.Any(e => e.InvolveCard(card));
        }
        
        /// <summary>
        /// Check if any events involve a specific player
        /// </summary>
        public bool AnyEventsInvolvePlayer(Player player)
        {
            return events.Any(e => e.InvolvePlayer(player));
        }
        
        /// <summary>
        /// Get the most recent event of a specific type
        /// </summary>
        public T GetMostRecentEvent<T>() where T : GameEvent
        {
            return events.OfType<T>().LastOrDefault();
        }
        
        /// <summary>
        /// Cancel all events that match a condition
        /// </summary>
        public void CancelEvents(System.Func<GameEvent, bool> condition)
        {
            foreach (var gameEvent in events.Where(condition))
            {
                gameEvent.Cancel();
                Debug.Log($"❌ EventWindow: Cancelled event '{gameEvent.name}'");
            }
        }
        
        /// <summary>
        /// Get a summary of all events in this window
        /// </summary>
        public string GetEventSummary()
        {
            if (!HasEvents)
                return "No events";
                
            var eventNames = events.Select(e => e.name).ToList();
            return $"Events: {string.Join(", ", eventNames)}";
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Debug method to log current state
        /// </summary>
        [ContextMenu("Log Event Window State")]
        public void LogCurrentState()
        {
            Debug.Log($"📋 EventWindow State:");
            Debug.Log($"   Events: {events.Count}");
            Debug.Log($"   Then Abilities: {thenAbilities.Count}");
            Debug.Log($"   Provinces to Refill: {provincesToRefill.Count}");
            
            foreach (var gameEvent in events)
            {
                Debug.Log($"   • {gameEvent.name} (Order: {gameEvent.order}, Cancelled: {gameEvent.IsCancelled()})");
            }
        }
        
        /// <summary>
        /// Check if this event window is still valid
        /// </summary>
        public bool IsValid()
        {
            return events.Any(e => !e.IsCancelled());
        }
        
        #endregion
    }
}