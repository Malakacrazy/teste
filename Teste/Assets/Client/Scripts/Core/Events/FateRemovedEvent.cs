using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when fate is removed from a character
    /// </summary>
    [Serializable]
    public class FateRemovedEvent : GameEvent
    {
        /// <summary>
        /// Character that had fate removed
        /// </summary>
        public BaseCard Character { get; private set; }
        
        /// <summary>
        /// Amount of fate that was removed
        /// </summary>
        public int AmountRemoved { get; private set; }
        
        /// <summary>
        /// Will the character leave play due to this fate removal?
        /// </summary>
        public bool WillCharacterLeave { get; private set; }
        
        /// <summary>
        /// Character's fate count before removal
        /// </summary>
        public int FateBeforeRemoval { get; private set; }
        
        /// <summary>
        /// Character's fate count after removal
        /// </summary>
        public int FateAfterRemoval { get; private set; }
        
        /// <summary>
        /// Initialize fate removed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the fate removal</param>
        /// <param name="character">Character losing fate</param>
        /// <param name="amountRemoved">Amount of fate removed</param>
        /// <param name="source">Source of the effect</param>
        public FateRemovedEvent(Game game, Player triggeredBy, BaseCard character, int amountRemoved, object source = null) 
            : base(game, triggeredBy, source)
        {
            Character = character;
            AmountRemoved = amountRemoved;
            FateAfterRemoval = character.FateTokens;
            FateBeforeRemoval = FateAfterRemoval + amountRemoved;
            WillCharacterLeave = FateAfterRemoval <= 0;
            
            // Add specific event data
            AddEventData("character_id", character.CardId);
            AddEventData("character_name", character.Name);
            AddEventData("character_owner", character.Owner?.PlayerId);
            AddEventData("amount_removed", amountRemoved);
            AddEventData("fate_before", FateBeforeRemoval);
            AddEventData("fate_after", FateAfterRemoval);
            AddEventData("will_leave_play", WillCharacterLeave);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string leaveText = WillCharacterLeave ? " (character will leave play)" : "";
            return $"Fate removed from {Character.Name}: -{AmountRemoved} ({FateBeforeRemoval}→{FateAfterRemoval}){leaveText}";
        }
    }
}