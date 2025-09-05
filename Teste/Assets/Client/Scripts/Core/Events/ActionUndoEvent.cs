using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when an action is undone
    /// </summary>
    [Serializable]
    public class ActionUndoEvent : GameEvent
    {
        /// <summary>
        /// Type of action that was undone
        /// </summary>
        public string ActionType { get; private set; }
        
        /// <summary>
        /// Name of the action that was undone
        /// </summary>
        public string ActionName { get; private set; }
        
        /// <summary>
        /// Target of the undone action
        /// </summary>
        public object ActionTarget { get; private set; }
        
        /// <summary>
        /// Reason for the undo
        /// </summary>
        public string UndoReason { get; private set; }
        
        /// <summary>
        /// Whether the undo was successful
        /// </summary>
        public bool UndoSuccessful { get; private set; }
        
        /// <summary>
        /// Initialize action undo event
        /// </summary>
        public ActionUndoEvent(Game game, Player triggeredBy, string actionType, string actionName, 
            object actionTarget, string undoReason = "player_request", bool undoSuccessful = true, object source = null) 
            : base(game, triggeredBy, source)
        {
            ActionType = actionType;
            ActionName = actionName;
            ActionTarget = actionTarget;
            UndoReason = undoReason;
            UndoSuccessful = undoSuccessful;
            
            // Add specific event data
            AddEventData("action_type", actionType);
            AddEventData("action_name", actionName);
            AddEventData("undo_reason", undoReason);
            AddEventData("undo_successful", undoSuccessful);
            AddEventData("player_id", triggeredBy?.PlayerId);
            
            if (actionTarget is BaseCard card)
            {
                AddEventData("target_card_id", card.CardId);
                AddEventData("target_card_name", card.Name);
            }
        }
        
        public string GetDescription()
        {
            var success = UndoSuccessful ? "successfully" : "failed to";
            return $"{TriggeredBy.Name} {success} undid {ActionName} ({UndoReason})";
        }
    }
}