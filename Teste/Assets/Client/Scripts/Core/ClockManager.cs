using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Clock configuration details for different timing systems
    /// </summary>
    [System.Serializable]
    public class ClockDetails
    {
        public ClockType type = ClockType.None;
        public int time = 0;          // Main time in minutes
        public int periods = 0;       // Number of periods (for Byoyomi)
        public int timePeriod = 0;    // Time per period in seconds (for Byoyomi)
    }

    /// <summary>
    /// Available clock types for competitive play
    /// </summary>
    public enum ClockType
    {
        None,       // No time limit
        Timer,      // Simple countdown timer
        Chess,      // Chess clock (stops when opponent's turn)
        Hourglass,  // Shared time between players
        Byoyomi     // Japanese time system with overtime periods
    }

    /// <summary>
    /// Clock state information for UI updates
    /// </summary>
    [System.Serializable]
    public class ClockState
    {
        public ClockMode mode;
        public float timeLeft;
        public int stateId;
        public float mainTime;
        public string name;
        public int periods;         // For Byoyomi
        public float timePeriod;    // For Byoyomi
    }

    /// <summary>
    /// Clock operational modes
    /// </summary>
    public enum ClockMode
    {
        Off,    // Clock is inactive
        Stop,   // Clock is stopped but ready
        Down,   // Counting down
        Up      // Counting up
    }

    /// <summary>
    /// Factory class for creating different types of clocks
    /// </summary>
    public static class ClockSelector
    {
        /// <summary>
        /// Create a clock instance based on the specified details
        /// </summary>
        public static IClock CreateClock(Player player, ClockDetails details = null)
        {
            details = details ?? new ClockDetails();

            switch (details.type)
            {
                case ClockType.None:
                    return new Clock(player, details.time * 60);

                case ClockType.Timer:
                    return new Timer(player, details.time * 60);

                case ClockType.Chess:
                    return new ChessClock(player, details.time * 60);

                case ClockType.Hourglass:
                    return new Hourglass(player, details.time * 60);

                case ClockType.Byoyomi:
                    return new Byoyomi(player, details.time * 60, details.periods, details.timePeriod);

                default:
                    throw new ArgumentException($"Unknown clock type: {details.type}");
            }
        }

        /// <summary>
        /// Get clock type from string (for serialization)
        /// </summary>
        public static ClockType ParseClockType(string typeString)
        {
            if (Enum.TryParse<ClockType>(typeString, true, out ClockType result))
            {
                return result;
            }
            return ClockType.None;
        }
    }

    /// <summary>
    /// Interface for all clock implementations
    /// </summary>
    public interface IClock
    {
        Player Player { get; }
        float TimeLeft { get; }
        float MainTime { get; }
        ClockMode Mode { get; }
        string Name { get; }
        bool IsPaused { get; }
        int StateId { get; }

        void Start();
        void Stop();
        void Pause();
        void Restart();
        void Reset();
        void Modify(float seconds);
        void OpponentStart();
        void TimeRanOut();
        ClockState GetState();
        void UpdateTimeLeft(float deltaTime);
    }

    /// <summary>
    /// Base clock implementation with core timing functionality
    /// </summary>
    public class Clock : IClock
    {
        [Header("Clock Properties")]
        public Player Player { get; protected set; }
        public float TimeLeft { get; protected set; }
        public float MainTime { get; protected set; }
        public ClockMode Mode { get; protected set; } = ClockMode.Off;
        public string Name { get; protected set; } = "Clock";
        public bool IsPaused { get; protected set; } = false;
        public int StateId { get; protected set; } = 0;

        protected float timerStart = 0f;
        protected bool isRunning = false;

        public Clock(Player player, float time)
        {
            Player = player;
            MainTime = time;
            TimeLeft = time;
            Name = "Clock";
        }

        public virtual void Start()
        {
            if (!IsPaused)
            {
                timerStart = Time.time;
                isRunning = true;
                UpdateStateId();
                
                if (Player?.game?.debugMode == true)
                {
                    Debug.Log($"⏰ {Name} started for {Player.name}");
                }
            }
        }

        public virtual void Stop()
        {
            if (isRunning && timerStart > 0)
            {
                float elapsed = Time.time - timerStart;
                UpdateTimeLeft(elapsed);
                timerStart = 0f;
                isRunning = false;
                UpdateStateId();
                
                if (Player?.game?.debugMode == true)
                {
                    Debug.Log($"⏰ {Name} stopped for {Player.name}. Time left: {TimeLeft:F1}s");
                }
            }
        }

        public virtual void Pause()
        {
            IsPaused = true;
            if (isRunning)
            {
                Stop();
            }
            
            if (Player?.game?.debugMode == true)
            {
                Debug.Log($"⏸️ {Name} paused for {Player.name}");
            }
        }

        public virtual void Restart()
        {
            IsPaused = false;
            
            if (Player?.game?.debugMode == true)
            {
                Debug.Log($"▶️ {Name} unpaused for {Player.name}");
            }
        }

        public virtual void Modify(float seconds)
        {
            TimeLeft += seconds;
            if (TimeLeft < 0)
            {
                TimeLeft = 0;
            }
            UpdateStateId();
        }

        public virtual void Reset()
        {
            // Override in derived classes for specific reset behavior
        }

        public virtual void OpponentStart()
        {
            timerStart = Time.time;
            UpdateStateId();
        }

        public virtual void TimeRanOut()
        {
            // Override in derived classes for specific timeout behavior
        }

        public virtual void UpdateTimeLeft(float deltaTime)
        {
            if (TimeLeft <= 0 || deltaTime < 0)
            {
                return;
            }

            switch (Mode)
            {
                case ClockMode.Down:
                    Modify(-deltaTime);
                    if (TimeLeft <= 0)
                    {
                        TimeLeft = 0;
                        TimeRanOut();
                    }
                    break;

                case ClockMode.Up:
                    Modify(deltaTime);
                    break;
            }
        }

        public void UpdateStateId()
        {
            StateId++;
        }

        public virtual ClockState GetState()
        {
            return new ClockState
            {
                mode = Mode,
                timeLeft = TimeLeft,
                stateId = StateId,
                mainTime = MainTime,
                name = Name
            };
        }

        // Update method for MonoBehaviour integration
        public virtual void Update()
        {
            if (isRunning && !IsPaused)
            {
                float elapsed = Time.time - timerStart;
                timerStart = Time.time;
                UpdateTimeLeft(elapsed);
            }
        }
    }

    /// <summary>
    /// Simple countdown timer implementation
    /// </summary>
    public class Timer : Clock
    {
        public Timer(Player player, float time) : base(player, time)
        {
            Mode = ClockMode.Down;
            Name = "Timer";
        }

        public override void TimeRanOut()
        {
            Player?.game?.AddMessage("{0}'s timer has expired", Player.name);
            
            if (Player?.game?.debugMode == true)
            {
                Debug.Log($"⏰ Timer expired for {Player.name}");
            }
        }
    }

    /// <summary>
    /// Chess clock implementation that stops when opponent's turn begins
    /// </summary>
    public class ChessClock : Clock
    {
        public ChessClock(Player player, float time) : base(player, time)
        {
            Mode = ClockMode.Stop;
            Name = "Chess Clock";
        }

        public override void Start()
        {
            Mode = ClockMode.Down;
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            Mode = ClockMode.Stop;
        }

        public override void TimeRanOut()
        {
            Player?.game?.AddMessage("{0}'s clock has run out", Player.name);
            
            if (Player.opponent != null && Player.opponent.clock.TimeLeft > 0)
            {
                Player.game.RecordWinner(Player.opponent, "clock");
            }
            
            if (Player?.game?.debugMode == true)
            {
                Debug.Log($"⏰ Chess clock ran out for {Player.name}");
            }
        }
    }

    /// <summary>
    /// Hourglass implementation where opponent gains time when player uses time
    /// </summary>
    public class Hourglass : ChessClock
    {
        public Hourglass(Player player, float time) : base(player, time)
        {
            Name = "Hourglass";
        }

        public override void OpponentStart()
        {
            Mode = ClockMode.Up;
            base.OpponentStart();
        }

        public override void UpdateTimeLeft(float deltaTime)
        {
            base.UpdateTimeLeft(deltaTime);
            
            // In hourglass mode, when this player loses time, opponent gains time
            if (Mode == ClockMode.Down && Player.opponent?.clock != null)
            {
                Player.opponent.clock.Modify(deltaTime);
            }
        }
    }

    /// <summary>
    /// Byoyomi (Japanese) time system with overtime periods
    /// </summary>
    public class Byoyomi : ChessClock
    {
        [Header("Byoyomi Properties")]
        public int Periods { get; private set; }
        public float TimePeriod { get; private set; }
        public int PeriodsRemaining { get; private set; }

        public Byoyomi(Player player, float time, int periods, float timePeriod) : base(player, time)
        {
            Periods = periods;
            TimePeriod = timePeriod;
            PeriodsRemaining = periods;
            TimeLeft = time + (periods * timePeriod);
            Name = "Byoyomi";
        }

        public override void Reset()
        {
            if (TimeLeft > 0 && TimeLeft < Periods * TimePeriod)
            {
                PeriodsRemaining = Mathf.CeilToInt(TimeLeft / TimePeriod);
                TimeLeft = PeriodsRemaining * TimePeriod;
                UpdateStateId();
                
                if (Player?.game?.debugMode == true)
                {
                    Debug.Log($"🕰️ Byoyomi reset for {Player.name}. Periods remaining: {PeriodsRemaining}");
                }
            }
        }

        public override void TimeRanOut()
        {
            if (PeriodsRemaining > 0)
            {
                // Enter next byoyomi period
                PeriodsRemaining--;
                TimeLeft = TimePeriod;
                
                Player?.game?.AddMessage("{0} enters byoyomi period {1}", Player.name, Periods - PeriodsRemaining);
                
                if (Player?.game?.debugMode == true)
                {
                    Debug.Log($"🕰️ {Player.name} enters byoyomi period {Periods - PeriodsRemaining}");
                }
            }
            else
            {
                // All periods exhausted
                base.TimeRanOut();
            }
        }

        public override ClockState GetState()
        {
            var state = base.GetState();
            state.periods = PeriodsRemaining;
            state.timePeriod = TimePeriod;
            return state;
        }

        /// <summary>
        /// Check if currently in byoyomi periods
        /// </summary>
        public bool IsInByoyomi()
        {
            return TimeLeft <= Periods * TimePeriod;
        }

        /// <summary>
        /// Get current period number (1-based)
        /// </summary>
        public int GetCurrentPeriod()
        {
            if (!IsInByoyomi())
            {
                return 0; // Main time
            }
            return Periods - PeriodsRemaining + 1;
        }
    }

    /// <summary>
    /// Clock manager component for handling multiple clocks in a game
    /// </summary>
    public class ClockManager : MonoBehaviour
    {
        [Header("Clock Management")]
        public bool enableClocks = true;
        public bool debugMode = false;
        public ClockDetails defaultClockSettings = new ClockDetails();

        [Header("Global Clock Controls")]
        public bool allClocksPaused = false;
        public float globalTimeMultiplier = 1.0f;

        // Active clocks
        private Dictionary<Player, IClock> playerClocks = new Dictionary<Player, IClock>();
        private Game game;

        // Events
        public event Action<Player, IClock> OnClockCreated;
        public event Action<Player> OnClockExpired;
        public event Action<Player> OnClockWarning;

        void Awake()
        {
            game = FindObjectOfType<Game>();
        }

        void Update()
        {
            if (enableClocks && !allClocksPaused)
            {
                UpdateAllClocks();
            }
        }

        #region Clock Creation and Management

        /// <summary>
        /// Create clock for player
        /// </summary>
        public IClock CreateClockForPlayer(Player player, ClockDetails details = null)
        {
            details = details ?? defaultClockSettings;
            
            IClock clock = ClockSelector.CreateClock(player, details);
            
            if (playerClocks.ContainsKey(player))
            {
                playerClocks[player] = clock;
            }
            else
            {
                playerClocks.Add(player, clock);
            }

            // Assign clock to player
            player.clock = clock;
            
            OnClockCreated?.Invoke(player, clock);
            
            if (debugMode)
            {
                Debug.Log($"⏰ Created {clock.Name} for {player.name} with {details.time} minutes");
            }

            return clock;
        }

        /// <summary>
        /// Remove clock for player
        /// </summary>
        public void RemoveClockForPlayer(Player player)
        {
            if (playerClocks.ContainsKey(player))
            {
                playerClocks.Remove(player);
                player.clock = null;
                
                if (debugMode)
                {
                    Debug.Log($"⏰ Removed clock for {player.name}");
                }
            }
        }

        /// <summary>
        /// Get clock for player
        /// </summary>
        public IClock GetClockForPlayer(Player player)
        {
            return playerClocks.ContainsKey(player) ? playerClocks[player] : null;
        }

        #endregion

        #region Clock Control

        /// <summary>
        /// Start all clocks
        /// </summary>
        public void StartAllClocks()
        {
            foreach (var clock in playerClocks.Values)
            {
                clock.Start();
            }
            
            if (debugMode)
            {
                Debug.Log("⏰ Started all clocks");
            }
        }

        /// <summary>
        /// Stop all clocks
        /// </summary>
        public void StopAllClocks()
        {
            foreach (var clock in playerClocks.Values)
            {
                clock.Stop();
            }
            
            if (debugMode)
            {
                Debug.Log("⏰ Stopped all clocks");
            }
        }

        /// <summary>
        /// Pause all clocks
        /// </summary>
        public void PauseAllClocks()
        {
            allClocksPaused = true;
            foreach (var clock in playerClocks.Values)
            {
                clock.Pause();
            }
            
            if (debugMode)
            {
                Debug.Log("⏸️ Paused all clocks");
            }
        }

        /// <summary>
        /// Resume all clocks
        /// </summary>
        public void ResumeAllClocks()
        {
            allClocksPaused = false;
            foreach (var clock in playerClocks.Values)
            {
                clock.Restart();
            }
            
            if (debugMode)
            {
                Debug.Log("▶️ Resumed all clocks");
            }
        }

        /// <summary>
        /// Reset all clocks
        /// </summary>
        public void ResetAllClocks()
        {
            foreach (var clock in playerClocks.Values)
            {
                clock.Reset();
            }
            
            if (debugMode)
            {
                Debug.Log("🔄 Reset all clocks");
            }
        }

        #endregion

        #region Update and Events

        /// <summary>
        /// Update all active clocks
        /// </summary>
        private void UpdateAllClocks()
        {
            float deltaTime = Time.deltaTime * globalTimeMultiplier;
            
            foreach (var kvp in playerClocks)
            {
                var player = kvp.Key;
                var clock = kvp.Value;
                
                if (clock is Clock baseClock)
                {
                    baseClock.Update();
                }
                
                // Check for warnings (e.g., 30 seconds remaining)
                CheckClockWarnings(player, clock);
            }
        }

        /// <summary>
        /// Check for clock warnings
        /// </summary>
        private void CheckClockWarnings(Player player, IClock clock)
        {
            const float warningThreshold = 30f; // 30 seconds
            
            if (clock.TimeLeft <= warningThreshold && clock.TimeLeft > 0 && clock.Mode == ClockMode.Down)
            {
                // Only warn once per threshold crossing
                if (!player.HasFlag("clock_warning_sent"))
                {
                    OnClockWarning?.Invoke(player);
                    player.SetFlag("clock_warning_sent", true);
                    
                    if (debugMode)
                    {
                        Debug.Log($"⚠️ Clock warning for {player.name}: {clock.TimeLeft:F1}s remaining");
                    }
                }
            }
            else if (clock.TimeLeft > warningThreshold)
            {
                // Reset warning flag
                player.SetFlag("clock_warning_sent", false);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get all clock states for UI updates
        /// </summary>
        public Dictionary<Player, ClockState> GetAllClockStates()
        {
            var states = new Dictionary<Player, ClockState>();
            
            foreach (var kvp in playerClocks)
            {
                states[kvp.Key] = kvp.Value.GetState();
            }
            
            return states;
        }

        /// <summary>
        /// Get formatted time string
        /// </summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0)
                seconds = 0;
                
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            
            if (minutes > 0)
            {
                return $"{minutes}:{secs:D2}";
            }
            else
            {
                return $"0:{secs:D2}";
            }
        }

        /// <summary>
        /// Get debug information
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"ClockManager Debug Info:\n";
            info += $"Clocks Enabled: {enableClocks}\n";
            info += $"All Clocks Paused: {allClocksPaused}\n";
            info += $"Global Time Multiplier: {globalTimeMultiplier}\n";
            info += $"Active Clocks: {playerClocks.Count}\n";

            foreach (var kvp in playerClocks)
            {
                var player = kvp.Key;
                var clock = kvp.Value;
                info += $"\n{player.name}: {clock.Name}";
                info += $" - Time: {FormatTime(clock.TimeLeft)}";
                info += $" - Mode: {clock.Mode}";
                info += $" - Paused: {clock.IsPaused}";
                
                if (clock is Byoyomi byoyomi)
                {
                    info += $" - Periods: {byoyomi.PeriodsRemaining}/{byoyomi.Periods}";
                }
            }

            return info;
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for player clock integration
    /// </summary>
    public static class ClockExtensions
    {
        /// <summary>
        /// Start player's clock
        /// </summary>
        public static void StartClock(this Player player)
        {
            player.clock?.Start();
        }

        /// <summary>
        /// Stop player's clock
        /// </summary>
        public static void StopClock(this Player player)
        {
            player.clock?.Stop();
        }

        /// <summary>
        /// Pause player's clock
        /// </summary>
        public static void PauseClock(this Player player)
        {
            player.clock?.Pause();
        }

        /// <summary>
        /// Get formatted time remaining
        /// </summary>
        public static string GetFormattedTimeRemaining(this Player player)
        {
            if (player.clock == null)
                return "--:--";
                
            return ClockManager.FormatTime(player.clock.TimeLeft);
        }

        /// <summary>
        /// Check if player's clock is running low
        /// </summary>
        public static bool IsClockRunningLow(this Player player, float threshold = 60f)
        {
            return player.clock != null && player.clock.TimeLeft <= threshold && player.clock.TimeLeft > 0;
        }
    }
}