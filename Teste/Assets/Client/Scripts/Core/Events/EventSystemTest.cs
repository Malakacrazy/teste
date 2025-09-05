using System;
using System.Collections;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame.Testing
{
    /// <summary>
    /// Test component to verify the event system is working correctly.
    /// Attach this to a GameObject in the scene to run basic event system tests.
    /// </summary>
    public class EventSystemTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        private IEventBus eventBus;
        private int eventsReceived = 0;
        
        void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunEventSystemTests());
            }
        }
        
        public IEnumerator RunEventSystemTests()
        {
            Debug.Log("🧪 Starting Event System Tests");
            
            // Test 1: Basic Event Bus Creation
            yield return StartCoroutine(TestEventBusCreation());
            yield return new WaitForSeconds(0.5f);
            
            // Test 2: Event Subscription and Publishing
            yield return StartCoroutine(TestEventSubscriptionAndPublishing());
            yield return new WaitForSeconds(0.5f);
            
            // Test 3: Game Integration
            yield return StartCoroutine(TestGameIntegration());
            yield return new WaitForSeconds(0.5f);
            
            // Test 4: Event Handler Integration
            yield return StartCoroutine(TestEventHandlerIntegration());
            
            Debug.Log("🧪 Event System Tests Completed");
        }
        
        private IEnumerator TestEventBusCreation()
        {
            Debug.Log("🧪 Test 1: Event Bus Creation");
            
            try
            {
                eventBus = new GameEventBus(enableDetailedLogging, true);
                
                if (eventBus != null && eventBus.IsEnabled)
                {
                    Debug.Log("✅ Event bus created successfully");
                }
                else
                {
                    Debug.LogError("❌ Event bus creation failed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Event bus creation exception: {ex.Message}");
            }
            
            yield return null;
        }
        
        private IEnumerator TestEventSubscriptionAndPublishing()
        {
            Debug.Log("🧪 Test 2: Event Subscription and Publishing");
            
            if (eventBus == null)
            {
                Debug.LogError("❌ Cannot test subscription - event bus is null");
                yield break;
            }
            
            eventsReceived = 0;
            
            try
            {
                // Subscribe to test events
                var subscription = eventBus.Subscribe<FateRemovedEvent>(evt =>
                {
                    eventsReceived++;
                    if (enableDetailedLogging)
                    {
                        Debug.Log($"📨 Received FateRemovedEvent: {evt.Character?.Name} lost {evt.AmountRemoved} fate");
                    }
                });
                
                // Create a mock game and character for testing
                var testGame = Game.Instance;
                if (testGame == null)
                {
                    Debug.LogWarning("⚠️ No Game instance found, creating mock data");
                    // In a real test, you'd create proper mock objects
                    yield break;
                }
                
                // Test publishing an event
                var mockCharacter = CreateMockCharacter();
                var testEvent = new FateRemovedEvent(testGame, null, mockCharacter, 1, this);
                
                eventBus.Publish(testEvent);
                
                yield return new WaitForSeconds(0.1f);
                
                if (eventsReceived > 0)
                {
                    Debug.Log("✅ Event subscription and publishing working");
                }
                else
                {
                    Debug.LogError("❌ Events not received");
                }
                
                // Clean up subscription
                eventBus.Unsubscribe(subscription);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Event subscription test exception: {ex.Message}");
            }
        }
        
        private IEnumerator TestGameIntegration()
        {
            Debug.Log("🧪 Test 3: Game Integration");
            
            var game = Game.Instance;
            if (game == null)
            {
                Debug.LogError("❌ No Game instance found");
                yield break;
            }
            
            try
            {
                var gameEventBus = game.GetEventBus();
                
                if (gameEventBus != null && gameEventBus.IsEnabled)
                {
                    Debug.Log("✅ Game event bus integration working");
                    
                    if (enableDetailedLogging)
                    {
                        var debugInfo = game.GetEventSystemDebugInfo();
                        Debug.Log($"🔍 Event system debug: {debugInfo}");
                    }
                }
                else
                {
                    Debug.LogError("❌ Game event bus not available or disabled");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Game integration test exception: {ex.Message}");
            }
            
            yield return null;
        }
        
        private IEnumerator TestEventHandlerIntegration()
        {
            Debug.Log("🧪 Test 4: Event Handler Integration");
            
            var game = Game.Instance;
            if (game == null)
            {
                Debug.LogError("❌ No Game instance found for handler test");
                yield break;
            }
            
            var gameEventBus = game.GetEventBus();
            if (gameEventBus == null)
            {
                Debug.LogError("❌ No event bus available for handler test");
                yield break;
            }
            
            try
            {
                // Test if handlers are properly initialized and responding
                var initialEventCount = gameEventBus.GetSubscriptionCount();
                Debug.Log($"📊 Current event subscriptions: {initialEventCount}");
                
                // Publish a test event and see if handlers process it
                var mockCharacter = CreateMockCharacter();
                var testEvent = new FateRemovedEvent(game, null, mockCharacter, 1, this);
                
                gameEventBus.Publish(testEvent);
                
                yield return new WaitForSeconds(0.2f);
                
                Debug.Log("✅ Event handler integration test completed");
                
                if (enableDetailedLogging)
                {
                    var debugInfo = gameEventBus.GetDebugInfo();
                    Debug.Log($"🔍 Final event bus state: {debugInfo}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Event handler integration test exception: {ex.Message}");
            }
        }
        
        private BaseCard CreateMockCharacter()
        {
            // Create a simple mock character for testing
            var mockGameObject = new GameObject("MockCharacter");
            var mockCard = mockGameObject.AddComponent<BaseCard>();
            
            // Initialize basic properties
            mockCard.name = "Test Character";
            
            // Clean up after test
            Destroy(mockGameObject, 1f);
            
            return mockCard;
        }
        
        [ContextMenu("Run Event System Tests")]
        public void RunTestsManually()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(RunEventSystemTests());
            }
            else
            {
                Debug.LogWarning("⚠️ Tests can only be run in play mode");
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event bus if we created one for testing
            if (eventBus is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}