using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Extensions;

namespace L5RGame
{
    /// <summary>
    /// Core conflict resolution logic for L5R conflicts.
    /// Handles skill calculation, winner determination, and resolution effects.
    /// Works in conjunction with ConflictFlow for the complete conflict pipeline.
    /// </summary>
    [System.Serializable]
    public class ConflictResolution : BaseStep
    {
        [Header("Resolution Configuration")]
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private bool allowManualOverride = false;
        
        // Dependencies
        public Conflict conflict;
        
        public ConflictResolution(Game gameInstance) : base(gameInstance)
        {
        }
        
        // Resolution state
        private int finalAttackerSkill;
        private int finalDefenderSkill;
        private Player resolvedWinner;
        private Player resolvedLoser;
        private int skillDifference;
        private bool isUnopposed;
        private bool resolutionComplete;
        
        // Events for integration
        public event Action<ConflictResolution> OnSkillsCalculated;
        public event Action<ConflictResolution> OnWinnerDetermined;
        public event Action<ConflictResolution> OnResolutionComplete;
        
        #region Properties
        
        public int AttackerSkill => finalAttackerSkill;
        public int DefenderSkill => finalDefenderSkill;
        public Player Winner => resolvedWinner;
        public Player Loser => resolvedLoser;
        public int SkillDifference => skillDifference;
        public bool IsUnopposed => isUnopposed;
        public bool IsResolutionComplete => resolutionComplete;
        public bool HasWinner => resolvedWinner != null;
        public bool IsTie => resolvedWinner == null && resolvedLoser == null && resolutionComplete;
        
        #endregion
        
        #region Constructors
        
        public ConflictResolution(Game game, Conflict conflict) : base(game)
        {
            this.conflict = conflict ?? throw new ArgumentNullException(nameof(conflict));
            
            Initialize();
        }
        
        private void Initialize()
        {
            finalAttackerSkill = 0;
            finalDefenderSkill = 0;
            resolvedWinner = null;
            resolvedLoser = null;
            skillDifference = 0;
            isUnopposed = false;
            resolutionComplete = false;
        }
        
        #endregion
        
        #region Core Resolution Methods
        
