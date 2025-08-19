using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    [System.Serializable]
    public class ConflictOpportunities
    {
        public int military = 1;
        public int political = 1;
        public int total = 2;
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

    public class Player : GameObject
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
        public StrongholdCard stronghold;
        public RoleCard role;

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

        // References
        public Player opponent;
        public Deck deck;
        public Game game;
        public ClockManager clock;
        public PreparedDeck preparedDeck;

        // Systems
        private List<CostReducer> costReducers = new List<CostReducer>();
        private List<PlayableLocation> playableLocations = new List<PlayableLocation>();
        private Dictionary<string, AbilityLimit> abilityMaxByIdentifier = new Dictionary<string, AbilityLimit>();
        private PlayerSettings settings = new PlayerSettings();
        private PlayerPromptState promptState;

        // Static location arrays for easy reference
        private static readonly string[] ProvinceLocations = {
            Locations.StrongholdProvince,
            Locations.ProvinceOne,
            Locations.ProvinceTwo,
            Locations.ProvinceThree,
            Locations.ProvinceFour
        };

        public void Initialize(string playerId, UserInfo userInfo, bool isOwner, Game gameInstance, ClockSettings clockSettings)
        {
            // Base initialization
            base.Initialize(gameInstance, userInfo.username);
            
            id = playerId;
            user = userInfo;
            emailHash = userInfo.emailHash;
            owner = isOwner;
            game = gameInstance;
            
            // Initialize clock
            clock = gameObject.AddComponent<ClockManager>();
            clock.Initialize(this, clockSettings);
            
            // Initialize prompt state
            promptState = new PlayerPromptState(this);
            
            // Set up initial playable locations
            InitializePlayableLocations();
            
            Debug.Log($"🎮 Player {userInfo.username} initialized");
        }

        private void InitializePlayableLocations()
        {
            playableLocations = new List<PlayableLocation>
            {
                new PlayableLocation(PlayTypes.PlayFromHand, this, Locations.Hand),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceOne),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceTwo),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceThree),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceFour)
            };
        }

        // Clock management
        public void StartClock()
        {
            clock.Start();
            if (opponent != null)
            {
                opponent.clock.OpponentStart();
            }
        }

        public void StopClock()
        {
            clock.Stop();
        }

        public void ResetClock()
        {
            clock.Reset();
        }

        // Card searching and validation methods
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
            return cardsInPlay.Any(card => card.selected);
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

            var cardsToReturn = new List<BaseCard>();

            foreach (var card in cardList)
            {
                if (predicate(card))
                {
                    cardsToReturn.Add(card);
                }

                // Check attachments
                if (card.attachments != null)
                {
                    cardsToReturn.AddRange(card.attachments.Where(predicate));
                }
            }

            return cardsToReturn;
        }

        public bool AreLocationsAdjacent(string location1, string location2)
        {
            int index1 = Array.IndexOf(ProvinceLocations, location1);
            int index2 = Array.IndexOf(ProvinceLocations, location2);
            return index1 > -1 && index2 > -1 && Mathf.Abs(index1 - index2) == 1;
        }

        // Province management
        public BaseCard GetDynastyCardInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.FirstOrDefault(card => card.isDynasty);
        }

        public List<BaseCard> GetDynastyCardsInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.Where(card => card.isDynasty).ToList();
        }

        public BaseCard GetProvinceCardInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.FirstOrDefault(card => card.isProvince);
        }

        public bool AnyCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return game.allCards.Any(card => 
                card.controller == this && 
                card.location == Locations.PlayArea && 
                predicate(card));
        }

        public List<BaseCard> FilterCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return game.allCards.Where(card => 
                card.controller == this && 
                card.location == Locations.PlayArea && 
                predicate(card)).ToList();
        }

        // Game state properties
        public bool HasComposure()
        {
            return opponent != null && opponent.showBid > showBid;
        }

        public List<string> GetLegalConflictTypes(ConflictProperties properties)
        {
            var types = properties.type ?? new List<string> { ConflictTypes.Military, ConflictTypes.Political };
            if (!types.GetType().IsArray && !(types is List<string>))
                types = new List<string> { types.ToString() };

            var forcedDeclaredType = properties.forcedDeclaredType ?? 
                                   (game.currentConflict?.forcedDeclaredType);

            if (!string.IsNullOrEmpty(forcedDeclaredType))
            {
                return new List<string> { forcedDeclaredType }.Where(type =>
                    types.Contains(type) &&
                    GetConflictOpportunities() > 0 &&
                    !GetEffects(EffectNames.CannotDeclareConflictsOfType).Contains(type)
                ).ToList();
            }

            return types.Where(type =>
                GetConflictOpportunities(type) > 0 &&
                !GetEffects(EffectNames.CannotDeclareConflictsOfType).Contains(type)
            ).ToList();
        }

        public bool HasLegalConflictDeclaration(ConflictProperties properties)
        {
            var conflictType = GetLegalConflictTypes(properties);
            if (conflictType.Count == 0)
                return false;

            var conflictRing = properties.ring ?? game.rings.Values.ToList();
            if (!(conflictRing is List<Ring>))
                conflictRing = new List<Ring> { (Ring)conflictRing };

            var validRings = ((List<Ring>)conflictRing).Where(ring => ring.CanDeclare(this)).ToList();
            if (validRings.Count == 0)
                return false;

            var cards = properties.attacker != null 
                ? new List<BaseCard> { properties.attacker } 
                : cardsInPlay.ToList();

            if (opponent == null)
            {
                return conflictType.Any(type => 
                    validRings.Any(ring => 
                        cards.Any(card => card.CanDeclareAsAttacker(type, ring))));
            }

            var conflictProvince = properties.province ?? opponent.GetProvinces();
            if (!(conflictProvince is List<BaseCard>))
                conflictProvince = new List<BaseCard> { (BaseCard)conflictProvince };

            return conflictType.Any(type => 
                validRings.Any(ring => 
                    ((List<BaseCard>)conflictProvince).Any(province =>
                        province.CanDeclare(type, ring) &&
                        cards.Any(card => card.CanDeclareAsAttacker(type, ring, province)))));
        }

        public List<BaseCard> GetProvinces(System.Func<BaseCard, bool> predicate = null)
        {
            predicate = predicate ?? (card => true);
            var provinces = new List<BaseCard>();

            foreach (var location in ProvinceLocations)
            {
                provinces.AddRange(GetSourceList(location)
                    .Where(card => card.type == CardTypes.Province && predicate(card)));
            }

            return provinces;
        }

        public int GetNumberOfFaceupProvinces(System.Func<BaseCard, bool> predicate = null)
        {
            predicate = predicate ?? (card => true);
            return GetProvinces(card => !card.facedown && predicate(card)).Count;
        }

        public int GetNumberOfOpponentsFaceupProvinces(System.Func<BaseCard, bool> predicate = null)
        {
            return opponent?.GetNumberOfFaceupProvinces(predicate) ?? 0;
        }

        public int GetNumberOfCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return game.allCards.Count(card =>
                card.controller == this &&
                card.location == Locations.PlayArea &&
                predicate(card));
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
                holdings.AddRange(GetSourceList(province)
                    .Where(card => card.GetCardType() == CardTypes.Holding && !card.facedown));
            }
            return holdings;
        }

        public bool IsCardInPlayableLocation(BaseCard card, string playingType = null)
        {
            return playableLocations.Any(location =>
                (string.IsNullOrEmpty(playingType) || location.playingType == playingType) &&
                location.Contains(card));
        }

        public BaseCard GetDuplicateInPlay(BaseCard card)
        {
            if (!card.IsUnique())
                return null;

            return FindCard(cardsInPlay, playCard =>
                playCard != card && (playCard.id == card.id || playCard.name == card.name));
        }

        // Card drawing and deck management
        public void DrawCardsToHand(int numCards)
        {
            int remainingCards = 0;

            if (numCards > conflictDeck.Count)
            {
                remainingCards = numCards - conflictDeck.Count;
                var cards = conflictDeck.ToList();
                DeckRanOutOfCards("conflict");
                
                game.QueueSimpleStep(() => {
                    foreach (var card in cards)
                    {
                        MoveCard(card, Locations.Hand);
                    }
                    return true;
                });
                
                game.QueueSimpleStep(() => {
                    DrawCardsToHand(remainingCards);
                    return true;
                });
            }
            else
            {
                var cardsToDraw = conflictDeck.Take(numCards).ToList();
                foreach (var card in cardsToDraw)
                {
                    MoveCard(card, Locations.Hand);
                }
            }
        }

        public void DeckRanOutOfCards(string deckType)
        {
            var discardPile = GetSourceList(deckType + " discard pile");
            game.AddMessage("{0}'s {1} deck has run out of cards, so they lose 5 honor", this, deckType);
            
            // Use game actions to lose honor
            var loseHonorAction = game.Actions.LoseHonor(5);
            loseHonorAction.Resolve(this, game.GetFrameworkContext());
            
            game.QueueSimpleStep(() => {
                foreach (var card in discardPile.ToList())
                {
                    MoveCard(card, deckType + " deck");
                }
                
                if (deckType == "dynasty")
                {
                    ShuffleDynastyDeck();
                }
                else
                {
                    ShuffleConflictDeck();
                }
                return true;
            });
        }

        public bool ReplaceDynastyCard(string location)
        {
            if (GetSourceList(location).Count > 1)
                return false;

            if (dynastyDeck.Count == 0)
            {
                DeckRanOutOfCards("dynasty");
                game.QueueSimpleStep(() => {
                    ReplaceDynastyCard(location);
                    return true;
                });
            }
            else
            {
                MoveCard(dynastyDeck.First(), location);
            }
            return true;
        }

        public void ShuffleConflictDeck()
        {
            if (name != "Dummy Player")
            {
                game.AddMessage("{0} is shuffling their conflict deck", this);
            }
            
            game.EmitEvent(EventNames.OnDeckShuffled, new Dictionary<string, object>
            {
                {"player", this},
                {"deck", Decks.ConflictDeck}
            });
            
            // Shuffle the deck
            conflictDeck = conflictDeck.OrderBy(x => UnityEngine.Random.value).ToList();
        }

        public void ShuffleDynastyDeck()
        {
            if (name != "Dummy Player")
            {
                game.AddMessage("{0} is shuffling their dynasty deck", this);
            }
            
            game.EmitEvent(EventNames.OnDeckShuffled, new Dictionary<string, object>
            {
                {"player", this},
                {"deck", Decks.DynastyDeck}
            });
            
            // Shuffle the deck
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
            var setConflictDeclarationType = MostRecentEffect(EffectNames.SetConflictDeclarationType);
            var maxConflicts = MostRecentEffect(EffectNames.SetMaxConflicts);

            if (!string.IsNullOrEmpty(setConflictDeclarationType) && type != "total")
            {
                if (type != setConflictDeclarationType)
                {
                    return 0;
                }
                else if (maxConflicts != null)
                {
                    return Mathf.Max(0, (int)maxConflicts - game.GetConflicts(this).Count);
                }
                return conflictOpportunities.total;
            }

            if (maxConflicts != null)
            {
                return Mathf.Max(0, (int)maxConflicts - game.GetConflicts(this).Count);
            }

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

        // Deck preparation and initialization
        public void PrepareDecks()
        {
            var deckBuilder = new Deck(deck);
            preparedDeck = deckBuilder.Prepare(this);
            
            faction = preparedDeck.faction;
            provinceDeck = preparedDeck.provinceCards;
            
            if (preparedDeck.stronghold is StrongholdCard)
            {
                stronghold = preparedDeck.stronghold as StrongholdCard;
            }
            
            if (preparedDeck.role is RoleCard)
            {
                role = preparedDeck.role as RoleCard;
            }
            
            conflictDeck = preparedDeck.conflictCards;
            dynastyDeck = preparedDeck.dynastyCards;

            // Register event reactions for events in deck (for bluff windows)
            foreach (var card in conflictDeck.Where(c => c.type == CardTypes.Event))
            {
                foreach (var reaction in card.abilities.reactions)
                {
                    reaction.RegisterEvents();
                }
            }
        }

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

        // Cost reduction system
        public CostReducer AddCostReducer(EffectSource source, CostReducerProperties properties)
        {
            var reducer = new CostReducer(game, source, properties);
            costReducers.Add(reducer);
            return reducer;
        }

        public void RemoveCostReducer(CostReducer reducer)
        {
            if (costReducers.Contains(reducer))
            {
                reducer.UnregisterEvents();
                costReducers.Remove(reducer);
            }
        }

        public PlayableLocation AddPlayableLocation(string type, Player player, string location, List<BaseCard> cards = null)
        {
            if (player == null) return null;
            
            var playableLocation = new PlayableLocation(type, player, location, cards ?? new List<BaseCard>());
            playableLocations.Add(playableLocation);
            return playableLocation;
        }

        public void RemovePlayableLocation(PlayableLocation location)
        {
            playableLocations.Remove(location);
        }

        public List<object> GetAlternateFatePools(string playingType, BaseCard card, AbilityContext context)
        {
            var effects = GetEffects(EffectNames.AlternateFatePool);
            var alternateFatePools = new List<object>();
            
            foreach (var effect in effects)
            {
                var match = effect as System.Func<BaseCard, object>;
                var result = match?.Invoke(card);
                if (result != null)
                {
                    var fateSource = result as IFateSource;
                    if (fateSource?.fate > 0)
                    {
                        alternateFatePools.Add(result);
                    }
                }
            }

            var rings = alternateFatePools.OfType<Ring>().ToList();
            var cards = alternateFatePools.Where(a => !(a is Ring)).ToList();

            if (!CheckRestrictions("takeFateFromRings", context))
            {
                foreach (var ring in rings)
                {
                    alternateFatePools.Remove(ring);
                }
            }

            foreach (var cardSource in cards.OfType<BaseCard>())
            {
                if (!cardSource.AllowGameAction("removeFate"))
                {
                    alternateFatePools.Remove(cardSource);
                }
            }

            return alternateFatePools.Distinct().ToList();
        }

        public int GetMinimumCost(string playingType, AbilityContext context, BaseCard target, bool ignoreType = false)
        {
            var card = context.source;
            int reducedCost = GetReducedCost(playingType, card, target, ignoreType);
            var alternateFatePools = GetAlternateFatePools(playingType, card, context);
            int alternateFate = alternateFatePools.OfType<IFateSource>().Sum(pool => pool.fate);
            
            int triggeredCostReducers = 0;
            var fakeWindow = new FakeChoiceWindow(() => triggeredCostReducers++);
            var fakeEvent = game.GetEvent(EventNames.OnCardPlayed, new Dictionary<string, object>
            {
                {"card", card},
                {"player", this},
                {"context", context}
            }, () => true);
            
            game.EmitEvent(EventNames.OnCardPlayed + ":" + AbilityTypes.Interrupt, new Dictionary<string, object>
            {
                {"event", fakeEvent},
                {"window", fakeWindow}
            });
            
            return Mathf.Max(reducedCost - triggeredCostReducers - alternateFate, 0);
        }

        public int GetReducedCost(string playingType, BaseCard card, BaseCard target, bool ignoreType = false)
        {
            int baseCost = card.GetCost();
            var matchingReducers = costReducers.Where(reducer => 
                reducer.CanReduce(playingType, card, target, ignoreType)).ToList();
            
            int reducedCost = matchingReducers.Aggregate(baseCost, 
                (cost, reducer) => cost - reducer.GetAmount(card, this));
            
            return Mathf.Max(reducedCost, 0);
        }

        public int GetAvailableAlternateFate(string playingType, AbilityContext context)
        {
            var card = context.source;
            var alternateFatePools = GetAlternateFatePools(playingType, card, context);
            int alternateFate = alternateFatePools.OfType<IFateSource>().Sum(pool => pool.fate);
            return Mathf.Max(alternateFate, 0);
        }

        public int GetTargetingCost(BaseCard abilitySource, object targets)
        {
            int targetCost = 0;
            
            if (targets != null)
            {
                List<BaseCard> targetList;
                if (targets is List<BaseCard>)
                {
                    targetList = (List<BaseCard>)targets;
                }
                else
                {
                    targetList = new List<BaseCard> { (BaseCard)targets };
                }

                targetList = targetList.Where(t => t != null).ToList();
                
                foreach (var target in targetList)
                {
                    var effects = target.GetEffects(EffectNames.FateCostToTarget);
                    foreach (var effect in effects)
                    {
                        var costEffect = effect as ITargetCostEffect;
                        if (costEffect != null)
                        {
                            bool typeMatch = string.IsNullOrEmpty(costEffect.cardType) || 
                                           abilitySource.type == costEffect.cardType;
                            bool controllerMatch = true;
                            
                            if (costEffect.targetPlayer == Players.Self && 
                                abilitySource.controller != target.controller)
                            {
                                controllerMatch = false;
                            }
                            else if (costEffect.targetPlayer == Players.Opponent && 
                                     abilitySource.controller != target.controller.opponent)
                            {
                                controllerMatch = false;
                            }

                            if (typeMatch && controllerMatch)
                            {
                                targetCost += costEffect.amount;
                            }
                        }
                    }
                }
            }

            return targetCost;
        }

        public void MarkUsedReducers(string playingType, BaseCard card, BaseCard target = null)
        {
            var matchingReducers = costReducers.Where(reducer => 
                reducer.CanReduce(playingType, card, target)).ToList();
            
            foreach (var reducer in matchingReducers)
            {
                reducer.MarkUsed();
                if (reducer.IsExpired())
                {
                    RemoveCostReducer(reducer);
                }
            }
        }

        // Ability limit management
        public void RegisterAbilityMax(string maxIdentifier, AbilityLimit limit)
        {
            if (abilityMaxByIdentifier.ContainsKey(maxIdentifier))
                return;

            abilityMaxByIdentifier[maxIdentifier] = limit;
            limit.RegisterEvents(game);
        }

        public bool IsAbilityAtMax(string maxIdentifier)
        {
            if (!abilityMaxByIdentifier.TryGetValue(maxIdentifier, out AbilityLimit limit))
                return false;

            return limit.IsAtMax(this);
        }

        public void IncrementAbilityMax(string maxIdentifier)
        {
            if (abilityMaxByIdentifier.TryGetValue(maxIdentifier, out AbilityLimit limit))
            {
                limit.Increment(this);
            }
        }

        // Phase management
        public void BeginDynasty()
        {
            if (resetTimerAtEndOfRound)
            {
                // Reset timer logic
            }

            foreach (var card in cardsInPlay)
            {
                card.isNew = false;
            }

            passedDynasty = false;
            limitedPlayed = 0;
            conflictOpportunities.military = 1;
            conflictOpportunities.political = 1;
            conflictOpportunities.total = 2;
        }

        public void CollectFate()
        {
            ModifyFate(GetTotalIncome());
            game.RaiseEvent(EventNames.OnFateCollected, new Dictionary<string, object>
            {
                {"player", this}
            });
        }

        public void ShowConflictDeck()
        {
            showConflict = true;
        }

        public void ShowDynastyDeck()
        {
            showDynasty = true;
        }

        // List management methods
        public List<BaseCard> GetSourceList(string source)
        {
            switch (source)
            {
                case Locations.Hand:
                    return hand;
                case Locations.ConflictDeck:
                    return conflictDeck;
                case Locations.DynastyDeck:
                    return dynastyDeck;
                case Locations.ConflictDiscardPile:
                    return conflictDiscardPile;
                case Locations.RemovedFromGame:
                    return removedFromGame;
                case Locations.PlayArea:
                    return cardsInPlay;
                case Locations.ProvinceOne:
                    return provinceOne;
                case Locations.ProvinceTwo:
                    return provinceTwo;
                case Locations.ProvinceThree:
                    return provinceThree;
                case Locations.ProvinceFour:
                    return provinceFour;
                case Locations.StrongholdProvince:
                    return strongholdProvince;
                case Locations.ProvinceDeck:
                    return provinceDeck;
                case Locations.Provinces:
                    var allProvinces = new List<BaseCard>();
                    allProvinces.AddRange(provinceOne);
                    allProvinces.AddRange(provinceTwo);
                    allProvinces.AddRange(provinceThree);
                    allProvinces.AddRange(provinceFour);
                    allProvinces.AddRange(strongholdProvince);
                    return allProvinces;
                case Locations.UnderneathStronghold:
                    return underneathStronghold;
                default:
                    if (additionalPiles.ContainsKey(source))
                    {
                        return additionalPiles[source].cards;
                    }
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
                case Locations.Hand:
                    hand = targetList;
                    break;
                case Locations.ConflictDeck:
                    conflictDeck = targetList;
                    break;
                case Locations.DynastyDeck:
                    dynastyDeck = targetList;
                    break;
                case Locations.ConflictDiscardPile:
                    conflictDiscardPile = targetList;
                    break;
                case Locations.DynastyDiscardPile:
                    dynastyDiscardPile = targetList;
                    break;
                case Locations.RemovedFromGame:
                    removedFromGame = targetList;
                    break;
                case Locations.PlayArea:
                    cardsInPlay = targetList;
                    break;
                case Locations.ProvinceOne:
                    provinceOne = targetList;
                    break;
                case Locations.ProvinceTwo:
                    provinceTwo = targetList;
                    break;
                case Locations.ProvinceThree:
                    provinceThree = targetList;
                    break;
                case Locations.ProvinceFour:
                    provinceFour = targetList;
                    break;
                case Locations.StrongholdProvince:
                    strongholdProvince = targetList;
                    break;
                case Locations.ProvinceDeck:
                    provinceDeck = targetList;
                    break;
                case Locations.UnderneathStronghold:
                    underneathStronghold = targetList;
                    break;
                default:
                    if (additionalPiles.ContainsKey(source))
                    {
                        additionalPiles[source].cards = targetList;
                    }
                    break;
            }
        }

        // Card movement and manipulation
        public void Drop(string cardId, string source, string target)
        {
            var sourceList = GetSourceList(source);
            var card = FindCardByUuid(sourceList, cardId);

            // Validate the drop operation
            if (!game.manualMode || source == target || 
                !IsLegalLocationForCard(card, target) || card.location != source)
            {
                return;
            }

            // Don't allow two province cards in one province
            if (card.isProvince && target != Locations.ProvinceDeck && 
                GetSourceList(target).Any(c => c.isProvince))
            {
                return;
            }

            string display = "a card";
            if (!card.facedown && source != Locations.Hand || 
                new[] { Locations.PlayArea, Locations.DynastyDiscardPile, 
                       Locations.ConflictDiscardPile, Locations.RemovedFromGame }.Contains(target))
            {
                display = card.name;
            }

            game.AddMessage("{0} manually moves {1} from their {2} to their {3}", 
                           this, display, source, target);
            MoveCard(card, target);
            game.CheckGameState(true);
        }

        public bool IsLegalLocationForCard(BaseCard card, string location)
        {
            if (card == null) return false;

            var conflictCardLocations = new[]
            {
                Locations.Hand, Locations.ConflictDeck, Locations.ConflictDiscardPile, 
                Locations.RemovedFromGame
            };

            var dynastyCardLocations = ProvinceLocations.Concat(new[]
            {
                Locations.DynastyDeck, Locations.DynastyDiscardPile, 
                Locations.RemovedFromGame, Locations.UnderneathStronghold
            }).ToArray();

            var legalLocations = new Dictionary<string, string[]>
            {
                {"stronghold", new[] { Locations.StrongholdProvince }},
                {"role", new[] { Locations.Role }},
                {"province", ProvinceLocations.Concat(new[] { Locations.ProvinceDeck }).ToArray()},
                {"holding", dynastyCardLocations},
                {"conflictCharacter", conflictCardLocations.Concat(new[] { Locations.PlayArea }).ToArray()},
                {"dynastyCharacter", dynastyCardLocations.Concat(new[] { Locations.PlayArea }).ToArray()},
                {"event", conflictCardLocations.Concat(new[] { Locations.BeingPlayed }).ToArray()},
                {"attachment", conflictCardLocations.Concat(new[] { Locations.PlayArea }).ToArray()}
            };

            string type = card.type;
            if (location == Locations.DynastyDiscardPile || location == Locations.ConflictDiscardPile)
            {
                type = card.printedType ?? card.type;
            }

            if (type == "character")
            {
                type = card.isDynasty ? "dynastyCharacter" : "conflictCharacter";
            }

            return legalLocations.ContainsKey(type) && legalLocations[type].Contains(location);
        }

        public void PromptForAttachment(BaseCard card, string playingType)
        {
            game.QueueStep(new AttachmentPrompt(game, this, card, playingType));
        }

        // Combat and conflict methods
        public bool IsAttackingPlayer()
        {
            return game.currentConflict != null && game.currentConflict.attackingPlayer == this;
        }

        public bool IsDefendingPlayer()
        {
            return game.currentConflict != null && game.currentConflict.defendingPlayer == this;
        }

        public bool IsLessHonorableThanOpponent()
        {
            return honor < (opponent?.honor ?? -1);
        }

        public void ResetForConflict()
        {
            foreach (var card in cardsInPlay)
            {
                card.ResetForConflict();
            }
        }

        // Properties
        public int HonorBid => Mathf.Max(0, showBid + honorBidModifier);

        public int GloryModifier => GetEffects(EffectNames.ChangePlayerGloryModifier)
            .OfType<int>().Sum();

        public int SkillModifier => GetEffects(EffectNames.ChangePlayerSkillModifier)
            .OfType<int>().Sum();

        // Resource management
        public void ModifyFate(int amount)
        {
            fate = Mathf.Max(0, fate + amount);
        }

        public void ModifyHonor(int amount)
        {
            honor = Mathf.Max(0, honor + amount);
        }

        public List<Ring> GetClaimedRings()
        {
            return game.rings.Values.Where(ring => ring.IsConsideredClaimed(this)).ToList();
        }

        public int GetGloryCount()
        {
            int cardGlory = cardsInPlay.Sum(card => card.GetContributionToImperialFavor());
            return cardGlory + GetClaimedRings().Count + GloryModifier;
        }

        // Imperial Favor management
        public void ClaimImperialFavor()
        {
            if (opponent != null)
            {
                opponent.LoseImperialFavor();
            }

            var handlers = new List<System.Action>
            {
                () => {
                    imperialFavor = "military";
                    game.AddMessage("{0} claims the Emperor's military favor!", this);
                },
                () => {
                    imperialFavor = "political";
                    game.AddMessage("{0} claims the Emperor's political favor!", this);
                }
            };

            game.PromptWithHandlerMenu(this, new HandlerMenuPromptProperties
            {
                activePromptTitle = "Which side of the Imperial Favor would you like to claim?",
                source = "Imperial Favor",
                choices = new List<string> { "Military", "Political" },
                handlers = handlers
            });
        }

        public void LoseImperialFavor()
        {
            imperialFavor = "";
        }

        // Deck selection
        public void SelectDeck(Deck selectedDeck)
        {
            if (deck != null)
            {
                deck.selected = false;
            }
            
            deck = selectedDeck;
            deck.selected = true;
            
            if (selectedDeck.stronghold.Count > 0)
            {
                stronghold = new StrongholdCard(this, selectedDeck.stronghold[0]);
            }
            
            faction = selectedDeck.faction;
        }

        // Card movement
        public void MoveCard(BaseCard card, string targetLocation, CardMoveOptions options = null)
        {
            options = options ?? new CardMoveOptions();
            
            RemoveCardFromPile(card);

            if (targetLocation.EndsWith(" bottom"))
            {
                options.bottom = true;
                targetLocation = targetLocation.Replace(" bottom", "");
            }

            var targetPile = GetSourceList(targetLocation);

            if (!IsLegalLocationForCard(card, targetLocation) || 
                (targetPile != null && targetPile.Contains(card)))
            {
                return;
            }

            string location = card.location;

            if (location == Locations.PlayArea || 
                (card.type == CardTypes.Holding && card.IsInProvince() && 
                 !ProvinceLocations.Contains(targetLocation)))
            {
                if (card.owner != this)
                {
                    card.owner.MoveCard(card, targetLocation, options);
                    return;
                }

                // Remove attachments in manual play
                foreach (var attachment in card.attachments.ToList())
                {
                    attachment.LeavesPlay();
                    attachment.owner.MoveCard(attachment, 
                        attachment.isDynasty ? Locations.DynastyDiscardPile : Locations.ConflictDiscardPile);
                }

                card.LeavesPlay();
                card.controller = this;
            }
            else if (targetLocation == Locations.PlayArea)
            {
                card.SetDefaultController(this);
                card.controller = this;
                
                if (card.type == CardTypes.Attachment)
                {
                    PromptForAttachment(card, "");
                    return;
                }
            }
            else if (location == Locations.BeingPlayed && card.owner != this)
            {
                card.owner.MoveCard(card, targetLocation, options);
                return;
            }
            else if (card.type == CardTypes.Holding && ProvinceLocations.Contains(targetLocation))
            {
                card.controller = this;
            }
            else
            {
                card.controller = card.owner;
            }

            // Handle different target locations
            if (ProvinceLocations.Contains(targetLocation))
            {
                if (new[] { Locations.DynastyDeck }.Contains(location))
                {
                    card.facedown = true;
                }
                if (!takenDynastyMulligan && card.isDynasty)
                {
                    card.facedown = false;
                }
                targetPile.Add(card);
            }
            else if (new[] { Locations.ConflictDeck, Locations.DynastyDeck }.Contains(targetLocation) && !options.bottom)
            {
                targetPile.Insert(0, card);
            }
            else if (new[] { Locations.ConflictDiscardPile, Locations.DynastyDiscardPile, Locations.RemovedFromGame }.Contains(targetLocation))
            {
                targetPile.Insert(0, card);
            }
            else if (targetPile != null)
            {
                targetPile.Add(card);
            }

            card.MoveTo(targetLocation);
        }

        public void RemoveCardFromPile(BaseCard card)
        {
            if (card.controller != this)
            {
                card.controller.RemoveCardFromPile(card);
                return;
            }

            var originalLocation = card.location;
            var originalPile = GetSourceList(originalLocation);

            if (originalPile != null)
            {
                originalPile.Remove(card);
            }
        }

        // Income and resources
        public int GetTotalIncome()
        {
            return stronghold?.cardData?.fate ?? 0;
        }

        public int GetTotalHonor()
        {
            return honor;
        }

        // Selection and prompt state
        public void SetSelectedCards(List<BaseCard> cards)
        {
            promptState.SetSelectedCards(cards);
        }

        public void ClearSelectedCards()
        {
            promptState.ClearSelectedCards();
        }

        public void SetSelectableCards(List<BaseCard> cards)
        {
            promptState.SetSelectableCards(cards);
        }

        public void ClearSelectableCards()
        {
            promptState.ClearSelectableCards();
        }

        public void SetSelectableRings(List<Ring> rings)
        {
            promptState.SetSelectableRings(rings);
        }

        public void ClearSelectableRings()
        {
            promptState.ClearSelectableRings();
        }

        public List<object> GetSummaryForCardList(List<BaseCard> list, Player activePlayer, bool hideWhenFaceup = false)
        {
            return list.Select(card => card.GetSummary(activePlayer, hideWhenFaceup)).Cast<object>().ToList();
        }

        public string GetCardSelectionState(BaseCard card)
        {
            return promptState.GetCardSelectionState(card);
        }

        public string GetRingSelectionState(Ring ring)
        {
            return promptState.GetRingSelectionState(ring);
        }

        public object CurrentPrompt()
        {
            return promptState.GetState();
        }

        public void SetPrompt(object prompt)
        {
            promptState.SetPrompt(prompt);
        }

        public void CancelPrompt()
        {
            promptState.CancelPrompt();
        }

        // Phase actions
        public void PassDynasty()
        {
            passedDynasty = true;
        }

        public void SetShowBid(int bid)
        {
            showBid = bid;
            game.AddMessage("{0} reveals a bid of {1}", this, bid);
        }

        // Effect checking
        public bool IsTopConflictCardShown()
        {
            return AnyEffect(EffectNames.ShowTopConflictCard);
        }

        public bool EventsCannotBeCancelled()
        {
            return AnyEffect(EffectNames.EventsCannotBeCancelled);
        }

        public bool IsTopDynastyCardShown()
        {
            return AnyEffect(EffectNames.ShowTopDynastyCard);
        }

        // Ring effects
        public void ResolveRingEffects(object elements, bool optional = true)
        {
            List<string> elementList;
            if (elements is string)
            {
                elementList = new List<string> { (string)elements };
            }
            else
            {
                elementList = (List<string>)elements;
            }

            optional = optional && elementList.Count == 1;
            
            var effects = elementList.Select(element => 
                RingEffects.ContextFor(this, element, optional)).ToList();
            
            effects = effects.OrderBy(context => 
                firstPlayer ? context.ability.defaultPriority : -context.ability.defaultPriority).ToList();
            
            var choices = effects.Select(context => new EffectChoice
            {
                title = context.ability.title,
                handler = () => game.ResolveAbility(context)
            }).ToList();
            
            game.OpenSimultaneousEffectWindow(choices);
        }

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

        // State for UI
        public PlayerState GetState(Player activePlayer)
        {
            bool isActivePlayer = activePlayer == this;
            var promptStateData = isActivePlayer ? promptState.GetState() : new object();
            
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
                phase = game.currentPhase,
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
                user = user // Note: Should omit sensitive data in real implementation
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

            if (role != null)
            {
                state.role = role.GetSummary(activePlayer);
            }

            if (stronghold != null)
            {
                state.stronghold = stronghold.GetSummary(activePlayer);
            }

            if (IsTopConflictCardShown() && conflictDeck.Count > 0)
            {
                state.conflictDeckTopCard = conflictDeck.First().GetSummary(activePlayer);
            }

            if (IsTopDynastyCardShown() && dynastyDeck.Count > 0)
            {
                state.dynastyDeckTopCard = dynastyDeck.First().GetSummary(activePlayer);
            }

            if (clock != null)
            {
                state.clock = clock.GetState();
            }

            // Merge prompt state
            state.promptState = promptStateData;

            return state;
        }

        // IronPython Integration
        public void ExecuteCardScript(BaseCard card, string eventType, params object[] parameters)
        {
            if (game.enablePythonScripting && !string.IsNullOrEmpty(card.scriptName))
            {
                var allParams = new List<object> { card, this }.Concat(parameters).ToArray();
                game.ExecuteCardScript(card.scriptName, eventType, allParams);
            }
        }

        // Event handlers for card scripts
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

    // Static classes for constants
    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
    }

    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
    }

    public static class Decks
    {
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
    }
}