using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame
{
    /// <summary>
    /// Bridge class that integrates C# EarthRingEffect with IronPython scripting
    /// Provides fallback to C# implementation when Python is unavailable
    /// </summary>
    [Serializable]
    public class EarthRingEffectBridge : EarthRingEffect
    {
        #region Fields
        
        [Header("Python Integration")]
        [SerializeField] private bool preferPythonImplementation = true;
        [SerializeField] private bool fallbackToCSharp = true;
        [SerializeField] private string pythonScriptName = "earth_ring_effect";
        
        [Header("AI Integration")]
        [SerializeField] private bool enableStrategicAnalysis = true;
        [SerializeField] private float strategicValueThreshold = 6.0f;
        
        private bool pythonScriptLoaded = false;
        private bool pythonExecutionFailed = false;
        
        #endregion
        
        #region Constructor
        
        public EarthRingEffectBridge() : this(true) { }
        
        public EarthRingEffectBridge(bool optional) : base(optional)
        {
            // Initialize Python integration if available
            InitializePythonIntegration();
        }
        
        #endregion
        
        #region BaseAbility Override
        
        public override void Initialize(BaseCard sourceCard, Game gameInstance)
        {
            base.Initialize(sourceCard, gameInstance);
            InitializePythonIntegration();
        }
        
        public override bool CanExecute(AbilityContext context)
        {
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                try
                {
                    return ExecutePythonCanExecute(context);
                }
                catch (Exception e)
                {
                    HandlePythonError("CanExecute", e);
                }
            }
            
            // Fallback to C# implementation
            return base.CanExecute(context);
        }
        
        public override void ExecuteAbility(AbilityContext context)
        {
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                try
                {
                    ExecutePythonAbility(context);
                    return;
                }
                catch (Exception e)
                {
                    HandlePythonError("ExecuteAbility", e);
                }
            }
            
            // Fallback to C# implementation
            base.ExecuteAbility(context);
        }
        
        #endregion
        
        #region Python Integration
        
        /// <summary>
        /// Initialize Python integration
        /// </summary>
        private void InitializePythonIntegration()
        {
            if (!preferPythonImplementation || !PythonManager.Instance.IsEnabled)
            {
                return;
            }
            
            try
            {
                LoadPythonScript();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to initialize Python integration for EarthRingEffect: {e.Message}");
                pythonExecutionFailed = true;
            }
        }
        
        /// <summary>
        /// Load the Python script
        /// </summary>
        private void LoadPythonScript()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (PythonManager.Instance.LoadScript(pythonScriptName))
            {
                pythonScriptLoaded = true;
                
                // Initialize the script instance
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "create_earth_ring_effect", true);
                
                Debug.Log($"✅ Python script '{pythonScriptName}' loaded successfully");
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to load Python script '{pythonScriptName}'");
            }
#endif
        }
        
        /// <summary>
        /// Check if Python implementation should be used
        /// </summary>
        /// <returns>True if Python should be used</returns>
        private bool ShouldUsePythonImplementation()
        {
            return preferPythonImplementation && 
                   pythonScriptLoaded && 
                   !pythonExecutionFailed && 
                   PythonManager.Instance.IsEnabled;
        }
        
        /// <summary>
        /// Execute Python CanExecute method
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if ability can execute</returns>
        private bool ExecutePythonCanExecute(AbilityContext context)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var result = PythonManager.Instance.ExecuteFunction(
                pythonScriptName, 
                "earth_ring_effect.can_execute", 
                context
            );
            
            if (result is bool boolResult)
            {
                return boolResult;
            }
            
            // Default to true if result is not boolean
            return true;
#else
            return true;
#endif
        }
        
        /// <summary>
        /// Execute Python ability implementation
        /// </summary>
        /// <param name="context">Ability context</param>
        private void ExecutePythonAbility(AbilityContext context)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            PythonManager.Instance.ExecuteFunction(
                pythonScriptName,
                "execute_earth_ring_effect",
                context
            );
