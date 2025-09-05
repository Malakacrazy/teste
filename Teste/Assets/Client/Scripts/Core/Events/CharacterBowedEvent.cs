using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a character is bowed
    /// </summary>
    [Serializable]
    public class CharacterBowedEvent : GameEvent
    {
        /// <summary>
        /// Character that was bowed
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Was the character already bowed before this effect?
        /// </summary>
        public bool WasAlreadyBowed { get; private set; }
        
        /// <summary>
        /// Reason for bowing (e.g., "water ring effect", "conflict participation")
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Initialize character bowed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who bowed the character</param>
        /// <param name="character">Character being bowed</param>
        /// <param name="wasAlreadyBowed">Was already in bowed state</param>
        /// <param name="reason">Reason for bowing</param>
        /// <param name="source">Source of the effect</param>
        public CharacterBowedEvent(Game game, Player triggeredBy, BaseCard character, bool wasAlreadyBowed, string reason = "unknown", object source = null) 
            : base(game, triggeredBy, source)
        {
            Character = character;
            WasAlreadyBowed = wasAlreadyBowed;
            Reason = reason;
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("was_already_bowed", wasAlreadyBowed);
            AddEventData("bow_status_changed", !wasAlreadyBowed);
            AddEventData("reason", reason);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string statusText = WasAlreadyBowed ? " (already bowed)" : " (newly bowed)";
            string reasonText = !string.IsNullOrEmpty(Reason) ? $" - {Reason}" : "";
            return $"{Character.Name} is bowed{statusText}{reasonText}";
        }
    }
}