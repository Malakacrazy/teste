using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    /// <summary>
    /// DrawPhase manages the draw phase of the L5R game, including:
    /// - Honor bidding mechanics
    /// - Honor dial reveal and honor transfer
    /// - Card drawing based on honor bids
    /// - Action window after card draw
    /// 
    /// Phase Structure:
    /// 2.1 Draw phase begins
    /// 2.2 Honor bid (simultaneous hidden bids)
    /// 2.3 Reveal honor dials
    /// 2.4 Transfer honor based on bids
    /// 2.5 Draw cards equal to honor bid
    /// 2.6 ACTION WINDOW
    /// 2.7 Draw phase ends
    /// </summary>
    public class DrawPhase : GamePhase
    {
        [Header("Draw Phase Settings")]
        
        // Phase step management  
        protected List<IGameStep> steps;
        public float honorBidTimeout = 30f;
        public float honorRevealDelay = 2f;
        public float cardDrawDelay = 1f;
        public bool enableHonorBidAnimation = true;

        [Header("Honor Bid Rules")]
        public int minHonorBid = 1;
        public int maxHonorBid = 5;
        public bool allowZeroBid = true; // Some variants allow 0 bid

        // Current state
        private Dictionary<Player, int> playerHonorBids = new Dictionary<Player, int>();
        private Dictionary<Player, bool> playersReadyForBid = new Dictionary<Player, bool>();
        private bool honorBidsRevealed = false;
        private bool honorTransferComplete = false;
        private bool cardDrawComplete = false;

        // Honor bid prompt
        private HonorBidPrompt currentHonorBidPrompt;

        // Events
        public System.Action<Dictionary<Player, int>> OnHonorBidsRevealed;
        public System.Action<Player, int, int> OnHonorTransferred; // player, oldHonor, newHonor
        public System.Action<Player, int> OnCardsDrawn; // player, cardCount
        public System.Action OnDrawPhaseCardDrawComplete;

        public DrawPhase(Game game) : base(game, GamePhases.Draw)
        {
            InitializeDrawPhase();
        }

        #region Phase Initialization
        private void InitializeDrawPhase()
        {
            // Initialize the draw phase steps
            steps = new List<IGameStep>
            {
                new SimpleStep(game, BeginDrawPhase),
                new SimpleStep(game, DisplayHonorBidPrompt),
                new SimpleStep(game, RevealHonorBids),
                new SimpleStep(game, TransferHonor),
                new SimpleStep(game, DrawConflictCards),
                new ActionWindow(game),
                new SimpleStep(game, EndDrawPhase)
            };
        }
        #endregion

        #region Phase Execution
        protected override bool StartPhase()
        {
            base.StartPhase();
            
            game.AddMessage("=== Draw Phase Begins ===");
            ExecutePythonScript("on_draw_phase_start");
            
            // Reset phase state
            playerHonorBids.Clear();
            playersReadyForBid.Clear();
            honorBidsRevealed = false;
            honorTransferComplete = false;
            cardDrawComplete = false;

            // Initialize player states
            foreach (var player in game.GetPlayers())
            {
                playersReadyForBid[player] = false;
                player.honorBid = 0; // Reset previous bid
            }

            BeginDrawPhase();
            return true;
        }

        private bool BeginDrawPhase()
        {
            game.AddMessage("Draw phase begins. Players prepare to bid honor.");
            ExecutePythonScript("on_draw_phase_began");

            // Allow players to make pre-bid actions
            game.QueueStep(new SimpleStep(game, DisplayHonorBidPrompt));
            return true;
        }

        private bool DisplayHonorBidPrompt()
        {
            game.AddMessage("Players make their honor bids simultaneously.");
            ExecutePythonScript("on_honor_bid_prompt_start");

            // Simplified honor bidding - players will bid through UI
            // The complex HonorBidPrompt system has been simplified

            // Start the bidding process
            foreach (var player in game.GetPlayers())
            {
                PromptPlayerForHonorBid(player);
            }

            // Start timeout coroutine
            game.StartCoroutine(HonorBidTimeoutCoroutine());
            return true;
        }

        private void PromptPlayerForHonorBid(Player player)
        {
            var bidOptions = new List<HonorBidOption>();
            
            // Generate valid bid options based on player's current honor
            int minBid = allowZeroBid ? 0 : minHonorBid;
            int maxBid = Mathf.Min(maxHonorBid, player.honor);

            for (int bid = minBid; bid <= maxBid; bid++)
            {
                bidOptions.Add(new HonorBidOption
                {
                    bidAmount = bid,
                    displayText = GetHonorBidDisplayText(bid),
                    canAfford = player.honor >= bid
                });
            }

            // Apply any effects that modify available bids
            ModifyHonorBidOptions(player, bidOptions);

            // Simplified: Player will make bid through UI
            // TODO: Implement simplified honor bid prompt
            ExecutePythonScript("on_player_honor_bid_prompted", player, bidOptions);
        }

        private string GetHonorBidDisplayText(int bid)
        {
            return bid switch
            {
                0 => "0 Honor - Draw 0 cards (no honor loss)",
                1 => "1 Honor - Draw 1 card",
                2 => "2 Honor - Draw 2 cards", 
                3 => "3 Honor - Draw 3 cards",
                4 => "4 Honor - Draw 4 cards",
                5 => "5 Honor - Draw 5 cards",
                _ => $"{bid} Honor - Draw {bid} cards"
            };
        }

        private void ModifyHonorBidOptions(Player player, List<HonorBidOption> bidOptions)
        {
            // Apply effects that modify available honor bids
            var restrictedBids = player.GetEffects(EffectNames.RestrictHonorBid);
            if (restrictedBids.Any())
            {
                bidOptions.RemoveAll(option => restrictedBids.Contains(option.bidAmount));
            }

            // Apply effects that add additional bid options
            var additionalBids = player.GetEffects(EffectNames.AddHonorBidOption);
            foreach (var additionalBidObj in additionalBids)
            {
                if (additionalBidObj is int additionalBid)
                {
                    if (!bidOptions.Any(option => option.bidAmount == additionalBid))
                    {
                        bidOptions.Add(new HonorBidOption
                        {
                            bidAmount = additionalBid,
                            displayText = GetHonorBidDisplayText(additionalBid),
                            canAfford = player.honor >= additionalBid
                        });
                    }
                }
            }

            bidOptions.Sort((a, b) => a.bidAmount.CompareTo(b.bidAmount));
        }
        #endregion

        #region Honor Bidding
        private void OnPlayerHonorBidChanged(Player player, int newBid)
        {
            playerHonorBids[player] = newBid;
            playersReadyForBid[player] = true;
            
            ExecutePythonScript("on_player_honor_bid_changed", player, newBid);
            
            game.AddMessage("{0} has submitted their honor bid.", player.name);
        }

        private void OnAllHonorBidsSubmitted()
        {
            if (playersReadyForBid.Values.All(ready => ready))
            {
                game.QueueStep(new SimpleStep(game, RevealHonorBids));
            }
        }

        private IEnumerator HonorBidTimeoutCoroutine()
        {
            yield return new WaitForSeconds(honorBidTimeout);
            
            // Auto-submit bids for players who haven't submitted
            foreach (var player in game.GetPlayers())
            {
                if (!playersReadyForBid[player])
                {
                    int defaultBid = allowZeroBid ? 0 : minHonorBid;
                    defaultBid = Mathf.Min(defaultBid, player.honor);
                    
                    playerHonorBids[player] = defaultBid;
                    playersReadyForBid[player] = true;
                    
                    game.AddMessage("{0} times out and automatically bids {1} honor.", player.name, defaultBid);
                }
            }

            OnAllHonorBidsSubmitted();
        }

        private bool RevealHonorBids()
        {
            if (honorBidsRevealed) return true;

            game.StartCoroutine(RevealHonorBidsCoroutine());
            return true;
        }

        private IEnumerator RevealHonorBidsCoroutine()
        {
            game.AddMessage("=== Revealing Honor Bids ===");
            ExecutePythonScript("on_honor_bids_reveal_start", playerHonorBids);

            yield return new WaitForSeconds(honorRevealDelay);

            // Reveal each player's bid
            foreach (var kvp in playerHonorBids)
            {
                var player = kvp.Key;
                var bid = kvp.Value;
                
                player.honorBid = bid;
                game.AddMessage("{0} bid {1} honor.", player.name, bid);
                
                yield return new WaitForSeconds(0.5f);
            }

            honorBidsRevealed = true;
            OnHonorBidsRevealed?.Invoke(new Dictionary<Player, int>(playerHonorBids));
            ExecutePythonScript("on_honor_bids_revealed", playerHonorBids);

            game.QueueStep(new SimpleStep(game, TransferHonor));
        }
        #endregion

        #region Honor Transfer
        private bool TransferHonor()
        {
            if (honorTransferComplete) return true;

            game.StartCoroutine(TransferHonorCoroutine());
            return true;
        }

        private IEnumerator TransferHonorCoroutine()
        {
            game.AddMessage("=== Transferring Honor ===");
            ExecutePythonScript("on_honor_transfer_start");

            yield return new WaitForSeconds(0.5f);

            foreach (var player in game.GetPlayers())
            {
                int bidAmount = player.honorBid;
                if (bidAmount > 0)
                {
                    int oldHonor = player.honor;
                    player.LoseHonor(bidAmount);
                    int newHonor = player.honor;
                    
                    game.AddMessage("{0} loses {1} honor (from {2} to {3}).", 
                                  player.name, bidAmount, oldHonor, newHonor);
                    
                    OnHonorTransferred?.Invoke(player, oldHonor, newHonor);
                    ExecutePythonScript("on_player_honor_transferred", player, bidAmount, oldHonor, newHonor);
                    
                    yield return new WaitForSeconds(0.3f);
                }
            }

            honorTransferComplete = true;
            ExecutePythonScript("on_honor_transfer_complete");
            
            game.QueueStep(new SimpleStep(game, DrawConflictCards));
        }
        #endregion

        #region Card Drawing
        private bool DrawConflictCards()
        {
            if (cardDrawComplete) return true;

            game.StartCoroutine(DrawConflictCardsCoroutine());
            return true;
        }

        private IEnumerator DrawConflictCardsCoroutine()
        {
            game.AddMessage("=== Drawing Cards ===");
            ExecutePythonScript("on_card_draw_start");

            yield return new WaitForSeconds(cardDrawDelay);

            foreach (var player in game.GetPlayers())
            {
                int cardsToDraw = CalculateCardsToDraw(player);
                
                if (cardsToDraw > 0)
                {
                    game.AddMessage("{0} draws {1} cards for the draw phase.", player.name, cardsToDraw);
                    
                    // Execute card draw action
                    var drawAction = new DrawCardsAction(player, cardsToDraw);
                    var context = game.GetFrameworkContext(player);
                    drawAction.Resolve(player, context);
                    
                    OnCardsDrawn?.Invoke(player, cardsToDraw);
                    ExecutePythonScript("on_player_cards_drawn", player, cardsToDraw);
                }
                else
                {
                    game.AddMessage("{0} draws no cards this phase.", player.name);
                    ExecutePythonScript("on_player_no_cards_drawn", player);
                }
                
                yield return new WaitForSeconds(cardDrawDelay);
            }

            cardDrawComplete = true;
            OnDrawPhaseCardDrawComplete?.Invoke();
            ExecutePythonScript("on_card_draw_complete");

            // Proceed to action window
            var actionWindow = new ActionWindow(game);
            actionWindow.Initialize(game, "Action Window", "draw", null);
            game.QueueStep(actionWindow);
        }

        private int CalculateCardsToDraw(Player player)
        {
            int baseDraw = player.honorBid;
            
            // Apply minimum draw (usually 1 if bid > 0, or 0 if bid = 0)
            int minDraw = (baseDraw == 0 && allowZeroBid) ? 0 : 1;
            
            // Apply effects that modify cards drawn
            int modifierEffects = player.SumEffects(EffectNames.ModifyCardsDrawnInDrawPhase);
            
            int totalDraw = baseDraw + modifierEffects;
            
            // Ensure minimum draw
            totalDraw = Mathf.Max(totalDraw, minDraw);
            
            // Apply maximum draw limits if any
            int maxDraw = player.GetEffectValue(EffectNames.MaxCardsDrawnInDrawPhase, int.MaxValue);
            totalDraw = Mathf.Min(totalDraw, maxDraw);

            return Mathf.Max(0, totalDraw);
        }
        #endregion

        #region Phase Management
        protected override bool EndPhase()
        {
            ExecutePythonScript("on_draw_phase_end");
            game.AddMessage("=== Draw Phase Ends ===");

            // Cleanup
            if (currentHonorBidPrompt != null)
            {
                currentHonorBidPrompt.Cleanup();
                currentHonorBidPrompt = null;
            }

            base.EndPhase();
            return true;
        }

        private bool EndDrawPhase()
        {
            game.QueueStep(new SimpleStep(game, () => { EndPhase(); return true; }));
            return true;
        }
        #endregion

        #region Public Interface
        public Dictionary<Player, int> GetHonorBids()
        {
            return new Dictionary<Player, int>(playerHonorBids);
        }

        public bool AreHonorBidsRevealed()
        {
            return honorBidsRevealed;
        }

        public bool IsHonorTransferComplete()
        {
            return honorTransferComplete;
        }

        public bool IsCardDrawComplete()
        {
            return cardDrawComplete;
        }

        public void ForceRevealHonorBids()
        {
            if (!honorBidsRevealed)
            {
                // Auto-submit any remaining bids
                foreach (var player in game.GetPlayers())
                {
                    if (!playersReadyForBid[player])
                    {
                        int defaultBid = allowZeroBid ? 0 : minHonorBid;
                        playerHonorBids[player] = defaultBid;
                        playersReadyForBid[player] = true;
                    }
                }
                
                OnAllHonorBidsSubmitted();
            }
        }
        #endregion

        #region IronPython Integration
        protected virtual void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                game.ExecutePhaseScript("draw_phase.py", methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for draw phase: {ex.Message}");
            }
        }

        // Phase-specific Python events
        public void OnHonorBidModified(Player player, int originalBid, int modifiedBid)
        {
            ExecutePythonScript("on_honor_bid_modified", player, originalBid, modifiedBid);
        }

        public void OnCardDrawModified(Player player, int originalDraw, int modifiedDraw)
        {
            ExecutePythonScript("on_card_draw_modified", player, originalDraw, modifiedDraw);
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogDrawPhaseStatus()
        {
            Debug.Log($"Draw Phase Status:\n" +
                     $"Honor Bids Revealed: {honorBidsRevealed}\n" +
                     $"Honor Transfer Complete: {honorTransferComplete}\n" +
                     $"Card Draw Complete: {cardDrawComplete}\n" +
                     $"Players Ready: {playersReadyForBid.Values.Count(ready => ready)}/{game.GetPlayers().Count}\n" +
                     $"Current Bids: {string.Join(", ", playerHonorBids.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"))}");
        }
        #endregion
    }

    #region Supporting Classes
    [System.Serializable]
    public class HonorBidOption
    {
        public int bidAmount;
        public string displayText;
        public bool canAfford;
        public bool isRestricted;
    }

    [System.Serializable]
    public class ChoicePrompt
    {
        public string promptText;
        public List<string> options;
        public float timeoutSeconds;
        public System.Action<int> onChoiceMade;
        public System.Action onTimeout;
    }
    #endregion
}