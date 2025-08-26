using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GamePipeline : MonoBehaviour
    {
        private List<IGameStep> steps = new List<IGameStep>();
        private int currentStepIndex = 0;

        public void Initialize()
        {
            steps.Clear();
            currentStepIndex = 0;
        }

        public void Initialize(List<IGameStep> initialSteps)
        {
            steps = new List<IGameStep>(initialSteps);
            currentStepIndex = 0;
        }

        public void HandleCardClicked(Player player, BaseCard card)
        {
            // Handle card click logic
        }

        public bool HandleRingClicked(Player player, Ring ring)
        {
            // Handle ring click logic
            return false;
        }

        public bool HandleMenuCommand(Player player, string arg, string uuid, string method)
        {
            // Handle menu command logic
            return false;
        }

        public T QueueStep<T>(T step) where T : IGameStep
        {
            steps.Add(step);
            return step;
        }

        public void Continue()
        {
            // Execute current step and move to next
            if (currentStepIndex < steps.Count)
            {
                var currentStep = steps[currentStepIndex];
                if (currentStep.Execute() && currentStep.IsComplete())
                {
                    currentStepIndex++;
                }
            }
        }

        public bool IsComplete()
        {
            return currentStepIndex >= steps.Count;
        }
    }
}
