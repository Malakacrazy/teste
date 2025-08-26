using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    // Note: Constants moved to GameConstants.cs to avoid duplicates

    // Note: EffectNames moved to GameConstants.cs

    // Note: EventNames and AbilityTypes are in GameConstants.cs

    public static class RingEffects
    {
        public static AbilityContext ContextFor(Player player, string element, bool optional)
        {
            var context = new GameObject("AbilityContext").AddComponent<AbilityContext>();
            context.game = player.game;
            context.player = player;
            return context;
        }
    }

    // Placeholder classes for missing types
    public class AttachmentPrompt
    {
        public AttachmentPrompt(Game game, Player player, BaseCard card, string playingType) { }
    }

    [System.Serializable]
    public class PlayerSettings
    {
        public Dictionary<string, bool> promptedActionWindows = new Dictionary<string, bool>
        {
            {"dynasty", true},
            {"draw", true},
            {"preConflict", true},
            {"conflict", true},
            {"fate", true},
            {"regroup", true}
        };
        
        public Dictionary<string, object> timerSettings = new Dictionary<string, object>();
        public Dictionary<string, object> optionSettings = new Dictionary<string, object>();
        public int windowTimer = 10;
    }

    public class Player : MonoBehaviour
    {
        [Header("Player Identity")]
        public UserInfo user;
        public string emailHash;
        public string id;
        public bool owner;
        public string printedType = "player";
        
        [Header("Network State")]
        public object socket;
        public bool disconnected = false;
        public bool left = false;
        public string lobbyId;

        [Header("Card Collections")]
        public List<BaseCard> dynastyDeck = new List<BaseCard>();
        public List<BaseCard> conflictDeck = new List<BaseCard>();
        public List<BaseCard> provinceDeck = new List<BaseCard>();
        public List<BaseCard> hand = new List<BaseCard>();
        public List<BaseCard> cardsInPlay = new List<BaseCard>();
        
        // Province locations
        public List<BaseCard> strongholdProvince = new List<BaseCard>();
        public List<BaseCard> provinceOne = new List<BaseCard>();
        public List<BaseCard> provinceTwo = new List<BaseCard>();
        public List<BaseCard> provinceThree = new List<BaseCard>();
        public List<BaseCard> provinceFour = new List<BaseCard>();
        
        // Discard and special locations
        public List<BaseCard> dynastyDiscardPile = new List<BaseCard>();
        public List<BaseCard> conflictDiscardPile = new List<BaseCard>();
        public List<BaseCard> removedFromGame = new List<BaseCard>();
        public List<BaseCard> underneathStronghold = new List<BaseCard>();
        
        public Dictionary<string, AdditionalPile> additionalPiles = new Dictionary<string, AdditionalPile>();

        [Header("Player Cards")]
        public Faction faction;
        public BaseCard stronghold;
        public BaseCard role;

        [Header("Phase Values")]
        public bool hideProvinceDeck = false;
        public bool takenDynastyMulligan = false;
        public bool takenConflictMulligan = false;
        public bool passedDynasty = false;
        public bool actionPhasePriority = false;
        public int honorBidModifier = 0;
        public int showBid = 0;
        public ConflictOpportunities conflictOpportunities = new ConflictOpportunities();
        public string imperialFavor = "";

        [Header("Game Resources")]
        public int fate = 0;
        public int honor = 0;
        public bool readyToStart = false;
        public int limitedPlayed = 0;
        public int maxLimited = 1;
        public bool firstPlayer = false;

        [Header("Game State")]
        public bool showConflict = false;
        public bool showDynasty = false;
        public bool resetTimerAtEndOfRound = false;
        public bool noTimer = false;

        // References
        public Player opponent;
        public Deck deck;
        public Game game;
        public MonoBehaviour clock;
        public PreparedDeck preparedDeck;

        // Systems
        private List<object> costReducers = new List<object>();
        private List<object> playableLocations = new List<object>();
        private Dictionary<string, object> abilityMaxByIdentifier = new Dictionary<string, object>();
        private PlayerSettings settings = new PlayerSettings();
        private object promptState;

        // Static location arrays for easy reference
        private static readonly string[] ProvinceLocations = {
            Locations.StrongholdProvince,
            Locations.ProvinceOne,
            Locations.ProvinceTwo,
            Locations.ProvinceThree,
            Locations.ProvinceFour
        };

        // Properties that need to be accessible from Game.cs
        public Dictionary<string, bool> promptedActionWindows => settings.promptedActionWindows;
        public Dictionary<string, object> timerSettings => settings.timerSettings;
        public Dictionary<string, object> optionSettings => settings.optionSettings;

        // Placeholder methods for missing functionality
        public List<object> GetEffects(string effectName)
        {
            return new List<object>();
        }

        public object MostRecentEffect(string effectName)
        {
            return null;
        }

        public bool AnyEffect(string effectName)
        {
            return false;
        }

        public bool CheckRestrictions(string restriction, AbilityContext context = null)
        {
            return true; // Placeholder implementation
        }

        public void Initialize(string playerId, UserInfo userInfo, bool isOwner, Game gameInstance, ClockSettings clockSettings)
        {
            id = playerId;
            user = userInfo;
            emailHash = userInfo.emailHash;
            owner = isOwner;
            game = gameInstance;
            
            // Initialize clock - placeholder
            clock = gameObject.AddComponent<MonoBehaviour>();
            
            // Initialize prompt state
            promptState = new object();
            
            // Set up initial playable locations
            InitializePlayableLocations();
            
            Debug.Log($"Player {userInfo.username} initialized");
        }

        private void InitializePlayableLocations()
        {
            playableLocations = new List<object>
            {
                new object(), // Placeholder implementations
                new object(),
                new object(),
                new object(),
                new object()
            };
        }

        // Clock management - placeholders
        public void StartClock() { }
        public void StopClock() { }
        public void ResetClock() { }

        // Card searching methods - simplified placeholders
        public bool IsCardUuidInList(List<BaseCard> list, BaseCard card)
        {
            return list.Any(c => c.uuid == card.uuid);
        }

        public bool IsCardNameInList(List<BaseCard> list, BaseCard card)
        {
            return list.Any(c => c.name == card.name);
        }

        public bool AreCardsSelected()
        {
            return false; // Placeholder
        }

        public List<BaseCard> RemoveCardByUuid(List<BaseCard> list, string uuid)
        {
            return list.Where(card => card.uuid != uuid).ToList();
        }

        public BaseCard FindCardByName(List<BaseCard> list, string name)
        {
            return FindCard(list, card => card.name == name);
        }

        public BaseCard FindCardByUuid(List<BaseCard> list, string uuid)
        {
            return FindCard(list, card => card.uuid == uuid);
        }

        public BaseCard FindCardInPlayByUuid(string uuid)
        {
            return FindCard(cardsInPlay, card => card.uuid == uuid);
        }

        public BaseCard FindCard(List<BaseCard> cardList, System.Func<BaseCard, bool> predicate)
        {
            var cards = FindCards(cardList, predicate);
            return cards.FirstOrDefault();
        }

        public List<BaseCard> FindCards(List<BaseCard> cardList, System.Func<BaseCard, bool> predicate)
        {
            if (cardList == null) return new List<BaseCard>();
            return cardList.Where(predicate).ToList();
        }

        public bool AreLocationsAdjacent(string location1, string location2)
        {
            int index1 = Array.IndexOf(ProvinceLocations, location1);
            int index2 = Array.IndexOf(ProvinceLocations, location2);
            return index1 > -1 && index2 > -1 && Mathf.Abs(index1 - index2) == 1;
        }

        // Province management - placeholders
        public BaseCard GetDynastyCardInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.FirstOrDefault();
        }

        public List<BaseCard> GetDynastyCardsInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.ToList();
        }

        public BaseCard GetProvinceCardInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.FirstOrDefault();
        }

        public bool AnyCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return cardsInPlay.Any(predicate);
        }

        public List<BaseCard> FilterCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return cardsInPlay.Where(predicate).ToList();
        }

        // Game state properties
        public bool HasComposure()
        {
            return opponent != null && opponent.showBid > showBid;
        }

        // Placeholder methods for conflict management
        public List<string> GetLegalConflictTypes(ConflictProperties properties)
        {
            return new List<string> { ConflictTypes.Military, ConflictTypes.Political };
        }

        public bool HasLegalConflictDeclaration(ConflictProperties properties)
        {
            return true; // Placeholder
        }

        public List<BaseCard> GetProvinces(System.Func<BaseCard, bool> predicate = null)
        {
            predicate = predicate ?? (card => true);
            var provinces = new List<BaseCard>();

            foreach (var location in ProvinceLocations)
            {
                provinces.AddRange(GetSourceList(location).Where(predicate));
            }

            return provinces;
        }

        public int GetNumberOfFaceupProvinces(System.Func<BaseCard, bool> predicate = null)
        {
            return GetProvinces().Count; // Placeholder
        }

        public int GetNumberOfOpponentsFaceupProvinces(System.Func<BaseCard, bool> predicate = null)
        {
            return opponent?.GetNumberOfFaceupProvinces(predicate) ?? 0;
        }

        public int GetNumberOfCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return cardsInPlay.Count(predicate);
        }

        public int GetNumberOfHoldingsInPlay()
        {
            return GetHoldingsInPlay().Count;
        }

        public List<BaseCard> GetHoldingsInPlay()
        {
            var holdings = new List<BaseCard>();
            foreach (var province in ProvinceLocations)
            {
                holdings.AddRange(GetSourceList(province));
            }
            return holdings;
        }

        public bool IsCardInPlayableLocation(BaseCard card, string playingType = null)
        {
            return true; // Placeholder
        }

        public BaseCard GetDuplicateInPlay(BaseCard card)
        {
            return null; // Placeholder
        }

        // Deck management with placeholder implementations
        public void DrawCardsToHand(int numCards)
        {
            var cardsToDraw = conflictDeck.Take(numCards).ToList();
            foreach (var card in cardsToDraw)
            {
                MoveCard(card, Locations.Hand);
            }
        }

        public void DeckRanOutOfCards(string deckType)
        {
            game.AddMessage("{0}'s {1} deck has run out of cards", this, deckType);
        }

        public bool ReplaceDynastyCard(string location)
        {
            if (dynastyDeck.Count > 0)
            {
                MoveCard(dynastyDeck.First(), location);
            }
            return true;
        }

        public void ShuffleConflictDeck()
        {
            conflictDeck = conflictDeck.OrderBy(x => UnityEngine.Random.value).ToList();
        }

        public void ShuffleDynastyDeck()
        {
            dynastyDeck = dynastyDeck.OrderBy(x => UnityEngine.Random.value).ToList();
        }

        // Conflict management
        public void AddConflictOpportunity(string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                switch (type.ToLower())
                {
                    case "military":
                        conflictOpportunities.military++;
                        break;
                    case "political":
                        conflictOpportunities.political++;
                        break;
                }
            }
            conflictOpportunities.total++;
        }

        public int GetConflictOpportunities(string type = "total")
        {
            switch (type.ToLower())
            {
                case "military":
                    return conflictOpportunities.military;
                case "political":
                    return conflictOpportunities.political;
                default:
                    return conflictOpportunities.total;
            }
        }

        // Simplified placeholder methods
        public void PrepareDecks() { }

        public void Initialize()
        {
            opponent = game.GetOtherPlayer(this);
            PrepareDecks();
            ShuffleConflictDeck();
            ShuffleDynastyDeck();
            
            fate = 0;
            honor = 0;
            readyToStart = false;
            limitedPlayed = 0;
            maxLimited = 1;
            firstPlayer = false;
        }

        // Cost system placeholders
        public object AddCostReducer(object source, object properties)
        {
            var reducer = new object();
            costReducers.Add(reducer);
            return reducer;
        }

        public void RemoveCostReducer(object reducer) { costReducers.Remove(reducer); }

        public object AddPlayableLocation(string type, Player player, string location, List<BaseCard> cards = null)
        {
            var playableLocation = new object();
            playableLocations.Add(playableLocation);
            return playableLocation;
        }

        public void RemovePlayableLocation(object location) { playableLocations.Remove(location); }

        public List<object> GetAlternateFatePools(string playingType, BaseCard card, AbilityContext context)
        {
            return new List<object>();
        }

        public int GetMinimumCost(string playingType, AbilityContext context, BaseCard target, bool ignoreType = false)
        {
            return 0;
        }

        public int GetReducedCost(string playingType, BaseCard card, BaseCard target, bool ignoreType = false)
        {
            return 0;
        }

        public int GetAvailableAlternateFate(string playingType, AbilityContext context) { return 0; }
        public int GetTargetingCost(BaseCard abilitySource, object targets) { return 0; }
        public void MarkUsedReducers(string playingType, BaseCard card, BaseCard target = null) { }

        // Ability limit management - placeholders
        public void RegisterAbilityMax(string maxIdentifier, object limit)
        {
            abilityMaxByIdentifier[maxIdentifier] = limit;
        }

        public bool IsAbilityAtMax(string maxIdentifier)
        {
            return abilityMaxByIdentifier.ContainsKey(maxIdentifier);
        }

        public void IncrementAbilityMax(string maxIdentifier) { }

        // Phase management
        public void BeginDynasty()
        {
            passedDynasty = false;
            limitedPlayed = 0;
            conflictOpportunities.military = 1;
            conflictOpportunities.political = 1;
            conflictOpportunities.total = 2;
        }

        public void CollectFate() { ModifyFate(GetTotalIncome()); }
        public void ShowConflictDeck() { showConflict = true; }
        public void ShowDynastyDeck() { showDynasty = true; }

        // List management methods
        public List<BaseCard> GetSourceList(string source)
        {
            switch (source)
            {
                case Locations.Hand: return hand;
                case Locations.ConflictDeck: return conflictDeck;
                case Locations.DynastyDeck: return dynastyDeck;
                case Locations.ConflictDiscardPile: return conflictDiscardPile;
                case Locations.DynastyDiscardPile: return dynastyDiscardPile;
                case Locations.RemovedFromGame: return removedFromGame;
                case Locations.PlayArea: return cardsInPlay;
                case Locations.ProvinceOne: return provinceOne;
                case Locations.ProvinceTwo: return provinceTwo;
                case Locations.ProvinceThree: return provinceThree;
                case Locations.ProvinceFour: return provinceFour;
                case Locations.StrongholdProvince: return strongholdProvince;
                case Locations.ProvinceDeck: return provinceDeck;
                case Locations.UnderneathStronghold: return underneathStronghold;
                case Locations.Provinces:
                    var allProvinces = new List<BaseCard>();
                    allProvinces.AddRange(provinceOne);
                    allProvinces.AddRange(provinceTwo);
                    allProvinces.AddRange(provinceThree);
                    allProvinces.AddRange(provinceFour);
                    allProvinces.AddRange(strongholdProvince);
                    return allProvinces;
                default:
                    if (additionalPiles.ContainsKey(source))
                        return additionalPiles[source].cards;
                    break;
            }
            return new List<BaseCard>();
        }

        public void CreateAdditionalPile(string name, AdditionalPileProperties properties)
        {
            additionalPiles[name] = new AdditionalPile
            {
                cards = new List<BaseCard>(),
                properties = properties
            };
        }

        public void UpdateSourceList(string source, List<BaseCard> targetList)
        {
            switch (source)
            {
                case Locations.Hand: hand = targetList; break;
                case Locations.ConflictDeck: conflictDeck = targetList; break;
                case Locations.DynastyDeck: dynastyDeck = targetList; break;
                case Locations.ConflictDiscardPile: conflictDiscardPile = targetList; break;
                case Locations.DynastyDiscardPile: dynastyDiscardPile = targetList; break;
                case Locations.RemovedFromGame: removedFromGame = targetList; break;
                case Locations.PlayArea: cardsInPlay = targetList; break;
                case Locations.ProvinceOne: provinceOne = targetList; break;
                case Locations.ProvinceTwo: provinceTwo = targetList; break;
                case Locations.ProvinceThree: provinceThree = targetList; break;
                case Locations.ProvinceFour: provinceFour = targetList; break;
                case Locations.StrongholdProvince: strongholdProvince = targetList; break;
                case Locations.ProvinceDeck: provinceDeck = targetList; break;
                case Locations.UnderneathStronghold: underneathStronghold = targetList; break;
                default:
                    if (additionalPiles.ContainsKey(source))
                        additionalPiles[source].cards = targetList;
                    break;
            }
        }

        // Simplified card movement and UI methods
        public void Drop(string cardId, string source, string target)
        {
            var sourceList = GetSourceList(source);
            var card = FindCardByUuid(sourceList, cardId);

            if (card != null && IsLegalLocationForCard(card, target))
            {
                game.AddMessage("{0} manually moves {1} from their {2} to their {3}", 
                               this, card.name, source, target);
                MoveCard(card, target);
            }
        }

        public bool IsLegalLocationForCard(BaseCard card, string location)
        {
            return card != null && !string.IsNullOrEmpty(location);
        }

        public void PromptForAttachment(BaseCard card, string playingType) { }

        // Combat placeholders
        public bool IsAttackingPlayer() { return game.currentConflict != null; }
        public bool IsDefendingPlayer() { return game.currentConflict != null; }
        public bool IsLessHonorableThanOpponent() { return honor < (opponent?.honor ?? -1); }
        public void ResetForConflict() { }

        // Properties
        public int HonorBid => Mathf.Max(0, showBid + honorBidModifier);
        public int GloryModifier => 0;
        public int SkillModifier => 0;

        // Resource management
        public void ModifyFate(int amount) { fate = Mathf.Max(0, fate + amount); }
        public void ModifyHonor(int amount) { honor = Mathf.Max(0, honor + amount); }

        public List<Ring> GetClaimedRings() { return new List<Ring>(); }
        public int GetGloryCount() { return GetClaimedRings().Count + GloryModifier; }

        // Imperial Favor placeholders
        public void ClaimImperialFavor()
        {
            if (opponent != null)
                opponent.LoseImperialFavor();
                
            // Simplified implementation - just claim military for now
            imperialFavor = "military";
            game.AddMessage("{0} claims the Emperor's military favor!", this);
        }

        public void LoseImperialFavor() { imperialFavor = ""; }

        // Card movement - simplified
        public void MoveCard(BaseCard card, string targetLocation, CardMoveOptions options = null)
        {
            RemoveCardFromPile(card);
            var targetPile = GetSourceList(targetLocation);
            targetPile?.Add(card);
        }

        public void RemoveCardFromPile(BaseCard card)
        {
            hand.Remove(card);
            conflictDeck.Remove(card);
            dynastyDeck.Remove(card);
            conflictDiscardPile.Remove(card);
            dynastyDiscardPile.Remove(card);
            removedFromGame.Remove(card);
            cardsInPlay.Remove(card);
            provinceOne.Remove(card);
            provinceTwo.Remove(card);
            provinceThree.Remove(card);
            provinceFour.Remove(card);
            strongholdProvince.Remove(card);
            provinceDeck.Remove(card);
            underneathStronghold.Remove(card);
        }

        // Resources and UI
        public int GetTotalIncome() { return 7; } // Default starting fate
        public int GetTotalHonor() { return honor; }

        // Selection placeholders
        public void SetSelectedCards(List<BaseCard> cards) { }
        public void ClearSelectedCards() { }
        public void SetSelectableCards(List<BaseCard> cards) { }
        public void ClearSelectableCards() { }
        public void SetSelectableRings(List<Ring> rings) { }
        public void ClearSelectableRings() { }

        public List<object> GetSummaryForCardList(List<BaseCard> list, Player activePlayer, bool hideWhenFaceup = false)
        {
            return list.Cast<object>().ToList();
        }

        public string GetCardSelectionState(BaseCard card) { return "unselectable"; }
        public string GetRingSelectionState(Ring ring) { return "unselectable"; }

        public object CurrentPrompt() { return promptState; }
        public void SetPrompt(object prompt) { promptState = prompt; }
        public void CancelPrompt() { promptState = null; }

        // Phase actions
        public void PassDynasty() { passedDynasty = true; }
        public void SetShowBid(int bid)
{
    showBid = bid;
    game.AddMessage("{0} reveals a bid of {1}", this, bid);
}

        // Effect checking - placeholders
        public bool IsTopConflictCardShown() { return AnyEffect(EffectNames.ShowTopConflictCard); }
        public bool EventsCannotBeCancelled() { return AnyEffect(EffectNames.EventsCannotBeCancelled); }
        public bool IsTopDynastyCardShown() { return AnyEffect(EffectNames.ShowTopDynastyCard); }

        // Ring effects - placeholder
        public void ResolveRingEffects(object elements, bool optional = true) { }

        // Statistics
        public PlayerStats GetStats()
        {
            return new PlayerStats
            {
                fate = fate,
                honor = GetTotalHonor(),
                conflictsRemaining = GetConflictOpportunities(),
                militaryRemaining = GetConflictOpportunities("military"),
                politicalRemaining = GetConflictOpportunities("political")
            };
        }

        // State for UI - simplified
        public PlayerState GetState(Player activePlayer)
        {
            bool isActivePlayer = activePlayer == this;
            var promptStateData = isActivePlayer ? promptState : new object();

            var state = new PlayerState
            {
                cardPiles = new CardPiles
                {
                    cardsInPlay = GetSummaryForCardList(cardsInPlay, activePlayer),
                    conflictDiscardPile = GetSummaryForCardList(conflictDiscardPile, activePlayer),
                    dynastyDiscardPile = GetSummaryForCardList(dynastyDiscardPile, activePlayer),
                    hand = GetSummaryForCardList(hand, activePlayer, true),
                    removedFromGame = GetSummaryForCardList(removedFromGame, activePlayer),
                    provinceDeck = GetSummaryForCardList(provinceDeck, activePlayer, true)
                },
                disconnected = disconnected,
                faction = faction,
                firstPlayer = firstPlayer,
                hideProvinceDeck = hideProvinceDeck,
                id = id,
                imperialFavor = imperialFavor,
                left = left,
                name = name,
                numConflictCards = conflictDeck.Count,
                numDynastyCards = dynastyDeck.Count,
                numProvinceCards = provinceDeck.Count,
                optionSettings = settings.optionSettings,
                phase = game?.currentPhase ?? "",
                promptedActionWindows = settings.promptedActionWindows,
                provinces = new Provinces
                {
                    one = GetSummaryForCardList(provinceOne, activePlayer, !readyToStart),
                    two = GetSummaryForCardList(provinceTwo, activePlayer, !readyToStart),
                    three = GetSummaryForCardList(provinceThree, activePlayer, !readyToStart),
                    four = GetSummaryForCardList(provinceFour, activePlayer, !readyToStart)
                },
                showBid = showBid,
                stats = GetStats(),
                timerSettings = settings.timerSettings,
                strongholdProvince = GetSummaryForCardList(strongholdProvince, activePlayer),
                user = user
            };

            if (showConflict)
            {
                state.showConflictDeck = true;
                state.cardPiles.conflictDeck = GetSummaryForCardList(conflictDeck, activePlayer);
            }

            if (showDynasty)
            {
                state.showDynastyDeck = true;
                state.cardPiles.dynastyDeck = GetSummaryForCardList(dynastyDeck, activePlayer);
            }

            if (role != null) state.role = role;
            if (stronghold != null) state.stronghold = stronghold;

            if (IsTopConflictCardShown() && conflictDeck.Count > 0)
                state.conflictDeckTopCard = conflictDeck.First();

            if (IsTopDynastyCardShown() && dynastyDeck.Count > 0)
                state.dynastyDeckTopCard = dynastyDeck.First();

            if (clock != null) state.clock = new object();

            state.promptState = promptStateData;
            return state;
        }

        // IronPython Integration - placeholders
        public void ExecuteCardScript(BaseCard card, string eventType, params object[] parameters)
        {
            if (game.enablePythonScripting)
            {
                var allParams = new List<object> { card, this }.Concat(parameters).ToArray();
                // Placeholder - would need game.ExecuteCardScript implementation
            }
        }

        public void OnCardPlayed(BaseCard card)
        {
            ExecuteCardScript(card, "on_card_played", new Dictionary<string, object>());
        }

        public void OnCardEnterPlay(BaseCard card)
        {
            ExecuteCardScript(card, "on_enter_play");
        }

        public void OnCardLeavePlay(BaseCard card)
        {
            ExecuteCardScript(card, "on_leave_play");
        }

        public void OnConflictDeclared(BaseCard card, Conflict conflict)
        {
            ExecuteCardScript(card, "on_conflict", conflict);
        }

        // Deck selection - placeholder
        public void SelectDeck(Deck selectedDeck)
        {
            deck = selectedDeck;
            // Placeholder - Deck doesn't have faction property
            // faction = selectedDeck.faction;
        }
        
        public Dictionary<string, object> GetShortSummary()
        {
            return new Dictionary<string, object>
            {
                {"name", name},
                {"id", id},
                {"fate", fate},
                {"honor", honor}
            };
        }
        
        public Dictionary<string, object> GetCardSelectionState(BaseCard card)
        {
            return new Dictionary<string, object>
            {
                {"selectable", false}
            };
        }
    }

    // Supporting classes
    [System.Serializable]
    public class ConflictProperties
    {
        public List<string> type;
        public object ring;
        public object province;
        public BaseCard attacker;
        public string forcedDeclaredType;
    }

    [System.Serializable]
    public class CardMoveOptions
    {
        public bool bottom = false;
        public bool facedown = false;
    }

    [System.Serializable]
    public class AdditionalPile
    {
        public List<BaseCard> cards = new List<BaseCard>();
        public AdditionalPileProperties properties;
    }

    [System.Serializable]
    public class AdditionalPileProperties
    {
        public string name;
        public bool isPrivate = true;
    }

    [System.Serializable]
    public class PlayerStats
    {
        public int fate;
        public int honor;
        public int conflictsRemaining;
        public int militaryRemaining;
        public int politicalRemaining;
    }

    [System.Serializable]
    public class PlayerState
    {
        public CardPiles cardPiles;
        public bool disconnected;
        public Faction faction;
        public bool firstPlayer;
        public bool hideProvinceDeck;
        public string id;
        public string imperialFavor;
        public bool left;
        public string name;
        public int numConflictCards;
        public int numDynastyCards;
        public int numProvinceCards;
        public Dictionary<string, object> optionSettings;
        public string phase;
        public Dictionary<string, bool> promptedActionWindows;
        public Provinces provinces;
        public int showBid;
        public PlayerStats stats;
        public Dictionary<string, object> timerSettings;
        public List<object> strongholdProvince;
        public UserInfo user;
        public bool showConflictDeck = false;
        public bool showDynastyDeck = false;
        public List<object> conflictDeck;
        public List<object> dynastyDeck;
        public object role;
        public object stronghold;
        public object conflictDeckTopCard;
        public object dynastyDeckTopCard;
        public object clock;
        public object promptState;
    }

    [System.Serializable]
    public class CardPiles
    {
        public List<object> cardsInPlay;
        public List<object> conflictDiscardPile;
        public List<object> dynastyDiscardPile;
        public List<object> hand;
        public List<object> removedFromGame;
        public List<object> provinceDeck;
        public List<object> conflictDeck;
        public List<object> dynastyDeck;
    }

    [System.Serializable]
    public class Provinces
    {
        public List<object> one;
        public List<object> two;
        public List<object> three;
        public List<object> four;
    }

    // Interfaces for cost system
    public interface IFateSource
    {
        int fate { get; }
    }

    public interface ITargetCostEffect
    {
        string cardType { get; }
        string targetPlayer { get; }
        int amount { get; }
    }

    // Fake choice window for cost calculation
    public class FakeChoiceWindow
    {
        private System.Action addChoiceAction;
        
        public FakeChoiceWindow(System.Action addChoice)
        {
            addChoiceAction = addChoice;
        }
        
        public void AddChoice()
        {
            addChoiceAction?.Invoke();
        }
    }

    // Note: PlayTypes, ConflictTypes, Players, and Decks are in GameConstants.cs
}
