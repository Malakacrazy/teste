using System.Collections.Generic;
using UnityEngine;

namespace L5RGame.Core
{
    /// <summary>
    /// Specialized event window for handling "then" abilities that trigger after the main event resolution.
    /// This window type filters out certain ability types and transfers events to the previous window when reset.
    /// </summary>
    public class ThenEventWindow : EventWindow
    {
        #region Constructor
        
        /// <summary>
        /// Initialize a new ThenEventWindow
        /// </summary>
        /// <param name="game">Reference to the main game instance</param>
        /// <param name="events">Events to process in this window</param>
        /// <param name="previousWindow">The previous event window in the chain</param>
        public ThenEventWindow(Game game, List<GameEvent> events, EventWindow previousWindow = null) 
            : base(game, events, previousWindow)
        {
            Debug.Log($"🔄 ThenEventWindow: Created with {events?.Count ?? 0} events");
        }
        
        #endregion
        
        #region EventWindow Overrides
        
        /// <summary>
        /// Open an ability window, but filter out forced reactions and reactions
        /// since "then" abilities should not trigger these types
        /// </summary>
        /// <param name="abilityType">The type of ability window to open</param>
        /// <returns>True if window was opened or skipped appropriately</returns>
        protected override bool OpenWindow(string abilityType)
        {
            // Skip forced reactions and reactions for "then" windows
            // These ability types should not be available during "then" resolution
            if (abilityType == AbilityTypes.ForcedReaction || abilityType == AbilityTypes.Reaction)
            {
                Debug.Log($"🔄 ThenEventWindow: Skipping {abilityType} window for then abilities");
                return true; // Continue pipeline without opening the window
            }
            
            // For all other ability types, use the base implementation
            return base.OpenWindow(abilityType);
        }
        
        /// <summary>
        /// Reset the current event window by transferring all events back to the previous window
        /// This ensures that any unresolved events from the "then" window are not lost
        /// </summary>
        /// <returns>True when reset is complete</returns>
        protected override bool ResetCurrentEventWindow()
        {
            // Transfer all events from this window back to the previous window
            // This is important for maintaining event continuity in the event chain
            if (previousEventWindow != null)
            {
                foreach (var gameEvent in events)
                {
                    previousEventWindow.AddEvent(gameEvent);
                    Debug.Log($"🔄 ThenEventWindow: Transferred event '{gameEvent.name}' back to previous window");
                }
                
                Debug.Log($"🔄 ThenEventWindow: Transferred {events.Count} events to previous window");
            }
            else
            {
                Debug.LogWarning("⚠️ ThenEventWindow: No previous window to transfer events to!");
            }
            
            // Call base implementation to complete the reset process
            return base.ResetCurrentEventWindow();
        }
        
        #endregion
        
        #region Debug and Utility Methods
        
        /// <summary>
        /// Get debug information about this window
        /// </summary>
        /// <returns>Debug info string</returns>
        public override string GetDebugInfo()
        {
            return $"ThenEventWindow: {events.Count} events, " +
                   $"Previous: {(previousEventWindow != null ? previousEventWindow.GetType().Name : "None")}";
        }
        
        /// <summary>
        /// Check if this window has any "then" abilities ready to execute
        /// </summary>
        /// <returns>True if there are then abilities waiting</returns>
        public bool HasThenAbilities()
        {
            return thenAbilities != null && thenAbilities.Count > 0;
        }
        
        #endregion
    }
}
