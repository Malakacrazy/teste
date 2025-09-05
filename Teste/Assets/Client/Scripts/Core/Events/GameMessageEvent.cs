using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a game message should be displayed to players
    /// </summary>
    [Serializable]
    public class GameMessageEvent : GameEvent
    {
        /// <summary>
        /// The message to display
        /// </summary>
        public string Message { get; private set; }
        
        /// <summary>
        /// Message category for filtering/styling
        /// </summary>
        public string Category { get; private set; }
        
        /// <summary>
        /// Priority level for message display
        /// </summary>
        public int Priority { get; private set; }
        
        /// <summary>
        /// Initialize game message event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the message</param>
        /// <param name="message">Message to display</param>
        /// <param name="category">Message category</param>
        /// <param name="priority">Message priority (higher = more important)</param>
        /// <param name="source">Source of the message</param>
        public GameMessageEvent(Game game, Player triggeredBy, string message, string category = "general", int priority = 0, object source = null) 
            : base(game, triggeredBy, source)
        {
            Message = message;
            Category = category;
            Priority = priority;
            
            // Add specific event data
            AddEventData("message", message);
            AddEventData("category", category);
            AddEventData("priority", priority);
            AddEventData("player_id", triggeredBy?.PlayerId);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            return $"Game message ({Category}): {Message}";
        }
    }
}