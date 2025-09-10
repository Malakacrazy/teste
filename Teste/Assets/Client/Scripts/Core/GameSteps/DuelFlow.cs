using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Manages the complete duel resolution pipeline following L5R rules.
    /// Handles the entire flow from duel declaration through cleanup.
    /// 
    /// Duel Timing:
    /// D.1 Duel begins.
    /// D.2 Establish challenger and challengee.
    /// D.3 Duel honor bid.
    /// D.4 Reveal honor dials.
    /// D.5 Transfer honor.
    /// D.6 Modify dueling skill.
    /// D.7 Compare skill value and determine results.
    /// D.8 Apply duel results.
    /// D.9 Duel ends.
    /// </summary>
    public class DuelFlow : BaseStepWithPipeline
    {
        [Header("Duel Flow Properties")]
        public Duel duel;
        public System.Action<object> costHandler;
        public System.Action<Duel> resolutionHandler;
        
        [Header("Debug Information")]
        [SerializeField] private bool enableDuelLogging = true;
        [SerializeField] private bool showHonorBidDetails = true;
        
        #region Constructor
        
        public DuelFlow(Game game, Duel duel, System.Action<object> costHandler, System.Action<Duel> resolutionHandler) 
            : base(game, "DuelFlow")
        {
            this.duel = duel ?? throw new ArgumentNullException(nameof(duel));
            this.costHandler = costHandler ?? throw new ArgumentNullException(nameof(costHandler));
            this.resolutionHandler = resolutionHandler ?? throw new ArgumentNullException(nameof(resolutionHandler));
            
            InitializeDuelPipeline();
        }
        
        #endregion
        
        #region Pipeline Initialization
        
        /// <summary>
        /// Initialize the duel resolution pipeline
        /// </summary>
        public override void Initialize()
        {
            if (enableDuelLogging)
            {
                Debug.Log($"🗡️ Initializing DuelFlow pipeline for duel: {duel.GetType().Name}");
            }
            
            pipeline.Initialize(new List<IGameStep>
            {
                new SimpleStep(game, SetCurrentDuel, "SetCurrentDuel"),
                new SimpleStep(game, PromptForHonorBid, "PromptForHonorBid"),
                new SimpleStep(game, ModifyDuelingSkill, "ModifyDuelingSkill"),
                new SimpleStep(game, DetermineResults, "DetermineResults"),
                new SimpleStep(game, AnnounceResult, "AnnounceResult"),
                new SimpleStep(game, ApplyDuelResults, "ApplyDuelResults"),
                new SimpleStep(game, CleanUpDuel, "CleanUpDuel"),
                new SimpleStep(game, () => { game.CheckGameState(true); return true; }, "CheckGameState")
            });
        }
        
        /// <summary>
        /// Initialize the duel pipeline system
        /// </summary>
        private void InitializeDuelPipeline()
        {
            Initialize();
            
            if (enableDuelLogging)
            {
                Debug.Log($"🗡️ DuelFlow pipeline initialized with {pipeline.Length} steps");
            }
        }
        
        #endregion
        
        #region Duel Pipeline Steps
        
        /// <summary>
        /// D.1 - Set current duel and establish context
        /// </summary>
        public bool SetCurrentDuel()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ D.1 - Setting current duel");
            }
            
            duel.previousDuel = game.currentDuel;
            game.currentDuel = duel;
            game.CheckGameState(true);
            
            return true;
        }
        
        /// <summary>
        /// D.3 - Prompt players for honor bid
        /// </summary>
        public bool PromptForHonorBid()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ D.3 - Prompting for honor bid");
            }
            
            var prohibitedBids = new Dictionary<string, List<object>>();
            
            foreach (var player in game.GetPlayers())
            {
                var cannotBidEffects = player.GetEffects("CannotBidInDuels");
                prohibitedBids[player.name] = cannotBidEffects.Distinct().ToList();
            }
            
            string promptMessage = $"Choose your bid for the duel\n{duel.GetTotalsForDisplay()}";
            
            if (showHonorBidDetails)
            {
                Debug.Log($"🗡️ Honor bid prompt: {promptMessage}");
            }
            
            // Simplified implementation - in a real game this would prompt for honor bid
            UnityEngine.Debug.Log($"Prompting for honor bid: {promptMessage}");
            
            return true;
        }
        
        /// <summary>
        /// D.6 - Modify dueling skill values
        /// </summary>
        public bool ModifyDuelingSkill()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ D.6 - Modifying dueling skill");
            }
            
            duel.ModifyDuelingSkill();
            
            return true;
        }
        
        /// <summary>
        /// D.7 - Compare skill values and determine results
        /// </summary>
        public bool DetermineResults()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ D.7 - Determining duel results");
            }
            
            duel.DetermineResult();
            
            return true;
        }
        
        /// <summary>
        /// Announce the duel result to all players
        /// </summary>
        public bool AnnounceResult()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ Announcing duel result");
            }
            
            game.AddMessage(duel.GetTotalsForDisplay());
            
            if (duel.winner == null)
            {
                game.AddMessage("The duel ends in a draw");
                
                if (enableDuelLogging)
                {
                    Debug.Log("🗡️ Duel ended in a draw");
                }
            }
            else if (enableDuelLogging)
            {
                Debug.Log($"🗡️ Duel winner: {(duel.winner as BaseCard)?.name ?? "Unknown"}");
            }
            
            // Raise AfterDuel event
            var eventParameters = new Dictionary<string, object>
            {
                ["duel"] = duel,
                ["winner"] = duel.winner,
                ["loser"] = duel.loser
            };
            
            game.RaiseEvent("AfterDuel", eventParameters);
            
            return true;
        }
        
        /// <summary>
        /// D.8 - Apply duel results
        /// </summary>
        public bool ApplyDuelResults()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ D.8 - Applying duel results");
            }
            
            var eventParameters = new Dictionary<string, object>
            {
                ["duel"] = duel
            };
            
            game.RaiseEvent("OnDuelResolution", eventParameters, () => 
            {
                resolutionHandler?.Invoke(duel);
                return true;
            });
            
            return true;
        }
        
        /// <summary>
        /// D.9 - Clean up duel and restore previous state
        /// </summary>
        public bool CleanUpDuel()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ D.9 - Cleaning up duel");
            }
            
            game.currentDuel = duel.previousDuel;
            
            var eventParameters = new Dictionary<string, object>
            {
                ["duel"] = duel
            };
            
            game.RaiseEvent("OnDuelFinished", eventParameters);
            
            return true;
        }
        
        #endregion
        
        #region Unity Lifecycle and Debug
        
        /// <summary>
        /// Get debug information about the current duel flow state
        /// </summary>
        public override string GetDebugInfo()
        {
            var baseDebugInfo = base.GetDebugInfo();
            
            return $"{baseDebugInfo}\n" +
                   $"Duel Type: {duel?.GetType().Name ?? "None"}\n" +
                   $"Winner: {(duel?.winner as BaseCard)?.name ?? "None"}\n" +
                   $"Loser: {(duel?.loser as BaseCard)?.name ?? "None"}\n" +
                   $"Previous Duel: {duel?.previousDuel?.GetType().Name ?? "None"}\n" +
                   $"Totals Display: {duel?.GetTotalsForDisplay() ?? "N/A"}\n" +
                   $"Cost Handler: {(costHandler != null ? "Yes" : "No")}\n" +
                   $"Resolution Handler: {(resolutionHandler != null ? "Yes" : "No")}\n" +
                   $"Duel Logging: {enableDuelLogging}\n" +
                   $"Show Honor Bid Details: {showHonorBidDetails}";
        }
        
        /// <summary>
        /// Clean up when destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (enableDuelLogging)
            {
                Debug.Log("🗡️ DuelFlow destroyed");
            }
            
            // Clean up handlers
            costHandler = null;
            resolutionHandler = null;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a duel flow with default handlers
        /// </summary>
        public static DuelFlow CreateDefault(Game game, Duel duel)
        {
            return new DuelFlow(
                game, 
                duel, 
                (cost) => Debug.Log($"🗡️ Default cost handler: {cost}"),
                (duelResult) => Debug.Log($"🗡️ Default resolution handler: {duelResult}")
            );
        }
        
        /// <summary>
        /// Create a duel flow with custom cost handler
        /// </summary>
        public static DuelFlow CreateWithCostHandler(Game game, Duel duel, System.Action<object> costHandler)
        {
            return new DuelFlow(
                game, 
                duel, 
                costHandler,
                (duelResult) => Debug.Log($"🗡️ Default resolution handler: {duelResult}")
            );
        }
        
        /// <summary>
        /// Create a duel flow with custom resolution handler
        /// </summary>
        public static DuelFlow CreateWithResolutionHandler(Game game, Duel duel, System.Action<Duel> resolutionHandler)
        {
            return new DuelFlow(
                game, 
                duel, 
                (cost) => Debug.Log($"🗡️ Default cost handler: {cost}"),
                resolutionHandler
            );
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for DuelFlow
    /// </summary>
    public static class DuelFlowExtensions
    {
        /// <summary>
        /// Check if duel flow is currently processing
        /// </summary>
        public static bool IsProcessingDuel(this DuelFlow duelFlow)
        {
            return duelFlow != null && duelFlow.HasStarted && !duelFlow.IsComplete;
        }
        
        /// <summary>
        /// Get the current duel step name
        /// </summary>
        public static string GetCurrentDuelStep(this DuelFlow duelFlow)
        {
            return duelFlow?.CurrentSubStep?.StepName ?? "None";
        }
        
        /// <summary>
        /// Get duel completion percentage
        /// </summary>
        public static float GetDuelProgress(this DuelFlow duelFlow)
        {
            return duelFlow?.PipelineProgress ?? 0f;
        }
    }
}
