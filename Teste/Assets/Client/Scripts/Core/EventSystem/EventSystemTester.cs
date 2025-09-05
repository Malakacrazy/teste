using System.Collections.Generic;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Component for testing and debugging the Event System.
    /// Add this to a GameObject to test event publishing and handling.
    /// </summary>
    public class EventSystemTester : MonoBehaviour
    {
        [Header("Event System Test Configuration")]
        [SerializeField] private bool autoRunTests = true;
        [SerializeField] private float testInterval = 5f;
        [SerializeField] private bool showDetailedLogs = true;
        
        private IEventBus eventBus;
        private Game gameInstance;
        private float testTimer;
        private int testCounter = 0;
        
        [Header("Test Results")]
        [SerializeField] private bool eventSystemInitialized = false;
        [SerializeField] private int eventsPublished = 0;
        [SerializeField] private int eventsReceived = 0;
        
        /// <summary>
        /// Initialize the tester
        /// </summary>
        void Start()
        {
            gameInstance = FindObjectOfType<Game>();
            if (gameInstance == null)
            {
                Debug.LogError("🔴 EventSystemTester: No Game instance found!");
                return;
            }
            
            // Initialize event system if not already done
            gameInstance.InitializeEventSystem();
            eventBus = gameInstance.GetEventBus();
            
            if (eventBus != null)
            {
                eventSystemInitialized = true;
                SetupTestSubscriptions();
                Debug.Log("✅ EventSystemTester initialized successfully");
            }
            else
            {
                Debug.LogError("🔴 EventSystemTester: Failed to get event bus!");
            }
        }
        
        /// <summary>
        /// Update test timing
        /// </summary>
        void Update()
        {
            if (!autoRunTests || !eventSystemInitialized) return;
            
            testTimer += Time.deltaTime;
            if (testTimer >= testInterval)
            {
                testTimer = 0f;
                RunAutomaticTest();
            }
        }
        
        /// <summary>
        /// Set up test event subscriptions
        /// </summary>
        private void SetupTestSubscriptions()
        {
            eventBus.Subscribe<FateRemovedEvent>(OnTestFateRemoved);
            eventBus.Subscribe<RingResolvedEvent>(OnTestRingResolved);
            eventBus.Subscribe<CharacterHonoredEvent>(OnTestCharacterHonored);
            eventBus.Subscribe<CharacterDishonoredEvent>(OnTestCharacterDishonored);
            eventBus.Subscribe<CardDrawnEvent>(OnTestCardDrawn);
            eventBus.Subscribe<AbilityExecutedEvent>(OnTestAbilityExecuted);
            
            Debug.Log("🧪 Test subscriptions set up for all event types");
        }
        
        /// <summary>
        /// Run an automatic test cycle
        /// </summary>
        private void RunAutomaticTest()
        {
            testCounter++;
            Debug.Log($"🧪 Running automatic test cycle #{testCounter}");
            
            // Test different event types
            switch (testCounter % 6)
            {
                case 1:
                    TestFateRemovedEvent();
                    break;
                case 2:
                    TestRingResolvedEvent();
                    break;
                case 3:
                    TestCharacterHonoredEvent();
                    break;
                case 4:
                    TestCharacterDishonoredEvent();
                    break;
                case 5:
                    TestCardDrawnEvent();
                    break;
                case 0:
                    TestAbilityExecutedEvent();
                    break;
            }
            
            // Show statistics every 6 tests
            if (testCounter % 6 == 0)
            {
                ShowEventSystemStatistics();
            }
        }
        
        /// <summary>
        /// Test fate removed events
        /// </summary>
        [ContextMenu("Test Fate Removed Event")]
        public void TestFateRemovedEvent()
        {
            if (eventBus == null) return;
            
            // Create a mock character for testing
            var mockCharacter = CreateMockCharacter("Test Character", 3);
            var mockPlayer = CreateMockPlayer("Test Player");
            
            var fateEvent = new FateRemovedEvent(
                game: gameInstance,
                triggeredBy: mockPlayer,
                character: mockCharacter,
                amountRemoved: 2,
                source: this
            );
            
            eventBus.Publish(fateEvent);
            eventsPublished++;
            
            if (showDetailedLogs)
                Debug.Log("🧪 Published FateRemovedEvent");
        }
        
        /// <summary>
        /// Test ring resolved events
        /// </summary>
        [ContextMenu("Test Ring Resolved Event")]
        public void TestRingResolvedEvent()
        {
            if (eventBus == null) return;
            
            var mockPlayer = CreateMockPlayer("Test Player");
            var mockRing = CreateMockRing("void");
            
            var ringEvent = new RingResolvedEvent(
                game: gameInstance,
                triggeredBy: mockPlayer,
                ring: mockRing,
                effectChosen: "fate_removed",
                effectTarget: null,
                source: this
            );
            
            eventBus.Publish(ringEvent);
            eventsPublished++;
            
            if (showDetailedLogs)
                Debug.Log("🧪 Published RingResolvedEvent");
        }
        
        /// <summary>
        /// Test character honored events
        /// </summary>
        [ContextMenu("Test Character Honored Event")]
        public void TestCharacterHonoredEvent()
        {
            if (eventBus == null) return;
            
            var mockCharacter = CreateMockCharacter("Honorable Samurai", 4);
            var mockPlayer = CreateMockPlayer("Test Player");
            
            var honorEvent = new CharacterHonoredEvent(
                game: gameInstance,
                triggeredBy: mockPlayer,
                character: mockCharacter,
                wasAlreadyHonored: false,
                source: this
            );
            
            eventBus.Publish(honorEvent);
            eventsPublished++;
            
            if (showDetailedLogs)
                Debug.Log("🧪 Published CharacterHonoredEvent");
        }
        
        /// <summary>
        /// Test character dishonored events
        /// </summary>
        [ContextMenu("Test Character Dishonored Event")]
        public void TestCharacterDishonoredEvent()
        {
            if (eventBus == null) return;
            
            var mockCharacter = CreateMockCharacter("Dishonored Ronin", 2);
            var mockPlayer = CreateMockPlayer("Test Player");
            
            var dishonorEvent = new CharacterDishonoredEvent(
                game: gameInstance,
                triggeredBy: mockPlayer,
                character: mockCharacter,
                wasAlreadyDishonored: false,
                source: this
            );
            
            eventBus.Publish(dishonorEvent);
            eventsPublished++;
            
            if (showDetailedLogs)
                Debug.Log("🧪 Published CharacterDishonoredEvent");
        }
        
        /// <summary>
        /// Test card drawn events
        /// </summary>
        [ContextMenu("Test Card Drawn Event")]
        public void TestCardDrawnEvent()
        {
            if (eventBus == null) return;
            
            var mockCard = CreateMockCharacter("Drawn Card", 1);
            var mockPlayer = CreateMockPlayer("Test Player");
            
            var drawEvent = new CardDrawnEvent(
                game: gameInstance,
                triggeredBy: mockPlayer,
                card: mockCard,
                deckType: "conflict",
                cardsDrawnCount: 1,
                handSizeAfterDraw: 5,
                source: this
            );
            
            eventBus.Publish(drawEvent);
            eventsPublished++;
            
            if (showDetailedLogs)
                Debug.Log("🧪 Published CardDrawnEvent");
        }
        
        /// <summary>
        /// Test ability executed events
        /// </summary>
        [ContextMenu("Test Ability Executed Event")]
        public void TestAbilityExecutedEvent()
        {
            if (eventBus == null) return;
            
            var mockAbility = CreateMockAbility("Test Ability");
            var mockPlayer = CreateMockPlayer("Test Player");
            
            var abilityEvent = new AbilityExecutedEvent(
                game: gameInstance,
                triggeredBy: mockPlayer,
                ability: mockAbility,
                sourceCard: null,
                target: null,
                wasSuccessful: true,
                failureReason: null
            );
            
            eventBus.Publish(abilityEvent);
            eventsPublished++;
            
            if (showDetailedLogs)
                Debug.Log("🧪 Published AbilityExecutedEvent");
        }
        
        /// <summary>
        /// Show event system statistics
        /// </summary>
        [ContextMenu("Show Event System Statistics")]
        public void ShowEventSystemStatistics()
        {
            if (eventBus == null) return;
            
            var stats = eventBus.GetStatistics();
            
            Debug.Log($"📊 Event System Statistics:\n" +
                     $"• Total Events Published: {stats.TotalEventsPublished}\n" +
                     $"• Total Handlers Executed: {stats.TotalHandlersExecuted}\n" +
                     $"• Total Errors: {stats.TotalErrors}\n" +
                     $"• Average Execution Time: {stats.AverageHandlerExecutionTime:F2}ms\n" +
                     $"• Last Event Time: {stats.LastEventTime}\n" +
                     $"• Test Events Published: {eventsPublished}\n" +
                     $"• Test Events Received: {eventsReceived}");
            
            // Show event type breakdown
            if (stats.EventTypeCount.Count > 0)
            {
                Debug.Log("📈 Event Type Breakdown:");
                foreach (var kvp in stats.EventTypeCount)
                {
                    Debug.Log($"  • {kvp.Key}: {kvp.Value}");
                }
            }
        }
        
        #region Test Event Handlers
        
        private void OnTestFateRemoved(FateRemovedEvent e)
        {
            eventsReceived++;
            if (showDetailedLogs)
                Debug.Log($"✅ Received: {e.GetDescription()}");
        }
        
        private void OnTestRingResolved(RingResolvedEvent e)
        {
            eventsReceived++;
            if (showDetailedLogs)
                Debug.Log($"✅ Received: {e.GetDescription()}");
        }
        
        private void OnTestCharacterHonored(CharacterHonoredEvent e)
        {
            eventsReceived++;
            if (showDetailedLogs)
                Debug.Log($"✅ Received: {e.GetDescription()}");
        }
        
        private void OnTestCharacterDishonored(CharacterDishonoredEvent e)
        {
            eventsReceived++;
            if (showDetailedLogs)
                Debug.Log($"✅ Received: {e.GetDescription()}");
        }
        
        private void OnTestCardDrawn(CardDrawnEvent e)
        {
            eventsReceived++;
            if (showDetailedLogs)
                Debug.Log($"✅ Received: {e.GetDescription()}");
        }
        
        private void OnTestAbilityExecuted(AbilityExecutedEvent e)
        {
            eventsReceived++;
            if (showDetailedLogs)
                Debug.Log($"✅ Received: {e.GetDescription()}");
        }
        
        #endregion
        
        #region Mock Object Creation
        
        /// <summary>
        /// Create mock character for testing
        /// </summary>
        private BaseCard CreateMockCharacter(string name, int fateTokens)
        {
            var mockCharacter = gameObject.AddComponent<BaseCard>();
            mockCharacter.name = name;
            // Note: In a real implementation, you'd set more properties
            return mockCharacter;
        }
        
        /// <summary>
        /// Create mock player for testing
        /// </summary>
        private Player CreateMockPlayer(string name)
        {
            var mockPlayer = gameObject.AddComponent<Player>();
            mockPlayer.name = name;
            return mockPlayer;
        }
        
        /// <summary>
        /// Create mock ring for testing
        /// </summary>
        private Ring CreateMockRing(string element)
        {
            var mockRing = gameObject.AddComponent<Ring>();
            mockRing.element = element;
            return mockRing;
        }
        
        /// <summary>
        /// Create mock ability for testing
        /// </summary>
        private BaseAbility CreateMockAbility(string title)
        {
            var mockAbility = gameObject.AddComponent<BaseAbility>();
            mockAbility.title = title;
            return mockAbility;
        }
        
        #endregion
        
        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<FateRemovedEvent>(OnTestFateRemoved);
                eventBus.Unsubscribe<RingResolvedEvent>(OnTestRingResolved);
                eventBus.Unsubscribe<CharacterHonoredEvent>(OnTestCharacterHonored);
                eventBus.Unsubscribe<CharacterDishonoredEvent>(OnTestCharacterDishonored);
                eventBus.Unsubscribe<CardDrawnEvent>(OnTestCardDrawn);
                eventBus.Unsubscribe<AbilityExecutedEvent>(OnTestAbilityExecuted);
                Debug.Log("🧪 EventSystemTester cleanup completed");
            }
        }
    }
}