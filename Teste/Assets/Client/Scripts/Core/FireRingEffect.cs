using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// C# implementation of Fire Ring Effect ability
    /// Allows honoring or dishonoring a character when Fire Ring is resolved
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
                Game.AddMessage($"{context.Player.Name} cannot resolve the fire ring (no valid targets)");
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
            var allCharacters = Game.GameState.GetAllCardsInPlay()
                .Where(card => card.CardType == CardTypes.Character)
                .ToList();
            
            foreach (var character in allCharacters)
            {
                bool canTarget = false;
                
                // Check if character can be honored
                if (allowHonor && character.CanBeHonored(context))
                {
                    canTarget = true;
                }
                
                // Check if character can be dishonored
                if (allowDishonor && character.CanBeDishonored(context))
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
            
            if (allowHonor && target.CanBeHonored(context))
            {
                actions.Add("Honor");
            }
            
            if (allowDishonor && target.CanBeDishonored(context))
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
                Game.AddMessage($"{context.Player.Name} chooses not to resolve the fire ring");
                LogAnalyticsEvent(context, "not_resolved", null, null);
            }
            
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
                Game.AddMessage($"{context.Player.Name} chooses not to resolve the fire ring (no valid targets)");
                LogAnalyticsEvent(context, "no_targets", null, null);
            }
            else
            {
                Game.AddMessage($"{context.Player.Name} cannot resolve the fire ring (no valid targets)");
                LogAnalyticsEvent(context, "forced_no_targets", null, null);
            }
            
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
            if (allowHonor && selectedTarget.CanBeHonored(context))
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
            if (allowDishonor && selectedTarget.CanBeDishonored(context))
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
            Game.AddMessage($"{context.Player.Name} resolves the fire ring, honoring {selectedTarget.Name}");
            
            // Create and execute honor action
            var honorAction = Game.Actions.CreateHonorAction();
            honorAction.Resolve(selectedTarget, context);
            
            // Log analytics
            LogAnalyticsEvent(context, "honor", selectedTarget, "honored");
        }
        
        /// <summary>
        /// Execute dishonor action on selected target
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDishonorAction(AbilityContext context)
        {
            Game.AddMessage($"{context.Player.Name} resolves the fire ring, dishonoring {selectedTarget.Name}");
            
            // Create and execute dishonor action
            var dishonorAction = Game.Actions.CreateDishonorAction();
            dishonorAction.Resolve(selectedTarget, context);
            
            // Log analytics
            LogAnalyticsEvent(context, "dishonor", selectedTarget, "dishonored");
        }
        
        /// <summary>
        /// Execute don't resolve action
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDontResolve(AbilityContext context)
        {
            Game.AddMessage($"{context.Player.Name} chooses not to resolve the fire ring");
            
            // Log analytics
            LogAnalyticsEvent(context, "not_resolved", selectedTarget, null);
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
                { "ring_element", "fire" },
                { "valid_targets_count", validTargets?.Count ?? 0 }
            };
            
            if (target != null)
            {
                analyticsData.Add("target_id", target.CardId);
                analyticsData.Add("target_name", target.Name);
                analyticsData.Add("target_owner", target.Owner.PlayerId);
            }
            
            if (!string.IsNullOrEmpty(result))
            {
                analyticsData.Add("result", result);
            }
            
            Game.Analytics.LogEvent("fire_ring_effect", analyticsData);
        }
        
        #endregion
        
        #region Advanced Configuration
        
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
            
            if (allowHonor && target.CanBeHonored(context))
            {
                actions.Add("Honor");
            }
            
            if (allowDishonor && target.CanBeDishonored(context))
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
            if (target.Owner != context.Player && allowDishonor && target.CanBeDishonored(context))
            {
                value += 3f;
            }
            
            // Higher value for own characters when honoring
            if (target.Owner == context.Player && allowHonor && target.CanBeHonored(context))
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
            var preview = $"Fire Ring Effect Preview:\n";
            preview += $"• Allow Honor: {allowHonor}\n";
            preview += $"• Allow Dishonor: {allowDishonor}\n";
            preview += $"• Optional: {isOptional}\n";
            preview += $"• Require Valid Target: {requireValidTarget}";
            
            Debug.Log(preview);
        }
#endif
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for action choices
    /// </summary>
    [Serializable]
    public class ActionChoice
    {
        public string Text;
        public string Value;
        public string Description;
        public bool IsAvailable;
        
        public override string ToString()
        {
            return $"{Text} ({Value}) - Available: {IsAvailable}";
        }
    }
    
    /// <summary>
    /// Data structure for target selection
    /// </summary>
    [Serializable]
    public class TargetSelectionData
    {
        public BaseCard Target;
        public string DisplayName;
        public string Description;
        public bool IsValid;
        
        public override string ToString()
        {
            return $"{DisplayName} - Valid: {IsValid}";
        }
    }
}
