using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when an ability is executed
    /// </summary>
    [Serializable]
    public class AbilityExecutedEvent : GameEvent
    {
        /// <summary>
        /// Ability that was executed
        /// </summary>
        public BaseAbility Ability { get; private set; }
        
        /// <summary>
        /// Card that owns the ability (if any)
        /// </summary>
        public BaseCard SourceCard { get; private set; }
        
        /// <summary>
        /// Target of the ability (if any)
        /// </summary>
        public object Target { get; private set; }
        
        /// <summary>
        /// Was the ability execution successful?
        /// </summary>
        public bool WasSuccessful { get; private set; }
        
        /// <summary>
        /// Reason for failure (if not successful)
        /// </summary>
        public string FailureReason { get; private set; }
        
        /// <summary>
        /// Was the ability execution successful? (Compatibility property)
        /// </summary>
        public bool Successful => WasSuccessful;
        
        /// <summary>
        /// Initialize ability executed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who executed the ability</param>
        /// <param name="ability">Ability that was executed</param>
        /// <param name="sourceCard">Card that owns the ability</param>
        /// <param name="target">Target of the ability</param>
        /// <param name="wasSuccessful">Was execution successful</param>
        /// <param name="failureReason">Reason for failure</param>
        public AbilityExecutedEvent(Game game, Player triggeredBy, BaseAbility ability, BaseCard sourceCard = null, object target = null, bool wasSuccessful = true, string failureReason = null) 
            : base(game, triggeredBy, ability)
        {
            Ability = ability;
            SourceCard = sourceCard;
            Target = target;
            WasSuccessful = wasSuccessful;
            FailureReason = failureReason;
            
            // Add specific event data
            AddEventData("ability_name", ability.Title);
            AddEventData("ability_type", ability.GetType().Name);
            AddEventData("was_successful", wasSuccessful);
            
            if (sourceCard != null)
            {
                AddEventData("source_card_id", sourceCard.CardId);
                AddEventData("source_card_name", sourceCard.Name);
            }
            
            if (target != null)
            {
                AddEventData("target_type", target.GetType().Name);
                if (target is BaseCard targetCard)
                {
                    AddEventData("target_card_id", targetCard.CardId);
                    AddEventData("target_card_name", targetCard.Name);
                }
            }
            
            if (!wasSuccessful && !string.IsNullOrEmpty(failureReason))
            {
                AddEventData("failure_reason", failureReason);
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string result = WasSuccessful ? "executed" : $"failed ({FailureReason})";
            string targetText = "";
            
            if (Target is BaseCard targetCard)
            {
                targetText = $" targeting {targetCard.Name}";
            }
            else if (Target != null)
            {
                targetText = $" targeting {Target}";
            }
            
            return $"{Ability.Title} {result}{targetText}";
        }
    }
}