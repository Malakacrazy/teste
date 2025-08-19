using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class Player : MonoBehaviour
    {
        // Basic properties
        public string id;
        public string name;
        public string emailHash;
        public string lobbyId;
        public bool owner;
        public bool left;
        public bool disconnected;
        public object socket;
        
        // Game stats
        public int honor;
        public int fate;
        public bool firstPlayer;
        public Player opponent;
        
        // Game state
        public bool showConflict;
        public bool showDynasty;
        public string imperialFavor;
        public Faction faction;
        public Deck deck;
        public PreparedDeck preparedDeck;
        public BaseCard stronghold;
        
        // Card collections
        public List<BaseCard> cardsInPlay = new List<BaseCard>();
        
        // Settings
        public Dictionary<string, bool> promptedActionWindows = new Dictionary<string, bool>();
        public Dictionary<string, bool> timerSettings = new Dictionary<string, bool>();
        public Dictionary<string, bool> optionSettings = new Dictionary<string, bool>();
        
        // Conflict opportunities
        public ConflictOpportunities conflictOpportunities = new ConflictOpportunities();
        
        // Placeholder methods
        public void Initialize(string playerId, UserInfo user, bool isOwner, Game gameInstance, ClockSettings clockSettings) { }
        public void Initialize() { }
        public BaseCard FindCardInPlayByUuid(string cardId) => null;
        public List<BaseCard> FindCards(List<BaseCard> list, System.Func<BaseCard, bool> predicate) => new List<BaseCard>();
        public void StopClock() { }
        public void ResetClock() { }
        public List<BaseCard> GetSourceList(string location) => new List<BaseCard>();
        public void ShowConflictDeck() { }
        public void ShowDynastyDeck() { }
        public void Drop(string cardId, string source, string target) { }
        public void SelectDeck(Deck selectedDeck) { }
        public void ShuffleConflictDeck() { }
        public void ShuffleDynastyDeck() { }
        public int GetTotalHonor() => honor;
        public object GetState(object activePlayer) => null;
        public List<BaseCard> GetProvinces() => new List<BaseCard>();
        public bool CheckRestrictions(string restriction) => true;
        public void LoseImperialFavor() { }
        public bool IsAttackingPlayer() => false;
        public void RemoveCardFromPile(BaseCard card) { }
    }
}