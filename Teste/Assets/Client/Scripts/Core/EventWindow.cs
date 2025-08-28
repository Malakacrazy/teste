using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Event window step
    /// </summary>
    public class EventWindow : BaseStepWithPipeline, IGameStep
    {
        protected List<GameEvent> events;
        
        public EventWindow(Game game) : base(game)
        {
        }
        
        public EventWindow(Game game, List<GameEvent> events) : base(game)
        {
            this.events = events ?? new List<GameEvent>();
        }

        protected override void InitializePipeline()
        {
            base.InitializePipeline();
            // Add event processing steps here
        }

        bool IGameStep.IsComplete() => IsComplete;

        public override string GetDebugInfo()
        {
            var eventCount = events?.Count ?? 0;
            return $"EventWindow - Processing {eventCount} events";
        }
    }
}
