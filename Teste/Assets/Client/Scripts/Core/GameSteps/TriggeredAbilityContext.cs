using System;

namespace L5RGame
{
    [System.Serializable]
    public class TriggeredAbilityContext : AbilityContext
    {
        public GameEvent triggeringEvent;
        public bool hasBeenInitiated = false;
        public GameEvent eventObject { get { return triggeringEvent; } set { triggeringEvent = value; } }
        
        public TriggeredAbilityContext() : base(new AbilityContextProperties()) { }
        
        public TriggeredAbilityContext(GameEvent gameEvent) : base(new AbilityContextProperties())
        {
            triggeringEvent = gameEvent;
            eventObject = gameEvent;
        }
        
        /// <summary>
        /// Cancel this triggered ability context
        /// </summary>
        public void Cancel()
        {
            if (triggeringEvent != null)
            {
                triggeringEvent.Cancel();
            }
        }
    }
}