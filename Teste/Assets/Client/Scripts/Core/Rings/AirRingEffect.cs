using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Events;

namespace L5RGame
{
    /// <summary>
    /// Event-driven implementation of Air Ring Effect ability
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
                Choices = GetAvailableChoices().Keys.Cast<object>().ToList(),
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
            
            // Show choice UI
            var choiceUI = Game.UI.GetChoiceWindow();
            choiceUI.ShowChoices(
                title: "Air Ring Effect",
                description: "Choose an effect to resolve:",
                choices: availableChoices.ToArray(),
                onChoiceSelected: (selectedChoice) => HandleChoiceSelection(context, selectedChoice),
                allowCancel: isOptional
            );
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
            var eventBus = context.Game.GetEventBus();
            
            var gainHonorAction = GameActions.CreateGainHonorAction(context.Player, honorGainAmount);
            gainHonorAction.Resolve(context.Player, context);
            
            // Publish air ring gain honor event
            eventBus.Publish(new AirRingGainHonorEvent(
                context.Game,
                context.Player,
                honorGainAmount,
                context.Player.Honor,
                this
            ));
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
            
            var eventBus = context.Game.GetEventBus();
            int playerHonorBefore = context.Player.Honor;
            int opponentHonorBefore = context.Player.Opponent.Honor;
            
            var takeHonorAction = GameActions.CreateTakeHonorAction(context.Player, context.Player.Opponent, honorTakeAmount);
            takeHonorAction.Resolve(context.Player.Opponent, context);
            
            // Publish air ring take honor event
            eventBus.Publish(new AirRingTakeHonorEvent(
                context.Game,
                context.Player,
                context.Player.Opponent,
                honorTakeAmount,
                playerHonorBefore,
                context.Player.Honor,
                opponentHonorBefore,
                context.Player.Opponent.Honor,
                this
            ));
        }
        
        /// <summary>
        /// Execute don't resolve effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDontResolve(AbilityContext context)
        {
            var eventBus = context.Game.GetEventBus();
            var ringElement = GetCurrentRingElement(context);
            
            // Publish air ring not resolved event
            eventBus.Publish(new AirRingNotResolvedEvent(
                context.Game,
                context.Player,
                ringElement,
                "player_choice",
                this
            ));
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
        
        #region Advanced Configuration
        
        /// <summary>
        /// Configure honor amounts
        /// </summary>
        /// <param name="gainAmount">Amount of honor to gain</param>
        /// <param name="takeAmount">Amount of honor to take from opponent</param>
        public void ConfigureHonorAmounts(int gainAmount, int takeAmount)
        {
            honorGainAmount = Mathf.Max(0, gainAmount);
            honorTakeAmount = Mathf.Max(0, takeAmount);
        }
        
        /// <summary>
        /// Get the expected honor advantage from gain option
        /// </summary>
        /// <returns>Honor advantage from gain</returns>
        public int GetGainHonorAdvantage()
        {
            return honorGainAmount;
        }
        
        /// <summary>
        /// Get the expected honor advantage from take option
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>Honor advantage from take (includes swing)</returns>
        public int GetTakeHonorAdvantage(AbilityContext context)
        {
            if (context.Player.Opponent == null || context.Player.Opponent.Honor <= 0)
                return 0;
                
            return honorTakeAmount * 2; // Player gains, opponent loses
        }
        
        /// <summary>
        /// Determine optimal choice based on game state
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>Recommended choice</returns>
        public string GetOptimalChoice(AbilityContext context)
        {
            // If opponent has honor and take gives more advantage
            if (context.Player.Opponent != null && 
                context.Player.Opponent.Honor > 0 && 
                GetTakeHonorAdvantage(context) > GetGainHonorAdvantage())
            {
                return CHOICE_TAKE_HONOR;
            }
            
            // Default to gain honor
            return CHOICE_GAIN_HONOR;
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
        
        /// <summary>
        /// Show effect preview in inspector
        /// </summary>
        [ContextMenu("Show Effect Preview")]
        private void ShowEffectPreview()
        {
            var preview = $"Air Ring Effect Preview:\n";
            preview += $"• Gain Honor: {honorGainAmount}\n";
            preview += $"• Take Honor: {honorTakeAmount} (swing: {honorTakeAmount * 2})\n";
            preview += $"• Optional: {isOptional}";
            
            Debug.Log(preview);
        }
#endif
        
        #endregion
    }
}