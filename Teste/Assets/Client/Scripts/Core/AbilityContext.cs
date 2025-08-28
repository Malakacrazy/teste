using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    // Supporting classes and interfaces for AbilityContext
    public class StatusToken
    {
        public string name;
        public object value;
        public Player owner;
        
        public StatusToken() { }
        public StatusToken(string tokenName, object tokenValue, Player tokenOwner = null)
        {
            name = tokenName;
            value = tokenValue;
            owner = tokenOwner;
        }
    }

    // AbilityLimit moved to separate AbilityLimit.cs file



    // EffectSource moved to separate EffectSource.cs file

    public static class Stages
    {
        public const string PreTarget = "pretarget";
        public const string Target = "target";
        public const string Cost = "cost";
        public const string Effect = "effect";
        public const string PostEffect = "posteffect";
    }

    /// <summary>
    /// Properties for creating an AbilityContext
    /// </summary>
    [System.Serializable]
    public class AbilityContextProperties
    {
        public Game game;
        public object source;
        public Player player;
        public BaseAbility ability;
        public Dictionary<string, object> costs;
        public Dictionary<string, object> targets;
        public Dictionary<string, object> rings;
        public Dictionary<string, object> selects;
        public Dictionary<string, object> tokens;
        public List<object> events;
        public string stage;
        public object targetAbility;
        public object eventObj;
    }

    /// <summary>
    /// Provides context for ability execution, including targets, costs, and game state
    /// </summary>
    public class AbilityContext : MonoBehaviour
    {
        [Header("Core Context")]
        public Game game;
        public object source;
        public Player player;
        public BaseAbility ability;

        [Header("Ability Data")]
        public Dictionary<string, object> costs = new Dictionary<string, object>();
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        public Dictionary<string, object> rings = new Dictionary<string, object>();
        public Dictionary<string, object> selects = new Dictionary<string, object>();
        public Dictionary<string, object> tokens = new Dictionary<string, object>();
        public List<object> events = new List<object>();
        public string stage;
        public object targetAbility;

        [Header("Resolved Context")]
        public object target;
        public string select;
        public Ring ring;
        public StatusToken token;
        public object eventObj;

        [Header("Game State")]
        public List<ProvinceRefillData> provincesToRefill = new List<ProvinceRefillData>();
        public bool subResolution = false;
        public Player choosingPlayerOverride = null;
        public List<GameAction> gameActionsResolutionChain = new List<GameAction>();
        public string playType;

        public void Awake()
        {
            // Initialize dictionaries if they're null (can happen in Unity)
            if (costs == null) costs = new Dictionary<string, object>();
            if (targets == null) targets = new Dictionary<string, object>();
            if (rings == null) rings = new Dictionary<string, object>();
            if (selects == null) selects = new Dictionary<string, object>();
            if (tokens == null) tokens = new Dictionary<string, object>();
            if (events == null) events = new List<object>();
            if (provincesToRefill == null) provincesToRefill = new List<ProvinceRefillData>();
            if (gameActionsResolutionChain == null) gameActionsResolutionChain = new List<GameAction>();
        }
        
        /// <summary>
        /// Constructor that accepts AbilityContextProperties
        /// </summary>
        public AbilityContext(AbilityContextProperties properties)
        {
            Awake();
            Initialize(properties);
        }

        /// <summary>
        /// Sets the current stage of ability execution
        /// </summary>
        /// <param name="newStage">The stage to set</param>
        public void SetStage(string newStage)
        {
            stage = newStage;
        }

        /// <summary>
        /// Initialize the ability context with the provided properties
        /// </summary>
        /// <param name="properties">Context properties</param>
        public void Initialize(AbilityContextProperties properties)
        {
            game = properties.game;
            source = properties.source ?? new EffectSource();
            player = properties.player;
            ability = properties.ability ?? new BaseAbility();
            costs = properties.costs ?? new Dictionary<string, object>();
            targets = properties.targets ?? new Dictionary<string, object>();
            rings = properties.rings ?? new Dictionary<string, object>();
            selects = properties.selects ?? new Dictionary<string, object>();
            tokens = properties.tokens ?? new Dictionary<string, object>();
            events = properties.events ?? new List<object>();
            stage = properties.stage ?? Stages.Effect;
            targetAbility = properties.targetAbility;
            eventObj = properties.eventObj;

            // Determine play type from player's playable locations (placeholder implementation)
            playType = "playFromHand"; // Simplified for now
        }

        /// <summary>
        /// Creates a copy of this context with optional new properties
        /// </summary>
        /// <param name="newProps">Properties to override in the copy</param>
        /// <returns>New AbilityContext with modified properties</returns>
        public AbilityContext Copy(Dictionary<string, object> newProps = null)
        {
            var copyGO = new GameObject("AbilityContext_Copy");
            var copy = copyGO.AddComponent<AbilityContext>();
            
            copy.Initialize(GetProps());
            
            if (newProps != null)
            {
                foreach (var kvp in newProps)
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "game": copy.game = kvp.Value as Game; break;
                        case "source": copy.source = kvp.Value; break;
                        case "player": copy.player = kvp.Value as Player; break;
                        case "ability": copy.ability = kvp.Value as BaseAbility; break;
                        case "stage": copy.stage = kvp.Value as string; break;
                        case "targetability": copy.targetAbility = kvp.Value; break;
                    }
                }
            }
            
            // Copy resolved context
            copy.target = target;
            copy.select = select;
            copy.ring = ring;
            copy.token = token;
            
            // Copy game state
            copy.provincesToRefill = new List<ProvinceRefillData>(provincesToRefill);
            copy.subResolution = subResolution;
            copy.choosingPlayerOverride = choosingPlayerOverride;
            copy.gameActionsResolutionChain = new List<GameAction>(gameActionsResolutionChain);
            copy.playType = playType;
            
            return copy;
        }

        /// <summary>
        /// Queues a province to be refilled after the current ability resolves
        /// </summary>
        /// <param name="targetPlayer">Player whose province should be refilled</param>
        /// <param name="location">Province location to refill</param>
        public void RefillProvince(Player targetPlayer, string location)
        {
            provincesToRefill.Add(new ProvinceRefillData
            {
                player = targetPlayer,
                location = location
            });
        }

        /// <summary>
        /// Executes all queued province refills
        /// </summary>
        public void Refill()
        {
            foreach (var player in game.GetPlayersInFirstPlayerOrder())
            {
                var playerRefills = provincesToRefill.Where(refill => refill.player == player).ToList();
                
                foreach (var refill in playerRefills)
                {
                    game.QueueSimpleStep(() =>
                    {
                        refill.player.ReplaceDynastyCard(refill.location);
                        return true;
                    });
                }
            }
            
            provincesToRefill.Clear();
        }

        /// <summary>
        /// Gets the properties that define this context
        /// </summary>
        /// <returns>Properties object for creating copies</returns>
        public AbilityContextProperties GetProps()
        {
            return new AbilityContextProperties
            {
                game = game,
                source = source,
                player = player,
                ability = ability,
                costs = new Dictionary<string, object>(costs ?? new Dictionary<string, object>()),
                targets = new Dictionary<string, object>(targets ?? new Dictionary<string, object>()),
                rings = new Dictionary<string, object>(rings ?? new Dictionary<string, object>()),
                selects = new Dictionary<string, object>(selects ?? new Dictionary<string, object>()),
                tokens = new Dictionary<string, object>(tokens ?? new Dictionary<string, object>()),
                events = new List<object>(events ?? new List<object>()),
                stage = stage,
                targetAbility = targetAbility
            };
        }

        /// <summary>
        /// Gets the resolved target of the specified name
        /// </summary>
        /// <param name="targetName">Name of the target to retrieve</param>
        /// <returns>Resolved target or null if not found</returns>
        public object GetTarget(string targetName)
        {
            return targets?.GetValueOrDefault(targetName);
        }

        /// <summary>
        /// Gets all resolved targets as a list
        /// </summary>
        /// <returns>List of all resolved targets</returns>
        public List<object> GetTargets()
        {
            return targets?.Values.ToList() ?? new List<object>();
        }

        /// <summary>
        /// Gets resolved targets of a specific type
        /// </summary>
        /// <typeparam name="T">Type to filter by</typeparam>
        /// <returns>List of targets of the specified type</returns>
        public List<T> GetTargets<T>() where T : class
        {
            return targets?.Values.OfType<T>().ToList() ?? new List<T>();
        }

        /// <summary>
        /// Sets a resolved target
        /// </summary>
        /// <param name="targetName">Name of the target</param>
        /// <param name="targetValue">Value of the target</param>
        public void SetTarget(string targetName, object targetValue)
        {
            if (targets == null) targets = new Dictionary<string, object>();
            targets[targetName] = targetValue;
        }

        /// <summary>
        /// Gets the resolved cost of the specified name
        /// </summary>
        /// <param name="costName">Name of the cost to retrieve</param>
        /// <returns>Resolved cost or null if not found</returns>
        public object GetCost(string costName)
        {
            return costs?.GetValueOrDefault(costName);
        }

        /// <summary>
        /// Sets a resolved cost
        /// </summary>
        /// <param name="costName">Name of the cost</param>
        /// <param name="costValue">Value of the cost</param>
        public void SetCost(string costName, object costValue)
        {
            if (costs == null) costs = new Dictionary<string, object>();
            costs[costName] = costValue;
        }

        /// <summary>
        /// Gets the resolved ring of the specified name
        /// </summary>
        /// <param name="ringName">Name of the ring to retrieve</param>
        /// <returns>Resolved ring or null if not found</returns>
        public Ring GetRing(string ringName)
        {
            return rings?.GetValueOrDefault(ringName) as Ring;
        }

        /// <summary>
        /// Sets a resolved ring
        /// </summary>
        /// <param name="ringName">Name of the ring</param>
        /// <param name="ringValue">Ring object</param>
        public void SetRing(string ringName, Ring ringValue)
        {
            if (rings == null) rings = new Dictionary<string, object>();
            rings[ringName] = ringValue;
        }

        /// <summary>
        /// Gets the resolved selection of the specified name
        /// </summary>
        /// <param name="selectName">Name of the selection to retrieve</param>
        /// <returns>Resolved selection or null if not found</returns>
        public object GetSelect(string selectName)
        {
            return selects?.GetValueOrDefault(selectName);
        }

        /// <summary>
        /// Sets a resolved selection
        /// </summary>
        /// <param name="selectName">Name of the selection</param>
        /// <param name="selectValue">Value of the selection</param>
        public void SetSelect(string selectName, object selectValue)
        {
            if (selects == null) selects = new Dictionary<string, object>();
            selects[selectName] = selectValue;
        }

        /// <summary>
        /// Adds an event to the context
        /// </summary>
        /// <param name="gameEvent">Event to add</param>
        public void AddEvent(object gameEvent)
        {
            if (events == null) events = new List<object>();
            if (!events.Contains(gameEvent))
            {
                events.Add(gameEvent);
            }
        }

        /// <summary>
        /// Removes an event from the context
        /// </summary>
        /// <param name="gameEvent">Event to remove</param>
        public void RemoveEvent(object gameEvent)
        {
            events?.Remove(gameEvent);
        }

        /// <summary>
        /// Checks if the context contains a specific event
        /// </summary>
        /// <param name="gameEvent">Event to check for</param>
        /// <returns>True if the event is in the context</returns>
        public bool HasEvent(object gameEvent)
        {
            return events?.Contains(gameEvent) ?? false;
        }

        /// <summary>
        /// Gets all events of a specific type
        /// </summary>
        /// <typeparam name="T">Type of events to retrieve</typeparam>
        /// <returns>List of events of the specified type</returns>
        public List<T> GetEvents<T>() where T : class
        {
            return events?.OfType<T>().ToList() ?? new List<T>();
        }

        /// <summary>
        /// Gets the effective choosing player (with override consideration)
        /// </summary>
        /// <returns>The player who should make choices</returns>
        public Player GetChoosingPlayer()
        {
            return choosingPlayerOverride ?? player;
        }

        /// <summary>
        /// Sets a temporary override for the choosing player
        /// </summary>
        /// <param name="overridePlayer">Player to override with</param>
        public void SetChoosingPlayerOverride(Player overridePlayer)
        {
            choosingPlayerOverride = overridePlayer;
        }

        /// <summary>
        /// Clears the choosing player override
        /// </summary>
        public void ClearChoosingPlayerOverride()
        {
            choosingPlayerOverride = null;
        }

        /// <summary>
        /// Checks if this is a sub-resolution of another ability
        /// </summary>
        /// <returns>True if this is a sub-resolution</returns>
        public bool IsSubResolution()
        {
            return subResolution;
        }

        /// <summary>
        /// Marks this context as a sub-resolution
        /// </summary>
        /// <param name="isSubResolution">Whether this is a sub-resolution</param>
        public void SetSubResolution(bool isSubResolution)
        {
            subResolution = isSubResolution;
        }

        /// <summary>
        /// Gets the source as a specific type
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <returns>Source cast to the specified type</returns>
        public T GetSource<T>() where T : class
        {
            return source as T;
        }

        /// <summary>
        /// Checks if the source is of a specific type
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if source is of the specified type</returns>
        public bool IsSourceOfType<T>() where T : class
        {
            return source is T;
        }

        /// <summary>
        /// Gets the play type if this context involves playing a card
        /// </summary>
        /// <returns>Play type or null if not applicable</returns>
        public string GetPlayType()
        {
            return playType;
        }

        /// <summary>
        /// Checks if the context is for playing from a specific location
        /// </summary>
        /// <param name="targetPlayType">Play type to check</param>
        /// <returns>True if playing from the specified location type</returns>
        public bool IsPlayType(string targetPlayType)
        {
            return playType == targetPlayType;
        }

        /// <summary>
        /// Creates a framework context for system effects
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="contextPlayer">Player for the context (optional)</param>
        /// <returns>Framework ability context</returns>
        public static AbilityContext CreateFrameworkContext(Game gameInstance, Player contextPlayer = null)
        {
            var contextGO = new GameObject("FrameworkContext");
            var context = contextGO.AddComponent<AbilityContext>();
            context.Initialize(new AbilityContextProperties
            {
                game = gameInstance,
                player = contextPlayer,
                source = new EffectSource(),
                ability = new BaseAbility(),
                stage = Stages.Effect
            });
            return context;
        }
        
        /// <summary>
        /// Copy resolved context values
        /// </summary>
        private void CopyResolvedContext(AbilityContext source, AbilityContext target)
        {
            target.target = source.target;
            target.select = source.select;
            target.ring = source.ring;
            target.token = source.token;
            target.eventObj = source.eventObj;
        }

        /// <summary>
        /// Creates a context for card ability execution
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceCard">Source card</param>
        /// <param name="contextPlayer">Player executing the ability</param>
        /// <param name="cardAbility">The ability being executed</param>
        /// <returns>Card ability context</returns>
        public static AbilityContext CreateCardContext(Game gameInstance, BaseCard sourceCard, Player contextPlayer, BaseAbility cardAbility)
        {
            var contextGO = new GameObject("CardContext");
            var context = contextGO.AddComponent<AbilityContext>();
            context.Initialize(new AbilityContextProperties
            {
                game = gameInstance,
                source = sourceCard,
                player = contextPlayer,
                ability = cardAbility,
                stage = Stages.PreTarget
            });
            return context;
        }
        
        /// <summary>
        /// Creates a context for card ability execution (simplified overload)
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceCard">Source card</param>
        /// <param name="contextPlayer">Player executing the ability</param>
        /// <returns>Card ability context</returns>
        public static AbilityContext CreateCardContext(Game gameInstance, BaseCard sourceCard, Player contextPlayer)
        {
            return CreateCardContext(gameInstance, sourceCard, contextPlayer, new BaseAbility());
        }
        
        /// <summary>
        /// Creates a general context (simplified version of CreateCardContext)
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceCard">Source card</param>
        /// <param name="contextPlayer">Player executing the ability</param>
        /// <returns>Ability context</returns>
        public static AbilityContext CreateContext(Game gameInstance, BaseCard sourceCard, Player contextPlayer)
        {
            return CreateCardContext(gameInstance, sourceCard, contextPlayer, new BaseAbility());
        }

        /// <summary>
        /// Creates a context for ring effect execution
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceRing">Source ring</param>
        /// <param name="contextPlayer">Player executing the effect</param>
        /// <returns>Ring effect context</returns>
        public static AbilityContext CreateRingContext(Game gameInstance, Ring sourceRing, Player contextPlayer)
        {
            var contextGO = new GameObject("RingContext");
            var context = contextGO.AddComponent<AbilityContext>();
            context.Initialize(new AbilityContextProperties
            {
                game = gameInstance,
                source = sourceRing,
                player = contextPlayer,
                stage = Stages.Effect
            });
            return context;
        }

        /// <summary>
        /// Debug method to log context information
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogContext()
        {
            Debug.Log($"AbilityContext - Stage: {stage}, Player: {player?.name}, Source: {source?.GetType().Name}");
            Debug.Log($"   Targets: {targets?.Count ?? 0}, Costs: {costs?.Count ?? 0}, Events: {events?.Count ?? 0}");
            
            if (provincesToRefill?.Count > 0)
                Debug.Log($"   Provinces to refill: {provincesToRefill.Count}");
                
            if (gameActionsResolutionChain?.Count > 0)
                Debug.Log($"   Resolution chain: {gameActionsResolutionChain.Count} actions");
        }

        /// <summary>
        /// Gets a string representation of the context for debugging
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"AbilityContext[{stage}] - {source?.GetType().Name ?? "Unknown"} by {player?.name ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Data for province refill operations
    /// </summary>
    [System.Serializable]
    public class ProvinceRefillData
    {
        public Player player;
        public string location;
    }



    /// <summary>
    /// Game action base class (placeholder)
    /// </summary>
    public partial class GameAction
    {
        public string actionType;
        public object target;
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public virtual bool CanExecute(AbilityContext context) { return true; }
        public virtual void Execute(AbilityContext context) { }
    }

    /// <summary>
    /// Extension methods for AbilityContext
    /// </summary>
    public static class AbilityContextExtensions
    {
        /// <summary>
        /// Checks if the context has any resolved targets
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if there are resolved targets</returns>
        public static bool HasTargets(this AbilityContext context)
        {
            return (context.targets?.Count ?? 0) > 0;
        }

        /// <summary>
        /// Checks if the context has any resolved costs
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if there are resolved costs</returns>
        public static bool HasCosts(this AbilityContext context)
        {
            return (context.costs?.Count ?? 0) > 0;
        }

        /// <summary>
        /// Checks if the context has any events
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if there are events</returns>
        public static bool HasEvents(this AbilityContext context)
        {
            return (context.events?.Count ?? 0) > 0;
        }

        /// <summary>
        /// Gets the first target of a specific type
        /// </summary>
        /// <typeparam name="T">Type to look for</typeparam>
        /// <param name="context">Context to search</param>
        /// <returns>First target of the specified type, or null</returns>
        public static T GetFirstTarget<T>(this AbilityContext context) where T : class
        {
            return context.targets?.Values.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Checks if the context is for a specific player
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <param name="targetPlayer">Player to check for</param>
        /// <returns>True if the context is for the specified player</returns>
        public static bool IsForPlayer(this AbilityContext context, Player targetPlayer)
        {
            return context.player == targetPlayer;
        }
    }
}
