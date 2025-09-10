using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a "then" ability that executes after the primary ability resolves.
    /// These are chained abilities that execute conditionally based on the success of previous effects.
    /// </summary>
    public class ThenAbility : BaseAbility
    {
        [Header("Then Ability Properties")]
        public BaseCard card;
        public ThenAbilityProperties thenProperties;

        /// <summary>
        /// Constructor for then abilities
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceCard">Source card for this ability</param>
        /// <param name="properties">Properties defining the then ability</param>
        public ThenAbility(Game gameInstance, BaseCard sourceCard, ThenAbilityProperties properties) : base()
        {
            game = gameInstance;
            card = sourceCard;
            thenProperties = properties;
            
            // Copy properties from the then ability properties
            if (properties != null)
            {
                InitializeFromThenProperties(properties);
            }

            // Then abilities cannot target first by default
            cannotTargetFirst = true;
        }

        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public ThenAbility() : base()
        {
            cannotTargetFirst = true;
        }

        /// <summary>
        /// Initialize from then ability properties
        /// </summary>
        /// <param name="properties">Then ability properties</param>
        private void InitializeFromThenProperties(ThenAbilityProperties properties)
        {
            title = properties.title ?? "";
            handler = properties.handler ?? ExecuteGameActions;
            effect = properties.effect;
            
            // Set game actions if provided
            if (properties.gameActions != null && properties.gameActions.Count > 0)
            {
                gameAction = properties.gameActions;
            }
            else if (properties.gameAction != null)
            {
                gameAction = new List<object> { properties.gameAction };
            }

            // Set cost if provided
            if (properties.cost != null)
            {
                cost = properties.cost;
            }

            // Set targets if provided
            if (properties.targets != null)
            {
                targets = properties.targets;
            }
            else if (properties.target != null)
            {
                targets = new Dictionary<string, object> { ["default"] = properties.target };
            }

            // Set limit if provided
            if (properties.limit != null)
            {
                limit = properties.limit;
            }
        }

        /// <summary>
        /// Creates a context for this then ability
        /// </summary>
        /// <param name="player">Player executing the ability</param>
        /// <returns>Ability context</returns>
        public override AbilityContext CreateContext(Player player = null)
        {
            if (player == null)
                player = card?.controller;

            var properties = new AbilityContextProperties
            {
                ability = this,
                game = game,
                player = player,
                source = card,
                stage = Stages.PreTarget
            };

            var contextGO = new GameObject("ThenAbilityContext");
            var context = contextGO.AddComponent<AbilityContext>();
            context.Initialize(properties);
            
            return context;
        }

        /// <summary>
        /// Checks game actions for potential execution, including chained then abilities
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if the ability has potential to execute</returns>
        public virtual bool CheckGameActionsForPotential(AbilityContext context)
        {
            // Check if base game actions have potential
            if (base.HasLegalTargets(context))
            {
                return true;
            }

            // If all game actions are optional and there's a then clause, check the then ability
            if (gameAction != null && gameAction.All(action => IsGameActionOptional(action, context)) && 
                thenProperties?.then != null)
            {
                var thenClause = GetThenClause(context);
                if (thenClause != null)
                {
                    var thenAbility = new ThenAbility(game, card, thenClause);
                    var thenContext = thenAbility.CreateContext(context.player);
                    return string.IsNullOrEmpty(thenAbility.MeetsRequirements(thenContext));
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a game action is optional
        /// </summary>
        /// <param name="action">Game action to check</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if the action is optional</returns>
        private bool IsGameActionOptional(object action, AbilityContext context)
        {
            if (action is GameAction gameAction)
            {
                return gameAction.IsOptional(context);
            }
            return false;
        }

        /// <summary>
        /// Gets the then clause from properties
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Then ability properties</returns>
        private ThenAbilityProperties GetThenClause(AbilityContext context)
        {
            if (thenProperties?.then == null)
                return null;

            if (thenProperties.then is System.Func<AbilityContext, ThenAbilityProperties> thenFunc)
            {
                return thenFunc(context);
            }
            else if (thenProperties.then is ThenAbilityProperties thenProps)
            {
                return thenProps;
            }

            return null;
        }

        /// <summary>
        /// Display message for the then ability
        /// </summary>
        /// <param name="context">Ability context</param>
        public override void DisplayMessage(AbilityContext context)
        {
            if (thenProperties?.message != null)
            {
                string message = thenProperties.message;
                
                if (thenProperties.message is System.Func<AbilityContext, string> messageFunc)
                {
                    message = messageFunc(context);
                }

                if (!string.IsNullOrEmpty(message))
                {
                    var messageArgs = new List<object> { context.player, context.source, context.target };
                    
                    if (thenProperties.messageArgs != null)
                    {
                        object args = thenProperties.messageArgs;
                        if (thenProperties.messageArgs is System.Func<AbilityContext, object[]> argsFunc)
                        {
                            args = argsFunc(context);
                        }

                        if (args is object[] argsArray)
                        {
                            messageArgs.AddRange(argsArray);
                        }
                    }

                    game.AddMessage(message, messageArgs.ToArray());
                }
            }
            else
            {
                base.DisplayMessage(context);
            }
        }

        /// <summary>
        /// Gets the game actions for this ability, including target-specific actions
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of game actions</returns>
        public override List<object> GetGameActions(AbilityContext context)
        {
            var actions = new List<object>();

            // Get actions from targets
            if (targets != null)
            {
                foreach (var target in targets.Values)
                {
                    var targetActions = GetTargetGameActions(target, context);
                    actions.AddRange(targetActions);
                }
            }

            // Add the ability's own game actions
            if (gameAction != null)
            {
                actions.AddRange(gameAction);
            }

            return actions;
        }

        /// <summary>
        /// Gets game actions from a target
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="context">Ability context</param>
        /// <returns>List of game actions from the target</returns>
        private List<object> GetTargetGameActions(object target, AbilityContext context)
        {
            if (target == null)
                return new List<object>();

            // Try to get game action from target using reflection or known interfaces
            try
            {
                var targetType = target.GetType();
                var getGameActionMethod = targetType.GetMethod("GetGameAction");
                
                if (getGameActionMethod != null)
                {
                    var result = getGameActionMethod.Invoke(target, new object[] { context });
                    if (result is List<object> actionList)
                        return actionList;
                    else if (result != null)
                        return new List<object> { result };
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ThenAbility.GetTargetGameActions: Error getting actions from target: {e.Message}");
            }

            return new List<object>();
        }

        /// <summary>
        /// Execute the then ability handler
        /// </summary>
        /// <param name="context">Ability context</param>
        public override void ExecuteHandler(AbilityContext context)
        {
            try
            {
                if (handler != null)
                {
                    handler(context);
                }
                else
                {
                    ExecuteGameActions(context);
                }

                // Queue game state check after execution
                game.QueueSimpleStep(() => 
                {
                    game.CheckGameState();
                    return true;
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing ThenAbility {GetTitle()}: {e.Message}");
            }
        }

        /// <summary>
        /// Execute game actions for this ability
        /// </summary>
        /// <param name="context">Ability context</param>
        public virtual void ExecuteGameActions(AbilityContext context)
        {
            context.events = context.events ?? new List<object>();
            var actions = GetGameActions(context);
            var thenClause = GetThenClause(context);

            // Execute all game actions
            foreach (var action in actions)
            {
                game.QueueSimpleStep(() => 
                {
                    if (action is GameAction gameAction)
                    {
                        gameAction.AddEventsToArray(context.events, context);
                    }
                    return true;
                });
            }

            // Process events and handle then clause
            game.QueueSimpleStep(() => 
            {
                var eventsToResolve = context.events.Where(e => !IsEventCancelled(e) && !IsEventResolved(e)).ToList();
                
                if (eventsToResolve.Count > 0)
                {
                    var window = OpenEventWindow(eventsToResolve);
                    
                    if (thenClause != null)
                    {
                        var thenCondition = thenProperties?.thenCondition;
                        window.AddThenAbility(new ThenAbility(game, card, thenClause), context, thenCondition);
                    }
                }
                else if (thenClause?.thenCondition != null && EvaluateThenCondition(thenClause.thenCondition, context))
                {
                    // Execute then ability directly if condition is met
                    var thenAbility = new ThenAbility(game, card, thenClause);
                    var thenContext = thenAbility.CreateContext(context.player);
                    game.ResolveAbility(thenContext);
                }
                
                return true;
            });
        }

        /// <summary>
        /// Checks if an event is cancelled
        /// </summary>
        /// <param name="gameEvent">Event to check</param>
        /// <returns>True if event is cancelled</returns>
        private bool IsEventCancelled(object gameEvent)
        {
            if (gameEvent is GameEvent evt)
                return evt.cancelled;
                
            // Try reflection for other event types
            try
            {
                var eventType = gameEvent.GetType();
                var cancelledProperty = eventType.GetProperty("cancelled") ?? eventType.GetField("cancelled");
                if (cancelledProperty != null)
                {
                    return (bool)(cancelledProperty.GetValue(gameEvent) ?? false);
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Checks if an event is resolved
        /// </summary>
        /// <param name="gameEvent">Event to check</param>
        /// <returns>True if event is resolved</returns>
        private bool IsEventResolved(object gameEvent)
        {
            if (gameEvent is GameEvent evt)
                return evt.resolved;
                
            // Try reflection for other event types
            try
            {
                var eventType = gameEvent.GetType();
                var resolvedProperty = eventType.GetProperty("resolved") ?? eventType.GetField("resolved");
                if (resolvedProperty != null)
                {
                    return (bool)(resolvedProperty.GetValue(gameEvent) ?? false);
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Evaluates a then condition
        /// </summary>
        /// <param name="condition">Condition to evaluate</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if condition is met</returns>
        private bool EvaluateThenCondition(System.Func<AbilityContext, bool> condition, AbilityContext context)
        {
            try
            {
                return condition?.Invoke(context) ?? true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error evaluating then condition: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Opens an event window for processing events
        /// </summary>
        /// <param name="events">Events to process</param>
        /// <returns>Event window</returns>
        public virtual ThenEventWindow OpenEventWindow(List<object> events)
        {
            return game.OpenThenEventWindow(events);
        }

        /// <summary>
        /// Indicates this is a card ability
        /// </summary>
        /// <returns>Always returns true</returns>
        public override bool IsCardAbility()
        {
            return true;
        }

        /// <summary>
        /// Gets the title of this then ability
        /// </summary>
        /// <returns>Ability title</returns>
        public override string GetTitle()
        {
            return thenProperties?.title ?? title ?? "Then Ability";
        }

        /// <summary>
        /// String representation of this then ability
        /// </summary>
        /// <returns>String describing the ability</returns>
        public override string ToString()
        {
            return $"ThenAbility[{card?.printedName ?? "Unknown"}]: {GetTitle()}";
        }

        // Property aliases for compatibility
        public ThenAbilityProperties Properties => thenProperties;
        public BaseCard Card => card;
    }

    /// <summary>
    /// Properties for defining then abilities
    /// </summary>
    [Serializable]
    public class ThenAbilityProperties
    {
        [Header("Basic Properties")]
        public string title;
        public Action<AbilityContext> handler;
        public Action<AbilityContext> effect;
        
        [Header("Game Actions")]
        public List<object> gameActions;
        public object gameAction;
        
        [Header("Targeting")]
        public Dictionary<string, object> targets;
        public object target;
        
        [Header("Costs and Limits")]
        public List<ICost> cost;
        public AbilityLimit limit;
        
        [Header("Messaging")]
        public object message; // Can be string or Func<AbilityContext, string>
        public object messageArgs; // Can be object[] or Func<AbilityContext, object[]>
        
        [Header("Then Chaining")]
        public object then; // Can be ThenAbilityProperties or Func<AbilityContext, ThenAbilityProperties>
        public System.Func<AbilityContext, bool> thenCondition;

        public ThenAbilityProperties()
        {
            gameActions = new List<object>();
            targets = new Dictionary<string, object>();
            cost = new List<ICost>();
        }
    }

    /// <summary>
    /// Event window for then abilities
    /// </summary>
    public class ThenEventWindow
    {
        public List<object> events;
        public List<ThenAbilityInfo> thenAbilities;

        public ThenEventWindow(List<object> eventList)
        {
            events = eventList ?? new List<object>();
            thenAbilities = new List<ThenAbilityInfo>();
        }

        /// <summary>
        /// Adds a then ability to be executed after events resolve
        /// </summary>
        /// <param name="ability">Then ability</param>
        /// <param name="context">Ability context</param>
        /// <param name="condition">Condition for execution</param>
        public void AddThenAbility(ThenAbility ability, AbilityContext context, System.Func<AbilityContext, bool> condition = null)
        {
            thenAbilities.Add(new ThenAbilityInfo
            {
                ability = ability,
                context = context,
                condition = condition
            });
        }
    }

    /// <summary>
    /// Information about a then ability to be executed
    /// </summary>
    [Serializable]
    public class ThenAbilityInfo
    {
        public ThenAbility ability;
        public AbilityContext context;
        public System.Func<AbilityContext, bool> condition;
    }

    /// <summary>
    /// Extension methods for then ability functionality
    /// </summary>
    public static class ThenAbilityExtensions
    {
        /// <summary>
        /// Creates a then ability from an object definition
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="card">Source card</param>
        /// <param name="thenObject">Then ability definition</param>
        /// <returns>Then ability or null</returns>
        public static ThenAbility CreateThenAbility(this Game game, BaseCard card, object thenObject)
        {
            if (thenObject == null)
                return null;

            ThenAbilityProperties properties = null;

            if (thenObject is ThenAbilityProperties props)
            {
                properties = props;
            }
            else if (thenObject is System.Func<AbilityContext, ThenAbilityProperties> propsFunc)
            {
                // Create a temporary context to evaluate the function
                var tempContext = AbilityContext.CreateFrameworkContext(game, card.controller);
                properties = propsFunc(tempContext);
            }
            else
            {
                Debug.LogWarning("ThenAbilityExtensions.CreateThenAbility: Unknown then object type");
                return null;
            }

            if (properties != null)
            {
                return new ThenAbility(game, card, properties);
            }

            return null;
        }

        /// <summary>
        /// Checks if an ability has a then clause
        /// </summary>
        /// <param name="ability">Ability to check</param>
        /// <returns>True if ability has a then clause</returns>
        public static bool HasThenClause(this BaseAbility ability)
        {
            if (ability is ThenAbility thenAbility)
            {
                return thenAbility.thenProperties?.then != null;
            }

            return false;
        }
    }
}