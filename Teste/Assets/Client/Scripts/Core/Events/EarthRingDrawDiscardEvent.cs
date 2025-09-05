using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when Earth Ring effect draws cards and forces discard
    /// </summary>
    [Serializable]
    public class EarthRingDrawDiscardEvent : GameEvent
    {
        /// <summary>
        /// Number of cards drawn by the player
        /// </summary>
        public int CardsDrawn { get; private set; }
        
        /// <summary>
        /// Number of cards discarded by opponent
        /// </summary>
        public int CardsDiscarded { get; private set; }
        
        /// <summary>
        /// Whether opponent actually discarded cards
        /// </summary>
        public bool OpponentDiscarded { get; private set; }
        
        /// <summary>
        /// Whether the discard was random or chosen
        /// </summary>
        public bool DiscardWasRandom { get; private set; }
        
        /// <summary>
        /// Net card advantage gained (+/- cards)
        /// </summary>
        public int CardAdvantage { get; private set; }
        
        /// <summary>
        /// Initialize earth ring draw discard event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the earth ring</param>
        /// <param name="cardsDrawn">Number of cards drawn</param>
        /// <param name="cardsDiscarded">Number of cards discarded</param>
        /// <param name="opponentDiscarded">Whether opponent discarded</param>
        /// <param name="discardWasRandom">Whether discard was random</param>
        /// <param name="source">Source of the effect</param>
        public EarthRingDrawDiscardEvent(Game game, Player triggeredBy, int cardsDrawn, int cardsDiscarded, bool opponentDiscarded, bool discardWasRandom, object source = null) 
            : base(game, triggeredBy, source)
        {
            CardsDrawn = cardsDrawn;
            CardsDiscarded = cardsDiscarded;
            OpponentDiscarded = opponentDiscarded;
            DiscardWasRandom = discardWasRandom;
            CardAdvantage = cardsDrawn + (opponentDiscarded ? cardsDiscarded : 0);
            
            // Add specific event data
            AddEventData("cards_drawn", cardsDrawn);
            AddEventData("cards_discarded", cardsDiscarded);
            AddEventData("opponent_discarded", opponentDiscarded);
            AddEventData("discard_was_random", discardWasRandom);
            AddEventData("card_advantage", CardAdvantage);
            AddEventData("ring_element", "earth");
            AddEventData("player_id", triggeredBy.PlayerId);
            
            if (triggeredBy.Opponent != null)
            {
                AddEventData("opponent_id", triggeredBy.Opponent.PlayerId);
                AddEventData("player_hand_size_after", triggeredBy.Hand.Count);
                AddEventData("opponent_hand_size_after", triggeredBy.Opponent.Hand.Count);
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string description = $"{TriggeredBy.Name} resolves the earth ring, drawing {CardsDrawn} card(s)";
            
            if (OpponentDiscarded)
            {
                string discardType = DiscardWasRandom ? "at random" : "of choice";
                description += $" and forcing opponent to discard {CardsDiscarded} card(s) {discardType}";
            }
            
            return description;
        }
    }
}