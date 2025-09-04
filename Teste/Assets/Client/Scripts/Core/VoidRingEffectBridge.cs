using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame
{
    /// <summary>
    /// Bridge class that integrates C# VoidRingEffect with IronPython scripting
    /// Provides advanced fate removal analysis and strategic recommendations
    /// </summary>
    [Serializable]
    public class VoidRingEffectBridge : VoidRingEffect
    {
        #region Fields
        
        [Header("Python Integration")]
        [SerializeField] private bool preferPythonImplementation = true;
        [SerializeField] private bool fallbackToCSharp = true;
        [SerializeField] private string pythonScriptName = "void_ring_effect";
        
        [Header("AI Integration")]
        [SerializeField] private bool enableAIRecommendations = true;
        [SerializeField] private bool showStrategicValues = true;
        [SerializeField] private float autoExecuteThreshold = 7.0f;
        [SerializeField] private bool highlightCharactersLeaving = true;
        
        [Header("Advanced Analytics")]
        [SerializeField] private bool enableOutcomeSimulation = true;
        [SerializeField] private bool logDetailedAnalytics = true;
        [SerializeField] private bool trackFateEconomy = true;
        [SerializeField] private bool enableThreatAssessment = true;
        
        private bool pythonScriptLoaded = false;
        private bool pythonExecutionFailed = false;
        private VoidRingRecommendation currentRecommendation;
        private List<EffectOutcome> simulatedOutcomes;
        
        #endregion
        
        #region Constructor
        
        public VoidRingEffectBridge() : this(true) { }
        
        public VoidRingEffectBridge(bool optional) : base(optional)
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
            // Generate AI recommendation if enabled
            if (enableAIRecommendations)
            {
                GenerateAIRecommendation(context);
            }
            
            // Simulate outcomes if enabled
            if (enableOutcomeSimulation)
            {
                SimulateEffectOutcomes(context);
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
                Debug.LogWarning($"Failed to initialize Python integration for VoidRingEffect: {e.Message}");
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
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "create_void_ring_effect", true);
                
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
                "void_ring_effect.can_execute", 
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
                "execute_void_ring_effect",
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
                    Debug.Log($"🤖 AI Recommendation: Target {currentRecommendation.Target.Name} " +
                             $"(Strategic Value: {currentRecommendation.StrategicValue:F1}/10)");
                    Debug.Log($"💡 Reasoning: {currentRecommendation.Reasoning}");
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
        public VoidRingRecommendation GetAIRecommendation(AbilityContext context)
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
        private VoidRingRecommendation GetPythonAIRecommendation(AbilityContext context)
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
                var value = Convert.ToSingle(recommendationDict.get("value", 0.0));
                var reasoning = recommendationDict.get("reasoning", "").ToString();
                
                if (target != null)
                {
                    return new VoidRingRecommendation
                    {
                        Target = target,
                        StrategicValue = value,
                        Reasoning = reasoning,
                        WillCauseLeaving = target.FateTokens <= fateToRemove,
                        Source = "Python AI"
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
        private VoidRingRecommendation GenerateAIRecommendationCSharp(AbilityContext context)
        {
            var bestTarget = GetBestTargetRecommendation(context);
            if (bestTarget == null)
            {
                return null;
            }
            
            var strategicValue = GetTargetStrategicValue(bestTarget, context);
            var reasoning = GenerateReasoningCSharp(bestTarget, context);
            
            return new VoidRingRecommendation
            {
                Target = bestTarget,
                StrategicValue = strategicValue,
                Reasoning = reasoning,
                WillCauseLeaving = bestTarget.FateTokens <= fateToRemove,
                Source = "C# AI"
            };
        }
        
        /// <summary>
        /// Generate reasoning for C# recommendation
        /// </summary>
        /// <param name="target">Recommended target</param>
        /// <param name="context">Ability context</param>
        /// <returns>Reasoning string</returns>
        private string GenerateReasoningCSharp(BaseCard target, AbilityContext context)
        {
            var reasons = new List<string>();
            
            if (target.Owner != context.Player)
            {
                reasons.Add("opponent's character");
                
                if (target.FateTokens <= fateToRemove)
                {
                    reasons.Add("will remove from play");
                }
                else
                {
                    reasons.Add($"has {target.FateTokens} fate");
                }
            }
            else
            {
                reasons.Add("your character");
                if (target.HasLeavesPlayAbilities)
                {
                    reasons.Add("triggers beneficial leave-play effects");
                }
            }
            
            if (target.Power >= 4)
            {
                reasons.Add($"powerful ({target.Power} power)");
            }
            
            if (target.IsParticipatingInConflict)
            {
                reasons.Add("participating in conflict");
            }
            
            return string.Join(", ", reasons);
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
        
        #region Outcome Simulation
        
        /// <summary>
        /// Simulate all possible effect outcomes
        /// </summary>
        /// <param name="context">Ability context</param>
        private void SimulateEffectOutcomes(AbilityContext context)
        {
            try
            {
                // Try Python simulation first
                if (ShouldUsePythonImplementation())
                {
                    simulatedOutcomes = GetPythonSimulatedOutcomes(context);
                }
                else
                {
                    // Fallback to C# simulation
                    simulatedOutcomes = SimulateOutcomesCSharp(context);
                }
                
                if (simulatedOutcomes != null && simulatedOutcomes.Count > 0)
                {
                    Debug.Log($"📊 Simulated {simulatedOutcomes.Count} possible outcomes");
                    
                    // Log top outcomes
                    var topOutcomes = simulatedOutcomes.OrderByDescending(o => o.StrategicValue).Take(3);
                    foreach (var outcome in topOutcomes)
                    {
                        Debug.Log($"  • {outcome.Target.Name}: {outcome.FateBefore} → {outcome.FateAfter} fate " +
                                 $"(Value: {outcome.StrategicValue:F1}, Leaves: {outcome.WillLeavePlay})");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to simulate outcomes: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get simulated outcomes from Python script
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of simulated outcomes</returns>
        private List<EffectOutcome> GetPythonSimulatedOutcomes(AbilityContext context)
        {
            var outcomes = new List<EffectOutcome>();
            
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                var result = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "simulate_outcomes",
                    context
                );
                
                if (result is IList<object> pythonOutcomes)
                {
                    foreach (var outcome in pythonOutcomes)
                    {
                        if (outcome is PythonDictionary outcomeDict)
                        {
                            var target = outcomeDict.get("target", null) as BaseCard;
                            if (target != null)
                            {
                                outcomes.Add(new EffectOutcome
                                {
                                    Target = target,
                                    FateBefore = Convert.ToInt32(outcomeDict.get("fate_before", 0)),
                                    FateAfter = Convert.ToInt32(outcomeDict.get("fate_after", 0)),
                                    WillLeavePlay = Convert.ToBoolean(outcomeDict.get("will_leave_play", false)),
                                    StrategicValue = Convert.ToSingle(outcomeDict.get("strategic_value", 0.0)),
                                    OwnerType = outcomeDict.get("owner", "unknown").ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HandlePythonError("GetPythonSimulatedOutcomes", e);
            }
#endif
            
            return outcomes;
        }
        
        /// <summary>
        /// Simulate outcomes using C# logic
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>List of simulated outcomes</returns>
        private List<EffectOutcome> SimulateOutcomesCSharp(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            var outcomes = new List<EffectOutcome>();
            
            foreach (var target in validTargets)
            {
                var fateBefore = target.FateTokens;
                var fateAfter = Mathf.Max(0, fateBefore - fateToRemove);
                var willLeave = fateAfter <= 0;
                var strategicValue = GetTargetStrategicValue(target, context);
                
                outcomes.Add(new EffectOutcome
                {
                    Target = target,
                    FateBefore = fateBefore,
                    FateAfter = fateAfter,
                    WillLeavePlay = willLeave,
                    StrategicValue = strategicValue,
                    OwnerType = target.Owner == context.Player ? "player" : "opponent"
                });
            }
            
            // Sort by strategic value
            outcomes = outcomes.OrderByDescending(o => o.StrategicValue).ToList();
            
            return outcomes;
        }
        
        /// <summary>
        /// Get comprehensive effect analysis
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Effect analysis data</returns>
        public EffectAnalysis GetEffectAnalysis(AbilityContext context)
        {
            var validTargets = GetValidCharacterTargets(context);
            var charactersLeaving = GetCharactersThatWouldLeave(context);
            var recommendation = currentRecommendation;
            var outcomes = simulatedOutcomes ?? SimulateOutcomesCSharp(context);
            
            var analysis = new EffectAnalysis
            {
                ValidTargetsCount = validTargets.Count,
                CharactersLeavingCount = charactersLeaving.Count,
                TotalFateAvailable = validTargets.Sum(t => t.FateTokens),
                AverageStrategicValue = outcomes.Any() ? outcomes.Average(o => o.StrategicValue) : 0f,
                BestOutcome = outcomes.FirstOrDefault(),
                Recommendation = recommendation,
                ThreatLevel = CalculateThreatLevel(context, validTargets),
                FateEconomyImpact = CalculateFateEconomyImpact(context, validTargets)
            };
            
            return analysis;
        }
        
        /// <summary>
        /// Calculate threat level of opponent characters
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="validTargets">Valid targets</param>
        /// <returns>Threat level score</returns>
        private float CalculateThreatLevel(AbilityContext context, List<BaseCard> validTargets)
        {
            if (!enableThreatAssessment)
                return 0f;
                
            float threatLevel = 0f;
            var opponentTargets = validTargets.Where(t => t.Owner != context.Player).ToList();
            
            foreach (var target in opponentTargets)
            {
                // Base threat from power
                threatLevel += target.Power * 0.5f;
                
                // Extra threat from fate (sustainability)
                threatLevel += target.FateTokens * 0.3f;
                
                // Extra threat from special abilities
                if (target.HasSpecialAbilities)
                {
                    threatLevel += 1.0f;
                }
                
                // Extra threat if participating in conflict
                if (target.IsParticipatingInConflict)
                {
                    threatLevel += 2.0f;
                }
            }
            
            return Mathf.Clamp(threatLevel, 0f, 10f);
        }
        
        /// <summary>
        /// Calculate fate economy impact
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="validTargets">Valid targets</param>
        /// <returns>Economy impact score</returns>
        private float CalculateFateEconomyImpact(AbilityContext context, List<BaseCard> validTargets)
        {
            if (!trackFateEconomy)
                return 0f;
                
            float economyImpact = 0f;
            
            foreach (var target in validTargets)
            {
                var fateValue = Mathf.Min(target.FateTokens, fateToRemove);
                
                if (target.Owner != context.Player)
                {
                    // Positive impact - removing opponent's fate
                    economyImpact += fateValue;
                    
                    // Extra value if it causes character to leave
                    if (target.FateTokens <= fateToRemove)
                    {
                        economyImpact += target.Cost * 0.5f; // Fraction of character cost
                    }
                }
                else
                {
                    // Negative impact - removing own fate
                    economyImpact -= fateValue;
                }
            }
            
            return economyImpact;
        }
        
        #endregion
        
        #region Enhanced UI Integration
        
        /// <summary>
        /// Get enhanced target data with comprehensive information
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Enhanced target selection data</returns>
        public List<EnhancedVoidTargetData> GetEnhancedTargetData(AbilityContext context)
        {
            var targets = GetValidCharacterTargets(context);
            var enhancedData = new List<EnhancedVoidTargetData>();
            
            foreach (var target in targets)
            {
                var data = new EnhancedVoidTargetData
                {
                    Target = target,
                    DisplayName = target.Name,
                    FateTokens = target.FateTokens,
                    WillLeavePlay = target.FateTokens <= fateToRemove,
                    StrategicValue = GetTargetStrategicValue(target, context),
                    IsRecommended = currentRecommendation?.Target == target,
                    OwnerType = target.Owner == context.Player ? "Player" : "Opponent",
                    Power = target.Power,
                    Cost = target.Cost
                };
                
                // Generate description
                data.Description = GenerateEnhancedTargetDescription(data);
                
                enhancedData.Add(data);
            }
            
            // Sort by strategic value and highlight leaving characters
            enhancedData = enhancedData
                .OrderByDescending(d => d.WillLeavePlay ? 1 : 0)
                .ThenByDescending(d => d.StrategicValue)
                .ToList();
            
            return enhancedData;
        }
        
        /// <summary>
        /// Generate enhanced description for target data
        /// </summary>
        /// <param name="data">Target data</param>
        /// <returns>Description string</returns>
        private string GenerateEnhancedTargetDescription(EnhancedVoidTargetData data)
        {
            var description = $"{data.OwnerType}'s character ({data.Power} power, {data.FateTokens} fate)";
            
            if (data.WillLeavePlay && highlightCharactersLeaving)
            {
                description += " ⚠️ WILL LEAVE PLAY";
            }
            
            if (showStrategicValues)
            {
                description += $" | Value: {data.StrategicValue:F1}/10";
            }
            
            if (data.IsRecommended)
            {
                description += " | ⭐ AI Recommended";
            }
            
            return description;
        }
        
        #endregion
        
        #region Enhanced Analytics
        
        /// <summary>
        /// Log enhanced analytics with comprehensive data
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="selectedTarget">Selected target</param>
        /// <param name="wasRecommended">Whether this was the AI recommendation</param>
        protected void LogEnhancedAnalytics(AbilityContext context, BaseCard selectedTarget, bool wasRecommended)
        {
            if (!logDetailedAnalytics)
                return;
                
            var analysis = GetEffectAnalysis(context);
            
            var analyticsData = new Dictionary<string, object>
            {
                { "ability_id", AbilityId },
                { "player_id", context.Player.PlayerId },
                { "selected_target_id", selectedTarget?.CardId },
                { "was_ai_recommended", wasRecommended },
                { "implementation_used", ShouldUsePythonImplementation() ? "python" : "csharp" },
                { "turn_number", Game.TurnManager.CurrentTurn },
                { "valid_targets_count", analysis.ValidTargetsCount },
                { "characters_leaving_count", analysis.CharactersLeavingCount },
                { "total_fate_available", analysis.TotalFateAvailable },
                { "threat_level", analysis.ThreatLevel },
                { "fate_economy_impact", analysis.FateEconomyImpact }
            };
            
            if (selectedTarget != null)
            {
                analyticsData.Add("target_fate_before", selectedTarget.FateTokens + fateToRemove);
                analyticsData.Add("target_fate_after", selectedTarget.FateTokens);
                analyticsData.Add("target_will_leave", selectedTarget.FateTokens <= 0);
                analyticsData.Add("target_power", selectedTarget.Power);
                analyticsData.Add("target_cost", selectedTarget.Cost);
                analyticsData.Add("target_owner", selectedTarget.Owner == context.Player ? "player" : "opponent");
            }
            
            if (currentRecommendation != null)
            {
                analyticsData.Add("ai_recommended_target", currentRecommendation.Target.CardId);
                analyticsData.Add("ai_strategic_value", currentRecommendation.StrategicValue);
                analyticsData.Add("ai_reasoning", currentRecommendation.Reasoning);
                analyticsData.Add("player_followed_recommendation", wasRecommended);
            }
            
            Game.Analytics.LogEvent("void_ring_effect_enhanced", analyticsData);
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
            simulatedOutcomes = null;
            
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
        
        /// <summary>
        /// Configure Python fate amount at runtime
        /// </summary>
        /// <param name="amount">Fate amount to remove</param>
        [ContextMenu("Configure Python Fate Amount")]
        public void ConfigurePythonFateAmount(int amount = 1)
        {
            if (!ShouldUsePythonImplementation())
            {
                Debug.LogWarning("Python implementation not available");
                return;
            }
            
            try
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "configure_fate_amount", amount);
#endif
                Debug.Log($"🐍 Python fate amount configured: {amount}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to configure Python fate amount: {e.Message}");
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
                pythonScriptName = "void_ring_effect";
            }
            
            if (autoExecuteThreshold < 0)
                autoExecuteThreshold = 0;
            if (autoExecuteThreshold > 10)
                autoExecuteThreshold = 10;
        }
        
        /// <summary>
        /// Show current analysis in console
        /// </summary>
        [ContextMenu("Show Effect Analysis")]
        public void ShowEffectAnalysisInConsole()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Effect analysis only available during play mode");
                return;
            }
            
            // This would need a proper context in a real game
            var mockContext = new AbilityContext(); // Simplified
            var analysis = GetEffectAnalysis(mockContext);
            
            Debug.Log($"📊 Void Ring Effect Analysis:\n" +
                     $"Valid Targets: {analysis.ValidTargetsCount}\n" +
                     $"Characters Leaving: {analysis.CharactersLeavingCount}\n" +
                     $"Total Fate Available: {analysis.TotalFateAvailable}\n" +
                     $"Average Strategic Value: {analysis.AverageStrategicValue:F1}\n" +
                     $"Threat Level: {analysis.ThreatLevel:F1}\n" +
                     $"Fate Economy Impact: {analysis.FateEconomyImpact:F1}");
        }
#endif
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for Void Ring AI recommendations
    /// </summary>
    [Serializable]
    public class VoidRingRecommendation
    {
        public BaseCard Target;
        public float StrategicValue;
        public string Reasoning;
        public bool WillCauseLeaving;
        public string Source;
        
        public override string ToString()
        {
            return $"Target {Target?.Name} (Value: {StrategicValue:F1}) - {Reasoning}";
        }
    }
    
    /// <summary>
    /// Data structure for effect outcomes
    /// </summary>
    [Serializable]
    public class EffectOutcome
    {
        public BaseCard Target;
        public int FateBefore;
        public int FateAfter;
        public bool WillLeavePlay;
        public float StrategicValue;
        public string OwnerType;
        
        public override string ToString()
        {
            return $"{Target?.Name}: {FateBefore} → {FateAfter} fate (Value: {StrategicValue:F1}, Leaves: {WillLeavePlay})";
        }
    }
    
    /// <summary>
    /// Enhanced target data with comprehensive information
    /// </summary>
    [Serializable]
    public class EnhancedVoidTargetData
    {
        public BaseCard Target;
        public string DisplayName;
        public string Description;
        public int FateTokens;
        public bool WillLeavePlay;
        public float StrategicValue;
        public bool IsRecommended;
        public string OwnerType;
        public int Power;
        public int Cost;
        
        public override string ToString()
        {
            var leaving = WillLeavePlay ? " (Leaving)" : "";
            var recommended = IsRecommended ? " (Recommended)" : "";
            return $"{DisplayName} - {FateTokens} fate, Value: {StrategicValue:F1}{leaving}{recommended}";
        }
    }
    
    /// <summary>
    /// Comprehensive effect analysis data
    /// </summary>
    [Serializable]
    public class EffectAnalysis
    {
        public int ValidTargetsCount;
        public int CharactersLeavingCount;
        public int TotalFateAvailable;
        public float AverageStrategicValue;
        public EffectOutcome BestOutcome;
        public VoidRingRecommendation Recommendation;
        public float ThreatLevel;
        public float FateEconomyImpact;
        
        public override string ToString()
        {
            return $"Void Ring Analysis: {ValidTargetsCount} targets, {CharactersLeavingCount} leaving, " +
                   $"Threat: {ThreatLevel:F1}, Economy: {FateEconomyImpact:F1}";
        }
    }
}
