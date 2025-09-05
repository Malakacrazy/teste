using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when Air Ring effect is used to take honor from opponent
    /// </summary>
    [Serializable]
    public class AirRingTakeHonorEvent : GameEvent
    {
        /// <summary>
        /// Player who honor was taken from
        /// </summary>
        public Player Target { get; private set; }
        
        /// <summary>
        /// Amount of honor taken
        /// </summary>
        public int HonorTaken { get; private set; }
        
        /// <summary>
        /// Player's honor before taking
        /// </summary>
        public int PlayerHonorBefore { get; private set; }
        
        /// <summary>
        /// Player's honor after taking
        /// </summary>
        public int PlayerHonorAfter { get; private set; }
        
        /// <summary>
        /// Target's honor before losing
        /// </summary>
        public int TargetHonorBefore { get; private set; }
        
        /// <summary>
        /// Target's honor after losing
        /// </summary>
        public int TargetHonorAfter { get; private set; }
        
        /// <summary>
        /// Total honor swing (player gain + opponent loss)
        /// </summary>
        public int HonorSwing { get; private set; }
        
        /// <summary>
        /// Initialize air ring take honor event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who took honor</param>
        /// <param name="target">Player who lost honor</param>
        /// <param name="honorTaken">Amount of honor taken</param>
        /// <param name="playerHonorBefore">Player's honor before</param>
        /// <param name="playerHonorAfter">Player's honor after</param>
        /// <param name="targetHonorBefore">Target's honor before</param>
        /// <param name="targetHonorAfter">Target's honor after</param>
        /// <param name="source">Source of the effect</param>
        public AirRingTakeHonorEvent(Game game, Player triggeredBy, Player target, int honorTaken, 
            int playerHonorBefore, int playerHonorAfter, int targetHonorBefore, int targetHonorAfter, object source = null) 
            : base(game, triggeredBy, source)
        {
            Target = target;
            HonorTaken = honorTaken;
            PlayerHonorBefore = playerHonorBefore;
            PlayerHonorAfter = playerHonorAfter;
            TargetHonorBefore = targetHonorBefore;
            TargetHonorAfter = targetHonorAfter;
            HonorSwing = (playerHonorAfter - playerHonorBefore) + (targetHonorBefore - targetHonorAfter);
            
            // Add specific event data
            AddEventData("ring_element", "air");
            AddEventData("choice", "take_honor");
            AddEventData("player_id", triggeredBy.PlayerId);
            AddEventData("target_id", target.PlayerId);
            AddEventData("honor_taken", honorTaken);
            AddEventData("player_honor_before", playerHonorBefore);
            AddEventData("player_honor_after", playerHonorAfter);
            AddEventData("target_honor_before", targetHonorBefore);
            AddEventData("target_honor_after", targetHonorAfter);
            AddEventData("honor_swing", HonorSwing);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            return $"{TriggeredBy.Name} resolves the air ring, taking {HonorTaken} honor from {Target.Name} (swing: {HonorSwing})";
        }
    }
}