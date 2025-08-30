using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Interface for game steps that can be executed in the pipeline
    /// </summary>
    public interface IGameStep
    {
        /// <summary>
        /// Execute this step
        /// </summary>
        /// <returns>True to continue to next step, false to pause pipeline</returns>
        bool Execute();

        /// <summary>
        /// Check if this step is complete
        /// </summary>
        bool IsComplete { get; }
        
        /// <summary>
        /// Check if this step can be cancelled
        /// </summary>
        bool CanCancel { get; }

        /// <summary>
        /// Continue execution of this step
        /// </summary>
        bool Continue();

        /// <summary>
        /// Cancel this step if possible
        /// </summary>
        bool CancelStep();

        /// <summary>
        /// Queue a sub-step within this step
        /// </summary>
        void QueueStep(IGameStep step);

        /// <summary>
        /// Handle card being clicked
        /// </summary>
        bool OnCardClicked(Player player, BaseCard card);

        /// <summary>
        /// Handle ring being clicked
        /// </summary>
        bool OnRingClicked(Player player, Ring ring);

        /// <summary>
        /// Handle menu command
        /// </summary>
        bool OnMenuCommand(Player player, string command, string arg1, string arg2);

        /// <summary>
        /// Get debug information about this step
        /// </summary>
        string GetDebugInfo();

        /// <summary>
        /// Get the name of this step for debugging
        /// </summary>
        string StepName { get; }
    }
}
