using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a GameAction is successfully executed
    /// </summary>
    [Serializable]
    public class ActionExecutedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public ActionExecutedEvent() : base() { }
        
        /// <summary>
        /// Type of action that was executed
        /// </summary>
        public string ActionType { get; private set; }
        
        /// <summary>
        /// Name/description of the action
        /// </summary>
        public string ActionName { get; private set; }
        
        /// <summary>
        /// Target of the action (if applicable)
        /// </summary>
        public object ActionTarget { get; private set; }
        
        /// <summary>
        /// Parameters used in the action
        /// </summary>
        public Dictionary<string, object> ActionParameters { get; private set; }
        
        /// <summary>
        /// Result of the action execution
        /// </summary>
        public object ActionResult { get; private set; }
        
        /// <summary>
        /// Time taken to execute the action in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; private set; }
        
        /// <summary>
        /// Cost paid for executing this action
        /// </summary>
        public Dictionary<string, int> ActionCosts { get; private set; }
        
        /// <summary>
        /// Whether this action can be undone
        /// </summary>
        public bool CanUndo { get; private set; }
        
        /// <summary>
        /// Priority level of this action
        /// </summary>
        public int Priority { get; private set; }
        
        /// <summary>
        /// Initialize action executed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who executed the action</param>
        /// <param name="actionType">Type of action</param>
        /// <param name="actionName">Name of action</param>
        /// <param name="actionTarget">Target of action</param>
        /// <param name="actionParameters">Action parameters</param>
        /// <param name="actionResult">Result of action</param>
        /// <param name="executionTimeMs">Execution time</param>
        /// <param name="actionCosts">Costs paid</param>
        /// <param name="canUndo">Whether action can be undone</param>
        /// <param name="priority">Action priority</param>
        /// <param name="source">Source of the action</param>
        public ActionExecutedEvent(Game game, Player triggeredBy, string actionType, string actionName,
            object actionTarget = null, Dictionary<string, object> actionParameters = null,
            object actionResult = null, long executionTimeMs = 0, Dictionary<string, int> actionCosts = null,
            bool canUndo = false, int priority = 0, object source = null) 
            : base(game, triggeredBy, source)
        {
            ActionType = actionType;
            ActionName = actionName;
            ActionTarget = actionTarget;
            ActionParameters = actionParameters ?? new Dictionary<string, object>();
            ActionResult = actionResult;
            ExecutionTimeMs = executionTimeMs;
            ActionCosts = actionCosts ?? new Dictionary<string, int>();
            CanUndo = canUndo;
            Priority = priority;
            
            // Add specific event data
            AddEventData("action_type", actionType);
            AddEventData("action_name", actionName);
            AddEventData("execution_time_ms", executionTimeMs);
            AddEventData("can_undo", canUndo);
            AddEventData("priority", priority);
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
            
            // Add result information if available
            if (actionResult != null)
            {
                AddEventData("result_type", actionResult.GetType().Name);
                AddEventData("result_string", actionResult.ToString());
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string targetText = ActionTarget != null ? $" targeting {GetTargetDescription()}" : "";
            string costText = ActionCosts.Count > 0 ? $" (costs: {GetCostDescription()})" : "";
            string timeText = ExecutionTimeMs > 0 ? $" [{ExecutionTimeMs}ms]" : "";
            string priorityText = Priority > 0 ? $" [P{Priority}]" : "";
            string undoText = CanUndo ? " [UNDOABLE]" : "";
            
            return $"{TriggeredBy.Name} executed {ActionName}{targetText}{costText}{timeText}{priorityText}{undoText}";
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
        public static ActionExecutedEvent CreateCardPlayAction(Game game, Player player, BaseCard card,
            Dictionary<string, int> costs, long executionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "card_type", card.CardType.ToString() },
                { "fate_cost", card.FateCost },
                { "played_from", "hand" }
            };
            
            return new ActionExecutedEvent(game, player, "card_play", $"Play {card.Name}", card,
                parameters, true, executionTime, costs, false, 1, source);
        }
        
        /// <summary>
        /// Factory method for ability activation actions
        /// </summary>
        public static ActionExecutedEvent CreateAbilityActivation(Game game, Player player, BaseAbility ability,
            object target = null, Dictionary<string, int> costs = null, long executionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "ability_title", ability.Title },
                { "ability_type", ability.GetType().Name },
                { "requires_target", ability.RequiresTarget }
            };
            
            return new ActionExecutedEvent(game, player, "ability_activation", $"Activate {ability.Title}", target,
                parameters, true, executionTime, costs, false, ability.DefaultPriority, source);
        }
        
        /// <summary>
        /// Factory method for draw card actions
        /// </summary>
        public static ActionExecutedEvent CreateDrawCardAction(Game game, Player player, int cardCount,
            string drawFrom = "deck", long executionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "cards_drawn", cardCount },
                { "draw_from", drawFrom },
                { "hand_size_before", player.Hand.Count },
                { "hand_size_after", player.Hand.Count + cardCount }
            };
            
            return new ActionExecutedEvent(game, player, "draw_cards", $"Draw {cardCount} card(s)", player,
                parameters, cardCount, executionTime, null, false, 0, source);
        }
        
        /// <summary>
        /// Factory method for discard actions
        /// </summary>
        public static ActionExecutedEvent CreateDiscardAction(Game game, Player player, BaseCard card,
            bool wasRandom = false, long executionTime = 0, object source = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "was_random", wasRandom },
                { "discard_from", "hand" },
                { "card_type", card?.CardType.ToString() ?? "unknown" }
            };
            
            return new ActionExecutedEvent(game, player, "discard_card", $"Discard {card?.Name ?? "card"}", card,
                parameters, true, executionTime, null, true, 0, source);
        }
        
        /// <summary>
        /// Factory method for honor gain/loss actions
        /// </summary>
        public static ActionExecutedEvent CreateHonorAction(Game game, Player player, int honorChange,
            string reason = "unknown", long executionTime = 0, object source = null)
        {
            var actionName = honorChange > 0 ? $"Gain {honorChange} honor" : $"Lose {Math.Abs(honorChange)} honor";
            
            var parameters = new Dictionary<string, object>
            {
                { "honor_change", honorChange },
                { "reason", reason },
                { "honor_before", player.Honor },
                { "honor_after", player.Honor + honorChange }
            };
            
            return new ActionExecutedEvent(game, player, "honor_change", actionName, player,
                parameters, honorChange, executionTime, null, false, 0, source);
        }
    }
}