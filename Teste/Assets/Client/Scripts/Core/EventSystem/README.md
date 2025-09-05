# 🎯 Event-Driven Architecture for L5R Card Game

## Overview

The Event-Driven Architecture (EDA) transforms the L5R card game codebase from a tightly coupled system into a modern, maintainable, and extensible architecture. This system eliminates direct dependencies between game components by using a publish-subscribe pattern with strongly-typed events.

## 🚀 Quick Start

### 1. Initialize the Event System

```csharp
public class Game : MonoBehaviour
{
    void Start()
    {
        // Initialize the event system
        InitializeEventSystem();
    }
}
```

### 2. Using Events in Ring Effects

```csharp
public class MyRingEffect : BaseAbility
{
    private IEventBus eventBus;
    
    public override void Initialize(BaseCard sourceCard, Game gameInstance)
    {
        base.Initialize(sourceCard, gameInstance);
        eventBus = gameInstance.GetEventBus();
    }
    
    private void ExecuteEffect(AbilityContext context, BaseCard target)
    {
        // Do game logic
        target.FateTokens -= 2;
        
        // Publish event instead of calling analytics/messages directly
        eventBus.Publish(new FateRemovedEvent(
            game: context.Game,
            triggeredBy: context.Player,
            character: target,
            amountRemoved: 2,
            source: this
        ));
    }
}
```

## 📁 Architecture Overview

```
EventSystem/
├── Core/
│   ├── IEventBus.cs              # Event bus interface
│   ├── GameEventBus.cs           # High-performance implementation
│   └── EventBusStatistics.cs     # Performance monitoring
├── Events/
│   ├── GameEvent.cs              # Base event class
│   ├── FateRemovedEvent.cs       # Fate removal events
│   ├── RingResolvedEvent.cs      # Ring resolution events
│   ├── CharacterHonoredEvent.cs  # Character honor events
│   ├── CharacterDishonoredEvent.cs # Character dishonor events
│   ├── CardDrawnEvent.cs         # Card drawing events
│   └── AbilityExecutedEvent.cs   # Ability execution events
├── Handlers/
│   ├── AnalyticsEventHandler.cs  # Analytics integration
│   └── GameMessageHandler.cs     # Game message generation
├── Tools/
│   ├── EventSystemTester.cs      # Testing and debugging
│   └── EventLogger.cs            # Event logging and replay
└── README.md                     # This file
```

## 🎯 Core Components

### IEventBus

The central nervous system of the event architecture.

```csharp
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : GameEvent;
    void Subscribe<T>(Func<T, Task> handler) where T : GameEvent;
    void Unsubscribe<T>(Action<T> handler) where T : GameEvent;
    void Publish<T>(T gameEvent) where T : GameEvent;
    Task PublishAsync<T>(T gameEvent) where T : GameEvent;
    EventBusStatistics GetStatistics();
}
```

### GameEvent Base Class

All events inherit from this base class which provides:
- Unique event ID
- Timestamp
- Triggering player
- Source object
- Extensible event data dictionary

```csharp
public abstract class GameEvent
{
    public string EventId { get; }
    public DateTime Timestamp { get; }
    public Game Game { get; }
    public Player TriggeredBy { get; }
    public object Source { get; }
    public Dictionary<string, object> EventData { get; }
}
```

## 🔥 Available Events

### FateRemovedEvent
Published when fate is removed from a character.

**Properties:**
- `BaseCard Character` - Character losing fate
- `int AmountRemoved` - Fate amount removed
- `bool WillCharacterLeave` - Will character leave play
- `int FateBeforeRemoval` - Fate before removal
- `int FateAfterRemoval` - Fate after removal

**Usage:**
```csharp
var fateEvent = new FateRemovedEvent(
    game: gameInstance,
    triggeredBy: player,
    character: target,
    amountRemoved: 2,
    source: this
);
eventBus.Publish(fateEvent);
```

### RingResolvedEvent
Published when a ring effect is resolved.

**Properties:**
- `Ring Ring` - The ring that was resolved
- `string EffectChosen` - Effect that was chosen
- `BaseCard EffectTarget` - Target of effect (if any)
- `bool WasResolved` - Was effect actually resolved

### CharacterHonoredEvent / CharacterDishonoredEvent
Published when character honor status changes.

**Properties:**
- `BaseCard Character` - Character affected
- `bool WasAlreadyHonored/Dishonored` - Previous state

