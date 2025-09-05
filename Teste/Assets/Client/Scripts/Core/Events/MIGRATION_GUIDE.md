# Event-Driven Architecture Migration Guide

## Overview

This guide explains how to migrate from the tightly coupled L5R card game architecture to the new event-driven system. The event system eliminates direct dependencies between components and provides a scalable, testable architecture.

## ✨ Benefits of the Event System

- **Decoupling**: Ring effects no longer directly call analytics, UI, or message systems
- **Testability**: Each component can be tested in isolation
- **Extensibility**: New event handlers can be added without modifying existing code
- **Performance**: Asynchronous event processing for non-blocking operations
- **Debugging**: Comprehensive event logging and monitoring
- **Maintainability**: Clear separation of concerns

## 🔧 Architecture Overview

### Core Components

1. **GameEvent**: Base class for all game events
2. **IEventBus**: Interface for publishing and subscribing to events
3. **GameEventBus**: High-performance event bus implementation
4. **Event Handlers**: Specialized components that react to events
   - **AnalyticsEventHandler**: Replaces direct `Game.Analytics.LogEvent()` calls
   - **GameMessageHandler**: Replaces direct `game.AddMessage()` calls
   - **UIEventHandler**: Replaces direct UI update calls

### Event Flow
```
Ring Effect → Publishes Event → Event Bus → Event Handlers → Analytics/UI/Messages
```

## 📋 Migration Steps

### Step 1: Update Game Class

The Game class has been updated to initialize the event system:

```csharp
// Event system components are automatically initialized
private IEventBus eventBus;
private AnalyticsEventHandler analyticsHandler;
private GameMessageHandler messageHandler;
private UIEventHandler uiHandler;

// Access the event bus
public IEventBus GetEventBus() => eventBus;
```

### Step 2: Migrate Ring Effects

#### Before (Coupled):
```csharp
// Direct analytics call
Game.Analytics.LogEvent("void_ring_effect", analyticsData);

// Direct message call
context.Game.AddMessage($"{player.Name} resolves void ring");

// Direct UI call
Game.UI.UpdateCharacterDisplay(target);
```

#### After (Event-Driven):
```csharp
// Publish events instead
var fateRemovedEvent = new FateRemovedEvent(
    game: context.Game,
    triggeredBy: context.Player,
    character: target,
    amountRemoved: fateToRemove,
    source: this
);
eventBus.Publish(fateRemovedEvent);
```

### Step 3: Initialize Event Bus in Abilities

Add event bus initialization to your abilities:

```csharp
public override void Initialize(BaseCard sourceCard, Game gameInstance)
{
    base.Initialize(sourceCard, gameInstance);
    
    // Get the event bus
    eventBus = gameInstance.GetEventBus();
}
```

### Step 4: Replace Direct Calls with Events

| Old Direct Call | New Event |
|----------------|-----------|
| `Game.Analytics.LogEvent()` | `eventBus.Publish(new AnalyticsEvent())` |
| `game.AddMessage()` | `eventBus.Publish(new GameMessageEvent())` |
| `Game.UI.UpdateDisplay()` | `eventBus.Publish(new UIUpdateEvent())` |
| Character fate changes | `FateRemovedEvent` |
| Character honor/dishonor | `CharacterHonoredEvent/DishonoredEvent` |
| Character bow/ready | `CharacterStatusChangedEvent` |
| Card movement | `CardMovedEvent` |
| Ring resolution | `RingResolvedEvent` |

## 🎯 Specific Event Types

### FateRemovedEvent
```csharp
var evt = new FateRemovedEvent(game, player, character, amount, source);
eventBus.Publish(evt);
```

**Replaces**:
- Direct analytics logging of fate removal
- Direct messages about fate changes
- Direct UI updates to character displays

### RingResolvedEvent
```csharp
var evt = new RingResolvedEvent(game, player, ring, effectChosen, target, source);
eventBus.Publish(evt);
```

**Replaces**:
- Ring-specific analytics calls
- Ring resolution messages
- Ring UI state updates

### CharacterStatusChangedEvent
```csharp
var evt = new CharacterStatusChangedEvent(game, player, character, wasBowed, source);
eventBus.Publish(evt);
```

**Replaces**:
- Bow/ready analytics
- Status change messages
- Character animation triggers

## 🔄 Migration Examples

### VoidRingEffect Migration

#### Before (VoidRingEffect.cs):
```csharp
private void ExecuteRemoveFate(AbilityContext context, BaseCard target)
{
    // Direct coupling to analytics
    Game.Analytics.LogEvent("void_ring_effect", new Dictionary<string, object>
    {
        { "player_id", context.Player.PlayerId },
        { "character_id", target.CardId },
        // ... more analytics data
    });
    
    // Direct coupling to messages
    context.Game.AddMessage($"{context.Player.Name} resolves void ring, removing fate from {target.Name}");
    
    // Execute the action
    var removeFateAction = GameActions.CreateRemoveFateAction(target, fateToRemove);
    removeFateAction.Resolve(target, context);
    
    // Direct coupling to UI
    Game.UI.RefreshCharacterDisplay(target);
}
```

#### After (VoidRingEffectRefactored.cs):
```csharp
private void ExecuteRemoveFate(AbilityContext context, BaseCard target)
{
    // Execute the action
    var removeFateAction = GameActions.CreateRemoveFateAction(target, fateToRemove);
    removeFateAction.Resolve(target, context);
    
    // Publish events - handlers will automatically respond
    PublishFateRemovedEvent(context, target, originalFate);
    PublishRingResolvedEvent(context, "fate_removed", target);
}

private void PublishFateRemovedEvent(AbilityContext context, BaseCard target, int originalFate)
{
    var evt = new FateRemovedEvent(context.Game, context.Player, target, fateToRemove, this);
    eventBus.Publish(evt);
}
```

