using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// C# implementation of Earth Ring Effect ability
    /// Provides card draw and discard mechanics when Earth Ring is resolved
    /// </summary>
    [Serializable]
    public class EarthRingEffect : BaseAbility
    {
        #region Properties
        
        [Header("Earth Ring Configuration")]
        [SerializeField] private bool isOptional = true;
        [SerializeField] public int cardsToDrawPlayer = 1;
        [SerializeField] public int cardsToDiscardOpponent = 1;
        [SerializeField] public bool discardAtRandom = true;
        
        public override string Title => "Earth Ring Effect";
        public override bool CannotTargetFirst => true;
        public override int DefaultPriority => 1;
        
        // Choice constants
        private const string CHOICE_DRAW_AND_DISCARD = "Draw a card and opponent discards";
        private const string CHOICE_DONT_RESOLVE = "Don't resolve";
        
        #endregion
        
        #region Constructor
        
        public EarthRingEffect() : this(true) { }
        
        public EarthRingEffect(bool optional)
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
            // Earth Ring Effect can always be executed when triggered
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
                Source = "Earth Ring",
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
            
            // Always available: Draw card and opponent discards
            choices[CHOICE_DRAW_AND_DISCARD] = context => true;
            
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
                title: "Earth Ring Effect",
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
                case CHOICE_DRAW_AND_DISCARD:
                    ExecuteDrawAndDiscard(context);
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
        /// Execute draw card and opponent discard effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDrawAndDiscard(AbilityContext context)
        {
            var opponent = context.Player.Opponent;
            
            if (opponent != null && opponent.Hand.Count > 0)
            {
                // Both effects: draw and discard
                Game.AddMessage($"{context.Player.Name} resolves the earth ring, drawing a card and forcing {opponent.Name} to discard a card at random");
                
                // Execute draw action for player
                var drawAction = Game.Actions.CreateDrawCardsAction(cardsToDrawPlayer);
                drawAction.Resolve(context.Player, context);
                
                // Execute discard action for opponent
                var discardAction = discardAtRandom 
                    ? Game.Actions.CreateDiscardRandomAction(cardsToDiscardOpponent)
                    : Game.Actions.CreateDiscardAction(cardsToDiscardOpponent);
                    
                discardAction.Resolve(opponent, context);
                
                // Log analytics
                LogDrawAndDiscardAnalytics(context, true);
            }
            else
            {
                // Only draw effect (no opponent or opponent has no cards)
                Game.AddMessage($"{context.Player.Name} resolves the earth ring, drawing a card");
                
                var drawAction = Game.Actions.CreateDrawCardsAction(cardsToDrawPlayer);
                drawAction.Resolve(context.Player, context);
                
                // Log analytics
                LogDrawAndDiscardAnalytics(context, false);
            }
        }
        
        /// <summary>
        /// Execute don't resolve effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        private void ExecuteDontResolve(AbilityContext context)
        {
            Game.AddMessage($"{context.Player.Name} chooses not to resolve the earth ring");
            
            // Log analytics event
            Game.Analytics.LogEvent("earth_ring_not_resolved", new Dictionary<string, object>
            {
                { "player_id", context.Player.PlayerId },
                { "ring_element", "earth" }
            });
        }
        
        /// <summary>
        /// Log analytics for draw and discard actions
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <param name="opponentDiscarded">Whether opponent discarded a card</param>
        private void LogDrawAndDiscardAnalytics(AbilityContext context, bool opponentDiscarded)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "player_id", context.Player.PlayerId },
                { "cards_drawn", cardsToDrawPlayer },
                { "hand_size_after", context.Player.Hand.Count },
                { "opponent_discarded", opponentDiscarded }
            };
            
            if (opponentDiscarded && context.Player.Opponent != null)
            {
                analyticsData.Add("opponent_id", context.Player.Opponent.PlayerId);
                analyticsData.Add("opponent_hand_size_after", context.Player.Opponent.Hand.Count);
                analyticsData.Add("cards_discarded", cardsToDiscardOpponent);
                analyticsData.Add("discard_at_random", discardAtRandom);
            }
            
            Game.Analytics.LogEvent("earth_ring_draw_discard", analyticsData);
        }
        
        #endregion
        
        #region Advanced Configuration
        
        /// <summary>
        /// Configure draw and discard amounts
        /// </summary>
        /// <param name="drawAmount">Number of cards player draws</param>
        /// <param name="discardAmount">Number of cards opponent discards</param>
        /// <param name="randomDiscard">Whether discard is random</param>
        public void ConfigureAmounts(int drawAmount, int discardAmount, bool randomDiscard = true)
        {
            cardsToDrawPlayer = Mathf.Max(0, drawAmount);
            cardsToDiscardOpponent = Mathf.Max(0, discardAmount);
            discardAtRandom = randomDiscard;
        }
        
        /// <summary>
        /// Check if the effect will have full impact (both draw and discard)
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>True if both effects will occur</returns>
        public bool WillHaveFullImpact(AbilityContext context)
        {
            return context.Player.Opponent != null && 
                   context.Player.Opponent.Hand.Count >= cardsToDiscardOpponent &&
                   context.Player.Deck.Count >= cardsToDrawPlayer;
        }
        
        /// <summary>
        /// Get the expected card advantage from this effect
        /// </summary>
        /// <param name="context">Ability execution context</param>
        /// <returns>Net card advantage (+/- cards)</returns>
        public int GetExpectedCardAdvantage(AbilityContext context)
        {
            int advantage = cardsToDrawPlayer; // Player draws
            
            if (context.Player.Opponent != null && context.Player.Opponent.Hand.Count > 0)
            {
                advantage += cardsToDiscardOpponent; // Opponent loses cards
            }
            
            return advantage;
        }
        
        #endregion
        
        #region Unity Inspector Methods
        
#if UNITY_EDITOR
        /// <summary>
        /// Validate configuration in Unity Inspector
        /// </summary>
        public void OnValidate()
        {
            if (cardsToDrawPlayer < 0)
                cardsToDrawPlayer = 0;
                
            if (cardsToDiscardOpponent < 0)
                cardsToDiscardOpponent = 0;
                
            // Ensure at least one card is affected for meaningful effect
            if (cardsToDrawPlayer == 0 && cardsToDiscardOpponent == 0)
            {
                cardsToDrawPlayer = 1;
                Debug.LogWarning("Earth Ring Effect: At least one card must be drawn or discarded");
            }
        }
        
        /// <summary>
        /// Show effect preview in inspector
        /// </summary>
        [ContextMenu("Show Effect Preview")]
        private void ShowEffectPreview()
        {
            var preview = $"Earth Ring Effect Preview:\n";
            preview += $"• Player draws: {cardsToDrawPlayer} card(s)\n";
            preview += $"• Opponent discards: {cardsToDiscardOpponent} card(s)";
            preview += discardAtRandom ? " (random)" : " (choice)";
            preview += $"\n• Optional: {isOptional}";
            
            Debug.Log(preview);
        }
#endif
        
        #endregion
    }
}
