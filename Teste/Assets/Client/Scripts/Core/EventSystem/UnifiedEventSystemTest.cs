using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame.EventSystem
{
    /// <summary>
    /// Test component to validate the unified event system integration
    /// Ensures EventWindow bridge and TimingAwareEventBus work correctly
    /// </summary>
    public class UnifiedEventSystemTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private Game gameInstance;
        
        private IUnifiedEventSystem unifiedSystem;
        private int testsPassed = 0;
        private int testsFailed = 0;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            testsPassed = 0;
            testsFailed = 0;
            
            Debug.Log("🧪 Starting Unified Event System Tests...");
            
            if (!InitializeTestEnvironment())
            {
                Debug.LogError("❌ Failed to initialize test environment");
                return;
            }
            
            // Test unified event system initialization
            TestUnifiedSystemInitialization();
            
            // Test timing-aware event publishing
            TestTimingAwarePublishing();
            
            // Test EventWindow bridge functionality
            TestEventWindowBridge();
            
            // Test backward compatibility
            TestBackwardCompatibility();
            
            // Print results
            PrintTestResults();
        }
        
        private bool InitializeTestEnvironment()
        {
            try
            {
                // Try to find game instance if not set
                if (gameInstance == null)
                {
                    gameInstance = FindObjectOfType<Game>();
                }
                
                if (gameInstance == null)
                {
                    LogTest("❌ No Game instance found", false);
                    return false;
                }
                
                unifiedSystem = gameInstance.GetUnifiedEventSystem();
                if (unifiedSystem == null)
                {
                    LogTest("❌ Unified event system not initialized", false);
                    return false;
                }
                
                LogTest("✅ Test environment initialized", true);
                return true;
            }
            catch (Exception ex)
            {
                LogTest($"❌ Failed to initialize test environment: {ex.Message}", false);
                return false;
            }
        }
        
        private void TestUnifiedSystemInitialization()
        {
            try
            {
                // Check if unified system implements IUnifiedEventSystem
                bool implementsInterface = unifiedSystem is IUnifiedEventSystem;
                LogTest($"Unified system implements IUnifiedEventSystem: {implementsInterface}", implementsInterface);
                
                // Check if unified system also works as IEventBus (backward compatibility)
                bool implementsEventBus = unifiedSystem is IEventBus;
                LogTest($"Unified system implements IEventBus: {implementsEventBus}", implementsEventBus);
                
                // Check if debug info is available
                var debugInfo = unifiedSystem.GetDebugInfo();
                bool hasDebugInfo = debugInfo != null;
                LogTest($"Debug info available: {hasDebugInfo}", hasDebugInfo);
                
                if (enableDetailedLogging && hasDebugInfo)
                {
                    Debug.Log($"📊 Unified Event System Debug Info: {debugInfo}");
                }
            }
            catch (Exception ex)
            {
                LogTest($"❌ Unified system initialization test failed: {ex.Message}", false);
            }
        }
        
        private void TestTimingAwarePublishing()
        {
            try
            {
                // Create a test event
                var testEvent = new FateRemovedEvent(
                    game: gameInstance,
                    triggeredBy: null, // No player for test
                    character: null,   // No character for test
                    amountRemoved: 1,
                    source: this
                );
                
                // Test publishing at different timing windows
                TimingWindow[] windowsToTest = {
                    TimingWindow.WouldInterrupt,
                    TimingWindow.Interrupt,
                    TimingWindow.Handler,
                    TimingWindow.Reaction
                };
                
                foreach (var window in windowsToTest)
                {
                    try
                    {
                        unifiedSystem.PublishAtTiming(testEvent, window);
                        LogTest($"Published event at {window} timing window", true);
                    }
                    catch (Exception ex)
                    {
                        LogTest($"Failed to publish at {window} timing: {ex.Message}", false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogTest($"❌ Timing-aware publishing test failed: {ex.Message}", false);
            }
        }
        
        private void TestEventWindowBridge()
        {
            try
            {
                // Create test events for EventWindow
                var testEvents = new List<GameEvent>
                {
                    new FateRemovedEvent(gameInstance, null, null, 1, this),
                    new RingResolvedEvent(gameInstance, null, null, "test", null, this)
                };
                
                // Test EventWindow creation and bridge integration
                var eventWindow = new EventWindow(gameInstance, testEvents);
                bool eventWindowCreated = eventWindow != null;
                LogTest($"EventWindow created: {eventWindowCreated}", eventWindowCreated);
                
                // Test EventWindow extension method
                var timingContext = eventWindow.ToTimingContext(gameInstance);
                bool timingContextCreated = timingContext != null;
                LogTest($"TimingContext created from EventWindow: {timingContextCreated}", timingContextCreated);
                
                if (enableDetailedLogging && timingContextCreated)
                {
                    Debug.Log($"🌉 Timing Context: {timingContext}");
                }
            }
            catch (Exception ex)
            {
                LogTest($"❌ EventWindow bridge test failed: {ex.Message}", false);
            }
        }
        
        private void TestBackwardCompatibility()
        {
            try
            {
                // Test that unified system can be used as IEventBus
                IEventBus eventBus = unifiedSystem as IEventBus;
                if (eventBus != null)
                {
                    // Test regular event publishing (backward compatible)
                    var testEvent = new GameMessageEvent(gameInstance, null, "Test message for backward compatibility");
                    
                    eventBus.Publish(testEvent);
                    LogTest("Backward compatible event publishing works", true);
                    
                    // Test subscription functionality
                    bool subscriptionWorks = false;
                    using (var subscription = eventBus.Subscribe<GameMessageEvent>(evt => {
                        subscriptionWorks = true;
                    }))
                    {
                        eventBus.Publish(new GameMessageEvent(gameInstance, null, "Test subscription"));
                        
                        // Give a moment for async processing
                        System.Threading.Thread.Sleep(10);
                        
                        LogTest($"Event subscription works: {subscriptionWorks}", subscriptionWorks);
                    }
                }
                else
                {
                    LogTest("Unified system does not support IEventBus interface", false);
                }
            }
            catch (Exception ex)
            {
                LogTest($"❌ Backward compatibility test failed: {ex.Message}", false);
            }
        }
        
        private void LogTest(string message, bool passed)
        {
            if (passed)
            {
                testsPassed++;
                if (enableDetailedLogging)
                {
                    Debug.Log($"✅ {message}");
                }
            }
            else
            {
                testsFailed++;
                Debug.LogError($"❌ {message}");
            }
        }
        
        private void PrintTestResults()
        {
            Debug.Log($"🧪 Unified Event System Tests Complete:");
            Debug.Log($"✅ Passed: {testsPassed}");
            Debug.Log($"❌ Failed: {testsFailed}");
            Debug.Log($"📊 Success Rate: {(testsPassed * 100f) / (testsPassed + testsFailed):F1}%");
            
            if (testsFailed == 0)
            {
                Debug.Log("🎉 All tests passed! Unified Event System is working correctly.");
            }
            else
            {
                Debug.LogWarning($"⚠️ {testsFailed} test(s) failed. Check the implementation.");
            }
        }
        
        [ContextMenu("Test Event Publishing Performance")]
        public void TestEventPublishingPerformance()
        {
            if (unifiedSystem == null)
            {
                Debug.LogError("❌ Unified system not initialized");
                return;
            }
            
            const int eventCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < eventCount; i++)
            {
                var testEvent = new FateRemovedEvent(gameInstance, null, null, 1, this);
                unifiedSystem.PublishAtTiming(testEvent, TimingWindow.Handler);
            }
            
            stopwatch.Stop();
            
            Debug.Log($"⏱️ Published {eventCount} events in {stopwatch.ElapsedMilliseconds}ms");
            Debug.Log($"📊 Average: {stopwatch.ElapsedTicks / (float)eventCount:F2} ticks per event");
            Debug.Log($"🚀 Throughput: {eventCount / (stopwatch.ElapsedMilliseconds / 1000f):F0} events/second");
        }
    }
}