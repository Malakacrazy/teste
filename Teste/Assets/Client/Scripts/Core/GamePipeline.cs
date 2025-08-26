using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Manages the game's execution pipeline and step processing
    /// </summary>
    public class GamePipeline : MonoBehaviour
    {
        [Header("Pipeline State")]
        public bool isProcessing = false;
        
        private Game game;
        private Queue<IGameStep> steps = new Queue<IGameStep>();
        private IGameStep currentStep = null;
        
        /// <summary>
        /// Initialize the pipeline
        /// </summary>
        public void Initialize(Game gameInstance)
        {
            game = gameInstance;
            Debug.Log("🔄 Game pipeline initialized");
        }
        
        /// <summary>
        /// Queue a step to be processed
        /// </summary>
        public void QueueStep(IGameStep step)
        {
            if (step != null)
            {
                steps.Enqueue(step);
                
                if (!isProcessing)
                {
                    ProcessNextStep();
                }
            }
        }
        
        /// <summary>
        /// Continue processing the current step or move to the next one
        /// </summary>
        public void Continue()
        {
            if (currentStep != null)
            {
                currentStep.Cleanup();
                currentStep = null;
            }
            
            ProcessNextStep();
        }
        
        /// <summary>
        /// Process the next step in the queue
        /// </summary>
        private void ProcessNextStep()
        {
            if (steps.Count == 0)
            {
                isProcessing = false;
                return;
            }
            
            isProcessing = true;
            currentStep = steps.Dequeue();
            
            try
            {
                currentStep.Initialize();
                bool shouldContinue = currentStep.Continue();
                
                if (shouldContinue)
                {
                    // Step completed immediately, process next
                    Continue();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing game step: {e.Message}");
                Continue(); // Try to continue despite error
            }
        }
        
        /// <summary>
        /// Handle card click events
        /// </summary>
        public bool HandleCardClicked(Player player, BaseCard card)
        {
            if (currentStep != null)
            {
                currentStep.OnCardClicked(player, card);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Handle ring click events
        /// </summary>
        public bool HandleRingClicked(Player player, Ring ring)
        {
            if (currentStep != null)
            {
                currentStep.OnRingClicked(player, ring);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Handle menu command events
        /// </summary>
        public bool HandleMenuCommand(Player player, string command, string uuid, string method)
        {
            if (currentStep != null)
            {
                currentStep.OnMenuCommand(player, command, uuid, uuid, method);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Clear all queued steps
        /// </summary>
        public void ClearSteps()
        {
            steps.Clear();
            if (currentStep != null)
            {
                currentStep.Cleanup();
                currentStep = null;
            }
            isProcessing = false;
        }
        
        /// <summary>
        /// Get the number of queued steps
        /// </summary>
        public int GetQueuedStepCount()
        {
            return steps.Count;
        }
        
        /// <summary>
        /// Check if the pipeline is currently processing
        /// </summary>
        public bool IsProcessing()
        {
            return isProcessing;
        }
        
        /// <summary>
        /// Get the current step being processed
        /// </summary>
        public IGameStep GetCurrentStep()
        {
            return currentStep;
        }
        
        /// <summary>
        /// Force the pipeline to stop processing (for emergency situations)
        /// </summary>
        public void ForceStop()
        {
            if (currentStep != null)
            {
                currentStep.Cleanup();
                currentStep = null;
            }
            steps.Clear();
            isProcessing = false;
            
            Debug.LogWarning("⚠️ Game pipeline force stopped");
        }
    }
}
