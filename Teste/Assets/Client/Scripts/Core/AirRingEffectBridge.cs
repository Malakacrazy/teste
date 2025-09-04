using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame
{
    [System.Serializable]
    public class ChoiceData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public bool IsValid;
        
        // Missing properties for compilation
        public static string Text { get; set; }
        public static string Value { get; set; }
        public static bool Available { get; set; }
        
        public ChoiceData() { }
        
        public ChoiceData(string id, string displayName, string description = "", bool isValid = true)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            IsValid = isValid;
        }
        
        public override string ToString()
        {
            return $"{DisplayName} - Valid: {IsValid}";
        }
    }

    /// <summary>
    /// Bridge class that integrates C# AirRingEffect with IronPython scripting
    /// Provides fallback to C# implementation when Python is unavailable
    /// </summary>
    [Serializable]
    public class AirRingEffectBridge : AirRingEffect
    {
        #region Fields

        [Header("Python Integration")]
        [SerializeField] private bool preferPythonImplementation = true;
        [SerializeField] private bool fallbackToCSharp = true;
        [SerializeField] private string pythonScriptName = "air_ring_effect";

        private bool pythonScriptLoaded = false;
        private bool pythonExecutionFailed = false;

        #endregion

        #region Constructor

        public AirRingEffectBridge() : this(true) { }

        public AirRingEffectBridge(bool optional) : base(optional)
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
                Debug.LogWarning($"Failed to initialize Python integration for AirRingEffect: {e.Message}");
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
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "create_air_ring_effect", true);
                
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
                "air_ring_effect.can_execute", 
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
                "execute_air_ring_effect",
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
                    "get_air_ring_choices",
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
    }
}