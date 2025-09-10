using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for game phases that manage multiple steps in a pipeline
    /// </summary>
    public class Phase : BaseStepWithPipeline
    {
        [Header("Phase Configuration")]
        [SerializeField] private string phaseName;
        
        private List<BaseStep> phaseSteps = new List<BaseStep>();

        public Phase(Game game, string name) : base(game, name)
        {
            this.phaseName = name;
        }

        /// <summary>
        /// Initialize phase with its constituent steps
        /// </summary>
        /// <param name="steps">Steps that make up this phase</param>
        public virtual void InitializePhase(List<BaseStep> steps)
        {
            pipeline.Initialize(new List<IGameStep> { new SimpleStep(game, CreatePhase) });
            
            var startStep = new SimpleStep(game, StartPhase);
            var endStep = new SimpleStep(game, EndPhase);
            
            phaseSteps.Clear();
            phaseSteps.Add(startStep);
            phaseSteps.AddRange(steps ?? new List<BaseStep>());
            phaseSteps.Add(endStep);
        }

        /// <summary>
        /// Create the phase and queue all its steps
        /// </summary>
        protected virtual bool CreatePhase()
        {
            game.RaiseEvent(EventNames.OnPhaseCreated, 
                new Dictionary<string, object> { { "phase", phaseName } }, 
                (eventData) =>
                {
                    foreach (var step in phaseSteps)
                    {
                        game.QueueStep(step);
                    }
                    return true;
                });
            
            return true;
        }

        /// <summary>
        /// Start the phase and raise appropriate events
        /// </summary>
        protected virtual bool StartPhase()
        {
            game.RaiseEvent(EventNames.OnPhaseStarted, 
                new Dictionary<string, object> { { "phase", phaseName } }, 
                (eventData) =>
                {
                    game.currentPhase = phaseName;
                    
                    if (phaseName != "setup")
                    {
                        game.AddAlert("endofround", "turn: {0} - {1} phase", 
                                     game.roundNumber, phaseName);
                    }
                    return true;
                });
            
            return true;
        }

        /// <summary>
        /// End the phase and clean up
        /// </summary>
        protected virtual bool EndPhase()
        {
            game.RaiseEvent(EventNames.OnPhaseEnded, 
                new Dictionary<string, object> { { "phase", phaseName } });
            
            game.currentPhase = "";
            
            return true;
        }

        /// <summary>
        /// Phase name property
        /// </summary>
        public string Name => phaseName;

        /// <summary>
        /// Steps that make up this phase
        /// </summary>
        public IReadOnlyList<BaseStep> Steps => phaseSteps.AsReadOnly();

        public override string GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            return $"Phase '{phaseName}' - {baseInfo} - Steps: {phaseSteps.Count}";
        }
    }
}