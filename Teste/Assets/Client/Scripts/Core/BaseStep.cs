using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game steps in the L5R pipeline system.
    /// </summary>
    [System.Serializable]
    public abstract class BaseStep : IGameStep
    {
        [Header("Base Step Configuration")]
        [SerializeField] protected Game game;
        [SerializeField] protected string stepName;
        [SerializeField] protected bool isComplete = false;
        [SerializeField] protected bool canCancel = true;
        [SerializeField] protected float timeoutDuration = 0f; // 0 = no timeout
        
        // Step state
        protected DateTime stepStartTime;
        protected bool hasStarted = false;
        protected Exception lastError;
        
        #region Properties
        
        public Game Game => game;
        public virtual string StepName => !string.IsNullOrEmpty(stepName) ? stepName : GetType().Name;
        public virtual bool IsComplete => isComplete;
        public virtual bool CanCancel => canCancel;
        public bool HasStarted => hasStarted;
        public DateTime StartTime => stepStartTime;
        public TimeSpan ElapsedTime => hasStarted ? DateTime.Now - stepStartTime : TimeSpan.Zero;
        public Exception LastError => lastError;
        public bool HasTimedOut => timeoutDuration > 0 && ElapsedTime.TotalSeconds > timeoutDuration;
        
        #endregion
        
        #region Constructors
        
        public BaseStep(Game game)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            Initialize();
        }
        
        public BaseStep(Game game, string stepName) : this(game)
        {
            this.stepName = stepName;
        }
        
        #endregion
        
        #region IGameStep Implementation
        
        public virtual bool Execute()
        {
            try
            {
                if (!hasStarted)
                {
                    StartStep();
                }
                
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
        
        public virtual bool Continue()
        {
            return true;
        }
        
        bool IGameStep.IsComplete() => IsComplete;
        
        public virtual void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Default implementation - no handling
        }
        
        public virtual void OnCardClicked(Player player, BaseCard card)
        {
            // Default implementation - no handling
        }
        
        public virtual void OnRingClicked(Player player, Ring ring)
        {
            // Default implementation - no handling
        }
        
        public virtual void Initialize()
        {
            isComplete = false;
            hasStarted = false;
            lastError = null;
        }
        
        public virtual void Cleanup()
        {
            // Override in derived classes
        }
        
        public virtual string GetDebugInfo()
        {
            var info = $"{StepName}";
            
            if (HasStarted)
            {
                info += $" (Running {ElapsedTime.TotalSeconds:F1}s)";
            }
            
            if (IsComplete)
            {
                info += " [Complete]";
            }
            
            if (LastError != null)
            {
                info += " [Error]";
            }
            
            return info;
        }
        
        #endregion
        
        #region Internal Step Management
        
        protected void StartStep()
        {
            hasStarted = true;
            stepStartTime = DateTime.Now;
            LogStep("Step started");
            OnStepStart();
        }
        
        protected void CompleteStep()
        {
            isComplete = true;
            LogStep($"Step completed (took {ElapsedTime.TotalMilliseconds:F0}ms)");
            OnStepComplete();
        }
        
        protected void HandleError(Exception ex)
        {
            lastError = ex;
            isComplete = true;
            LogError($"Step error: {ex.Message}");
        }
        
        protected void HandleTimeout()
        {
            isComplete = true;
            LogWarning($"Step timed out after {timeoutDuration} seconds");
        }
        
        #endregion
        
        #region Step Lifecycle Methods
        
        protected virtual void OnStepStart()
        {
            // Override in derived classes
        }
        
        protected virtual void OnStepComplete()
        {
            // Override in derived classes
        }
        
        #endregion
        
        #region Utility Methods
        
        public virtual bool CanPlayerInteract(Player player)
        {
            return player != null && !IsComplete && HasStarted;
        }
        
        public virtual Player GetActivePlayer()
        {
            return game?.GetActivePlayer();
        }
        
        public virtual void ForceComplete()
        {
            if (!IsComplete)
            {
                isComplete = true;
                LogStep("Step force completed");
                OnStepComplete();
            }
        }
        
        #endregion
        
        #region Logging
        
        protected virtual void LogStep(string message)
        {
            Debug.Log($"🔄 {StepName}: {message}");
        }
        
        protected virtual void LogWarning(string message)
        {
            Debug.LogWarning($"⚠️ {StepName}: {message}");
        }
        
        protected virtual void LogError(string message)
        {
            Debug.LogError($"❌ {StepName}: {message}");
        }
        
        #endregion
        
        #region Overrides
        
        public override string ToString()
        {
            return GetDebugInfo();
        }
        
        #endregion
    }
}
