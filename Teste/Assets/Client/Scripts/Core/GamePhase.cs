using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game phases
    /// </summary>
    public abstract class GamePhase : BaseStepWithPipeline
    {
        [Header("Phase Properties")]
        [SerializeField] protected string phaseName;
        [SerializeField] protected bool phaseActive = false;
        [SerializeField] protected float phaseStartDelay = 0f;

        // Phase events
        public event Action<GamePhase> OnPhaseStarted;
        public event Action<GamePhase> OnPhaseEnded;
        public event Action<GamePhase, string> OnPhaseMessageAdded;

        protected List<IGameStep> steps = new List<IGameStep>();

        public string PhaseName => phaseName;
        public bool IsActive => phaseActive;

        protected GamePhase(Game game, string phaseName) : base(game, phaseName)
        {
            this.phaseName = phaseName;
        }

        public virtual void StartPhase()
        {
            phaseActive = true;
            game.currentPhase = phaseName;
            OnPhaseStarted?.Invoke(this);
        }

        public virtual void EndPhase()
        {
            phaseActive = false;
            OnPhaseEnded?.Invoke(this);
        }

        protected virtual void ExecutePythonScript(string methodName, params object[] parameters)
        {
            // Default implementation
        }
    }
}
