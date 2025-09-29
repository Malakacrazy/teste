using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame
{
    /// <summary>
    /// Event-driven implementation of Void Ring Effect ability.
    /// Uses the event system instead of direct coupling to analytics, UI, and messages.
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
        private IEventBus eventBus;
        
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
            
            // Get the event bus from the game instance
            eventBus = gameInstance.GetEventBus(); // This method will be added to Game class
            
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
            var allCharacters = context.Game.GetAllCardsInPlay()
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
            // Publish ring resolved event for not resolving
            PublishRingResolvedEvent(context, "not_resolved", null);
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Handle case where no valid targets exist
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void HandleNoValidTargets(AbilityContext context)
        {
            // Publish ring resolved event for no targets
            string effectChosen = isOptional ? "no_targets" : "forced_no_targets";
            PublishRingResolvedEvent(context, effectChosen, null);
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Execute fate removal on the selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Target character</param>
        private void ExecuteRemoveFate(AbilityContext context, BaseCard target)
        {
            // Store original fate count for event
            var originalFate = target.FateTokens;
            
            // Create and execute remove fate action
            var removeFateAction = GameActions.CreateRemoveFateAction(target, fateToRemove);
            removeFateAction.Resolve(target, context);
            
            // Publish fate removed event instead of calling analytics/messages directly
            PublishFateRemovedEvent(context, target, originalFate);
            
            // Publish ring resolved event
            PublishRingResolvedEvent(context, "fate_removed", target);
            
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
            if (target.FateTokens <= 0 && target.Location == "PlayArea")
            {
                var discardAction = GameActions.CreateDiscardAction(target.controller, target);
                discardAction.Resolve(target, context);
                
                // Publish character leaves play event
                PublishCharacterLeavesPlayEvent(context, target, "no fate");
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
                var fateRemovedTriggers = target.GetAbilitiesWithTrigger(EventNames.OnFateLost);
                foreach (var trigger in fateRemovedTriggers)
                {
                    trigger.TryExecute(context);
                }
            }
            
            // No need to trigger direct game events - the event handlers will react to our published events
        }
        
        #endregion
        
        #region Event Publishing Methods
        
        /// <summary>
        /// Publish a fate removed event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="target">Character that lost fate</param>
        /// <param name="originalFate">Fate count before removal</param>
        private void PublishFateRemovedEvent(AbilityContext context, BaseCard target, int originalFate)
        {
            try
            {
                if (eventBus == null) return;
                
                var fateRemovedEvent = new FateRemovedEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    character: target,
                    amountRemoved: fateToRemove,
                    source: this
                );
                
                // Publish as Handler event (during effect resolution)
                PublishHandler(fateRemovedEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish FateRemovedEvent: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish a ring resolved event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="effectChosen">Effect that was chosen</param>
        /// <param name="target">Target of the effect (if any)</param>
        private void PublishRingResolvedEvent(AbilityContext context, string effectChosen, BaseCard target)
        {
            try
            {
                if (eventBus == null) return;
                
                // Get the void ring from the game
                var voidRing = context.Game.rings.TryGetValue("void", out Ring ring) ? ring : null;
                
                var ringResolvedEvent = new RingResolvedEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    ring: voidRing,
                    effectChosen: effectChosen,
                    effectTarget: target,
                    source: this
                );
                
                // Publish as Reaction event (after ring resolution)
                PublishReaction(ringResolvedEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish RingResolvedEvent: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish a character leaves play event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="character">Character leaving play</param>
        /// <param name="reason">Reason for leaving</param>
        private void PublishCharacterLeavesPlayEvent(AbilityContext context, BaseCard character, string reason)
        {
            try
            {
                if (eventBus == null) return;
                
                var characterLeavesEvent = new CharacterLeavesPlayEvent(
                    game: context.Game,
                    character: character,
                    destination: "DiscardPile",
                    reason: reason,
                    source: this
                );
                
                // Publish as Handler event (during character leaving resolution)
                PublishHandler(characterLeavesEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish CharacterLeavesPlayEvent: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Advanced Configuration (Preserved from Original)
        
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
            var preview = $"Void Ring Effect Preview (Event-Driven):\n";
            preview += $"• Fate to Remove: {fateToRemove}\n";
            preview += $"• Allow Own Characters: {allowTargetingOwnCharacters}\n";
            preview += $"• Allow Opponent Characters: {allowTargetingOpponentCharacters}\n";
            preview += $"• Optional: {isOptional}\n";
            preview += $"• Require Valid Target: {requireValidTarget}\n";
            preview += $"• Uses Event System: YES (decoupled from direct analytics/UI/message calls)";
            
            Debug.Log(preview);
        }
#endif
        
        #endregion
    }
    
    // Supporting classes preserved from original for compatibility
    public class FateRemovedEventArgs : EventArgs
    {
        public BaseCard Character;
        public Player Player;
        public BaseAbility Source;
        public int AmountRemoved;
    }
    
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