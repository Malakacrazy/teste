using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when Air Ring effect is not resolved (player choice)
    /// </summary>
    [Serializable]
    public class AirRingNotResolvedEvent : GameEvent
    {
        /// <summary>
        /// Ring element that was not resolved
        /// </summary>
        public string RingElement { get; private set; }
        
        /// <summary>
        /// Reason why the air ring was not resolved
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Initialize air ring not resolved event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who chose not to resolve</param>
        /// <param name="ringElement">Ring element that wasn't resolved</param>
        /// <param name="reason">Reason for not resolving</param>
        /// <param name="source">Source of the effect</param>
        public AirRingNotResolvedEvent(Game game, Player triggeredBy, string ringElement = "air", string reason = "unknown", object source = null) 
            : base(game, triggeredBy, source)
        {
            RingElement = ringElement;
            Reason = reason;
            
            // Add specific event data
            AddEventData("ring_element", ringElement);
            AddEventData("player_id", triggeredBy.PlayerId);
            AddEventData("reason", reason);
            AddEventData("resolution_status", "not_resolved");
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string reasonText = !string.IsNullOrEmpty(Reason) ? $" ({Reason})" : "";
            return $"{TriggeredBy.Name} chooses not to resolve the {RingElement} ring{reasonText}";
        }
    }
}