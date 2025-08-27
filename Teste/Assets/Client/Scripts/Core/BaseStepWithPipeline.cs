using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base step that includes an internal pipeline for managing sub-steps.
    /// Allows complex steps to be composed of multiple smaller steps while maintaining
    /// the same interface as a simple BaseStep.
    /// </summary>
    [System.Serializable]
    public abstract class BaseStepWithPipeline : BaseStep
    {
        [Header("Pipeline Configuration")]
        [SerializeField] protected GamePipeline pipeline;
        [SerializeField] protected bool autoInitializePipeline = true;
        [SerializeField] protected bool debugPipelineSteps = false;
        
        // Pipeline state
        protected bool pipelineInitialized = false;
        protected int completedSteps = 0;
        protected int totalSteps = 0;
        
        // Events specific to pipeline steps
        public event Action<BaseStepWithPipeline, BaseStep> OnSubStepStarted;
        public event Action<BaseStepWithPipeline, BaseStep> OnSubStepCompleted;
        public event Action<BaseStepWithPipeline, BaseStep, Exception> OnSubStepError;
        
        #region Properties
        
        /// <summary>
        /// The internal pipeline managing sub-steps
        /// </summary>
        public GamePipeline Pipeline => pipeline;
        
        /// <summary>
        /// Number of steps in the pipeline
        /// </summary>
        public int PipelineLength => pipeline?.Length ?? 0;
        
        /// <summary>
        /// Whether the pipeline is empty (and thus complete)
        /// </summary>
        public override bool IsComplete => pipeline?.Length == 0;
        
        /// <summary>
        /// Current sub-step being executed
        /// </summary>
        public BaseStep CurrentSubStep => pipeline?.CurrentStep;
        
        /// <summary>
        /// Progress through the pipeline (0.0 to 1.0)
        /// </summary>
        public float PipelineProgress 
        { 
            get 
            { 
                if (totalSteps == 0) return 1.0f;
                return (float)completedSteps / totalSteps;
            } 
        }
        
        /// <summary>
        /// Whether the pipeline has been initialized
        /// </summary>
        public bool IsPipelineInitialized => pipelineInitialized;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Constructor for BaseStepWithPipeline
        /// </summary>
        /// <param name="game">Game instance</param>
        public BaseStepWithPipeline(Game game) : base(game)
        {
            InitializePipelineSystem();
        }
        
        /// <summary>
        /// Constructor with custom step name
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="stepName">Custom step name</param>
        public BaseStepWithPipeline(Game game, string stepName) : base(game, stepName)
        {
            InitializePipelineSystem();
        }
        
        #endregion
        
        #region Pipeline System Initialization
        
        /// <summary>
        /// Initialize the pipeline system
        /// </summary>
        private void InitializePipelineSystem()
        {
            pipeline = new GamePipeline();
            
            // Subscribe to pipeline events
            pipeline.OnStepStarted += OnPipelineStepStarted;
            pipeline.OnStepCompleted += OnPipelineStepCompleted;
            pipeline.OnStepError += OnPipelineStepError;
            pipeline.OnPipelineCompleted += OnPipelineCompleted;
            
            if (autoInitializePipeline)
            {
                InitializePipeline();
            }
        }
        
        /// <summary>
        /// Initialize the pipeline with steps. Override in derived classes.
        /// </summary>
        protected virtual void InitializePipeline()
        {
            // Override in derived classes to set up the pipeline
            pipelineInitialized = true;
        }
        
        /// <summary>
        /// Force pipeline initialization if not done automatically
        /// </summary>
        protected void EnsurePipelineInitialized()
        {
            if (!pipelineInitialized)
            {
                InitializePipeline();
                totalSteps = PipelineLength;
            }
        }
        
        #endregion
        
        #region Step Execution Override
        
        /// <summary>
        /// Execute this step by processing the internal pipeline
        /// </summary>
        /// <returns>True if step (and pipeline) completed successfully</returns>
        public override bool Execute()
        {
            try
            {
                if (!hasStarted)
                {
                    StartStep();
                    EnsurePipelineInitialized();
                    totalSteps = PipelineLength;
                }
                
                // Check for timeout
                if (HasTimedOut)
                {
                    HandleTimeout();
                    return true;
                }
                
                // Process pipeline
                bool completed = Continue();
                
                if (completed)
                {
                    CompleteStep();
                }
                
                return completed;
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return true;
            }
        }
        
        /// <summary>
        /// Continue pipeline execution
        /// </summary>
        /// <returns>True if pipeline is complete</returns>
        public override bool Continue()
        {
            try
            {
                EnsurePipelineInitialized();
                
                if (debugPipelineSteps && pipeline.CurrentStep != null)
                {
                    LogStep($"Processing sub-step: {pipeline.CurrentStep.GetDebugInfo()}");
                }
                
                return pipeline.Continue();
            }
            catch (Exception ex)
            {
                LogError($"Pipeline execution error: {ex.Message}");
                game.ReportError(ex);
                
                // Try to continue despite the error
                return IsComplete;
            }
        }
        
        #endregion
        
        #region Pipeline Management
        
        /// <summary>
        /// Queue a step in the pipeline
        /// </summary>
        /// <param name="step">Step to queue</param>
        public virtual void QueueStep(BaseStep step)
        {
            if (step == null)
            {
                LogWarning("Attempted to queue null step");
                return;
            }
            
            pipeline.QueueStep(step);
            
            if (debugPipelineSteps)
            {
                LogStep($"Queued sub-step: {step.GetDebugInfo()}");
            }
            
            // Update total steps if we're tracking progress
            if (hasStarted)
            {
                totalSteps = PipelineLength;
            }
        }
        
        /// <summary>
        /// Queue multiple steps at once
        /// </summary>
        /// <param name="steps">Steps to queue</param>
        public virtual void QueueSteps(params BaseStep[] steps)
        {
            foreach (var step in steps.Where(s => s != null))
            {
                QueueStep(step);
            }
        }
        
        /// <summary>
        /// Queue multiple steps from collection
        /// </summary>
        /// <param name="steps">Steps to queue</param>
        public virtual void QueueSteps(IEnumerable<BaseStep> steps)
        {
            foreach (var step in steps.Where(s => s != null))
            {
                QueueStep(step);
            }
        }
        
        /// <summary>
        /// Insert step at the front of the pipeline
        /// </summary>
        /// <param name="step">Step to insert</param>
        public virtual void InsertStep(BaseStep step)
        {
            if (step == null)
            {
                LogWarning("Attempted to insert null step");
                return;
            }
            
            pipeline.InsertStep(step);
            
            if (debugPipelineSteps)
            {
                LogStep($"Inserted sub-step: {step.GetDebugInfo()}");
            }
            
            if (hasStarted)
            {
                totalSteps = PipelineLength;
            }
        }
        
        /// <summary>
        /// Cancel the current step in the pipeline
        /// </summary>
        public virtual void CancelCurrentStep()
        {
            if (pipeline.CurrentStep != null)
            {
                LogStep($"Cancelling current sub-step: {pipeline.CurrentStep.GetDebugInfo()}");
                pipeline.CancelStep();
            }
        }
        
        /// <summary>
        /// Cancel all remaining steps in the pipeline
        /// </summary>
        public virtual void CancelPipeline()
        {
            LogStep("Cancelling entire pipeline");
            pipeline.Clear();
        }
        
        /// <summary>
        /// Skip the current step and move to the next
        /// </summary>
        public virtual void SkipCurrentStep()
        {
            if (pipeline.CurrentStep != null)
            {
                LogStep($"Skipping current sub-step: {pipeline.CurrentStep.GetDebugInfo()}");
                pipeline.SkipCurrentStep();
            }
        }
        
        #endregion
        
        #region User Interaction Delegation
        
        /// <summary>
        /// Handle card clicks by delegating to the pipeline
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="card">Card that was clicked</param>
        public override void OnCardClicked(Player player, BaseCard card)
        {
            try
            {
                EnsurePipelineInitialized();
                
                pipeline.HandleCardClicked(player, card);
                
                if (debugPipelineSteps)
                {
                    LogStep($"Card click handled by sub-step: {card.name}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling card click: {ex.Message}");
                game.ReportError(ex);
            }
        }
        
        /// <summary>
        /// Handle ring clicks by delegating to the pipeline
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="ring">Ring that was clicked</param>
        public override void OnRingClicked(Player player, Ring ring)
        {
            try
            {
                EnsurePipelineInitialized();
                
                pipeline.HandleRingClicked(player, ring);
                
                if (debugPipelineSteps)
                {
                    LogStep($"Ring click handled by sub-step: {ring.element}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling ring click: {ex.Message}");
                game.ReportError(ex);
            }
        }
        
        /// <summary>
        /// Handle menu commands by delegating to the pipeline
        /// </summary>
        /// <param name="player">Player who issued command</param>
        /// <param name="command">Command issued</param>
        /// <param name="arg">Argument</param>
        /// <param name="uuid">UUID</param>
        /// <param name="method">Method</param>
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            try
            {
                EnsurePipelineInitialized();
                
                pipeline.HandleMenuCommand(player, command, new object[] { arg, uuid, method });
                
                if (debugPipelineSteps)
                {
                    LogStep($"Menu command handled by sub-step: {command}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling menu command: {ex.Message}");
                game.ReportError(ex);
            }
        }
        
        /// <summary>
        /// Handle province click events. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="province">Province that was clicked</param>
        public virtual void OnProvinceClicked(Player player, BaseCard province)
        {
            try
            {
                EnsurePipelineInitialized();
                
                pipeline.HandleProvinceClicked(player, province);
                
                if (debugPipelineSteps)
                {
                    LogStep($"Province click handled by sub-step: {province.name}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling province click: {ex.Message}");
                game.ReportError(ex);
            }
        }
        
        /// <summary>
        /// Handle button click events. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="buttonId">Button ID</param>
        /// <param name="args">Additional arguments</param>
        public virtual void OnButtonClicked(Player player, string buttonId, object[] args = null)
        {
            try
            {
                EnsurePipelineInitialized();
                
                pipeline.HandleButtonClicked(player, buttonId, args);
                
                if (debugPipelineSteps)
                {
                    LogStep($"Button click handled by sub-step: {buttonId}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling button click: {ex.Message}");
                game.ReportError(ex);
            }
        }
        
        #endregion
        
        #region Pipeline Event Handlers
        
        /// <summary>
        /// Called when a sub-step starts
        /// </summary>
        /// <param name="step">Step that started</param>
        private void OnPipelineStepStarted(BaseStep step)
        {
            if (debugPipelineSteps)
            {
                LogStep($"Sub-step started: {step.GetDebugInfo()}");
            }
            
            OnSubStepStarted?.Invoke(this, step);
        }
        
        /// <summary>
        /// Called when a sub-step completes
        /// </summary>
        /// <param name="step">Step that completed</param>
        private void OnPipelineStepCompleted(BaseStep step)
        {
            completedSteps++;
            
            if (debugPipelineSteps)
            {
                LogStep($"Sub-step completed: {step.GetDebugInfo()} ({completedSteps}/{totalSteps})");
            }
            
            OnSubStepCompleted?.Invoke(this, step);
        }
        
        /// <summary>
        /// Called when a sub-step encounters an error
        /// </summary>
        /// <param name="step">Step that had error</param>
        /// <param name="exception">The exception</param>
        private void OnPipelineStepError(BaseStep step, Exception exception)
        {
            LogError($"Sub-step error in {step.GetDebugInfo()}: {exception.Message}");
            OnSubStepError?.Invoke(this, step, exception);
        }
        
        /// <summary>
        /// Called when the entire pipeline completes
        /// </summary>
        private void OnPipelineCompleted()
        {
            if (debugPipelineSteps)
            {
                LogStep("Pipeline completed");
            }
        }
        
        #endregion
        
        #region Cancellation Override
        
        /// <summary>
        /// Cancel this step and its pipeline
        /// </summary>
        /// <returns>True if cancelled successfully</returns>
        public virtual bool CancelStep()
        {
            if (!CanCancel || IsComplete)
                return false;
            
            try
            {
                // Cancel current pipeline step
                CancelCurrentStep();
                
                // Clear remaining pipeline
                CancelPipeline();
                
                // Mark as complete
                isComplete = true;
                LogStep("Pipeline step cancelled");
                
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error during cancellation: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Reset Override
        
        /// <summary>
        /// Reset this step and reinitialize the pipeline
        /// </summary>
        public virtual void ResetPipeline()
        {
            // Reset step state
            isComplete = false;
            hasStarted = false;
            
            // Reset pipeline state
            completedSteps = 0;
            totalSteps = 0;
            pipelineInitialized = false;
            
            // Clear and reinitialize pipeline
            pipeline.Clear();
            
            if (autoInitializePipeline)
            {
                InitializePipeline();
            }
            
            LogStep("Pipeline step reset");
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information including pipeline state
        /// </summary>
        /// <returns>Debug information string</returns>
        public override string GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            if (PipelineLength > 0)
            {
                baseInfo += $" (Pipeline: {completedSteps}/{totalSteps})";
                
                if (CurrentSubStep != null)
                {
                    baseInfo += $" [Current: {CurrentSubStep.StepName}]";
                }
            }
            else
            {
                baseInfo += " (Pipeline: Empty)";
            }
            
            return baseInfo;
        }
        
        /// <summary>
        /// Get detailed debug information including pipeline contents
        /// </summary>
        /// <returns>Detailed debug information</returns>
        public virtual string GetDetailedPipelineDebugInfo()
        {
            var info = GetDebugInfo();
            
            info += "\nPipeline Information:\n";
            info += $"  Initialized: {pipelineInitialized}\n";
            info += $"  Length: {PipelineLength}\n";
            info += $"  Progress: {PipelineProgress:P1}\n";
            info += $"  Completed Steps: {completedSteps}/{totalSteps}\n";
            
            if (CurrentSubStep != null)
            {
                info += $"  Current Step: {CurrentSubStep.GetDebugInfo()}\n";
            }
            
            // List remaining steps
            var remainingSteps = pipeline.GetRemainingSteps();
            if (remainingSteps.Any())
            {
                info += "  Remaining Steps:\n";
                foreach (var step in remainingSteps.Take(5)) // Show first 5
                {
                    info += $"    - {step.GetDebugInfo()}\n";
                }
                
                if (remainingSteps.Count() > 5)
                {
                    info += $"    ... and {remainingSteps.Count() - 5} more\n";
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// Enable or disable pipeline debug logging
        /// </summary>
        /// <param name="enabled">Whether to enable debug logging</param>
        public void SetPipelineDebugging(bool enabled)
        {
            debugPipelineSteps = enabled;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for BaseStepWithPipeline
    /// </summary>
    public static class BaseStepWithPipelineExtensions
    {
        /// <summary>
        /// Check if pipeline has any steps
        /// </summary>
        public static bool HasSteps(this BaseStepWithPipeline step)
        {
            return step.PipelineLength > 0;
        }
        
        /// <summary>
        /// Get pipeline completion percentage
        /// </summary>
        public static float GetCompletionPercentage(this BaseStepWithPipeline step)
        {
            return step.PipelineProgress * 100f;
        }
        
        /// <summary>
        /// Check if pipeline is currently processing a step
        /// </summary>
        public static bool IsProcessingStep(this BaseStepWithPipeline step)
        {
            return step.CurrentSubStep != null && step.CurrentSubStep.HasStarted && !step.CurrentSubStep.IsComplete;
        }
        
        /// <summary>
        /// Get remaining step count
        /// </summary>
        public static int GetRemainingStepCount(this BaseStepWithPipeline step)
        {
            return step.PipelineLength;
        }
    }
}
