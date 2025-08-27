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

        // IGameStep implementation
        public virtual bool Execute()
        {
            return true;
        }

        public virtual bool IsComplete()
        {
            return true;
        }

        public virtual bool Continue()
        {
            return !IsComplete();
        }

        public virtual void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Default implementation - can be overridden by derived classes
        }

        public virtual void OnCardClicked(Player player, BaseCard card)
        {
            // Default implementation - can be overridden by derived classes
        }

        public virtual void OnRingClicked(Player player, Ring ring)
        {
            // Default implementation - can be overridden by derived classes
        }

        public virtual void Initialize()
        {
            // Default implementation - can be overridden by derived classes
        }

        public virtual void Cleanup()
        {
            // Default implementation - can be overridden by derived classes
        }

        public virtual bool CanCancel => false;
    }
}