### CardDrawnEvent
Published when cards are drawn.

**Properties:**
- `BaseCard Card` - Card that was drawn
- `string DeckType` - Type of deck (conflict/dynasty)
- `int CardsDrawnCount` - Number in this draw batch
- `int HandSizeAfterDraw` - Hand size after drawing

### AbilityExecutedEvent
Published when abilities are executed.

**Properties:**
- `BaseAbility Ability` - Ability that executed
- `BaseCard SourceCard` - Card owning the ability
- `object Target` - Target of ability
- `bool WasSuccessful` - Execution result
- `string FailureReason` - Reason for failure (if any)

## 🎛️ Event Handlers

### AnalyticsEventHandler

Automatically captures all game events and sends them to the analytics system. **Replaces all direct `Game.Analytics.LogEvent()` calls**.

**Features:**
- Handles all event types automatically
- Extracts relevant analytics data
- Provides consistent event naming
- Error handling and fallback behavior

### GameMessageHandler

Generates contextual game messages based on events. **Replaces all direct `game.AddMessage()` calls**.

**Features:**
- Context-aware message generation
- Player-specific messaging
- Message filtering to prevent spam
- Localization-ready structure

## 🧪 Testing and Debugging

### EventSystemTester

Comprehensive testing component for validating the event system.

**Features:**
- Automatic test cycles
- Manual test methods (via context menu)
- Event statistics monitoring
- Mock object creation
- Unity Inspector integration

**Usage:**
```csharp
// Automatic - runs every 5 seconds
// Or manual via Unity Inspector context menus:
// "Test Fate Removed Event"
// "Test Ring Resolved Event"
// "Show Event System Statistics"
```

### EventLogger

Advanced logging and replay system for debugging.

**Features:**
- Complete event history capture
- File export for analysis
- Event filtering and search
- Performance monitoring
- Replay capability for debugging

**Usage:**
```csharp
// Get recent events
var recentEvents = eventLogger.GetRecentEvents(10);

// Get events by type
var fateEvents = eventLogger.GetEventsByType("FateRemovedEvent", 5);

// Export to file
eventLogger.ExportEventsToFile();
```

## 🔧 Integration Guide

### Migrating Existing Ring Effects

**Before (Tightly Coupled):**
```csharp
public void ExecuteRingEffect(AbilityContext context, BaseCard target)
{
    // Direct coupling - BAD
    context.Game.AddMessage($"Removing fate from {target.Name}");
    Game.Analytics.LogEvent("fate_removed", analyticsData);
    
    // Game logic
    target.FateTokens -= 2;
}
```

**After (Event-Driven):**
```csharp
public void ExecuteRingEffect(AbilityContext context, BaseCard target)
{
    // Game logic only
    target.FateTokens -= 2;
    
    // Publish event - analytics and messages handled automatically
    eventBus.Publish(new FateRemovedEvent(
        game: context.Game,
        triggeredBy: context.Player,
        character: target,
        amountRemoved: 2,
        source: this
    ));
}
```

### Creating Custom Events

```csharp
[Serializable]
public class ConflictStartedEvent : GameEvent
{
    public Conflict Conflict { get; private set; }
    public Player Attacker { get; private set; }
    public Player Defender { get; private set; }
    
    public ConflictStartedEvent(Game game, Conflict conflict) 
        : base(game, conflict.AttackingPlayer, conflict)
    {
        Conflict = conflict;
        Attacker = conflict.AttackingPlayer;
        Defender = conflict.DefendingPlayer;
        
        // Add event-specific data
        AddEventData("conflict_type", conflict.ConflictType);
        AddEventData("attacking_province", conflict.Province?.Name);
    }
    
    public override string GetDescription()
    {
        return $"{Attacker.Name} attacks {Defender.Name} at {Conflict.Province?.Name}";
    }
}
```

### Creating Custom Event Handlers

```csharp
public class SoundEffectsHandler : MonoBehaviour
{
    private IEventBus eventBus;
    
    public void Initialize(IEventBus eventBus)
    {
        this.eventBus = eventBus;
        eventBus.Subscribe<FateRemovedEvent>(OnFateRemoved);
        eventBus.Subscribe<ConflictStartedEvent>(OnConflictStarted);
    }
    
    private void OnFateRemoved(FateRemovedEvent e)
    {
        AudioManager.PlaySound("fate_removed");
        if (e.WillCharacterLeave)
        {
            AudioManager.PlaySound("character_leaves_play");
        }
    }
    
    private void OnConflictStarted(ConflictStartedEvent e)
    {
        AudioManager.PlaySound($"conflict_{e.Conflict.ConflictType}_started");
    }
}
```