#endif
        }
        
        /// <summary>
        /// Get available choices from Python script
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of available choices</returns>
        public List<ChoiceData> GetPythonChoices(AbilityContext context)
        {
            var choices = new List<ChoiceData>();
            
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!ShouldUsePythonImplementation())
            {
                return choices;
            }
            
            try
            {
                var result = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "get_earth_ring_choices",
                    context
                );
                
                if (result is IList<object> pythonChoices)
                {
                    foreach (var choice in pythonChoices)
                    {
                        if (choice is PythonDictionary choiceDict)
                        {
                            var choiceData = new ChoiceData
                            {
                                Text = choiceDict.get("text", "Unknown Choice").ToString(),
                                Value = choiceDict.get("value", "unknown").ToString(),
                                Available = Convert.ToBoolean(choiceDict.get("available", true)),
                                Description = choiceDict.get("description", "").ToString()
                            };
                            
                            choices.Add(choiceData);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HandlePythonError("GetPythonChoices", e);
            }
#endif
            
            return choices;
        }
        
        /// <summary>
        /// Handle Python execution errors
        /// </summary>
        /// <param name="method">Method that failed</param>
        /// <param name="exception">Exception details</param>
        private void HandlePythonError(string method, Exception exception)
        {
            Debug.LogError($"❌ Python execution failed in {method}: {exception.Message}");
            
            if (fallbackToCSharp)
            {
                Debug.Log("🔄 Falling back to C# implementation");
                pythonExecutionFailed = true;
            }
            else
            {
                throw exception;
            }
        }
        
        #endregion
        
        #region Strategic Analysis
        
        /// <summary>
        /// Get strategic value of executing this ability
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Strategic value score (0-10)</returns>
        public float GetStrategicValue(AbilityContext context)
        {
            if (!enableStrategicAnalysis)
                return 5.0f; // Default neutral value
                
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                try
                {
                    return GetPythonStrategicValue(context);
                }
                catch (Exception e)
                {
                    HandlePythonError("GetStrategicValue", e);
                }
            }
            
            // Fallback to C# strategic analysis
            return CalculateStrategicValueCSharp(context);
        }
        
        /// <summary>
        /// Get strategic value from Python script
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Strategic value score</returns>
        private float GetPythonStrategicValue(AbilityContext context)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var result = PythonManager.Instance.ExecuteFunction(
                pythonScriptName,
                "get_strategic_value",
                context
            );
            
            if (result is float floatResult)
                return floatResult;
            if (result is int intResult)
                return (float)intResult;
                
            return 5.0f; // Default if conversion fails
#else
            return 5.0f;
