using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for game steps that use a pipeline system
    /// </summary>
    public abstract class BaseStepWithPipeline : IGameStep
    {
        protected Game game;

        public BaseStepWithPipeline(Game game)
        {
            this.game = game;
        }

        // Virtual Initialize method that can be overridden
        public virtual void Initialize()
        {
            // Default implementation - can be overridden by derived classes
        }

        // IGameStep implementation
        public virtual bool Execute()
        {
            return true;
        }

        public virtual bool IsComplete()
        {
            return true;
        }

        public virtual bool CanCancel => false;
    }
}
