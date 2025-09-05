using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using L5RGame.Events;
using L5RGame.EventSystem;
using UnityEngine;

namespace L5RGame.Actions
{
    /// <summary>
    /// Base class for all event-driven game actions
    /// Replaces direct method calls with event publishing
    /// </summary>
    public abstract class EventDrivenGameAction
    {
        #region Properties
        
        /// <summary>
        /// Unique identifier for this action
        /// </summary>
        public string ActionId { get; private set; }
        
        /// <summary>
        /// Type of action (for categorization)
        /// </summary>
        public abstract string ActionType { get; }
        
        /// <summary>
        /// Human-readable name of the action
        /// </summary>
        public abstract string ActionName { get; }
        
        /// <summary>
        /// Player executing this action
        /// </summary>
        public Player ExecutingPlayer { get; protected set; }
        
        /// <summary>
        /// Game instance
        /// </summary>
        public Game Game { get; protected set; }
        
        /// <summary>
        /// Event bus for publishing events
        /// </summary>
        public IEventBus EventBus { get; private set; }
        
        /// <summary>
        /// Target of the action (if applicable)
        /// </summary>
        public virtual object Target { get; protected set; }
        
        /// <summary>
        /// Parameters for this action
        /// </summary>
        public Dictionary<string, object> Parameters { get; private set; }
        
        /// <summary>
        /// Costs to pay for this action
        /// </summary>
        public Dictionary<string, int> Costs { get; protected set; }
        
        /// <summary>
        /// Whether this action can be undone
        /// </summary>
        public virtual bool CanUndo { get; protected set; }
        
        /// <summary>
        /// Priority of this action (higher = more important)
        /// </summary>
        public virtual int Priority { get; protected set; }
        
        /// <summary>
        /// Whether this action has been executed
        /// </summary>
        public bool IsExecuted { get; private set; }
        
        /// <summary>
        /// Result of the action execution
        /// </summary>
        public object Result { get; protected set; }
        
        /// <summary>
        /// Time when action was created
        /// </summary>
        public DateTime CreatedTime { get; private set; }
        
        /// <summary>
        /// Time when action was executed
        /// </summary>
        public DateTime? ExecutedTime { get; private set; }
        
        #endregion
        
        #region Constructor
        
