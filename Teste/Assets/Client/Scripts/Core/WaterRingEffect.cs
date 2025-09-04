using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// C# implementation of Water Ring Effect ability
    /// Allows bowing characters with no fate or readying bowed characters
    /// </summary>
    [Serializable]
    public class WaterRingEffect : BaseAbility
    {
        #region Properties
        
        [Header("Water Ring Configuration")]
        [SerializeField] private bool isOptional = true;
        [SerializeField] private bool requireValidTarget = false;
        [SerializeField] private bool allowTargetingOwnCharacters = true;
        [SerializeField] private bool allowTargetingOpponentCharacters = true;
        [SerializeField] private bool allowBowingReadiedCharacters = true;
        [SerializeField] private bool allowReadyingBowedCharacters = true;
        
        public override string Title => "Water Ring Effect";
        public override bool CannotTargetFirst => true;
        public override int DefaultPriority => 3;
        
        // Current execution state
        private List<BaseCard> validTargets;
        
        #endregion
        
        #region Constructor
        
        public WaterRingEffect() : this(true) { }
        
        public WaterRingEffect(bool optional)
        {
            isOptional = optional;
            
            // Configure targeting parameters
            ConfigureTargeting();
        }
        
        #endregion
        
        #region BaseAbility Implementation
        
        public override void Initialize(BaseCard sourceCard, Game gameInstance)
        {
            base.Initialize(sourceCard, gameInstance);
            ConfigureTargeting();
        }
        
        public override bool CanExecute(AbilityContext context)
        {
            // Water Ring Effect can execute if there are valid character targets
            var targets = GetValidCharacterTargets(context);
            
            if (requireValidTarget && targets.Count == 0)
            {
                return false;
            }
            
            return true;
        }
        
        public override void ExecuteAbility(AbilityContext context)
        {
            // Get valid character targets
            validTargets = GetValidCharacterTargets(context);
            
            if (validTargets.Count == 0)
            {
                // No valid targets
                HandleNoValidTargets(context);
                return;
            }
            
            // Show target selection
            ShowTargetSelection(context);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Configure the targeting system
        /// </summary>
        private void ConfigureTargeting()
        {
            var targetConfig = new TargetConfiguration
            {
                Mode = TargetModes.Select,
                ActivePromptTitle = "Choose character to bow or unbow",
                Source = "Water Ring",
                CardTypeFilter = CardTypes.Character,
                AllowCancel = isOptional
            };
            
            SetTargetConfiguration(targetConfig);
        }
        
        /// <summary>
        /// Get valid character targets for bow/unbow actions
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>List of valid character targets</returns>
        public List<BaseCard> GetValidCharacterTargets(AbilityContext context)
        {
            var targets = new List<BaseCard>();
            
            // Get all characters in play area
            var allCharacters = Game.GameState.GetAllCardsInPlay()
                .Where(card => card.CardType == CardTypes.Character && 
                              card.Location == CardLocation.PlayArea)
                .ToList();
            
            foreach (var character in allCharacters)
            {
                // Check ownership restrictions
                if (character.Owner == context.Player && !allowTargetingOwnCharacters)
                    continue;
                    
                if (character.Owner != context.Player && !allowTargetingOpponentCharacters)
                    continue;
                
                // Check if character is a valid target
                if (IsValidWaterRingTarget(character, context))
                {
                    targets.Add(character);
                }
            }
            
            return targets;
        }
        
        /// <summary>
        /// Check if a character is a valid target for Water Ring Effect
        /// </summary>
        /// <param name="character">Character to check</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if character is a valid target</returns>
        private bool IsValidWaterRingTarget(BaseCard character, AbilityContext context)
        {
            // Character must be in play area
            if (character.Location != CardLocation.PlayArea)
                return false;
            
            // Check for bowed characters that can be readied
            if (character.IsBowed && allowReadyingBowedCharacters)
            {
                return character.AllowGameAction("ready", context);
            }
            
            // Check for readied characters with no fate that can be bowed
            if (!character.IsBowed && allowBowingReadiedCharacters)
            {
                if (character.FateTokens == 0 && character.AllowGameAction("bow", context))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Show target selection UI
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ShowTargetSelection(AbilityContext context)
        {
            // Create target selection data
            var targetData = validTargets.Select(target => new TargetSelectionData
            {
                Target = target,
                DisplayName = target.Name,
                Description = GetTargetDescription(target, context),
                IsValid = true
            }).ToList();
            
            // Show target selection UI
            var targetUI = Game.UI.GetTargetSelectionWindow();
            targetUI.ShowTargetSelection(
                title: "Water Ring Effect",
                description: "Choose character to bow or unbow:",
                targets: targetData.ToArray(),
                onTargetSelected: (selectedTarget) => HandleTargetSelection(context, selectedTarget),
                allowCancel: isOptional,
                onCancel: () => HandleCancelTargetSelection(context)
            );
        }
        
        /// <summary>
        /// Get description for a target character
        /// </summary>
        /// <param name="target">Target character</param>
        /// <param name="context">Ability context</param>
        /// <returns>Description string</returns>
        private string GetTargetDescription(BaseCard target, AbilityContext context)
        {
            var ownerText = target.Owner == context.Player ? "Your" : "Opponent's";
            var actionText = target.IsBowed ? "Ready" : "Bow";
            var statusText = target.IsBowed ? "bowed" : "readied";
            var fateText = target.FateTokens == 0 ? " (no fate)" : $" ({target.FateTokens} fate)";
            
            return $"{ownerText} {statusText} character{fateText} - Will {actionText}";
        }
        
        /// <summary>
        /// Handle target selection
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Selected target</param>
        private void HandleTargetSelection(AbilityContext context, BaseCard target)
        {
            if (target.IsBowed)
            {
                ExecuteReadyAction(context, target);
            }
            else
            {
                ExecuteBowAction(context, target);
            }
            
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Handle target selection cancellation
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void HandleCancelTargetSelection(AbilityContext context)
        {
            Game.AddMessage($"{context.Player.Name} chooses not to resolve the water ring");
            LogAnalyticsEvent(context, "not_resolved", null, null);
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Handle case where no valid targets exist
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void HandleNoValidTargets(AbilityContext context)
        {
            if (isOptional)
            {
                Game.AddMessage($"{context.Player.Name} chooses not to resolve the water ring (no valid targets)");
                LogAnalyticsEvent(context, "no_targets", null, null);
            }
            else
            {
                Game.AddMessage($"{context.Player.Name} cannot resolve the water ring (no valid targets)");
                LogAnalyticsEvent(context, "forced_no_targets", null, null);
            }
            
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Execute ready action on bowed character
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Target character</param>
        private void ExecuteReadyAction(AbilityContext context, BaseCard target)
        {
            Game.AddMessage($"{context.Player.Name} resolves the water ring, readying {target.Name}");
            
            // Create and execute ready action
            var readyAction = Game.Actions.CreateReadyAction();
            readyAction.Resolve(target, context);
            
            // Log analytics
            LogAnalyticsEvent(context, "ready", target, "readied");
            
            // Trigger additional effects
            TriggerReadyEffects(context, target);
        }
        
        /// <summary>
        /// Execute bow action on readied character
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Target character</param>
        private void ExecuteBowAction(AbilityContext context, BaseCard target)
        {
            Game.AddMessage($"{context.Player.Name} resolves the water ring, bowing {target.Name}");
            
            // Create and execute bow action
            var bowAction = Game.Actions.CreateBowAction();
            bowAction.Resolve(target, context);
            
            // Log analytics
            LogAnalyticsEvent(context, "bow", target, "bowed");
            
            // Trigger additional effects
            TriggerBowEffects(context, target);
        }
        
        /// <summary>
        /// Trigger additional effects when character is readied
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="target">Target character</param>
        private void TriggerReadyEffects(AbilityContext context, BaseCard target)
        {
            // Check for ready-based triggers
            if (target.HasAbilities)
            {
                var readyTriggers = target.GetAbilitiesWithTrigger(AbilityTrigger.CharacterReadied);
                foreach (var trigger in readyTriggers)
                {
                    trigger.TryExecute(context);
                }
            }
            
            // Trigger game-wide ready events
            Game.TriggerEvent("character_readied", new CharacterStatusEventArgs
            {
                Character = target,
                Player = context.Player,
                Source = this,
                PreviousStatus = CharacterStatus.Bowed,
                NewStatus = CharacterStatus.Ready
            });
        }
        
        /// <summary>
        /// Trigger additional effects when character is bowed
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="target">Target character</param>
        private void TriggerBowEffects(AbilityContext context, BaseCard target)
        {
            // Check for bow-based triggers
            if (target.HasAbilities)
            {
                var bowTriggers = target.GetAbilitiesWithTrigger(AbilityTrigger.CharacterBowed);
                foreach (var trigger in bowTriggers)
                {
                    trigger.TryExecute(context);
                }
            }
            
            // Trigger game-wide bow events
            Game.TriggerEvent("character_bowed", new CharacterStatusEventArgs
            {
                Character = target,
                Player = context.Player,
                Source = this,
                PreviousStatus = CharacterStatus.Ready,
                NewStatus = CharacterStatus.Bowed
            });
        }
        
        /// <summary>
        /// Log analytics event
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="action">Action taken</param>
        /// <param name="target">Target character (if any)</param>
        /// <param name="result">Result of action</param>
        private void LogAnalyticsEvent(AbilityContext context, string action, BaseCard target, string result)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", context.Player.PlayerId },
                { "action", action },
                { "ring_element", "water" },
                { "valid_targets_count", validTargets?.Count ?? 0 }
            };
            
            if (target != null)
            {
                analyticsData.Add("target_id", target.CardId);
                analyticsData.Add("target_name", target.Name);
                analyticsData.Add("target_owner", target.Owner.PlayerId);
                analyticsData.Add("target_power", target.Power);
                analyticsData.Add("target_fate", target.FateTokens);
                analyticsData.Add("previous_status", target.IsBowed ? "ready" : "bowed");
                analyticsData.Add("new_status", result);
            }
            
            Game.Analytics.LogEvent("water_ring_effect", analyticsData);
        }
        
        #endregion
        
        #region Advanced Configuration
        
        /// <summary>
        /// Configure allowed actions
        /// </summary>
        /// <param name="allowBowing">Allow bowing readied characters</param>
        /// <param name="allowReadying">Allow readying bowed characters</param>
        public void ConfigureAllowedActions(bool allowBowing, bool allowReadying)
        {
            allowBowingReadiedCharacters = allowBowing;
            allowReadyingBowedCharacters = allowReadying;
            
            if (!allowBowing && !allowReadying)
            {
                Debug.LogWarning("Water Ring Effect: At least one action (bow or ready) should be allowed");
                allowReadyingBowedCharacters = true; // Default fallback
            }
        }
        
        /// <summary>
        /// Configure targeting restrictions
        /// </summary>
        /// <param name="allowOwn">Allow targeting own characters</param>
        /// <param name="allowOpponent">Allow targeting opponent characters</param>
        public void ConfigureTargeting(bool allowOwn, bool allowOpponent)
        {
            allowTargetingOwnCharacters = allowOwn;
            allowTargetingOpponentCharacters = allowOpponent;
            
            if (!allowOwn && !allowOpponent)
            {
                Debug.LogWarning("Water Ring Effect: At least one targeting option should be allowed");
                allowTargetingOpponentCharacters = true; // Default fallback
            }
        }
        
        /// <summary>
        /// Get strategic value of targeting a specific character
        /// </summary>
        /// <param name="target">Target character</param>
        /// <param name="context">Ability context</param>
        /// <returns>Strategic value score</returns>
        public float GetTargetStrategicValue(BaseCard target, AbilityContext context)
        {
            float value = 0f;
            
            // Base value for any status change
            value += 2f;
            
            if (target.IsBowed)
            {
                // Readying characters
                if (target.Owner == context.Player)
                {
                    // High value for readying own characters
                    value += 4f;
                    
                    // Extra value for powerful characters
                    if (target.Power >= 4)
                    {
                        value += 2f;
                    }
                    
                    // Extra value if character has useful abilities
                    if (target.HasActionAbilities)
                    {
                        value += 1f;
                    }
                }
                else
                {
                    // Lower value for readying opponent's characters
                    value -= 1f;
                }
            }
            else
            {
                // Bowing characters (only possible if they have no fate)
                if (target.Owner != context.Player)
                {
                    // High value for bowing opponent's characters
                    value += 4f;
                    
                    // Extra value for powerful characters
                    if (target.Power >= 4)
                    {
                        value += 2f;
                    }
                    
                    // Extra value if character is participating in conflicts
                    if (target.IsParticipatingInConflict)
                    {
                        value += 2f;
                    }
                }
                else
                {
                    // Lower value for bowing own characters
                    value -= 2f;
                    
                    // Unless it's strategic (e.g., triggering bow abilities)
                    if (target.HasBowTriggeredAbilities)
                    {
                        value += 3f;
                    }
                }
            }
            
            return Mathf.Clamp(value, 0f, 10f);
        }
        
        /// <summary>
        /// Get the best target recommendation
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Recommended target or null</returns>
        public BaseCard GetBestTargetRecommendation(AbilityContext context)
        {
            var targets = GetValidCharacterTargets(context);
            if (targets.Count == 0)
                return null;
            
            BaseCard bestTarget = null;
            float bestValue = -1f;
            
            foreach (var target in targets)
            {
                float value = GetTargetStrategicValue(target, context);
                if (value > bestValue)
                {
                    bestValue = value;
                    bestTarget = target;
                }
            }
            
            return bestTarget;
        }
        
        /// <summary>
        /// Get characters that can be readied
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of characters that can be readied</returns>
        public List<BaseCard> GetCharactersThatCanBeReadied(AbilityContext context)
        {
            return GetValidCharacterTargets(context)
                .Where(c => c.IsBowed && allowReadyingBowedCharacters)
                .ToList();
        }
        
        /// <summary>
        /// Get characters that can be bowed
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of characters that can be bowed</returns>
        public List<BaseCard> GetCharactersThatCanBeBowed(AbilityContext context)
        {
            return GetValidCharacterTargets(context)
                .Where(c => !c.IsBowed && c.FateTokens == 0 && allowBowingReadiedCharacters)
                .ToList();
        }
        
        /// <summary>
        /// Get effect impact summary
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Impact summary</returns>
        public WaterRingImpactSummary GetEffectImpact(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            var canReady = GetCharactersThatCanBeReadied(context);
            var canBow = GetCharactersThatCanBeBowed(context);
            var bestTarget = GetBestTargetRecommendation(context);
            
            return new WaterRingImpactSummary
            {
                ValidTargetsCount = validTargets.Count,
                CharactersThatCanBeReadied = canReady.Count,
                CharactersThatCanBeBowed = canBow.Count,
                BestTarget = bestTarget,
                BestTargetValue = bestTarget != null ? GetTargetStrategicValue(bestTarget, context) : 0f,
                RecommendedAction = bestTarget?.IsBowed == true ? "Ready" : "Bow"
            };
        }
        
        #endregion
        
        #region Unity Inspector Methods
        
#if UNITY_EDITOR
        /// <summary>
        /// Validate configuration in Unity Inspector
        /// </summary>
        private void OnValidate()
        {
            if (!allowBowingReadiedCharacters && !allowReadyingBowedCharacters)
            {
                allowReadyingBowedCharacters = true;
                Debug.LogWarning("Water Ring Effect: At least one action must be allowed");
            }
            
            if (!allowTargetingOwnCharacters && !allowTargetingOpponentCharacters)
            {
                allowTargetingOpponentCharacters = true;
                Debug.LogWarning("Water Ring Effect: At least one targeting option must be allowed");
            }
        }
        
        /// <summary>
        /// Show effect preview in inspector
        /// </summary>
        [ContextMenu("Show Effect Preview")]
        private void ShowEffectPreview()
        {
            var preview = $"Water Ring Effect Preview:\n";
            preview += $"• Allow Bowing: {allowBowingReadiedCharacters}\n";
            preview += $"• Allow Readying: {allowReadyingBowedCharacters}\n";
            preview += $"• Allow Own Characters: {allowTargetingOwnCharacters}\n";
            preview += $"• Allow Opponent Characters: {allowTargetingOpponentCharacters}\n";
            preview += $"• Optional: {isOptional}\n";
            preview += $"• Require Valid Target: {requireValidTarget}";
            
            Debug.Log(preview);
        }
#endif
        
        #endregion
    }
    
    /// <summary>
    /// Character status enumeration
    /// </summary>
    public enum CharacterStatus
    {
        Ready,
        Bowed
    }
    
    /// <summary>
    /// Event arguments for character status changes
    /// </summary>
    public class CharacterStatusEventArgs : EventArgs
    {
        public BaseCard Character;
        public Player Player;
        public BaseAbility Source;
        public CharacterStatus PreviousStatus;
        public CharacterStatus NewStatus;
    }
    
    /// <summary>
    /// Summary of Water Ring effect impact
    /// </summary>
    [Serializable]
    public class WaterRingImpactSummary
    {
        public int ValidTargetsCount;
        public int CharactersThatCanBeReadied;
        public int CharactersThatCanBeBowed;
        public BaseCard BestTarget;
        public float BestTargetValue;
        public string RecommendedAction;
        
        public override string ToString()
        {
            var summary = $"Water Ring Impact: {ValidTargetsCount} valid targets";
            if (CharactersThatCanBeReadied > 0)
            {
                summary += $", {CharactersThatCanBeReadied} can be readied";
            }
            if (CharactersThatCanBeBowed > 0)
            {
                summary += $", {CharactersThatCanBeBowed} can be bowed";
            }
            if (BestTarget != null)
            {
                summary += $", best target: {RecommendedAction} {BestTarget.Name} (value: {BestTargetValue:F1})";
            }
            return summary;
        }
    }
}
