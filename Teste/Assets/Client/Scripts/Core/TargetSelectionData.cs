using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Data structure for target selection operations
    /// </summary>
    [Serializable]
    public class TargetSelectionData
    {
        /// <summary>
        /// Title to display for target selection
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of what is being selected
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Available targets to choose from
        /// </summary>
        public List<object> AvailableTargets { get; set; } = new List<object>();
        
        /// <summary>
        /// Maximum number of targets that can be selected
        /// </summary>
        public int MaxTargets { get; set; } = 1;
        
        /// <summary>
        /// Minimum number of targets that must be selected
        /// </summary>
        public int MinTargets { get; set; } = 1;
        
        /// <summary>
        /// Whether the selection can be cancelled
        /// </summary>
        public bool AllowCancel { get; set; } = true;
        
        /// <summary>
        /// Filter criteria for valid targets
        /// </summary>
        public Dictionary<string, object> FilterCriteria { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Additional properties for the selection
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Target object (for compatibility)
        /// </summary>
        public object Target { get; set; }
        
        /// <summary>
        /// Display name for the target (for compatibility)
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Whether the target is valid (for compatibility)
        /// </summary>
        public bool IsValid { get; set; } = true;
        
        /// <summary>
        /// Create target selection data
        /// </summary>
        /// <param name="title">Selection title</param>
        /// <param name="description">Selection description</param>
        /// <param name="availableTargets">Available targets</param>
        /// <param name="maxTargets">Max targets</param>
        /// <param name="minTargets">Min targets</param>
        /// <param name="allowCancel">Allow cancel</param>
        public TargetSelectionData(string title, string description, List<object> availableTargets, 
            int maxTargets = 1, int minTargets = 1, bool allowCancel = true)
        {
            Title = title;
            Description = description;
            AvailableTargets = availableTargets ?? new List<object>();
            MaxTargets = maxTargets;
            MinTargets = minTargets;
            AllowCancel = allowCancel;
        }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public TargetSelectionData()
        {
        }
    }
}