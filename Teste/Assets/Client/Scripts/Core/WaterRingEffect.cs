using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame
{
    /// <summary>
    /// Event-driven implementation of Water Ring Effect ability.
    /// Allows bowing readied characters or readying bowed characters when Water Ring is resolved.
    /// Uses the event system instead of direct coupling to analytics, UI, and messages.
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
        
        // Choice constants
        private const string CHOICE_BOW = "bow";
        private const string CHOICE_READY = "ready";
        private const string CHOICE_BACK = "back";
        private const string CHOICE_DONT_RESOLVE = "dont_resolve";
        
        // Current execution state
        private BaseCard selectedTarget;
        private List<BaseCard> validTargets;
        private IEventBus eventBus;
        
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
            
            // Get the event bus from the game instance
            eventBus = gameInstance.GetEventBus();
            
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
                ActivePromptTitle = "Choose character to bow or ready",
                Source = "Water Ring",
                CardTypeFilter = CardTypes.Character,
                AllowCancel = isOptional
            };
            
            SetTargetConfiguration(targetConfig);
        }
        
        /// <summary>
        /// Get valid character targets for bow/ready actions
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
                // Check ownership restrictions
                if (character.Owner == context.Player && !allowTargetingOwnCharacters)
                    continue;
                    
                if (character.Owner != context.Player && !allowTargetingOpponentCharacters)
                    continue;
                
                bool canTarget = false;
                
                // Check if character can be bowed (readied characters)
                if (allowBowingReadiedCharacters && !character.isBowed && character.CanBeBowed(context))
                {
                    canTarget = true;
                }
                
                // Check if character can be readied (bowed characters)
                if (allowReadyingBowedCharacters && character.isBowed && character.CanBeReadied(context))
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
                description: "Choose character to bow or ready:",
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
            
            if (allowBowingReadiedCharacters && !target.isBowed && target.CanBeBowed(context))
            {
                actions.Add("Bow");
            }
            
            if (allowReadyingBowedCharacters && target.isBowed && target.CanBeReadied(context))
            {
                actions.Add("Ready");
            }
            
            string statusText = target.isBowed ? "Bowed" : "Ready";
            return $"{statusText} - Available actions: {string.Join(", ", actions)}";
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
            PublishRingResolvedEvent(context, "not_resolved", null);
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
            
            // Add bow option if available
            if (allowBowingReadiedCharacters && !selectedTarget.isBowed && selectedTarget.CanBeBowed(context))
            {
                choices.Add(new ActionChoice
                {
                    Text = $"Bow {selectedTarget.Name}",
                    Value = CHOICE_BOW,
                    Description = "Make this character bowed",
                    IsAvailable = true
                });
            }
            
            // Add ready option if available
            if (allowReadyingBowedCharacters && selectedTarget.isBowed && selectedTarget.CanBeReadied(context))
            {
                choices.Add(new ActionChoice
                {
                    Text = $"Ready {selectedTarget.Name}",
                    Value = CHOICE_READY,
                    Description = "Make this character ready",
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
                    Text = "Don't resolve the water ring",
                    Value = CHOICE_DONT_RESOLVE,
                    Description = "Cancel ring effect resolution",
                    IsAvailable = true
                });
            }
            
            // Show choice UI
            var choiceUI = Game.UI.GetChoiceWindow();
            choiceUI.ShowChoices(
                title: "Water Ring Effect",
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
                case CHOICE_BOW:
                    ExecuteBowAction(context);
                    break;
                    
                case CHOICE_READY:
                    ExecuteReadyAction(context);
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
        /// Execute bow action on selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteBowAction(AbilityContext context)
        {
            // Store previous bow state
            bool wasAlreadyBowed = selectedTarget.isBowed;
            
            // Create and execute bow action
            var bowAction = GameActions.CreateBowAction(selectedTarget);
            bowAction.Resolve(selectedTarget, context);
            
            // Publish character bowed event instead of direct analytics/messages
            PublishCharacterBowedEvent(context, selectedTarget, wasAlreadyBowed, "water ring effect");
            
            // Publish ring resolved event
            PublishRingResolvedEvent(context, "bow", selectedTarget);
        }
        
        /// <summary>
        /// Execute ready action on selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteReadyAction(AbilityContext context)
        {
            // Store previous ready state
            bool wasAlreadyReady = !selectedTarget.isBowed;
            
            // Create and execute ready action
            var readyAction = GameActions.CreateReadyAction(selectedTarget);
            readyAction.Resolve(selectedTarget, context);
            
            // Publish character readied event instead of direct analytics/messages
            PublishCharacterReadiedEvent(context, selectedTarget, wasAlreadyReady, "water ring effect");
            
            // Publish ring resolved event
            PublishRingResolvedEvent(context, "ready", selectedTarget);
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
        /// Publish a character bowed event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="character">Character that was bowed</param>
        /// <param name="wasAlreadyBowed">Was already bowed before this effect</param>
        /// <param name="reason">Reason for bowing</param>
        private void PublishCharacterBowedEvent(AbilityContext context, BaseCard character, bool wasAlreadyBowed, string reason)
        {
            try
            {
                if (eventBus == null) return;
                
                var bowedEvent = new CharacterBowedEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    character: character,
                    wasAlreadyBowed: wasAlreadyBowed,
                    reason: reason,
                    source: this
                );
                
                eventBus.Publish(bowedEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish CharacterBowedEvent: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish a character readied event
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="character">Character that was readied</param>
        /// <param name="wasAlreadyReady">Was already ready before this effect</param>
        /// <param name="reason">Reason for readying</param>
        private void PublishCharacterReadiedEvent(AbilityContext context, BaseCard character, bool wasAlreadyReady, string reason)
        {
            try
            {
                if (eventBus == null) return;
                
                var readiedEvent = new CharacterReadiedEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    character: character,
                    wasAlreadyReady: wasAlreadyReady,
                    reason: reason,
                    source: this
                );
                
                eventBus.Publish(readiedEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish CharacterReadiedEvent: {ex.Message}");
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
                
                // Get the water ring from the game
                var waterRing = context.Game.rings.TryGetValue("water", out Ring ring) ? ring : null;
                
                var ringResolvedEvent = new RingResolvedEvent(
                    game: context.Game,
                    triggeredBy: context.Player,
                    ring: waterRing,
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
        
        #region Advanced Configuration
        
        /// <summary>
        /// Configure which actions are allowed
        /// </summary>
        /// <param name="allowBowing">Allow bowing ready characters</param>
        /// <param name="allowReadying">Allow readying bowed characters</param>
        public void ConfigureAllowedActions(bool allowBowing, bool allowReadying)
        {
            allowBowingReadiedCharacters = allowBowing;
            allowReadyingBowedCharacters = allowReadying;
            
            if (!allowBowing && !allowReadying)
            {
                Debug.LogWarning("Water Ring Effect: At least one action (bow or ready) should be allowed");
                allowBowingReadiedCharacters = true; // Default fallback
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
        /// Get available actions for a specific target
        /// </summary>
        /// <param name="target">Target character</param>
        /// <param name="context">Ability context</param>
        /// <returns>List of available action names</returns>
        public List<string> GetAvailableActionsForTarget(BaseCard target, AbilityContext context)
        {
            var actions = new List<string>();
            
            if (allowBowingReadiedCharacters && !target.isBowed && target.CanBeBowed(context))
            {
                actions.Add("Bow");
            }
            
            if (allowReadyingBowedCharacters && target.isBowed && target.CanBeReadied(context))
            {
                actions.Add("Ready");
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
            
            // Bowing opponent characters is generally valuable
            if (target.Owner != context.Player && !target.isBowed)
            {
                value += 3f;
                
                // Extra value if participating in conflicts
                if (target.IsParticipatingInConflict)
                {
                    value += 2f;
                }
            }
            
            // Readying own characters is valuable
            if (target.Owner == context.Player && target.isBowed)
            {
                value += 2f;
                
                // Extra value for powerful ready characters
                if (target.Power >= 3)
                {
                    value += 1f;
                }
            }
            
            return Mathf.Clamp(value, 0f, 10f);
        }
        
        /// <summary>
        /// Get effect impact summary
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Impact summary</returns>
        public WaterRingEffectImpactSummary GetEffectImpact(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            var bowTargets = validTargets.Where(t => !t.isBowed && t.CanBeBowed(context)).ToList();
            var readyTargets = validTargets.Where(t => t.isBowed && t.CanBeReadied(context)).ToList();
            var ownTargets = validTargets.Where(t => t.Owner == context.Player).ToList();
            var opponentTargets = validTargets.Where(t => t.Owner != context.Player).ToList();
            
            return new WaterRingEffectImpactSummary
            {
                ValidTargetsCount = validTargets.Count,
                BowableTargetsCount = bowTargets.Count,
                ReadyableTargetsCount = readyTargets.Count,
                OwnTargetsCount = ownTargets.Count,
                OpponentTargetsCount = opponentTargets.Count
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
                allowBowingReadiedCharacters = true;
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
            var preview = $"Water Ring Effect Preview (Event-Driven):\n";
            preview += $"• Allow Bowing Ready Characters: {allowBowingReadiedCharacters}\n";
            preview += $"• Allow Readying Bowed Characters: {allowReadyingBowedCharacters}\n";
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
    
    /// <summary>
    /// Summary of water ring effect impact
    /// </summary>
    [Serializable]
    public class WaterRingEffectImpactSummary
    {
        public int ValidTargetsCount;
        public int BowableTargetsCount;
        public int ReadyableTargetsCount;
        public int OwnTargetsCount;
        public int OpponentTargetsCount;
        
        public override string ToString()
        {
            var summary = $"Water Ring Impact: {ValidTargetsCount} valid targets";
            if (BowableTargetsCount > 0)
            {
                summary += $", {BowableTargetsCount} can be bowed";
            }
            if (ReadyableTargetsCount > 0)
            {
                summary += $", {ReadyableTargetsCount} can be readied";
            }
            summary += $" ({OwnTargetsCount} own, {OpponentTargetsCount} opponent)";
            return summary;
        }
    }
}