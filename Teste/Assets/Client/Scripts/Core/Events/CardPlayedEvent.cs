using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a card is played
    /// </summary>
    [Serializable]
    public class CardPlayedEvent : GameEvent
    {
        /// <summary>
        /// Card that was played
        /// </summary>
        public BaseCard Card { get; private set; }
        
        /// <summary>
        /// Player who played the card
        /// </summary>
        public Player PlayingPlayer { get; private set; }
        
        /// <summary>
        /// Cost paid to play the card
        /// </summary>
        public int CostPaid { get; private set; }
        
        /// <summary>
        /// Where the card was played to
        /// </summary>
        public string PlayedTo { get; private set; }
        
        /// <summary>
        /// Initialize card played event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="playingPlayer">Player who played the card</param>
        /// <param name="card">Card that was played</param>
        /// <param name="costPaid">Cost paid</param>
        /// <param name="playedTo">Where card was played</param>
        /// <param name="source">Source of the play</param>
        public CardPlayedEvent(Game game, Player playingPlayer, BaseCard card, int costPaid, 
            string playedTo = "play", object source = null) 
            : base(game, playingPlayer, source)
        {
            Card = card;
            PlayingPlayer = playingPlayer;
            CostPaid = costPaid;
            PlayedTo = playedTo;
            
            // Add specific event data
            AddEventData("card_id", card.CardId);
            AddEventData("card_name", card.Name);
            AddEventData("card_type", card.CardType.ToString());
            AddEventData("cost_paid", costPaid);
            AddEventData("played_to", playedTo);
            AddEventData("player_id", playingPlayer.PlayerId);
            AddEventData("fate_remaining", playingPlayer.Fate);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            return $"{PlayingPlayer.Name} played {Card.Name} for {CostPaid} fate";
        }
    }
}