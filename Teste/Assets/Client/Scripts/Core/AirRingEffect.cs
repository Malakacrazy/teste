using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// C# implementation of Air Ring Effect ability
    /// Provides honor manipulation choices when Air Ring is resolved
    /// </summary>
    [Serializable]
    public class AirRingEffect : BaseAbility
    {
        #region Properties
        
        [Header("Air Ring Configuration")]
        [SerializeField] private bool isOptional = true;
        [SerializeField] private int honorGainAmount = 2;
        [SerializeField] private int honorTakeAmount = 1;
        
        public override string Title => "Air Ring Effect";
        public override bool CannotTargetFirst => true;
        public override int DefaultPriority => 5;
        
        // Choice constants
        private const string CHOICE_GAIN_HONOR = "Gain 2 Honor";
        private const string CHOICE_TAKE_HONOR = "Take 1 Honor from opponent";
        private const string CHOICE_DONT_RESOLVE = "Don't resolve";
        
        #endregion
        
        #region Constructor
        
        public AirRingEffect() : this(true) { }
        
        public AirRingEffect(bool optional)
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
            // Air Ring Effect can always be executed when triggered
            return true;
        }
        
        public override void ExecuteAbility(AbilityContext context)
        {
            // Show choice selection UI
            ShowChoiceSelection(context);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Configure the targeting system for choice selection
        /// </summary>
        private void ConfigureTargeting()
        {
            var targetConfig = new TargetConfiguration
            {
                Mode = TargetModes.Select,
                ActivePromptTitle = "Choose an effect to resolve",
                Source = "Air Ring",
                Choices = GetAvailableChoices(),
                AllowCancel = isOptional
            };
            
            SetTargetConfiguration(targetConfig);
        }
        
        /// <summary>
        /// Get available choices based on game state
        /// </summary>
        /// <returns>Dictionary of choice text to validation functions</returns>
        private Dictionary<string, Func<AbilityContext, bool>> GetAvailableChoices()
        {
            var choices = new Dictionary<string, Func<AbilityContext, bool>>();
            
            // Always available: Gain honor
            choices[CHOICE_GAIN_HONOR] = context => true;
            
            // Available if opponent exists: Take honor from opponent
            choices[CHOICE_TAKE_HONOR] = context => 
                context.Player.Opponent != null && context.Player.Opponent.Honor > 0;
            
            // Available if optional: Don't resolve
            if (isOptional)
            {
                choices[CHOICE_DONT_RESOLVE] = context => true;
            }
            
            return choices;
        }
        
        /// <summary>
        /// Show the choice selection UI
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ShowChoiceSelection(AbilityContext context)
        {
            var choices = GetAvailableChoices();
            var availableChoices = new List<string>();
            
            foreach (var choice in choices)
            {
                if (choice.Value(context))
                {
                    availableChoices.Add(choice.Key);
                }
            }
            
            // Show choice UI using game instance
            if (context.Game != null)
            {
                Debug.Log($"Air Ring Effect: Showing {availableChoices.Count} choices to {context.Player.Name}");
                // For now, just auto-select the first choice as a placeholder
                if (availableChoices.Count > 0)
                {
                    HandleChoiceSelection(context, availableChoices[0]);
                }
            }
        }
        
        /// <summary>
        /// Handle the player's choice selection
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="selectedChoice">The choice selected by the player</param>
        private void HandleChoiceSelection(AbilityContext context, string selectedChoice)
        {
            switch (selectedChoice)
            {
                case CHOICE_GAIN_HONOR:
                    ExecuteGainHonor(context);
                    break;
                    
                case CHOICE_TAKE_HONOR:
                    ExecuteTakeHonor(context);
                    break;
                    
                case CHOICE_DONT_RESOLVE:
                    ExecuteDontResolve(context);
                    break;
                    
                default:
                    Debug.LogWarning($"Unknown choice selected: {selectedChoice}");
                    break;
            }
            
            // Complete ability execution
            CompleteExecution(context);
        }
        
        /// <summary>
        /// Execute gain honor effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteGainHonor(AbilityContext context)
        {
            context.Game.AddMessage($"{context.Player.Name} resolves the air ring, gaining {honorGainAmount} honor");
            
            var gainHonorAction = context.Game.Actions.CreateGainHonorAction(honorGainAmount);
            gainHonorAction.Resolve(context.Player, context);
            
            // Log for analytics
            if (Game.Analytics != null)
            {
                Game.Analytics.LogEvent("air_ring_gain_honor", new Dictionary<string, object>
                {
                    { "player_id", context.Player.PlayerId },
                    { "amount", honorGainAmount },
                    { "total_honor", context.Player.Honor }
                });
            }
        }
        
        /// <summary>
        /// Execute take honor from opponent effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteTakeHonor(AbilityContext context)
        {
            if (context.Player.Opponent == null)
            {
                Debug.LogWarning("No opponent to take honor from");
                return;
            }
            
            context.Game.AddMessage($"{context.Player.Name} resolves the air ring, taking {honorTakeAmount} honor from {context.Player.Opponent.Name}");
            
            var takeHonorAction = context.Game.Actions.CreateTakeHonorAction(honorTakeAmount);
            takeHonorAction.Resolve(context.Player.Opponent, context);
            
            // Log for analytics
            if (Game.Analytics != null)
            {
                Game.Analytics.LogEvent("air_ring_take_honor", new Dictionary<string, object>
                {
                    { "player_id", context.Player.PlayerId },
                    { "opponent_id", context.Player.Opponent.PlayerId },
                    { "amount", honorTakeAmount },
                    { "player_honor", context.Player.Honor },
                    { "opponent_honor", context.Player.Opponent.Honor }
                });
            }
        }
        
        /// <summary>
        /// Execute don't resolve effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDontResolve(AbilityContext context)
        {
            var ringElement = GetCurrentRingElement(context);
            context.Game.AddMessage($"{context.Player.Name} chooses not to resolve the {ringElement} ring");
            
            // Log for analytics
            if (Game.Analytics != null)
            {
                Game.Analytics.LogEvent("air_ring_not_resolved", new Dictionary<string, object>
                {
                    { "player_id", context.Player.PlayerId },
                    { "ring_element", ringElement }
                });
            }
        }
        
        /// <summary>
        /// Get the current ring element being resolved
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>Ring element name</returns>
        private string GetCurrentRingElement(AbilityContext context)
        {
            if (context.Game.CurrentConflict != null)
            {
                return context.Game.CurrentConflict.Element?.ToString().ToLower() ?? "air";
            }
            
            return "air"; // Default to air if no current conflict
        }
        
        #endregion
        
        #region Unity Inspector Methods
        
#if UNITY_EDITOR
        /// <summary>
        /// Validate configuration in Unity Inspector
        /// </summary>
        private void OnValidate()
        {
            if (honorGainAmount < 0)
                honorGainAmount = 0;
                
            if (honorTakeAmount < 0)
                honorTakeAmount = 0;
        }
#endif
        
        #endregion
    }
}