### Benefits of the Migration:
1. **VoidRingEffect** is now focused only on game logic
2. **AnalyticsEventHandler** automatically logs the event
3. **GameMessageHandler** automatically generates appropriate messages
4. **UIEventHandler** automatically updates the UI
5. Easy to test each component in isolation
6. Easy to add new handlers (e.g., sound effects, network sync)

## 🧪 Testing

### Unit Testing Ring Effects
```csharp
[Test]
public void VoidRingEffect_PublishesCorrectEvents()
{
    // Arrange
    var mockEventBus = new Mock<IEventBus>();
    var voidRing = new VoidRingEffectRefactored();
    // ... setup
    
    // Act
    voidRing.ExecuteAbility(context);
    
    // Assert
    mockEventBus.Verify(bus => bus.Publish(It.IsAny<FateRemovedEvent>()), Times.Once);
    mockEventBus.Verify(bus => bus.Publish(It.IsAny<RingResolvedEvent>()), Times.Once);
}
```

### Integration Testing
Use the provided `EventSystemTest` component to verify the complete event system:

```csharp
// Attach EventSystemTest to a GameObject
// Set runTestsOnStart = true
// Check console for test results
```

## 🔍 Debugging and Monitoring

### Enable Debug Logging
```csharp
// In Game class initialization
eventBus = new GameEventBus(
    enableDebugLogging: true,  // Shows all event activity
    enablePerformanceMonitoring: true  // Tracks performance metrics
);
```

### Debug Information
```csharp
// Get event system state
var debugInfo = game.GetEventSystemDebugInfo();
Debug.Log($"Event System: {debugInfo}");

// Get specific handler stats
Debug.Log($"Events processed: {analyticsHandler.EventsProcessed}");
Debug.Log($"Errors: {analyticsHandler.ErrorCount}");
```

## ⚠️ Common Migration Pitfalls

### 1. Forgetting Event Bus Initialization
```csharp
// ❌ Wrong
eventBus.Publish(evt); // NullReferenceException

// ✅ Correct
if (eventBus != null)
{
    eventBus.Publish(evt);
}
```

### 2. Over-Publishing Events
```csharp
// ❌ Don't publish for every tiny state change
eventBus.Publish(new CharacterPowerChangedEvent(...));
eventBus.Publish(new CharacterCostChangedEvent(...));

// ✅ Publish for meaningful game events
eventBus.Publish(new CharacterPlayedEvent(...));
```

### 3. Synchronous Event Handling
```csharp
// ❌ Don't block the main thread
eventBus.Subscribe<Event>(evt => {
    Thread.Sleep(1000); // Blocks the game
});

// ✅ Use async for long-running operations
eventBus.Subscribe<Event>(async evt => {
    await ProcessEventAsync(evt);
});
```

## 🚀 Performance Considerations

### Event Bus Performance
- **Thread-Safe**: Uses ConcurrentCollections for multi-threading
- **Memory Efficient**: Automatic cleanup of disposed subscriptions
- **Error Isolated**: Handler errors don't crash other handlers
- **Metrics**: Built-in performance monitoring

### Best Practices
1. **Event Granularity**: Publish events for significant game state changes
2. **Handler Efficiency**: Keep event handlers lightweight and fast
3. **Subscription Cleanup**: Always dispose subscriptions when done
4. **Async Operations**: Use async handlers for I/O operations
5. **Error Handling**: Handlers should catch their own exceptions

## 📈 Extending the System

### Adding New Event Types
```csharp
[Serializable]
public class CustomGameEvent : GameEvent
{
    public override string EventName => "CustomEvent";
    public string CustomData { get; private set; }
    
    public CustomGameEvent(Game game, string customData) 
        : base(game)
    {
        CustomData = customData;
    }
}
```

### Adding New Event Handlers
```csharp
public class SoundEffectHandler : BaseEventHandler<FateRemovedEvent>
{
    public override string HandlerName => "Sound Effect Handler";
    
    public override void Handle(FateRemovedEvent gameEvent)
    {
        // Play fate removal sound effect
        AudioManager.PlaySound("FateRemoved");
    }
}
```

### Custom Event Filtering
```csharp
public class PlayerSpecificHandler : BaseEventHandler, IFilteringEventHandler
{
    private Player targetPlayer;
    
    public bool ShouldHandle(GameEvent gameEvent)
    {
        return gameEvent.TriggeredBy == targetPlayer;
    }
    
    protected override void SubscribeToEvents()
    {
        SubscribeToAll(HandleEvent);
    }
    
    private void HandleEvent(GameEvent evt)
    {
        // Handle only events from target player
    }
}
```

## 🎯 Rollback Strategy

If you need to rollback to the old system:

1. Set `enableEventSystem = false` in Game class
2. Use the original ring effect classes (VoidRingEffect.cs, etc.)
3. Event handlers will be disabled but won't break the game
4. Direct calls will still work as before

The migration is designed to be gradual and safe - both systems can coexist during the transition.

## 📞 Support

For questions about the migration:

1. Check the `EventSystemTest` component for working examples
2. Use `game.GetEventSystemDebugInfo()` for troubleshooting
3. Enable debug logging to see event flow
4. Refer to the refactored `VoidRingEffectRefactored.cs` as a template

## 🏁 Conclusion

The event-driven architecture provides a solid foundation for the L5R card game that will:

- **Scale** with new features and complexity
- **Improve** code quality and maintainability  
- **Enable** easier testing and debugging
- **Support** future enhancements like networking and replays
- **Reduce** coupling between game systems

The migration preserves all existing functionality while providing a path for future improvements. Each ring effect can be migrated individually, making the process gradual and low-risk.