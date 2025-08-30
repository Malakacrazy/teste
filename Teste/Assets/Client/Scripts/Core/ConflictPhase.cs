using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    /// <summary>
    /// ConflictPhase manages the conflict phase of the L5R game, including:
    /// - Conflict declarations and resolution
    /// - Player turn order for conflicts
    /// - Imperial Favor determination through glory count
    /// - Action windows between conflicts
    /// 
    /// Phase Structure:
    /// 3.1 Conflict phase begins -> ACTION WINDOW
    /// 3.2 Next player declares conflict or passes
    /// 3.3 Conflict ends, return to action window
    /// 3.4 Determine Imperial Favor (glory count)
    /// 3.5 Conflict phase ends
    /// </summary>
    public class ConflictPhaseManager : GamePhase
    {
        [Header("Conflict Phase Settings")]
        
        // Phase step management
        protected List<IGameStep> steps;
        public float conflictDeclarationTimeout = 30f;
        public bool enableSkipConflictOption = true;
        public bool autoPassWhenNoValidAttackers = true;

        [Header("Imperial Favor")]
        public bool enableImperialFavorGloryCount = true;
        public float gloryCountDelay = 1f;

        // Current state
        private Player currentPlayer;
        private bool isWaitingForConflictDeclaration = false;
        private int totalConflictOpportunities = 0;
        private int completedConflicts = 0;

        // Action windows and steps
        private ActionWindow preConflictActionWindow;
        private ConflictResolution currentConflictResolution;

        // Events
        public System.Action<Player> OnPlayerTurnToDeclaireConflict;
        public System.Action<Player> OnPlayerPassedConflict;
        public System.Action<Player> OnImperialFavorClaimed;
        public System.Action OnAllConflictsComplete;

        public ConflictPhaseManager(Game game) : base(game, GamePhases.Conflict)
        {
            InitializeConflictPhase();
        }

        #region Phase Initialization
        private void InitializeConflictPhase()
        {
            // Initialize the conflict phase steps
            steps = new List<IGameStep>
            {
                new SimpleStep(game, BeginConflictPhase),
                CreateActionWindow(game, "Pre-Conflict Action Window", "preConflict"),
                new SimpleStep(game, StartConflictChoice)
            };
        }
        #endregion

        #region Phase Execution
        protected override bool StartPhase()
        {
            base.StartPhase();
            
            game.AddMessage("=== Conflict Phase Begins ===");
            ExecutePythonScript("on_conflict_phase_start");
            
            // Reset conflict opportunities for all players
            foreach (var player in game.GetPlayers())
            {
                player.ResetConflictOpportunities();
            }

            totalConflictOpportunities = CalculateTotalConflictOpportunities();
            completedConflicts = 0;
            
            BeginConflictPhase();
            return true;
        }

        private bool BeginConflictPhase()
        {
            currentPlayer = game.GetFirstPlayer();
            
            game.AddMessage("Conflict phase begins. {0} has the first opportunity to declare conflicts.", currentPlayer.name);
            ExecutePythonScript("on_conflict_phase_began", currentPlayer);

            // Execute pre-conflict action window
            game.QueueStep(CreateActionWindow(game, "Action Window", "preConflict"));
            game.QueueStep(new SimpleStep(game, StartConflictChoice));
            return true;
        }

        private bool StartConflictChoice()
        {
            // Check if current player has conflict opportunities
            if (currentPlayer.GetConflictOpportunities() == 0)
            {
                // Switch to opponent if current player has no opportunities
                if (currentPlayer.opponent != null)
                {
                    currentPlayer = currentPlayer.opponent;
                }
            }

            // If current player has opportunities, let them declare or pass
            if (currentPlayer.GetConflictOpportunities() > 0)
            {
                HandleConflictDeclaration();
            }
            else
            {
                // No more conflict opportunities, proceed to Imperial Favor
                game.QueueStep(new SimpleStep(game, ClaimImperialFavor));
            }
            return true;
        }

        private void HandleConflictDeclaration()
        {
            var context = game.GetFrameworkContext(currentPlayer);
            
            // Check if player can actually declare a conflict
            if (CanPlayerDeclareConflict(currentPlayer, context))
            {
                // Player can declare conflict - prompt them
                PromptForConflictDeclaration();
            }
            else
            {
                // Player cannot declare conflict - auto-pass
                if (autoPassWhenNoValidAttackers)
                {
                    var conflict = new Conflict(game, currentPlayer, currentPlayer.opponent, null, null, null);
                    game.AddMessage("{0} passes their conflict opportunity as none of their characters can be declared as an attacker", currentPlayer.name);
                    conflict.PassConflict();
                    
                    OnConflictPassed();
                }
                else
                {
                    PromptForConflictDeclaration(); // Let them manually pass
                }
            }
        }

        private void PromptForConflictDeclaration()
        {
            isWaitingForConflictDeclaration = true;
            OnPlayerTurnToDeclaireConflict?.Invoke(currentPlayer);
            
            game.PromptForAction(currentPlayer, new ConflictDeclarationPrompt
            {
                player = currentPlayer,
                timeoutSeconds = conflictDeclarationTimeout,
                onConflictDeclared = OnConflictDeclared,
                onConflictPassed = OnConflictPassed,
                enableSkipOption = enableSkipConflictOption
            });

            ExecutePythonScript("on_conflict_declaration_prompt", currentPlayer);
        }
        #endregion

        #region Conflict Declaration Handling
        private bool CanPlayerDeclareConflict(Player player, AbilityContext context)
        {
            // Check if player has any valid attackers
            var potentialAttackers = player.GetCharactersInPlay()
                .Where(character => !character.bowed && (character.GetMilitarySkill() > 0 || character.GetPoliticalSkill() > 0))
                .ToList();

            if (potentialAttackers.Count == 0)
                return false;

            // Check if there are any valid provinces to attack
            var validProvinces = player.opponent?.GetProvinces()
                .Where(province => province.CanBeAttacked())
                .ToList();

            return validProvinces?.Count > 0;
        }

        private void OnConflictDeclared(ConflictDeclaration declaration)
        {
            isWaitingForConflictDeclaration = false;
            
            // Create and resolve the conflict
            var conflict = new Conflict(game, currentPlayer, currentPlayer.opponent, null, null, null);
            conflict.DeclareConflict();
            bool success = true;

            if (success)
            {
                currentConflictResolution = new ConflictResolution(game, conflict);
                game.QueueStep(currentConflictResolution);
                
                completedConflicts++;
                currentPlayer.UseConflictOpportunity("military"); // Default to military for now
                
                game.AddMessage("{0} declares a {1} conflict at {2}!", 
                              currentPlayer.name, declaration.conflictType, declaration.targetProvince.name);
                
                ExecutePythonScript("on_conflict_declared", declaration, conflict);
            }
            else
            {
                game.AddMessage("{0}'s conflict declaration was invalid. They must choose again.", currentPlayer.name);
                PromptForConflictDeclaration();
                return;
            }

            // Switch to opponent and continue conflict choice
            if (currentPlayer.opponent != null)
            {
                currentPlayer = currentPlayer.opponent;
            }

            // Queue next action window and conflict choice
            game.QueueStep(CreateActionWindow(game, "Action Window", "preConflict"));
            game.QueueStep(new SimpleStep(game, StartConflictChoice));
        }

        private void OnConflictPassed()
        {
            isWaitingForConflictDeclaration = false;
            
            currentPlayer.UseConflictOpportunity("military"); // Default to military for now
            
            game.AddMessage("{0} passes their conflict opportunity.", currentPlayer.name);
            OnPlayerPassedConflict?.Invoke(currentPlayer);
            ExecutePythonScript("on_conflict_passed", currentPlayer);

            // Switch to opponent and continue conflict choice
            if (currentPlayer.opponent != null)
            {
                currentPlayer = currentPlayer.opponent;
            }

            // Queue next action window and conflict choice
            game.QueueStep(CreateActionWindow(game, "Action Window", "preConflict"));
            game.QueueStep(new SimpleStep(game, StartConflictChoice));
        }
        #endregion

        #region Imperial Favor
        private bool ClaimImperialFavor()
        {
            if (!enableImperialFavorGloryCount)
            {
                EndConflictPhase();
                return true;
            }

            game.AddMessage("=== Determining Imperial Favor ===");
            ExecutePythonScript("on_imperial_favor_determination_start");

            // Perform glory count
            game.StartCoroutine(PerformGloryCountCoroutine());
            return true;
        }

        private IEnumerator PerformGloryCountCoroutine()
        {
            yield return new WaitForSeconds(gloryCountDelay);

            var gloryResult = PerformGloryCount();
            
            if (gloryResult.winner != null)
            {
                ClaimImperialFavorForPlayer(gloryResult.winner);
            }
            else
            {
                game.AddMessage("Glory count results in a tie. Imperial Favor remains unchanged.");
                ExecutePythonScript("on_imperial_favor_tie", gloryResult);
            }

            yield return new WaitForSeconds(gloryCountDelay);
            EndConflictPhase();
        }

        private GloryCountResult PerformGloryCount()
        {
            var result = new GloryCountResult();
            
            foreach (var player in game.GetPlayers())
            {
                int playerGlory = CalculatePlayerGlory(player);
                result.playerGloryTotals[player] = playerGlory;
                
                game.AddMessage("{0} has {1} total glory.", player.name, playerGlory);
            }

            // Determine winner
            var maxGlory = result.playerGloryTotals.Values.Max();
            var playersWithMaxGlory = result.playerGloryTotals
                .Where(kvp => kvp.Value == maxGlory)
                .Select(kvp => kvp.Key)
                .ToList();

            if (playersWithMaxGlory.Count == 1)
            {
                result.winner = playersWithMaxGlory[0];
                result.winningGlory = maxGlory;
            }
            else
            {
                result.isTie = true;
                result.tiedPlayers = playersWithMaxGlory;
            }

            ExecutePythonScript("on_glory_count_completed", result);
            return result;
        }

        private int CalculatePlayerGlory(Player player)
        {
            int totalGlory = 0;

            // Count glory from characters in play
            foreach (var character in player.GetCharactersInPlay())
            {
                totalGlory += character.GetContributionToImperialFavor();
            }

            // Add any bonus glory from effects
            totalGlory += player.SumEffects(EffectNames.ModifyGloryForImperialFavor);

            return totalGlory;
        }

        private void ClaimImperialFavorForPlayer(Player winner)
        {
            var previousHolder = game.GetImperialFavorHolder();
            
            game.SetImperialFavorHolder(winner);
            
            if (previousHolder != winner)
            {
                game.AddMessage("{0} claims the Imperial Favor!", winner.name);
                
                if (previousHolder != null)
                {
                    game.AddMessage("{0} loses the Imperial Favor.", previousHolder.name);
                    ExecutePythonScript("on_imperial_favor_lost", previousHolder);
                }
                
                ExecutePythonScript("on_imperial_favor_claimed", winner, previousHolder);
                OnImperialFavorClaimed?.Invoke(winner);
            }
            else
            {
                game.AddMessage("{0} retains the Imperial Favor.", winner.name);
                ExecutePythonScript("on_imperial_favor_retained", winner);
            }
        }
        #endregion

        #region Phase Management
        protected override bool EndPhase()
        {
            OnAllConflictsComplete?.Invoke();
            ExecutePythonScript("on_conflict_phase_end");
            
            game.AddMessage("=== Conflict Phase Ends ===");
            
            // Clean up any ongoing conflict resolution
            if (currentConflictResolution != null)
            {
                currentConflictResolution.Cleanup();
                currentConflictResolution = null;
            }

            base.EndPhase();
            return true;
        }

        private void EndConflictPhase()
        {
            game.QueueStep(new SimpleStep(game, () => { EndPhase(); return true; }));
        }

        private int CalculateTotalConflictOpportunities()
        {
            return game.GetPlayers().Sum(player => player.GetConflictOpportunities());
        }
        #endregion

        #region Conflict Management
        public bool CanAdvanceToNextPlayer()
        {
            return !isWaitingForConflictDeclaration && currentConflictResolution == null;
        }

        public Player GetCurrentActivePlayer()
        {
            return currentPlayer;
        }

        public int GetRemainingConflictOpportunities()
        {
            return game.GetPlayers().Sum(player => player.GetConflictOpportunities());
        }

        public bool HasMoreConflicts()
        {
            return GetRemainingConflictOpportunities() > 0;
        }

        public void ForceSkipConflict()
        {
            if (isWaitingForConflictDeclaration)
            {
                OnConflictPassed();
            }
        }
        #endregion

        #region IronPython Integration
        protected virtual void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                game.ExecutePhaseScript("conflict_phase.py", methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for conflict phase: {ex.Message}");
            }
        }

        // Phase-specific Python events
        public void OnConflictResolved(Conflict conflict)
        {
            ExecutePythonScript("on_conflict_resolved", conflict);
        }

        public void OnCharacterAssignedToConflict(BaseCard character, string side)
        {
            ExecutePythonScript("on_character_assigned_to_conflict", character, side);
        }

        public void OnProvinceBroken(BaseCard province)
        {
            ExecutePythonScript("on_province_broken", province);
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogConflictPhaseStatus()
        {
            Debug.Log($"Conflict Phase Status:\n" +
                     $"Current Player: {currentPlayer?.name}\n" +
                     $"Waiting for Declaration: {isWaitingForConflictDeclaration}\n" +
                     $"Completed Conflicts: {completedConflicts}\n" +
                     $"Remaining Opportunities: {GetRemainingConflictOpportunities()}\n" +
                     $"Current Conflict: {currentConflictResolution != null}");
        }

        private ActionWindow CreateActionWindow(Game game, string windowName, string windowType)
        {
            var window = new ActionWindow(game);
            window.Initialize(game, windowName, windowType, null);
            return window;
        }
        #endregion
    }

    #region Supporting Classes
    [System.Serializable]
    public class ConflictDeclarationPrompt
    {
        public Player player;
        public float timeoutSeconds;
        public System.Action<ConflictDeclaration> onConflictDeclared;
        public System.Action onConflictPassed;
        public bool enableSkipOption;
    }

    [System.Serializable]
    public class ConflictDeclaration
    {
        public string conflictType; // "military" or "political"
        public BaseCard targetProvince;
        public Ring conflictRing;
        public List<BaseCard> attackers = new List<BaseCard>();
        public Player declaringPlayer;
    }

    [System.Serializable]
    public class GloryCountResult
    {
        public Dictionary<Player, int> playerGloryTotals = new Dictionary<Player, int>();
        public Player winner;
        public int winningGlory;
        public bool isTie = false;
        public List<Player> tiedPlayers = new List<Player>();
    }


    #endregion
}