## 📊 Performance Considerations

### Event Bus Performance

- **Thread-safe**: Uses `ConcurrentDictionary` and `ConcurrentBag`
- **Async Support**: Non-blocking event processing
- **Error Isolation**: Handler errors don't crash other handlers
- **Memory Efficient**: Automatic cleanup and statistics

### Best Practices

1. **Keep Events Lightweight**: Only include necessary data
2. **Use Async for Heavy Operations**: UI updates, file I/O, network calls
3. **Handle Errors Gracefully**: Always wrap event handlers in try-catch
4. **Unsubscribe Properly**: Prevent memory leaks in OnDestroy()

```csharp
// Good - lightweight event
eventBus.Publish(new FateRemovedEvent(game, player, card, 2, this));

// Bad - heavy operation in handler
void OnFateRemoved(FateRemovedEvent e)
{
    SaveGameState(); // This should be async
    UpdateComplexUI(); // This should be async
}

// Good - async heavy operations
async void OnFateRemovedAsync(FateRemovedEvent e)
{
    await SaveGameStateAsync();
    await UpdateComplexUIAsync();
}
```

## 🔍 Troubleshooting

### Common Issues

**Events Not Being Received:**
```csharp
// Check if subscribed correctly
Debug.Log($"Subscribers for FateRemovedEvent: {eventBus.GetSubscriberCount<FateRemovedEvent>()}");

// Check if event bus is enabled
Debug.Log($"Event bus enabled: {eventBus.IsEnabled}");
```

**Memory Leaks:**
```csharp
// Always unsubscribe in OnDestroy
void OnDestroy()
{
    if (eventBus != null)
    {
        eventBus.Unsubscribe<FateRemovedEvent>(OnFateRemoved);
    }
}
```

**Performance Issues:**
```csharp
// Check event statistics
var stats = eventBus.GetStatistics();
Debug.Log($"Average execution time: {stats.AverageHandlerExecutionTime}ms");
Debug.Log($"Total errors: {stats.TotalErrors}");
```

## 🎯 Migration Checklist

### For Ring Effects:
- [ ] Add `eventBus` field and initialize in `Initialize()`
- [ ] Replace direct `Game.Analytics.LogEvent()` with event publishing
- [ ] Replace direct `game.AddMessage()` with event publishing
- [ ] Remove direct UI coupling
- [ ] Test with EventSystemTester

### For New Features:
- [ ] Define appropriate events
- [ ] Create event handlers for cross-cutting concerns
- [ ] Use async handlers for heavy operations
- [ ] Add proper cleanup in OnDestroy
- [ ] Add tests and documentation

## 📈 Benefits Achieved

### Before Event-Driven Architecture:
- 🔴 **Tight Coupling**: Ring effects directly called analytics, UI, messages
- 🔴 **Hard to Test**: Complex setup needed to test individual components
- 🔴 **Difficult to Extend**: Adding features required modifying existing code
- 🔴 **Fragile**: Changes in one system could break others

### After Event-Driven Architecture:
- ✅ **Loose Coupling**: Components only depend on event contracts
- ✅ **Highly Testable**: Can test components in complete isolation
- ✅ **Easily Extensible**: New features just add new event handlers
- ✅ **Robust**: Changes in one system don't affect others
- ✅ **Observable**: Complete audit trail of all game events
- ✅ **Debuggable**: Advanced tooling for troubleshooting

## 🚀 Future Enhancements

The event system foundation enables powerful future features:

- **Network Multiplayer**: Events can be serialized and sent over network
- **Replay System**: Complete game replay from event history
- **AI Training**: Events provide perfect training data
- **Real-time Analytics**: Stream events to analytics dashboard
- **Plugin System**: Third-party plugins can subscribe to events
- **Automated Testing**: Generate test scenarios from event patterns

## 📞 Support

For questions about the event system implementation, refer to:
- Event system source code and comments
- EventSystemTester for examples
- EventLogger for debugging capabilities
- Unity console logs (filtered by 🎯, 📊, 💬, 🧪, 📝 emojis)

The Event-Driven Architecture represents a fundamental transformation of the L5R codebase, providing a solid foundation for current and future development needs while maintaining all existing game functionality.