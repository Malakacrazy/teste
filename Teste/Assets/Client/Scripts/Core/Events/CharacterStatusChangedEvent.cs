using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a character's status (bowed/ready) changes
    /// </summary>
    [Serializable]
    public class CharacterStatusChangedEvent : GameEvent
    {
        /// <summary>
        /// Character whose status changed
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Was the character bowed before this change
        /// </summary>
        public bool WasBowed { get; private set; }
        
        /// <summary>
        /// Is the character bowed after this change
        /// </summary>
        public bool IsBowed { get; private set; }
        
        /// <summary>
        /// Description of the status change
        /// </summary>
        public string StatusChange { get; private set; }
        
        /// <summary>
        /// Initialize character status changed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the status change</param>
        /// <param name="character">Character whose status changed</param>
        /// <param name="wasBowed">Was character bowed before</param>
        /// <param name="isBowed">Is character bowed after</param>
        /// <param name="source">Source of the status change</param>
        public CharacterStatusChangedEvent(Game game, Player triggeredBy, BaseCard character, 
            bool wasBowed, bool isBowed, object source = null) 
            : base(game, triggeredBy, source)
        {
            Character = character;
            WasBowed = wasBowed;
            IsBowed = isBowed;
            StatusChange = (wasBowed, isBowed) switch
            {
                (false, true) => "bowed",
                (true, false) => "readied",
                _ => "no_change"
            };
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("was_bowed", wasBowed);
            AddEventData("is_bowed", isBowed);
            AddEventData("status_change", StatusChange);
            AddEventData("player_id", triggeredBy?.PlayerId);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            return $"{Character.Name} {StatusChange}";
        }
    }
}