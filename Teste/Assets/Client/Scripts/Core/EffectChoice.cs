using UnityEngine;
using System;

namespace L5RGame
{
    public class EffectChoice : MonoBehaviour
    {
        [Header("Effect Choice Configuration")]
        public string choiceName = "Default Choice";
        public string description = "Default effect choice";
        
        private System.Action executeAction;
        private System.Func<bool> executeFunction;
        
        /// <summary>
        /// Initialize the effect choice with an action
        /// </summary>
        public void Initialize(string name, string desc, System.Action action)
        {
            choiceName = name;
            description = desc;
            executeAction = action;
        }
        
        /// <summary>
        /// Initialize the effect choice with a function
        /// </summary>
        public void Initialize(string name, string desc, System.Func<bool> function)
        {
            choiceName = name;
            description = desc;
            executeFunction = function;
        }
        
        /// <summary>
        /// Execute the effect choice
        /// </summary>
        public void Execute()
        {
            try
            {
                if (executeAction != null)
                {
                    executeAction.Invoke();
                }
                else if (executeFunction != null)
                {
                    executeFunction.Invoke();
                }
                else
                {
                    Debug.LogWarning($"EffectChoice '{choiceName}' has no execute action or function defined.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing EffectChoice '{choiceName}': {e.Message}");
            }
        }
        
        /// <summary>
        /// Check if this effect choice can be executed
        /// </summary>
        public bool CanExecute()
        {
            return executeAction != null || executeFunction != null;
        }
        
        /// <summary>
        /// Get debug information about this effect choice
        /// </summary>
        public string GetDebugInfo()
        {
            return $"EffectChoice: {choiceName} - {description} - Can Execute: {CanExecute()}";
        }
    }
}