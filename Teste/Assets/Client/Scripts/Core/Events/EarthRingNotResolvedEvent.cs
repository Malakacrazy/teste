using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when Earth Ring effect is not resolved (player choice)
    /// </summary>
    [Serializable]
    public class EarthRingNotResolvedEvent : GameEvent
    {
        /// <summary>
        /// Reason why the earth ring was not resolved
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Initialize earth ring not resolved event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who chose not to resolve</param>
        /// <param name="reason">Reason for not resolving</param>
        /// <param name="source">Source of the effect</param>
        public EarthRingNotResolvedEvent(Game game, Player triggeredBy, string reason = "unknown", object source = null) 
            : base(game, triggeredBy, source)
        {
            Reason = reason;
            
            // Add specific event data
            AddEventData("ring_element", "earth");
            AddEventData("player_id", triggeredBy.PlayerId);
            AddEventData("reason", reason);
            AddEventData("resolution_status", "not_resolved");
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string reasonText = !string.IsNullOrEmpty(Reason) ? $" ({Reason})" : "";
            return $"{TriggeredBy.Name} chooses not to resolve the earth ring{reasonText}";
        }
    }
}