        /// <summary>
        /// Execute complete conflict resolution
        /// </summary>
        public ConflictResolutionResult ResolveConflict()
        {
            if (resolutionComplete)
            {
                LogResolution("Conflict already resolved, returning cached result");
                return CreateResult();
            }
            
            LogResolution("Starting conflict resolution");
            
            try
            {
                // Step 1: Calculate final skills
                CalculateSkills();
                
                // Step 2: Determine winner
                DetermineWinner();
                
                // Step 3: Check for unopposed
                CheckUnopposed();
                
                // Step 4: Apply resolution effects
                ApplyResolutionEffects();
                
                // Step 5: Mark complete
                resolutionComplete = true;
                OnResolutionComplete?.Invoke(this);
                
                LogResolution($"Resolution complete - Winner: {Winner?.name ?? "None"}, Skills: {AttackerSkill} vs {DefenderSkill}");
                
                return CreateResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during conflict resolution: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Calculate final skill values for both sides
        /// </summary>
        public void CalculateSkills()
        {
            LogResolution("Calculating final skill values");
            
            // Calculate attacker skill
            finalAttackerSkill = CalculateAttackerSkill();
            
            // Calculate defender skill
            finalDefenderSkill = CalculateDefenderSkill();
            
            // Apply global skill modifiers
            ApplyGlobalSkillModifiers();
            
            // Ensure non-negative values
            finalAttackerSkill = Mathf.Max(0, finalAttackerSkill);
            finalDefenderSkill = Mathf.Max(0, finalDefenderSkill);
            
            LogResolution($"Final skills calculated - Attacker: {finalAttackerSkill}, Defender: {finalDefenderSkill}");
            
            OnSkillsCalculated?.Invoke(this);
        }
        
        /// <summary>
        /// Calculate attacker's total skill
        /// </summary>
        private int CalculateAttackerSkill()
        {
            int baseSkill = 0;
            
            // Sum skill from all attacking characters
            foreach (var attacker in conflict.attackers)
            {
                int characterSkill = GetCharacterSkillForConflict(attacker, conflict.conflictType);
                baseSkill += characterSkill;
                
                LogResolution($"Attacker {attacker.name}: {characterSkill} {conflict.conflictType} skill");
            }
            
            // Apply attacker-specific skill modifiers
            int modifiedSkill = ApplyAttackerSkillModifiers(baseSkill);
            
            LogResolution($"Attacker base skill: {baseSkill}, modified: {modifiedSkill}");
            
            return modifiedSkill;
        }
        
        /// <summary>
        /// Calculate defender's total skill
        /// </summary>
        private int CalculateDefenderSkill()
        {
            if (conflict.defenders.Count == 0)
            {
                LogResolution("No defenders - defender skill is 0");
                return 0;
            }
            
            int baseSkill = 0;
            
            // Sum skill from all defending characters
            foreach (var defender in conflict.defenders)
            {
                int characterSkill = GetCharacterSkillForConflict(defender, conflict.conflictType);
                baseSkill += characterSkill;
                
                LogResolution($"Defender {defender.name}: {characterSkill} {conflict.conflictType} skill");
            }
            
            // Apply defender-specific skill modifiers
            int modifiedSkill = ApplyDefenderSkillModifiers(baseSkill);
            
            LogResolution($"Defender base skill: {baseSkill}, modified: {modifiedSkill}");
            
            return modifiedSkill;
        }
        
        /// <summary>
        /// Get character's skill for specific conflict type
        /// </summary>
        private int GetCharacterSkillForConflict(BaseCard character, string conflictType)
        {
            if (character == null || !character.IsCharacter())
                return 0;
            
            int baseSkill = character.GetSkill(conflictType);
            
            // Apply character-specific skill modifiers (simplified)
            var skillModifiers = 0; // TODO: Implement effect system
            
            // Apply conditional skill bonuses (simplified)
            var conditionalBonuses = 0; // TODO: Implement conditional bonuses
            
            int finalSkill = baseSkill + skillModifiers + conditionalBonuses;
            
            return Mathf.Max(0, finalSkill);
        }
        
        /// <summary>
        /// Apply skill modifiers specific to attackers
        /// </summary>
        private int ApplyAttackerSkillModifiers(int baseSkill)
        {
            // Simplified for now - TODO: Implement full effect system
            return baseSkill;
        }
        
        /// <summary>
        /// Apply skill modifiers specific to defenders
        /// </summary>
        private int ApplyDefenderSkillModifiers(int baseSkill)
        {
            // Simplified for now - TODO: Implement full effect system
            return baseSkill;
        }
        
        /// <summary>
        /// Apply global skill modifiers that affect both sides
        /// </summary>
        private void ApplyGlobalSkillModifiers()
        {
            // Simplified for now - TODO: Implement full effect system
        }
        
        /// <summary>
        /// Determine the winner based on final skills
        /// </summary>
        public void DetermineWinner()
        {
            LogResolution("Determining conflict winner");
            
            if (!resolutionComplete && (finalAttackerSkill == 0 && finalDefenderSkill == 0))
            {
                // Recalculate skills if not done yet
                CalculateSkills();
            }
            
            // Calculate skill difference
            skillDifference = finalAttackerSkill - finalDefenderSkill;
            
            // Determine winner based on skill difference
            if (skillDifference > 0)
            {
                resolvedWinner = conflict.attackingPlayer;
                resolvedLoser = conflict.defendingPlayer;
                LogResolution($"Attacker wins by {skillDifference} skill");
            }
            else if (skillDifference < 0)
            {
                resolvedWinner = conflict.defendingPlayer;
                resolvedLoser = conflict.attackingPlayer;
                skillDifference = Math.Abs(skillDifference);
                LogResolution($"Defender wins by {skillDifference} skill");
            }
            else
            {
                resolvedWinner = null;
                resolvedLoser = null;
                skillDifference = 0;
                LogResolution("Conflict is a tie - no winner");
            }
            
            // Apply winner to conflict object
            ApplyResultToConflict();
            
            OnWinnerDetermined?.Invoke(this);
        }
        
        /// <summary>
        /// Check if conflict is unopposed
        /// </summary>
        private void CheckUnopposed()
        {
            // Check if forced unopposed by effects
            var forcedUnopposed = conflict.GetEffects(EffectNames.ForceConflictUnopposed).Any();
            
            // Check natural unopposed (no defenders and attacker wins)
            var naturalUnopposed = conflict.defenders.Count == 0 && resolvedWinner == conflict.attackingPlayer;
            
            isUnopposed = forcedUnopposed || naturalUnopposed;
            
            if (isUnopposed)
            {
                LogResolution($"Conflict is unopposed ({(forcedUnopposed ? "forced" : "natural")})");
            }
        }
        
        /// <summary>
        /// Apply resolution effects to the conflict
        /// </summary>
        private void ApplyResolutionEffects()
        {
            LogResolution("Applying resolution effects");
            
            // Update conflict with resolution results
            conflict.winnerDetermined = true;
            conflict.conflictUnopposed = isUnopposed;
            
            // Simplified for now - TODO: Implement full effect system
        }
        
        /// <summary>
        /// Apply results to the conflict object
        /// </summary>
        private void ApplyResultToConflict()
        {
            conflict.winner = resolvedWinner;
            conflict.loser = resolvedLoser;
            conflict.winnerSkill = resolvedWinner == conflict.attackingPlayer ? finalAttackerSkill : finalDefenderSkill;
            conflict.loserSkill = resolvedLoser == conflict.attackingPlayer ? finalAttackerSkill : finalDefenderSkill;
            conflict.skillDifference = skillDifference;
            conflict.attackerSkill = finalAttackerSkill;
            conflict.defenderSkill = finalDefenderSkill;
        }
        
        #endregion
        
        #region Manual Override Methods
        
        /// <summary>
        /// Manually set the conflict winner (for manual mode)
        /// </summary>
        public void SetManualWinner(Player winner)
        {
            if (!allowManualOverride)
            {
                Debug.LogWarning("Manual override is not enabled for this conflict resolution");
                return;
            }
            
            LogResolution($"Manual override: Setting winner to {winner?.name ?? "None"}");
            
            resolvedWinner = winner;
            
            if (winner == conflict.attackingPlayer)
            {
                resolvedLoser = conflict.defendingPlayer;
            }
            else if (winner == conflict.defendingPlayer)
            {
                resolvedLoser = conflict.attackingPlayer;
            }
            else
            {
                resolvedLoser = null;
            }
            
            // Recalculate skill difference if needed
            if (resolvedWinner != null && resolvedLoser != null)
            {
                skillDifference = Math.Abs(finalAttackerSkill - finalDefenderSkill);
            }
            else
            {
                skillDifference = 0;
            }
            
            ApplyResultToConflict();
            OnWinnerDetermined?.Invoke(this);
        }
        
        /// <summary>
        /// Enable or disable manual override
        /// </summary>
        public void SetManualOverride(bool enabled)
        {
            allowManualOverride = enabled;
            LogResolution($"Manual override {(enabled ? "enabled" : "disabled")}");
        }
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Check if attacker is the winner
        /// </summary>
        public bool IsAttackerWinner()
        {
            return resolvedWinner == conflict.attackingPlayer;
        }
        
        /// <summary>
        /// Check if defender is the winner
        /// </summary>
        public bool IsDefenderWinner()
        {
            return resolvedWinner == conflict.defendingPlayer;
        }
        
        /// <summary>
        /// Get detailed skill breakdown for debugging
        /// </summary>
        public ConflictSkillBreakdown GetSkillBreakdown()
        {
            return new ConflictSkillBreakdown
            {
                attackerBaseSkill = conflict.attackers.Sum(a => a.GetSkill(conflict.conflictType)),
                attackerModifiers = finalAttackerSkill - conflict.attackers.Sum(a => a.GetSkill(conflict.conflictType)),
                attackerFinalSkill = finalAttackerSkill,
                defenderBaseSkill = conflict.defenders.Sum(d => d.GetSkill(conflict.conflictType)),
                defenderModifiers = finalDefenderSkill - conflict.defenders.Sum(d => d.GetSkill(conflict.conflictType)),
                defenderFinalSkill = finalDefenderSkill,
                skillDifference = skillDifference,
                winner = resolvedWinner?.name ?? "None"
            };
        }
        
        /// <summary>
        /// Create comprehensive result object
        /// </summary>
        private ConflictResolutionResult CreateResult()
        {
            return new ConflictResolutionResult
            {
                winner = resolvedWinner,
                loser = resolvedLoser,
                attackerSkill = finalAttackerSkill,
                defenderSkill = finalDefenderSkill,
                skillDifference = skillDifference,
                isUnopposed = isUnopposed,
                isTie = IsTie,
                resolutionComplete = resolutionComplete,
                conflict = conflict
            };
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Reset resolution state for recalculation
        /// </summary>
        public void Reset()
        {
            LogResolution("Resetting conflict resolution");
            Initialize();
        }
        
        /// <summary>
        /// Log resolution details if enabled
        /// </summary>
        private void LogResolution(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"⚔️ ConflictResolution: {message}");
            }
        }
        
        /// <summary>
        /// Get summary string for debugging
        /// </summary>
        public override string ToString()
        {
            if (!resolutionComplete)
                return "ConflictResolution[Not Resolved]";
                
            return $"ConflictResolution[{resolvedWinner?.name ?? "Tie"} wins {finalAttackerSkill} vs {finalDefenderSkill}]";
        }
        
        #endregion
        
        #region IGameStep Implementation
        
        /// <summary>
        /// Continue processing the conflict resolution
        /// </summary>
        public bool Continue()
        {
            if (!resolutionComplete)
            {
                ResolveConflict();
            }
            return resolutionComplete;
        }
        
        /// <summary>
        /// Check if resolution is complete
        /// </summary>
        public bool IsComplete()
        {
            return resolutionComplete;
        }
        
        /// <summary>
        /// Handle menu commands during resolution
        /// </summary>
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // No menu commands during automatic resolution
        }
        
