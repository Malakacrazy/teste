using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a card is drawn
    /// </summary>
    [Serializable]
    public class CardDrawnEvent : GameEvent
    {
        /// <summary>
        /// Card that was drawn
        /// </summary>
        public BaseCard Card { get; private set; }
        
        /// <summary>
        /// Deck type the card was drawn from
        /// </summary>
        public string DeckType { get; private set; }
        
        /// <summary>
        /// Number of cards drawn in this batch
        /// </summary>
        public int CardsDrawnCount { get; private set; }
        
        /// <summary>
        /// Player's hand size after drawing
        /// </summary>
        public int HandSizeAfterDraw { get; private set; }
        
        /// <summary>
        /// Initialize card drawn event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who drew the card</param>
        /// <param name="card">Card that was drawn</param>
        /// <param name="deckType">Type of deck (conflict/dynasty)</param>
        /// <param name="cardsDrawnCount">Number of cards in this draw batch</param>
        /// <param name="handSizeAfterDraw">Hand size after drawing</param>
        /// <param name="source">Source of the effect</param>
        public CardDrawnEvent(Game game, Player triggeredBy, BaseCard card, string deckType, int cardsDrawnCount, int handSizeAfterDraw, object source = null) 
            : base(game, triggeredBy, source)
        {
            Card = card;
            DeckType = deckType;
            CardsDrawnCount = cardsDrawnCount;
            HandSizeAfterDraw = handSizeAfterDraw;
            
            // Add specific event data
            AddEventData("card_id", card.CardId);
            AddEventData("card_name", card.Name);
            AddEventData("card_type", card.CardType);
            AddEventData("deck_type", deckType);
            AddEventData("cards_drawn_count", cardsDrawnCount);
            AddEventData("hand_size_after", handSizeAfterDraw);
            AddEventData("player_id", triggeredBy?.PlayerId);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string batchText = CardsDrawnCount > 1 ? $" ({CardsDrawnCount} cards)" : "";
            return $"{TriggeredBy?.Name ?? "Unknown"} draws {Card.Name} from {DeckType} deck{batchText}";
        }
    }
}