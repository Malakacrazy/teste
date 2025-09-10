using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    /// <summary>
    /// FatePhase manages the fate phase of the L5R game, including:
    /// - Discarding characters with no fate
    /// - Removing fate from all characters
    /// - Placing fate on unclaimed rings
    /// - Action window for final actions
    /// - Readying all cards
    /// - Province discard and replacement
    /// - Ring return and first player token passing
    /// 
    /// Phase Structure:
    /// 4.1 Fate phase begins
    /// 4.2 Discard characters with no fate
    /// 4.3 Remove fate from characters
    /// 4.4 Place fate on unclaimed rings
    /// 4.5 ACTION WINDOW
    /// 4.6 Ready cards
    /// 4.7 Discard from provinces
    /// 4.8 Return rings
    /// 4.9 Pass first player token
    /// 4.10 Fate phase ends
    /// </summary>
    public class FatePhase : GamePhase
    {
        [Header("Fate Phase Settings")]
        
        // Phase step management
        protected List<IGameStep> steps;
        public float characterDiscardDelay = 1f;
        public float fateRemovalDelay = 0.5f;
        public float ringFateDelay = 0.3f;
        public float cardReadyDelay = 0.2f;
        public bool enableDiscardAnimation = true;
        public bool enableFateAnimation = true;

        [Header("Province Management")]
        public bool autoDiscardFromBrokenProvinces = true;
        public bool autoReplaceEmptyProvinces = true;

        // Current state
        private Dictionary<Player, List<DrawCard>> charactersToDiscard = new Dictionary<Player, List<DrawCard>>();
        private List<BaseCard> cardsWithFateRemoved = new List<BaseCard>();
        private List<Ring> ringsWithFateAdded = new List<Ring>();
        private List<BaseCard> cardsReadied = new List<BaseCard>();
        private Dictionary<Player, List<BaseCard>> provincesDiscarded = new Dictionary<Player, List<BaseCard>>();

        // Phase state tracking
        private bool charactersDiscarded = false;
        private bool fateRemoved = false;
        private bool ringFatePlaced = false;
        private bool cardsReadiedPhaseComplete = false;
        private bool provincesProcessed = false;
        private bool ringsReturned = false;
        private bool firstPlayerPassed = false;

        // Events
        public System.Action<Player, List<DrawCard>> OnCharactersDiscarded;
        public System.Action<List<BaseCard>> OnFateRemovedFromCards;
        public System.Action<List<Ring>> OnFatePlacedOnRings;
        public System.Action<List<BaseCard>> OnCardsReadied;
        public System.Action<Player, List<BaseCard>> OnProvincesDiscarded;
        public System.Action<List<Ring>> OnRingsReturned;
        public System.Action<Player, Player> OnFirstPlayerPassed;

        public FatePhase(Game game) : base(game, GamePhases.Fate)
        {
            InitializeFatePhase();
        }

        #region Phase Initialization
        private void InitializeFatePhase()
        {
            // Initialize the fate phase steps
            steps = new List<IGameStep>
            {
                new SimpleStep(game, BeginFatePhase),
                new SimpleStep(game, DiscardCharactersWithNoFate),
                new SimpleStep(game, RemoveFateFromCharacters),
                new SimpleStep(game, PlaceFateOnUnclaimedRings),
                new ActionWindow(game, "Action Window"),
                new SimpleStep(game, ReadyCards),
                new SimpleStep(game, DiscardFromProvinces),
                new SimpleStep(game, ReturnRings),
                new SimpleStep(game, PassFirstPlayer),
                new SimpleStep(game, EndFatePhase)
            };
        }
        #endregion

        #region Phase Execution
        protected override bool StartPhase()
        {
            base.StartPhase();
            
            game.AddMessage("=== Fate Phase Begins ===");
            ExecutePythonScript("on_fate_phase_start");
            
            // Reset phase state
            charactersToDiscard.Clear();
            cardsWithFateRemoved.Clear();
            ringsWithFateAdded.Clear();
            cardsReadied.Clear();
            provincesDiscarded.Clear();

            charactersDiscarded = false;
            fateRemoved = false;
            ringFatePlaced = false;
            cardsReadiedPhaseComplete = false;
            provincesProcessed = false;
            ringsReturned = false;
            firstPlayerPassed = false;

            BeginFatePhase();
            return true;
        }

        private bool BeginFatePhase()
        {
            game.AddMessage("Fate phase begins. Preparing to discard characters with no fate...");
            ExecutePythonScript("on_fate_phase_began");

            game.QueueStep(new SimpleStep(game, DiscardCharactersWithNoFate));
            return true;
        }
        #endregion

        #region Character Discard
        private bool DiscardCharactersWithNoFate()
        {
            if (charactersDiscarded) return true;

            game.StartCoroutine(DiscardCharactersWithNoFateCoroutine());
            return true;
        }

        private IEnumerator DiscardCharactersWithNoFateCoroutine()
        {
            game.AddMessage("=== Discarding Characters With No Fate ===");
            ExecutePythonScript("on_character_discard_start");

            var playersInOrder = game.GetPlayersInFirstPlayerOrder();

            foreach (var player in playersInOrder)
            {
                var charactersWithNoFate = player.GetCardsInPlay()
                    .OfType<DrawCard>()
                    .Where(card => card.fate == 0 && card.AllowGameAction("discardFromPlay", game.GetFrameworkContext()))
                    .ToList();

                if (charactersWithNoFate.Count > 0)
                {
                    charactersToDiscard[player] = charactersWithNoFate;
                    yield return game.StartCoroutine(PromptPlayerToDiscardCharacters(player, charactersWithNoFate));
                }
                else
                {
                    game.AddMessage("{0} has no characters with 0 fate to discard.", player.name);
                    ExecutePythonScript("on_player_no_characters_to_discard", player);
                }

                yield return new WaitForSeconds(0.2f);
            }

            charactersDiscarded = true;
            ExecutePythonScript("on_all_characters_discarded", charactersToDiscard);

            game.QueueStep(new SimpleStep(game, RemoveFateFromCharacters));
        }

        private IEnumerator PromptPlayerToDiscardCharacters(Player player, List<DrawCard> cardsToDiscard)
        {
            if (cardsToDiscard.Count == 0) yield break;

            game.AddMessage("{0} must discard {1} character(s) with no fate.", player.name, cardsToDiscard.Count);

            // For now, auto-discard all characters with no fate
            // In a full implementation, this would prompt the player for selective discard
            var discardedThisPlayer = new List<DrawCard>();

            foreach (var character in cardsToDiscard.ToList())
            {
                if (character.AllowGameAction("discardFromPlay", game.GetFrameworkContext()))
                {
                    var discardAction = new DiscardFromPlayAction(character);
                    var context = game.GetFrameworkContext();
                    discardAction.Apply(context);

                    discardedThisPlayer.Add(character);
                    
                    game.AddMessage("{0} discards {1} (no fate remaining).", player.name, character.name);
                    ExecutePythonScript("on_character_discarded", player, character);

                    if (enableDiscardAnimation)
                    {
                        yield return new WaitForSeconds(characterDiscardDelay);
                    }
                }
            }

            if (discardedThisPlayer.Count > 0)
            {
                OnCharactersDiscarded?.Invoke(player, discardedThisPlayer);
                ExecutePythonScript("on_player_characters_discarded", player, discardedThisPlayer);
            }
        }
        #endregion

        #region Fate Removal
        private bool RemoveFateFromCharacters()
        {
            if (fateRemoved) return true;

            game.StartCoroutine(RemoveFateFromCharactersCoroutine());
            return true;
        }

        private IEnumerator RemoveFateFromCharactersCoroutine()
        {
            game.AddMessage("=== Removing Fate From Characters ===");
            ExecutePythonScript("on_fate_removal_start");

            var cardsWithFate = game.GetAllCardsInPlay()
                .Where(card => card.AllowGameAction("removeFate", game.GetFrameworkContext()) && card.GetTokenCount("fate") > 0)
                .ToList();

            if (cardsWithFate.Count > 0)
            {
                foreach (var card in cardsWithFate)
                {
                    int fateToRemove = card.GetTokenCount("fate");
                    if (fateToRemove > 0)
                    {
                        var removeFateAction = new RemoveFateAction(card, fateToRemove);
                        var context = game.GetFrameworkContext();
                        removeFateAction.Apply(context);

                        cardsWithFateRemoved.Add(card);
                        
                        game.AddMessage("Removing {0} fate from {1}.", fateToRemove, card.name);
                        ExecutePythonScript("on_fate_removed_from_card", card, fateToRemove);

                        if (enableFateAnimation)
                        {
                            yield return new WaitForSeconds(fateRemovalDelay);
                        }
                    }
                }

                OnFateRemovedFromCards?.Invoke(cardsWithFateRemoved);
            }
            else
            {
                game.AddMessage("No fate to remove from characters.");
            }

            fateRemoved = true;
            ExecutePythonScript("on_fate_removal_complete", cardsWithFateRemoved);

            game.QueueStep(new SimpleStep(game, PlaceFateOnUnclaimedRings));
        }
        #endregion

        #region Ring Fate Placement
        private bool PlaceFateOnUnclaimedRings()
        {
            if (ringFatePlaced) return true;

            game.StartCoroutine(PlaceFateOnUnclaimedRingsCoroutine());
            return true;
        }

        private IEnumerator PlaceFateOnUnclaimedRingsCoroutine()
        {
            game.AddMessage("=== Placing Fate On Unclaimed Rings ===");
            ExecutePythonScript("on_place_fate_on_rings_start");

            // Raise event for any effects that might modify this process
            foreach (var ring in game.GetAllRings())
            {
                if (!ring.IsClaimed())
                {
                    ring.ModifyFate(1);
                    ringsWithFateAdded.Add(ring);
                    
                    game.AddMessage("Placing 1 fate on unclaimed {0} ring (now has {1} fate).", 
                                  ring.element, ring.GetFate());
                    
                    ExecutePythonScript("on_fate_placed_on_ring", ring);
                }
            }

            // Visual delay for each ring
            foreach (var ring in ringsWithFateAdded)
            {
                yield return new WaitForSeconds(ringFateDelay);
            }

            if (ringsWithFateAdded.Count > 0)
            {
                OnFatePlacedOnRings?.Invoke(ringsWithFateAdded);
                game.AddMessage("Fate placed on {0} unclaimed ring(s).", ringsWithFateAdded.Count);
            }
            else
            {
                game.AddMessage("All rings are claimed - no fate placed on rings.");
            }

            ringFatePlaced = true;
            ExecutePythonScript("on_place_fate_on_rings_complete", ringsWithFateAdded);

            // Proceed to action window
            game.QueueStep(new ActionWindow(game, "Action Window"));
        }
        #endregion

        #region Card Readying
        private bool ReadyCards()
        {
            if (cardsReadiedPhaseComplete) return true;

            game.StartCoroutine(ReadyCardsCoroutine());
            return true;
        }

        private IEnumerator ReadyCardsCoroutine()
        {
            game.AddMessage("=== Readying Cards ===");
            ExecutePythonScript("on_card_ready_start");

            var cardsToReady = game.GetAllCards()
                .Where(card => card.bowed && card.ReadiesDuringReadyPhase())
                .ToList();

            if (cardsToReady.Count > 0)
            {
                foreach (var card in cardsToReady)
                {
                    var readyAction = new ReadyAction(card);
                    var context = game.GetFrameworkContext();
                    readyAction.Apply(context);

                    cardsReadied.Add(card);
                    ExecutePythonScript("on_card_readied", card);

                    yield return new WaitForSeconds(cardReadyDelay);
                }

                game.AddMessage("Readied {0} card(s).", cardsReadied.Count);
                OnCardsReadied?.Invoke(cardsReadied);
            }
            else
            {
                game.AddMessage("No cards to ready.");
            }

            cardsReadiedPhaseComplete = true;
            ExecutePythonScript("on_card_ready_complete", cardsReadied);

            game.QueueStep(new SimpleStep(game, DiscardFromProvinces));
        }
        #endregion

        #region Province Discard
        private bool DiscardFromProvinces()
        {
            if (provincesProcessed) return true;

            game.StartCoroutine(DiscardFromProvincesCoroutine());
            return true;
        }

        private IEnumerator DiscardFromProvincesCoroutine()
        {
            game.AddMessage("=== Processing Province Cards ===");
            ExecutePythonScript("on_province_discard_start");

            var playersInOrder = game.GetPlayersInFirstPlayerOrder();

            foreach (var player in playersInOrder)
            {
                yield return game.StartCoroutine(DiscardFromProvincesForPlayer(player));
                yield return new WaitForSeconds(0.5f);
            }

            provincesProcessed = true;
            ExecutePythonScript("on_province_discard_complete", provincesDiscarded);

            game.QueueStep(new SimpleStep(game, ReturnRings));
        }

        private IEnumerator DiscardFromProvincesForPlayer(Player player)
        {
            var cardsToDiscard = new List<BaseCard>();
            var cardsOnUnbrokenProvinces = new List<BaseCard>();

            var provinceLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo, 
                Locations.ProvinceThree, Locations.ProvinceFour, 
                Locations.StrongholdProvince
            };

            // Collect cards from all provinces
            foreach (var location in provinceLocations)
            {
                var provinceCard = player.GetProvinceCardInProvince(location);
                var dynastyCards = player.GetDynastyCardsInProvince(location)
                    .Where(card => card.isDynasty && !card.facedown)
                    .ToList();

                if (dynastyCards.Count > 0 && provinceCard != null)
                {
                    if (provinceCard.isBroken)
                    {
                        cardsToDiscard.AddRange(dynastyCards);
                    }
                    else
                    {
                        cardsOnUnbrokenProvinces.AddRange(dynastyCards);
                    }
                }
            }

            // Handle optional discard from unbroken provinces
            if (cardsOnUnbrokenProvinces.Count > 0)
            {
                // For now, auto-skip optional discard
                // In full implementation, this would prompt the player
                game.AddMessage("{0} chooses not to discard any dynasty cards from unbroken provinces.", player.name);
                ExecutePythonScript("on_player_skipped_optional_discard", player, cardsOnUnbrokenProvinces);
            }

            // Discard cards from broken provinces
            if (cardsToDiscard.Count > 0)
            {
                game.AddMessage("{0} discards {1} from broken provinces.", 
                              player.name, string.Join(", ", cardsToDiscard.Select(c => c.name)));

                foreach (var card in cardsToDiscard)
                {
                    var discardAction = new DiscardCardAction(card);
                    var context = game.GetFrameworkContext();
                    discardAction.Apply(context);

                    ExecutePythonScript("on_card_discarded_from_province", player, card);
                    yield return new WaitForSeconds(0.2f);
                }

                provincesDiscarded[player] = cardsToDiscard;
                OnProvincesDiscarded?.Invoke(player, cardsToDiscard);
                ExecutePythonScript("on_player_provinces_discarded", player, cardsToDiscard);
            }

            // Replace empty provinces
            if (autoReplaceEmptyProvinces)
            {
                yield return game.StartCoroutine(ReplaceEmptyProvinces(player));
            }
        }

        private IEnumerator ReplaceEmptyProvinces(Player player)
        {
            var standardProvinces = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo,
                Locations.ProvinceThree, Locations.ProvinceFour
            };

            foreach (var location in standardProvinces)
            {
                var dynastyCard = player.GetDynastyCardInProvince(location);
                if (dynastyCard == null)
                {
                    player.ReplaceDynastyCard(location);
                    game.AddMessage("{0} replaces the dynasty card in {1}.", player.name, location);
                    ExecutePythonScript("on_dynasty_card_replaced", player, location);
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }
        #endregion

        #region Ring Return
        private bool ReturnRings()
        {
            if (ringsReturned) return true;

            game.StartCoroutine(ReturnRingsCoroutine());
            return true;
        }

        private IEnumerator ReturnRingsCoroutine()
        {
            game.AddMessage("=== Returning Claimed Rings ===");
            ExecutePythonScript("on_ring_return_start");

            var claimedRings = game.GetAllRings().Where(ring => ring.IsClaimed()).ToList();

            if (claimedRings.Count > 0)
            {
                foreach (var ring in claimedRings)
                {
                    var returnAction = new ReturnRingAction(ring);
                    var context = game.GetFrameworkContext();
                    returnAction.Apply(context);

                    game.AddMessage("Returning {0} ring to unclaimed pool.", ring.element);
                    ExecutePythonScript("on_ring_returned", ring);

                    yield return new WaitForSeconds(0.3f);
                }

                OnRingsReturned?.Invoke(claimedRings);
                game.AddMessage("Returned {0} claimed ring(s).", claimedRings.Count);
            }
            else
            {
                game.AddMessage("No claimed rings to return.");
            }

            ringsReturned = true;
            ExecutePythonScript("on_ring_return_complete", claimedRings);

            game.QueueStep(new SimpleStep(game, PassFirstPlayer));
        }
        #endregion

        #region First Player Passing
        private bool PassFirstPlayer()
        {
            if (firstPlayerPassed) return true;

            var currentFirstPlayer = game.GetFirstPlayer();
            var nextFirstPlayer = game.GetOtherPlayer(currentFirstPlayer);

            if (nextFirstPlayer != null)
            {
                game.SetFirstPlayer(nextFirstPlayer);

                game.AddMessage("First player token passes from {0} to {1}.", currentFirstPlayer.name, nextFirstPlayer.name);
                
                OnFirstPlayerPassed?.Invoke(currentFirstPlayer, nextFirstPlayer);
                ExecutePythonScript("on_first_player_passed", currentFirstPlayer, nextFirstPlayer);
            }
            else
            {
                game.AddMessage("No opponent found - first player token remains with {0}.", currentFirstPlayer.name);
            }

            firstPlayerPassed = true;
            game.QueueStep(new SimpleStep(game, EndFatePhase));
            return true;
        }
        #endregion

        #region Phase Management
        protected override bool EndPhase()
        {
            ExecutePythonScript("on_fate_phase_end");
            game.AddMessage("=== Fate Phase Ends ===");

            base.EndPhase();
            return true;
        }

        private bool EndFatePhase()
        {
            game.QueueStep(new SimpleStep(game, () => { EndPhase(); return true; }));
            return true;
        }
        #endregion

        #region Public Interface
        public Dictionary<Player, List<DrawCard>> GetCharactersDiscarded()
        {
            return new Dictionary<Player, List<DrawCard>>(charactersToDiscard);
        }

        public List<BaseCard> GetCardsWithFateRemoved()
        {
            return new List<BaseCard>(cardsWithFateRemoved);
        }

        public List<Ring> GetRingsWithFateAdded()
        {
            return new List<Ring>(ringsWithFateAdded);
        }

        public List<BaseCard> GetCardsReadied()
        {
            return new List<BaseCard>(cardsReadied);
        }

        public Dictionary<Player, List<BaseCard>> GetProvincesDiscarded()
        {
            return new Dictionary<Player, List<BaseCard>>(provincesDiscarded);
        }

        public bool AreCharactersDiscarded() => charactersDiscarded;
        public bool IsFateRemoved() => fateRemoved;
        public bool IsRingFatePlaced() => ringFatePlaced;
        public bool AreCardsReadied() => cardsReadiedPhaseComplete;
        public bool AreProvincesProcessed() => provincesProcessed;
        public bool AreRingsReturned() => ringsReturned;
        public bool IsFirstPlayerPassed() => firstPlayerPassed;
        #endregion

        #region IronPython Integration
        protected virtual void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                game.ExecutePhaseScript("fate_phase.py", methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for fate phase: {ex.Message}");
            }
        }

        // Phase-specific Python events
        public void OnCharacterSavedFromDiscard(DrawCard character, Player player)
        {
            ExecutePythonScript("on_character_saved_from_discard", character, player);
        }

        public void OnRingFateModified(Ring ring, int originalFate, int newFate)
        {
            ExecutePythonScript("on_ring_fate_modified", ring, originalFate, newFate);
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogFatePhaseStatus()
        {
            Debug.Log($"Fate Phase Status:\n" +
                     $"Characters Discarded: {charactersDiscarded}\n" +
                     $"Fate Removed: {fateRemoved}\n" +
                     $"Ring Fate Placed: {ringFatePlaced}\n" +
                     $"Cards Readied: {cardsReadiedPhaseComplete}\n" +
                     $"Provinces Processed: {provincesProcessed}\n" +
                     $"Rings Returned: {ringsReturned}\n" +
                     $"First Player Passed: {firstPlayerPassed}");
        }

        public void LogPhaseResults()
        {
            Debug.Log($"Fate Phase Results:\n" +
                     $"Characters Discarded: {charactersToDiscard.Values.Sum(list => list.Count)}\n" +
                     $"Cards With Fate Removed: {cardsWithFateRemoved.Count}\n" +
                     $"Rings With Fate Added: {ringsWithFateAdded.Count}\n" +
                     $"Cards Readied: {cardsReadied.Count}\n" +
                     $"Province Cards Discarded: {provincesDiscarded.Values.Sum(list => list.Count)}");
        }
        #endregion
    }

    #region Supporting Action Classes
    public partial class DiscardFromPlayAction
    {
        public DrawCard targetCard;

        public DiscardFromPlayAction(DrawCard card)
        {
            targetCard = card;
        }

        public void Apply(AbilityContext context)
        {
            if (targetCard != null)
            {
                targetCard.MoveTo(Locations.DynastyDiscardPile);
            }
        }
    }

    public partial class RemoveFateAction
    {
        public BaseCard targetCard;
        public int amount;

        public RemoveFateAction(BaseCard card, int fateAmount)
        {
            targetCard = card;
            amount = fateAmount;
        }

        public void Apply(AbilityContext context)
        {
            if (targetCard != null)
            {
                targetCard.RemoveToken("fate", amount);
            }
        }
    }

    public partial class ReadyAction
    {
        public BaseCard targetCard;

        public ReadyAction(BaseCard card)
        {
            targetCard = card;
        }

        public void Apply(AbilityContext context)
        {
            if (targetCard != null && targetCard.bowed)
            {
                targetCard.Ready();
            }
        }
    }

    public partial class DiscardCardAction
    {
        public BaseCard targetCard;

        public DiscardCardAction(BaseCard card)
        {
            targetCard = card;
        }

        public void Apply(AbilityContext context)
        {
            if (targetCard != null)
            {
                targetCard.MoveTo(Locations.DynastyDiscardPile);
            }
        }
    }

    public partial class ReturnRingAction
    {
        public Ring targetRing;

        public ReturnRingAction(Ring ring) : base(new RingActionProperties { Target = new List<object> { ring } })
        {
            targetRing = ring;
        }

        public void Apply(AbilityContext context)
        {
            if (targetRing != null)
            {
                targetRing.ReturnToUnclaimed();
            }
        }
    }
    #endregion
}