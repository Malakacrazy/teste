namespace L5RGame
{
    /// <summary>
    /// Enumeration of possible card locations in the L5R game
    /// </summary>
    public enum CardLocation
    {
        /// <summary>
        /// Card is in play area
        /// </summary>
        PlayArea,
        
        /// <summary>
        /// Card is in discard pile
        /// </summary>
        DiscardPile,
        
        /// <summary>
        /// Card is in conflict discard pile
        /// </summary>
        ConflictDiscardPile,
        
        /// <summary>
        /// Card is in dynasty discard pile
        /// </summary>
        DynastyDiscardPile,
        
        /// <summary>
        /// Card is in hand
        /// </summary>
        Hand,
        
        /// <summary>
        /// Card is in deck
        /// </summary>
        Deck,
        
        /// <summary>
        /// Card is in conflict deck
        /// </summary>
        ConflictDeck,
        
        /// <summary>
        /// Card is in dynasty deck
        /// </summary>
        DynastyDeck,
        
        /// <summary>
        /// Card is in a province
        /// </summary>
        Province,
        
        /// <summary>
        /// Card is removed from game
        /// </summary>
        RemovedFromGame,
        
        /// <summary>
        /// Card is being played
        /// </summary>
        BeingPlayed,
        
        /// <summary>
        /// Card is in limbo (temporary state)
        /// </summary>
        Limbo,
        
        /// <summary>
        /// Card location is unknown or not set
        /// </summary>
        Unknown
    }
}