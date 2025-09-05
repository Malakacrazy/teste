using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published for time-based mechanics and timer management
    /// </summary>
    [Serializable]
    public class TimerEvent : GameEvent
    {
        /// <summary>
        /// The type of timer event
        /// </summary>
        public string TimerType { get; private set; }
        
        /// <summary>
        /// The timer action (start, stop, pause, resume, expire)
        /// </summary>
        public string TimerAction { get; private set; }
        
        /// <summary>
        /// Timer duration in seconds
        /// </summary>
        public float Duration { get; private set; }
        
        /// <summary>
        /// Remaining time in seconds
        /// </summary>
        public float RemainingTime { get; private set; }
        
        /// <summary>
        /// Timer identifier
        /// </summary>
        public string TimerId { get; private set; }
        
        /// <summary>
        /// Whether this is a player-specific timer
        /// </summary>
        public bool IsPlayerTimer { get; private set; }
        
        /// <summary>
        /// Additional timer data
        /// </summary>
        public object TimerData { get; private set; }
        
        /// <summary>
        /// Initialize timer event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player associated with the timer</param>
        /// <param name="timerType">Type of timer</param>
        /// <param name="timerAction">Timer action</param>
        /// <param name="timerId">Timer identifier</param>
        /// <param name="duration">Timer duration</param>
        /// <param name="remainingTime">Remaining time</param>
        /// <param name="isPlayerTimer">Whether this is player-specific</param>
        /// <param name="timerData">Additional timer data</param>
        /// <param name="source">Source of the timer event</param>
        public TimerEvent(Game game, Player triggeredBy, string timerType, string timerAction,
            string timerId, float duration = 0f, float remainingTime = 0f, bool isPlayerTimer = false,
            object timerData = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            TimerType = timerType;
            TimerAction = timerAction;
            TimerId = timerId;
            Duration = duration;
            RemainingTime = remainingTime;
            IsPlayerTimer = isPlayerTimer;
            TimerData = timerData;
            
            // Add specific event data
            AddEventData("timer_type", TimerType);
            AddEventData("timer_action", TimerAction);
            AddEventData("timer_id", TimerId);
            AddEventData("duration", Duration);
            AddEventData("remaining_time", RemainingTime);
            AddEventData("is_player_timer", IsPlayerTimer);
            AddEventData("player_id", triggeredBy?.PlayerId);
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            var playerInfo = IsPlayerTimer && TriggeredBy != null ? $" for {TriggeredBy.Name}" : "";
            return $"{TimerType} timer '{TimerId}' {TimerAction}{playerInfo} ({RemainingTime:F1}s remaining)";
        }
        
        /// <summary>
        /// Static factory methods for common timer events
        /// </summary>
        public static TimerEvent PlayerClockStarted(Game game, Player player, float duration, object source = null)
        {
            return new TimerEvent(game, player, "player_clock", "start", $"clock_{player.PlayerId}", 
                duration, duration, true, null, source);
        }
        
        public static TimerEvent PlayerClockStopped(Game game, Player player, float remainingTime, object source = null)
        {
            return new TimerEvent(game, player, "player_clock", "stop", $"clock_{player.PlayerId}", 
                0f, remainingTime, true, null, source);
        }
        
        public static TimerEvent PlayerClockExpired(Game game, Player player, object source = null)
        {
            return new TimerEvent(game, player, "player_clock", "expire", $"clock_{player.PlayerId}", 
                0f, 0f, true, null, source);
        }
        
        public static TimerEvent ActionWindowTimer(Game game, Player player, string windowType, string action, 
            float duration = 0f, float remainingTime = 0f, object source = null)
        {
            return new TimerEvent(game, player, "action_window", action, $"window_{windowType}", 
                duration, remainingTime, false, windowType, source);
        }
        
        public static TimerEvent PhaseTimer(Game game, string phase, string action, float duration = 0f, 
            float remainingTime = 0f, object source = null)
        {
            return new TimerEvent(game, null, "phase", action, $"phase_{phase}", 
                duration, remainingTime, false, phase, source);
        }
        
        public static TimerEvent PromptTimer(Game game, Player player, string promptType, string action,
            float duration = 0f, float remainingTime = 0f, object source = null)
        {
            return new TimerEvent(game, player, "prompt", action, $"prompt_{promptType}_{player.PlayerId}", 
                duration, remainingTime, true, promptType, source);
        }
        
        public static TimerEvent DelayedEffect(Game game, Player player, string effectId, string action,
            float delay = 0f, object effectData = null, object source = null)
        {
            return new TimerEvent(game, player, "delayed_effect", action, effectId, 
                delay, delay, false, effectData, source);
        }
    }
}