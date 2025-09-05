using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a player takes a game action or makes a decision
    /// </summary>
    [Serializable]
    public class PlayerActionEvent : GameEvent
    {
        /// <summary>
        /// Type of action taken
        /// </summary>
        public string ActionType { get; private set; }
        
        /// <summary>
        /// Target of the action (if applicable)
        /// </summary>
        public object ActionTarget { get; private set; }
        
        /// <summary>
        /// Parameters/choices made in the action
        /// </summary>
        public Dictionary<string, object> ActionParameters { get; private set; }
        
        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool ActionSuccessful { get; private set; }
        
        /// <summary>
        /// Cost paid for the action (fate, honor, cards, etc.)
        /// </summary>
        public Dictionary<string, int> ActionCosts { get; private set; }
        
        /// <summary>
        /// Time taken to make the decision (in milliseconds)
        /// </summary>
        public long DecisionTimeMs { get; private set; }
        
        /// <summary>
        /// Whether this was an automatic action vs player choice
        /// </summary>
        public bool IsAutomaticAction { get; private set; }
        
        /// <summary>
        /// Initialize player action event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player taking the action</param>
        /// <param name="actionType">Type of action</param>
        /// <param name="actionTarget">Target of action</param>
        /// <param name="actionParameters">Action parameters</param>
        /// <param name="actionSuccessful">Whether action succeeded</param>
        /// <param name="actionCosts">Costs paid</param>
        /// <param name="decisionTimeMs">Decision time</param>
        /// <param name="isAutomatic">Whether automatic</param>
        /// <param name="source">Source of the action</param>
        public PlayerActionEvent(Game game, Player triggeredBy, string actionType, object actionTarget = null,
            Dictionary<string, object> actionParameters = null, bool actionSuccessful = true,
            Dictionary<string, int> actionCosts = null, long decisionTimeMs = 0, bool isAutomatic = false, object source = null) 
            : base(game, triggeredBy, source)
        {
            ActionType = actionType;
            ActionTarget = actionTarget;
            ActionParameters = actionParameters ?? new Dictionary<string, object>();
            ActionSuccessful = actionSuccessful;
            ActionCosts = actionCosts ?? new Dictionary<string, int>();
            DecisionTimeMs = decisionTimeMs;
            IsAutomaticAction = isAutomatic;
            
            // Add specific event data
            AddEventData("action_type", actionType);
            AddEventData("action_successful", actionSuccessful);
            AddEventData("decision_time_ms", decisionTimeMs);
            AddEventData("is_automatic", isAutomatic);
            AddEventData("player_id", triggeredBy?.PlayerId);
            
            // Add target information if available
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
            
            // Add costs information
            foreach (var cost in ActionCosts)
            {
                AddEventData($"cost_{cost.Key}", cost.Value);
            }
            
            // Add parameters information
            foreach (var param in ActionParameters)
            {
                AddEventData($"param_{param.Key}", param.Value);
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string targetText = ActionTarget != null ? $" targeting {GetTargetDescription()}" : "";
            string costText = ActionCosts.Count > 0 ? $" (costs: {GetCostDescription()})" : "";
            string timeText = DecisionTimeMs > 0 ? $" [{DecisionTimeMs}ms]" : "";
            string autoText = IsAutomaticAction ? " [AUTO]" : "";
            
            return $"{TriggeredBy.Name} performs {ActionType}{targetText}{costText}{timeText}{autoText}";
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
        /// Get formatted cost description
        /// </summary>
        private string GetCostDescription()
        {
            var costs = new List<string>();
            foreach (var cost in ActionCosts)
            {
                costs.Add($"{cost.Value} {cost.Key}");
            }
            return string.Join(", ", costs);
        }
        
        /// <summary>
        /// Factory method for card play actions
        /// </summary>
        public static PlayerActionEvent CreateCardPlayAction(Game game, Player player, BaseCard card, 
            Dictionary<string, int> costs, long decisionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "card_type", card.CardType.ToString() },
                { "played_from", "hand" }
            };
            
            return new PlayerActionEvent(game, player, "play_card", card, parameters, true, costs, decisionTime, false, source);
        }
        
        /// <summary>
        /// Factory method for ability activation
        /// </summary>
        public static PlayerActionEvent CreateAbilityActivation(Game game, Player player, BaseAbility ability, 
            object target = null, Dictionary<string, int> costs = null, long decisionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "ability_title", ability.Title },
                { "ability_type", ability.GetType().Name }
            };
            
            return new PlayerActionEvent(game, player, "activate_ability", target, parameters, true, costs, decisionTime, false, source);
        }
        
        /// <summary>
        /// Factory method for player choices
        /// </summary>
        public static PlayerActionEvent CreatePlayerChoice(Game game, Player player, string choiceType, 
            string selectedOption, List<string> availableOptions, long decisionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "choice_type", choiceType },
                { "selected_option", selectedOption },
                { "available_options", string.Join(",", availableOptions) },
                { "option_count", availableOptions.Count }
            };
            
            return new PlayerActionEvent(game, player, "make_choice", selectedOption, parameters, true, null, decisionTime, false, source);
        }
        
        /// <summary>
        /// Factory method for automatic actions
        /// </summary>
        public static PlayerActionEvent CreateAutomaticAction(Game game, Player player, string actionType, 
            object target = null, Dictionary<string, object> parameters = null, object source = null)
        {
            return new PlayerActionEvent(game, player, actionType, target, parameters, true, null, 0, true, source);
        }
    }
}