        protected EventDrivenGameAction(Game game, Player executingPlayer)
        {
            ActionId = Guid.NewGuid().ToString();
            Game = game ?? throw new ArgumentNullException(nameof(game));
            ExecutingPlayer = executingPlayer ?? throw new ArgumentNullException(nameof(executingPlayer));
            EventBus = game.GetEventBus();
            Parameters = new Dictionary<string, object>();
            Costs = new Dictionary<string, int>();
            CreatedTime = DateTime.UtcNow;
            CanUndo = false;
            Priority = 0;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Execute this action with full event-driven flow
        /// </summary>
        /// <returns>True if action was successful</returns>
        public async Task<bool> ExecuteAsync()
        {
            if (IsExecuted)
            {
                UnityEngine.Debug.LogWarning($"Action {ActionName} ({ActionId}) has already been executed");
                return false;
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 1. Publish PlayerActionEvent
                PublishPlayerActionEvent();
                
                // 2. Validate the action
                if (!await ValidateActionAsync())
                {
                    var validationFailure = CreateValidationFailureEvent();
                    EventBus.Publish(validationFailure);
                    return false;
                }
                
                // 3. Pay costs
                if (!await PayCostsAsync())
                {
                    var costFailure = CreateInsufficientResourcesEvent();
                    EventBus.Publish(costFailure);
                    return false;
                }
                
                // 4. Execute the core action logic
                Result = await ExecuteActionLogicAsync();
                
                // 5. Mark as executed
                IsExecuted = true;
                ExecutedTime = DateTime.UtcNow;
                stopwatch.Stop();
                
                // 6. Publish successful execution event
                var successEvent = CreateActionExecutedEvent(stopwatch.ElapsedMilliseconds);
                EventBus.Publish(successEvent);
                
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Publish failure event
                var failureEvent = CreateExecutionFailureEvent(ex, stopwatch.ElapsedMilliseconds);
                EventBus.Publish(failureEvent);
                
                UnityEngine.Debug.LogError($"Failed to execute action {ActionName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Undo this action if possible
        /// </summary>
        /// <returns>True if undo was successful</returns>
        public virtual async Task<bool> UndoAsync()
        {
            if (!CanUndo || !IsExecuted)
            {
                return false;
            }
            
            try
            {
                var result = await UndoActionLogicAsync();
                
                if (result)
                {
                    // Publish undo event
                    EventBus.Publish(new ActionUndoEvent(Game, ExecutingPlayer, ActionType, ActionName, Target, "action_undo", true, this));
                }
                
                return result;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to undo action {ActionName}: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Validate that this action can be executed
        /// </summary>
        /// <returns>True if action is valid</returns>
        protected abstract Task<bool> ValidateActionAsync();
        
        /// <summary>
        /// Execute the core logic of this action
        /// </summary>
        /// <returns>Result of the action</returns>
        protected abstract Task<object> ExecuteActionLogicAsync();
        
        /// <summary>
        /// Undo the effects of this action
        /// </summary>
        /// <returns>True if undo was successful</returns>
        protected virtual Task<bool> UndoActionLogicAsync()
        {
            return Task.FromResult(false); // Default: cannot undo
        }
        
        #endregion
        
        #region Protected Helper Methods
        
        /// <summary>
        /// Pay the costs for this action
        /// </summary>
        /// <returns>True if costs were successfully paid</returns>
        protected virtual async Task<bool> PayCostsAsync()
        {
            foreach (var cost in Costs)
            {
                if (!await PaySpecificCostAsync(cost.Key, cost.Value))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Pay a specific cost
        /// </summary>
        /// <param name="costType">Type of cost (fate, honor, cards, etc.)</param>
        /// <param name="amount">Amount to pay</param>
        /// <returns>True if cost was paid</returns>
        protected virtual async Task<bool> PaySpecificCostAsync(string costType, int amount)
        {
            switch (costType.ToLower())
            {
                case "fate":
                    if (ExecutingPlayer.Fate >= amount)
                    {
                        ExecutingPlayer.SpendFate(amount);
                        EventBus.Publish(ActionExecutedEvent.CreateHonorAction(Game, ExecutingPlayer, -amount, "action_cost"));
                        return true;
                    }
                    break;
                    
                case "honor":
                    if (ExecutingPlayer.Honor >= amount)
                    {
                        ExecutingPlayer.LoseHonor(amount);
                        EventBus.Publish(ActionExecutedEvent.CreateHonorAction(Game, ExecutingPlayer, -amount, "action_cost"));
                        return true;
                    }
                    break;
                    
                case "cards":
                    if (ExecutingPlayer.Hand.Count >= amount)
                    {
                        for (int i = 0; i < amount; i++)
                        {
                            var card = ExecutingPlayer.Hand[UnityEngine.Random.Range(0, ExecutingPlayer.Hand.Count)];
                            ExecutingPlayer.DiscardCard(card);
                            EventBus.Publish(ActionExecutedEvent.CreateDiscardAction(Game, ExecutingPlayer, card, true));
                        }
                        return true;
                    }
                    break;
            }
            
            return false;
        }
        
        /// <summary>
        /// Add a parameter to this action
        /// </summary>
        /// <param name="key">Parameter key</param>
        /// <param name="value">Parameter value</param>
        protected void AddParameter(string key, object value)
        {
            Parameters[key] = value;
        }
        
        /// <summary>
        /// Add a cost to this action
        /// </summary>
        /// <param name="costType">Type of cost</param>
        /// <param name="amount">Amount of cost</param>
        protected void AddCost(string costType, int amount)
        {
            if (Costs.ContainsKey(costType))
            {
                Costs[costType] += amount;
            }
            else
            {
                Costs[costType] = amount;
            }
        }
        
        #endregion
        
        #region Event Creation Methods
        
        private void PublishPlayerActionEvent()
        {
            var playerActionEvent = new PlayerActionEvent(Game, ExecutingPlayer, ActionType, Target, 
                Parameters, true, Costs, 0, false, this);
            EventBus.Publish(playerActionEvent);
        }
        
        private ActionExecutedEvent CreateActionExecutedEvent(long executionTimeMs)
        {
            return new ActionExecutedEvent(Game, ExecutingPlayer, ActionType, ActionName, Target, 
                Parameters, Result, executionTimeMs, Costs, CanUndo, Priority, this);
        }
        
        private ActionFailedEvent CreateValidationFailureEvent()
        {
            return ActionFailedEvent.CreateValidationFailure(Game, ExecutingPlayer, ActionType, 
                ActionName, "Action validation failed", Target, this);
        }
        
        private ActionFailedEvent CreateInsufficientResourcesEvent()
        {
            var missingResource = GetMissingResourceType();
            var required = Costs.ContainsKey(missingResource) ? Costs[missingResource] : 0;
            var available = GetAvailableResource(missingResource);
            
            return ActionFailedEvent.CreateInsufficientResourcesFailure(Game, ExecutingPlayer, 
                ActionType, ActionName, missingResource, required, available, this);
        }
        
        private ActionFailedEvent CreateExecutionFailureEvent(Exception exception, long executionTimeMs)
        {
            return ActionFailedEvent.CreateExecutionFailure(Game, ExecutingPlayer, ActionType, 
                ActionName, exception, Target, true, this);
        }
        
        private string GetMissingResourceType()
        {
            foreach (var cost in Costs)
            {
                if (GetAvailableResource(cost.Key) < cost.Value)
                {
                    return cost.Key;
                }
            }
            return "unknown";
        }
        
        private int GetAvailableResource(string resourceType)
        {
            return resourceType.ToLower() switch
            {
                "fate" => ExecutingPlayer.Fate,
                "honor" => ExecutingPlayer.Honor,
                "cards" => ExecutingPlayer.Hand.Count,
                _ => 0
            };
        }
        
        #endregion
        
        #region Debug and Utility
        
        public override string ToString()
        {
            var status = IsExecuted ? "EXECUTED" : "PENDING";
            var target = Target != null ? $" -> {Target}" : "";
            return $"{ActionName} [{status}]{target} (ID: {ActionId.Substring(0, 8)})";
        }
        
        public Dictionary<string, object> GetDebugInfo()
        {
            return new Dictionary<string, object>
            {
                { "action_id", ActionId },
                { "action_type", ActionType },
                { "action_name", ActionName },
                { "executing_player", ExecutingPlayer?.Name ?? "unknown" },
                { "target", Target?.ToString() ?? "none" },
                { "is_executed", IsExecuted },
                { "can_undo", CanUndo },
                { "priority", Priority },
                { "created_time", CreatedTime },
                { "executed_time", ExecutedTime },
                { "parameters_count", Parameters.Count },
                { "costs_count", Costs.Count },
                { "result", Result?.ToString() ?? "none" }
            };
        }
        
        #endregion
    }
}