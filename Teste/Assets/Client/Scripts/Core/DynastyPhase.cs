using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    /// <summary>
    /// DynastyPhase manages the dynasty phase of the L5R game, including:
    /// - Dynasty card revealing
    /// - Fate collection from strongholds
    /// - Dynasty action window for purchasing and placing cards
    /// - Round number advancement and conflict record reset
    /// 
    /// Phase Structure:
    /// 1.1 Dynasty phase begins
    /// 1.2 Reveal facedown dynasty cards
    /// 1.3 Collect fate from strongholds
    /// 1.4 SPECIAL ACTION WINDOW (Dynasty actions)
    /// 1.5 Dynasty phase ends
    /// </summary>
    public class DynastyPhase : GamePhase
    {
        [Header("Dynasty Phase Settings")]
        public float cardRevealDelay = 0.5f;
        public float fateCollectionDelay = 1f;
        public bool enableRevealAnimation = true;
        public bool enableFateAnimation = true;

        [Header("Round Management")]
        public bool advanceRoundNumber = true;
        public bool resetConflictRecord = true;

        // Current state
        private List<BaseCard> revealedCards = new List<BaseCard>();
        private Dictionary<Player, int> fateCollected = new Dictionary<Player, int>();
        private bool cardsRevealed = false;
        private bool fateCollectionComplete = false;

        // Dynasty action window
        private DynastyActionWindow dynastyActionWindow;

        // Events
        public System.Action<List<BaseCard>> OnDynastyCardsRevealed;
        public System.Action<Player, int> OnPlayerFateCollected;
        public System.Action OnAllFateCollected;
        public System.Action OnDynastyActionWindowStarted;

        public DynastyPhase(Game game) : base(game, GamePhases.Dynasty)
        {
            InitializeDynastyPhase();
        }

        #region Phase Initialization
        private void InitializeDynastyPhase()
        {
            // Initialize the dynasty phase steps
            steps = new List<IGameStep>
            {
                new SimpleStep(game, CreatePhase),
                new SimpleStep(game, BeginDynasty),
                new SimpleStep(game, FlipDynastyCards),
                new SimpleStep(game, CollectFate),
                new SimpleStep(game, DynastyActionWindowStep),
                new SimpleStep(game, EndDynastyPhase)
            };
        }
        #endregion

        #region Phase Creation and Start
        public override void StartPhase()
        {
            base.StartPhase();
            
            game.AddMessage("=== Dynasty Phase Begins ===");
            ExecutePythonScript("on_dynasty_phase_start");
            
            // Reset phase state
            revealedCards.Clear();
            fateCollected.Clear();
            cardsRevealed = false;
            this.fateCollectionComplete = false;

            CreatePhase();
        }

        private bool CreatePhase()
        {
            if (advanceRoundNumber)
            {
                game.roundNumber++;
                game.AddMessage("=== Round {0} ===", game.roundNumber);
                ExecutePythonScript("on_round_advanced", game.roundNumber);
            }

            if (resetConflictRecord)
            {
                // game.conflictRecord.Clear(); // Temporarily disabled due to access level
                ExecutePythonScript("on_conflict_record_reset");
            }

            BeginDynasty();
            return true;
        }

        private bool BeginDynasty()
        {
            var playersInOrder = game.GetPlayersInFirstPlayerOrder();
            
            foreach (var player in playersInOrder)
            {
                player.BeginDynasty();
                ExecutePythonScript("on_player_begin_dynasty", player);
            }

            game.AddMessage("Dynasty phase begins. Revealing dynasty cards...");
            ExecutePythonScript("on_dynasty_phase_began");

            game.QueueStep(new SimpleStep(game, FlipDynastyCards));
            return true;
        }
        #endregion

        #region Dynasty Card Revealing
        private bool FlipDynastyCards()
        {
            if (cardsRevealed) return true;

            game.StartCoroutine(FlipDynastyCardsCoroutine());
            return true;
        }

        private IEnumerator FlipDynastyCardsCoroutine()
        {
            game.AddMessage("=== Revealing Dynasty Cards ===");
            ExecutePythonScript("on_dynasty_cards_reveal_start");

            var playersInOrder = game.GetPlayersInFirstPlayerOrder();
            var allRevealedCards = new List<BaseCard>();

            foreach (var player in playersInOrder)
            {
                var playerRevealedCards = new List<BaseCard>();
                var provinces = new[]
                {
                    Locations.ProvinceOne,
                    Locations.ProvinceTwo, 
                    Locations.ProvinceThree,
                    Locations.ProvinceFour
                };

                foreach (var provinceLocation in provinces)
                {
                    var card = player.GetDynastyCardInProvince(provinceLocation);
                    if (card != null && card.facedown)
                    {
                        // Apply flip dynasty action
                        var flipAction = new FlipDynastyAction(card);
                        var context = game.GetFrameworkContext();
                        flipAction.Apply(context);

                        playerRevealedCards.Add(card);
                        allRevealedCards.Add(card);

                        ExecutePythonScript("on_dynasty_card_revealed", card, player, provinceLocation);

                        if (enableRevealAnimation)
                        {
                            yield return new WaitForSeconds(cardRevealDelay);
                        }
                    }
                }

                if (playerRevealedCards.Count > 0)
                {
                    game.AddMessage("{0} reveals {1}", player.name, string.Join(", ", playerRevealedCards.Select(c => c.name)));
                    ExecutePythonScript("on_player_dynasty_cards_revealed", player, playerRevealedCards);
                }
                else
                {
                    game.AddMessage("{0} has no facedown dynasty cards to reveal.", player.name);
                }

                yield return new WaitForSeconds(0.2f);
            }

            revealedCards = allRevealedCards;
            cardsRevealed = true;
            
            OnDynastyCardsRevealed?.Invoke(allRevealedCards);
            ExecutePythonScript("on_all_dynasty_cards_revealed", allRevealedCards);

            game.QueueStep(new SimpleStep(game, CollectFate));
        }
        #endregion

        #region Fate Collection
        private bool CollectFate()
        {
            if (fateCollectionComplete) return true;

            game.StartCoroutine(CollectFateCoroutine());
            return true;
        }

        private IEnumerator CollectFateCoroutine()
        {
            game.AddMessage("=== Collecting Fate ===");
            ExecutePythonScript("on_fate_collection_start");

            yield return new WaitForSeconds(fateCollectionDelay);

            var playersInOrder = game.GetPlayersInFirstPlayerOrder();

            foreach (var player in playersInOrder)
            {
                int fateToCollect = CalculateFateToCollect(player);
                int oldFate = player.fate;
                
                player.CollectFate();
                
                int actualFateCollected = player.fate - oldFate;
                fateCollected[player] = actualFateCollected;

                game.AddMessage("{0} collects {1} fate (from {2} to {3}).", 
                              player.name, actualFateCollected, oldFate, player.fate);

                OnPlayerFateCollected?.Invoke(player, actualFateCollected);
                ExecutePythonScript("on_player_fate_collected", player, actualFateCollected, oldFate, player.fate);

                if (enableFateAnimation)
                {
                    yield return new WaitForSeconds(fateCollectionDelay);
                }
            }

            fateCollectionComplete = true;
            OnAllFateCollected?.Invoke();
            ExecutePythonScript("on_all_fate_collected", fateCollected);

            game.QueueStep(new SimpleStep(game, DynastyActionWindowStep));
        }

        private int CalculateFateToCollect(Player player)
        {
            int baseFate = 0;

            // Get fate from stronghold
            if (player.stronghold != null)
            {
                baseFate = player.stronghold.GetFate();
            }

            // Apply effects that modify fate collection
            int modifierEffects = player.SumEffects(EffectNames.ModifyFateCollectedInDynastyPhase);
            
            // Apply multiplier effects
            float multiplier = player.GetEffectValue(EffectNames.ModifyFateCollectionMultiplier, 1);
            
            int totalFate = Mathf.RoundToInt((baseFate + modifierEffects) * multiplier);
            
            return Mathf.Max(0, totalFate);
        }
        #endregion

        #region Dynasty Action Window
        private bool DynastyActionWindowStep()
        {
            game.AddMessage("=== Dynasty Action Window ===");
            game.AddMessage("Players may now purchase and play dynasty cards, or trigger action abilities.");
            
            dynastyActionWindow = new DynastyActionWindow(game);
            dynastyActionWindow.OnActionWindowComplete += OnDynastyActionWindowComplete;
            
            OnDynastyActionWindowStarted?.Invoke();
            ExecutePythonScript("on_dynasty_action_window_start");
            
            game.QueueStep(dynastyActionWindow);
            return true;
        }

        private void OnDynastyActionWindowComplete()
        {
            ExecutePythonScript("on_dynasty_action_window_complete");
            game.QueueStep(new SimpleStep(game, EndDynastyPhase));
        }
        #endregion

        #region Phase Management
        public override void EndPhase()
        {
            ExecutePythonScript("on_dynasty_phase_end");
            game.AddMessage("=== Dynasty Phase Ends ===");

            // Cleanup dynasty action window
            if (dynastyActionWindow != null)
            {
                dynastyActionWindow.Cleanup();
                dynastyActionWindow = null;
            }

            base.EndPhase();
        }

        private bool EndDynastyPhase()
        {
            game.QueueStep(new SimpleStep(game, () => { EndPhase(); return true; }));
            return true;
        }
        #endregion

        #region Public Interface
        public List<BaseCard> GetRevealedCards()
        {
            return new List<BaseCard>(revealedCards);
        }

        public Dictionary<Player, int> GetFateCollectedThisPhase()
        {
            return new Dictionary<Player, int>(fateCollected);
        }

        public bool AreCardsRevealed()
        {
            return cardsRevealed;
        }

        public bool IsFateCollected()
        {
            return fateCollectionComplete;
        }

        public bool IsDynastyActionWindowActive()
        {
            return dynastyActionWindow != null && dynastyActionWindow.IsActive();
        }

        public int GetPlayerFateCollected(Player player)
        {
            return fateCollected.TryGetValue(player, out int fate) ? fate : 0;
        }
        #endregion

        #region Dynasty Specific Actions
        public bool CanPlayerAffordCard(Player player, BaseCard card)
        {
            if (card is DrawCard drawCard)
            {
                int cost = drawCard.GetCost();
                return player.fate >= cost;
            }
            return false;
        }

        private BaseCard GetProvinceByLocation(Player player, string location)
        {
            switch (location.ToLower())
            {
                case "province 1":
                case "province1":
                    return player.GetProvince(0);
                case "province 2":
                case "province2":
                    return player.GetProvince(1);
                case "province 3":
                case "province3":
                    return player.GetProvince(2);
                case "province 4":
                case "province4":
                    return player.GetProvince(3);
                default:
                    return null;
            }
        }

        public bool CanPlayerPlayCardFromProvince(Player player, BaseCard card, string provinceLocation)
        {
            // Check basic conditions
            if (!CanPlayerAffordCard(player, card))
                return false;

            // Check if card is in the specified province
            var cardInProvince = player.GetDynastyCardInProvince(provinceLocation);
            if (cardInProvince != card)
                return false;

            // Check if province is broken
            var province = GetProvinceByLocation(player, provinceLocation);
            if (province != null && province.isBroken)
                return false;

            // Check card-specific restrictions
            var context = game.GetFrameworkContext(player);
            return card.CanPlay(context, "dynasty");
        }

        public void HandleCardPurchase(Player player, BaseCard card, string provinceLocation)
        {
            if (!CanPlayerPlayCardFromProvince(player, card, provinceLocation))
            {
                game.AddMessage("{0} cannot play {1} from {2}.", player.name, card.name, provinceLocation);
                return;
            }

            if (card is DrawCard drawCard)
            {
                int cost = drawCard.GetCost();
                player.SpendFate(cost);
                
                // Move card from province to play area or hand
                var playAction = new PlayCardFromProvinceAction(drawCard, provinceLocation);
                var context = game.GetFrameworkContext(player);
                playAction.Apply(context);

                game.AddMessage("{0} plays {1} from {2} for {3} fate.", 
                              player.name, card.name, provinceLocation, cost);

                ExecutePythonScript("on_card_played_from_province", player, card, provinceLocation, cost);
            }
        }
        #endregion

        #region IronPython Integration
        protected override void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                game.ExecutePhaseScript("dynasty_phase.py", methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for dynasty phase: {ex.Message}");
            }
        }

        // Phase-specific Python events
        public void OnCardMovedToProvince(Player player, BaseCard card, string provinceLocation)
        {
            ExecutePythonScript("on_card_moved_to_province", player, card, provinceLocation);
        }

        public void OnProvinceEmptied(Player player, string provinceLocation)
        {
            ExecutePythonScript("on_province_emptied", player, provinceLocation);
        }

        public void OnStrongholdAbilityUsed(Player player, StrongholdCard stronghold)
        {
            ExecutePythonScript("on_stronghold_ability_used", player, stronghold);
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogDynastyPhaseStatus()
        {
            Debug.Log($"Dynasty Phase Status:\n" +
                     $"Round Number: {game.roundNumber}\n" +
                     $"Cards Revealed: {cardsRevealed} ({revealedCards.Count} total)\n" +
                     $"Fate Collected: {fateCollectionComplete}\n" +
                     $"Dynasty Action Window Active: {IsDynastyActionWindowActive()}\n" +
                     $"Player Fate: {string.Join(", ", game.GetPlayers().Select(p => $"{p.name}:{p.fate}"))}");
        }

        public void LogRevealedCards()
        {
            if (revealedCards.Count > 0)
            {
                Debug.Log($"Revealed Dynasty Cards ({revealedCards.Count}):\n" +
                         string.Join("\n", revealedCards.Select(card => $"- {card.name} ({card.owner.name})")));
            }
            else
            {
                Debug.Log("No dynasty cards were revealed this phase.");
            }
        }
        #endregion
    }

    #region Supporting Classes
    public class FlipDynastyAction
    {
        public BaseCard targetCard;

        public FlipDynastyAction(BaseCard card)
        {
            targetCard = card;
        }

        public void Apply(AbilityContext context)
        {
            if (targetCard != null && targetCard.facedown)
            {
                targetCard.FlipFaceup();
            }
        }
    }

    public class PlayCardFromProvinceAction
    {
        public DrawCard targetCard;
        public string provinceLocation;

        public PlayCardFromProvinceAction(DrawCard card, string location)
        {
            targetCard = card;
            provinceLocation = location;
        }

        public void Apply(AbilityContext context)
        {
            if (targetCard != null)
            {
                // Move card to appropriate location based on type
                if (targetCard.type == CardTypes.Character)
                {
                    targetCard.MoveTo(Locations.PlayArea);
                }
                else if (targetCard.type == CardTypes.Holding)
                {
                    // Holdings typically stay in provinces as attachments
                    targetCard.MoveTo(provinceLocation);
                }
                
                var playContext = AbilityContext.CreateCardContext(context.game, targetCard, targetCard.controller);
                targetCard.Play(playContext);
            }
        }
    }

    // Dynasty-specific action window with special purchasing rules
    public class DynastyActionWindow : ActionWindow
    {
        public System.Action OnActionWindowComplete;

        public DynastyActionWindow(Game game) : base(game)
        {
        }

        public new void Execute()
        {
            game.OpenDynastyActionWindow(() => 
            {
                isComplete = true;
                OnActionWindowComplete?.Invoke();
            });
        }

        public bool IsActive()
        {
            return !isComplete;
        }

        public new void Cleanup()
        {
            base.Cleanup();
            OnActionWindowComplete = null;
        }
    }
    #endregion
}