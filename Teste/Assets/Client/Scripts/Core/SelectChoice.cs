using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a choice selection in select-based targeting.
    /// Perfect C# port of the original JavaScript SelectChoice.
    /// </summary>
    [Serializable]
    public class SelectChoice
    {
        [Header("Choice Configuration")]
        public string choice;
        
        public SelectChoice(string choiceValue)
        {
            choice = choiceValue ?? throw new ArgumentNullException(nameof(choiceValue));
        }
        
        /// <summary>
        /// Get short summary for UI display and debugging
        /// </summary>
        public object GetShortSummary()
        {
            return new
            {
                id = choice,
                label = choice,
                name = choice,
                type = TargetModes.Select
            };
        }
        
        /// <summary>
        /// Get the choice value
        /// </summary>
        public string GetChoice()
        {
            return choice;
        }
        
        public override string ToString()
        {
            return choice;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is SelectChoice other)
                return choice == other.choice;
            if (obj is string str)
                return choice == str;
            return false;
        }
        
        public override int GetHashCode()
        {
            return choice?.GetHashCode() ?? 0;
        }
    }
}
