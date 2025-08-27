using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Game pipeline for managing game flow and steps
    /// </summary>
    public class GamePipeline : MonoBehaviour
    {
        private Game game;
        private Queue<IGameStep> steps = new Queue<IGameStep>();
        private IGameStep currentStep;
        private bool isProcessing = false;

        public void Initialize(Game gameInstance)
        {
            game = gameInstance;
        }

        public void Initialize(List<IGameStep> initialSteps)
        {
            foreach (var step in initialSteps)
            {
                QueueStep(step);
            }
        }

        public void QueueStep(IGameStep step)
        {
            if (step != null)
            {
                steps.Enqueue(step);
            }
        }

        public void Continue()
        {
            if (isProcessing) return;

            isProcessing = true;

            try
            {
                while (steps.Count > 0 || (currentStep != null && !currentStep.IsComplete()))
                {
                    if (currentStep == null || currentStep.IsComplete())
                    {
                        // Move to next step
                        if (steps.Count > 0)
                        {
                            currentStep?.Cleanup();
                            currentStep = steps.Dequeue();
                            currentStep.Initialize();
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Execute current step
                    if (currentStep != null)
                    {
                        bool shouldContinue = currentStep.Execute();
                        if (!shouldContinue)
                        {
                            // Step is waiting for input
                            break;
                        }
                    }
                }
            }
            finally
            {
                isProcessing = false;
            }
        }

        public bool HandleCardClicked(Player player, BaseCard card)
        {
            if (currentStep != null)
            {
                currentStep.OnCardClicked(player, card);
                return true;
            }
            return false;
        }

        public bool HandleRingClicked(Player player, Ring ring)
        {
            if (currentStep != null)
            {
                currentStep.OnRingClicked(player, ring);
                return true;
            }
            return false;
        }

        public bool HandleMenuCommand(Player player, string arg, string uuid, string method)
        {
            if (currentStep != null)
            {
                currentStep.OnMenuCommand(player, arg, "", uuid, method);
                return true;
            }
            return false;
        }
    }
}
