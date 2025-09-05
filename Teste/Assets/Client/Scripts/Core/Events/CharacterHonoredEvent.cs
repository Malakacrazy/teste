using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a character is honored
    /// </summary>
    [Serializable]
    public class CharacterHonoredEvent : GameEvent
    {
        /// <summary>
        /// Character that was honored
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Was the character already honored before this effect?
        /// </summary>
        public bool WasAlreadyHonored { get; private set; }
        
        /// <summary>
        /// Initialize character honored event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who honored the character</param>
        /// <param name="character">Character being honored</param>
        /// <param name="wasAlreadyHonored">Was already in honored state</param>
        /// <param name="source">Source of the effect</param>
        public CharacterHonoredEvent(Game game, Player triggeredBy, BaseCard character, bool wasAlreadyHonored, object source = null) 
            : base(game, triggeredBy, source)
        {
            Character = character;
            WasAlreadyHonored = wasAlreadyHonored;
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("was_already_honored", wasAlreadyHonored);
            AddEventData("honor_status_changed", !wasAlreadyHonored);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string statusText = WasAlreadyHonored ? " (already honored)" : " (newly honored)";
            return $"{Character.Name} is honored{statusText}";
        }
    }
}