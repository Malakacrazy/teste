using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a conflict starts
    /// </summary>
    [Serializable]
    public class ConflictStartedEvent : GameEvent
    {
        /// <summary>
        /// Player who initiated the conflict
        /// </summary>
        public Player AttackingPlayer { get; private set; }
        
        /// <summary>
        /// Player defending in the conflict
        /// </summary>
        public Player DefendingPlayer { get; private set; }
        
        /// <summary>
        /// Type of conflict (Military, Political)
        /// </summary>
        public string ConflictType { get; private set; }
        
        /// <summary>
        /// Ring being contested
        /// </summary>
        public Ring TargetRing { get; private set; }
        
        /// <summary>
        /// The conflict instance
        /// </summary>
        public Conflict Conflict { get; private set; }
        
        /// <summary>
        /// Initialize conflict started event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="attackingPlayer">Attacking player</param>
        /// <param name="defendingPlayer">Defending player</param>
        /// <param name="conflictType">Type of conflict</param>
        /// <param name="targetRing">Ring being contested</param>
        /// <param name="conflict">Conflict instance</param>
        /// <param name="source">Source of the conflict</param>
        public ConflictStartedEvent(Game game, Player attackingPlayer, Player defendingPlayer, 
            string conflictType, Ring targetRing, Conflict conflict = null, object source = null) 
            : base(game, attackingPlayer, source)
        {
            AttackingPlayer = attackingPlayer;
            DefendingPlayer = defendingPlayer;
            ConflictType = conflictType;
            TargetRing = targetRing;
            Conflict = conflict;
            
            // Add specific event data
            AddEventData("attacking_player", attackingPlayer.PlayerId);
            AddEventData("defending_player", defendingPlayer.PlayerId);
            AddEventData("conflict_type", conflictType);
            AddEventData("target_ring", targetRing?.element);
            AddEventData("conflict_id", conflict?.GetHashCode().ToString() ?? Guid.NewGuid().ToString());
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            return $"{AttackingPlayer.Name} starts {ConflictType} conflict against {DefendingPlayer.Name} targeting {TargetRing?.element} ring";
        }
    }
}