using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Manages ability usage limits (per round, per conflict, etc.)
    /// </summary>
    [System.Serializable]
    public class AbilityLimit
    {
        public int max = 1;
        public int maxUses = 1;
        public string identifier;
        public string limitType = "unlimited";
        public bool perRound;
        public bool perConflict;
        public Dictionary<string, int> currentUses;
        public BaseAbility ability;

        public AbilityLimit()
        {
            currentUses = new Dictionary<string, int>();
        }
        
        public AbilityLimit(int maxUses, string limitIdentifier = null)
        {
            this.maxUses = maxUses;
            this.max = maxUses;
            identifier = limitIdentifier;
            currentUses = new Dictionary<string, int>();
        }

        public static AbilityLimit PerRound(int max)
        {
            return new AbilityLimit { limitType = "perRound", maxUses = max };
        }

        public static AbilityLimit PerConflict(int max)
        {
            return new AbilityLimit { limitType = "perConflict", maxUses = max };
        }

        public static AbilityLimit PerPhase(int max)
        {
            return new AbilityLimit { limitType = "perPhase", maxUses = max };
        }

        public void RegisterEvents(Game game)
        {
            // Register event handlers based on limit type
        }

        public bool IsAtMax(Player player = null)
        {
            // Check if limit has been reached
            return false;
        }

        public void Increment(Player player = null)
        {
            // Increment usage counter
        }
        
        /// <summary>
        /// Mark the limit as used
        /// </summary>
        public void MarkUsed()
        {
            Increment();
        }

        /// <summary>
        /// Check if the limit has expired
        /// </summary>
        public bool IsExpired()
        {
            return IsAtMax();
        }

        /// <summary>
        /// Unregister event handlers
        /// </summary>
        public void UnregisterEvents()
        {
            // Unregister event handlers based on limit type
        }
        
        /// <summary>
        /// Reset the limit usage counter
        /// </summary>
        public void Reset()
        {
            currentUses.Clear();
        }
    }
}
