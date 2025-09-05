using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a character is dishonored
    /// </summary>
    [Serializable]
    public class CharacterDishonoredEvent : GameEvent
    {
        /// <summary>
        /// Character that was dishonored
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Was the character already dishonored before this effect?
        /// </summary>
        public bool WasAlreadyDishonored { get; private set; }
        
        /// <summary>
        /// Initialize character dishonored event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who dishonored the character</param>
        /// <param name="character">Character being dishonored</param>
        /// <param name="wasAlreadyDishonored">Was already in dishonored state</param>
        /// <param name="source">Source of the effect</param>
        public CharacterDishonoredEvent(Game game, Player triggeredBy, BaseCard character, bool wasAlreadyDishonored, object source = null) 
            : base(game, triggeredBy, source)
        {
            Character = character;
            WasAlreadyDishonored = wasAlreadyDishonored;
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("was_already_dishonored", wasAlreadyDishonored);
            AddEventData("dishonor_status_changed", !wasAlreadyDishonored);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string statusText = WasAlreadyDishonored ? " (already dishonored)" : " (newly dishonored)";
            return $"{Character.Name} is dishonored{statusText}";
        }
    }
}