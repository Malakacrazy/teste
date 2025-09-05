using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a character leaves play
    /// </summary>
    [Serializable]
    public class CharacterLeavesPlayEvent : GameEvent
    {
        /// <summary>
        /// Character that is leaving play
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Where the character is going
        /// </summary>
        public string Destination { get; private set; }
        
        /// <summary>
        /// Reason for leaving play
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Location the character was in before leaving
        /// </summary>
        public string PreviousLocation { get; private set; }
        
        /// <summary>
        /// Initialize character leaves play event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="character">Character leaving play</param>
        /// <param name="destination">Where character is going</param>
        /// <param name="reason">Reason for leaving</param>
        /// <param name="source">Source of the effect</param>
        public CharacterLeavesPlayEvent(Game game, BaseCard character, string destination, string reason, object source = null) 
            : base(game, character.Owner, source)
        {
            Character = character;
            Destination = destination;
            Reason = reason;
            PreviousLocation = character.Location;
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("destination", destination);
            AddEventData("reason", reason);
            AddEventData("previous_location", PreviousLocation);
            AddEventData("fate_tokens", character.FateTokens);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            return $"{Character.Name} leaves play → {Destination} (reason: {Reason})";
        }
    }
}