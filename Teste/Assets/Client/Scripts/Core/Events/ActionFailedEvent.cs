using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a GameAction fails to execute
    /// </summary>
    [Serializable]
    public class ActionFailedEvent : GameEvent
    {
        /// <summary>
        /// Type of action that failed
        /// </summary>
        public string ActionType { get; private set; }
        
        /// <summary>
        /// Name/description of the action that failed
        /// </summary>
        public string ActionName { get; private set; }
        
        /// <summary>
        /// Target of the failed action (if applicable)
        /// </summary>
        public object ActionTarget { get; private set; }
        
        /// <summary>
        /// Parameters that were used in the failed action
        /// </summary>
        public Dictionary<string, object> ActionParameters { get; private set; }
        
        /// <summary>
        /// Reason why the action failed
        /// </summary>
        public string FailureReason { get; private set; }
        
        /// <summary>
        /// Category of failure (validation, execution, network, etc.)
        /// </summary>
        public ActionFailureCategory FailureCategory { get; private set; }
        
        /// <summary>
        /// Severity of the failure
        /// </summary>
        public ActionFailureSeverity FailureSeverity { get; private set; }
        
        /// <summary>
        /// Whether this action can be retried
        /// </summary>
        public bool CanRetry { get; private set; }
        
        /// <summary>
        /// Number of retry attempts made
        /// </summary>
        public int RetryAttempts { get; private set; }
        
        /// <summary>
        /// Maximum retry attempts allowed
        /// </summary>
        public int MaxRetryAttempts { get; private set; }
        
        /// <summary>
        /// Suggested alternative actions
        /// </summary>
        public List<string> AlternativeActions { get; private set; }
        
        /// <summary>
        /// Exception that caused the failure (if any)
        /// </summary>
        public string ExceptionMessage { get; private set; }
        
        /// <summary>
        /// Time when the action failed
        /// </summary>
        public DateTime FailureTime { get; private set; }
        
        /// <summary>
        /// Initialize action failed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who attempted the action</param>
        /// <param name="actionType">Type of action</param>
        /// <param name="actionName">Name of action</param>
        /// <param name="failureReason">Reason for failure</param>
        /// <param name="actionTarget">Target of action</param>
        /// <param name="actionParameters">Action parameters</param>
        /// <param name="failureCategory">Category of failure</param>
        /// <param name="failureSeverity">Severity of failure</param>
        /// <param name="canRetry">Whether action can be retried</param>
        /// <param name="retryAttempts">Current retry attempts</param>
        /// <param name="maxRetryAttempts">Maximum retry attempts</param>
        /// <param name="alternativeActions">Suggested alternatives</param>
        /// <param name="exceptionMessage">Exception message</param>
        /// <param name="source">Source of the action</param>
        public ActionFailedEvent(Game game, Player triggeredBy, string actionType, string actionName, string failureReason,
            object actionTarget = null, Dictionary<string, object> actionParameters = null,
            ActionFailureCategory failureCategory = ActionFailureCategory.Execution,
            ActionFailureSeverity failureSeverity = ActionFailureSeverity.Error,
            bool canRetry = false, int retryAttempts = 0, int maxRetryAttempts = 3,
            List<string> alternativeActions = null, string exceptionMessage = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            ActionType = actionType;
            ActionName = actionName;
            ActionTarget = actionTarget;
            ActionParameters = actionParameters ?? new Dictionary<string, object>();
            FailureReason = failureReason;
            FailureCategory = failureCategory;
            FailureSeverity = failureSeverity;
            CanRetry = canRetry;
            RetryAttempts = retryAttempts;
            MaxRetryAttempts = maxRetryAttempts;
            AlternativeActions = alternativeActions ?? new List<string>();
            ExceptionMessage = exceptionMessage;
            FailureTime = DateTime.UtcNow;
            
            // Add specific event data
            AddEventData("action_type", actionType);
            AddEventData("action_name", actionName);
            AddEventData("failure_reason", failureReason);
            AddEventData("failure_category", failureCategory.ToString());
            AddEventData("failure_severity", failureSeverity.ToString());
            AddEventData("can_retry", canRetry);
            AddEventData("retry_attempts", retryAttempts);
            AddEventData("max_retry_attempts", maxRetryAttempts);
            AddEventData("alternatives_count", AlternativeActions.Count);
            AddEventData("player_id", triggeredBy?.PlayerId);
            
            // Add target information
            if (actionTarget is BaseCard card)
            {
                AddEventData("target_card_id", card.CardId);
                AddEventData("target_card_name", card.Name);
                AddEventData("target_type", "card");
            }
            else if (actionTarget is Player player)
            {
                AddEventData("target_player_id", player.PlayerId);
                AddEventData("target_type", "player");
            }
            else if (actionTarget != null)
            {
                AddEventData("target_type", actionTarget.GetType().Name);
                AddEventData("target_string", actionTarget.ToString());
            }
            
            // Add parameters information
            foreach (var param in ActionParameters)
            {
                AddEventData($"param_{param.Key}", param.Value);
            }
            
            // Add alternatives
            if (AlternativeActions.Count > 0)
            {
                AddEventData("alternatives", string.Join(",", AlternativeActions));
            }
            
            // Add exception info if available
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                AddEventData("exception_message", exceptionMessage);
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string severityIcon = FailureSeverity switch
            {
                ActionFailureSeverity.Critical => "🚨",
                ActionFailureSeverity.Error => "❌",
                ActionFailureSeverity.Warning => "⚠️",
                ActionFailureSeverity.Info => "ℹ️",
                _ => "❓"
            };
            
            string targetText = ActionTarget != null ? $" targeting {GetTargetDescription()}" : "";
            string retryText = CanRetry ? $" (retry {RetryAttempts}/{MaxRetryAttempts})" : "";
            string alternativesText = AlternativeActions.Count > 0 ? $" [{AlternativeActions.Count} alternatives]" : "";
            
            return $"{severityIcon} {TriggeredBy.Name} failed to execute {ActionName}{targetText}: {FailureReason}{retryText}{alternativesText}";
        }
        
        /// <summary>
        /// Get formatted target description
        /// </summary>
        private string GetTargetDescription()
        {
            if (ActionTarget is BaseCard card)
                return $"'{card.Name}'";
            if (ActionTarget is Player player)
                return $"player '{player.Name}'";
            return ActionTarget?.ToString() ?? "unknown";
        }
        
        /// <summary>
        /// Check if this failure can be retried
        /// </summary>
        /// <returns>True if retry is possible</returns>
        public bool CanRetryAction()
        {
            return CanRetry && RetryAttempts < MaxRetryAttempts;
        }
        
        /// <summary>
        /// Get the next suggested alternative action
        /// </summary>
        /// <returns>Alternative action or null</returns>
        public string GetNextAlternativeAction()
        {
            if (AlternativeActions.Count > RetryAttempts && RetryAttempts < AlternativeActions.Count)
            {
                return AlternativeActions[RetryAttempts];
            }
            return null;
        }
        
        /// <summary>
        /// Factory method for validation failures
        /// </summary>
        public static ActionFailedEvent CreateValidationFailure(Game game, Player player, string actionType, 
            string actionName, string validationMessage, object target = null, object source = null)
        {
            return new ActionFailedEvent(game, player, actionType, actionName, validationMessage, target, null,
                ActionFailureCategory.Validation, ActionFailureSeverity.Warning, false, 0, 0, null, null, source);
        }
        
        /// <summary>
        /// Factory method for insufficient resources failures
        /// </summary>
        public static ActionFailedEvent CreateInsufficientResourcesFailure(Game game, Player player, string actionType,
            string actionName, string resourceType, int required, int available, object source = null)
        {
            var reason = $"Insufficient {resourceType}: need {required}, have {available}";
            var alternatives = new List<string> { $"Gain more {resourceType}", "Choose different action" };
            
            var parameters = new Dictionary<string, object>
            {
                { "resource_type", resourceType },
                { "required", required },
                { "available", available }
            };
            
            return new ActionFailedEvent(game, player, actionType, actionName, reason, null, parameters,
                ActionFailureCategory.InsufficientResources, ActionFailureSeverity.Error, true, 0, 1, 
                alternatives, null, source);
        }
        
        /// <summary>
        /// Factory method for illegal move failures
        /// </summary>
        public static ActionFailedEvent CreateIllegalMoveFailure(Game game, Player player, string actionType,
            string actionName, string ruleName, object target = null, List<string> alternatives = null, object source = null)
        {
            var reason = $"Illegal move: violates {ruleName}";
            
            return new ActionFailedEvent(game, player, actionType, actionName, reason, target, null,
                ActionFailureCategory.RuleViolation, ActionFailureSeverity.Error, false, 0, 0, 
                alternatives, null, source);
        }
        
        /// <summary>
        /// Factory method for execution failures with exceptions
        /// </summary>
        public static ActionFailedEvent CreateExecutionFailure(Game game, Player player, string actionType,
            string actionName, Exception exception, object target = null, bool canRetry = true, object source = null)
        {
            var reason = $"Execution error: {exception.Message}";
            
            return new ActionFailedEvent(game, player, actionType, actionName, reason, target, null,
                ActionFailureCategory.Execution, ActionFailureSeverity.Error, canRetry, 0, 3, 
                null, exception.ToString(), source);
        }
        
        /// <summary>
        /// Factory method for network/connectivity failures
        /// </summary>
        public static ActionFailedEvent CreateNetworkFailure(Game game, Player player, string actionType,
            string actionName, string networkError, object source = null)
        {
            var reason = $"Network error: {networkError}";
            var alternatives = new List<string> { "Retry action", "Check connection" };
            
            return new ActionFailedEvent(game, player, actionType, actionName, reason, null, null,
                ActionFailureCategory.Network, ActionFailureSeverity.Warning, true, 0, 5, 
                alternatives, null, source);
        }
    }
    
    /// <summary>
    /// Categories of action failures
    /// </summary>
    public enum ActionFailureCategory
    {
        Validation,
        InsufficientResources,
        RuleViolation,
        Execution,
        Network,
        Timeout,
        Permission,
        Unknown
    }
    
    /// <summary>
    /// Severity levels for action failures
    /// </summary>
    public enum ActionFailureSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}