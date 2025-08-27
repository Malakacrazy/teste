using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Simple step for basic game actions
    /// </summary>
    public partial class SimpleStep : BaseStep, IGameStep
    {
        private Func<bool> stepFunction;
        
        public SimpleStep(Game gameInstance, Func<bool> step) : base(gameInstance)
        {
            stepFunction = step;
        }
        
        public override bool Continue()
        {
            try
            {
                bool result = stepFunction?.Invoke() ?? true;
                if (result)
                {
                    ForceComplete();
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in SimpleStep: {e.Message}");
                ForceComplete();
                return true; // Continue despite error
            }
        }
        
        public override string GetDebugInfo()
        {
            return $"SimpleStep - Function: {(stepFunction != null ? "Set" : "Null")}";
        }
    }
}
