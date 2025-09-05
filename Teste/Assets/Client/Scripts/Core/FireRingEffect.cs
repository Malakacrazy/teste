using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame
{
    /// <summary>
    /// Event-driven implementation of Fire Ring Effect ability.
    /// Allows honoring or dishonoring a character when Fire Ring is resolved.
    /// Uses the event system instead of direct coupling to analytics, UI, and messages.
    /// </summary>
    [Serializable]
    public class FireRingEffect : BaseAbility
    {
        #region Properties
        
        [Header("Fire Ring Configuration")]
        [SerializeField] private bool isOptional = true;
        [SerializeField] private bool allowHonor = true;
        [SerializeField] private bool allowDishonor = true;
        [SerializeField] private bool requireValidTarget = false;
        
        public override string Title => "Fire Ring Effect";
        public override bool CannotTargetFirst => true;
        public override int DefaultPriority => 4;
        
        // Choice constants
        private const string CHOICE_HONOR = "honor";
        private const string CHOICE_DISHONOR = "dishonor";
        private const string CHOICE_BACK = "back";
        private const string CHOICE_DONT_RESOLVE = "dont_resolve";
        
        // Current execution state
        private BaseCard selectedTarget;
        private List<BaseCard> validTargets;
        private IEventBus eventBus;
        
        #endregion
        
        #region Constructor
        
        public FireRingEffect() : this(true) { }
        
        public FireRingEffect(bool optional)
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
            eventBus = gameInstance.GetEventBus();
            
            ConfigureTargeting();
        }
        
        public override bool CanExecute(AbilityContext context)
        {
            // Fire Ring Effect can execute if there are valid character targets
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
            
            if (validTargets.Count == 0 && !isOptional)
            {
                // No valid targets and not optional - cannot execute
                PublishRingResolvedEvent(context, "forced_no_targets", null);
                CompleteExecution(context);
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
                ActivePromptTitle = "Choose character to honor or dishonor",
                Source = "Fire Ring",
                CardTypeFilter = CardTypes.Character,
                AllowCancel = isOptional
            };
            
            SetTargetConfiguration(targetConfig);
        }
        
        /// <summary>
        /// Get valid character targets for honor/dishonor
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
                bool canTarget = false;
                
                // Check if character can be honored
                if (allowHonor && character.CanBeHonored)
                {
                    canTarget = true;
                }
                
                // Check if character can be dishonored
                if (allowDishonor && character.CanBeDishonored)
                {
                    canTarget = true;
                }
                
                if (canTarget)
                {
                    targets.Add(character);
                }
            }
            
            return targets;
        }
        
        /// <summary>
        /// Show target selection UI
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ShowTargetSelection(AbilityContext context)
        {
            if (validTargets.Count == 0)
            {
                // No valid targets - show don't resolve option
                HandleNoValidTargets(context);
                return;
            }
            
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
                title: "Fire Ring Effect",
                description: "Choose character to honor or dishonor:",
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
            var actions = new List<string>();
            
            if (allowHonor && target.CanBeHonored)
            {
                actions.Add("Honor");
            }
            
            if (allowDishonor && target.CanBeDishonored)
            {
                actions.Add("Dishonor");
            }
            
            return $"Available actions: {string.Join(", ", actions)}";
        }
        
        /// <summary>
        /// Handle target selection
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="target">Selected target</param>
        private void HandleTargetSelection(AbilityContext context, BaseCard target)
        {
            selectedTarget = target;
            
            // Show action selection for the selected target
            ShowActionSelection(context);
        }
        
        /// <summary>
        /// Handle target selection cancellation
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void HandleCancelTargetSelection(AbilityContext context)
        {
            if (isOptional)
            {
                PublishRingResolvedEvent(context, "not_resolved", null);
            }
            
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Handle case where no valid targets exist
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void HandleNoValidTargets(AbilityContext context)
        {
            string effectChosen = isOptional ? "no_targets" : "forced_no_targets";
            PublishRingResolvedEvent(context, effectChosen, null);
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Show action selection for the chosen target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ShowActionSelection(AbilityContext context)
        {
            var choices = new List<ActionChoice>();
            
            // Add honor option if available
            if (allowHonor && selectedTarget.CanBeHonored)
            {
                choices.Add(new ActionChoice
                {
                    Text = $"Honor {selectedTarget.Name}",
                    Value = CHOICE_HONOR,
                    Description = "Grant honor status to this character",
                    IsAvailable = true
                });
            }
            
            // Add dishonor option if available
            if (allowDishonor && selectedTarget.CanBeDishonored)
            {
                choices.Add(new ActionChoice
                {
                    Text = $"Dishonor {selectedTarget.Name}",
                    Value = CHOICE_DISHONOR,
                    Description = "Grant dishonor status to this character",
                    IsAvailable = true
                });
            }
            
            // Add back option
            choices.Add(new ActionChoice
            {
                Text = "Back",
                Value = CHOICE_BACK,
                Description = "Choose a different character",
                IsAvailable = true
            });
            
            // Add don't resolve option if optional
            if (isOptional)
            {
                choices.Add(new ActionChoice
                {
                    Text = "Don't resolve the fire ring",
                    Value = CHOICE_DONT_RESOLVE,
                    Description = "Cancel ring effect resolution",
                    IsAvailable = true
                });
            }
            
            // Show choice UI
            var choiceUI = Game.UI.GetChoiceWindow();
            choiceUI.ShowChoices(
                title: "Fire Ring Effect",
                description: $"Choose action for {selectedTarget.Name}:",
                choices: choices.Select(c => c.Text).ToArray(),
                onChoiceSelected: (selectedChoice) => HandleActionSelection(context, choices, selectedChoice),
                allowCancel: false
            );
        }
        
        /// <summary>
        /// Handle action selection
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="choices">Available choices</param>
        /// <param name="selectedChoiceText">Selected choice text</param>
        private void HandleActionSelection(AbilityContext context, List<ActionChoice> choices, string selectedChoiceText)
        {
            var selectedChoice = choices.FirstOrDefault(c => c.Text == selectedChoiceText);
            if (selectedChoice == null)
            {
                Debug.LogWarning($"Unknown choice selected: {selectedChoiceText}");
                return;
            }
            
            switch (selectedChoice.Value)
            {
                case CHOICE_HONOR:
                    ExecuteHonorAction(context);
                    break;
                    
                case CHOICE_DISHONOR:
                    ExecuteDishonorAction(context);
                    break;
                    
                case CHOICE_BACK:
                    // Go back to target selection
                    ShowTargetSelection(context);
                    return;
                    
                case CHOICE_DONT_RESOLVE:
                    ExecuteDontResolve(context);
                    break;
                    
                default:
                    Debug.LogWarning($"Unhandled choice value: {selectedChoice.Value}");
                    break;
            }
            
            // Complete execution for non-back choices
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Execute honor action on selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteHonorAction(AbilityContext context)
        {
            // Store previous honor state
            bool wasAlreadyHonored = selectedTarget.IsHonored;
            
            // Create and execute honor action
            var honorAction = GameActions.CreateHonorAction(selectedTarget);
            honorAction.Resolve(selectedTarget, context);
            
            // Publish character honored event instead of direct analytics/messages
            PublishCharacterHonoredEvent(context, selectedTarget, wasAlreadyHonored);
            
            // Publish ring resolved event
            PublishRingResolvedEvent(context, "honor", selectedTarget);
        }
        
        /// <summary>
        /// Execute dishonor action on selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDishonorAction(AbilityContext context)
        {
            // Store previous dishonor state
            bool wasAlreadyDishonored = selectedTarget.IsDishonored;
            
            // Create and execute dishonor action
            var dishonorAction = GameActions.CreateDishonorAction(selectedTarget);
            dishonorAction.Resolve(selectedTarget, context);
            
            // Publish character dishonored event instead of direct analytics/messages
            PublishCharacterDishonoredEvent(context, selectedTarget, wasAlreadyDishonored);
            
            // Publish ring resolved event
            PublishRingResolvedEvent(context, "dishonor", selectedTarget);
        }
        
        /// <summary>
        /// Execute don't resolve action
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDontResolve(AbilityContext context)
        {
            // Publish ring resolved event for not resolving
            PublishRingResolvedEvent(context, "not_resolved", selectedTarget);
        }
        
        #endregion
        
        #region Event Publishing Methods
        
        /// <summary>
        /// Publish a character honored event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="character">Character that was honored</param>
        /// <param name="wasAlreadyHonored">Was already honored before this effect</param>
        private void PublishCharacterHonoredEvent(AbilityContext context, BaseCard character, bool wasAlreadyHonored)
        {
            try
            {
                if (eventBus == null) return;
                
                var honoredEvent = new CharacterHonoredEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    character: character,
                    wasAlreadyHonored: wasAlreadyHonored,
                    source: this
                );
                
                eventBus.Publish(honoredEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish CharacterHonoredEvent: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish a character dishonored event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="character">Character that was dishonored</param>
        /// <param name="wasAlreadyDishonored">Was already dishonored before this effect</param>
        private void PublishCharacterDishonoredEvent(AbilityContext context, BaseCard character, bool wasAlreadyDishonored)
        {
            try
            {
                if (eventBus == null) return;
                
                var dishonoredEvent = new CharacterDishonoredEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    character: character,
                    wasAlreadyDishonored: wasAlreadyDishonored,
                    source: this
                );
                
                eventBus.Publish(dishonoredEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish CharacterDishonoredEvent: {ex.Message}");
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
                
                // Get the fire ring from the game
                var fireRing = context.Game.rings.TryGetValue("fire", out Ring ring) ? ring : null;
                
                var ringResolvedEvent = new RingResolvedEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    ring: fireRing,
                    effectChosen: effectChosen,
                    effectTarget: target,
                    source: this
                );
                
                eventBus.Publish(ringResolvedEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish RingResolvedEvent: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Advanced Configuration (Preserved from Original)
        
        /// <summary>
        /// Configure which actions are allowed
        /// </summary>
        /// <param name="honor">Allow honor actions</param>
        /// <param name="dishonor">Allow dishonor actions</param>
        public void ConfigureAllowedActions(bool honor, bool dishonor)
        {
            allowHonor = honor;
            allowDishonor = dishonor;
            
            if (!allowHonor && !allowDishonor)
            {
                Debug.LogWarning("Fire Ring Effect: At least one action (honor or dishonor) should be allowed");
                allowHonor = true; // Default fallback
            }
        }
        
        /// <summary>
        /// Get available actions for a specific target
        /// </summary>
        /// <param name="target">Target character</param>
        /// <param name="context">Ability context</param>
        /// <returns>List of available action names</returns>
        public List<string> GetAvailableActionsForTarget(BaseCard target, AbilityContext context)
        {
            var actions = new List<string>();
            
            if (allowHonor && target.CanBeHonored)
            {
                actions.Add("Honor");
            }
            
            if (allowDishonor && target.CanBeDishonored)
            {
                actions.Add("Dishonor");
            }
            
            return actions;
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
            
            // Base value for any action
            value += 2f;
            
            // Higher value for powerful characters
            if (target.Power >= 4)
            {
                value += 2f;
            }
            
            // Higher value for opponent's characters when dishonoring
            if (target.Owner != context.Player && allowDishonor && target.CanBeDishonored)
            {
                value += 3f;
            }
            
            // Higher value for own characters when honoring
            if (target.Owner == context.Player && allowHonor && target.CanBeHonored)
            {
                value += 2f;
            }
            
            // Bonus for characters that are already participating in conflicts
            if (target.IsParticipatingInConflict)
            {
                value += 1f;
            }
            
            return Mathf.Clamp(value, 0f, 10f);
        }
        
        /// <summary>
        /// Get the best target recommendation for a specific action
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="preferredAction">Preferred action (honor/dishonor)</param>
        /// <returns>Recommended target or null</returns>
        public BaseCard GetBestTargetRecommendation(AbilityContext context, string preferredAction = null)
        {
            var targets = GetValidCharacterTargets(context);
            if (targets.Count == 0)
                return null;
            
            // Filter targets by preferred action if specified
            if (!string.IsNullOrEmpty(preferredAction))
            {
                if (preferredAction.ToLower() == "honor")
                {
                    targets = targets.Where(t => t.CanBeHonored).ToList();
                }
                else if (preferredAction.ToLower() == "dishonor")
                {
                    targets = targets.Where(t => t.CanBeDishonored).ToList();
                }
            }
            
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
        /// Get effect impact summary
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Impact summary</returns>
        public FireRingEffectImpactSummary GetEffectImpact(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            var honorTargets = validTargets.Where(t => t.CanBeHonored).ToList();
            var dishonorTargets = validTargets.Where(t => t.CanBeDishonored).ToList();
            var bestHonorTarget = GetBestTargetRecommendation(context, "honor");
            var bestDishonorTarget = GetBestTargetRecommendation(context, "dishonor");
            
            return new FireRingEffectImpactSummary
            {
                ValidTargetsCount = validTargets.Count,
                HonorableTargetsCount = honorTargets.Count,
                DishonorableTargetsCount = dishonorTargets.Count,
                BestHonorTarget = bestHonorTarget,
                BestDishonorTarget = bestDishonorTarget,
                BestHonorValue = bestHonorTarget != null ? GetTargetStrategicValue(bestHonorTarget, context) : 0f,
                BestDishonorValue = bestDishonorTarget != null ? GetTargetStrategicValue(bestDishonorTarget, context) : 0f
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
            if (!allowHonor && !allowDishonor)
            {
                allowHonor = true;
                Debug.LogWarning("Fire Ring Effect: At least one action must be allowed");
            }
        }
        
        /// <summary>
        /// Show effect preview in inspector
        /// </summary>
        [ContextMenu("Show Effect Preview")]
        private void ShowEffectPreview()
        {
            var preview = $"Fire Ring Effect Preview (Event-Driven):\n";
            preview += $"• Allow Honor: {allowHonor}\n";
            preview += $"• Allow Dishonor: {allowDishonor}\n";
            preview += $"• Optional: {isOptional}\n";
            preview += $"• Require Valid Target: {requireValidTarget}\n";
            preview += $"• Uses Event System: YES (decoupled from direct analytics/UI/message calls)";
            
            Debug.Log(preview);
        }
#endif
        
        #endregion
    }
    
    /// <summary>
    /// Summary of fire ring effect impact
    /// </summary>
    [Serializable]
    public class FireRingEffectImpactSummary
    {
        public int ValidTargetsCount;
        public int HonorableTargetsCount;
        public int DishonorableTargetsCount;
        public BaseCard BestHonorTarget;
        public BaseCard BestDishonorTarget;
        public float BestHonorValue;
        public float BestDishonorValue;
        
        public override string ToString()
        {
            var summary = $"Fire Ring Impact: {ValidTargetsCount} valid targets";
            if (HonorableTargetsCount > 0)
            {
                summary += $", {HonorableTargetsCount} can be honored";
            }
            if (DishonorableTargetsCount > 0)
            {
                summary += $", {DishonorableTargetsCount} can be dishonored";
            }
            if (BestHonorTarget != null)
            {
                summary += $", best honor target: {BestHonorTarget.Name} (value: {BestHonorValue:F1})";
            }
            if (BestDishonorTarget != null)
            {
                summary += $", best dishonor target: {BestDishonorTarget.Name} (value: {BestDishonorValue:F1})";
            }
            return summary;
        }
    }
}