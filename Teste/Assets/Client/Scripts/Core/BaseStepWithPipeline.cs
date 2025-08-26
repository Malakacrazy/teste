using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Minimal placeholder for BaseStepWithPipeline
    /// Just enough to allow AbilityResolver to compile.
    /// </summary>
    public abstract class BaseStepWithPipeline : IGameStep
    {
        protected Game game;

        public BaseStepWithPipeline(Game game)
        {
            this.game = game;
        }

        // Placeholder Initialize method (to be overridden)
        protected abstract void Initialize();

        // Basic IGameStep implementation
        public virtual bool Execute()
        {
            return true;
        }

        public virtual bool IsComplete => true;
        public virtual bool CanCancel => false;
    }
}
