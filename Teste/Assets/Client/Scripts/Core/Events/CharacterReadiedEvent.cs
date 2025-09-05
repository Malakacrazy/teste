using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a character is readied
    /// </summary>
    [Serializable]
    public class CharacterReadiedEvent : GameEvent
    {
        /// <summary>
        /// Character that was readied
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Was the character already ready before this effect?
        /// </summary>
        public bool WasAlreadyReady { get; private set; }
        
        /// <summary>
        /// Reason for readying (e.g., "water ring effect", "start of turn")
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Initialize character readied event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who readied the character</param>
        /// <param name="character">Character being readied</param>
        /// <param name="wasAlreadyReady">Was already in ready state</param>
        /// <param name="reason">Reason for readying</param>
        /// <param name="source">Source of the effect</param>
        public CharacterReadiedEvent(Game game, Player triggeredBy, BaseCard character, bool wasAlreadyReady, string reason = "unknown", object source = null) 
            : base(game, triggeredBy, source)
        {
            Character = character;
            WasAlreadyReady = wasAlreadyReady;
            Reason = reason;
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("was_already_ready", wasAlreadyReady);
            AddEventData("ready_status_changed", !wasAlreadyReady);
            AddEventData("reason", reason);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string statusText = WasAlreadyReady ? " (already ready)" : " (newly readied)";
            string reasonText = !string.IsNullOrEmpty(Reason) ? $" - {Reason}" : "";
            return $"{Character.Name} is readied{statusText}{reasonText}";
        }
    }
}