using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a card moves between locations
    /// </summary>
    [Serializable]
    public class CardMovedEvent : GameEvent
    {
        /// <summary>
        /// Card that was moved
        /// </summary>
        public BaseCard Card { get; private set; }
        
        /// <summary>
        /// Location the card moved from
        /// </summary>
        public CardLocation FromLocation { get; private set; }
        
        /// <summary>
        /// Location the card moved to
        /// </summary>
        public CardLocation ToLocation { get; private set; }
        
        /// <summary>
        /// Controller of the card before the move
        /// </summary>
        public Player FromController { get; private set; }
        
        /// <summary>
        /// Controller of the card after the move
        /// </summary>
        public Player ToController { get; private set; }
        
        /// <summary>
        /// Initialize card moved event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the move</param>
        /// <param name="card">Card that was moved</param>
        /// <param name="fromLocation">Source location</param>
        /// <param name="toLocation">Destination location</param>
        /// <param name="fromController">Previous controller</param>
        /// <param name="toController">New controller</param>
        /// <param name="source">Source of the move</param>
        public CardMovedEvent(Game game, Player triggeredBy, BaseCard card, 
            CardLocation fromLocation, CardLocation toLocation, 
            Player fromController, Player toController, object source = null) 
            : base(game, triggeredBy, source)
        {
            Card = card;
            FromLocation = fromLocation;
            ToLocation = toLocation;
            FromController = fromController;
            ToController = toController;
            
            // Add specific event data
            AddEventData("card_id", card.CardId);
            AddEventData("card_name", card.Name);
            AddEventData("from_location", fromLocation.ToString());
            AddEventData("to_location", toLocation.ToString());
            AddEventData("from_controller", fromController.PlayerId);
            AddEventData("to_controller", toController.PlayerId);
            AddEventData("zone_change", fromLocation != toLocation);
            AddEventData("controller_change", fromController != toController);
            AddEventData("player_id", triggeredBy?.PlayerId);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            var controllerChange = FromController != ToController ? $" (controller: {FromController.Name} → {ToController.Name})" : "";
            return $"{Card.Name} moved from {FromLocation} to {ToLocation}{controllerChange}";
        }
    }
}