using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a game phase in L5R with its own pipeline of steps.
    /// Phases have proper start/end events and can contain multiple sub-steps.
    /// </summary>
    [System.Serializable]
    public class GamePhase : BaseStepWithPipeline
    {
        [Header("Phase Configuration")]
        [SerializeField] protected string phaseName;
        [SerializeField] protected List<BaseStep> phaseSteps = new List<BaseStep>();
        [SerializeField] protected bool skipPhaseEvents = false;
        [SerializeField] protected bool isSetupPhase = false;
        
        // Phase state
        protected DateTime phaseStartTime;
        protected bool phaseStarted = false;
        protected bool phaseEnded = false;
        
        // Events specific to phases
        public event Action<GamePhase> OnPhaseCreated;
        public event Action<GamePhase> OnPhaseStarted;
        public event Action<GamePhase> OnPhaseEnded;
        public event Action<GamePhase, BaseStep> OnPhaseStepAdded;
        
        #region Properties
        
        /// <summary>
        /// Name of this phase
        /// </summary>
        public string Name => phaseName;
        
        /// <summary>
        /// Steps that make up this phase
        /// </summary>
        public IReadOnlyList<BaseStep> Steps => phaseSteps.AsReadOnly();
        
        /// <summary>
        /// Whether this phase has started
        /// </summary>
        public bool HasPhaseStarted => phaseStarted;
        
        /// <summary>
        /// Whether this phase has ended
        /// </summary>
        public bool HasPhaseEnded => phaseEnded;
        
        /// <summary>
        /// Time when phase started
        /// </summary>
        public DateTime PhaseStartTime => phaseStartTime;
        
        /// <summary>
        /// How long this phase has been running
        /// </summary>
        public TimeSpan PhaseElapsedTime => phaseStarted ? DateTime.Now - phaseStartTime : TimeSpan.Zero;
        
        /// <summary>
        /// Whether this is the setup phase
        /// </summary>
        public bool IsSetupPhase
        {
            get => isSetupPhase;
            set => isSetupPhase = value;
        }
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Create a new game phase
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="phaseName">Name of the phase</param>
        public GamePhase(Game game, string phaseName) : base(game, $"Phase: {phaseName}")
        {
            this.phaseName = phaseName ?? throw new ArgumentNullException(nameof(phaseName));
            this.isSetupPhase = phaseName.ToLower() == "setup";
            Initialize();
        }
        
        /// <summary>
        /// Create a new game phase with steps
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="phaseName">Name of the phase</param>
        /// <param name="steps">Steps that make up this phase</param>
        public GamePhase(Game game, string phaseName, params BaseStep[] steps) : this(game, phaseName)
        {
            if (steps != null)
            {
                phaseSteps.AddRange(steps);
            }
        }
        
        /// <summary>
        /// Create a new game phase with step list
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="phaseName">Name of the phase</param>
        /// <param name="steps">List of steps that make up this phase</param>
        public GamePhase(Game game, string phaseName, List<BaseStep> steps) : this(game, phaseName)
        {
            if (steps != null)
            {
                phaseSteps.AddRange(steps);
            }
        }
        
        #endregion
        
        #region Phase Pipeline Setup
        
        /// <summary>
        /// Initialize the phase pipeline with the provided steps
        /// </summary>
        /// <param name="steps">Steps to include in this phase</param>
        public virtual void InitializePhase(List<BaseStep> steps = null)
        {
            if (steps != null)
            {
                phaseSteps.Clear();
                phaseSteps.AddRange(steps);
            }
            
            InitializePipeline();
        }
        
        /// <summary>
        /// Initialize the pipeline with phase creation, start, steps, and end
        /// </summary>
        protected override void InitializePipeline()
        {
            if (pipelineInitialized)
                return;
            
            // Clear existing pipeline
            pipeline.Clear();
            
            // Step 1: Create phase (triggers phase creation events)
            QueueStep(new SimpleStep(game, CreatePhase, "Create Phase"));
            
            pipelineInitialized = true;
            LogPhase("Phase pipeline initialized");
        }
        
        /// <summary>
        /// Create the phase and queue all its steps
        /// </summary>
        protected virtual bool CreatePhase()
        {
            try
            {
                LogPhase($"Creating phase: {phaseName}");
                
                // Raise phase created event
                if (!skipPhaseEvents)
                {
                    game.RaiseEvent(EventNames.OnPhaseCreated, new Dictionary<string, object> { { "phase", phaseName } }, 
                        () => { QueueAllPhaseSteps(); return true; });
                }
                else
                {
                    QueueAllPhaseSteps();
                }
                
                OnPhaseCreated?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error creating phase {phaseName}: {ex.Message}");
                return true; // Complete with error
            }
        }
        
        /// <summary>
        /// Queue all steps that make up this phase
        /// </summary>
        protected virtual void QueueAllPhaseSteps()
        {
            // Step 1: Phase start
            var startStep = new SimpleStep(game, StartPhase, "Start Phase");
            QueueStep(startStep);
            
            // Step 2: Phase-specific steps
            foreach (var step in phaseSteps)
            {
                if (step != null)
                {
                    QueueStep(step);
                    LogPhase($"Queued phase step: {step.StepName}");
                }
            }
            
            // Step 3: Phase end
            var endStep = new SimpleStep(game, EndPhase, "End Phase");
            QueueStep(endStep);
            
            LogPhase($"Queued {phaseSteps.Count + 2} steps for phase {phaseName}");
        }
        
        #endregion
        
        #region Phase Lifecycle
        
        /// <summary>
        /// Start the phase
        /// </summary>
        protected virtual bool StartPhase()
        {
            try
            {
                if (phaseStarted)
                {
                    LogWarning($"Phase {phaseName} already started");
                    return true;
                }
                
                phaseStarted = true;
                phaseStartTime = DateTime.Now;
                
                LogPhase($"Starting phase: {phaseName}");
                
                // Raise phase started event
                if (!skipPhaseEvents)
                {
                    game.RaiseEvent(EventNames.OnPhaseStarted, new Dictionary<string, object> { { "phase", phaseName } }, 
                        () => { OnPhaseStartLogic(); return true; });
                }
                else
                {
                    OnPhaseStartLogic();
                }
                
                OnPhaseStarted?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error starting phase {phaseName}: {ex.Message}");
                return true; // Complete with error
            }
        }
        
        /// <summary>
        /// Logic to execute when phase starts
        /// </summary>
        protected virtual void OnPhaseStartLogic()
        {
            // Set current phase in game
            game.currentPhase = phaseName;
            
            // Add turn alert (except for setup phase)
            if (!isSetupPhase && phaseName.ToLower() != "setup")
            {
                game.AddAlert("endofround", $"turn: {game.roundNumber} - {phaseName} phase");
            }
            
            LogPhase($"Phase {phaseName} is now active");
        }
        
        /// <summary>
        /// End the phase
        /// </summary>
        protected virtual bool EndPhase()
        {
            try
            {
                if (phaseEnded)
                {
                    LogWarning($"Phase {phaseName} already ended");
                    return true;
                }
                
                phaseEnded = true;
                var phaseDuration = PhaseElapsedTime;
                
                LogPhase($"Ending phase: {phaseName} (Duration: {phaseDuration.TotalSeconds:F1}s)");
                
                // Raise phase ended event
                if (!skipPhaseEvents)
                {
                    game.RaiseEvent(EventNames.OnPhaseEnded, new Dictionary<string, object> { { "phase", phaseName } });
                }
                
                // Clear current phase in game
                if (game.currentPhase == phaseName)
                {
                    game.currentPhase = "";
                }
                
                OnPhaseEnded?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error ending phase {phaseName}: {ex.Message}");
                return true; // Complete with error
            }
        }
        
        #endregion
        
        #region Step Management
        
        /// <summary>
        /// Add a step to this phase
        /// </summary>
        /// <param name="step">Step to add</param>
        public virtual void AddStep(BaseStep step)
        {
            if (step == null)
            {
                LogWarning("Attempted to add null step to phase");
                return;
            }
            
            if (phaseStarted)
            {
                LogWarning($"Cannot add step to phase {phaseName} - phase already started");
                return;
            }
            
            phaseSteps.Add(step);
            OnPhaseStepAdded?.Invoke(this, step);
            
            LogPhase($"Added step to phase {phaseName}: {step.StepName}");
        }
        
        /// <summary>
        /// Add multiple steps to this phase
        /// </summary>
        /// <param name="steps">Steps to add</param>
        public virtual void AddSteps(params BaseStep[] steps)
        {
            if (steps != null)
            {
                foreach (var step in steps.Where(s => s != null))
                {
                    AddStep(step);
                }
            }
        }
        
        /// <summary>
        /// Insert a step at a specific position
        /// </summary>
        /// <param name="index">Index to insert at</param>
        /// <param name="step">Step to insert</param>
        public virtual void InsertStep(int index, BaseStep step)
        {
            if (step == null)
            {
                LogWarning("Attempted to insert null step to phase");
                return;
            }
            
            if (phaseStarted)
            {
                LogWarning($"Cannot insert step to phase {phaseName} - phase already started");
                return;
            }
            
            index = Mathf.Clamp(index, 0, phaseSteps.Count);
            phaseSteps.Insert(index, step);
            
            OnPhaseStepAdded?.Invoke(this, step);
            LogPhase($"Inserted step at index {index} in phase {phaseName}: {step.StepName}");
        }
        
        /// <summary>
        /// Remove a step from this phase
        /// </summary>
        /// <param name="step">Step to remove</param>
        /// <returns>True if step was removed</returns>
        public virtual bool RemoveStep(BaseStep step)
        {
            if (phaseStarted)
            {
                LogWarning($"Cannot remove step from phase {phaseName} - phase already started");
                return false;
            }
            
            bool removed = phaseSteps.Remove(step);
            if (removed)
            {
                LogPhase($"Removed step from phase {phaseName}: {step?.StepName}");
            }
            
            return removed;
        }
        
        /// <summary>
        /// Clear all steps from this phase
        /// </summary>
        public virtual void ClearSteps()
        {
            if (phaseStarted)
            {
                LogWarning($"Cannot clear steps from phase {phaseName} - phase already started");
                return;
            }
            
            int removedCount = phaseSteps.Count;
            phaseSteps.Clear();
            
            LogPhase($"Cleared {removedCount} steps from phase {phaseName}");
        }
        
        #endregion
        
        #region Phase Control
        
        /// <summary>
        /// Skip this phase (mark as complete without executing steps)
        /// </summary>
        public virtual void SkipPhase()
        {
            if (phaseEnded)
            {
                LogWarning($"Phase {phaseName} already ended, cannot skip");
                return;
            }
            
            LogPhase($"Skipping phase: {phaseName}");
            
            // Clear remaining pipeline steps
            CancelPipeline();
            
            // Mark as started and ended
            if (!phaseStarted)
            {
                phaseStarted = true;
                phaseStartTime = DateTime.Now;
            }
            
            phaseEnded = true;
            
            // Clear current phase
            if (game.currentPhase == phaseName)
            {
                game.currentPhase = "";
            }
            
            // Force completion
            ForceComplete();
        }
        
        /// <summary>
        /// Pause this phase
        /// </summary>
        public virtual void PausePhase()
        {
            if (!phaseStarted || phaseEnded)
            {
                LogWarning($"Cannot pause phase {phaseName} - not in correct state");
                return;
            }
            
            // Cancel current step in pipeline
            CancelCurrentStep();
            
            LogPhase($"Paused phase: {phaseName}");
        }
        
        /// <summary>
        /// Resume this phase
        /// </summary>
        public virtual void ResumePhase()
        {
            if (!phaseStarted || phaseEnded)
            {
                LogWarning($"Cannot resume phase {phaseName} - not in correct state");
                return;
            }
            
            LogPhase($"Resumed phase: {phaseName}");
            
            // Phase will continue with next step in pipeline automatically
        }
        
        #endregion
        
        #region Reset Override
        
        /// <summary>
        /// Reset this phase to be executed again
        /// </summary>
        public virtual void Reset()
        {
            
            // Reset phase-specific state
            phaseStarted = false;
            phaseEnded = false;
            phaseStartTime = default(DateTime);
            
            LogPhase($"Phase {phaseName} reset");
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information about this phase
        /// </summary>
        /// <returns>Debug information string</returns>
        public override string GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            var phaseInfo = $" [Phase: {phaseName}";
            
            if (phaseStarted && !phaseEnded)
            {
                phaseInfo += $", Running {PhaseElapsedTime.TotalSeconds:F1}s";
            }
            else if (phaseEnded)
            {
                phaseInfo += ", Complete";
            }
            else
            {
                phaseInfo += ", Not Started";
            }
            
            phaseInfo += $", Steps: {phaseSteps.Count}]";
            
            return baseInfo + phaseInfo;
        }
        
        /// <summary>
        /// Get detailed debug information about this phase
        /// </summary>
        /// <returns>Detailed debug information</returns>
        public virtual string GetDetailedDebugInfo()
        {
            var info = "";
            
            info += "\nPhase Information:\n";
            info += $"  Phase Name: {phaseName}\n";
            info += $"  Is Setup Phase: {isSetupPhase}\n";
            info += $"  Phase Started: {phaseStarted}\n";
            info += $"  Phase Ended: {phaseEnded}\n";
            
            if (phaseStarted)
            {
                info += $"  Start Time: {phaseStartTime:HH:mm:ss.fff}\n";
                info += $"  Elapsed Time: {PhaseElapsedTime.TotalSeconds:F1}s\n";
            }
            
            info += $"  Phase Steps: {phaseSteps.Count}\n";
            foreach (var step in phaseSteps.Take(5)) // Show first 5
            {
                info += $"    - {step.GetDebugInfo()}\n";
            }
            
            if (phaseSteps.Count > 5)
            {
                info += $"    ... and {phaseSteps.Count - 5} more\n";
            }
            
            return info;
        }
        
        /// <summary>
        /// Get phase statistics
        /// </summary>
        /// <returns>Phase statistics</returns>
        public virtual PhaseStatistics GetPhaseStatistics()
        {
            return new PhaseStatistics
            {
                PhaseName = phaseName,
                IsSetupPhase = isSetupPhase,
                HasStarted = phaseStarted,
                HasEnded = phaseEnded,
                ElapsedTime = PhaseElapsedTime,
                TotalSteps = phaseSteps.Count,
                CompletedSteps = completedSteps,
                PipelineLength = PipelineLength,
                Progress = PipelineProgress
            };
        }
        
        /// <summary>
        /// Log phase message
        /// </summary>
        /// <param name="message">Message to log</param>
        protected virtual void LogPhase(string message)
        {
            Debug.Log($"🎯 GamePhase[{phaseName}]: {message}");
        }
        
        /// <summary>
        /// Log phase warning
        /// </summary>
        /// <param name="message">Warning message to log</param>
        protected virtual void LogWarning(string message)
        {
            Debug.LogWarning($"⚠️ GamePhase[{phaseName}]: {message}");
        }
        
        /// <summary>
        /// Log phase error
        /// </summary>
        /// <param name="message">Error message to log</param>
        protected virtual void LogError(string message)
        {
            Debug.LogError($"❌ GamePhase[{phaseName}]: {message}");
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a simple phase with basic steps
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="phaseName">Phase name</param>
        /// <param name="steps">Phase steps</param>
        /// <returns>New game phase</returns>
        public static GamePhase CreateSimplePhase(Game game, string phaseName, params BaseStep[] steps)
        {
            return new GamePhase(game, phaseName, steps);
        }
        
        /// <summary>
        /// Create a phase with a single action
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="phaseName">Phase name</param>
        /// <param name="action">Action to execute</param>
        /// <returns>New game phase</returns>
        public static GamePhase CreateActionPhase(Game game, string phaseName, System.Func<bool> action)
        {
            var step = new SimpleStep(game, action, $"{phaseName} Action");
            return new GamePhase(game, phaseName, step);
        }
        
        /// <summary>
        /// Create an empty phase (for dynamic step addition)
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="phaseName">Phase name</param>
        /// <returns>New empty game phase</returns>
        public static GamePhase CreateEmptyPhase(Game game, string phaseName)
        {
            return new GamePhase(game, phaseName);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Statistics for phase monitoring
    /// </summary>
    [System.Serializable]
    public class PhaseStatistics
    {
        public string PhaseName;
        public bool IsSetupPhase;
        public bool HasStarted;
        public bool HasEnded;
        public TimeSpan ElapsedTime;
        public int TotalSteps;
        public int CompletedSteps;
        public int PipelineLength;
        public float Progress;
        
        public override string ToString()
        {
            return $"Phase[{PhaseName}]: {Progress:P1} complete ({CompletedSteps}/{TotalSteps} steps)";
        }
    }
    
    /// <summary>
    /// Extension methods for GamePhase
    /// </summary>
    public static class GamePhaseExtensions
    {
        /// <summary>
        /// Check if phase is currently active
        /// </summary>
        public static bool IsActive(this GamePhase phase)
        {
            return phase.HasPhaseStarted && !phase.HasPhaseEnded;
        }
        
        /// <summary>
        /// Check if phase is complete
        /// </summary>
        public static bool IsComplete(this GamePhase phase)
        {
            return phase.HasPhaseEnded || phase.IsComplete;
        }
        
        /// <summary>
        /// Get phase duration in seconds
        /// </summary>
        public static double GetDurationInSeconds(this GamePhase phase)
        {
            return phase.PhaseElapsedTime.TotalSeconds;
        }
        
        /// <summary>
        /// Check if phase has any steps
        /// </summary>
        public static bool HasSteps(this GamePhase phase)
        {
            return phase.Steps.Count > 0;
        }
        
        /// <summary>
        /// Get remaining steps count
        /// </summary>
        public static int GetRemainingStepsCount(this GamePhase phase)
        {
            return phase.PipelineLength;
        }
        
        /// <summary>
        /// Check if phase can be modified (add/remove steps)
        /// </summary>
        public static bool CanBeModified(this GamePhase phase)
        {
            return !phase.HasPhaseStarted;
        }
        
        /// <summary>
        /// Clone this phase with a new name
        /// </summary>
        public static GamePhase CloneWithName(this GamePhase phase, string newName)
        {
            var clone = new GamePhase(phase.Game, newName);
            
            // Copy steps
            foreach (var step in phase.Steps)
            {
                clone.AddStep(step);
            }
            
            clone.IsSetupPhase = phase.IsSetupPhase;
            
            return clone;
        }
    }
}
