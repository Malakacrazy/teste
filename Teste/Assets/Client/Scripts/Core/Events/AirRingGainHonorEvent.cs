using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when Air Ring effect is used to gain honor
    /// </summary>
    [Serializable]
    public class AirRingGainHonorEvent : L5RGame.GameEvent
    {
        /// <summary>
        /// Amount of honor gained
        /// </summary>
        public int HonorGained { get; private set; }
        
        /// <summary>
        /// Player's total honor after gaining
        /// </summary>
        public int TotalHonorAfter { get; private set; }
        
        /// <summary>
        /// Initialize air ring gain honor event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who gained honor</param>
        /// <param name="honorGained">Amount of honor gained</param>
        /// <param name="totalHonorAfter">Total honor after gaining</param>
        /// <param name="source">Source of the effect</param>
        public AirRingGainHonorEvent(Game game, Player triggeredBy, int honorGained, int totalHonorAfter, object source = null) 
            : base(game, triggeredBy, source)
        {
            HonorGained = honorGained;
            TotalHonorAfter = totalHonorAfter;
            
            // Add specific event data
            AddEventData("ring_element", "air");
            AddEventData("choice", "gain_honor");
            AddEventData("player_id", triggeredBy.PlayerId);
            AddEventData("honor_gained", honorGained);
            AddEventData("total_honor_after", totalHonorAfter);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            return $"{TriggeredBy.Name} resolves the air ring, gaining {HonorGained} honor (total: {TotalHonorAfter})";
        }
    }
}