#endif
        }
        
        /// <summary>
        /// Calculate strategic value using C# logic
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Strategic value score</returns>
        private float CalculateStrategicValueCSharp(AbilityContext context)
        {
            float value = 0;
            
            // Base value for drawing cards
            if (context.Player.Deck.Count > 0)
            {
                value += 3.0f; // Drawing is always beneficial
            }
            
            // Additional value if opponent has cards to discard
            if (context.Player.Opponent != null && context.Player.Opponent.Hand.Count > 0)
            {
                value += 4.0f; // Opponent discard is very valuable
                
                // Extra value if opponent has many cards
                if (context.Player.Opponent.Hand.Count >= 5)
                {
                    value += 2.0f;
                }
            }
            
            // Penalty if player hand is already full
            if (context.Player.Hand.Count >= 6)
            {
                value -= 1.0f;
            }
            
            // Bonus for card advantage
            int expectedAdvantage = GetExpectedCardAdvantage(context);
            value += expectedAdvantage * 0.5f;
            
            return Mathf.Clamp(value, 0f, 10f);
        }
        
        /// <summary>
        /// Check if the ability should be auto-executed based on strategic value
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if should auto-execute</returns>
        public bool ShouldAutoExecute(AbilityContext context)
        {
            if (!enableStrategicAnalysis)
                return false;
                
            float strategicValue = GetStrategicValue(context);
            return strategicValue >= strategicValueThreshold;
        }
        
        #endregion
        
        #region Enhanced Analytics
        
        /// <summary>
        /// Get detailed effect preview for UI
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Effect preview information</returns>
        public EffectPreview GetEffectPreview(AbilityContext context)
        {
            var preview = new EffectPreview
            {
                Title = Title,
                StrategicValue = GetStrategicValue(context),
                CardAdvantage = GetExpectedCardAdvantage(context),
                WillHaveFullImpact = WillHaveFullImpact(context)
            };
            
            // Player effects
            preview.PlayerEffects.Add($"Draw {cardsToDrawPlayer} card(s)");
            
            // Opponent effects
            if (context.Player.Opponent != null && context.Player.Opponent.Hand.Count > 0)
            {
                string discardType = discardAtRandom ? "random" : "chosen";
                preview.OpponentEffects.Add($"Discard {cardsToDiscardOpponent} {discardType} card(s)");
            }
            else
            {
                preview.OpponentEffects.Add("No discard (no opponent or empty hand)");
            }
            
            return preview;
        }
        
        /// <summary>
        /// Log enhanced analytics for the effect
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="choice">Selected choice</param>
        /// <param name="strategicValue">Calculated strategic value</param>
        protected void LogEnhancedAnalytics(AbilityContext context, string choice, float strategicValue)
        {
            var analyticsData = new Dictionary<string, object>
            {
                { "ability_id", AbilityId.EarthRing },
                { "player_id", context.Player.PlayerId },
                { "choice_selected", choice },
                { "strategic_value", strategicValue },
                { "turn_number", Game.TurnManager.CurrentTurn },
                { "player_hand_size", context.Player.Hand.Count },
                { "player_deck_size", context.Player.Deck.Count },
                { "implementation_used", ShouldUsePythonImplementation() ? "python" : "csharp" }
            };
            
            if (context.Player.Opponent != null)
            {
                analyticsData.Add("opponent_id", context.Player.Opponent.PlayerId);
                analyticsData.Add("opponent_hand_size", context.Player.Opponent.Hand.Count);
            }
            
            Game.Analytics.LogEvent("earth_ring_effect_executed", analyticsData);
        }
        
        #endregion
        
        #region Hot Reload Support
        
        /// <summary>
        /// Reload Python script for hot reload during development
        /// </summary>
        [ContextMenu("Reload Python Script")]
        public void ReloadPythonScript()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Python script reload only available during play mode");
                return;
            }
            
            pythonScriptLoaded = false;
            pythonExecutionFailed = false;
            
            try
            {
                // Execute reload function in Python
#if UNITY_EDITOR || UNITY_STANDALONE
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "reload_script");
#endif
                
                // Reinitialize
                InitializePythonIntegration();
                
                Debug.Log("🔄 Python script reloaded successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to reload Python script: {e.Message}");
            }
        }
        
        #endregion
        
        #region Unity Inspector
        
#if UNITY_EDITOR
        /// <summary>
        /// Custom inspector validation
        /// </summary>
        private void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(pythonScriptName))
            {
                pythonScriptName = "earth_ring_effect";
            }
            
            if (strategicValueThreshold < 0)
                strategicValueThreshold = 0;
            if (strategicValueThreshold > 10)
                strategicValueThreshold = 10;
        }
        
        /// <summary>
        /// Draw custom inspector information
        /// </summary>
        [ContextMenu("Show Status")]
        public void ShowStatusInConsole()
        {
            Debug.Log(Game.TurnManager.GetImplementationStatus());
        }
#endif
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for effect preview information
    /// </summary>
    [Serializable]
    public class EffectPreview
    {
        public string Title;
        public float StrategicValue;
        public int CardAdvantage;
        public bool WillHaveFullImpact;
        public List<string> PlayerEffects = new List<string>();
        public List<string> OpponentEffects = new List<string>();
        
        public override string ToString()
        {
            return $"{Title} - Strategic Value: {StrategicValue:F1}, Card Advantage: +{CardAdvantage}";
        }
    }
}
