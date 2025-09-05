using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when game rule validation occurs
    /// </summary>
    [Serializable]
    public class ValidationEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public ValidationEvent() : base() { }
        
        /// <summary>
        /// Type of validation performed
        /// </summary>
        public string ValidationType { get; private set; }
        
        /// <summary>
        /// Result of validation (valid, invalid, warning)
        /// </summary>
        public ValidationResult Result { get; private set; }
        
        /// <summary>
        /// Validation message/reason
        /// </summary>
        public string Message { get; private set; }
        
        /// <summary>
        /// Rule that was validated
        /// </summary>
        public string RuleName { get; private set; }
        
        /// <summary>
        /// Object being validated
        /// </summary>
        public object ValidationTarget { get; private set; }
        
        /// <summary>
        /// Additional context for the validation
        /// </summary>
        public Dictionary<string, object> ValidationContext { get; private set; }
        
        /// <summary>
        /// Severity level of validation issue
        /// </summary>
        public ValidationSeverity Severity { get; private set; }
        
        /// <summary>
        /// Suggested fix for validation failures
        /// </summary>
        public string SuggestedFix { get; private set; }
        
        /// <summary>
        /// Initialize validation event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered validation</param>
        /// <param name="validationType">Type of validation</param>
        /// <param name="result">Validation result</param>
        /// <param name="message">Validation message</param>
        /// <param name="ruleName">Rule being validated</param>
        /// <param name="validationTarget">Target object</param>
        /// <param name="severity">Severity level</param>
        /// <param name="suggestedFix">Suggested fix</param>
        /// <param name="validationContext">Additional context</param>
        /// <param name="source">Source of validation</param>
        public ValidationEvent(Game game, Player triggeredBy, string validationType, ValidationResult result,
            string message, string ruleName = null, object validationTarget = null, 
            ValidationSeverity severity = ValidationSeverity.Error, string suggestedFix = null,
            Dictionary<string, object> validationContext = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            ValidationType = validationType;
            Result = result;
            Message = message;
            RuleName = ruleName;
            ValidationTarget = validationTarget;
            Severity = severity;
            SuggestedFix = suggestedFix;
            ValidationContext = validationContext ?? new Dictionary<string, object>();
            
            // Add specific event data
            AddEventData("validation_type", validationType);
            AddEventData("result", result.ToString());
            AddEventData("message", message);
            AddEventData("rule_name", ruleName);
            AddEventData("severity", severity.ToString());
            AddEventData("suggested_fix", suggestedFix);
            AddEventData("player_id", triggeredBy?.PlayerId);
            
            // Add target information
            if (validationTarget is BaseCard card)
            {
                AddEventData("target_card_id", card.CardId);
                AddEventData("target_card_name", card.Name);
                AddEventData("target_type", "card");
            }
            else if (validationTarget is BaseAbility ability)
            {
                AddEventData("target_ability", ability.Title);
                AddEventData("target_type", "ability");
            }
            else if (validationTarget != null)
            {
                AddEventData("target_type", validationTarget.GetType().Name);
            }
            
            // Add context information
            foreach (var context in ValidationContext)
            {
                AddEventData($"context_{context.Key}", context.Value);
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string severityIcon = Severity switch
            {
                ValidationSeverity.Error => "❌",
                ValidationSeverity.Warning => "⚠️",
                ValidationSeverity.Info => "ℹ️",
                _ => "🔍"
            };
            
            string targetText = ValidationTarget != null ? $" on {GetTargetDescription()}" : "";
            string ruleText = !string.IsNullOrEmpty(RuleName) ? $" [{RuleName}]" : "";
            
            return $"{severityIcon} {ValidationType} validation{targetText}: {Result} - {Message}{ruleText}";
        }
        
        /// <summary>
        /// Get formatted target description
        /// </summary>
        private string GetTargetDescription()
        {
            if (ValidationTarget is BaseCard card)
                return $"'{card.Name}'";
            if (ValidationTarget is BaseAbility ability)
                return $"ability '{ability.Title}'";
            return ValidationTarget?.GetType().Name ?? "unknown";
        }
        
        /// <summary>
        /// Factory method for card play validation
        /// </summary>
        public static ValidationEvent CreateCardPlayValidation(Game game, Player player, BaseCard card, 
            bool canPlay, string reason, string suggestedFix = null, object source = null)
        {
            var result = canPlay ? ValidationResult.Valid : ValidationResult.Invalid;
            var severity = canPlay ? ValidationSeverity.Info : ValidationSeverity.Error;
            
            var context = new Dictionary<string, object>
            {
                { "fate_cost", card.FateCost },
                { "player_fate", player.Fate },
                { "hand_size", player.Hand.Count }
            };
            
            return new ValidationEvent(game, player, "card_play", result, reason, "can_play_card", 
                card, severity, suggestedFix, context, source);
        }
        
        /// <summary>
        /// Factory method for ability activation validation
        /// </summary>
        public static ValidationEvent CreateAbilityValidation(Game game, Player player, BaseAbility ability, 
            bool canActivate, string reason, string suggestedFix = null, object source = null)
        {
            var result = canActivate ? ValidationResult.Valid : ValidationResult.Invalid;
            var severity = canActivate ? ValidationSeverity.Info : ValidationSeverity.Error;
            
            var context = new Dictionary<string, object>
            {
                { "ability_type", ability.GetType().Name },
                { "ability_title", ability.Title }
            };
            
            return new ValidationEvent(game, player, "ability_activation", result, reason, "can_activate_ability", 
                ability, severity, suggestedFix, context, source);
        }
        
        /// <summary>
        /// Factory method for target validation
        /// </summary>
        public static ValidationEvent CreateTargetValidation(Game game, Player player, object target, 
            BaseAbility ability, bool isValidTarget, string reason, object source = null)
        {
            var result = isValidTarget ? ValidationResult.Valid : ValidationResult.Invalid;
            var severity = isValidTarget ? ValidationSeverity.Info : ValidationSeverity.Warning;
            
            var context = new Dictionary<string, object>
            {
                { "ability_title", ability.Title },
                { "target_required", ability.RequiresTarget }
            };
            
            return new ValidationEvent(game, player, "target_validation", result, reason, "valid_target", 
                target, severity, null, context, source);
        }
        
        /// <summary>
        /// Factory method for game state validation
        /// </summary>
        public static ValidationEvent CreateGameStateValidation(Game game, Player player, string stateType, 
            bool isValid, string reason, ValidationSeverity severity = ValidationSeverity.Warning, object source = null)
        {
            var result = isValid ? ValidationResult.Valid : ValidationResult.Invalid;
            
            var context = new Dictionary<string, object>
            {
                { "game_phase", game?.CurrentPhase ?? "unknown" },
                { "turn_number", game?.TurnNumber ?? 0 }
            };
            
            return new ValidationEvent(game, player, "game_state", result, reason, stateType, 
                null, severity, null, context, source);
        }
    }
    
    /// <summary>
    /// Result of a validation check
    /// </summary>
    public enum ValidationResult
    {
        Valid,
        Invalid,
        Warning
    }
    
    /// <summary>
    /// Severity level of validation issues
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}