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
    [System.Serializable]
    public class WaterRingRecommendation
    {
        public string RecommendedAction;
        public BaseCard RecommendedTarget;
        public float Confidence;
        public string Reasoning;
        public List<BaseCard> AlternativeTargets;
        
        // Properties for compilation - made settable
        public string Action { get; set; }
        public BaseCard Target { get; set; }
        public float StrategicValue { get; set; }
        public bool WillChangeBoardState { get; set; } = true;
        public string Source { get; set; } = "AI";
        
        public WaterRingRecommendation() 
        {
            AlternativeTargets = new List<BaseCard>();
        }
        
        public WaterRingRecommendation(string action, BaseCard target = null, float confidence = 0.5f)
        {
            RecommendedAction = action;
            RecommendedTarget = target;
            Confidence = confidence;
            Action = action;
            Target = target;
            StrategicValue = confidence * 100f;
            AlternativeTargets = new List<BaseCard>();
        }
        
        public override string ToString()
        {
            return $"Action: {RecommendedAction}, Target: {RecommendedTarget?.name ?? "None"}, Confidence: {Confidence:P1}";
        }
    }

    [System.Serializable]
    public class BoardPositionAnalysis
    {
        public int PlayerCardCount;
        public int OpponentCardCount;
        public List<BaseCard> HighValueTargets;
        public List<BaseCard> LowValueTargets;
        public float PositionalAdvantage;
        public Dictionary<string, object> Metadata;
        
        // Instance properties for compilation
        public int TotalCharactersInPlay { get; set; }
        public int OwnReadyCharacters { get; set; }
        public int OwnBowedCharacters { get; set; }
        public int OpponentReadyCharacters { get; set; }
        public int OpponentBowedCharacters { get; set; }
        public int VulnerableCharacters { get; set; }
        public int HighPowerTargets { get; set; }
        public string Summary { get; set; } = "Board analysis complete";
        public List<string> TacticalSuggestions { get; set; } = new List<string>();
        
        public BoardPositionAnalysis()
        {
            HighValueTargets = new List<BaseCard>();
            LowValueTargets = new List<BaseCard>();
            Metadata = new Dictionary<string, object>();
        }
        
        public override string ToString()
        {
            return $"Player: {PlayerCardCount}, Opponent: {OpponentCardCount}, Advantage: {PositionalAdvantage:F2}";
        }
    }

    [System.Serializable]
    public class EnhancedWaterTargetData
    {
        public BaseCard Target;
        public string DisplayName;
        public bool IsValid;
        public float TacticalValue;
        public string PositionalAdvantage;
        public Dictionary<string, object> Analysis;
        
        // Instance properties for compilation
        public bool IsBowed { get; set; }
        public string Action { get; set; }
        public int FateTokens { get; set; }
        public int Power { get; set; }
        public float StrategicValue { get; set; }
        public bool IsRecommended { get; set; }
        public string OwnerType { get; set; }
        public bool IsParticipatingInConflict { get; set; }
        public bool IsVulnerable { get; set; }
        public string Description { get; set; }
        
        public EnhancedWaterTargetData()
        {
            Analysis = new Dictionary<string, object>();
        }
        
        public EnhancedWaterTargetData(BaseCard target, bool isValid = true)
        {
            Target = target;
            DisplayName = target?.name ?? "Unknown";
            IsValid = isValid;
            TacticalValue = 0.5f;
            Analysis = new Dictionary<string, object>();
        }
        
        public override string ToString()
        {
            return $"{DisplayName} - Value: {TacticalValue:F2}, Valid: {IsValid}";
        }
    }

    /// <summary>
    /// Bridge class that integrates C# WaterRingEffect with IronPython scripting
    /// Provides advanced board position analysis and tactical recommendations
    /// </summary>
    [Serializable]
    public class WaterRingEffectBridge : WaterRingEffect
    {
        #region Fields

        [Header("Python Integration")]
        [SerializeField] private bool preferPythonImplementation = true;
        [SerializeField] private bool fallbackToCSharp = true;
        [SerializeField] private string pythonScriptName = "water_ring_effect";

        [Header("AI Integration")]
        [SerializeField] private bool enableAIRecommendations = true;
        [SerializeField] private bool showStrategicValues = true;
        [SerializeField] private float autoExecuteThreshold = 7.0f;
        [SerializeField] private bool highlightTacticalMoves = true;

        [Header("Board Analysis")]
        [SerializeField] private bool enableBoardPositionAnalysis = true;
        [SerializeField] private bool trackCharacterStatus = true;
        [SerializeField] private bool analyzeConflictParticipation = true;
        [SerializeField] private bool enableTacticalSuggestions = true;

        [Header("Advanced Analytics")]
        [SerializeField] private bool logDetailedAnalytics = true;
        [SerializeField] private bool trackStatusChanges = true;
        [SerializeField] private bool monitorBoardControl = true;

        private bool pythonScriptLoaded = false;
        private bool pythonExecutionFailed = false;
        private WaterRingRecommendation currentRecommendation;
        private BoardPositionAnalysis currentBoardAnalysis;

        #endregion

        #region Constructor

        public WaterRingEffectBridge() : this(true) { }

        public WaterRingEffectBridge(bool optional) : base(optional)
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

            // Analyze board position if enabled
            if (enableBoardPositionAnalysis)
            {
                AnalyzeBoardPosition(context);
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
                Debug.LogWarning($"Failed to initialize Python integration for WaterRingEffect: {e.Message}");
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
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "create_water_ring_effect", true);
                
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
                "water_ring_effect.can_execute", 
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
                "execute_water_ring_effect",
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
                    var actionText = currentRecommendation.Action;
                    Debug.Log($"🤖 AI Recommendation: {actionText} {currentRecommendation.Target.Name} " +
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
        public WaterRingRecommendation GetAIRecommendation(AbilityContext context)
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
        private WaterRingRecommendation GetPythonAIRecommendation(AbilityContext context)
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
                var action = recommendationDict.get("action", "").ToString();
                var reasoning = recommendationDict.get("reasoning", "").ToString();
                
                if (target != null && !string.IsNullOrEmpty(action))
                {
                    return new WaterRingRecommendation
                    {
                        Target = target,
                        Action = action,
                        StrategicValue = value,
                        Reasoning = reasoning,
                        WillChangeBoardState = true,
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
        private WaterRingRecommendation GenerateAIRecommendationCSharp(AbilityContext context)
        {
            var bestTarget = GetBestTargetRecommendation(context);
            if (bestTarget == null)
            {
                return null;
            }

            var strategicValue = GetTargetStrategicValue(bestTarget, context);
            var action = bestTarget.IsBowed ? "Ready" : "Bow";
            var reasoning = GenerateReasoningCSharp(bestTarget, context);

            return new WaterRingRecommendation
            {
                Target = bestTarget,
                Action = action,
                StrategicValue = strategicValue,
                Reasoning = reasoning,
                WillChangeBoardState = true,
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

            if (target.IsBowed)
            {
                if (target.Owner == context.Player)
                {
                    reasons.Add("ready your character");
                    if (target.Power >= 4)
                    {
                        reasons.Add($"powerful ({target.Power} power)");
                    }
                    if (target.HasActionAbilities)
                    {
                        reasons.Add("can use abilities");
                    }
                }
                else
                {
                    reasons.Add("ready opponent's character (questionable benefit)");
                }
            }
            else
            {
                if (target.Owner != context.Player)
                {
                    reasons.Add("bow opponent's character");
                    if (target.Power >= 4)
                    {
                        reasons.Add($"remove threat ({target.Power} power)");
                    }
                    if (target.IsParticipatingInConflict)
                    {
                        reasons.Add("remove from conflict");
                    }
                }
                else
                {
                    reasons.Add("bow your character");
                    if (target.HasBowTriggeredAbilities)
                    {
                        reasons.Add("trigger bow abilities");
                    }
                }
            }

            if (target.FateTokens == 0)
            {
                reasons.Add("no fate (vulnerable)");
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

        #region Board Position Analysis

        /// <summary>
        /// Analyze current board position for tactical insights
        /// </summary>
        /// <param name="context">Ability context</param>
        private void AnalyzeBoardPosition(AbilityContext context)
        {
            try
            {
                // Try Python board analysis first
                if (ShouldUsePythonImplementation())
                {
                    currentBoardAnalysis = GetPythonBoardAnalysis(context);
                }
                else
                {
                    // Fallback to C# analysis
                    currentBoardAnalysis = AnalyzeBoardPositionCSharp(context);
                }

                if (currentBoardAnalysis != null && enableTacticalSuggestions)
                {
                    Debug.Log($"📋 Board Analysis: {currentBoardAnalysis.Summary}");

                    if (currentBoardAnalysis.TacticalSuggestions.Count > 0)
                    {
                        Debug.Log($"💡 Tactical Suggestions: {string.Join(", ", currentBoardAnalysis.TacticalSuggestions)}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to analyze board position: {e.Message}");
            }
        }

        /// <summary>
        /// Get board analysis from Python script
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Board analysis data</returns>
        private BoardPositionAnalysis GetPythonBoardAnalysis(AbilityContext context)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var result = PythonManager.Instance.ExecuteFunction(
                pythonScriptName,
                "analyze_board_position",
                context
            );
            
            if (result is PythonDictionary analysisDict)
            {
                return new BoardPositionAnalysis
                {
                    TotalCharactersInPlay = Convert.ToInt32(analysisDict.get("total_characters_in_play", 0)),
                    OwnReadyCharacters = Convert.ToInt32(analysisDict.get("own_ready_characters", 0)),
                    OwnBowedCharacters = Convert.ToInt32(analysisDict.get("own_bowed_characters", 0)),
                    OpponentReadyCharacters = Convert.ToInt32(analysisDict.get("opponent_ready_characters", 0)),
                    OpponentBowedCharacters = Convert.ToInt32(analysisDict.get("opponent_bowed_characters", 0)),
                    VulnerableCharacters = Convert.ToInt32(analysisDict.get("vulnerable_characters", 0)),
                    HighPowerTargets = Convert.ToInt32(analysisDict.get("high_power_targets", 0)),
                    Summary = GenerateBoardSummary(analysisDict),
                    TacticalSuggestions = new List<string>()
                };
            }
#endif

            return null;
        }

        /// <summary>
        /// Analyze board position using C# logic
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Board analysis</returns>
        private BoardPositionAnalysis AnalyzeBoardPositionCSharp(AbilityContext context)
        {
            var allCharacters = Game.Instance.GetAllCardsInPlay()
                .Where(c => c.CardType == CardTypes.Character)
                .ToList();

            var analysis = new BoardPositionAnalysis();

            foreach (var character in allCharacters)
            {
                analysis.TotalCharactersInPlay++;

                if (character.Owner == context.Player)
                {
                    if (character.IsBowed)
                        analysis.OwnBowedCharacters++;
                    else
                        analysis.OwnReadyCharacters++;
                }
                else
                {
                    if (character.IsBowed)
                        analysis.OpponentBowedCharacters++;
                    else
                        analysis.OpponentReadyCharacters++;
                }

                if (character.FateTokens == 0)
                    analysis.VulnerableCharacters++;

                if (character.Power >= 4)
                    analysis.HighPowerTargets++;
            }

            analysis.Summary = GenerateBoardSummaryCSharp(analysis);
            analysis.TacticalSuggestions = GenerateTacticalSuggestions(analysis, context);

            return analysis;
        }

        /// <summary>
        /// Generate board summary from Python analysis
        /// </summary>
        /// <param name="analysisDict">Python analysis dictionary</param>
        /// <returns>Summary string</returns>
        private string GenerateBoardSummary(PythonDictionary analysisDict)
        {
            var ownReady = Convert.ToInt32(analysisDict.get("own_ready_characters", 0));
            var opponentReady = Convert.ToInt32(analysisDict.get("opponent_ready_characters", 0));
            var vulnerable = Convert.ToInt32(analysisDict.get("vulnerable_characters", 0));

            return $"Board: {ownReady} your ready vs {opponentReady} opponent ready, {vulnerable} vulnerable";
        }

        /// <summary>
        /// Generate board summary from C# analysis
        /// </summary>
        /// <param name="analysis">Board analysis</param>
        /// <returns>Summary string</returns>
        private string GenerateBoardSummaryCSharp(BoardPositionAnalysis analysis)
        {
            return $"Board: {analysis.OwnReadyCharacters} your ready vs {analysis.OpponentReadyCharacters} opponent ready, " +
                   $"{analysis.VulnerableCharacters} vulnerable";
        }

        /// <summary>
        /// Generate tactical suggestions based on board state
        /// </summary>
        /// <param name="analysis">Board analysis</param>
        /// <param name="context">Ability context</param>
        /// <returns>List of tactical suggestions</returns>
        private List<string> GenerateTacticalSuggestions(BoardPositionAnalysis analysis, AbilityContext context)
        {
            var suggestions = new List<string>();

            // Suggest readying own characters if many are bowed
            if (analysis.OwnBowedCharacters > analysis.OwnReadyCharacters)
            {
                suggestions.Add("Consider readying your characters for board presence");
            }

            // Suggest bowing opponent threats
            if (analysis.OpponentReadyCharacters > analysis.OwnReadyCharacters)
            {
                suggestions.Add("Priority: bow opponent's ready characters");
            }

            // Suggest targeting vulnerable characters
            if (analysis.VulnerableCharacters > 0)
            {
                suggestions.Add("Target vulnerable characters (no fate)");
            }

            // Suggest focusing on high-power targets
            if (analysis.HighPowerTargets > 2)
            {
                suggestions.Add("Focus on high-power threats");
            }

            return suggestions;
        }

        #endregion

        #region Enhanced UI Integration

        /// <summary>
        /// Get enhanced target data with board position context
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Enhanced target selection data</returns>
        public List<EnhancedWaterTargetData> GetEnhancedTargetData(AbilityContext context)
        {
            var targets = GetValidCharacterTargets(context);
            var enhancedData = new List<EnhancedWaterTargetData>();

            foreach (var target in targets)
            {
                var data = new EnhancedWaterTargetData
                {
                    Target = target,
                    DisplayName = target.Name,
                    IsBowed = target.IsBowed,
                    Action = target.IsBowed ? "Ready" : "Bow",
                    FateTokens = target.FateTokens,
                    Power = target.Power,
                    StrategicValue = GetTargetStrategicValue(target, context),
                    IsRecommended = currentRecommendation?.Target == target,
                    OwnerType = target.Owner == context.Player ? "Player" : "Opponent",
                    IsParticipatingInConflict = target.IsParticipatingInConflict,
                    IsVulnerable = target.FateTokens == 0
                };

                // Generate description
                data.Description = GenerateEnhancedTargetDescription(data);

                enhancedData.Add(data);
            }

            // Sort by tactical priority
            enhancedData = enhancedData
                .OrderByDescending(d => d.IsRecommended ? 1 : 0)
                .ThenByDescending(d => d.StrategicValue)
                .ThenByDescending(d => d.IsParticipatingInConflict ? 1 : 0)
                .ToList();

            return enhancedData;
        }

        /// <summary>
        /// Generate enhanced description for target data
        /// </summary>
        /// <param name="data">Target data</param>
        /// <returns>Description string</returns>
        private string GenerateEnhancedTargetDescription(EnhancedWaterTargetData data)
        {
            var description = $"{data.OwnerType}'s {(data.IsBowed ? "bowed" : "ready")} character " +
                             $"({data.Power} power, {data.FateTokens} fate) - Will {data.Action}";

            if (data.IsParticipatingInConflict && highlightTacticalMoves)
            {
                description += " ⚔️ In Conflict";
            }

            if (data.IsVulnerable)
            {
                description += " ⚠️ Vulnerable";
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
        /// Log enhanced analytics with board state data
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="selectedTarget">Selected target</param>
        /// <param name="action">Action taken</param>
        /// <param name="wasRecommended">Whether this was the AI recommendation</param>
        protected void LogEnhancedAnalytics(AbilityContext context, BaseCard selectedTarget,
                                          string action, bool wasRecommended)
        {
            if (!logDetailedAnalytics)
                return;

            var boardAnalysis = currentBoardAnalysis ?? AnalyzeBoardPositionCSharp(context);

            var analyticsData = new Dictionary<string, object>
            {
                { "ability_id", AbilityId.WaterRing },
                { "player_id", context.Player.PlayerId },
                { "selected_target_id", selectedTarget?.CardId },
                { "action", action },
                { "was_ai_recommended", wasRecommended },
                { "implementation_used", ShouldUsePythonImplementation() ? "python" : "csharp" },
                { "turn_number", Game.TurnManager.CurrentTurn }
            };

            // Add board state data
            if (boardAnalysis != null)
            {
                analyticsData.Add("board_total_characters", boardAnalysis.TotalCharactersInPlay);
                analyticsData.Add("board_own_ready", boardAnalysis.OwnReadyCharacters);
                analyticsData.Add("board_own_bowed", boardAnalysis.OwnBowedCharacters);
                analyticsData.Add("board_opponent_ready", boardAnalysis.OpponentReadyCharacters);
                analyticsData.Add("board_opponent_bowed", boardAnalysis.OpponentBowedCharacters);
                analyticsData.Add("board_vulnerable", boardAnalysis.VulnerableCharacters);
                analyticsData.Add("board_high_power", boardAnalysis.HighPowerTargets);
            }

            // Add target data
            if (selectedTarget != null)
            {
                analyticsData.Add("target_was_bowed", selectedTarget.IsBowed);
                analyticsData.Add("target_power", selectedTarget.Power);
                analyticsData.Add("target_fate", selectedTarget.FateTokens);
                analyticsData.Add("target_owner", selectedTarget.Owner == context.Player ? "player" : "opponent");
                analyticsData.Add("target_in_conflict", selectedTarget.IsParticipatingInConflict);
                analyticsData.Add("target_vulnerable", selectedTarget.FateTokens == 0);
            }

            // Add recommendation data
            if (currentRecommendation != null)
            {
                analyticsData.Add("ai_recommended_target", currentRecommendation.Target.CardId);
                analyticsData.Add("ai_recommended_action", currentRecommendation.Action);
                analyticsData.Add("ai_strategic_value", currentRecommendation.StrategicValue);
                analyticsData.Add("ai_reasoning", currentRecommendation.Reasoning);
                analyticsData.Add("player_followed_recommendation", wasRecommended);
            }

            Game.Analytics.LogEvent("water_ring_effect_enhanced", analyticsData);
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
            currentBoardAnalysis = null;

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
        
        #region Target Recommendation
        
        /// <summary>
        /// Get the best target recommendation
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>Best target or null</returns>
        public BaseCard GetBestTargetRecommendation(AbilityContext context)
        {
            var targets = GetPythonValidTargets(context);
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
        
        #endregion

    }
}