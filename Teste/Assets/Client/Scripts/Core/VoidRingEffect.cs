using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// C# implementation of Void Ring Effect ability
    /// Allows removing fate from a character when Void Ring is resolved
    /// </summary>
    [Serializable]
    public class VoidRingEffect : BaseAbility
    {
        #region Properties
        
        [Header("Void Ring Configuration")]
        [SerializeField] private bool isOptional = true;
        [SerializeField] public int fateToRemove = 1;
        [SerializeField] private bool requireValidTarget = false;
        [SerializeField] private bool allowTargetingOwnCharacters = true;
        [SerializeField] private bool allowTargetingOpponentCharacters = true;
        
        public override string Title => "Void Ring Effect";
        public override bool CannotTargetFirst => true;
        public override int DefaultPriority => 2;
        
        // Current execution state
        private List<BaseCard> validTargets;
        
        #endregion
        
        #region Constructor
        
        public VoidRingEffect() : this(true) { }
        
        public VoidRingEffect(bool optional)
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
            // Void Ring Effect can execute if there are valid character targets with fate
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
                ActivePromptTitle = "Choose character to remove fate from",
                Source = "Void Ring",
                CardTypeFilter = CardTypes.Character,
                AllowCancel = isOptional
            };
            
            SetTargetConfiguration(targetConfig);
        }
        
        /// <summary>
        /// Get valid character targets for fate removal
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>List of valid character targets</returns>
        public List<BaseCard> GetValidCharacterTargets(AbilityContext context)
        {
            var targets = new List<BaseCard>();
            
            // Get all characters in play
            var allCharacters = Game.GameState.GetAllCardsInPlay()
                .Where(card => card.CardType == CardTypes.Character)
                .ToList();
            
            foreach (var character in allCharacters)
            {
                // Check if character has fate to remove
                if (!CanRemoveFateFrom(character, context))
                    continue;
                
                // Check ownership restrictions
                if (character.Owner == context.Player && !allowTargetingOwnCharacters)
                    continue;
                    
                if (character.Owner != context.Player && !allowTargetingOpponentCharacters)
                    continue;
                
                targets.Add(character);
            }
            
            return targets;
        }
        
        /// <summary>
        /// Check if fate can be removed from a character
        /// </summary>
        /// <param name="character">Character to check</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if fate can be removed</returns>
        private bool CanRemoveFateFrom(BaseCard character, AbilityContext context)
        {
            // Character must have fate tokens
            if (character.FateTokens < fateToRemove)
                return false;
            
            // Character must allow the removeFate game action
            if (character.AllowGameAction("removeFate", context))
                return true;
            
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
                title: "Void Ring Effect",
                description: "Choose character to remove fate from:",
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
            var fateCount = target.FateTokens;
            var ownerText = target.Owner == context.Player ? "Your" : "Opponent's";
            
            return $"{ownerText} character with {fateCount} fate token{(fateCount != 1 ? "s" : "")}";
        }
        
        /// <summary>
        /// Handle target selection
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Selected target</param>
        private void HandleTargetSelection(AbilityContext context, BaseCard target)
        {
            ExecuteRemoveFate(context, target);
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Handle target selection cancellation
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void HandleCancelTargetSelection(AbilityContext context)
        {
            Game.AddMessage($"{context.Player.Name} chooses not to resolve the void ring");
            LogAnalyticsEvent(context, "not_resolved", null);
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
                Game.AddMessage($"{context.Player.Name} chooses not to resolve the void ring (no valid targets)");
                LogAnalyticsEvent(context, "no_targets", null);
            }
            else
            {
                Game.AddMessage($"{context.Player.Name} cannot resolve the void ring (no valid targets)");
                LogAnalyticsEvent(context, "forced_no_targets", null);
            }
            
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Execute fate removal on the selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Target character</param>
        private void ExecuteRemoveFate(AbilityContext context, BaseCard target)
        {
            Game.AddMessage($"{context.Player.Name} resolves the void ring, removing a fate from {target.Name}");
            
            // Create and execute remove fate action
            var removeFateAction = Game.Actions.CreateRemoveFateAction(fateToRemove);
            removeFateAction.Resolve(target, context);
            
            // Log analytics
            LogAnalyticsEvent(context, "fate_removed", target);
            
            // Check for character leaving play
            CheckCharacterLeaving(target, context);
            
            // Trigger additional effects
            TriggerFateRemovalEffects(context, target);
        }
        
        /// <summary>
        /// Check if character should leave play due to no fate
        /// </summary>
        /// <param name="target">Target character</param>
        /// <param name="context">Ability context</param>
        private void CheckCharacterLeaving(BaseCard target, AbilityContext context)
        {
            if (target.FateTokens <= 0 && target.Location == CardLocation.PlayArea)
            {
                // Character should leave play
                Game.AddMessage($"{target.Name} leaves play due to having no fate");
                
                var discardAction = Game.Actions.CreateDiscardAction();
                discardAction.Resolve(target, context);
                
                // Log character leaving
                LogAnalyticsEvent(context, "character_left_play", target);
            }
        }
        
        /// <summary>
        /// Trigger additional effects when fate is removed
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="target">Target character</param>
        private void TriggerFateRemovalEffects(AbilityContext context, BaseCard target)
        {
            // Check for fate-based triggers
            if (target.HasAbilities)
            {
                var fateRemovedTriggers = target.GetAbilitiesWithTrigger(AbilityTrigger.FateRemoved);
                foreach (var trigger in fateRemovedTriggers)
                {
                    trigger.TryExecute(context);
                }
            }
            
            // Trigger game-wide fate removal events
            Game.TriggerEvent("fate_removed", new FateRemovedEventArgs
            {
                Character = target,
                Player = context.Player,
                Source = this,
                AmountRemoved = fateToRemove
            });
        }
        
        /// <summary>
        /// Log analytics event
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="action">Action taken</param>
        /// <param name="target">Target character (if any)</param>
        private void LogAnalyticsEvent(AbilityContext context, string action, BaseCard target)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", context.Player.PlayerId },
                { "action", action },
                { "ring_element", "void" },
                { "valid_targets_count", validTargets?.Count ?? 0 },
                { "fate_to_remove", fateToRemove }
            };
            
            if (target != null)
            {
                analyticsData.Add("target_id", target.CardId);
                analyticsData.Add("target_name", target.Name);
                analyticsData.Add("target_owner", target.Owner.PlayerId);
                analyticsData.Add("target_fate_before", target.FateTokens + fateToRemove);
                analyticsData.Add("target_fate_after", target.FateTokens);
                analyticsData.Add("target_will_leave", target.FateTokens <= 0);
            }
            
            Game.Analytics.LogEvent("void_ring_effect", analyticsData);
        }
        
        #endregion
        
        #region Advanced Configuration
        
        /// <summary>
        /// Configure fate removal amount
        /// </summary>
        /// <param name="amount">Amount of fate to remove</param>
        public void ConfigureFateRemoval(int amount)
        {
            fateToRemove = Mathf.Max(1, amount);
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
                Debug.LogWarning("Void Ring Effect: At least one targeting option should be allowed");
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
            
            // Base value for any fate removal
            value += 2f;
            
            // Higher value for opponent's characters
            if (target.Owner != context.Player)
            {
                value += 3f;
                
                // Extra value for powerful opponent characters
                if (target.Power >= 4)
                {
                    value += 2f;
                }
                
                // Extra value if it will cause character to leave
                if (target.FateTokens <= fateToRemove)
                {
                    value += 4f; // Removing a character is very valuable
                }
            }
            else
            {
                // Lower value for own characters (usually not desired)
                value -= 2f;
                
                // Unless it's strategic (e.g., triggering leave-play effects)
                if (target.HasLeavesPlayAbilities)
                {
                    value += 3f;
                }
            }
            
            // Bonus for characters participating in conflicts
            if (target.IsParticipatingInConflict)
            {
                value += 1f;
            }
            
            // Bonus based on fate count (more fate = more valuable to remove)
            value += target.FateTokens * 0.5f;
            
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
        /// Check if the effect will cause any characters to leave play
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of characters that would leave play</returns>
        public List<BaseCard> GetCharactersThatWouldLeave(AbilityContext context)
        {
            var targets = GetValidCharacterTargets(context);
            return targets.Where(t => t.FateTokens <= fateToRemove).ToList();
        }
        
        /// <summary>
        /// Get effect impact summary
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Impact summary</returns>
        public EffectImpactSummary GetEffectImpact(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            var charactersLeaving = GetCharactersThatWouldLeave(context);
            var bestTarget = GetBestTargetRecommendation(context);
            
            return new EffectImpactSummary
            {
                ValidTargetsCount = validTargets.Count,
                CharactersLeavingCount = charactersLeaving.Count,
                BestTarget = bestTarget,
                BestTargetValue = bestTarget != null ? GetTargetStrategicValue(bestTarget, context) : 0f,
                TotalFateWouldRemove = validTargets.Count * fateToRemove
            };
        }
        
        #endregion
        
        #region Unity Inspector Methods
        
#if UNITY_EDITOR
        /// <summary>
        /// Validate configuration in Unity Inspector
        /// </summary>
        public void OnValidate()
        {
            if (fateToRemove < 1)
                fateToRemove = 1;
                
            if (!allowTargetingOwnCharacters && !allowTargetingOpponentCharacters)
            {
                allowTargetingOpponentCharacters = true;
                Debug.LogWarning("Void Ring Effect: At least one targeting option must be allowed");
            }
        }
        
        /// <summary>
        /// Show effect preview in inspector
        /// </summary>
        [ContextMenu("Show Effect Preview")]
        private void ShowEffectPreview()
        {
            var preview = $"Void Ring Effect Preview:\n";
            preview += $"• Fate to Remove: {fateToRemove}\n";
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
    /// Event arguments for fate removed events
    /// </summary>
    public class FateRemovedEventArgs : EventArgs
    {
        public BaseCard Character;
        public Player Player;
        public BaseAbility Source;
        public int AmountRemoved;
    }
    
    /// <summary>
    /// Summary of effect impact
    /// </summary>
    [Serializable]
    public class EffectImpactSummary
    {
        public int ValidTargetsCount;
        public int CharactersLeavingCount;
        public BaseCard BestTarget;
        public float BestTargetValue;
        public int TotalFateWouldRemove;
        
        public override string ToString()
        {
            var summary = $"Void Ring Impact: {ValidTargetsCount} valid targets";
            if (CharactersLeavingCount > 0)
            {
                summary += $", {CharactersLeavingCount} would leave play";
            }
            if (BestTarget != null)
            {
                summary += $", best target: {BestTarget.Name} (value: {BestTargetValue:F1})";
            }
            return summary;
        }
    }
}
