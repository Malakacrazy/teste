using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when the game state changes (phases, turns, rounds, etc.)
    /// </summary>
    [Serializable]
    public class GameStateChangedEvent : GameEvent
    {
        /// <summary>
        /// Previous game state
        /// </summary>
        public string PreviousState { get; private set; }
        
        /// <summary>
        /// New game state
        /// </summary>
        public string NewState { get; private set; }
        
        /// <summary>
        /// Reason for state change
        /// </summary>
        public string ChangeReason { get; private set; }
        
        /// <summary>
        /// Game turn number when state changed
        /// </summary>
        public int TurnNumber { get; private set; }
        
        /// <summary>
        /// Phase duration in milliseconds (if applicable)
        /// </summary>
        public long PhaseDurationMs { get; private set; }
        
        /// <summary>
        /// Initialize game state changed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the state change</param>
        /// <param name="previousState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="changeReason">Reason for change</param>
        /// <param name="turnNumber">Current turn number</param>
        /// <param name="phaseDurationMs">Phase duration if applicable</param>
        /// <param name="source">Source of the state change</param>
        public GameStateChangedEvent(Game game, Player triggeredBy, string previousState, string newState, 
            string changeReason = "unknown", int turnNumber = 0, long phaseDurationMs = 0, object source = null) 
            : base(game, triggeredBy, source)
        {
            PreviousState = previousState;
            NewState = newState;
            ChangeReason = changeReason;
            TurnNumber = turnNumber;
            PhaseDurationMs = phaseDurationMs;
            
            // Add specific event data
            AddEventData("previous_state", previousState);
            AddEventData("new_state", newState);
            AddEventData("change_reason", changeReason);
            AddEventData("turn_number", turnNumber);
            AddEventData("phase_duration_ms", phaseDurationMs);
            AddEventData("player_id", triggeredBy?.PlayerId);
            AddEventData("state_transition", $"{previousState}→{newState}");
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public override string GetDescription()
        {
            string durationText = PhaseDurationMs > 0 ? $" (duration: {PhaseDurationMs}ms)" : "";
            return $"Game state changed: {PreviousState} → {NewState} ({ChangeReason}){durationText}";
        }
        
        /// <summary>
        /// Factory method for phase transitions
        /// </summary>
        public static GameStateChangedEvent CreatePhaseTransition(Game game, Player triggeredBy, 
            string fromPhase, string toPhase, long phaseDurationMs = 0, object source = null)
        {
            return new GameStateChangedEvent(game, triggeredBy, fromPhase, toPhase, 
                "phase_transition", game?.TurnNumber ?? 0, phaseDurationMs, source);
        }
        
        /// <summary>
        /// Factory method for turn transitions
        /// </summary>
        public static GameStateChangedEvent CreateTurnTransition(Game game, Player newActivePlayer, 
            int newTurnNumber, object source = null)
        {
            return new GameStateChangedEvent(game, newActivePlayer, "turn_transition", "new_turn", 
                $"turn_{newTurnNumber}_started", newTurnNumber, 0, source);
        }
        
        /// <summary>
        /// Factory method for round transitions
        /// </summary>
        public static GameStateChangedEvent CreateRoundTransition(Game game, Player triggeredBy, 
            int newRound, string reason = "round_completed", object source = null)
        {
            return new GameStateChangedEvent(game, triggeredBy, $"round_{newRound - 1}", $"round_{newRound}", 
                reason, game?.TurnNumber ?? 0, 0, source);
        }
    }
}