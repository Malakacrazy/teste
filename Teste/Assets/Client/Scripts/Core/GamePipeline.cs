using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Game pipeline for managing sequences of game steps
    /// </summary>
    public class GamePipeline
    {
        private Queue<BaseStep> steps = new Queue<BaseStep>();
        private BaseStep currentStep;
        private bool isProcessing = false;
        
        // Events
        public event Action<BaseStep> OnStepStarted;
        public event Action<BaseStep> OnStepCompleted;
        public event Action<BaseStep, Exception> OnStepError;
        public event Action OnPipelineCompleted;
        
        #region Properties
        
        /// <summary>
        /// Number of remaining steps in the pipeline
        /// </summary>
        public int Length => steps.Count;
        
        /// <summary>
        /// Current step being processed
        /// </summary>
        public BaseStep CurrentStep => currentStep;
        
        /// <summary>
        /// Whether the pipeline is currently processing
        /// </summary>
        public bool IsProcessing => isProcessing;
        
        #endregion
        
        #region Step Management
        
        /// <summary>
        /// Add a step to the end of the pipeline
        /// </summary>
        public void QueueStep(BaseStep step)
        {
            if (step != null)
            {
                steps.Enqueue(step);
            }
        }
        
        /// <summary>
        /// Add a step to the front of the pipeline
        /// </summary>
        public void InsertStep(BaseStep step)
        {
            if (step != null)
            {
                var tempSteps = steps.ToList();
                steps.Clear();
                steps.Enqueue(step);
                foreach (var s in tempSteps)
                {
                    steps.Enqueue(s);
                }
            }
        }
        
        /// <summary>
        /// Clear all steps from the pipeline
        /// </summary>
        public void Clear()
        {
            steps.Clear();
            currentStep = null;
            isProcessing = false;
        }
        
        /// <summary>
        /// Get remaining steps in the pipeline
        /// </summary>
        public IEnumerable<BaseStep> GetRemainingSteps()
        {
            return steps.AsEnumerable();
        }
        
        #endregion
        
        #region Pipeline Processing
        
        /// <summary>
        /// Continue processing the pipeline
        /// </summary>
        public bool Continue()
        {
            try
            {
                // If no current step, get the next one
                if (currentStep == null)
                {
                    if (steps.Count == 0)
                    {
                        if (isProcessing)
                        {
                            isProcessing = false;
                            OnPipelineCompleted?.Invoke();
                        }
                        return true; // Pipeline complete
                    }
                    
                    currentStep = steps.Dequeue();
                    isProcessing = true;
                    OnStepStarted?.Invoke(currentStep);
                }
                
                // Execute current step
                bool stepCompleted = currentStep.Execute();
                
                if (stepCompleted)
                {
                    OnStepCompleted?.Invoke(currentStep);
                    currentStep = null; // Move to next step
                }
                
                return steps.Count == 0 && currentStep == null;
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(currentStep, ex);
                currentStep = null; // Skip failed step
                return false;
            }
        }
        
        /// <summary>
        /// Cancel the current step
        /// </summary>
        public void CancelStep()
        {
            if (currentStep != null)
            {
                currentStep.ForceComplete();
                currentStep = null;
            }
        }
        
        /// <summary>
        /// Skip the current step
        /// </summary>
        public void SkipCurrentStep()
        {
            if (currentStep != null)
            {
                OnStepCompleted?.Invoke(currentStep);
                currentStep = null;
            }
        }
        
        #endregion
        
        #region User Interaction Delegation
        
        /// <summary>
        /// Handle card click by delegating to current step
        /// </summary>
        public void HandleCardClicked(Player player, BaseCard card)
        {
            currentStep?.OnCardClicked(player, card);
        }
        
        /// <summary>
        /// Handle ring click by delegating to current step
        /// </summary>
        public void HandleRingClicked(Player player, Ring ring)
        {
            currentStep?.OnRingClicked(player, ring);
        }
        
        /// <summary>
        /// Handle menu command by delegating to current step
        /// </summary>
        public void HandleMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            currentStep?.OnMenuCommand(player, command, arg, uuid, method);
        }
        
        /// <summary>
        /// Handle menu command with object array
        /// </summary>
        public void HandleMenuCommand(Player player, string command, object[] args)
        {
            if (args != null && args.Length >= 3)
            {
                HandleMenuCommand(player, command, 
                    args[0]?.ToString() ?? "", 
                    args[1]?.ToString() ?? "", 
                    args[2]?.ToString() ?? "");
            }
            else
            {
                HandleMenuCommand(player, command, "", "", "");
            }
        }
        
        /// <summary>
        /// Handle province click by delegating to current step
        /// </summary>
        public void HandleProvinceClicked(Player player, BaseCard province)
        {
            currentStep?.OnCardClicked(player, province); // Treat as card click
        }
        
        /// <summary>
        /// Handle button click by delegating to current step
        /// </summary>
        public void HandleButtonClicked(Player player, string buttonId, object[] args)
        {
            // Convert to menu command
            HandleMenuCommand(player, buttonId, args);
        }
        
        #endregion
    }
}