        /// <summary>
        /// Handle card clicks during resolution
        /// </summary>
        public void OnCardClicked(Player player, BaseCard card)
        {
            // No card interaction during automatic resolution
        }
        
        /// <summary>
        /// Handle ring clicks during resolution
        /// </summary>
        public void OnRingClicked(Player player, Ring ring)
        {
            // No ring interaction during automatic resolution
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            // No cleanup needed
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create and immediately resolve a conflict
        /// </summary>
        public static ConflictResolutionResult QuickResolve(Game game, Conflict conflict)
        {
            var resolution = new ConflictResolution(game, conflict);
            return resolution.ResolveConflict();
        }
        
        /// <summary>
        /// Create resolution with manual override enabled
        /// </summary>
        public static ConflictResolution CreateManual(Game game, Conflict conflict)
        {
            var resolution = new ConflictResolution(game, conflict);
            resolution.SetManualOverride(true);
            return resolution;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Result of conflict resolution
    /// </summary>
    [System.Serializable]
    public class ConflictResolutionResult
    {
        public Player winner;
        public Player loser;
        public int attackerSkill;
        public int defenderSkill;
        public int skillDifference;
        public bool isUnopposed;
        public bool isTie;
        public bool resolutionComplete;
        public Conflict conflict;
        
        public bool HasWinner => winner != null;
        public bool IsAttackerWinner => winner != null && winner == conflict?.attackingPlayer;
        public bool IsDefenderWinner => winner != null && winner == conflict?.defendingPlayer;
    }
    
    /// <summary>
    /// Detailed skill breakdown for analysis
    /// </summary>
    [System.Serializable]
    public class ConflictSkillBreakdown
    {
        public int attackerBaseSkill;
        public int attackerModifiers;
        public int attackerFinalSkill;
        public int defenderBaseSkill;
        public int defenderModifiers;
        public int defenderFinalSkill;
        public int skillDifference;
        public string winner;
        
        public override string ToString()
        {
            return $"Attacker: {attackerBaseSkill}+{attackerModifiers}={attackerFinalSkill}, " +
                   $"Defender: {defenderBaseSkill}+{defenderModifiers}={defenderFinalSkill}, " +
                   $"Winner: {winner}";
        }
    }
    
    /// <summary>
    /// Extension methods for ConflictResolution integration
    /// </summary>
    public static class ConflictResolutionExtensions
    {
        /// <summary>
        /// Create and attach a ConflictResolution to this conflict
        /// </summary>
        public static ConflictResolution CreateResolution(this Conflict conflict, Game game)
        {
            return new ConflictResolution(game, conflict);
        }
        
        /// <summary>
        /// Quick resolve this conflict
        /// </summary>
        public static ConflictResolutionResult QuickResolve(this Conflict conflict, Game game)
        {
            return ConflictResolution.QuickResolve(game, conflict);
        }
        
        /// <summary>
        /// Check if this conflict has been resolved
        /// </summary>
        public static bool IsResolved(this Conflict conflict)
        {
            return conflict.winnerDetermined;
        }
        
        /// <summary>
        /// Get skill for conflict type with null safety
        /// </summary>
        public static int GetSkillSafe(this BaseCard card, string conflictType)
        {
            if (card == null || string.IsNullOrEmpty(conflictType))
                return 0;
                
            return card.GetSkill(conflictType);
        }
        
        /// <summary>
        /// Get skill based on conflict type
        /// </summary>
        public static int GetSkill(this BaseCard card, string conflictType)
        {
            if (card == null || string.IsNullOrEmpty(conflictType))
                return 0;
                
            return conflictType.ToLower() == "military" ? card.GetMilitarySkill() : card.GetPoliticalSkill();
        }
        
        /// <summary>
        /// Check if card is a character
        /// </summary>
        public static bool IsCharacter(this BaseCard card)
        {
            return card != null && card.GetCardType() == CardTypes.Character;
        }
        
        /// <summary>
        /// Check if card can participate in this conflict type
        /// </summary>
        public static bool CanParticipateInConflictType(this BaseCard card, string conflictType)
        {
            if (!card.IsCharacter())
                return false;
                
            // Characters with 0 skill can still participate (for abilities, etc.)
            return card.GetSkillSafe(conflictType) >= 0;
        }
        
        /// <summary>
        /// Get total skill for a list of characters
        /// </summary>
        public static int GetTotalSkill(this IEnumerable<BaseCard> characters, string conflictType)
        {
            return characters?.Sum(c => c.GetSkillSafe(conflictType)) ?? 0;
        }
    }
    
    /// <summary>
    /// Event arguments for conflict resolution events
    /// </summary>
    public class ConflictResolutionEventArgs : EventArgs
    {
        public ConflictResolution Resolution { get; }
        public Conflict Conflict { get; }
        public ConflictResolutionResult Result { get; }
        
        public ConflictResolutionEventArgs(ConflictResolution resolution, ConflictResolutionResult result)
        {
            Resolution = resolution;
            Conflict = resolution.conflict;
            Result = result;
        }
    }
    
    /// <summary>
    /// Helper class for testing conflict resolutions
    /// </summary>
    public static class ConflictResolutionTestHelper
    {
        /// <summary>
        /// Create a test conflict with specified participants
        /// </summary>
        public static Conflict CreateTestConflict(Game game, Player attacker, Player defender, 
            string conflictType, List<BaseCard> attackers = null, List<BaseCard> defenders = null)
        {
            var conflict = new Conflict(game, attacker, defender, null, null, null);
            
            if (attackers != null)
            {
                conflict.attackers.AddRange(attackers);
            }
            
            if (defenders != null)
            {
                conflict.defenders.AddRange(defenders);
            }
            
            return conflict;
        }
        
        /// <summary>
        /// Test resolution with specific skill values
        /// </summary>
        public static ConflictResolutionResult TestResolution(Game game, int attackerSkill, int defenderSkill)
        {
            var attacker = game.GetPlayers().First();
            var defender = game.GetPlayers().Last();
            
            var conflict = CreateTestConflict(game, attacker, defender, "military");
            
            // Mock the skill calculation
            var resolution = new ConflictResolution(game, conflict);
            
            // This would require exposing some internal methods for testing
            // or creating a test-specific subclass
            
            return resolution.ResolveConflict();
        }
    }
}