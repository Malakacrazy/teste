using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame
{
    [System.Serializable]
    public class AIRecommendation
    {
        public string RecommendedAction;
        public float Confidence;
        public string Reasoning;
        public Dictionary<string, object> Parameters;
        
        // Properties for compilation - made settable
        public string Action { get; set; }
        public BaseCard Target { get; set; }
        public float StrategicValue { get; set; }
        
        public AIRecommendation()
        {
            Parameters = new Dictionary<string, object>();
        }
        
        public AIRecommendation(string action, float confidence = 0.5f, string reasoning = "")
        {
            RecommendedAction = action;
            Confidence = confidence;
            Reasoning = reasoning;
            Action = action;
            StrategicValue = confidence * 100f;
            Parameters = new Dictionary<string, object>();
        }
        
        public override string ToString()
        {
            return $"Action: {RecommendedAction}, Confidence: {Confidence:P1}";
        }
    }

    [System.Serializable]
    public class EnhancedTargetData
    {
        public BaseCard Target;
        public string DisplayName;
        public string Description;
        public bool IsValid;
        public float Priority;
        public Dictionary<string, object> Metadata;
        
        // Instance properties for compilation
        public List<string> AvailableActions { get; set; } = new List<string>();
        public float StrategicValue { get; set; }
        public bool IsRecommended { get; set; }
        public string RecommendedAction { get; set; }
        
        public EnhancedTargetData()
        {
            Metadata = new Dictionary<string, object>();
        }
        
        public EnhancedTargetData(BaseCard target, string displayName = "", bool isValid = true)
        {
            Target = target;
            DisplayName = !string.IsNullOrEmpty(displayName) ? displayName : target?.name ?? "Unknown";
            IsValid = isValid;
            Priority = 0.5f;
            Metadata = new Dictionary<string, object>();
        }
        
        public override string ToString()
        {
            return $"{DisplayName} - Valid: {IsValid}, Priority: {Priority}";
        }
    }

    /// <summary>
    /// Bridge class that integrates C# FireRingEffect with IronPython scripting
    /// Provides advanced AI recommendations and fallback to C# implementation
    /// </summary>
    [Serializable]
    public class FireRingEffectBridge : FireRingEffect
    {
        #region Fields

        [Header("Python Integration")]
        [SerializeField] private bool preferPythonImplementation = true;
        [SerializeField] private bool fallbackToCSharp = true;
        [SerializeField] private string pythonScriptName = "fire_ring_effect";

        [Header("AI Integration")]
        [SerializeField] private bool enableAIRecommendations = true;
        [SerializeField] private bool showStrategicValues = true;
        [SerializeField] private float autoExecuteThreshold = 8.0f;
        [SerializeField] private bool highlightRecommendedTargets = true;

        [Header("Advanced Features")]
        [SerializeField] private bool enableTargetPreview = true;
        [SerializeField] private bool logDetailedAnalytics = true;
        [SerializeField] private bool enableSmartDefaults = true;

        private bool pythonScriptLoaded = false;
        private bool pythonExecutionFailed = false;
        private AIRecommendation currentRecommendation;

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
            // Generate AI recommendation if enabled
            if (enableAIRecommendations)
            {
                GenerateAIRecommendation(context);
            }

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
                Debug.LogWarning($"Failed to initialize Python integration for FireRingEffect: {e.Message}");
                pythonExecutionFailed = true;
            }
        }

        /// <summary>
        /// Load the Python script
        /// </summary>
        private void LoadPythonScript()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
        string scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Rings", pythonScriptName + ".py");

            if (PythonManager.Instance.LoadScript(scriptPath))
            {
                pythonScriptLoaded = true;
                
                // Initialize the script instance
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "create_fire_ring_effect", true);
                
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
                "fire_ring_effect.can_execute", 
                context
            );
            
            if (result is bool boolResult)
            {
                return boolResult;
            }
            
            return true; // Default to true if result is not boolean
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
                "execute_fire_ring_effect",
                context
            );
