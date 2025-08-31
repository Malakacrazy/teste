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
        
        /// <summary>
        /// Public access to events list
        /// </summary>
        public List<GameEvent> Events => events;
        
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

        /// <summary>
        /// Remove an event from this window
        /// </summary>
        /// <param name="gameEvent">Event to remove</param>
        public virtual void RemoveEvent(GameEvent gameEvent)
        {
            if (events != null && gameEvent != null)
            {
                events.Remove(gameEvent);
            }
        }

        /// <summary>
        /// Add an event to this window
        /// </summary>
        /// <param name="gameEvent">Event to add</param>
        public virtual void AddEvent(GameEvent gameEvent)
        {
            if (events != null && gameEvent != null)
            {
                events.Add(gameEvent);
            }
        }


        public override string GetDebugInfo()
        {
            var eventCount = events?.Count ?? 0;
            return $"EventWindow - Processing {eventCount} events";
        }
    }
}