#endif
        }

        /// <summary>
        /// Get valid targets from Python script
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of valid targets</returns>
        public List<BaseCard> GetPythonValidTargets(AbilityContext context)
        {
            var targets = new List<BaseCard>();

#if UNITY_EDITOR || UNITY_STANDALONE
            if (!ShouldUsePythonImplementation())
            {
                return targets;
            }
            
            try
            {
                var result = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "get_valid_targets",
                    context
                );
                
                if (result is IList<object> pythonTargets)
                {
                    foreach (var target in pythonTargets)
                    {
                        if (target is BaseCard card)
                        {
                            targets.Add(card);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HandlePythonError("GetPythonValidTargets", e);
            }
#endif

            return targets;
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

        #region AI Recommendations

        /// <summary>
        /// Generate AI recommendation for target selection
        /// </summary>
        /// <param name="context">Ability context</param>
        private void GenerateAIRecommendation(AbilityContext context)
        {
            try
            {
                currentRecommendation = GetAIRecommendation(context);

                if (currentRecommendation != null && showStrategicValues)
                {
                    Debug.Log($"🤖 AI Recommendation: {currentRecommendation.Action} {currentRecommendation.Target.Name} " +
                             $"(Strategic Value: {currentRecommendation.StrategicValue:F1}/10)");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to generate AI recommendation: {e.Message}");
            }
        }

        /// <summary>
        /// Get AI recommendation from Python or C# implementation
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>AI recommendation data</returns>
        public AIRecommendation GetAIRecommendation(AbilityContext context)
        {
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                try
                {
                    return GetPythonAIRecommendation(context);
                }
                catch (Exception e)
                {
                    HandlePythonError("GetAIRecommendation", e);
                }
            }

            // Fallback to C# implementation
            return GenerateAIRecommendationCSharp(context);
        }

        /// <summary>
        /// Get AI recommendation from Python script
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>AI recommendation</returns>
        private AIRecommendation GetPythonAIRecommendation(AbilityContext context)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var result = PythonManager.Instance.ExecuteFunction(
                pythonScriptName,
                "get_ai_recommendation",
                context
            );
            
            if (result is PythonDictionary recommendationDict)
            {
                var target = recommendationDict.get("target", null) as BaseCard;
                var action = recommendationDict.get("action", "").ToString();
                var value = Convert.ToSingle(recommendationDict.get("value", 0.0));
                
                if (target != null && !string.IsNullOrEmpty(action))
                {
                    return new AIRecommendation
                    {
                        Target = target,
                        Action = action,
                        StrategicValue = value,
                        Reasoning = $"Python AI analysis (Value: {value:F1})"
                    };
                }
            }
#endif

            return null;
        }

        /// <summary>
        /// Generate AI recommendation using C# logic
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>AI recommendation</returns>
        private AIRecommendation GenerateAIRecommendationCSharp(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            if (validTargets.Count == 0)
            {
                return null;
            }

            AIRecommendation bestRecommendation = null;
            float bestValue = -1f;

            foreach (var target in validTargets)
            {
                var availableActions = GetAvailableActionsForTarget(target, context);
                float targetValue = GetTargetStrategicValue(target, context);

                foreach (var action in availableActions)
                {
                    float actionModifier = GetActionModifier(action, target, context);
                    float totalValue = targetValue * actionModifier;

                    if (totalValue > bestValue)
                    {
                        bestValue = totalValue;
                        bestRecommendation = new AIRecommendation
                        {
                            Target = target,
                            Action = action,
                            StrategicValue = totalValue,
                            Reasoning = $"C# AI analysis - {action} {target.Name}"
                        };
                    }
                }
            }

            return bestRecommendation;
        }

        /// <summary>
        /// Get action modifier for strategic calculations
        /// </summary>
        /// <param name="action">Action name</param>
        /// <param name="target">Target character</param>
        /// <param name="context">Ability context</param>
        /// <returns>Modifier value</returns>
        private float GetActionModifier(string action, BaseCard target, AbilityContext context)
        {
            float modifier = 1.0f;

            if (action == "Honor" && target.Owner == context.Player)
            {
                modifier = 1.2f; // Prefer honoring own characters
            }
            else if (action == "Dishonor" && target.Owner != context.Player)
            {
                modifier = 1.3f; // Prefer dishonoring opponent's characters
            }

            return modifier;
        }

        /// <summary>
        /// Check if the ability should be auto-executed based on recommendation
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if should auto-execute</returns>
        public bool ShouldAutoExecute(AbilityContext context)
        {
            if (!enableAIRecommendations || currentRecommendation == null)
                return false;

            return currentRecommendation.StrategicValue >= autoExecuteThreshold;
        }

        #endregion

        #region Enhanced UI Integration

        /// <summary>
        /// Get enhanced target data with strategic information
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Enhanced target selection data</returns>
        public List<EnhancedTargetData> GetEnhancedTargetData(AbilityContext context)
        {
            var targets = GetValidCharacterTargets(context);
            var enhancedData = new List<EnhancedTargetData>();

            foreach (var target in targets)
            {
                var data = new EnhancedTargetData
                {
                    Target = target,
                    DisplayName = target.Name,
                    AvailableActions = GetAvailableActionsForTarget(target, context),
                    StrategicValue = GetTargetStrategicValue(target, context),
                    IsRecommended = currentRecommendation?.Target == target,
                    RecommendedAction = currentRecommendation?.Target == target ? currentRecommendation.Action : null
                };

                // Generate description
                data.Description = GenerateTargetDescription(data);

                enhancedData.Add(data);
            }

            // Sort by strategic value if enabled
            if (showStrategicValues)
            {
                enhancedData = enhancedData.OrderByDescending(d => d.StrategicValue).ToList();
            }

            return enhancedData;
        }

        /// <summary>
        /// Generate description for enhanced target data
        /// </summary>
        /// <param name="data">Target data</param>
        /// <returns>Description string</returns>
        private string GenerateTargetDescription(EnhancedTargetData data)
        {
            var description = $"Actions: {string.Join(", ", data.AvailableActions)}";

            if (showStrategicValues)
            {
                description += $" | Value: {data.StrategicValue:F1}/10";
            }

            if (data.IsRecommended && highlightRecommendedTargets)
            {
                description += $" | ⭐ Recommended: {data.RecommendedAction}";
            }

            return description;
        }

        #endregion

        #region Enhanced Analytics

        /// <summary>
        /// Log enhanced analytics with AI data
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="selectedTarget">Selected target</param>
        /// <param name="selectedAction">Selected action</param>
        /// <param name="wasRecommended">Whether this was the AI recommendation</param>
        protected void LogEnhancedAnalytics(AbilityContext context, BaseCard selectedTarget,
                                          string selectedAction, bool wasRecommended)
        {
            if (!logDetailedAnalytics)
                return;

            var analyticsData = new Dictionary<string, object>
            {
                { "ability_id", AbilityId.FireRing },
                { "player_id", context.Player.PlayerId },
                { "selected_target_id", selectedTarget?.CardId },
                { "selected_action", selectedAction },
                { "was_ai_recommended", wasRecommended },
                { "implementation_used", ShouldUsePythonImplementation() ? "python" : "csharp" },
                { "turn_number", Game.TurnManager.CurrentTurn }
            };

            if (currentRecommendation != null)
            {
                analyticsData.Add("ai_recommended_target", currentRecommendation.Target.CardId);
                analyticsData.Add("ai_recommended_action", currentRecommendation.Action);
                analyticsData.Add("ai_strategic_value", currentRecommendation.StrategicValue);
                analyticsData.Add("player_followed_recommendation", wasRecommended);
            }

            // Add valid targets data
            var validTargets = GetValidCharacterTargets(context);
            analyticsData.Add("valid_targets_count", validTargets.Count);
            analyticsData.Add("valid_target_ids", validTargets.Select(t => t.CardId).ToArray());

            Game.Analytics.LogEvent("fire_ring_effect_enhanced", analyticsData);
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
            currentRecommendation = null;

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

    }
}